#nullable enable
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Enums;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Support;
using SmartRAG.Models;
using SmartRAG.Models.Results;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartRAG.Services.Support;


/// <summary>
/// Service for classifying query intent (conversation vs information)
/// </summary>
public class QueryIntentClassifierService : IQueryIntentClassifierService
{
    private const int MinTokenCountForInformation = 8;
    private const int MaxTokenCountForInformation = 12;
    private const int MaxHistoryLength = 400;
    private const int MinTokenLength = 2;
    private const int MaxShortConversationLength = 25;

    private readonly IAIService _aiService;
    private readonly ILogger<QueryIntentClassifierService> _logger;
    private readonly ITextNormalizationService _textNormalizationService;
    private readonly SmartRagOptions _options;

    /// <summary>
    /// Initializes a new instance of the QueryIntentClassifierService
    /// </summary>
    /// <param name="aiService">AI service for text generation</param>
    /// <param name="logger">Logger instance for this service</param>
    /// <param name="textNormalizationService">Service for text normalization</param>
    /// <param name="options">SmartRAG configuration options</param>
    public QueryIntentClassifierService(
        IAIService aiService,
        ILogger<QueryIntentClassifierService> logger,
        ITextNormalizationService textNormalizationService,
        IOptions<SmartRagOptions> options)
    {
        _aiService = aiService;
        _logger = logger;
        _textNormalizationService = textNormalizationService;
        _options = options.Value;
    }

    /// <summary>
    /// [AI Query] Analyzes the query intent and returns both conversation classification and tokenized query terms.
    /// </summary>
    /// <param name="query">User query to analyze</param>
    /// <param name="conversationHistory">Optional conversation history for context</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Analysis result containing conversation flag and normalized tokens for non-conversational queries</returns>
    public async Task<QueryIntentAnalysisResult> AnalyzeQueryAsync(string query, string? conversationHistory = null, System.Threading.CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new QueryIntentAnalysisResult(true, Array.Empty<string>());
        }

        var trimmedQuery = query.Trim();
        var normalizedForMatching = _textNormalizationService.NormalizeForMatching(trimmedQuery);

        var heuristic = HeuristicClassify(trimmedQuery, out var heuristicScore);

