#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Database;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Parser;
using SmartRAG.Interfaces.Search;
using SmartRAG.Interfaces.Storage;
using SmartRAG.Interfaces.Storage.Qdrant;
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
        /// Determines whether the query should be treated as general conversation using AI intent detection
        /// </summary>
        public async Task<bool> IsGeneralConversationAsync(string query, string? conversationHistory = null)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            var trimmedQuery = string.IsNullOrWhiteSpace(query) ? string.Empty : query.Trim();

            // Fast pre-check: Obvious information queries (skip AI call)
            if (IsLikelyInformationQuery(trimmedQuery))
            {
                return false;
            }

            // Fast pre-check: Short simple queries (likely conversation)
            if (IsObviousConversation(trimmedQuery))
            {
                _logger.LogDebug("Query classified as CONVERSATION by fast pre-check: {Query}", _textNormalizationService.SanitizeForLog(trimmedQuery));
                return true;
            }

            // AI classification for ambiguous cases
            _logger.LogDebug("Query passed pre-checks, sending to AI for classification: {Query}", _textNormalizationService.SanitizeForLog(trimmedQuery));
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
- Greeting (any language): Hello, Hi, Hey, Hola, Bonjour, Merhaba, Selam
- About the AI: Who are you, What are you, What model, What can you do
- Small talk: How are you, What's your name, Where are you from, Are you ok
- Polite chat: Thank you, Thanks, Goodbye, See you, Nice to meet you

✓ Classify as INFORMATION ONLY if:
- Contains data request words: show, list, find, calculate, total, count, sum
- Contains question words: what IS/WAS, which one, how many, when did
- Contains numbers/dates: 2023, last year, top 10, over 1000
- Specific entity queries: record 123, item X, reference number Y

User: ""{0}""
{1}

CRITICAL: If unsure, default to CONVERSATION. Answer with ONE word only: CONVERSATION or INFORMATION", 
                    trimmedQuery, 
                    string.IsNullOrWhiteSpace(historySnippet) ? "" : $"Context: \"{historySnippet}\"");

                var classification = await _aiService.GenerateResponseAsync(classificationPrompt, Array.Empty<string>());
                _logger.LogDebug("AI classification result: {Classification}", classification);

                if (!string.IsNullOrWhiteSpace(classification))
                {
                    var normalizedResult = classification.Trim().ToUpperInvariant();

                    if (normalizedResult.Contains("CONVERSATION", StringComparison.Ordinal))
                    {
                        _logger.LogDebug("Query classified as CONVERSATION by AI: {Query}", _textNormalizationService.SanitizeForLog(trimmedQuery));
                        return true;
                    }

                    if (normalizedResult.Contains("INFORMATION", StringComparison.Ordinal))
                    {
                        _logger.LogDebug("Query classified as INFORMATION by AI: {Query}", _textNormalizationService.SanitizeForLog(trimmedQuery));
                        return false;
                    }

                    _logger.LogWarning("AI returned unclear classification: {Classification}", classification);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "AI classification failed. Defaulting to conversation.");
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

        /// <summary>
        /// Fast heuristic check for obvious conversation patterns (no AI call needed)
        /// </summary>
        private static bool IsObviousConversation(string query)
        {
            var trimmed = query.Trim();

            // Very short input (1-2 chars) is likely conversation
            if (trimmed.Length <= 2)
            {
                return true;
            }

            var tokens = trimmed.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Short queries (1-2 words) without question mark or numbers are likely chat
            if (tokens.Length <= 2 &&
                !trimmed.Contains('?', StringComparison.Ordinal) &&
                !trimmed.Any(char.IsDigit))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Fast heuristic check for obvious information queries (no AI call needed)
        /// </summary>
        private static bool IsLikelyInformationQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return false;
            }

            var trimmed = query.Trim();

            // Contains question mark → likely information query
            if (trimmed.Contains('?', StringComparison.Ordinal))
            {
                return true;
            }

            // Contains numbers → likely data query (e.g., "record 123", "top 10")
            if (trimmed.Any(char.IsDigit))
            {
                return true;
            }

            // Long queries (5+ words) are usually information requests
            var tokens = trimmed.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 5)
            {
                return true;
            }

            return false;
        }
    }
}

