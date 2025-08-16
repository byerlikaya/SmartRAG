namespace SmartRAG.Models;

public class QdrantConfig
{
    public string Host { get; set; } = "localhost";
    public bool UseHttps { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string CollectionName { get; set; } = "smartrag_documents";
    public int VectorSize { get; set; } = 768;
    public string DistanceMetric { get; set; } = "Cosine";

}
