#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Support;
using SmartRAG.Models;
using SmartRAG.Services.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services.Document
{
    /// <summary>
    /// Service for building RAG responses
    /// </summary>
    public class ResponseBuilderService : IResponseBuilderService
    {
        private readonly SmartRagOptions _options;
        private readonly IConfiguration _configuration;
        private readonly IConversationManagerService? _conversationManager;
        private readonly ILogger<ResponseBuilderService>? _logger;
        private readonly IAIService? _aiService;
        private readonly IPromptBuilderService? _promptBuilder;

        /// <summary>
        /// Initializes a new instance of the ResponseBuilderService
        /// </summary>
        /// <param name="options">SmartRAG configuration options</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="conversationManager">Optional service for managing conversations</param>
        /// <param name="logger">Optional logger instance</param>
        /// <param name="aiService">Optional AI service for merging responses</param>
        /// <param name="promptBuilder">Optional prompt builder service for merging responses</param>
        public ResponseBuilderService(
            IOptions<SmartRagOptions> options,
            IConfiguration configuration,
            IConversationManagerService? conversationManager = null,
            ILogger<ResponseBuilderService>? logger = null,
            IAIService? aiService = null,
            IPromptBuilderService? promptBuilder = null)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _conversationManager = conversationManager;
            _logger = logger;
            _aiService = aiService;
            _promptBuilder = promptBuilder;
        }

        /// <summary>
        /// Creates a RagResponse with standard configuration
        /// </summary>
        public RagResponse CreateRagResponse(string query, string answer, List<SearchSource> sources)
        {
            return new RagResponse
            {
                Query = query,
                Answer = answer,
                Sources = sources,
                SearchedAt = DateTime.UtcNow,
                Configuration = GetRagConfiguration()
            };
        }

        /// <summary>
        /// Gets RAG configuration from options and configuration
        /// </summary>
        public RagConfiguration GetRagConfiguration()
        {
            return new RagConfiguration
            {
                AIProvider = _options.AIProvider.ToString(),
                StorageProvider = _options.StorageProvider.ToString(),
                Model = _configuration["AI:OpenAI:Model"]
            };
        }

        /// <summary>
        /// Determines if a RAG response contains meaningful data
        /// Uses multiple checks: answer content, database results, source data, and error detection
        /// </summary>
        public bool HasMeaningfulData(RagResponse? response)
        {
            if (response == null)
            {
                return false;
            }

            // Check 0: System error sources indicate failure (not meaningful)
            // If response contains system error notification, query failed
            if (response.Sources?.Any(s => 
                s.SourceType?.Equals("System", StringComparison.OrdinalIgnoreCase) == true &&
                (s.RelevantContent?.Contains("Error", StringComparison.OrdinalIgnoreCase) == true ||
                 s.FileName?.Contains("Error", StringComparison.OrdinalIgnoreCase) == true)) == true)
            {
                _logger?.LogDebug("Response contains system error notification, no meaningful data");
                return false;
            }

            // Check 1: Database sources with actual data
            // If database returned 0 rows, it's not meaningful regardless of AI answer
            var databaseSources = response.Sources?.Where(s => 
                s.SourceType?.Equals("Database", StringComparison.OrdinalIgnoreCase) == true).ToList();
            
            if (databaseSources != null && databaseSources.Any())
            {
                // CRITICAL: Database query with rows > 0 is ALWAYS meaningful
                // Even if AI fails to generate perfect answer, database data exists
                var totalRows = 0;
                var hasMeaningfulData = false;
                
                foreach (var source in databaseSources)
                {
                    // Extract row count from FileName: "DatabaseName (123 rows)"
                    if (!string.IsNullOrWhiteSpace(source.FileName) && source.FileName.Contains("(") && source.FileName.Contains(" rows)"))
                    {
                        var startIndex = source.FileName.IndexOf('(') + 1;
                        var endIndex = source.FileName.IndexOf(" rows)");
                        if (startIndex > 0 && endIndex > startIndex)
                        {
                            var rowCountStr = source.FileName.Substring(startIndex, endIndex - startIndex).Trim();
                            if (int.TryParse(rowCountStr, out var rows))
                            {
                                totalRows += rows;
                                if (rows > 0)
                                {
                                    hasMeaningfulData = true;
                                }
                            }
                        }
                    }
                    
                    // Fallback: Check if RelevantContent has substantial data
                    if (!string.IsNullOrWhiteSpace(source.RelevantContent) && source.RelevantContent.Length >= 50)
                    {
                        hasMeaningfulData = true;
                    }
                }
                
                if (hasMeaningfulData && totalRows > 0)
                {
                    _logger?.LogInformation("Database query returned {TotalRows} rows - marking as meaningful data regardless of AI answer quality", totalRows);
                    return true;
                }
                
                if (!hasMeaningfulData)
                {
                    _logger?.LogDebug("Database sources exist but all returned 0 rows, no meaningful data");
                    return false;
                }
            }

            // Check 2: Answer text analysis (universal heuristics)
            if (!string.IsNullOrWhiteSpace(response.Answer) && !IndicatesMissingData(response.Answer))
            {
                return true;
            }

            // Check 3: Document/image sources with content
            if (response.Sources != null && response.Sources.Any(source =>
                source.DocumentId != Guid.Empty && !string.IsNullOrWhiteSpace(source.RelevantContent)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if an answer indicates missing data using universal language-agnostic heuristics
        /// Works for all languages without hardcoded patterns
        /// </summary>
        /// <param name="answer">The AI-generated answer to check</param>
        /// <param name="query">Optional: The original query to check for keyword repetition</param>
        public bool IndicatesMissingData(string answer, string? query = null)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                return true;
            }

            var normalized = answer.Trim();
            var lowerAnswer = normalized.ToLowerInvariant();
            
            // Universal Heuristic 1: Very short answers are usually negative responses
            // "No", "Hayır", "Nein", "Нет", etc. are all short
            if (normalized.Length < 15)
            {
                return true;
            }
            
            // Universal Heuristic 2: Answer is a question (AI doesn't know, asking back)
            // Works for all languages that use "?" punctuation
            if (normalized.EndsWith("?"))
            {
                return true;
            }
            
            // Universal Heuristic 3: Answer repeats the question (AI has no data)
            // When AI doesn't know, it often restates the question
            var answerWords = normalized.Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
            if (answerWords.Length < 5)
            {
                // Very few unique words = likely "I don't know" response
                return true;
            }
            
            // Universal Heuristic 4: Common ASCII negative patterns (language-independent)
            // These work across all Latin-script languages
            var universalNegativePatterns = new[]
            {
                " no ",           // Space-bounded "no" appears in many languages (English, Spanish, etc.)
                "404",            // HTTP status code (universal)
                "0 row",          // Database empty result (universal technical term)
                "empty",          // Technical term used across languages
                "null",           // Programming term (universal)
                "not provided",   // Universal: information/data not provided
                "not available",  // Universal: information/data not available
                "not found",      // Universal: information/data not found
                "cannot answer",  // Universal: AI cannot answer
                "cannot determine", // Universal: AI cannot determine
                "could not find", // Universal: AI could not find information
                "no information", // Universal: no information available
                "information not", // Universal: information not available/found/provided
                "data not"        // Universal: data not available/found/provided
            };
            
            if (universalNegativePatterns.Any(lowerAnswer.Contains))
            {
                return true;
            }
            
            // Universal Heuristic 4b: Check if answer length suggests it's a negative response
            // Very long answers that don't directly answer the question often indicate missing data
            // Pattern: Answer is long (>100 chars) but doesn't contain numbers or specific details
            if (normalized.Length > 100)
            {
                // Check if answer contains numbers (suggests specific data)
                var hasNumbers = normalized.Any(char.IsDigit);
                // Check if answer ends with question mark (suggests AI is asking back)
                var endsWithQuestion = normalized.EndsWith("?");
                
                // If answer is long but has no numbers and ends with question, likely negative
                if (!hasNumbers && endsWithQuestion)
                {
                    return true;
                }
            }
            
            // Universal Heuristic 5: Structure-based detection of negative responses
            // Language-agnostic detection: long answer + no numbers + negative/uncertainty patterns = missing data
            if (normalized.Length > 60)
            {
                var hasNumbers = normalized.Any(char.IsDigit); // Has specific data (numbers)?
                
                // If answer is long and has NO numbers, check for negative/uncertainty patterns
                // This works across ALL languages without relying on specific words
                if (!hasNumbers)
                {
                    // Pattern 1: Multiple sentences (explanatory structure suggests "I don't know")
                    // Works universally: "." is used in most languages
                    var hasMultipleSentences = normalized.Contains(".") && normalized.Split('.').Length > 2;
                    
                    // Pattern 2: Answer is very long (>150 chars) without numbers
                    // Long explanations without specific data usually indicate missing information
                    var isLongExplanation = normalized.Length > 150;
                    
                    // Pattern 3: Generic negative particles (work across many languages)
                    // These are common in English, Spanish, Portuguese, etc.
                    var hasNegativeParticle = lowerAnswer.Contains(" not ") || 
                                            lowerAnswer.Contains(" no ") ||
                                            lowerAnswer.Contains(" cannot ") ||
                                            lowerAnswer.Contains(" unable ") ||
                                            lowerAnswer.Contains("does not") ||
                                            lowerAnswer.Contains("do not");
                    
                    // Pattern 4: Verb roots that suggest missing/unavailable data
                    // These roots appear in many European languages with similar meanings
                    var hasMissingDataVerb = lowerAnswer.Contains("specif") ||   // specify, specified, especificar, etc.
                                           lowerAnswer.Contains("mention") ||     // mention, mentioned, mencionar, etc.
                                           lowerAnswer.Contains("provid") ||      // provide, provided, proveer, etc.
                                           lowerAnswer.Contains("availab") ||     // available, availability, disponible, etc.
                                           lowerAnswer.Contains("find") ||        // find, found, encontrar, etc.
                                           lowerAnswer.Contains("exist") ||       // exist, exists, existir, etc.
                                           lowerAnswer.Contains("present") ||     // present, presente, etc.
                                           lowerAnswer.Contains("includ");        // include, included, incluir, etc.
                    
                    // Trigger missing data detection if:
                    // - Multiple sentences OR long explanation
                    // - AND (negative particle OR missing-data verb)
                    if ((hasMultipleSentences || isLongExplanation) && 
                        (hasNegativeParticle || hasMissingDataVerb))
                    {
                        return true;
                    }
                }
            }
            
            // Universal Heuristic 5b: "Not specified/mentioned/indicated" pattern detection
            // When AI says information is "not specified" or "not mentioned", it indicates missing data
            // This works across languages that use similar structures (English, Spanish, etc.)
            // Pattern: "not" + verb (specified, mentioned, stated, indicated, provided, available)
            var notSpecifiedPatterns = new[]
            {
                "not specified",      // English: "not specified"
                "not mentioned",       // English: "not mentioned"
                "not stated",          // English: "not stated"
                "not indicated",       // English: "not indicated"
                "not provided",        // English: "not provided"
                "not available",       // English: "not available"
                "not found",           // English: "not found"
                "not clear",           // English: "not clear"
                "not explicit",        // English: "not explicit"
                "not explicitly",      // English: "not explicitly"
                "not directly",         // English: "not directly"
                "does not specify",    // English: "does not specify"
                "does not mention",    // English: "does not mention"
                "does not state",      // English: "does not state"
                "does not indicate",   // English: "does not indicate"
                "no information",      // English: "no information"
                "no data",             // English: "no data"
                "no details",          // English: "no details"
                "no specific",         // English: "no specific"
                "unclear",             // English: "unclear"
                "unavailable",         // English: "unavailable"
                "unspecified"          // English: "unspecified"
            };
            
            if (notSpecifiedPatterns.Any(pattern => lowerAnswer.Contains(pattern)))
            {
                return true;
            }
            
            // Universal Heuristic 6: Emoji indicators (works globally)
            // ❌, ⚠️, 🚫 etc. indicate negative/missing data
            if (normalized.Contains("❌") || normalized.Contains("⚠") || normalized.Contains("🚫"))
            {
                return true;
            }
            
            // Universal Heuristic 7: Check if AI mostly just repeats the query without answering
            // When AI doesn't know the answer, it often restates the question
            // CONSERVATIVE: Only trigger if >80% keywords matched + no new information
            if (!string.IsNullOrWhiteSpace(query) && normalized.Length > 50 && normalized.Length < 200)
            {
                var queryWords = query.ToLowerInvariant()
                    .Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 3) // Ignore short words (the, and, etc.)
                    .ToList();
                
                if (queryWords.Count > 0)
                {
                    var matchedKeywords = queryWords.Count(keyword => lowerAnswer.Contains(keyword));
                    var keywordRatio = (double)matchedKeywords / queryWords.Count;
                    
                    var hasNumbers = normalized.Any(char.IsDigit);
                    var answerWordCount = normalized.Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    var queryWordCount = query.Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    
                    // CONSERVATIVE: Require 80% keyword match + answer not much longer than query
                    // This catches "AI just repeating question" but not "AI answered using query keywords"
                    if (keywordRatio > 0.8 && !hasNumbers && answerWordCount < (queryWordCount * 1.5))
                    {
                        return true;
                    }
                }
            }
            
            // If none of the universal heuristics match, assume answer is meaningful
            return false;
        }

        /// <summary>
        /// Creates a fallback response when document query cannot answer the question
        /// </summary>
        public async Task<RagResponse> CreateFallbackResponseAsync(string query, string conversationHistory, string? preferredLanguage = null)
        {
            if (_conversationManager == null)
            {
                return CreateRagResponse(query, "Sorry, I cannot chat right now. Please try again later.", new List<SearchSource>());
            }

            if (_logger != null)
            {
                ServiceLogMessages.LogGeneralConversationQuery(_logger, null);
            }

            var chatResponse = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory, preferredLanguage);
            return CreateRagResponse(query, chatResponse, new List<SearchSource>());
        }

        /// <summary>
        /// Merges results from database and document queries into a unified response
        /// </summary>
        public async Task<RagResponse> MergeHybridResultsAsync(string query, RagResponse databaseResponse, RagResponse documentResponse, string conversationHistory, string? preferredLanguage = null)
        {
            var combinedSources = new List<SearchSource>();
            combinedSources.AddRange(databaseResponse.Sources);
            combinedSources.AddRange(documentResponse.Sources);

            var databaseContext = !string.IsNullOrEmpty(databaseResponse.Answer)
                ? databaseResponse.Answer
                : null;
            
            // If database Answer is empty but we have database sources, build context from sources
            if (string.IsNullOrEmpty(databaseContext) && databaseResponse.Sources != null && databaseResponse.Sources.Count > 0)
            {
                var databaseSourcesContext = string.Join("\n\n", databaseResponse.Sources
                    .Where(s => !string.IsNullOrEmpty(s.RelevantContent))
                    .Select(s => $"Database: {s.FileName}\n{s.RelevantContent}"));
                
                if (!string.IsNullOrEmpty(databaseSourcesContext))
                {
                    databaseContext = databaseSourcesContext;
                }
            }
            
            var documentContext = !string.IsNullOrEmpty(documentResponse.Answer)
                ? documentResponse.Answer
                : null;

            var combinedContext = new List<string>();
            if (!string.IsNullOrEmpty(databaseContext))
                combinedContext.Add(databaseContext);
            if (!string.IsNullOrEmpty(documentContext))
                combinedContext.Add(documentContext);

            string mergedAnswer;
            if (_aiService != null && _promptBuilder != null)
            {
                var mergePrompt = _promptBuilder.BuildHybridMergePrompt(query, databaseContext, documentContext, conversationHistory, preferredLanguage);
                mergedAnswer = await _aiService.GenerateResponseAsync(mergePrompt, combinedContext);
            }
            else
            {
                // Fallback: Simple merge without AI processing
                mergedAnswer = !string.IsNullOrEmpty(databaseContext) && !string.IsNullOrEmpty(documentContext)
                    ? $"{databaseContext}\n\n{documentContext}"
                    : databaseContext ?? documentContext ?? "No data available";
            }

            return CreateRagResponse(query, mergedAnswer, combinedSources);
        }
    }
}

