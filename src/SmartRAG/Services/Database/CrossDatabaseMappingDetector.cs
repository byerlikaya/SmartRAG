
namespace SmartRAG.Services.Database;


/// <summary>
/// Detects cross-database relationships based on Primary Key and Foreign Key information
/// </summary>
public class CrossDatabaseMappingDetector
{
    private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer;
    private readonly ILogger<CrossDatabaseMappingDetector> _logger;

    public CrossDatabaseMappingDetector(
        IDatabaseSchemaAnalyzer schemaAnalyzer,
        ILogger<CrossDatabaseMappingDetector> logger)
    {
        _schemaAnalyzer = schemaAnalyzer ?? throw new ArgumentNullException(nameof(schemaAnalyzer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Detects cross-database mappings by analyzing Primary Keys and Foreign Keys across all databases
    /// </summary>
    public async Task<List<CrossDatabaseMapping>> DetectMappingsAsync(
        List<DatabaseConnectionConfig>? connections,
        CancellationToken cancellationToken = default)
    {
        var mappings = new List<CrossDatabaseMapping>();

        if (connections == null || connections.Count < 2)
        {
            _logger.LogInformation("Less than 2 databases configured, no cross-database mappings to detect");
            return mappings;
        }

        var allSchemas = await _schemaAnalyzer.GetAllSchemasAsync(cancellationToken);
        if (allSchemas.Count < 2)
        {
            _logger.LogInformation("Less than 2 schemas available, no cross-database mappings to detect");
            return mappings;
        }

        var connectionMap = new Dictionary<string, DatabaseConnectionConfig>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in connections)
        {
            var key = c.Name;
            connectionMap[key] = c;
        }

        for (var i = 0; i < allSchemas.Count; i++)
        {
            var sourceSchema = allSchemas[i];
            if (!connectionMap.TryGetValue(sourceSchema.DatabaseName, out var sourceConnection))
                continue;

            for (var j = i + 1; j < allSchemas.Count; j++)
            {
                var targetSchema = allSchemas[j];
                if (!connectionMap.TryGetValue(targetSchema.DatabaseName, out var targetConnection))
                    continue;

                var detectedMappings = DetectMappingsBetweenSchemas(
                    sourceSchema,
                    sourceConnection,
                    targetSchema,
                    targetConnection);

                mappings.AddRange(detectedMappings);
            }
        }

        _logger.LogInformation("Detected {Count} cross-database mappings", mappings.Count);
        return mappings;
    }

    private List<CrossDatabaseMapping> DetectMappingsBetweenSchemas(
        DatabaseSchemaInfo sourceSchema,
        DatabaseConnectionConfig sourceConnection,
        DatabaseSchemaInfo targetSchema,
        DatabaseConnectionConfig targetConnection)
    {
        var mappings = new List<CrossDatabaseMapping>();

        foreach (var sourceTable in sourceSchema.Tables)
        {
            foreach (var sourceColumn in sourceTable.Columns)
            {
                if (!sourceColumn.IsPrimaryKey && !sourceColumn.IsForeignKey)
                    continue;

                var matchingTarget = FindMatchingColumn(
                    sourceColumn,
                    targetSchema);

                {
                    var targetColumn = matchingTarget!.Columns.FirstOrDefault(c =>
                        (sourceColumn.IsPrimaryKey && c.IsPrimaryKey) ||
                        (sourceColumn.IsForeignKey && c.IsPrimaryKey));

                    if (targetColumn == null)
                        continue;

                    var mapping = new CrossDatabaseMapping
                    {
                        SourceDatabase = sourceConnection.Name,
                        SourceTable = sourceTable.TableName,
                        SourceColumn = sourceColumn.ColumnName,
                        TargetDatabase = targetConnection.Name,
                        TargetTable = matchingTarget.TableName,
                        TargetColumn = targetColumn.ColumnName,
                        RelationshipType = sourceColumn.IsPrimaryKey ? "PrimaryKey" : "ForeignKey"
                    };

                    mappings.Add(mapping);
                    _logger.LogDebug(
                        "Detected mapping: {SourceDB}.{SourceTable}.{SourceColumn} -> {TargetDB}.{TargetTable}.{TargetColumn}",
                        mapping.SourceDatabase, mapping.SourceTable, mapping.SourceColumn,
                        mapping.TargetDatabase, mapping.TargetTable, mapping.TargetColumn);
                }
            }

            foreach (var foreignKey in sourceTable.ForeignKeys)
            {
                var matchingTarget = FindMatchingTableByForeignKey(
                    foreignKey,
                    targetSchema);

                var mapping = new CrossDatabaseMapping
                {
                    SourceDatabase = sourceConnection.Name,
                    SourceTable = sourceTable.TableName,
                    SourceColumn = foreignKey.ColumnName,
                    TargetDatabase = targetConnection.Name,
                    TargetTable = matchingTarget!.TableName,
                    TargetColumn = foreignKey.ReferencedColumn,
                    RelationshipType = "ForeignKey"
                };

                if (mappings.Any(m => m.SourceColumn == mapping.SourceColumn &&
                                      m.TargetColumn == mapping.TargetColumn &&
                                      m.SourceDatabase == mapping.SourceDatabase &&
                                      m.TargetDatabase == mapping.TargetDatabase))
                    continue;

                mappings.Add(mapping);
                _logger.LogDebug(
                    "Detected FK mapping: {SourceDB}.{SourceTable}.{SourceColumn} -> {TargetDB}.{TargetTable}.{TargetColumn}",
                    mapping.SourceDatabase, mapping.SourceTable, mapping.SourceColumn,
                    mapping.TargetDatabase, mapping.TargetTable, mapping.TargetColumn);
            }
        }

        return mappings;
    }

    private TableSchemaInfo? FindMatchingColumn(
        ColumnSchemaInfo sourceColumn,
        DatabaseSchemaInfo targetSchema)
    {
        foreach (var targetTable in targetSchema.Tables)
        {
            foreach (var targetColumn in targetTable.Columns)
            {
                if (targetColumn.DataType != sourceColumn.DataType)
                    continue;

                if ((!sourceColumn.IsPrimaryKey || !targetColumn.IsPrimaryKey) &&
                    (!sourceColumn.IsForeignKey || !targetColumn.IsPrimaryKey))
                    continue;
                if (AreColumnsSemanticallyRelated(sourceColumn.ColumnName, targetColumn.ColumnName))
                {
                    return targetTable;
                }
            }
        }

        return null;
    }

    private static TableSchemaInfo? FindMatchingTableByForeignKey(
        ForeignKeyInfo foreignKey,
        DatabaseSchemaInfo targetSchema)
    {
        foreach (var targetTable in targetSchema.Tables)
        {
            if (!targetTable.TableName.Equals(foreignKey.ReferencedTable, StringComparison.OrdinalIgnoreCase) &&
                !targetTable.TableName.EndsWith(foreignKey.ReferencedTable, StringComparison.OrdinalIgnoreCase))
                continue;

            var targetColumn = targetTable.Columns.FirstOrDefault(c =>
                c.ColumnName.Equals(foreignKey.ReferencedColumn, StringComparison.OrdinalIgnoreCase) &&
                c.IsPrimaryKey);

            if (targetColumn != null)
            {
                return targetTable;
            }
        }

        return null;
    }

    private static bool AreColumnsSemanticallyRelated(string column1, string column2)
    {
        var normalized1 = NormalizeColumnName(column1);
        var normalized2 = NormalizeColumnName(column2);

        if (normalized1.Equals(normalized2, StringComparison.OrdinalIgnoreCase))
            return true;

        var base1 = RemoveIdSuffix(normalized1);
        var base2 = RemoveIdSuffix(normalized2);

        return base1.Equals(base2, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeColumnName(string columnName)
    {
        return columnName.Replace("_", "").Replace(" ", "");
    }

    private static string RemoveIdSuffix(string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return columnName;

        if (columnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase) || columnName.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
            return columnName[..^2];

        if (columnName.EndsWith("_ID", StringComparison.OrdinalIgnoreCase) || columnName.EndsWith("_Id", StringComparison.OrdinalIgnoreCase))
            return columnName[..^3];

        return columnName;
    }
}

