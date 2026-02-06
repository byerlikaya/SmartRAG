namespace SmartRAG.Models;



/// <summary>
/// In-memory storage configuration
/// </summary>
public class InMemoryConfig
{
    /// <summary>
    /// Maximum number of documents to keep in memory
    /// </summary>
    public int MaxDocuments { get; set; } = 1000;
}

