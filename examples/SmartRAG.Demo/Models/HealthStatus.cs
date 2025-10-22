namespace SmartRAG.Demo.Models;

/// <summary>
/// Represents the health status of a service
/// </summary>
public class HealthStatus
{
    public bool IsHealthy { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

