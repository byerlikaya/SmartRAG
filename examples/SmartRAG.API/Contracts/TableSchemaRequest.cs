using SmartRAG.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartRAG.API.Contracts;


/// <summary>
/// API request model for getting table schema information
/// </summary>
public class TableSchemaApiRequest
{
    /// <summary>
    /// Database connection string
    /// </summary>
    /// <example>Server=localhost;Database=Northwind;Trusted_Connection=true;</example>
    [Required(ErrorMessage = "Connection string is required")]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Name of the table to get schema for
    /// </summary>
    /// <example>Customers</example>
    [Required(ErrorMessage = "Table name is required")]
    [MinLength(1, ErrorMessage = "Table name cannot be empty")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Type of database
    /// </summary>
    /// <example>SqlServer</example>
    [Required(ErrorMessage = "Database type is required")]
    public DatabaseType DatabaseType { get; set; } = DatabaseType.SQLite;
}

