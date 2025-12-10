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

            if (response.Sources?.Any(s => 
                s.SourceType?.Equals("System", StringComparison.OrdinalIgnoreCase) == true &&
                (s.RelevantContent?.Contains("Error", StringComparison.OrdinalIgnoreCase) == true ||
                 s.FileName?.Contains("Error", StringComparison.OrdinalIgnoreCase) == true)) == true)
            {
                _logger?.LogDebug("Response contains system error notification, no meaningful data");
                return false;
            }

            var databaseSources = response.Sources?.Where(s => 
                s.SourceType?.Equals("Database", StringComparison.OrdinalIgnoreCase) == true).ToList();
            
            if (databaseSources != null && databaseSources.Any())
            {
                var totalRows = 0;
                var hasMeaningfulData = false;
                
                foreach (var source in databaseSources)
                {
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

            if (!string.IsNullOrWhiteSpace(response.Answer) && !IndicatesMissingData(response.Answer))
            {
                return true;
            }

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
            
            if (normalized.Length < 15)
            {
                return true;
            }
            
            if (normalized.EndsWith("?"))
            {
                return true;
            }
            
            var answerWords = normalized.Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
            if (answerWords.Length < 5)
            {
                return true;
            }
            
            var universalNegativePatterns = new[]
            {
                " no ",
                "404",
                "0 row",
                "empty",
                "null",
                "not provided",
                "not available",
                "not found",
                "cannot answer",
                "cannot determine",
                "could not find",
                "no information",
                "information not",
                "data not"
            };
            
            if (universalNegativePatterns.Any(lowerAnswer.Contains))
            {
                return true;
            }
            
            if (normalized.Length > 100)
            {
                var hasNumbers = normalized.Any(char.IsDigit);
                var endsWithQuestion = normalized.EndsWith("?");
                
                if (!hasNumbers && endsWithQuestion)
                {
                    return true;
                }
            }
            
            if (normalized.Length > 60)
            {
                var hasNumbers = normalized.Any(char.IsDigit);
                
                if (!hasNumbers)
                {
                    var hasMultipleSentences = normalized.Contains(".") && normalized.Split('.').Length > 2;
                    var isLongExplanation = normalized.Length > 150;
                    var hasNegativeParticle = lowerAnswer.Contains(" not ") || 
                                            lowerAnswer.Contains(" no ") ||
                                            lowerAnswer.Contains(" cannot ") ||
                                            lowerAnswer.Contains(" unable ") ||
                                            lowerAnswer.Contains("does not") ||
                                            lowerAnswer.Contains("do not");
                    var hasMissingDataVerb = lowerAnswer.Contains("specif") ||
                                           lowerAnswer.Contains("mention") ||
                                           lowerAnswer.Contains("provid") ||
                                           lowerAnswer.Contains("availab") ||
                                           lowerAnswer.Contains("find") ||
                                           lowerAnswer.Contains("exist") ||
                                           lowerAnswer.Contains("present") ||
                                           lowerAnswer.Contains("includ");
                    
                    if ((hasMultipleSentences || isLongExplanation) && 
                        (hasNegativeParticle || hasMissingDataVerb))
                    {
                        return true;
                    }
                }
            }
            
            var notSpecifiedPatterns = new[]
            {
                "not specified",
                "not mentioned",
                "not stated",
                "not indicated",
                "not provided",
                "not available",
                "not found",
                "not clear",
                "not explicit",
                "not explicitly",
                "not directly",
                "does not specify",
                "does not mention",
                "does not state",
                "does not indicate",
                "no information",
                "no data",
                "no details",
                "no specific",
                "unclear",
                "unavailable",
                "unspecified"
            };
            
            if (notSpecifiedPatterns.Any(pattern => lowerAnswer.Contains(pattern)))
            {
                return true;
            }
            
            if (normalized.Contains("❌") || normalized.Contains("⚠") || normalized.Contains("🚫"))
            {
                return true;
            }
            
            if (!string.IsNullOrWhiteSpace(query) && normalized.Length > 50 && normalized.Length < 200)
            {
                var queryWords = query.ToLowerInvariant()
                    .Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 3)
                    .ToList();
                
                if (queryWords.Count > 0)
                {
                    var matchedKeywords = queryWords.Count(keyword => lowerAnswer.Contains(keyword));
                    var keywordRatio = (double)matchedKeywords / queryWords.Count;
                    
                    var hasNumbers = normalized.Any(char.IsDigit);
                    var answerWordCount = normalized.Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    var queryWordCount = query.Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    
                    if (keywordRatio > 0.8 && !hasNumbers && answerWordCount < (queryWordCount * 1.5))
                    {
                        return true;
                    }
                }
            }
            
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
                mergedAnswer = !string.IsNullOrEmpty(databaseContext) && !string.IsNullOrEmpty(documentContext)
                    ? $"{databaseContext}\n\n{documentContext}"
                    : databaseContext ?? documentContext ?? "No data available";
            }

            return CreateRagResponse(query, mergedAnswer, combinedSources);
        }
    }
}

