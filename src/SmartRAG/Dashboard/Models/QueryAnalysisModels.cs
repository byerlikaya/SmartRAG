using System.Text.Json.Serialization;

namespace SmartRAG.Dashboard.Models;

/// <summary>
/// Request model for query analysis
/// </summary>
public sealed class QueryAnalysisRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;
}

/// <summary>
/// Response model for query analysis
/// </summary>
public sealed class QueryAnalysisResponse
{
    [JsonPropertyName("originalQuery")]
    public string OriginalQuery { get; set; } = string.Empty;

    [JsonPropertyName("queryUnderstanding")]
    public string QueryUnderstanding { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("reasoning")]
    public string? Reasoning { get; set; }

    [JsonPropertyName("databaseQueries")]
    public List<DatabaseQueryItem> DatabaseQueries { get; set; } = new();
}

/// <summary>
/// Database query item for Dashboard display
/// </summary>
public sealed class DatabaseQueryItem
{
    [JsonPropertyName("databaseName")]
    public string DatabaseName { get; set; } = string.Empty;

    [JsonPropertyName("requiredTables")]
    public List<string> RequiredTables { get; set; } = new();

    [JsonPropertyName("generatedQuery")]
    public string GeneratedQuery { get; set; } = string.Empty;

    [JsonPropertyName("purpose")]
    public string Purpose { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public int Priority { get; set; }
}
