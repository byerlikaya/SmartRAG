namespace SmartRAG.Models;
public class SearchSource
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string RelevantContent { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
}