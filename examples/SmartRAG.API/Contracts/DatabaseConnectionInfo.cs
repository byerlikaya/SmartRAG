using SmartRAG.Enums;

namespace SmartRAG.API.Contracts;


/// <summary>
/// Database connection information DTO
/// </summary>
public class DatabaseConnectionInfoDto : IDto
{
    /// <summary>
    /// Unique database identifier
    /// </summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>
    /// Database name
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Database type
    /// </summary>
    public DatabaseType DatabaseType { get; set; }

    /// <summary>
    /// Whether the connection is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Whether the connection is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Schema analysis status
    /// </summary>
    public string SchemaStatus { get; set; } = "Unknown";

    /// <summary>
    /// Number of tables in this database
    /// </summary>
    public int TableCount { get; set; }

    /// <summary>
    /// Total row count (approximate)
    /// </summary>
    public long TotalRows { get; set; }

    /// <summary>
    /// When schema was last analyzed
    /// </summary>
    public DateTime? LastAnalyzed { get; set; }
}


