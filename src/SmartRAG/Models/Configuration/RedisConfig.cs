namespace SmartRAG.Models;


/// <summary>
/// Redis storage configuration
/// </summary>
public class RedisConfig
{
    /// <summary>
    /// Redis server connection string
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Redis server password for authentication
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Redis server username for authentication
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Redis database number to use
    /// </summary>
    public int Database { get; set; }

    /// <summary>
    /// Key prefix for all stored documents
    /// </summary>
    public string KeyPrefix { get; set; } = "smartrag:doc:";

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Whether to enable SSL for secure connections
    /// </summary>
    public bool EnableSsl { get; set; }

    /// <summary>
    /// Whether to use SSL for secure connections (alias for EnableSsl)
    /// </summary>
    public bool UseSsl
    {
        get => EnableSsl;
        set => EnableSsl = value;
    }

    /// <summary>
    /// Whether to enable vector search with RediSearch module
    /// </summary>
    public bool EnableVectorSearch { get; set; } = true;

    /// <summary>
    /// Distance metric for vector similarity calculation (COSINE, IP, L2)
    /// </summary>
    public string DistanceMetric { get; set; } = "COSINE";

    /// <summary>
    /// Dimension size of the vector embeddings
    /// </summary>
    public int VectorDimension { get; set; } = 768;

    /// <summary>
    /// Name of the RediSearch vector index
    /// </summary>
    public string VectorIndexName { get; set; } = "smartrag_vector_idx";

    /// <summary>
    /// Number of retry attempts for failed operations
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in milliseconds
    /// </summary>
    public int RetryDelay { get; set; } = 1000;
}

