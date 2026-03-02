using System.Text.Json.Serialization;

namespace SmartRAG.Dashboard.Models;

/// <summary>
/// Response model for database connection status
/// </summary>
public sealed class ConnectionStatusResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("databaseType")]
    public string DatabaseType { get; set; } = string.Empty;

    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("tableCount")]
    public int TableCount { get; set; }

    [JsonPropertyName("totalRowCount")]
    public long TotalRowCount { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
