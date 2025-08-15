namespace SmartRAG.Entities;

public class Document
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public List<DocumentChunk> Chunks { get; set; } = [];
    public Dictionary<string, object>? Metadata { get; set; }
    public long FileSize { get; set; }
}
