using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Npgsql;

namespace SmartRAG.Services.Database;


/// <summary>
/// Analyzes database schemas and generates intelligent metadata
/// </summary>
public class DatabaseSchemaAnalyzer : IDatabaseSchemaAnalyzer
{
    private readonly IDatabaseParserService _databaseParserService;
    private readonly ILogger<DatabaseSchemaAnalyzer> _logger;
    private readonly ConcurrentDictionary<string, DatabaseSchemaInfo> _schemaCache;
    private readonly ConcurrentDictionary<string, DateTime> _lastRefreshTimes;
    private readonly SmartRagOptions _options;

    public DatabaseSchemaAnalyzer(
        IDatabaseParserService databaseParserService,
        ILogger<DatabaseSchemaAnalyzer> logger,
        IOptions<SmartRagOptions> options)
    {
        _databaseParserService = databaseParserService;
        _logger = logger;
        _schemaCache = new ConcurrentDictionary<string, DatabaseSchemaInfo>();
        _lastRefreshTimes = new ConcurrentDictionary<string, DateTime>();
        _options = options.Value;
    }

    /// <summary>
    /// [DB Query] Analyzes database schema
    /// </summary>
    public async Task<DatabaseSchemaInfo> AnalyzeDatabaseSchemaAsync(DatabaseConnectionConfig connectionConfig, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var databaseId = await GetDatabaseIdAsync(connectionConfig, cancellationToken);

        var schemaInfo = new DatabaseSchemaInfo
        {
            DatabaseId = databaseId,
            DatabaseType = connectionConfig.DatabaseType,
            Status = SchemaAnalysisStatus.InProgress,
            LastAnalyzed = DateTime.UtcNow
        };

        try
        {
            schemaInfo.DatabaseName = await ExtractDatabaseNameAsync(connectionConfig, cancellationToken);

            var tableNames = await _databaseParserService.GetTableNamesAsync(
                connectionConfig.ConnectionString,
                connectionConfig.DatabaseType,
                cancellationToken);

            tableNames = FilterTables(tableNames, connectionConfig);

            long totalRows = 0;

            foreach (var tableName in tableNames)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var tableInfo = await AnalyzeTableAsync(
                    connectionConfig.ConnectionString,
                    tableName,
                    connectionConfig.DatabaseType,
                    cancellationToken);

                schemaInfo.Tables.Add(tableInfo);
                totalRows += tableInfo.RowCount;
            }

            schemaInfo.TotalRowCount = totalRows;

            schemaInfo.Status = SchemaAnalysisStatus.Completed;

            _schemaCache[databaseId] = schemaInfo;
            _lastRefreshTimes[databaseId] = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing schema for database");
            schemaInfo.Status = SchemaAnalysisStatus.Failed;
            schemaInfo.ErrorMessage = ex.Message;
        }

