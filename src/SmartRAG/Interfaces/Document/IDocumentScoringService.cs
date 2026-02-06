
namespace SmartRAG.Interfaces.Document;


/// <summary>
/// Service interface for scoring document chunks
/// </summary>
public interface IDocumentScoringService
{
    /// <summary>
    /// Scores document chunks based on query relevance
    /// </summary>
    /// <param name="chunks">Document chunks to score</param>
    /// <param name="query">Search query</param>
    /// <param name="queryWords">Tokenized query words</param>
    /// <param name="potentialNames">Potential names extracted from query</param>
    /// <returns>Scored document chunks</returns>
    List<DocumentChunk> ScoreChunks(List<DocumentChunk> chunks, string query, List<string> queryWords, List<string> potentialNames);
}


