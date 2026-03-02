using System.Text.Json.Serialization;

namespace SmartRAG.Dashboard.Models;

/// <summary>
/// Response model for database schema details
/// </summary>
public sealed class SchemaDetailResponse
{
    [JsonPropertyName("databaseName")]
    public string DatabaseName { get; set; } = string.Empty;

    [JsonPropertyName("databaseType")]
    public string DatabaseType { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("totalRowCount")]
    public long TotalRowCount { get; set; }

    [JsonPropertyName("tables")]
    public List<TableSchemaItem> Tables { get; set; } = new();
}

/// <summary>
/// Table schema item for Dashboard display
/// </summary>
public sealed class TableSchemaItem
{
    [JsonPropertyName("tableName")]
    public string TableName { get; set; } = string.Empty;

    [JsonPropertyName("rowCount")]
    public long RowCount { get; set; }

    [JsonPropertyName("columns")]
    public List<ColumnSchemaItem> Columns { get; set; } = new();

    [JsonPropertyName("foreignKeys")]
    public List<ForeignKeyItem> ForeignKeys { get; set; } = new();
}

/// <summary>
/// Column schema item for Dashboard display
/// </summary>
public sealed class ColumnSchemaItem
{
    [JsonPropertyName("columnName")]
    public string ColumnName { get; set; } = string.Empty;

    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = string.Empty;

    [JsonPropertyName("isNullable")]
    public bool IsNullable { get; set; }

    [JsonPropertyName("isPrimaryKey")]
    public bool IsPrimaryKey { get; set; }

    [JsonPropertyName("isForeignKey")]
    public bool IsForeignKey { get; set; }
}

/// <summary>
/// Foreign key item for Dashboard display
/// </summary>
public sealed class ForeignKeyItem
{
    [JsonPropertyName("columnName")]
    public string ColumnName { get; set; } = string.Empty;

    [JsonPropertyName("referencedTable")]
    public string ReferencedTable { get; set; } = string.Empty;

    [JsonPropertyName("referencedColumn")]
    public string ReferencedColumn { get; set; } = string.Empty;
}
