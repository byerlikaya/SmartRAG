
namespace SmartRAG.Models.Configuration;


/// <summary>
/// Configuration for connecting to an MCP server
/// </summary>
public class McpServerConfig
{
    /// <summary>
    /// Unique identifier for the server
    /// </summary>
    [Required]
    public string ServerId { get; set; } = string.Empty;

    /// <summary>
    /// Server endpoint URL (HTTP/HTTPS)
    /// </summary>
    [Required]
    [Url]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Optional HTTP headers for authentication or custom configuration
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Whether to automatically connect on startup
    /// </summary>
    public bool AutoConnect { get; set; } = true;

    /// <summary>
    /// Connection timeout in seconds (0 = use default)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}



