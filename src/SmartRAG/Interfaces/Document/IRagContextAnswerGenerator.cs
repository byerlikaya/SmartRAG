namespace SmartRAG.Interfaces.Document;


/// <summary>
/// Generates an answer from a given context string (e.g. for MCP merge or single-context RAG).
/// </summary>
public interface IRagContextAnswerGenerator
{
    /// <summary>
    /// Generates a RAG answer from the given context using the AI service.
    /// </summary>
    Task<string> GenerateRagAnswerFromContextAsync(string query, string context, string? conversationHistory, CancellationToken cancellationToken = default);
}
