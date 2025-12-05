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
        #region Fields

        private readonly SmartRagOptions _options;
        private readonly IConfiguration _configuration;
        private readonly IConversationManagerService? _conversationManager;
        private readonly ILogger<ResponseBuilderService>? _logger;
        private readonly IAIService? _aiService;
        private readonly IPromptBuilderService? _promptBuilder;

        #endregion

        #region Constructor

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

        #endregion

        #region Public Methods

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
        /// </summary>
        public bool HasMeaningfulData(RagResponse? response)
        {
            if (response == null)
            {
                return false;
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
        /// Checks if an answer indicates missing data using language-agnostic patterns
        /// </summary>
        public bool IndicatesMissingData(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                return true;
            }

            var normalized = answer.ToLowerInvariant();
            var indicators = new[]
            {
                "unable to find",
                "cannot find",
                "no data",
                "no information",
                "not available",
                "not found",
                "sorry"
            };

            return indicators.Any(normalized.Contains);
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

        #endregion
    }
}

