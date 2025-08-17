namespace SmartRAG.Interfaces;

/// <summary>
/// Service interface for AI operations
/// </summary>
public interface IAIService
{
    Task<string> GenerateResponseAsync(string query, IEnumerable<string> context);
    Task<List<float>> GenerateEmbeddingsAsync(string text);
    Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts);
}
