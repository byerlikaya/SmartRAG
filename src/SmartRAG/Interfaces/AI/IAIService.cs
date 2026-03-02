namespace SmartRAG.Interfaces.AI;

/// <summary>
/// Service interface for AI operations
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Generates AI response based on query and context
    /// </summary>
    /// <param name="query">User query or prompt</param>
    /// <param name="context">Collection of context strings to include in the response</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>AI-generated text response</returns>
    Task<string> GenerateResponseAsync(string query, IEnumerable<string> context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embedding vector for the given text
    /// </summary>
    /// <param name="text">Text to generate embedding for</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>List of float values representing the embedding vector</returns>
    Task<List<float>> GenerateEmbeddingsAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for multiple texts in batch
    /// </summary>
    /// <param name="texts">Collection of texts to generate embeddings for</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>List of embedding vectors, one for each input text</returns>
    Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}