namespace SmartRAG.Models.Health;

/// <summary>
/// Represents the health status of a service
/// </summary>
public sealed class HealthStatus
{
    /// <summary>
    /// Whether the service is healthy and operational
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Display name of the service (e.g. Ollama, Qdrant, Redis)
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional details (endpoint, connection state, error message)
    /// </summary>
    public string Details { get; set; } = string.Empty;
}
