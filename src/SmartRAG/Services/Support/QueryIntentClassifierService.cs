#nullable enable
using Microsoft.Extensions.Logging;
using SmartRAG.Enums;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Support;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services.Support
{
    /// <summary>
    /// Service for classifying query intent (conversation vs information)
    /// </summary>
    public class QueryIntentClassifierService : IQueryIntentClassifierService
    {
        private readonly IAIService _aiService;
        private readonly ILogger<QueryIntentClassifierService> _logger;
        private readonly ITextNormalizationService _textNormalizationService;

        /// <summary>
        /// Initializes a new instance of the QueryIntentClassifierService
        /// </summary>
        /// <param name="aiService">AI service for text generation</param>
        /// <param name="logger">Logger instance for this service</param>
        /// <param name="textNormalizationService">Service for text normalization</param>
        public QueryIntentClassifierService(
            IAIService aiService,
            ILogger<QueryIntentClassifierService> logger,
            ITextNormalizationService textNormalizationService)
        {
            _aiService = aiService;
            _logger = logger;
            _textNormalizationService = textNormalizationService;
        }

        /// <summary>
        /// [AI Query] Determines whether the query should be treated as general conversation using AI intent detection
        /// </summary>
        public async Task<bool> IsGeneralConversationAsync(string query, string? conversationHistory = null)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            var trimmedQuery = string.IsNullOrWhiteSpace(query) ? string.Empty : query.Trim();

            var heuristic = HeuristicClassify(trimmedQuery, out var heuristicScore);

            if (heuristic == HeuristicDecision.Conversation)
            {
                _logger.LogDebug("Query classified as CONVERSATION by heuristics (score={Score})", heuristicScore);
                return true;
            }
            
            if (heuristic == HeuristicDecision.Information && heuristicScore >= 4)
            {
                _logger.LogDebug("Query classified as INFORMATION by heuristics (strong indicators, score={Score})", heuristicScore);
                return false;
            }

            try
            {
                var historySnippet = string.Empty;
                if (!string.IsNullOrWhiteSpace(conversationHistory))
                {
                    const int maxHistoryLength = 400;
                    var normalizedHistory = conversationHistory.Trim();
                    historySnippet = normalizedHistory.Length > maxHistoryLength
                        ? normalizedHistory.Substring(normalizedHistory.Length - maxHistoryLength, maxHistoryLength)
                        : normalizedHistory;
                }

                var classificationPrompt = string.Format(
                    CultureInfo.InvariantCulture,
@"You MUST classify the input as CONVERSATION or INFORMATION.

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

{1}

User: ""{0}""

CRITICAL CONTEXT RULES:
- If conversation history shows previous questions about the AI AND current question is still about the AI, classify as CONVERSATION
- If conversation history shows AI questions BUT current question is about external topic, classify as INFORMATION
- Topic change detection: If user switches from AI questions to questions about external entities, it is INFORMATION
- If the user asks about a specific external topic, concept, or entity (not the AI), it is INFORMATION
- If unsure and no conversation history about AI, default to INFORMATION
- If unsure but conversation history shows AI-related questions AND current question seems about AI, default to CONVERSATION
- If unsure but current question contains external entity references, default to INFORMATION

Answer with ONE word only: CONVERSATION or INFORMATION",
                    trimmedQuery,
                    string.IsNullOrWhiteSpace(historySnippet) ? "" : $"Conversation History:\n\"{historySnippet}\"\n\n");

                var classification = await _aiService.GenerateResponseAsync(classificationPrompt, Array.Empty<string>());

                if (!string.IsNullOrWhiteSpace(classification))
                {
                    var normalizedResult = classification.Trim().ToUpperInvariant();

                    if (normalizedResult.Contains("CONVERSATION", StringComparison.Ordinal))
                    {
                        _logger.LogDebug("AI classified as CONVERSATION");
                        return true;
                    }

                    if (normalizedResult.Contains("INFORMATION", StringComparison.Ordinal))
                    {
                        _logger.LogDebug("AI classified as INFORMATION");
                        return false;
                    }

                    _logger.LogWarning("AI returned unclear classification: {Classification}", classification);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI classification failed; defaulting to conversation.");
                return true; // Safe default: treat as conversation
            }

            return true;
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

            if (lowerTrimmed == "/new" || lowerTrimmed == "/reset" || lowerTrimmed == "/clear" ||
                lowerTrimmed.StartsWith("/new ") || lowerTrimmed.StartsWith("/reset ") || lowerTrimmed.StartsWith("/clear "))
            {
                commandType = QueryCommandType.NewConversation;
                if (lowerTrimmed.StartsWith("/new "))
                    payload = trimmed[5..].TrimStart();
                else if (lowerTrimmed.StartsWith("/reset "))
                    payload = trimmed[7..].TrimStart();
                else if (lowerTrimmed.StartsWith("/clear "))
                    payload = trimmed[7..].TrimStart();
                return true;
            }

            if (trimmed.StartsWith("/chat", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("/talk", StringComparison.OrdinalIgnoreCase))
            {
                commandType = QueryCommandType.ForceConversation;
                payload = trimmed.Length > 5 ? trimmed[5..].TrimStart() : string.Empty;
                return true;
            }

            return false;
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
    }
}

