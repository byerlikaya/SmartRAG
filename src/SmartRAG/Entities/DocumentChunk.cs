namespace SmartRAG.Entities;

public class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public List<float>? Embedding { get; set; }
    public double? RelevanceScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
