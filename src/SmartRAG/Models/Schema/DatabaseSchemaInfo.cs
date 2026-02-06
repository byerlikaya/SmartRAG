using SmartRAG.Enums;
using System;
using System.Collections.Generic;

namespace SmartRAG.Models;


/// <summary>
/// Comprehensive schema information for a database
/// </summary>
public class DatabaseSchemaInfo
{
    /// <summary>
    /// Unique identifier for the database
    /// </summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>
    /// Database name (from connection or auto-detected)
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Database type
    /// </summary>
    public DatabaseType DatabaseType { get; set; }

    /// <summary>
    /// When this schema was last analyzed
    /// </summary>
    public DateTime LastAnalyzed { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tables in this database
    /// </summary>
    public List<TableSchemaInfo> Tables { get; set; } = new List<TableSchemaInfo>();

    /// <summary>
    /// Total row count across all tables (approximate)
    /// </summary>
    public long TotalRowCount { get; set; }

    /// <summary>
    /// Schema analysis status
    /// </summary>
    public SchemaAnalysisStatus Status { get; set; } = SchemaAnalysisStatus.Pending;

    /// <summary>
    /// Error message if analysis failed
    /// </summary>
    public string ErrorMessage { get; set; }
}

/// <summary>
/// Schema information for a single table
/// </summary>
public class TableSchemaInfo
{
    /// <summary>
    /// Table name
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Columns in this table
    /// </summary>
    public List<ColumnSchemaInfo> Columns { get; set; } = new List<ColumnSchemaInfo>();

    /// <summary>
    /// Primary key columns
    /// </summary>
    public List<string> PrimaryKeys { get; set; } = new List<string>();

    /// <summary>
    /// Foreign key relationships
    /// </summary>
    public List<ForeignKeyInfo> ForeignKeys { get; set; } = new List<ForeignKeyInfo>();

    /// <summary>
    /// Approximate row count
    /// </summary>
    public long RowCount { get; set; }

    /// <summary>
    /// Sample data (first few rows) for AI context
    /// </summary>
    public string SampleData { get; set; }
}

/// <summary>
/// Schema information for a column
/// </summary>
public class ColumnSchemaInfo
{
    /// <summary>
    /// Column name
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Data type
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Whether column is nullable
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Whether this is a primary key
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Whether this is a foreign key
    /// </summary>
    public bool IsForeignKey { get; set; }

    /// <summary>
    /// Maximum length (for string types)
    /// </summary>
    public int? MaxLength { get; set; }
}

/// <summary>
/// Foreign key relationship information
/// </summary>
public class ForeignKeyInfo
{
    /// <summary>
    /// Foreign key name
    /// </summary>
    public string ForeignKeyName { get; set; } = string.Empty;

    /// <summary>
    /// Column in current table
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Referenced table
    /// </summary>
    public string ReferencedTable { get; set; } = string.Empty;

    /// <summary>
    /// Referenced column
    /// </summary>
    public string ReferencedColumn { get; set; } = string.Empty;
}


