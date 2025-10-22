using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Npgsql;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartRAG.Services
{
    /// <summary>
    /// Service for parsing database files and live database connections
    /// </summary>
    public class DatabaseParserService : IDatabaseParserService
    {
        #region Constants

        private const int DefaultMaxRows = 1000;
        private const int DefaultQueryTimeout = 30;
        private const string SensitiveDataPlaceholder = "[SENSITIVE_DATA]";

        // Database file extensions
        private static readonly string[] DatabaseFileExtensions = { ".db", ".sqlite", ".sqlite3", ".db3" };

        // Sensitive column patterns
        private static readonly string[] DefaultSensitiveColumns = {
            "password", "pwd", "pass", "secret", "token", "key",
            "ssn", "social_security", "social_security_number",
            "credit_card", "creditcard", "cc_number", "card_number",
            "email", "email_address", "phone", "phone_number", "mobile"
        };

        #endregion

        #region Fields

        private readonly SmartRagOptions _options;
        private readonly ILogger<DatabaseParserService> _logger;
        private readonly IMemoryCache _cache;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the DatabaseParserService
        /// </summary>
        public DatabaseParserService(
            IOptions<SmartRagOptions> options,
            ILogger<DatabaseParserService> logger,
            IMemoryCache cache)
        {
            _options = options.Value;
            _logger = logger;
            _cache = cache;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Parses a database file (SQLite) and extracts content for RAG processing
        /// </summary>
        public async Task<string> ParseDatabaseFileAsync(Stream dbStream, string fileName)
        {
            if (dbStream == null) throw new ArgumentNullException(nameof(dbStream));
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));

            _logger.LogInformation("Starting database file parsing for: {FileName}", fileName);

            var tempPath = Path.GetTempFileName();
            
            try
            {
                using (var fileStream = File.Create(tempPath))
                {
                    await dbStream.CopyToAsync(fileStream);
                }

                var connectionString = $"Data Source={tempPath};Mode=ReadOnly;";
                
                var config = new DatabaseConfig
                {
                    Type = DatabaseType.SQLite,
                    ConnectionString = connectionString,
                    MaxRowsPerTable = DefaultMaxRows,
                    IncludeSchema = true,
                    SanitizeSensitiveData = true
                };

                var result = await ParseSQLiteDatabaseAsync(connectionString, config);
                
                _logger.LogInformation("Database file parsing completed for: {FileName}", fileName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing database file: {FileName}", fileName);
                throw;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary file: {TempPath}", tempPath);
                }
            }
        }

        /// <summary>
        /// Connects to a live database and extracts content based on configuration
        /// </summary>
        public async Task<string> ParseDatabaseConnectionAsync(string connectionString, DatabaseConfig config)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            if (config == null) throw new ArgumentNullException(nameof(config));

            _logger.LogInformation("Starting database connection parsing for type: {DatabaseType}", config.Type);

            try
            {
                if (config.Type == DatabaseType.SQLite)
                    return await ParseSQLiteDatabaseAsync(connectionString, config);
                else if (config.Type == DatabaseType.SqlServer)
                    return await ParseSqlServerDatabaseAsync(connectionString, config);
                else if (config.Type == DatabaseType.MySQL)
                    return await ParseMySqlDatabaseAsync(connectionString, config);
                else if (config.Type == DatabaseType.PostgreSQL)
                    return await ParsePostgreSqlDatabaseAsync(connectionString, config);
                else
                    throw new NotSupportedException($"Database type {config.Type} is not supported");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing database connection for type: {DatabaseType}", config.Type);
                throw;
            }
        }

        /// <summary>
        /// Gets list of table names from the database
        /// </summary>
        public async Task<List<string>> GetTableNamesAsync(string connectionString, DatabaseType databaseType)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            if (databaseType == DatabaseType.SQLite)
                return await GetSQLiteTableNamesAsync(connectionString);
            else if (databaseType == DatabaseType.SqlServer)
                return await GetSqlServerTableNamesAsync(connectionString);
            else if (databaseType == DatabaseType.MySQL)
                return await GetMySqlTableNamesAsync(connectionString);
            else if (databaseType == DatabaseType.PostgreSQL)
                return await GetPostgreSqlTableNamesAsync(connectionString);
            else
                throw new NotSupportedException($"Database type {databaseType} is not supported");
        }

        /// <summary>
        /// Gets schema information for a specific table
        /// </summary>
        public async Task<string> GetTableSchemaAsync(string connectionString, string tableName, DatabaseType databaseType)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));

            if (databaseType == DatabaseType.SQLite)
                return await GetSQLiteTableSchemaAsync(connectionString, tableName);
            else if (databaseType == DatabaseType.SqlServer)
                return await GetSqlServerTableSchemaAsync(connectionString, tableName);
            else if (databaseType == DatabaseType.MySQL)
                return await GetMySqlTableSchemaAsync(connectionString, tableName);
            else if (databaseType == DatabaseType.PostgreSQL)
                return await GetPostgreSqlTableSchemaAsync(connectionString, tableName);
            else
                throw new NotSupportedException($"Database type {databaseType} is not supported");
        }

        /// <summary>
        /// Extracts data from a specific table
        /// </summary>
        public async Task<string> ExtractTableDataAsync(string connectionString, string tableName, DatabaseType databaseType, int maxRows = DefaultMaxRows)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));

            var config = new DatabaseConfig
            {
                Type = databaseType,
                ConnectionString = connectionString,
                IncludedTables = new List<string> { tableName },
                MaxRowsPerTable = maxRows,
                IncludeSchema = true
            };

            return await ParseDatabaseConnectionAsync(connectionString, config);
        }

        /// <summary>
        /// Executes a custom SQL query and returns results
        /// </summary>
        public async Task<string> ExecuteQueryAsync(string connectionString, string query, DatabaseType databaseType, int maxRows = DefaultMaxRows)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrWhiteSpace(query)) throw new ArgumentNullException(nameof(query));

            _logger.LogInformation("Executing custom query for database type: {DatabaseType}", databaseType);

            if (databaseType == DatabaseType.SQLite)
                return await ExecuteSQLiteQueryAsync(connectionString, query, maxRows);
            else if (databaseType == DatabaseType.SqlServer)
                return await ExecuteSqlServerQueryAsync(connectionString, query, maxRows);
            else if (databaseType == DatabaseType.MySQL)
                return await ExecuteMySqlQueryAsync(connectionString, query, maxRows);
            else if (databaseType == DatabaseType.PostgreSQL)
                return await ExecutePostgreSqlQueryAsync(connectionString, query, maxRows);
            else
                throw new NotSupportedException($"Database type {databaseType} is not supported");
        }

        /// <summary>
        /// Validates database connection
        /// </summary>
        public async Task<bool> ValidateConnectionAsync(string connectionString, DatabaseType databaseType)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return false;

            try
            {
                if (databaseType == DatabaseType.SQLite)
                    return await ValidateSQLiteConnectionAsync(connectionString);
                else if (databaseType == DatabaseType.SqlServer)
                    return await ValidateSqlServerConnectionAsync(connectionString);
                else if (databaseType == DatabaseType.MySQL)
                    return await ValidateMySqlConnectionAsync(connectionString);
                else if (databaseType == DatabaseType.PostgreSQL)
                    return await ValidatePostgreSqlConnectionAsync(connectionString);
                else
                    return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database connection validation failed for type: {DatabaseType}", databaseType);
                return false;
            }
        }

        /// <summary>
        /// Gets supported database types
        /// </summary>
        public IEnumerable<DatabaseType> GetSupportedDatabaseTypes()
        {
            return new[]
            {
                DatabaseType.SQLite,
                DatabaseType.SqlServer,
                DatabaseType.MySQL,
                DatabaseType.PostgreSQL
            };
        }

        /// <summary>
        /// Gets supported file extensions for database files
        /// </summary>
        public IEnumerable<string> GetSupportedDatabaseFileExtensions()
        {
            return DatabaseFileExtensions;
        }

        /// <summary>
        /// Clears memory cache and disposes resources for memory optimization
        /// </summary>
        public void ClearMemoryCache()
        {
            _logger.LogInformation("Clearing database parser memory cache");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            _logger.LogInformation("Memory cache cleared successfully");
        }

        #endregion

        #region SQLite Implementation

        private async Task<string> ParseSQLiteDatabaseAsync(string connectionString, DatabaseConfig config)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();

                var content = new StringBuilder();
                content.AppendLine("=== SQLite Database Content ===");
                content.AppendLine($"Database: {connection.DataSource}");
                content.AppendLine();

                var allTables = await GetSQLiteTableNamesInternalAsync(connection);
                var tablesToProcess = FilterTables(allTables, config);

                _logger.LogInformation("Processing {TableCount} tables from SQLite database", tablesToProcess.Count);

                foreach (var tableName in tablesToProcess)
                {
                    try
                    {
                        content.AppendLine($"--- Table: {tableName} ---");

                        if (config.IncludeSchema)
                        {
                            var schema = await GetSQLiteTableSchemaInternalAsync(connection, tableName);
                            content.AppendLine(schema);
                        }

                        var tableData = await GetSQLiteTableDataInternalAsync(connection, tableName, config);
                        content.AppendLine(tableData);
                        content.AppendLine();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing table: {TableName}", tableName);
                        content.AppendLine($"Error processing table {tableName}: {ex.Message}");
                        content.AppendLine();
                    }
                }

                return content.ToString();
            }
        }

        private async Task<List<string>> GetSQLiteTableNamesAsync(string connectionString)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                return await GetSQLiteTableNamesInternalAsync(connection);
            }
        }

        private async Task<List<string>> GetSQLiteTableNamesInternalAsync(SqliteConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";

                var tables = new List<string>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }

                return tables;
            }
        }

        private async Task<string> GetSQLiteTableSchemaAsync(string connectionString, string tableName)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                return await GetSQLiteTableSchemaInternalAsync(connection, tableName);
            }
        }

        private async Task<string> GetSQLiteTableSchemaInternalAsync(SqliteConnection connection, string tableName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"PRAGMA table_info({tableName})";

                var schema = new StringBuilder();
                schema.AppendLine($"Table: {tableName}");
                schema.AppendLine("Columns:");

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var columnName = reader.GetString(1);
                        var dataType = reader.GetString(2);
                        var notNull = reader.GetBoolean(3);
                        var primaryKey = reader.GetBoolean(5);

                        schema.Append($"  - {columnName} ({dataType})");
                        if (notNull) schema.Append(" NOT NULL");
                        if (primaryKey) schema.Append(" PRIMARY KEY");
                        schema.AppendLine();
                    }
                }

                return schema.ToString();
            }
        }

        private async Task<string> GetSQLiteTableDataInternalAsync(SqliteConnection connection, string tableName, DatabaseConfig config)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT * FROM [{tableName}] LIMIT {config.MaxRowsPerTable}";
                command.CommandTimeout = config.QueryTimeoutSeconds;

                return await GetTableDataInternalAsync(command, config);
            }
        }

        private async Task<string> ExecuteSQLiteQueryAsync(string connectionString, string query, int maxRows)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.CommandTimeout = DefaultQueryTimeout;

                    return await ExecuteQueryInternalAsync(command, query, maxRows);
                }
            }
        }

        private async Task<bool> ValidateSQLiteConnectionAsync(string connectionString)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                return connection.State == System.Data.ConnectionState.Open;
            }
        }

        #endregion

        #region SQL Server Implementation

        private async Task<string> ParseSqlServerDatabaseAsync(string connectionString, DatabaseConfig config)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var content = new StringBuilder();
                    content.AppendLine("=== SQL Server Database Content ===");
                    content.AppendLine($"Database: {connection.Database}");
                    content.AppendLine($"Server: {connection.DataSource}");
                    content.AppendLine();

                    var allTables = await GetSqlServerTableNamesInternalAsync(connection);
                    var tablesToProcess = FilterTables(allTables, config);

                _logger.LogInformation("Processing {TableCount} tables from SQL Server database", tablesToProcess.Count);

                foreach (var tableName in tablesToProcess)
                {
                    try
                    {
                        content.AppendLine($"--- Table: {tableName} ---");

                        if (config.IncludeSchema)
                        {
                            var schema = await GetSqlServerTableSchemaInternalAsync(connection, tableName);
                            content.AppendLine(schema);
                        }

                        var tableData = await GetSqlServerTableDataInternalAsync(connection, tableName, config);
                        content.AppendLine(tableData);
                        content.AppendLine();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing table: {TableName}", tableName);
                        content.AppendLine($"Error processing table {tableName}: {ex.Message}");
                        content.AppendLine();
                    }
                }

                return content.ToString();
                }
            }
            catch (SqlException sqlEx)
            {
                // If database doesn't exist, return error message
                if (sqlEx.Number == 4060 || sqlEx.Message.Contains("Cannot open database"))
                {
                    _logger.LogWarning("SQL Server database does not exist yet");
                    return "=== SQL Server Database Content ===\nDatabase does not exist yet. Please create the database first.\n";
                }
                
                _logger.LogError(sqlEx, "Error parsing SQL Server database");
                throw;
            }
        }

        private async Task<List<string>> GetSqlServerTableNamesAsync(string connectionString)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    return await GetSqlServerTableNamesInternalAsync(connection);
                }
            }
            catch (SqlException sqlEx)
            {
                // If database doesn't exist, return empty list
                if (sqlEx.Number == 4060 || sqlEx.Message.Contains("Cannot open database"))
                {
                    _logger.LogInformation($"Database does not exist yet, returning empty table list");
                    return new List<string>();
                }
                
                // Re-throw other SQL errors
                _logger.LogError(sqlEx, "Error getting SQL Server table names");
                throw;
            }
        }

        private async Task<List<string>> GetSqlServerTableNamesInternalAsync(SqlConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_TYPE = 'BASE TABLE' 
                    AND TABLE_SCHEMA != 'sys'
                    ORDER BY TABLE_NAME";

                var tables = new List<string>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }

                return tables;
            }
        }

        private async Task<string> GetSqlServerTableSchemaAsync(string connectionString, string tableName)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    return await GetSqlServerTableSchemaInternalAsync(connection, tableName);
                }
            }
            catch (SqlException sqlEx)
            {
                // If database doesn't exist, return error message
                if (sqlEx.Number == 4060 || sqlEx.Message.Contains("Cannot open database"))
                {
                    _logger.LogWarning($"SQL Server database does not exist yet for table {tableName}");
                    return $"-- Table: {tableName}\n-- Database does not exist yet\n";
                }
                
                _logger.LogError(sqlEx, $"Error getting SQL Server table schema for {tableName}");
                throw;
            }
        }

        private async Task<string> GetSqlServerTableSchemaInternalAsync(SqlConnection connection, string tableName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT 
                        COLUMN_NAME,
                        DATA_TYPE,
                        IS_NULLABLE,
                        COLUMN_DEFAULT
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = @tableName
                    ORDER BY ORDINAL_POSITION";

                command.Parameters.AddWithValue("@tableName", tableName);

                var schema = new StringBuilder();
                schema.AppendLine($"Table: {tableName}");
                schema.AppendLine("Columns:");

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var columnName = reader.GetString(0);
                        var dataType = reader.GetString(1);
                        var isNullable = reader.GetString(2) == "YES";
                        var columnDefault = reader.IsDBNull(3) ? null : reader.GetString(3);

                        schema.Append($"  - {columnName} ({dataType})");
                        if (!isNullable) schema.Append(" NOT NULL");
                        if (!string.IsNullOrEmpty(columnDefault)) schema.Append($" DEFAULT {columnDefault}");
                        schema.AppendLine();
                    }
                }

                return schema.ToString();
            }
        }

        private async Task<string> GetSqlServerTableDataInternalAsync(SqlConnection connection, string tableName, DatabaseConfig config)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT TOP ({config.MaxRowsPerTable}) * FROM [{tableName}]";
                command.CommandTimeout = config.QueryTimeoutSeconds;

                return await GetTableDataInternalAsync(command, config);
            }
        }

        private async Task<string> ExecuteSqlServerQueryAsync(string connectionString, string query, int maxRows)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = query;
                        command.CommandTimeout = DefaultQueryTimeout;

                        return await ExecuteQueryInternalAsync(command, query, maxRows);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                // If database doesn't exist, return error message
                if (sqlEx.Number == 4060 || sqlEx.Message.Contains("Cannot open database"))
                {
                    _logger.LogWarning("SQL Server database does not exist yet for query execution");
                    return "Error: Database does not exist yet. Please create the database first.\n";
                }
                
                _logger.LogError(sqlEx, "Error executing SQL Server query");
                throw;
            }
        }

        private async Task<bool> ValidateSqlServerConnectionAsync(string connectionString)
        {
            try
            {
                // Try to connect - if database doesn't exist, try master
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    return connection.State == System.Data.ConnectionState.Open;
                }
            }
            catch (SqlException sqlEx)
            {
                // Check if error is related to database not existing (error codes: 4060, 18456)
                if (sqlEx.Number == 4060 || sqlEx.Message.Contains("Cannot open database"))
                {
                    // Database doesn't exist, try connecting to master to verify server is accessible
                    try
                    {
                        var builder = new SqlConnectionStringBuilder(connectionString);
                        var targetDatabase = builder.InitialCatalog;
                        builder.InitialCatalog = "master";
                        
                        using (var masterConnection = new SqlConnection(builder.ConnectionString))
                        {
                            await masterConnection.OpenAsync();
                            _logger.LogInformation($"SQL Server is accessible but database '{targetDatabase}' does not exist yet. This is expected for first-time setup.");
                            return true; // Server is accessible, database can be created
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "SQL Server validation failed - server not accessible");
                        return false;
                    }
                }
                else
                {
                    // Other SQL errors
                    _logger.LogWarning(sqlEx, $"SQL Server connection validation failed with error {sqlEx.Number}: {sqlEx.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SQL Server connection validation failed");
                return false;
            }
        }

        #endregion

        #region MySQL Implementation

        private async Task<string> ParseMySqlDatabaseAsync(string connectionString, DatabaseConfig config)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var content = new StringBuilder();
                content.AppendLine("=== MySQL Database Content ===");
                content.AppendLine($"Database: {connection.Database}");
                content.AppendLine($"Server: {connection.DataSource}");
                content.AppendLine($"Version: {connection.ServerVersion}");
                content.AppendLine();

                var allTables = await GetMySqlTableNamesInternalAsync(connection);
                var tablesToProcess = FilterTables(allTables, config);

                _logger.LogInformation("Processing {TableCount} tables from MySQL database", tablesToProcess.Count);

                foreach (var tableName in tablesToProcess)
                {
                    try
                    {
                        content.AppendLine($"--- Table: {tableName} ---");

                        if (config.IncludeSchema)
                        {
                            var schema = await GetMySqlTableSchemaInternalAsync(connection, tableName);
                            content.AppendLine(schema);
                        }

                        var tableData = await GetMySqlTableDataInternalAsync(connection, tableName, config);
                        content.AppendLine(tableData);
                        content.AppendLine();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing table: {TableName}", tableName);
                        content.AppendLine($"Error processing table {tableName}: {ex.Message}");
                        content.AppendLine();
                    }
                }

                return content.ToString();
            }
        }

        private async Task<List<string>> GetMySqlTableNamesAsync(string connectionString)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                return await GetMySqlTableNamesInternalAsync(connection);
            }
        }

        private async Task<List<string>> GetMySqlTableNamesInternalAsync(MySqlConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_TYPE = 'BASE TABLE'
                    ORDER BY TABLE_NAME";

                var tables = new List<string>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }

                return tables;
            }
        }

        private async Task<string> GetMySqlTableSchemaAsync(string connectionString, string tableName)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                return await GetMySqlTableSchemaInternalAsync(connection, tableName);
            }
        }

        private async Task<string> GetMySqlTableSchemaInternalAsync(MySqlConnection connection, string tableName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT 
                        COLUMN_NAME,
                        DATA_TYPE,
                        IS_NULLABLE,
                        COLUMN_DEFAULT,
                        COLUMN_KEY,
                        EXTRA
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = @tableName
                    ORDER BY ORDINAL_POSITION";

                command.Parameters.AddWithValue("@tableName", tableName);

                var schema = new StringBuilder();
                schema.AppendLine($"Table: {tableName}");
                schema.AppendLine("Columns:");

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var columnName = reader.GetString(0);
                        var dataType = reader.GetString(1);
                        var isNullable = reader.GetString(2) == "YES";
                        var columnDefault = reader.IsDBNull(3) ? null : reader.GetString(3);
                        var columnKey = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                        var extra = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);

                        schema.Append($"  - {columnName} ({dataType})");
                        if (!isNullable) schema.Append(" NOT NULL");
                        if (!string.IsNullOrEmpty(columnDefault)) schema.Append($" DEFAULT {columnDefault}");
                        if (columnKey == "PRI") schema.Append(" PRIMARY KEY");
                        if (!string.IsNullOrEmpty(extra) && extra.Contains("auto_increment")) schema.Append(" AUTO_INCREMENT");
                        schema.AppendLine();
                    }
                }

                return schema.ToString();
            }
        }

        private async Task<string> GetMySqlTableDataInternalAsync(MySqlConnection connection, string tableName, DatabaseConfig config)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT * FROM `{tableName}` LIMIT {config.MaxRowsPerTable}";
                command.CommandTimeout = config.QueryTimeoutSeconds;

                return await GetTableDataInternalAsync(command, config);
            }
        }

        private async Task<string> ExecuteMySqlQueryAsync(string connectionString, string query, int maxRows)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.CommandTimeout = DefaultQueryTimeout;

                    return await ExecuteQueryInternalAsync(command, query, maxRows);
                }
            }
        }

        private async Task<bool> ValidateMySqlConnectionAsync(string connectionString)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                return connection.State == System.Data.ConnectionState.Open;
            }
        }

        #endregion

        #region PostgreSQL Implementation

        private async Task<string> ParsePostgreSqlDatabaseAsync(string connectionString, DatabaseConfig config)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var content = new StringBuilder();
                content.AppendLine("=== PostgreSQL Database Content ===");
                content.AppendLine($"Database: {connection.Database}");
                content.AppendLine($"Server: {connection.Host}:{connection.Port}");
                content.AppendLine();

                var allTables = await GetPostgreSqlTableNamesInternalAsync(connection);
                var tablesToProcess = FilterTables(allTables, config);

                _logger.LogInformation("Processing {TableCount} tables from PostgreSQL database", tablesToProcess.Count);

                foreach (var tableName in tablesToProcess)
                {
                    try
                    {
                        content.AppendLine($"--- Table: {tableName} ---");

                        if (config.IncludeSchema)
                        {
                            var schema = await GetPostgreSqlTableSchemaInternalAsync(connection, tableName);
                            content.AppendLine(schema);
                        }

                        var tableData = await GetPostgreSqlTableDataInternalAsync(connection, tableName, config);
                        content.AppendLine(tableData);
                        content.AppendLine();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing table: {TableName}", tableName);
                        content.AppendLine($"Error processing table {tableName}: {ex.Message}");
                        content.AppendLine();
                    }
                }

                return content.ToString();
            }
        }

        private async Task<List<string>> GetPostgreSqlTableNamesAsync(string connectionString)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                return await GetPostgreSqlTableNamesInternalAsync(connection);
            }
        }

        private async Task<List<string>> GetPostgreSqlTableNamesInternalAsync(NpgsqlConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT tablename 
                    FROM pg_tables 
                    WHERE schemaname = 'public'
                    ORDER BY tablename";

                var tables = new List<string>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }

                return tables;
            }
        }

        private async Task<string> GetPostgreSqlTableSchemaAsync(string connectionString, string tableName)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                return await GetPostgreSqlTableSchemaInternalAsync(connection, tableName);
            }
        }

        private async Task<string> GetPostgreSqlTableSchemaInternalAsync(NpgsqlConnection connection, string tableName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT 
                        column_name,
                        data_type,
                        is_nullable,
                        column_default
                    FROM information_schema.columns 
                    WHERE table_schema = 'public' 
                    AND table_name = @tableName
                    ORDER BY ordinal_position";

                command.Parameters.AddWithValue("@tableName", tableName);

                var schema = new StringBuilder();
                schema.AppendLine($"Table: {tableName}");
                schema.AppendLine("Columns:");

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var columnName = reader.GetString(0);
                        var dataType = reader.GetString(1);
                        var isNullable = reader.GetString(2) == "YES";
                        var columnDefault = reader.IsDBNull(3) ? null : reader.GetString(3);

                        schema.Append($"  - {columnName} ({dataType})");
                        if (!isNullable) schema.Append(" NOT NULL");
                        if (!string.IsNullOrEmpty(columnDefault)) 
                        {
                            schema.Append($" DEFAULT {columnDefault}");
                            if (columnDefault.Contains("nextval"))
                                schema.Append(" [SERIAL]");
                        }
                        schema.AppendLine();
                    }
                }

                return schema.ToString();
            }
        }

        private async Task<string> GetPostgreSqlTableDataInternalAsync(NpgsqlConnection connection, string tableName, DatabaseConfig config)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT * FROM \"{tableName}\" LIMIT {config.MaxRowsPerTable}";
                command.CommandTimeout = config.QueryTimeoutSeconds;

                return await GetTableDataInternalAsync(command, config);
            }
        }

        private async Task<string> ExecutePostgreSqlQueryAsync(string connectionString, string query, int maxRows)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.CommandTimeout = DefaultQueryTimeout;

                    return await ExecuteQueryInternalAsync(command, query, maxRows);
                }
            }
        }

        private async Task<bool> ValidatePostgreSqlConnectionAsync(string connectionString)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                return connection.State == System.Data.ConnectionState.Open;
            }
        }

        #endregion

        #region Common Helper Methods

        /// <summary>
        /// Filters tables based on configuration
        /// </summary>
        private static List<string> FilterTables(List<string> allTables, DatabaseConfig config)
        {
            var result = allTables.ToList();

            if (config.IncludedTables.Any())
            {
                result = result.Where(t => config.IncludedTables.Contains(t, StringComparer.OrdinalIgnoreCase)).ToList();
            }

            if (config.ExcludedTables.Any())
            {
                result = result.Where(t => !config.ExcludedTables.Contains(t, StringComparer.OrdinalIgnoreCase)).ToList();
            }

            return result;
        }

        /// <summary>
        /// Checks if a column contains sensitive data
        /// </summary>
        private static bool IsSensitiveColumn(string columnName, List<string> sensitiveColumns)
        {
            var columnsToCheck = sensitiveColumns?.Any() == true ? sensitiveColumns : DefaultSensitiveColumns.ToList();
            
            return columnsToCheck.Any(sensitive => 
                columnName.IndexOf(sensitive, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Internal method to get table data
        /// </summary>
        private async Task<string> GetTableDataInternalAsync(DbCommand command, DatabaseConfig config)
        {
            var result = new StringBuilder();
            using (var reader = await command.ExecuteReaderAsync())
            {
                var columnCount = reader.FieldCount;
                var columnNames = new string[columnCount];
                var isSensitiveColumn = new bool[columnCount];

                for (int i = 0; i < columnCount; i++)
                {
                    columnNames[i] = reader.GetName(i);
                    isSensitiveColumn[i] = config.SanitizeSensitiveData && 
                        IsSensitiveColumn(columnNames[i], config.SensitiveColumns);
                }

                result.AppendLine(string.Join("\t", columnNames));

                var rowCount = 0;
                while (await reader.ReadAsync() && rowCount < config.MaxRowsPerTable)
                {
                    var values = new string[columnCount];
                    for (int i = 0; i < columnCount; i++)
                    {
                        if (reader.IsDBNull(i))
                        {
                            values[i] = "NULL";
                        }
                        else if (isSensitiveColumn[i])
                        {
                            values[i] = SensitiveDataPlaceholder;
                        }
                        else
                        {
                            values[i] = reader.GetValue(i).ToString();
                        }
                    }
                    result.AppendLine(string.Join("\t", values));
                    rowCount++;
                }

                result.AppendLine($"Rows: {rowCount}");
                return result.ToString();
            }
        }

        /// <summary>
        /// Internal method to execute query and format results
        /// </summary>
        private async Task<string> ExecuteQueryInternalAsync(DbCommand command, string query, int maxRows)
        {
            var result = new StringBuilder();
            result.AppendLine($"=== Query Results ===");
            result.AppendLine($"Query: {query}");
            result.AppendLine();

            using (var reader = await command.ExecuteReaderAsync())
            {
                var rowCount = 0;
                var columnCount = reader.FieldCount;
                var headers = new string[columnCount];
                
                for (int i = 0; i < columnCount; i++)
                {
                    headers[i] = reader.GetName(i);
                }
                result.AppendLine(string.Join("\t", headers));

                while (await reader.ReadAsync() && rowCount < maxRows)
                {
                    var values = new string[columnCount];
                    for (int i = 0; i < columnCount; i++)
                    {
                        values[i] = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString();
                    }
                    result.AppendLine(string.Join("\t", values));
                    rowCount++;
                }

                result.AppendLine($"\nRows extracted: {rowCount}");
                _logger.LogInformation("Custom query executed successfully, rows: {RowCount}", rowCount);
                return result.ToString();
            }
        }

        #endregion
    }
}