        return schemaInfo;
    }

    public async Task<List<DatabaseSchemaInfo>> GetAllSchemasAsync(CancellationToken cancellationToken = default)
    {
        if (_schemaCache.IsEmpty && _options.DatabaseConnections != null && _options.DatabaseConnections.Count > 0)
        {
            foreach (var connection in _options.DatabaseConnections)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    await AnalyzeDatabaseSchemaAsync(connection, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to analyze schema for configured database connection");
                }
            }
        }

        return _schemaCache.Values.ToList();
    }

    public Task<DatabaseSchemaInfo> GetSchemaAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _schemaCache.TryGetValue(databaseId, out var schema);
        return Task.FromResult(schema);
    }

    private async Task<string> GetDatabaseIdAsync(DatabaseConnectionConfig config, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(config.Name))
        {
            return config.Name;
        }

        var dbName = await ExtractDatabaseNameAsync(config, cancellationToken);
        return $"{config.DatabaseType}_{dbName}";
    }

    private async Task<string> ExtractDatabaseNameAsync(DatabaseConnectionConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (config.DatabaseType)
            {
                case DatabaseType.SqlServer:
                    try
                    {
                        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(config.ConnectionString);
                        if (!string.IsNullOrEmpty(builder.InitialCatalog))
                        {
                            return builder.InitialCatalog;
                        }
                    }
                    catch (Exception)
                    {
                        // Connection string parsing failed, fall through to connection attempt
                        // This is expected behavior when connection string format is non-standard
                    }
                    break;

                case DatabaseType.SQLite:
                    var match = System.Text.RegularExpressions.Regex.Match(
                        config.ConnectionString,
                        @"Data Source=(.+?)(;|$)");

                    if (match.Success)
                    {
                        var path = match.Groups[1].Value;
                        return System.IO.Path.GetFileNameWithoutExtension(path);
                    }
                    break;

                case DatabaseType.MySQL:
                    try
                    {
                        var builder = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder(config.ConnectionString);
                        if (!string.IsNullOrEmpty(builder.Database))
                        {
                            return builder.Database;
                        }
                    }
                    catch (Exception)
                    {
                        // Connection string parsing failed, fall through to connection attempt
                        // This is expected behavior when connection string format is non-standard
                    }
                    break;

                case DatabaseType.PostgreSQL:
                    try
                    {
                        var builder = new Npgsql.NpgsqlConnectionStringBuilder(config.ConnectionString);
                        if (!string.IsNullOrEmpty(builder.Database))
                        {
                            return builder.Database;
                        }
                    }
                    catch (Exception)
                    {
                        // Connection string parsing failed, fall through to connection attempt
                        // This is expected behavior when connection string format is non-standard
                    }
                    break;
            }

            try
            {
                using var connection = CreateConnection(config.ConnectionString, config.DatabaseType);
                await connection.OpenAsync(cancellationToken);
                var databaseName = connection.Database;

                if (!string.IsNullOrEmpty(databaseName))
                {
                    return databaseName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not open connection to extract database name, using connection string info");
            }

            return config.Name ?? "UnknownDatabase";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not extract database name from connection string");
            return config.Name ?? "UnknownDatabase";
        }
    }

    private List<string> FilterTables(List<string> tableNames, DatabaseConnectionConfig config)
    {
        if (config.IncludedTables != null && config.IncludedTables.Length > 0)
        {
            tableNames = tableNames
                .Where(t => config.IncludedTables.Contains(t, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        if (config.ExcludedTables != null && config.ExcludedTables.Length > 0)
        {
            tableNames = tableNames
                .Where(t => !config.ExcludedTables.Contains(t, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        return tableNames;
    }

    private async Task<TableSchemaInfo> AnalyzeTableAsync(
        string connectionString,
        string tableName,
        DatabaseType databaseType,
        CancellationToken cancellationToken = default)
    {
        var tableInfo = new TableSchemaInfo
        {
            TableName = tableName
        };

        try
        {
            using var connection = CreateConnection(connectionString, databaseType);
            await connection.OpenAsync(cancellationToken);

            tableInfo.Columns = await GetColumnsAsync(connection, tableName, databaseType);

            tableInfo.PrimaryKeys = tableInfo.Columns
                .Where(c => c.IsPrimaryKey)
                .Select(c => c.ColumnName)
                .ToList();

            tableInfo.ForeignKeys = await GetForeignKeysAsync(connection, tableName, databaseType);

            foreach (var fk in tableInfo.ForeignKeys)
            {
                var column = tableInfo.Columns.FirstOrDefault(c => c.ColumnName == fk.ColumnName);
                if (column != null)
                {
                    column.IsForeignKey = true;
                }
            }

            tableInfo.RowCount = await GetRowCountAsync(connection, tableName);

            tableInfo.SampleData = await GetSampleDataAsync(connection, tableName, databaseType);
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (databaseType == DatabaseType.SqlServer)
        {
            if (sqlEx.Number == 4060 || sqlEx.Message.Contains("Cannot open database"))
            {
                _logger.LogWarning("SQL Server database does not exist yet for table {TableName}", tableName);
                return tableInfo; // Return empty table info
            }

            _logger.LogWarning(sqlEx, "Error analyzing SQL Server table {TableName}", tableName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error analyzing table {TableName}", tableName);
        }

        return tableInfo;
    }

    private Task<List<ColumnSchemaInfo>> GetColumnsAsync(
        DbConnection connection,
        string tableName,
        DatabaseType databaseType)
    {
        var columns = new List<ColumnSchemaInfo>();

        DataTable schema;

        if (databaseType == DatabaseType.SQLite)
        {
            return GetColumnsSQLiteAsync(connection, tableName);
        }
        else
        {
            string schemaName = null;
            string actualTableName = tableName;
            
            if (tableName.Contains('.'))
            {
                var parts = tableName.Split('.', 2);
                schemaName = parts[0];
                actualTableName = parts[1];
            }
            
            schema = connection.GetSchema("Columns", new[] { null, schemaName, actualTableName, null });
        }

        foreach (DataRow row in schema.Rows)
        {
            var column = new ColumnSchemaInfo
            {
                ColumnName = row["COLUMN_NAME"].ToString() ?? string.Empty,
                DataType = row["DATA_TYPE"].ToString() ?? string.Empty,
                IsNullable = row["IS_NULLABLE"].ToString()?.ToUpper() == "YES"
            };

            if (row.Table.Columns.Contains("CHARACTER_MAXIMUM_LENGTH") &&
                row["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value)
            {
                var maxLengthValue = row["CHARACTER_MAXIMUM_LENGTH"];
                // Handle LONGBLOB/BLOB types that may return -1 or very large values
                if (maxLengthValue is long longValue)
                {
                    if (longValue > int.MaxValue || longValue < int.MinValue)
                    {
                        // For BLOB types, set to null or a reasonable max
                        column.MaxLength = null;
                    }
                    else
                    {
                        column.MaxLength = (int)longValue;
                    }
                }
                else
                {
                    try
                    {
                        column.MaxLength = Convert.ToInt32(maxLengthValue);
                    }
                    catch (OverflowException)
                    {
                        // For BLOB/LONGBLOB types, set to null
                        column.MaxLength = null;
                    }
                }
            }

            columns.Add(column);
        }

        if (databaseType != DatabaseType.SQLite)
        {
            try
            {
                string schemaName = null;
                string actualTableName = tableName;
                
                if (tableName.Contains('.'))
                {
                    var parts = tableName.Split('.', 2);
                    schemaName = parts[0];
                    actualTableName = parts[1];
                }
                
                var pkSchema = connection.GetSchema("IndexColumns", new[] { null, schemaName, actualTableName, null });
                var primaryKeyColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (DataRow row in pkSchema.Rows)
                {
                    if (row.Table.Columns.Contains("constraint_name") &&
                        row["constraint_name"].ToString()?.IndexOf("PK", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var columnName = row["column_name"].ToString();
                        if (!string.IsNullOrEmpty(columnName))
                        {
                            primaryKeyColumns.Add(columnName);
                        }
                    }
                }

                foreach (var column in columns)
                {
                    column.IsPrimaryKey = primaryKeyColumns.Contains(column.ColumnName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not retrieve primary key information for table {TableName}", tableName);
            }
        }

        return Task.FromResult(columns);
    }

    private Task<List<ColumnSchemaInfo>> GetColumnsSQLiteAsync(DbConnection connection, string tableName)
    {
        var columns = new List<ColumnSchemaInfo>();

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info('{tableName}')";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var column = new ColumnSchemaInfo
                {
                    ColumnName = reader["name"].ToString() ?? string.Empty,
                    DataType = reader["type"].ToString() ?? string.Empty,
                    IsNullable = reader["notnull"].ToString() == "0",
                    IsPrimaryKey = reader["pk"].ToString() != "0"
                };

                columns.Add(column);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting SQLite columns for table {TableName}", tableName);
        }

        return Task.FromResult(columns);
    }

    private Task<List<ForeignKeyInfo>> GetForeignKeysAsync(
        DbConnection connection,
        string tableName,
        DatabaseType databaseType)
    {
        var foreignKeys = new List<ForeignKeyInfo>();

        if (databaseType == DatabaseType.SQLite)
        {
            return GetForeignKeysSQLiteAsync(connection, tableName);
        }

        try
        {
            using var cmd = connection.CreateCommand();
            
            string schemaName = null;
            string actualTableName = tableName;
            
            if (tableName.Contains('.'))
            {
                var parts = tableName.Split('.', 2);
                schemaName = parts[0];
                actualTableName = parts[1];
            }
            
            if (databaseType == DatabaseType.SqlServer)
            {
                if (!string.IsNullOrEmpty(schemaName))
                {
                    cmd.CommandText = $@"
                        SELECT 
                            fk.name AS ForeignKeyName,
                            COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
                            SCHEMA_NAME(OBJECTPROPERTY(fkc.referenced_object_id, 'SchemaId')) + '.' + OBJECT_NAME(fkc.referenced_object_id) AS ReferencedTable,
                            COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn
                        FROM sys.foreign_keys AS fk
                        INNER JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
                        WHERE SCHEMA_NAME(OBJECTPROPERTY(fk.parent_object_id, 'SchemaId')) = '{schemaName}'
                        AND OBJECT_NAME(fk.parent_object_id) = '{actualTableName}'";
                }
                else
                {
                    cmd.CommandText = $@"
                        SELECT 
                            fk.name AS ForeignKeyName,
                            COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
                            OBJECT_NAME(fkc.referenced_object_id) AS ReferencedTable,
                            COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn
                        FROM sys.foreign_keys AS fk
                        INNER JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
                        WHERE OBJECT_NAME(fk.parent_object_id) = '{actualTableName}'";
                }
            }
            else if (databaseType == DatabaseType.MySQL)
            {
                cmd.CommandText = $@"
                    SELECT 
                        CONSTRAINT_NAME AS ForeignKeyName,
                        COLUMN_NAME AS ColumnName,
                        REFERENCED_TABLE_NAME AS ReferencedTable,
                        REFERENCED_COLUMN_NAME AS ReferencedColumn
                    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                    WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = '{actualTableName}' 
                    AND REFERENCED_TABLE_NAME IS NOT NULL";
            }
            else if (databaseType == DatabaseType.PostgreSQL)
            {
                if (!string.IsNullOrEmpty(schemaName))
                {
                    cmd.CommandText = $@"
                        SELECT 
                            tc.constraint_name AS ForeignKeyName,
                            kcu.column_name AS ColumnName,
                            ccu.table_schema || '.' || ccu.table_name AS ReferencedTable,
                            ccu.column_name AS ReferencedColumn
                        FROM information_schema.table_constraints AS tc 
                        JOIN information_schema.key_column_usage AS kcu ON tc.constraint_name = kcu.constraint_name
                        JOIN information_schema.constraint_column_usage AS ccu ON ccu.constraint_name = tc.constraint_name
                        WHERE tc.constraint_type = 'FOREIGN KEY' 
                        AND tc.table_schema = '{schemaName}'
                        AND tc.table_name = '{actualTableName}'";
                }
                else
                {
                    cmd.CommandText = $@"
                        SELECT 
                            tc.constraint_name AS ForeignKeyName,
                            kcu.column_name AS ColumnName,
                            ccu.table_name AS ReferencedTable,
                            ccu.column_name AS ReferencedColumn
                        FROM information_schema.table_constraints AS tc 
                        JOIN information_schema.key_column_usage AS kcu ON tc.constraint_name = kcu.constraint_name
                        JOIN information_schema.constraint_column_usage AS ccu ON ccu.constraint_name = tc.constraint_name
                        WHERE tc.constraint_type = 'FOREIGN KEY' 
                        AND tc.table_name = '{actualTableName}'";
                }
            }
            else
            {
                return Task.FromResult(foreignKeys);
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var fk = new ForeignKeyInfo
                {
                    ForeignKeyName = reader["ForeignKeyName"]?.ToString() ?? string.Empty,
                    ColumnName = reader["ColumnName"]?.ToString() ?? string.Empty,
                    ReferencedTable = reader["ReferencedTable"]?.ToString() ?? string.Empty,
                    ReferencedColumn = reader["ReferencedColumn"]?.ToString() ?? string.Empty
                };

                if (!string.IsNullOrEmpty(fk.ColumnName) && !string.IsNullOrEmpty(fk.ReferencedTable))
                {
                    foreignKeys.Add(fk);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not retrieve foreign keys for table {TableName}", tableName);
        }

        return Task.FromResult(foreignKeys);
    }

    private Task<List<ForeignKeyInfo>> GetForeignKeysSQLiteAsync(DbConnection connection, string tableName)
    {
        var foreignKeys = new List<ForeignKeyInfo>();

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA foreign_key_list('{tableName}')";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var fk = new ForeignKeyInfo
                {
                    ForeignKeyName = $"FK_{tableName}_{reader["from"]}",
                    ColumnName = reader["from"].ToString() ?? string.Empty,
                    ReferencedTable = reader["table"].ToString() ?? string.Empty,
                    ReferencedColumn = reader["to"].ToString() ?? string.Empty
                };

                if (!string.IsNullOrEmpty(fk.ColumnName) && !string.IsNullOrEmpty(fk.ReferencedTable))
                {
                    foreignKeys.Add(fk);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not retrieve SQLite foreign keys for table {TableName}", tableName);
        }

        return Task.FromResult(foreignKeys);
    }

    private async Task<long> GetRowCountAsync(DbConnection connection, string tableName)
    {
        try
        {
            using var cmd = connection.CreateCommand();
            string quotedTable;
            var connectionType = connection.GetType().Name;

            if (connectionType == "SqlConnection")
            {
                if (tableName.Contains('.'))
                {
                    var parts = tableName.Split('.', 2);
                    quotedTable = $"[{parts[0]}].[{parts[1]}]";
                }
                else
                {
                    quotedTable = $"[{tableName}]";
                }
            }
            else if (connectionType == "MySqlConnection")
            {
                if (tableName.Contains('.'))
                {
                    var parts = tableName.Split('.', 2);
                    quotedTable = $"`{parts[0]}`.`{parts[1]}`";
                }
                else
                {
                    quotedTable = $"`{tableName}`";
                }
            }
            else if (connectionType == "NpgsqlConnection")
            {
                if (tableName.Contains('.'))
                {
                    var parts = tableName.Split('.', 2);
                    quotedTable = $"\"{parts[0]}\".\"{parts[1]}\"";
                }
                else
                {
                    quotedTable = $"\"{tableName}\"";
                }
            }
            else
            {
                quotedTable = tableName;
            }

            cmd.CommandText = $"SELECT COUNT(*) FROM {quotedTable}";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not get row count for table {TableName}", tableName);
            return 0;
        }
    }

    private async Task<string> GetSampleDataAsync(
        DbConnection connection,
        string tableName,
        DatabaseType databaseType)
    {
        try
        {
            string quotedTable;
            var connectionType = connection.GetType().Name;
            
            if (connectionType == "SqlConnection")
            {
                if (tableName.Contains('.'))
                {
                    var parts = tableName.Split('.', 2);
                    quotedTable = $"[{parts[0]}].[{parts[1]}]";
                }
                else
                {
                    quotedTable = $"[{tableName}]";
                }
            }
            else if (connectionType == "MySqlConnection")
            {
                if (tableName.Contains('.'))
                {
                    var parts = tableName.Split('.', 2);
                    quotedTable = $"`{parts[0]}`.`{parts[1]}`";
                }
                else
                {
                    quotedTable = $"`{tableName}`";
                }
            }
            else if (connectionType == "NpgsqlConnection")
            {
                if (tableName.Contains('.'))
                {
                    var parts = tableName.Split('.', 2);
                    quotedTable = $"\"{parts[0]}\".\"{parts[1]}\"";
                }
                else
                {
                    quotedTable = $"\"{tableName}\"";
                }
            }
            else
            {
                quotedTable = tableName;
            }
            
            string query = databaseType switch
            {
                DatabaseType.SqlServer => $"SELECT TOP 3 * FROM {quotedTable}",
                DatabaseType.MySQL => $"SELECT * FROM {quotedTable} LIMIT 3",
                DatabaseType.PostgreSQL => $"SELECT * FROM {quotedTable} LIMIT 3",
                _ => $"SELECT * FROM {quotedTable} LIMIT 3",
            };
            using var cmd = connection.CreateCommand();
            cmd.CommandText = query;

            using var reader = await cmd.ExecuteReaderAsync();
            var sb = new StringBuilder();
            var rowCount = 0;

            while (await reader.ReadAsync() && rowCount < 3)
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    sb.Append($"{reader.GetName(i)}: {reader.GetValue(i)}, ");
                }
                sb.AppendLine();
                rowCount++;
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not get sample data for table {TableName}", tableName);
            return string.Empty;
        }
    }

    private DbConnection CreateConnection(string connectionString, DatabaseType databaseType)
    {
        if (databaseType == DatabaseType.SQLite)
        {
            var sanitizedConnectionString = ValidateAndSanitizeSQLiteConnectionString(connectionString);
            return new SqliteConnection(sanitizedConnectionString);
        }
        
        return databaseType switch
        {
            DatabaseType.SqlServer => new SqlConnection(connectionString),
            DatabaseType.MySQL => new MySqlConnection(connectionString),
            DatabaseType.PostgreSQL => new NpgsqlConnection(connectionString),
            _ => throw new NotSupportedException($"Database type {databaseType} is not supported"),
        };
    }

    private string ValidateAndSanitizeSQLiteConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        try
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);

            if (!string.IsNullOrEmpty(builder.DataSource))
            {
                var dataSource = builder.DataSource;

                if (Path.IsPathRooted(dataSource))
                {
                    builder.DataSource = Path.GetFullPath(dataSource);
                }
                else
                {
                    var currentDir = Directory.GetCurrentDirectory();
                    var resolvedPath = Path.Combine(currentDir, dataSource);
                    
                    if (!File.Exists(resolvedPath))
                    {
                        var projectRoot = FindProjectRoot(currentDir);
                        if (!string.IsNullOrEmpty(projectRoot))
                        {
                            var projectRootPath = Path.Combine(projectRoot, dataSource);
                            resolvedPath = projectRootPath;
                        }
                    }
                    
                    var fullPath = Path.GetFullPath(resolvedPath);
                    var directory = Path.GetDirectoryName(fullPath);
                    
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    builder.DataSource = fullPath;
                }
            }

            return builder.ConnectionString;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid SQLite connection string format: {ex.Message}", nameof(connectionString), ex);
        }
    }

    private static string FindProjectRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "SmartRAG.sln")))
            {
                return Path.Combine(dir.FullName, "examples", "SmartRAG.Demo");
            }
            dir = dir.Parent;
        }
        return null;
    }

}


