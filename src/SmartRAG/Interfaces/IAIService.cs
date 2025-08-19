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

}
