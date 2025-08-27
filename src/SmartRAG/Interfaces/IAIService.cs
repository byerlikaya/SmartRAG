using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces;

/// <summary>
/// Service interface for AI operations
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Generates AI response based on query and context
    /// </summary>
    Task<string> GenerateResponseAsync(string query, IEnumerable<string> context);

    /// <summary>
    /// Generates embedding vector for the given text
    /// </summary>
    Task<List<float>> GenerateEmbeddingsAsync(string text);

    /// <summary>
    /// Generates embeddings for multiple texts in batch
    /// </summary>
    Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts);
}
