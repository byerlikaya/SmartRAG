using SmartRAG.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartRAG.Models;


/// <summary>
/// Configuration for database parsing operations
/// </summary>
public class DatabaseConfig
{
    private const int DefaultMaxRowsPerTable = 1000;
    private const int DefaultQueryTimeoutSeconds = 30;
    private const int MinAllowedRowsPerTable = 1;
    private const int MaxAllowedRowsPerTable = 10000;
    private const int MinAllowedTimeoutSeconds = 1;
    private const int MaxAllowedTimeoutSeconds = 300;

    /// <summary>
    /// Type of database to connect to
    /// </summary>
    public DatabaseType Type { get; set; } = DatabaseType.SQLite;

    /// <summary>
    /// Database connection string
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// List of tables to include in extraction (empty means all tables)
    /// </summary>
    public List<string> IncludedTables { get; set; } = new List<string>();

    /// <summary>
    /// List of tables to exclude from extraction
    /// </summary>
    public List<string> ExcludedTables { get; set; } = new List<string>();

    /// <summary>
    /// Maximum number of rows to extract per table
    /// </summary>
    [Range(MinAllowedRowsPerTable, MaxAllowedRowsPerTable)]
    public int MaxRowsPerTable { get; set; } = DefaultMaxRowsPerTable;

    /// <summary>
    /// Whether to include table schema information
    /// </summary>
    public bool IncludeSchema { get; set; } = true;

    /// <summary>
    /// Whether to include foreign key relationships
    /// </summary>
    public bool IncludeForeignKeys { get; set; } = true;

    /// <summary>
    /// Query timeout in seconds
    /// </summary>
    [Range(MinAllowedTimeoutSeconds, MaxAllowedTimeoutSeconds)]
    public int QueryTimeoutSeconds { get; set; } = DefaultQueryTimeoutSeconds;

    /// <summary>
    /// Whether to sanitize sensitive data (replace with placeholders)
    /// </summary>
    public bool SanitizeSensitiveData { get; set; } = true;

    /// <summary>
    /// List of column name patterns that contain sensitive data.
    /// These are generic security patterns applicable to any domain.
    /// Users can customize this list based on their specific requirements.
    /// </summary>
    public List<string> SensitiveColumns { get; set; } = new List<string>
    {
        "password", "pwd", "pass", "secret", "token", "key",
        "ssn", "social_security", "social_security_number",
        "credit_card", "creditcard", "cc_number", "card_number"
    };
}

