using SmartRAG.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces;

/// <summary>
/// Interface for AI providers
/// </summary>
public interface IAIProvider
{
    /// <summary>
    /// Generates text response using the AI provider
    /// </summary>
    Task<string> GenerateTextAsync(string prompt, AIProviderConfig config);

    /// <summary>
    /// Generates embedding vector for the given text
    /// </summary>
    Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config);

    /// <summary>
    /// Generates embeddings for multiple texts in a single request (if supported)
    /// </summary>
    Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, AIProviderConfig config);

    /// <summary>
    /// Chunks text into smaller segments for processing
    /// </summary>
    Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000);
}
