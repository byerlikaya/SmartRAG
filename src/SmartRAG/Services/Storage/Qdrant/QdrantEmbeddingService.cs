
namespace SmartRAG.Services.Storage.Qdrant;


/// <summary>
/// Service for generating embeddings for text content using hash-based approach
/// </summary>
public class QdrantEmbeddingService : IQdrantEmbeddingService
{
    private const int DefaultVectorDimension = 768;

    private readonly ILogger<QdrantEmbeddingService> _logger;
    private readonly QdrantConfig _config;

    /// <summary>
    /// Initializes a new instance of the QdrantEmbeddingService
    /// </summary>
    /// <param name="config">Qdrant configuration options</param>
    /// <param name="logger">Logger instance for this service</param>
    public QdrantEmbeddingService(IOptions<QdrantConfig> config, ILogger<QdrantEmbeddingService> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    /// <summary>
    /// Generates an embedding vector for the given text using hash-based approach
    /// </summary>
    /// <param name="text">Text to generate embedding for</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>List of float values representing the embedding vector</returns>
    public async Task<List<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var vectorDimension = await GetVectorDimensionAsync(cancellationToken);

            var hash = text.GetHashCode();
            var random = new Random(hash);
            var embedding = new List<float>(vectorDimension);

            for (var i = 0; i < vectorDimension; i++)
            {
                if (i % 64 == 0)
                    cancellationToken.ThrowIfCancellationRequested();
                embedding.Add((float)(random.NextDouble() * 2 - 1)); // -1 to 1
            }

            cancellationToken.ThrowIfCancellationRequested();
            var sumSquares = embedding.Sum(t => t * t);

            var magnitude = (float)Math.Sqrt(sumSquares);
            if (!(magnitude > 0.001f))
                return embedding; // Avoid division by very small numbers
            var invMagnitude = 1.0f / magnitude;
            for (var i = 0; i < embedding.Count; i++)
            {
                embedding[i] *= invMagnitude;
            }

            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding for text");
            return new List<float>();
        }
    }

    private Task<int> GetVectorDimensionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return Task.FromResult(_config.VectorSize > 0 ? _config.VectorSize : DefaultVectorDimension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get vector dimension, using default");
            return Task.FromResult(DefaultVectorDimension);
        }
    }
}

