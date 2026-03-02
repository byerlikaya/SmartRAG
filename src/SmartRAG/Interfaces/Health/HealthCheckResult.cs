using SmartRAG.Models.Health;

namespace SmartRAG.Interfaces.Health;

/// <summary>
/// Aggregated result of a full health check across all configured services
/// </summary>
public sealed class HealthCheckResult
{
    /// <summary>
    /// AI provider status (Ollama, OpenAI, etc.)
    /// </summary>
    public HealthStatus Ai { get; set; } = new();

    /// <summary>
    /// Vector storage status (Qdrant when StorageProvider is Qdrant)
    /// </summary>
    public HealthStatus? Storage { get; set; }

    /// <summary>
    /// Conversation/cache storage status (Redis, SQLite, etc.)
    /// </summary>
    public HealthStatus Conversation { get; set; } = new();

    /// <summary>
    /// Database connection statuses when EnableDatabaseSearch is true
    /// </summary>
    public List<HealthStatus> Databases { get; set; } = new();
}
