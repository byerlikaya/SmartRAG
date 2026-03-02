using System.Text.Json.Serialization;
using SmartRAG.Models.Health;

namespace SmartRAG.Dashboard.Models;

/// <summary>
/// Response model for full health check
/// </summary>
public sealed class HealthCheckResponse
{
    [JsonPropertyName("ai")]
    public HealthStatus Ai { get; set; } = new();

    [JsonPropertyName("storage")]
    public HealthStatus? Storage { get; set; }

    [JsonPropertyName("conversation")]
    public HealthStatus Conversation { get; set; } = new();

    [JsonPropertyName("databases")]
    public List<HealthStatus> Databases { get; set; } = new();
}
