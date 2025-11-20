#nullable enable
using Microsoft.Extensions.Logging;
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

            // Unified heuristic classification (fast pre-check)
            var heuristic = HeuristicClassify(trimmedQuery, out var heuristicScore);

            if (heuristic == HeuristicDecision.Conversation)
            {
                _logger.LogDebug("Query classified as CONVERSATION by heuristics (score={Score}): {Query}", heuristicScore, _textNormalizationService.SanitizeForLog(trimmedQuery));
                return true;
            }
            if (heuristic == HeuristicDecision.Information)
            {
                _logger.LogDebug("Query classified as INFORMATION by heuristics (score={Score}): {Query}", heuristicScore, _textNormalizationService.SanitizeForLog(trimmedQuery));
                return false;
            }

            // AI classification for ambiguous cases
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
- About the AI (identity/capabilities, e.g., who/what are you, what can you do)
- Small talk (well-being, introductions, origin, casual chat)
- Polite chat (gratitude, farewells, niceties)

✓ Classify as INFORMATION ONLY if:
- Contains data-request intent (show, list, find, calculate, total, count, sum)
- Contains question words with informational intent (what, which, how many, when, how to)
- Contains numbers/dates indicating data queries (e.g., years, ranges, ""top N"", thresholds)
- Contains specific entity references (e.g., record identifiers, reference numbers)

User: ""{0}""
{1}

CRITICAL: If unsure, default to CONVERSATION. Answer with ONE word only: CONVERSATION or INFORMATION",
                    trimmedQuery,
                    string.IsNullOrWhiteSpace(historySnippet) ? "" : $"Context: \"{historySnippet}\"");

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

            // Final fallback: if AI gave unclear response, default to conversation
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

            // New conversation commands: /new, /reset, /clear
            if (lowerTrimmed == "/new" || lowerTrimmed == "/reset" || lowerTrimmed == "/clear" ||
                lowerTrimmed.StartsWith("/new ") || lowerTrimmed.StartsWith("/reset ") || lowerTrimmed.StartsWith("/clear "))
            {
                commandType = QueryCommandType.NewConversation;
                // Extract payload if command has parameters
                if (lowerTrimmed.StartsWith("/new "))
                    payload = trimmed[5..].TrimStart();
                else if (lowerTrimmed.StartsWith("/reset "))
                    payload = trimmed[7..].TrimStart();
                else if (lowerTrimmed.StartsWith("/clear "))
                    payload = trimmed[7..].TrimStart();
                return true;
            }

            // Force conversation commands: /chat, /talk
            if (trimmed.StartsWith("/chat", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("/talk", StringComparison.OrdinalIgnoreCase))
            {
                commandType = QueryCommandType.ForceConversation;
                payload = trimmed.Length > 5 ? trimmed[5..].TrimStart() : string.Empty;
                return true;
            }

            return false;
        }

        // Unified heuristic classifier with language-agnostic signals
        private enum HeuristicDecision { Unknown, Conversation, Information }

        private static HeuristicDecision HeuristicClassify(string query, out int score)
        {
            score = 0;
            var trimmed = query.Trim();
            var tokens = Tokenize(trimmed);

            // Information signals
            if (HasQuestionPunctuation(trimmed)) score++;
            if (HasUnicodeDigits(trimmed)) score++;
            if (HasMultipleNumericGroups(trimmed)) score++;
            if (tokens.Length >= 5) score++;
            if (HasOperatorsOrSymbols(trimmed)) score++;
            if (HasDateOrTimePattern(trimmed)) score++;
            if (HasNumericRangeOrList(trimmed)) score++;
            if (HasIdLikeToken(tokens)) score++;

            // Conversation signals (short, casual)
            var convoScore = 0;
            if (trimmed.Length <= 2) convoScore++;
            if (tokens.Length <= 2 && !HasQuestionPunctuation(trimmed) && !HasUnicodeDigits(trimmed)) convoScore++;

            // Decisions
            if (convoScore >= 1 && score == 0)
            {
                return HeuristicDecision.Conversation;
            }

            // Require at least 2 information signals
            if (score >= 2)
            {
                return HeuristicDecision.Information;
            }

            return HeuristicDecision.Unknown;
        }

        // Helper detectors (language-agnostic)
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

