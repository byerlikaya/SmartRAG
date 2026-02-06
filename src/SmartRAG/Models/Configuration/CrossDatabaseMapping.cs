using System.ComponentModel.DataAnnotations;

namespace SmartRAG.Models.Configuration;


/// <summary>
/// Defines a relationship between columns across different databases
/// </summary>
public class CrossDatabaseMapping
{
    /// <summary>
    /// Source database name
    /// </summary>
    [Required(ErrorMessage = "Source database name is required")]
    public string SourceDatabase { get; set; } = string.Empty;

    /// <summary>
    /// Source table name (optional, can be schema.table format)
    /// </summary>
    public string SourceTable { get; set; } = string.Empty;

    /// <summary>
    /// Source column name
    /// </summary>
    [Required(ErrorMessage = "Source column name is required")]
    public string SourceColumn { get; set; } = string.Empty;

    /// <summary>
    /// Target database name
    /// </summary>
    [Required(ErrorMessage = "Target database name is required")]
    public string TargetDatabase { get; set; } = string.Empty;

    /// <summary>
    /// Target table name (optional, can be schema.table format)
    /// </summary>
    public string TargetTable { get; set; } = string.Empty;

    /// <summary>
    /// Target column name
    /// </summary>
    [Required(ErrorMessage = "Target column name is required")]
    public string TargetColumn { get; set; } = string.Empty;

    /// <summary>
    /// Relationship type (PrimaryKey, ForeignKey, or AutoDetected)
    /// </summary>
    public string RelationshipType { get; set; } = "AutoDetected";
}

