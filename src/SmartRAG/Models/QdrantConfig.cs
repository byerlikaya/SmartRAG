namespace SmartRAG.Models
{
    /// <summary>
    /// Configuration settings for Qdrant vector database storage
    /// </summary>
    public class QdrantConfig
    {
        /// <summary>
        /// Qdrant server host address
        /// </summary>
        public string Host { get; set; } = "localhost";
        
        /// <summary>
        /// Whether to use HTTPS for secure connections
        /// </summary>
        public bool UseHttps { get; set; }
        
        /// <summary>
        /// API key for authentication (if required)
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
        
        /// <summary>
        /// Name of the vector collection to store documents
        /// </summary>
        public string CollectionName { get; set; } = "smartrag_documents";
        
        /// <summary>
        /// Dimension size of the vector embeddings
        /// </summary>
        public int VectorSize { get; set; } = 768;
        
        /// <summary>
        /// Distance metric for vector similarity calculation (Cosine, Dot, Euclidean)
        /// </summary>
        public string DistanceMetric { get; set; } = "Cosine";
    }
}
