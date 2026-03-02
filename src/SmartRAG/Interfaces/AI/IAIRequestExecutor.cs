
namespace SmartRAG.Interfaces.AI;

/// <summary>
/// Interface for executing AI requests with a specific provider
/// </summary>
public interface IAIRequestExecutor
{
    /// <summary>
    /// Generates a response using the specified provider
    /// </summary>
    /// <param name="provider">AI provider to use</param>
    /// <param name="query">User query or prompt</param>
    /// <param name="context">Collection of context strings</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task<string> GenerateResponseAsync(AIProvider provider, string query, IEnumerable<string> context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings using the specified provider
    /// </summary>
    /// <param name="provider">AI provider to use</param>
    /// <param name="text">Text to generate embedding for</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task<List<float>> GenerateEmbeddingsAsync(AIProvider provider, string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates batch embeddings using the specified provider
    /// </summary>
    /// <param name="provider">AI provider to use</param>
    /// <param name="texts">Collection of texts to generate embeddings for</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task<List<List<float>>> GenerateEmbeddingsBatchAsync(AIProvider provider, IEnumerable<string> texts, CancellationToken cancellationToken = default);
}