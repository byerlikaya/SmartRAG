using SmartRAG.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SmartRAG.API.Contracts;


/// <summary>
/// API request model for executing custom SQL queries
/// </summary>
public class QueryExecutionApiRequest
{
    /// <summary>
    /// Database connection string
    /// </summary>
    /// <example>Server=localhost;Database=Northwind;Trusted_Connection=true;</example>
    [Required(ErrorMessage = "Connection string is required")]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// SQL query to execute
    /// </summary>
    /// <example>SELECT TOP 10 CustomerID, CompanyName FROM Customers WHERE Country = 'USA'</example>
    [Required(ErrorMessage = "SQL query is required")]
    [MinLength(5, ErrorMessage = "Query must be at least 5 characters long")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Type of database
    /// </summary>
    /// <example>SqlServer</example>
    [Required(ErrorMessage = "Database type is required")]
    public DatabaseType DatabaseType { get; set; } = DatabaseType.SQLite;

    /// <summary>
    /// Maximum number of rows to return
    /// </summary>
    /// <example>100</example>
    [Range(1, 10000, ErrorMessage = "Max rows must be between 1 and 10000")]
    [DefaultValue(100)]
    public int MaxRows { get; set; } = 100;

    /// <summary>
    /// Query timeout in seconds
    /// </summary>
    /// <example>30</example>
    [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
    [DefaultValue(30)]
    public int QueryTimeoutSeconds { get; set; } = 30;
}

