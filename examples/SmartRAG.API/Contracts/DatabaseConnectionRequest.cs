
namespace SmartRAG.API.Contracts;


/// <summary>
/// API request model for connecting to live databases
/// </summary>
public class DatabaseConnectionApiRequest
{
    /// <summary>
    /// Database connection string
    /// </summary>
    /// <example>Server=localhost;Database=Northwind;Trusted_Connection=true;</example>
    [Required(ErrorMessage = "Connection string is required")]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Type of database to connect to
    /// </summary>
    /// <example>SqlServer</example>
    [Required(ErrorMessage = "Database type is required")]
    public DatabaseType DatabaseType { get; set; } = DatabaseType.SQLite;

    /// <summary>
    /// List of specific tables to include (optional - if empty, all tables will be processed)
    /// </summary>
    /// <example>["Customers", "Orders", "Products"]</example>
    public List<string> IncludedTables { get; set; } = new List<string>();

    /// <summary>
    /// List of tables to exclude from processing (optional)
    /// </summary>
    /// <example>["SystemLogs", "TempData"]</example>
    public List<string> ExcludedTables { get; set; } = new List<string>();

    /// <summary>
    /// Maximum number of rows to extract per table
    /// </summary>
    /// <example>1000</example>
    [Range(1, 10000, ErrorMessage = "Max rows must be between 1 and 10000")]
    [DefaultValue(1000)]
    public int MaxRows { get; set; } = 1000;

    /// <summary>
    /// Whether to include table schema information (column types, constraints)
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool IncludeSchema { get; set; } = true;

    /// <summary>
    /// Whether to include foreign key relationships
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool IncludeForeignKeys { get; set; } = true;

    /// <summary>
    /// Whether to include index information
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool IncludeIndexes { get; set; } = false;

    /// <summary>
    /// Whether to sanitize sensitive data (replace with placeholders)
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool SanitizeSensitiveData { get; set; } = true;

    /// <summary>
    /// Query timeout in seconds
    /// </summary>
    /// <example>30</example>
    [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
    [DefaultValue(30)]
    public int QueryTimeoutSeconds { get; set; } = 30;
}

