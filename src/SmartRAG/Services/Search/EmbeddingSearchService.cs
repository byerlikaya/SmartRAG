using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services.Search
{
    /// <summary>
    /// Service for embedding-based search operations
    /// </summary>
    public class EmbeddingSearchService : IEmbeddingSearchService
    {
        // Selection multipliers and minimums
        private const int CandidateMultiplier = 3;
        private const int CandidateMinCount = 30;
        private const int FinalTakeMultiplier = 2;
        private const int FinalMinCount = 20;

        // Query processing constants
        private const int EmptyEmbeddingCount = 0;
        private const int MinChunksWithEmbeddingsCount = 0;
        private const double RelevanceThreshold = 0.1;
        private const double HybridSemanticWeight = 0.8;
        private const double HybridKeywordWeight = 0.2;
        private const int MinVectorCount = 0;
        private const double DefaultScoreValue = 0.0;
        private const int DefaultScore = 0;

        private readonly IAIProviderFactory _aiProviderFactory;
        private readonly SmartRagOptions _options;
        private readonly IConfiguration _configuration;
        private readonly ISemanticSearchService _semanticSearchService;
        private readonly IDocumentScoringService _documentScoringService;
        private readonly ILogger<EmbeddingSearchService> _logger;

        /// <summary>
        /// Initializes a new instance of the EmbeddingSearchService
        /// </summary>
        /// <param name="aiProviderFactory">Factory for AI provider creation</param>
        /// <param name="options">SmartRAG configuration options</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="semanticSearchService">Service for semantic search operations</param>
        /// <param name="documentScoringService">Service for scoring document chunks</param>
        /// <param name="logger">Logger instance for this service</param>
        public EmbeddingSearchService(
            IAIProviderFactory aiProviderFactory,
            SmartRagOptions options,
            IConfiguration configuration,
            ISemanticSearchService semanticSearchService,
            IDocumentScoringService documentScoringService,
            ILogger<EmbeddingSearchService> logger)
        {
            _aiProviderFactory = aiProviderFactory;
            _options = options;
            _configuration = configuration;
            _semanticSearchService = semanticSearchService;
            _documentScoringService = documentScoringService;
            _logger = logger;
        }

        /// <summary>
        /// Performs embedding-based search on document chunks
        /// </summary>
        public async Task<List<DocumentChunk>> SearchByEmbeddingAsync(string query, List<DocumentChunk> allChunks, int maxResults)
        {
            try
            {
                var aiProvider = _aiProviderFactory.CreateProvider(_options.AIProvider);
                var providerKey = _options.AIProvider.ToString();
                var providerConfig = _configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

                if (providerConfig == null || string.IsNullOrEmpty(providerConfig.ApiKey))
                {
                    return new List<DocumentChunk>();
                }

                // Generate embedding for query
                var queryEmbedding = await aiProvider.GenerateEmbeddingAsync(query, providerConfig);
                if (queryEmbedding == null || queryEmbedding.Count == EmptyEmbeddingCount)
                {
                    return new List<DocumentChunk>();
                }

                // Calculate similarity for all chunks that have embeddings
                var chunksWithEmbeddings = allChunks.Where(c => c.Embedding != null && c.Embedding.Count > EmptyEmbeddingCount).ToList();

                if (chunksWithEmbeddings.Count == MinChunksWithEmbeddingsCount)
                {
                    return new List<DocumentChunk>();
                }

                // Enhanced semantic search with hybrid scoring
                var scoredChunks = await Task.WhenAll(chunksWithEmbeddings.Select(async chunk =>
                {
                    var semanticSimilarity = CalculateCosineSimilarity(queryEmbedding, chunk.Embedding);
                    var enhancedSemanticScore = await _semanticSearchService.CalculateEnhancedSemanticSimilarityAsync(query, chunk.Content);

                    // Hybrid scoring: Combine enhanced semantic similarity with keyword matching
                    var keywordScore = _documentScoringService.CalculateKeywordRelevanceScore(query, chunk.Content);
                    var hybridScore = (enhancedSemanticScore * HybridSemanticWeight) + (keywordScore * HybridKeywordWeight);

                    chunk.RelevanceScore = hybridScore;
                    return chunk;
                }));

                // Get top chunks based on hybrid scoring
                var relevantChunks = scoredChunks.ToList()
                    .Where(c => c.RelevanceScore > RelevanceThreshold)
                    .OrderByDescending(c => c.RelevanceScore)
                    .Take(Math.Max(maxResults * CandidateMultiplier, CandidateMinCount))
                    .ToList();

                return relevantChunks
                    .OrderByDescending(c => c.RelevanceScore)
                    .Take(Math.Max(maxResults * FinalTakeMultiplier, FinalMinCount))
                    .ToList();
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogEmbeddingSearchFailedError(_logger, ex);
                return new List<DocumentChunk>();
            }
        }

        /// <summary>
        /// Calculates cosine similarity between two vectors
        /// </summary>
        public double CalculateCosineSimilarity(List<float> a, List<float> b)
        {
            if (a == null || b == null || a.Count == MinVectorCount || b.Count == MinVectorCount) return DefaultScoreValue;

            var n = Math.Min(a.Count, b.Count);
            double dot = DefaultScore, na = DefaultScore, nb = DefaultScore;

            for (int i = 0; i < n; i++)
            {
                double va = a[i];
                double vb = b[i];
                dot += va * vb;
                na += va * va;
                nb += vb * vb;
            }

            if (na == DefaultScore || nb == DefaultScore) return DefaultScoreValue;
            return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
        }
    }
}

