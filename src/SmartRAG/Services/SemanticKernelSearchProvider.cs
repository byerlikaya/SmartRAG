using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;

namespace SmartRAG.Services;

/// <summary>
/// Semantic search provider using embeddings and cosine similarity
/// </summary>
public class SemanticKernelSearchProvider : ISemanticSearchProvider
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IAIProviderFactory _aiProviderFactory;
    private readonly IConfiguration _configuration;
    private readonly SmartRagOptions _options;
    private readonly ILogger<SemanticKernelSearchProvider> _logger;

    public SemanticKernelSearchProvider(
        IDocumentRepository documentRepository,
        IAIProviderFactory aiProviderFactory,
        IConfiguration configuration,
        SmartRagOptions options,
        ILogger<SemanticKernelSearchProvider> logger)
    {
        _documentRepository = documentRepository;
        _aiProviderFactory = aiProviderFactory;
        _configuration = configuration;
        _options = options;
        _logger = logger;
    }

    public async Task<List<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults = 5)
    {
        try
        {
            _logger.LogInformation("Starting semantic search for query: {Query}", query);

            // Get all documents and chunks
            var allDocuments = await _documentRepository.GetAllAsync();
            var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();

            if (allChunks.Count == 0)
            {
                _logger.LogInformation("No chunks found in repository");
                return new List<DocumentChunk>();
            }

            // Get configured AI provider
            var providerKey = _options.AIProvider.ToString();
            var providerConfig = _configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

            if (providerConfig == null || string.IsNullOrEmpty(providerConfig.ApiKey))
            {
                _logger.LogWarning("No AI provider configuration found");
                return new List<DocumentChunk>();
            }

            var aiProvider = _aiProviderFactory.CreateProvider(_options.AIProvider);

            // 1. Generate query embedding (ONCE)
            var queryEmbedding = await aiProvider.GenerateEmbeddingAsync(query, providerConfig);
            if (queryEmbedding == null || queryEmbedding.Count == 0)
            {
                _logger.LogWarning("Failed to generate query embedding");
                return new List<DocumentChunk>();
            }

            // 2. Generate batch embeddings for all chunks (ONCE)
            var allChunkContents = allChunks.Select(c => c.Content).ToList();
            var allChunkEmbeddings = await aiProvider.GenerateEmbeddingsBatchAsync(allChunkContents, providerConfig);

            if (allChunkEmbeddings == null || allChunkEmbeddings.Count != allChunks.Count)
            {
                _logger.LogWarning("Failed to generate batch embeddings for chunks");
                return new List<DocumentChunk>();
            }

            // 3. Calculate similarity for each chunk (NO API calls, just math!)
            var scoredChunks = new List<(DocumentChunk chunk, double score)>();

            for (int i = 0; i < allChunks.Count; i++)
            {
                var chunk = allChunks[i];
                var chunkEmbedding = allChunkEmbeddings[i];

                var similarity = CalculateCosineSimilarity(queryEmbedding, chunkEmbedding);
                scoredChunks.Add((chunk, similarity));
            }

            // 4. Sort by similarity and take top results
            var topChunks = scoredChunks
                .Where(x => x.score > 0.3) // Only include relevant chunks
                .OrderByDescending(x => x.score)
                .Take(maxResults)
                .Select(x =>
                {
                    x.chunk.RelevanceScore = x.score;
                    return x.chunk;
                })
                .ToList();

            _logger.LogInformation("Found {Count} relevant chunks with similarity > 0.3", topChunks.Count);
            return topChunks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during semantic search");
            return new List<DocumentChunk>();
        }
    }


    /// <summary>
    /// Calculate cosine similarity between two embedding vectors
    /// </summary>
    private static double CalculateCosineSimilarity(List<float> vectorA, List<float> vectorB)
    {
        if (vectorA.Count != vectorB.Count)
            return 0.0;

        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < vectorA.Count; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        if (normA == 0.0 || normB == 0.0)
            return 0.0;

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
