using SmartRAG.Models;

namespace SmartRAG.Interfaces;

/// <summary>
/// Interface for AI providers
/// </summary>
public interface IAIProvider
{
    Task<string> GenerateTextAsync(string prompt, AIProviderConfig config);
    Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config);
    Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000);
}
