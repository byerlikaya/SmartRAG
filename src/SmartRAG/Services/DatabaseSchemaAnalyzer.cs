using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Npgsql;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartRAG.Services
{
    /// <summary>
    /// Analyzes database schemas and generates intelligent metadata
    /// </summary>
    public class DatabaseSchemaAnalyzer : IDatabaseSchemaAnalyzer
    {
        private readonly IDatabaseParserService _databaseParserService;
        private readonly IAIService _aiService;
        private readonly ILogger<DatabaseSchemaAnalyzer> _logger;
        private readonly ConcurrentDictionary<string, DatabaseSchemaInfo> _schemaCache;
        private readonly ConcurrentDictionary<string, DateTime> _lastRefreshTimes;

        public DatabaseSchemaAnalyzer(
            IDatabaseParserService databaseParserService,
            IAIService aiService,
            ILogger<DatabaseSchemaAnalyzer> logger)
        {
            _databaseParserService = databaseParserService;
            _aiService = aiService;
            _logger = logger;
            _schemaCache = new ConcurrentDictionary<string, DatabaseSchemaInfo>();
            _lastRefreshTimes = new ConcurrentDictionary<string, DateTime>();
        }

        public async Task<DatabaseSchemaInfo> AnalyzeDatabaseSchemaAsync(DatabaseConnectionConfig connectionConfig)
        {
            var databaseId = await GetDatabaseIdAsync(connectionConfig);
            
            _logger.LogInformation("Analyzing schema for database: {DatabaseId}", databaseId);

            var schemaInfo = new DatabaseSchemaInfo
            {
                DatabaseId = databaseId,
                DatabaseType = connectionConfig.DatabaseType,
                Description = connectionConfig.Description,
                Status = SchemaAnalysisStatus.InProgress,
                LastAnalyzed = DateTime.UtcNow
            };

            try
            {
                // Get database name
                schemaInfo.DatabaseName = await ExtractDatabaseNameAsync(connectionConfig);

                // Get all tables
                var tableNames = await _databaseParserService.GetTableNamesAsync(
                    connectionConfig.ConnectionString, 
                    connectionConfig.DatabaseType);

                // Filter tables if needed
                tableNames = FilterTables(tableNames, connectionConfig);

                _logger.LogInformation("Found {Count} tables in database {DatabaseId}", tableNames.Count, databaseId);

                // Analyze each table
                long totalRows = 0;
                foreach (var tableName in tableNames)
                {
                    var tableInfo = await AnalyzeTableAsync(
                        connectionConfig.ConnectionString,
                        tableName,
                        connectionConfig.DatabaseType);
                    
                    schemaInfo.Tables.Add(tableInfo);
                    totalRows += tableInfo.RowCount;
                }

                schemaInfo.TotalRowCount = totalRows;

                // Generate AI summary if description not provided
                if (string.IsNullOrEmpty(schemaInfo.Description))
                {
                    schemaInfo.AISummary = await GenerateAISummaryAsync(schemaInfo);
                }

                schemaInfo.Status = SchemaAnalysisStatus.Completed;
                
                // Cache the result
                _schemaCache[databaseId] = schemaInfo;
                _lastRefreshTimes[databaseId] = DateTime.UtcNow;

                _logger.LogInformation("Schema analysis completed for database: {DatabaseId}", databaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing schema for database: {DatabaseId}", databaseId);
                schemaInfo.Status = SchemaAnalysisStatus.Failed;
                schemaInfo.ErrorMessage = ex.Message;
            }

            return schemaInfo;
        }

        public Task<DatabaseSchemaInfo> RefreshSchemaAsync(string databaseId)
        {
            _logger.LogInformation("Refreshing schema for database: {DatabaseId}", databaseId);
            
            // Get the original connection config from cache
            if (!_schemaCache.TryGetValue(databaseId, out var oldSchema))
            {
                throw new InvalidOperationException($"Database {databaseId} not found in cache");
            }

            // We need to store connection configs separately - for now throw error
            throw new NotImplementedException("Schema refresh requires connection config to be stored");
        }

        public async Task<List<DatabaseSchemaInfo>> GetAllSchemasAsync()
        {
            return await Task.FromResult(_schemaCache.Values.ToList());
        }

        public Task<DatabaseSchemaInfo> GetSchemaAsync(string databaseId)
        {
            _schemaCache.TryGetValue(databaseId, out var schema);
            return Task.FromResult(schema);
        }

        public Task<List<string>> GetSchemasNeedingRefreshAsync()
        {
            var needRefresh = new List<string>();
            var now = DateTime.UtcNow;

            foreach (var kvp in _lastRefreshTimes)
            {
                var databaseId = kvp.Key;
                var lastRefresh = kvp.Value;

                if (_schemaCache.TryGetValue(databaseId, out var schema))
                {
                    // Check if refresh is needed based on configured interval
                    // This would need connection config to check interval
                    // For now, just return empty list
                }
            }

            return Task.FromResult(needRefresh);
        }

        public async Task<string> GenerateAISummaryAsync(DatabaseSchemaInfo schemaInfo)
        {
            try
            {
                var prompt = BuildSummaryPrompt(schemaInfo);
                var summary = await _aiService.GenerateResponseAsync(prompt, new List<string>());
                return summary?.Trim() ?? "Database schema analysis completed.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate AI summary for database {DatabaseId}", schemaInfo.DatabaseId);
                return GenerateFallbackSummary(schemaInfo);
            }
        }

        #region Private Helper Methods

        private async Task<string> GetDatabaseIdAsync(DatabaseConnectionConfig config)
        {
            if (!string.IsNullOrEmpty(config.Name))
            {
                return config.Name;
            }

            // Auto-generate from database name in connection string
            var dbName = await ExtractDatabaseNameAsync(config);
            return $"{config.DatabaseType}_{dbName}";
        }

        private async Task<string> ExtractDatabaseNameAsync(DatabaseConnectionConfig config)
        {
            try
            {
                // Try to extract database name from connection string first (without opening connection)
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
                        catch { /* Fall through to connection attempt */ }
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
                        catch { /* Fall through to connection attempt */ }
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
                        catch { /* Fall through to connection attempt */ }
                        break;
                }
                
                // If we couldn't extract from connection string, try opening connection
                try
                {
                    using (var connection = CreateConnection(config.ConnectionString, config.DatabaseType))
                    {
                        await connection.OpenAsync();
                        var databaseName = connection.Database;
                        
                        if (!string.IsNullOrEmpty(databaseName))
                        {
                            return databaseName;
                        }
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
            // Apply included tables filter
            if (config.IncludedTables != null && config.IncludedTables.Length > 0)
            {
                tableNames = tableNames
                    .Where(t => config.IncludedTables.Contains(t, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }

            // Apply excluded tables filter
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
            DatabaseType databaseType)
        {
            var tableInfo = new TableSchemaInfo
            {
                TableName = tableName
            };

            try
            {
                using (var connection = CreateConnection(connectionString, databaseType))
                {
                await connection.OpenAsync();

                // Get columns
                tableInfo.Columns = await GetColumnsAsync(connection, tableName, databaseType);

                // Get primary keys
                tableInfo.PrimaryKeys = tableInfo.Columns
                    .Where(c => c.IsPrimaryKey)
                    .Select(c => c.ColumnName)
                    .ToList();

                // Get foreign keys
                tableInfo.ForeignKeys = await GetForeignKeysAsync(connection, tableName, databaseType);

                // Mark foreign key columns
                foreach (var fk in tableInfo.ForeignKeys)
                {
                    var column = tableInfo.Columns.FirstOrDefault(c => c.ColumnName == fk.ColumnName);
                    if (column != null)
                    {
                        column.IsForeignKey = true;
                    }
                }

                // Get row count
                tableInfo.RowCount = await GetRowCountAsync(connection, tableName);

                // Get sample data (first 3 rows)
                tableInfo.SampleData = await GetSampleDataAsync(connection, tableName, databaseType);
                }
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (databaseType == DatabaseType.SqlServer)
            {
                // If database doesn't exist, log warning and return empty table info
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
            
            // SQLite has different GetSchema behavior - use PRAGMA instead
            if (databaseType == DatabaseType.SQLite)
            {
                // For SQLite, use direct PRAGMA query
                return GetColumnsSQLiteAsync(connection, tableName);
            }
            else
            {
                // For other databases, use GetSchema
                schema = connection.GetSchema("Columns", new[] { null, null, tableName, null });
            }

            foreach (DataRow row in schema.Rows)
            {
                var column = new ColumnSchemaInfo
                {
                    ColumnName = row["COLUMN_NAME"].ToString() ?? string.Empty,
                    DataType = row["DATA_TYPE"].ToString() ?? string.Empty,
                    IsNullable = row["IS_NULLABLE"].ToString()?.ToUpper() == "YES"
                };

                // Try to get max length
                if (row.Table.Columns.Contains("CHARACTER_MAXIMUM_LENGTH") && 
                    row["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value)
                {
                    column.MaxLength = Convert.ToInt32(row["CHARACTER_MAXIMUM_LENGTH"]);
                }

                columns.Add(column);
            }

            // Get primary keys (only for non-SQLite databases, SQLite already has PK info)
            if (databaseType != DatabaseType.SQLite)
            {
                try
                {
                    var pkSchema = connection.GetSchema("IndexColumns", new[] { null, null, tableName, null });
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
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $"PRAGMA table_info('{tableName}')";
                    using (var reader = cmd.ExecuteReader())
                    {
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

            // SQLite uses PRAGMA for foreign keys
            if (databaseType == DatabaseType.SQLite)
            {
                return GetForeignKeysSQLiteAsync(connection, tableName);
            }

            try
            {
                // Use SQL query instead of GetSchema for better compatibility
                using (var cmd = connection.CreateCommand())
                {
                    if (databaseType == DatabaseType.SqlServer)
                    {
                        cmd.CommandText = $@"
                            SELECT 
                                fk.name AS ForeignKeyName,
                                COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
                                OBJECT_NAME(fkc.referenced_object_id) AS ReferencedTable,
                                COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn
                            FROM sys.foreign_keys AS fk
                            INNER JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
                            WHERE OBJECT_NAME(fk.parent_object_id) = '{tableName}'";
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
                            WHERE TABLE_NAME = '{tableName}' 
                            AND REFERENCED_TABLE_NAME IS NOT NULL";
                    }
                    else if (databaseType == DatabaseType.PostgreSQL)
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
                            WHERE tc.constraint_type = 'FOREIGN KEY' AND tc.table_name = '{tableName}'";
                    }
                    else
                    {
                        return Task.FromResult(foreignKeys);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
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
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $"PRAGMA foreign_key_list('{tableName}')";
                    using (var reader = cmd.ExecuteReader())
                    {
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
                using (var cmd = connection.CreateCommand())
                {
                    // Use appropriate identifier quoting based on database type
                    string quotedTable;
                    var connectionType = connection.GetType().Name;
                    
                    if (connectionType == "SqlConnection")
                    {
                        quotedTable = $"[{tableName}]";
                    }
                    else if (connectionType == "MySqlConnection")
                    {
                        quotedTable = $"`{tableName}`";
                    }
                    else if (connectionType == "NpgsqlConnection")
                    {
                        quotedTable = $"\"{tableName}\"";
                    }
                    else
                    {
                        quotedTable = tableName; // SQLite doesn't require quotes for simple table names
                    }
                    
                    cmd.CommandText = $"SELECT COUNT(*) FROM {quotedTable}";
                    var result = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt64(result);
                }
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
                // Use appropriate identifier quoting and limit syntax based on database type
                string query;
                switch (databaseType)
                {
                    case DatabaseType.SqlServer:
                        query = $"SELECT TOP 3 * FROM [{tableName}]";
                        break;
                    case DatabaseType.MySQL:
                        query = $"SELECT * FROM `{tableName}` LIMIT 3";
                        break;
                    case DatabaseType.PostgreSQL:
                        query = $"SELECT * FROM \"{tableName}\" LIMIT 3";
                        break;
                    case DatabaseType.SQLite:
                    default:
                        query = $"SELECT * FROM {tableName} LIMIT 3";
                        break;
                }

                using (var cmd = connection.CreateCommand())
                {
                cmd.CommandText = query;
                
                using (var reader = await cmd.ExecuteReaderAsync())
                {
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
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not get sample data for table {TableName}", tableName);
                return string.Empty;
            }
        }

        private DbConnection CreateConnection(string connectionString, DatabaseType databaseType)
        {
            switch (databaseType)
            {
                case DatabaseType.SQLite:
                    return new SqliteConnection(connectionString);
                case DatabaseType.SqlServer:
                    return new SqlConnection(connectionString);
                case DatabaseType.MySQL:
                    return new MySqlConnection(connectionString);
                case DatabaseType.PostgreSQL:
                    return new NpgsqlConnection(connectionString);
                default:
                    throw new NotSupportedException($"Database type {databaseType} is not supported");
            }
        }

        private string BuildSummaryPrompt(DatabaseSchemaInfo schemaInfo)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Analyze this database schema and provide a concise summary of its purpose and content:");
            sb.AppendLine();
            sb.AppendLine($"Database: {schemaInfo.DatabaseName} ({schemaInfo.DatabaseType})");
            sb.AppendLine($"Total Tables: {schemaInfo.Tables.Count}");
            sb.AppendLine($"Total Rows: {schemaInfo.TotalRowCount:N0}");
            sb.AppendLine();
            sb.AppendLine("Tables:");

            foreach (var table in schemaInfo.Tables.Take(10))
            {
                sb.AppendLine($"- {table.TableName} ({table.RowCount:N0} rows, {table.Columns.Count} columns)");
                sb.AppendLine($"  Columns: {string.Join(", ", table.Columns.Take(5).Select(c => c.ColumnName))}");
            }

            sb.AppendLine();
            sb.AppendLine("Provide a 2-3 sentence summary describing what kind of data this database contains and its likely purpose.");

            return sb.ToString();
        }

        private string GenerateFallbackSummary(DatabaseSchemaInfo schemaInfo)
        {
            return $"Database '{schemaInfo.DatabaseName}' contains {schemaInfo.Tables.Count} tables " +
                   $"with approximately {schemaInfo.TotalRowCount:N0} total rows. " +
                   $"Main tables: {string.Join(", ", schemaInfo.Tables.Take(5).Select(t => t.TableName))}.";
        }

        #endregion
    }
}