        switch (heuristic)
        {
            case HeuristicDecision.Conversation:
                return new QueryIntentAnalysisResult(true, Array.Empty<string>());
            case HeuristicDecision.Information when heuristicScore >= 4:
            {
                var heuristicTokens = _options.Features.EnableDocumentSearch
                    ? TokenizeForSearch(normalizedForMatching)
                    : Array.Empty<string>();
                return new QueryIntentAnalysisResult(false, heuristicTokens);
            }
            case HeuristicDecision.Unknown:
            default:
                try
                {
                    var historySnippet = string.Empty;
                    if (!string.IsNullOrWhiteSpace(conversationHistory))
                    {
                        var normalizedHistory = conversationHistory.Trim();
                        historySnippet = normalizedHistory.Length > MaxHistoryLength
                            ? normalizedHistory.Substring(normalizedHistory.Length - MaxHistoryLength, MaxHistoryLength)
                            : normalizedHistory;
                    }

                    var classificationPrompt = string.Format(
                        CultureInfo.InvariantCulture,
                        @"You MUST analyze the input and decide whether it is CONVERSATION or INFORMATION, and also provide search tokens when it is INFORMATION.

CRITICAL: Classify as CONVERSATION if:
- Greeting in any human language (generic salutations/greetings)
- About the AI itself (identity, capabilities, technical specifications, model information)
- Small talk (well-being, introductions, origin, casual chat)
- Polite chat (gratitude, farewells, niceties)
- Follow-up questions about the AI when previous conversation was about the AI AND current question is still about the AI

✓ Classify as INFORMATION if:
- Contains ANY technical terms, acronyms, or domain-specific vocabulary about external topics
- Asks for definitions, explanations, parts, or features of something external (not about the AI)
- Contains data-request intent (show, list, find, calculate, total, count, sum) about external data
- Contains question words with informational intent about external topics
- Contains numbers/dates indicating data queries about external data
- Contains specific entity references (not about the AI)
- Topic has changed from AI to external subject (even if previous conversation was about AI)
- CRITICAL: User asks about factual data that could exist in documents/databases - even if phrased as personal or possessive. ALWAYS classify as INFORMATION.
- CRITICAL: Queries about data in contracts, agreements, or records - even when phrased as personal forgetfulness, possession, or help requests - are INFORMATION. Such data exists in uploaded documents.

{1}

User: ""{0}""

CRITICAL CONTEXT RULES:
- If conversation history shows previous questions about the AI AND current question is still about the AI, classify as CONVERSATION
- If conversation history shows AI questions BUT current question is about external topic, classify as INFORMATION
- Topic change detection: If user switches from AI questions to questions about external entities, it is INFORMATION
- If the user asks about a specific external topic, concept, or entity (not the AI), it is INFORMATION
- CRITICAL: If the message contains BOTH greeting/small-talk AND a clear data request (asking for dates, amounts, or specific info from documents), classify as INFORMATION. The data request takes precedence.
- If unsure and no conversation history about AI, default to INFORMATION
- If unsure but conversation history shows AI-related questions AND current question seems about AI, default to CONVERSATION
- If unsure but current question contains external entity references, default to INFORMATION

OUTPUT FORMAT (JSON ONLY):
- You MUST return a single JSON object with the following shape:
- {{
""type"": ""CONVERSATION"" or ""INFORMATION"",
""tokens"": [ /* array of strings, can be empty for CONVERSATION */ ],
""answer"": ""Natural language answer for conversation queries, empty for information queries""
  }}

ANSWER RULES (CRITICAL FOR CONVERSATION QUERIES):
- For CONVERSATION queries, the ""answer"" field MUST contain a SPECIFIC, PERSONALIZED response to the user's question
- CRITICAL: You MUST read and understand the user's question carefully
- CRITICAL: You MUST provide an answer that DIRECTLY addresses what the user is asking
- CRITICAL: If the user asks about the AI (name, identity, capabilities), provide a SPECIFIC answer about the AI, NOT a generic greeting
- CRITICAL: If conversation history exists, read it carefully to understand the context and provide a CONTINUOUS, CONTEXTUAL response
- CRITICAL: Do NOT repeat generic greetings or standard responses - answer the SPECIFIC question being asked
- CRITICAL: If the user is frustrated or asking why you didn't answer, acknowledge their concern and provide the requested information
- CRITICAL: If the user asks about the AI's name or identity, respond with a specific identifier appropriate for the AI assistant
- CRITICAL: If the user asks why you didn't answer a previous question, acknowledge it and provide the answer they were looking for
- CRITICAL: The answer MUST be relevant to the CURRENT question, not a generic template response
- CRITICAL: Answer in the SAME language as the user's question
- Examples of GOOD answer patterns:
  * User asks about AI name → Answer should identify the AI assistant with a specific name or identifier
  * User asks why previous question wasn't answered → Answer should acknowledge the issue and provide the requested information
  * User asks how AI is doing → Answer should respond to the greeting appropriately
- Examples of BAD answer patterns (DO NOT use):
  * Generic greeting that doesn't address the specific question (when user asks a specific question)
  * Repeating previous responses without addressing the current question
  * Ignoring the user's specific question and providing a template response
  * Using a different language than the user's question

TOKEN RULES (CRITICAL FOR INFORMATION QUERIES):
- PURPOSE: These tokens will be used to search through document chunks to find relevant information. More comprehensive tokens = better search results.
- For INFORMATION queries:
  - ""tokens"" MUST be a non-empty array with AT LEAST {2} tokens (REQUIRED minimum, aim for {2}-{3} tokens for comprehensive coverage).
  - CRITICAL: If you return fewer than {2} tokens, the search will fail. Always aim for the full range.
  - CRITICAL: ONLY use words that appear in the user query. Do NOT add translations, synonyms, or words in other languages.
  - CRITICAL: Do NOT translate words from the query to another language. Use ONLY the exact language of the query.
  - CRITICAL: DO NOT CHANGE ANY CHARACTERS. Use EXACT characters from the query. If query has ""WordA"", return ""worda"" (lowercase only, same characters). If query has special characters, keep them EXACTLY as they are. NEVER transliterate or normalize characters.
  - CRITICAL: Include ALL question words from the query (all interrogative/question words that appear in the query, regardless of language - these are words that indicate the query is asking for information). Question words are ESSENTIAL for search.
  - Include BOTH single words AND important multi-word phrases from the query.
  - CRITICAL FOR WORD VARIANTS: For each significant word in the query, include its grammatical variants:
* If query has ""WordWithSuffix"", include: ""WordWithSuffix"", ""WordRoot"", ""WordRootVariant""
* If query has ""PluralWord"", include: ""PluralWord"", ""SingularWord"", ""PluralWordRoot""
* If query has ""WordInLocation"", include: ""WordInLocation"", ""WordRoot"", ""LocationForm""
* Generate ALL possible grammatical forms that might appear in documents (case forms, number forms, tense forms, etc. depending on the language's grammar rules)
  - Include ALL question words FROM THE QUERY (all interrogative words that appear in the query, regardless of language) - these are critical for search.
  - Use lower-case for all tokens while preserving original character encoding EXACTLY (e.g., if query has ""WordA"", keep it as ""worda"" with exact same special characters).
  - Remove punctuation and control characters ONLY (do NOT change letters).
  - Include meaningful words and stable stems that reflect how users naturally search in that language.
  - Do NOT invent tokens unrelated to the user query.
  - Do NOT add translations or equivalent words in other languages.
  - Prefer tokens that are likely to appear in document chunks (focus on nouns, key verbs, important phrases, and ALL question words).
  - EXAMPLE: For query ""How many items are in the warehouse?"" return: [""how"", ""many"", ""items"", ""item"", ""are"", ""in"", ""warehouse"", ""the"", ""how many"", ""items in"", ""warehouse items""] (10+ tokens with variants and phrases)
  - EXAMPLE: For query ""Where is ItemX located and how many are available?"" return: [""where"", ""is"", ""itemx"", ""item"", ""located"", ""location"", ""and"", ""how"", ""many"", ""are"", ""available"", ""how many"", ""where is"", ""itemx located"", ""are available""] (15+ tokens with all question words, variants, and phrases)
- For CONVERSATION:
  - ""tokens"" MAY be an empty array.
  - ""answer"" MUST contain a helpful natural language reply.

IMPORTANT:
- Return ONLY valid JSON.
- Do NOT wrap JSON in markdown code blocks (no ```json or ```).
- Do NOT add comments, explanations, markdown, or extra text outside the JSON object.
- Return the raw JSON object directly, starting with {{ and ending with }}.",
                        normalizedForMatching,
                        string.IsNullOrWhiteSpace(historySnippet) ? "" : $"Conversation History:\n\"{historySnippet}\"\n\n",
                        MinTokenCountForInformation,
                        MaxTokenCountForInformation);

                    cancellationToken.ThrowIfCancellationRequested();
                    var classification = await _aiService.GenerateResponseAsync(classificationPrompt, Array.Empty<string>()).ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(classification))
                    {
                        return new QueryIntentAnalysisResult(true, Array.Empty<string>());
                    }

                    try
                    {
                        var cleanedClassification = TryExtractJsonFromResponse(classification) ?? classification;
                        using var document = JsonDocument.Parse(cleanedClassification);
                        var root = document.RootElement;

                        if (root.ValueKind != JsonValueKind.Object)
                        {
                            return FallbackFromPlainClassification(classification, normalizedForMatching);
                        }

                        if (!root.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
                        {
                            return FallbackFromPlainClassification(classification, normalizedForMatching);
                        }

                        var typeValue = typeElement.GetString()?.Trim().ToUpperInvariant();
                        if (string.Equals(typeValue, "CONVERSATION", StringComparison.Ordinal))
                        {
                            if (HasQuestionPunctuation(trimmedQuery) && trimmedQuery.Length > 40 &&
                                Tokenize(trimmedQuery).Length >= 6)
                            {
                                _logger.LogDebug("Overriding LLM CONVERSATION to INFORMATION: query has data-request pattern");
                                var overrideTokens = _options.Features.EnableDocumentSearch
                                    ? TokenizeForSearch(normalizedForMatching)
                                    : Array.Empty<string>();
                                return new QueryIntentAnalysisResult(false, overrideTokens);
                            }
                            string? answer = null;
                            if (root.TryGetProperty("answer", out var answerElement) && answerElement.ValueKind == JsonValueKind.String)
                            {
                                answer = answerElement.GetString();
                                if (string.IsNullOrWhiteSpace(answer))
                                {
                                    answer = null;
                                }
                            }
                            
                            return new QueryIntentAnalysisResult(true, Array.Empty<string>(), answer);
                        }

                        if (string.Equals(typeValue, "INFORMATION", StringComparison.Ordinal))
                        {
                            string[] tokens = Array.Empty<string>();
                            if (root.TryGetProperty("tokens", out var tokensElement) && tokensElement.ValueKind == JsonValueKind.Array)
                            {
                                tokens = ExtractTokensFromJsonArray(tokensElement);
                        
                                if (tokens.Length < MinTokenCountForInformation)
                                {
                                    _logger.LogWarning("AI returned only {Count} tokens, expected at least {MinCount}. Response may be incomplete.", tokens.Length, MinTokenCountForInformation);
                                }
                            }

                            if (tokens.Length == 0 && _options.Features.EnableDocumentSearch)
                            {
                                tokens = TokenizeForSearch(normalizedForMatching);
                            }

                            return new QueryIntentAnalysisResult(false, tokens);
                        }

                        _logger.LogWarning("AI returned JSON with unknown type value: {Type}", typeValue);
                        return FallbackFromPlainClassification(classification, normalizedForMatching);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse AI response as JSON. Response: {Response}", classification?.Substring(0, Math.Min(200, classification?.Length ?? 0)));
                
                        var cleanedJson = TryExtractJsonFromResponse(classification);
                        if (!string.IsNullOrWhiteSpace(cleanedJson))
                        {
                            try
                            {
                                using var document = JsonDocument.Parse(cleanedJson);
                                var root = document.RootElement;
                        
                                if (root.ValueKind == JsonValueKind.Object)
                                {
                                    if (root.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String)
                                    {
                                        var typeValue = typeElement.GetString()?.Trim().ToUpperInvariant();
                                        if (string.Equals(typeValue, "CONVERSATION", StringComparison.Ordinal))
                                        {
                                            if (HasQuestionPunctuation(trimmedQuery) && trimmedQuery.Length > 40 &&
                                                Tokenize(trimmedQuery).Length >= 6)
                                            {
                                                var overrideTokens = _options.Features.EnableDocumentSearch
                                                    ? TokenizeForSearch(normalizedForMatching)
                                                    : Array.Empty<string>();
                                                return new QueryIntentAnalysisResult(false, overrideTokens);
                                            }
                                            string? answer = null;
                                            if (root.TryGetProperty("answer", out var answerElement) && answerElement.ValueKind == JsonValueKind.String)
                                            {
                                                answer = answerElement.GetString();
                                                if (string.IsNullOrWhiteSpace(answer))
                                                {
                                                    answer = null;
                                                }
                                            }
                                            
                                            return new QueryIntentAnalysisResult(true, Array.Empty<string>(), answer);
                                        }
                                
                                        if (string.Equals(typeValue, "INFORMATION", StringComparison.Ordinal))
                                        {
                                            string[] tokens = Array.Empty<string>();
                                            if (root.TryGetProperty("tokens", out var tokensElement) && tokensElement.ValueKind == JsonValueKind.Array)
                                            {
                                                tokens = ExtractTokensFromJsonArray(tokensElement);
                                            }
                                    
                                            if (tokens.Length == 0 && _options.Features.EnableDocumentSearch)
                                            {
                                                tokens = TokenizeForSearch(normalizedForMatching);
                                            }
                                    
                                            return new QueryIntentAnalysisResult(false, tokens);
                                        }
                                    }
                                }
                            }
                            catch (JsonException)
                            {
                                _logger.LogWarning("Failed to parse cleaned JSON, falling back to plain classification");
                            }
                        }
                
                        return FallbackFromPlainClassification(classification, normalizedForMatching);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI classification failed; defaulting to conversation.");
                    return new QueryIntentAnalysisResult(true, Array.Empty<string>());
                }
        }
    }

    /// <summary>
    /// Parses command from user input and extracts payload if available
    /// </summary>
    public bool TryParseCommand(string input, out QueryCommandType commandType, out string payload)
    {
        commandType = QueryCommandType.None;
        payload = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();
        var lowerTrimmed = trimmed.ToLowerInvariant();

        if (lowerTrimmed == "/new" || 
            lowerTrimmed == "/reset" || 
            lowerTrimmed == "/clear" ||
            lowerTrimmed.StartsWith("/new ") || 
            lowerTrimmed.StartsWith("/reset ") || 
            lowerTrimmed.StartsWith("/clear "))
        {
            commandType = QueryCommandType.NewConversation;
            if (lowerTrimmed.StartsWith("/new "))
                payload = trimmed[5..].TrimStart();
            else if (lowerTrimmed.StartsWith("/reset ") || lowerTrimmed.StartsWith("/clear "))
                payload = trimmed[7..].TrimStart();
            return true;
        }

        if (!trimmed.StartsWith("/chat", StringComparison.OrdinalIgnoreCase) && 
            !trimmed.StartsWith("/talk", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("/conversation", StringComparison.OrdinalIgnoreCase)) 
            return false;
        
        commandType = QueryCommandType.ForceConversation;
        payload = trimmed.Length > 5 ? trimmed[5..].TrimStart() : string.Empty;
        
        return true;

    }

    private HeuristicDecision HeuristicClassify(string query, out int score)
    {
        score = 0;
        var trimmed = query.Trim();
        var tokens = Tokenize(trimmed);

        if (HasQuestionPunctuation(trimmed)) score++;
        if (HasUnicodeDigits(trimmed)) score++;
        if (HasMultipleNumericGroups(trimmed)) score++;
        if (tokens.Length >= 5) score++;
        if (HasOperatorsOrSymbols(trimmed)) score++;
        if (HasDateOrTimePattern(trimmed)) score++;
        if (HasNumericRangeOrList(trimmed)) score++;
        if (HasIdLikeToken(tokens)) score++;
        if (HasQuestionPunctuation(trimmed) && tokens.Length >= 6 && trimmed.Length > 40)
            score += 2;

        var hasNoTechnicalIndicators = !HasUnicodeDigits(trimmed) && !HasOperatorsOrSymbols(trimmed) &&
            !HasDateOrTimePattern(trimmed) && !HasIdLikeToken(tokens);
        if (trimmed.Length <= MaxShortConversationLength && tokens.Length <= 2 && hasNoTechnicalIndicators)
        {
            return HeuristicDecision.Conversation;
        }

        var convoScore = 0;
        if (trimmed.Length <= 2) convoScore++;
        if (tokens.Length <= 2 && !HasQuestionPunctuation(trimmed) && !HasUnicodeDigits(trimmed)) convoScore++;

        if (convoScore >= 1 && score == 0)
        {
            return HeuristicDecision.Conversation;
        }

        if (score >= 3)
        {
            return HeuristicDecision.Information;
        }

        return HeuristicDecision.Unknown;
    }

    private static string[] Tokenize(string input) =>
        input.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

    private static bool HasQuestionPunctuation(string input) =>
        input.IndexOf('?', StringComparison.Ordinal) >= 0 ||
        input.IndexOf('¿', StringComparison.Ordinal) >= 0 ||
        input.IndexOf('؟', StringComparison.Ordinal) >= 0;

    private static bool HasUnicodeDigits(string input) =>
        input.Any(c => char.GetUnicodeCategory(c) == UnicodeCategory.DecimalDigitNumber);

    private static bool HasMultipleNumericGroups(string input)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(input, @"\p{Nd}+");
        return matches.Count >= 2;
    }

    private static bool HasOperatorsOrSymbols(string input) =>
        input.IndexOf('>') >= 0 || input.IndexOf('<') >= 0 ||
        input.IndexOf('=') >= 0 || input.IndexOf('+') >= 0 ||
        input.IndexOf('-') >= 0 || input.IndexOf('*') >= 0 ||
        input.IndexOf('/') >= 0 || input.IndexOf('%') >= 0 ||
        input.IndexOf('€') >= 0 || input.IndexOf('$') >= 0 ||
        input.IndexOf('£') >= 0 || input.IndexOf('¥') >= 0 ||
        input.IndexOf('₺') >= 0;

    private static bool HasDateOrTimePattern(string input) =>
        System.Text.RegularExpressions.Regex.IsMatch(input, @"\b\d{4}[-/\.]\d{1,2}[-/\.]\d{1,2}\b") ||
        System.Text.RegularExpressions.Regex.IsMatch(input, @"\b\d{1,2}:\d{2}(:\d{2})?\b");

    private static bool HasNumericRangeOrList(string input) =>
        System.Text.RegularExpressions.Regex.IsMatch(input, @"\b\d+\s*[-–—]\s*\d+\b") ||
        System.Text.RegularExpressions.Regex.IsMatch(input, @"\b\d+\s*,\s*\d+(\s*,\s*\d+)+\b");

    private static bool HasIdLikeToken(string[] tokens) =>
        tokens.Any(t => System.Text.RegularExpressions.Regex.IsMatch(t, @"\p{L}*\p{Nd}+\w*"));

    private string[] TokenizeForSearch(string input)
    {
        var basicTokens = Tokenize(input);

        return basicTokens
            .Where(t => t.Length > MinTokenLength)
            .Select(t => t.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private string[] ExtractTokensFromJsonArray(JsonElement tokensElement)
    {
        var tokens = tokensElement
            .EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .Where(s => s.Length > MinTokenLength)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (tokens.Length < MinTokenCountForInformation)
        {
            _logger.LogWarning("AI returned only {Count} tokens, expected at least {MinCount}. Tokens: {Tokens}", 
                tokens.Length, MinTokenCountForInformation, string.Join(", ", tokens));
        }

        return tokens;
    }

    private static string? TryExtractJsonFromResponse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        var trimmed = response.Trim();
        
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase) || 
            trimmed.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            var codeBlockStart = trimmed.IndexOf("```", StringComparison.OrdinalIgnoreCase);
            if (codeBlockStart >= 0)
            {
                var afterStart = trimmed.Substring(codeBlockStart + 3);
                var codeBlockEnd = afterStart.IndexOf("```", StringComparison.OrdinalIgnoreCase);
                if (codeBlockEnd >= 0)
                {
                    trimmed = afterStart.Substring(0, codeBlockEnd).Trim();
                }
                else
                {
                    trimmed = afterStart.Trim();
                }
            }
        }
        
        if (trimmed.StartsWith("json", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(4).Trim();
        }
        
        var startIndex = trimmed.IndexOf('{');
        if (startIndex < 0)
            return null;

        var endIndex = trimmed.LastIndexOf('}');
        if (endIndex < startIndex)
            return null;

        return trimmed.Substring(startIndex, endIndex - startIndex + 1);
    }

    private QueryIntentAnalysisResult FallbackFromPlainClassification(string? classification, string normalizedQuery)
    {
        if (string.IsNullOrWhiteSpace(classification))
        {
            return new QueryIntentAnalysisResult(true, Array.Empty<string>());
        }

        var normalizedResult = classification.Trim().ToUpperInvariant();

        if (normalizedResult.Contains("CONVERSATION", StringComparison.Ordinal))
        {
            return new QueryIntentAnalysisResult(true, Array.Empty<string>());
        }

        if (normalizedResult.Contains("INFORMATION", StringComparison.Ordinal))
        {
            var tokens = _options.Features.EnableDocumentSearch
                ? TokenizeForSearch(normalizedQuery)
                : Array.Empty<string>();
            return new QueryIntentAnalysisResult(false, tokens);
        }

        return new QueryIntentAnalysisResult(true, Array.Empty<string>());
    }
}


