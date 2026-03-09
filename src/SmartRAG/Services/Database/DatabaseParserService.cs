using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Npgsql;

namespace SmartRAG.Services.Database;


/// <summary>
/// Service for parsing database files and live database connections
/// </summary>
public class DatabaseParserService : IDatabaseParserService
{
    private const int DefaultMaxRows = 1000;
    private const int DefaultQueryTimeout = 30;
    private const string SensitiveDataPlaceholder = "[SENSITIVE_DATA]";

    private static readonly string[] DatabaseFileExtensions = { ".db", ".sqlite", ".sqlite3", ".db3" };

    private readonly ILogger<DatabaseParserService> _logger;

    /// <summary>
    /// Initializes a new instance of the DatabaseParserService
    /// </summary>
    public DatabaseParserService(
        ILogger<DatabaseParserService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses a database file (SQLite) and extracts content for RAG processing
    /// </summary>
    public async Task<string> ParseDatabaseFileAsync(Stream dbStream, string fileName, CancellationToken cancellationToken = default)
    {
        if (dbStream == null) throw new ArgumentNullException(nameof(dbStream));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));

        DatabaseLogMessages.LogDatabaseFileParsingStarted(_logger, fileName, null);

        var tempPath = Path.GetTempFileName();

        try
        {
            await using (var fileStream = File.Create(tempPath))
            {
                await dbStream.CopyToAsync(fileStream, cancellationToken);
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

            var result = await ParseSQLiteDatabaseAsync(connectionString, config, cancellationToken);

            DatabaseLogMessages.LogDatabaseFileParsingCompleted(_logger, fileName, null);
            return result;
        }
        catch (Exception ex)
        {
            DatabaseLogMessages.LogDatabaseFileParsingFailed(_logger, fileName, ex);
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
                DatabaseLogMessages.LogTempFileDeleteFailed(_logger, tempPath, ex);
            }
        }
    }

    /// <summary>
    /// [DB Query] Connects to a live database and extracts content based on configuration
    /// </summary>
    public async Task<string> ParseDatabaseConnectionAsync(string connectionString, DatabaseConfig config, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
        if (config == null) throw new ArgumentNullException(nameof(config));

        DatabaseLogMessages.LogDatabaseConnectionParsingStarted(_logger, config.Type.ToString(), null);

        try
        {
            return config.Type switch
            {
                DatabaseType.SQLite => await ParseSQLiteDatabaseAsync(connectionString, config, cancellationToken),
                DatabaseType.SqlServer =>
                    await ParseSqlServerDatabaseAsync(connectionString, config, cancellationToken),
                DatabaseType.MySQL => await ParseMySqlDatabaseAsync(connectionString, config, cancellationToken),
                DatabaseType.PostgreSQL => await ParsePostgreSqlDatabaseAsync(connectionString, config,
                    cancellationToken),
                _ => throw new NotSupportedException($"Database type {config.Type} is not supported")
            };
        }
        catch (Exception ex)
        {
            DatabaseLogMessages.LogDatabaseConnectionParsingFailed(_logger, config.Type.ToString(), ex);
            throw;
        }
    }

    /// <summary>
    /// Gets list of table names from the database
    /// </summary>
    public async Task<List<string>> GetTableNamesAsync(string connectionString, DatabaseType databaseType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

        return databaseType switch
        {
            DatabaseType.SQLite => await GetSQLiteTableNamesAsync(connectionString, cancellationToken),
            DatabaseType.SqlServer => await GetSqlServerTableNamesAsync(connectionString, cancellationToken),
            DatabaseType.MySQL => await GetMySqlTableNamesAsync(connectionString, cancellationToken),
            DatabaseType.PostgreSQL => await GetPostgreSqlTableNamesAsync(connectionString, cancellationToken),
            _ => throw new NotSupportedException($"Database type {databaseType} is not supported")
        };
    }


    /// <summary>
    /// [DB Query] Executes a custom SQL query and returns results
    /// </summary>
    public async Task<string> ExecuteQueryAsync(string connectionString, string query, DatabaseType databaseType, int maxRows = DefaultMaxRows, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
        if (string.IsNullOrWhiteSpace(query)) throw new ArgumentNullException(nameof(query));

        var preprocessed = StripCommentsAndTakeFirstStatement(query);
        var sanitizedQuery = ValidateAndSanitizeQuery(preprocessed);

        return databaseType switch
        {
            DatabaseType.SQLite => await ExecuteSQLiteQueryInternalAsync(connectionString, sanitizedQuery, maxRows,
                cancellationToken),
            DatabaseType.SqlServer => await ExecuteSqlServerQueryInternalAsync(connectionString, sanitizedQuery, maxRows,
                cancellationToken),
            DatabaseType.MySQL => await ExecuteMySqlQueryInternalAsync(connectionString, sanitizedQuery, maxRows,
                cancellationToken),
            DatabaseType.PostgreSQL => await ExecutePostgreSqlQueryInternalAsync(connectionString, sanitizedQuery, maxRows,
                cancellationToken),
            _ => throw new NotSupportedException($"Database type {databaseType} is not supported")
        };
    }

    private static string StripCommentsAndTakeFirstStatement(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return query;
        var sb = new System.Text.StringBuilder();
        var i = 0;
        while (i < query.Length)
        {
            if (i + 1 < query.Length && query[i] == '-' && query[i + 1] == '-')
            {
                while (i < query.Length && query[i] != '\n') i++;
                continue;
            }
            if (i + 1 < query.Length && query[i] == '/' && query[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < query.Length && (query[i] != '*' || query[i + 1] != '/')) i++;
                if (i + 1 < query.Length) i += 2;
                continue;
            }
            sb.Append(query[i++]);
        }
        var withoutComments = sb.ToString();
        var semicolonIdx = withoutComments.IndexOf(';');
        if (semicolonIdx >= 0)
        {
            var rest = withoutComments[(semicolonIdx + 1)..].Trim();
            if (rest.Length > 0 && char.IsLetter(rest[0]))
                withoutComments = withoutComments[..semicolonIdx].Trim();
        }
        return withoutComments.Trim();
    }

    private static string ValidateAndSanitizeQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or empty", nameof(query));

        var trimmed = query.Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("Query cannot be empty or whitespace", nameof(query));

        var upper = trimmed.ToUpperInvariant();

        if (!upper.StartsWith("SELECT ", StringComparison.Ordinal) &&
            !upper.StartsWith("SELECT\n", StringComparison.Ordinal) &&
            !upper.StartsWith("SELECT\t", StringComparison.Ordinal) &&
            !upper.Equals("SELECT", StringComparison.Ordinal))
        {
            throw new ArgumentException("Only single SELECT statements are allowed", nameof(query));
        }

        var dangerousKeywords = new[]
        {
            "INSERT", "UPDATE", "DELETE", "DROP", "TRUNCATE", "ALTER",
            "CREATE", "REPLACE", "EXEC", "EXECUTE", "MERGE",
            "GRANT", "REVOKE", "SP_", "XP_"
        };

        foreach (var keyword in dangerousKeywords)
        {
            var pattern = keyword.EndsWith("_", StringComparison.Ordinal)
                ? $@"\b{Regex.Escape(keyword)}"
                : $@"\b{Regex.Escape(keyword)}\b";

            if (Regex.IsMatch(upper, pattern, RegexOptions.IgnoreCase))
            {
                throw new ArgumentException($"Query contains dangerous keyword: {keyword}", nameof(query));
            }
        }

        if (upper.Contains(";--", StringComparison.Ordinal) ||
            upper.Contains(";/*", StringComparison.Ordinal))
        {
            throw new ArgumentException("Query contains potentially dangerous SQL comment patterns", nameof(query));
        }

        var semicolonIndex = trimmed.IndexOf(';');
        if (semicolonIndex >= 0)
        {
            var after = trimmed[(semicolonIndex + 1)..].Trim();
            if (after.Length > 0)
            {
                throw new ArgumentException("Only a single SQL statement is allowed", nameof(query));
            }
            trimmed = trimmed[..semicolonIndex].TrimEnd();
        }

        if (upper.Contains(" UNION ", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Query contains potentially dangerous SQL pattern: UNION", nameof(query));
        }

        return trimmed;
    }

    private static string SanitizeTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

        var sanitized = tableName.Trim();

        if (sanitized.Contains(";", StringComparison.Ordinal) ||
            sanitized.Contains("--", StringComparison.Ordinal) ||
            sanitized.Contains("/*", StringComparison.Ordinal) ||
            sanitized.Contains("*/", StringComparison.Ordinal) ||
            sanitized.Contains("'", StringComparison.Ordinal) ||
            sanitized.Contains("\"", StringComparison.Ordinal) ||
            sanitized.Contains("\\", StringComparison.Ordinal) ||
            sanitized.Contains("/", StringComparison.Ordinal))
        {
            throw new ArgumentException("Table name contains invalid characters", nameof(tableName));
        }

        return sanitized;
    }

    /// <summary>
    /// Validates database connection
    /// </summary>
    public async Task<bool> ValidateConnectionAsync(string connectionString, DatabaseType databaseType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return false;

        try
        {
            return databaseType switch
            {
                DatabaseType.SQLite => await ValidateSQLiteConnectionAsync(connectionString, cancellationToken),
                DatabaseType.SqlServer => await ValidateSqlServerConnectionAsync(connectionString, cancellationToken),
                DatabaseType.MySQL => await ValidateMySqlConnectionAsync(connectionString, cancellationToken),
                DatabaseType.PostgreSQL => await ValidatePostgreSqlConnectionAsync(connectionString, cancellationToken),
                _ => false
            };
        }
        catch (Exception ex)
        {
            DatabaseLogMessages.LogDatabaseConnectionValidationFailed(_logger, databaseType.ToString(), ex);
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

    private async Task<string> ParseSQLiteDatabaseAsync(string connectionString, DatabaseConfig config, CancellationToken cancellationToken = default)
    {
        var sanitizedConnectionString = ValidateAndSanitizeSQLiteConnectionString(connectionString);
        await using var connection = new SqliteConnection(sanitizedConnectionString);
        await connection.OpenAsync(cancellationToken);

        var content = new StringBuilder();
        content.AppendLine("=== SQLite Database Content ===");
        content.AppendLine($"Database: {connection.DataSource}");
        content.AppendLine();

        var allTables = await GetSQLiteTableNamesInternalAsync(connection, cancellationToken);
        var tablesToProcess = FilterTables(allTables, config);

        DatabaseLogMessages.LogProcessingTablesSqlite(_logger, tablesToProcess.Count, null);

        foreach (var tableName in tablesToProcess)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                content.AppendLine($"--- Table: {tableName} ---");

                if (config.IncludeSchema)
                {
                    var schema = await GetSQLiteTableSchemaInternalAsync(connection, tableName, cancellationToken);
                    content.AppendLine(schema);
                }

                var tableData = await GetSQLiteTableDataInternalAsync(connection, tableName, config, cancellationToken);
                content.AppendLine(tableData);
                content.AppendLine();
            }
            catch (Exception ex)
            {
                DatabaseLogMessages.LogErrorProcessingTable(_logger, tableName, ex);
                content.AppendLine($"Error processing table {tableName}: {ex.Message}");
                content.AppendLine();
            }
        }

        return content.ToString();
    }

    private async Task<List<string>> GetSQLiteTableNamesAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var sanitizedConnectionString = ValidateAndSanitizeSQLiteConnectionString(connectionString);
        await using var connection = new SqliteConnection(sanitizedConnectionString);
        await connection.OpenAsync(cancellationToken);
        return await GetSQLiteTableNamesInternalAsync(connection, cancellationToken);
    }

    private async Task<List<string>> GetSQLiteTableNamesInternalAsync(SqliteConnection connection, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private async Task<string> GetSQLiteTableSchemaInternalAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        var sanitizedTableName = SanitizeTableName(tableName);
        command.CommandText = $"PRAGMA table_info({sanitizedTableName})";

        var schema = new StringBuilder();
        schema.AppendLine($"Table: {tableName}");
        schema.AppendLine("Columns:");

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
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

    private async Task<string> GetSQLiteTableDataInternalAsync(SqliteConnection connection, string tableName, DatabaseConfig config, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        var sanitizedTableName = SanitizeTableName(tableName);
        command.CommandText = $"SELECT * FROM [{sanitizedTableName}] LIMIT {config.MaxRowsPerTable}";
        command.CommandTimeout = config.QueryTimeoutSeconds;

        return await GetTableDataInternalAsync(command, config, cancellationToken);
    }

    private async Task<string> ExecuteSQLiteQueryInternalAsync(string connectionString, string sanitizedQuery, int maxRows, CancellationToken cancellationToken = default)
    {
        var sanitizedConnectionString = ValidateAndSanitizeSQLiteConnectionString(connectionString);
        await using var connection = new SqliteConnection(sanitizedConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        // codeql[cs/sql-injection]: sanitizedQuery is validated by ValidateAndSanitizeQuery and restricted to single read-only SELECT statements.
        command.CommandText = sanitizedQuery;
        command.CommandTimeout = DefaultQueryTimeout;

        return await ExecuteQueryInternalAsync(command, maxRows, cancellationToken);
    }

    private async Task<bool> ValidateSQLiteConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var sanitizedConnectionString = ValidateAndSanitizeSQLiteConnectionString(connectionString);
        await using var connection = new SqliteConnection(sanitizedConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection.State == ConnectionState.Open;
    }

    private async Task<string> ParseSqlServerDatabaseAsync(string connectionString, DatabaseConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            var sanitizedConnectionString = ValidateAndSanitizeSqlServerConnectionString(connectionString);
            await using var connection = new SqlConnection(sanitizedConnectionString);
            await connection.OpenAsync(cancellationToken);

            var content = new StringBuilder();
            content.AppendLine("=== SQL Server Database Content ===");
            content.AppendLine($"Database: {connection.Database}");
            content.AppendLine($"Server: {connection.DataSource}");
            content.AppendLine();

            var allTables = await GetSqlServerTableNamesInternalAsync(connection, cancellationToken);
            var tablesToProcess = FilterTables(allTables, config);

            DatabaseLogMessages.LogProcessingTablesSqlServer(_logger, tablesToProcess.Count, null);

            foreach (var tableName in tablesToProcess)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    content.AppendLine($"--- Table: {tableName} ---");

                    if (config.IncludeSchema)
                    {
                        var schema = await GetSqlServerTableSchemaInternalAsync(connection, tableName, cancellationToken);
                        content.AppendLine(schema);
                    }

                    var tableData = await GetSqlServerTableDataInternalAsync(connection, tableName, config, cancellationToken);
                    content.AppendLine(tableData);
                    content.AppendLine();
                }
                catch (Exception ex)
                {
                    DatabaseLogMessages.LogErrorProcessingTable(_logger, tableName, ex);
                    content.AppendLine($"Error processing table {tableName}: {ex.Message}");
                    content.AppendLine();
                }
            }

            return content.ToString();
        }
        catch (SqlException sqlEx)
        {
            if (sqlEx.Number == 4060 || sqlEx.Message.Contains("Cannot open database"))
            {
                DatabaseLogMessages.LogSqlServerDatabaseNotExistYet(_logger, null);
                return "=== SQL Server Database Content ===\nDatabase does not exist yet. Please create the database first.\n";
            }

            DatabaseLogMessages.LogSqlServerParsingFailed(_logger, sqlEx);
            throw;
        }
    }

    private async Task<List<string>> GetSqlServerTableNamesAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            var sanitizedConnectionString = ValidateAndSanitizeSqlServerConnectionString(connectionString);
            await using var connection = new SqlConnection(sanitizedConnectionString);
            await connection.OpenAsync(cancellationToken);
            return await GetSqlServerTableNamesInternalAsync(connection, cancellationToken);
        }
        catch (SqlException sqlEx)
        {
            if (sqlEx.Number == 4060 || sqlEx.Message.Contains("Cannot open database"))
            {
                DatabaseLogMessages.LogDatabaseNotExistEmptyTableList(_logger, null);
                return new List<string>();
            }

            DatabaseLogMessages.LogSqlServerTableNamesFailed(_logger, sqlEx);
            throw;
        }
    }

    private async Task<List<string>> GetSqlServerTableNamesInternalAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
                SELECT TABLE_SCHEMA + '.' + TABLE_NAME AS FullTableName
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                AND TABLE_SCHEMA != 'sys'
                ORDER BY TABLE_SCHEMA, TABLE_NAME";

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private async Task<string> GetSqlServerTableSchemaInternalAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken = default)
    {
        var sanitizedTableName = SanitizeTableName(tableName);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
                SELECT
                    COLUMN_NAME,
                    DATA_TYPE,
                    IS_NULLABLE,
                    COLUMN_DEFAULT
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @tableName
                ORDER BY ORDINAL_POSITION";

        command.Parameters.AddWithValue("@tableName", sanitizedTableName);

        var schema = new StringBuilder();
        schema.AppendLine($"Table: {sanitizedTableName}");
        schema.AppendLine("Columns:");

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
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

    private async Task<string> GetSqlServerTableDataInternalAsync(SqlConnection connection, string tableName, DatabaseConfig config, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        var sanitizedTableName = SanitizeTableName(tableName);
        command.CommandText = $"SELECT TOP ({config.MaxRowsPerTable}) * FROM [{sanitizedTableName}]";
        command.CommandTimeout = config.QueryTimeoutSeconds;

        return await GetTableDataInternalAsync(command, config, cancellationToken);
    }

    private async Task<string> ExecuteSqlServerQueryInternalAsync(string connectionString, string sanitizedQuery, int maxRows, CancellationToken cancellationToken = default)
    {
        try
        {
            var sanitizedConnectionString = ValidateAndSanitizeSqlServerConnectionString(connectionString);
            await using var connection = new SqlConnection(sanitizedConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            // codeql[cs/sql-injection]: sanitizedQuery is validated by ValidateAndSanitizeQuery and restricted to single read-only SELECT statements.
            command.CommandText = sanitizedQuery;
            command.CommandTimeout = DefaultQueryTimeout;

            return await ExecuteQueryInternalAsync(command, maxRows, cancellationToken);
        }
        catch (SqlException sqlEx)
        {
            if (sqlEx.Number == 4060 || sqlEx.Message.Contains("Cannot open database"))
            {
                DatabaseLogMessages.LogSqlServerQueryExecutionNotExist(_logger, null);
                return "Error: Database does not exist yet. Please create the database first.\n";
            }

            DatabaseLogMessages.LogSqlServerQueryExecutionFailed(_logger, sqlEx);
            throw;
        }
    }

    private async Task<bool> ValidateSqlServerConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            var sanitizedConnectionString = ValidateAndSanitizeSqlServerConnectionString(connectionString);
            await using var connection = new SqlConnection(sanitizedConnectionString);
            await connection.OpenAsync(cancellationToken);
            return connection.State == ConnectionState.Open;
        }
        catch (SqlException sqlEx)
        {
            if (sqlEx.Number == 4060 || sqlEx.Message.Contains("Cannot open database"))
            {
                try
                {
                    var sanitizedConnectionString = ValidateAndSanitizeSqlServerConnectionString(connectionString);
                    var builder = new SqlConnectionStringBuilder(sanitizedConnectionString);
                    var targetDatabase = builder.InitialCatalog;
                    builder.InitialCatalog = "master";

                    await using var masterConnection = new SqlConnection(builder.ConnectionString);
                    await masterConnection.OpenAsync(cancellationToken);
                    DatabaseLogMessages.LogSqlServerAccessibleDbNotExist(_logger, targetDatabase, null);
                    return true;
                }
                catch (Exception ex)
                {
                    DatabaseLogMessages.LogSqlServerValidationNotAccessible(_logger, ex);
                    return false;
                }
            }

            DatabaseLogMessages.LogSqlServerConnectionValidationFailed(_logger, sqlEx.Number, sqlEx.Message, sqlEx);
            return false;
        }
        catch (Exception ex)
        {
            DatabaseLogMessages.LogSqlServerConnectionValidationFailedGeneric(_logger, ex);
            return false;
        }
    }

    private async Task<string> ParseMySqlDatabaseAsync(string connectionString, DatabaseConfig config, CancellationToken cancellationToken = default)
    {
        var sanitizedConnectionString = ValidateAndSanitizeMySqlConnectionString(connectionString);
        await using var connection = new MySqlConnection(sanitizedConnectionString);
        await connection.OpenAsync(cancellationToken);

        var content = new StringBuilder();
        content.AppendLine("=== MySQL Database Content ===");
        content.AppendLine($"Database: {connection.Database}");
        content.AppendLine($"Server: {connection.DataSource}");
        content.AppendLine($"Version: {connection.ServerVersion}");
        content.AppendLine();

        var allTables = await GetMySqlTableNamesInternalAsync(connection, cancellationToken);
        var tablesToProcess = FilterTables(allTables, config);

        DatabaseLogMessages.LogProcessingTablesMySql(_logger, tablesToProcess.Count, null);

        foreach (var tableName in tablesToProcess)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                content.AppendLine($"--- Table: {tableName} ---");

                if (config.IncludeSchema)
                {
                    var schema = await GetMySqlTableSchemaInternalAsync(connection, tableName, cancellationToken);
                    content.AppendLine(schema);
                }

                var tableData = await GetMySqlTableDataInternalAsync(connection, tableName, config, cancellationToken);
                content.AppendLine(tableData);
                content.AppendLine();
            }
            catch (Exception ex)
            {
                DatabaseLogMessages.LogErrorProcessingTable(_logger, tableName, ex);
                content.AppendLine($"Error processing table {tableName}: {ex.Message}");
                content.AppendLine();
            }
        }

        return content.ToString();
    }

    private async Task<List<string>> GetMySqlTableNamesAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var sanitizedConnectionString = ValidateAndSanitizeMySqlConnectionString(connectionString);
        await using var connection = new MySqlConnection(sanitizedConnectionString);
        await connection.OpenAsync(cancellationToken);
        return await GetMySqlTableNamesInternalAsync(connection, cancellationToken);
    }

    private async Task<List<string>> GetMySqlTableNamesInternalAsync(MySqlConnection connection, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
                SELECT TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = DATABASE()
                AND TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_NAME";

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static async Task<string> GetMySqlTableSchemaInternalAsync(MySqlConnection connection, string tableName, CancellationToken cancellationToken = default)
    {
        var sanitizedTableName = SanitizeTableName(tableName);
        await using var command = connection.CreateCommand();
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

        command.Parameters.AddWithValue("@tableName", sanitizedTableName);

        var schema = new StringBuilder();
        schema.AppendLine($"Table: {sanitizedTableName}");
        schema.AppendLine("Columns:");

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
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

    private async Task<string> GetMySqlTableDataInternalAsync(MySqlConnection connection, string tableName, DatabaseConfig config, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        var sanitizedTableName = SanitizeTableName(tableName);
        command.CommandText = $"SELECT * FROM `{sanitizedTableName}` LIMIT {config.MaxRowsPerTable}";
        command.CommandTimeout = config.QueryTimeoutSeconds;

        return await GetTableDataInternalAsync(command, config, cancellationToken);
    }

    private async Task<string> ExecuteMySqlQueryInternalAsync(string connectionString, string sanitizedQuery, int maxRows, CancellationToken cancellationToken = default)
    {
        var sanitizedConnectionString = ValidateAndSanitizeMySqlConnectionString(connectionString);
        await using var connection = new MySqlConnection(sanitizedConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        // codeql[cs/sql-injection]: sanitizedQuery is validated by ValidateAndSanitizeQuery and restricted to single read-only SELECT statements.
        command.CommandText = sanitizedQuery;
        command.CommandTimeout = DefaultQueryTimeout;

        return await ExecuteQueryInternalAsync(command, maxRows, cancellationToken);
    }

    private async Task<bool> ValidateMySqlConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var sanitizedConnectionString = ValidateAndSanitizeMySqlConnectionString(connectionString);
        await using var connection = new MySqlConnection(sanitizedConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection.State == ConnectionState.Open;
    }

    private async Task<string> ParsePostgreSqlDatabaseAsync(string connectionString, DatabaseConfig config, CancellationToken cancellationToken = default)
    {
        var sanitizedConnectionString = ValidateAndSanitizePostgreSqlConnectionString(connectionString);
        await using var connection = new NpgsqlConnection(sanitizedConnectionString);
        await connection.OpenAsync(cancellationToken);

        var content = new StringBuilder();
        content.AppendLine("=== PostgreSQL Database Content ===");
        content.AppendLine($"Database: {connection.Database}");
        content.AppendLine($"Server: {connection.Host}:{connection.Port}");
        content.AppendLine();

        var allTables = await GetPostgreSqlTableNamesInternalAsync(connection, cancellationToken);
        var tablesToProcess = FilterTables(allTables, config);

        DatabaseLogMessages.LogProcessingTablesPostgres(_logger, tablesToProcess.Count, null);

        foreach (var tableName in tablesToProcess)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                content.AppendLine($"--- Table: {tableName} ---");

                if (config.IncludeSchema)
                {
                    var schema = await GetPostgreSqlTableSchemaInternalAsync(connection, tableName, cancellationToken);
                    content.AppendLine(schema);
                }

                var tableData = await GetPostgreSqlTableDataInternalAsync(connection, tableName, config, cancellationToken);
                content.AppendLine(tableData);
                content.AppendLine();
            }
            catch (Exception ex)
            {
                DatabaseLogMessages.LogErrorProcessingTable(_logger, tableName, ex);
                content.AppendLine($"Error processing table {tableName}: {ex.Message}");
                content.AppendLine();
            }
        }

        return content.ToString();
    }

    private async Task<List<string>> GetPostgreSqlTableNamesAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var sanitizedConnectionString = ValidateAndSanitizePostgreSqlConnectionString(connectionString);
        await using var connection = new NpgsqlConnection(sanitizedConnectionString);
        await connection.OpenAsync(cancellationToken);
        return await GetPostgreSqlTableNamesInternalAsync(connection, cancellationToken);
    }

    private async Task<List<string>> GetPostgreSqlTableNamesInternalAsync(NpgsqlConnection connection, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
                SELECT
                    nspname || '.' || relname AS full_table_name
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE c.relkind = 'r'
                AND nspname NOT IN ('pg_catalog', 'information_schema')
                ORDER BY nspname, relname";

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var tableName = reader.GetString(0);
            tables.Add(tableName);
        }

        return tables;
    }

    private async Task<string> GetPostgreSqlTableSchemaInternalAsync(NpgsqlConnection connection, string tableName, CancellationToken cancellationToken = default)
    {
        var sanitizedTableName = SanitizeTableName(tableName);
        await using var command = connection.CreateCommand();
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

        command.Parameters.AddWithValue("@tableName", sanitizedTableName);

        var schema = new StringBuilder();
        schema.AppendLine($"Table: {sanitizedTableName}");
        schema.AppendLine("Columns:");

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
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

    private async Task<string> GetPostgreSqlTableDataInternalAsync(NpgsqlConnection connection, string tableName, DatabaseConfig config, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        var sanitizedTableName = SanitizeTableName(tableName);
        command.CommandText = $"SELECT * FROM \"{sanitizedTableName}\" LIMIT {config.MaxRowsPerTable}";
        command.CommandTimeout = config.QueryTimeoutSeconds;

        return await GetTableDataInternalAsync(command, config, cancellationToken);
    }

    private async Task<string> ExecutePostgreSqlQueryInternalAsync(string connectionString, string sanitizedQuery, int maxRows, CancellationToken cancellationToken = default)
    {
        var sanitizedConnectionString = ValidateAndSanitizePostgreSqlConnectionString(connectionString);
        await using var connection = new NpgsqlConnection(sanitizedConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        // codeql[cs/sql-injection]: sanitizedQuery is validated by ValidateAndSanitizeQuery and restricted to single read-only SELECT statements.
        command.CommandText = sanitizedQuery;
        command.CommandTimeout = DefaultQueryTimeout;

        return await ExecuteQueryInternalAsync(command, maxRows, cancellationToken);
    }

    private async Task<bool> ValidatePostgreSqlConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var sanitizedConnectionString = ValidateAndSanitizePostgreSqlConnectionString(connectionString);
        await using var connection = new NpgsqlConnection(sanitizedConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection.State == ConnectionState.Open;
    }

    /// <summary>
    /// Validates and sanitizes SQLite connection string to prevent path traversal attacks
    /// </summary>
    private string ValidateAndSanitizeSQLiteConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        try
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);

            // Validate Data Source path to prevent path traversal
            if (string.IsNullOrEmpty(builder.DataSource))
                return builder.ConnectionString;

            var dataSource = builder.DataSource;

            if (dataSource.Contains("..") || dataSource.Contains("//") || dataSource.Contains("\\\\"))
            {
                throw new ArgumentException("Invalid path in connection string: path traversal detected", nameof(connectionString));
            }

            if (!Path.IsPathRooted(dataSource))
            {
                var currentDir = Directory.GetCurrentDirectory();
                var resolvedPath = Path.Combine(currentDir, dataSource);

                // If file doesn't exist in current directory, try to find project root
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
            else
            {
                // For absolute paths, ensure they are within allowed boundaries
                var fullPath = Path.GetFullPath(dataSource);
                if (fullPath != dataSource)
                {
                    throw new ArgumentException("Invalid path in connection string: resolved path differs from provided path", nameof(connectionString));
                }
            }

            return builder.ConnectionString;
        }
        catch (ArgumentException)
        {
            throw;
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
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return string.Empty;
    }

    /// <summary>
    /// Validates and sanitizes SQL Server connection string using connection string builder
    /// </summary>
    private string ValidateAndSanitizeSqlServerConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return builder.ConnectionString;
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Invalid SQL Server connection string format: {ex.Message}", nameof(connectionString), ex);
        }
    }

    /// <summary>
    /// Validates and sanitizes MySQL connection string using connection string builder
    /// </summary>
    private static string ValidateAndSanitizeMySqlConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        try
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            return builder.ConnectionString;
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Invalid MySQL connection string format: {ex.Message}", nameof(connectionString), ex);
        }
    }

    /// <summary>
    /// Validates and sanitizes PostgreSQL connection string using connection string builder
    /// </summary>
    private static string ValidateAndSanitizePostgreSqlConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            return builder.ConnectionString;
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Invalid PostgreSQL connection string format: {ex.Message}", nameof(connectionString), ex);
        }
    }

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
        if (!sensitiveColumns.Any())
        {
            return false;
        }

        return sensitiveColumns.Any(sensitive =>
            columnName.IndexOf(sensitive, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    /// <summary>
    /// Internal method to get table data
    /// </summary>
    private async Task<string> GetTableDataInternalAsync(DbCommand command, DatabaseConfig config, CancellationToken cancellationToken = default)
    {
        var result = new StringBuilder();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var columnCount = reader.FieldCount;
        var columnNames = new string[columnCount];
        var isSensitiveColumn = new bool[columnCount];

        for (var i = 0; i < columnCount; i++)
        {
            columnNames[i] = reader.GetName(i);
            isSensitiveColumn[i] = config.SanitizeSensitiveData &&
                IsSensitiveColumn(columnNames[i], config.SensitiveColumns);
        }

        result.AppendLine(string.Join("\t", columnNames));

        var rowCount = 0;
        while (await reader.ReadAsync(cancellationToken) && rowCount < config.MaxRowsPerTable)
        {
            var values = new string[columnCount];
            for (var i = 0; i < columnCount; i++)
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

    /// <summary>
    /// Internal method to execute query and format results
    /// </summary>
    private static async Task<string> ExecuteQueryInternalAsync(DbCommand command, int maxRows, CancellationToken cancellationToken = default)
    {
        var result = new StringBuilder();
        result.AppendLine($"=== Query Results ===");
        result.AppendLine();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rowCount = 0;
        var columnCount = reader.FieldCount;
        var headers = new string[columnCount];

        for (var i = 0; i < columnCount; i++)
        {
            headers[i] = reader.GetName(i);
        }
        result.AppendLine(string.Join("\t", headers));

        while (await reader.ReadAsync(cancellationToken) && rowCount < maxRows)
        {
            var values = new string[columnCount];
            for (var i = 0; i < columnCount; i++)
            {
                values[i] = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString();
            }
            result.AppendLine(string.Join("\t", values));
            rowCount++;
        }

        result.AppendLine($"\nRows extracted: {rowCount}");
        return result.ToString();
    }
}

