using SmartRAG.Entities;

namespace SmartRAG.Models.Results;


/// <summary>
/// Result of document relevance calculation
/// </summary>
public class DocumentScoreResult
{
    /// <summary>
    /// Document entity
    /// </summary>
    public Document Document { get; set; } = null!;

    /// <summary>
    /// Calculated relevance score
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Number of query words matched in the document
    /// </summary>
    public int QueryWordMatches { get; set; }

    /// <summary>
    /// Number of unique keywords (keywords that appear only in this document)
    /// </summary>
    public int UniqueKeywords { get; set; }
}


