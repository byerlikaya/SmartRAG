using SmartRAG.Enums;

namespace SmartRAG.Models;

/// <summary>
/// Configuration options for SmartRag library
/// </summary>
public class SmartRagOptions
{
    /// <summary>
    /// AI provider to use for text generation and embeddings
    /// </summary>
    public AIProvider AIProvider { get; set; }

    /// <summary>
    /// Storage provider to use for document storage
    /// </summary>
    public StorageProvider StorageProvider { get; set; }

    /// <summary>
    /// Maximum size of each document chunk in characters
    /// </summary>
    public int MaxChunkSize { get; set; } = 1000;

    /// <summary>
    /// Minimum size of each document chunk in characters
    /// </summary>
    public int MinChunkSize { get; set; } = 100;

    /// <summary>
    /// Number of characters to overlap between adjacent chunks (for better context preservation)
    /// </summary>
    public int ChunkOverlap { get; set; } = 200;

    /// <summary>
    /// Maximum number of retry attempts for AI provider requests
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Retry policy for failed requests
    /// </summary>
    public RetryPolicy RetryPolicy { get; set; } = RetryPolicy.ExponentialBackoff;

    /// <summary>
    /// Whether to enable fallback providers when primary provider fails
    /// </summary>
    public bool EnableFallbackProviders { get; set; }

    /// <summary>
    /// List of fallback AI providers to try when primary provider fails
    /// </summary>
    public List<AIProvider> FallbackProviders { get; set; } = [];

    /// <summary>
    /// Maximum number of search results to return
    /// </summary>
    public int MaxSearchResults { get; set; } = 10;

}
