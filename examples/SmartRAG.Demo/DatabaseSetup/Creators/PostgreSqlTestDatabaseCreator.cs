using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartRAG.Demo.DatabaseSetup.Interfaces;
using SmartRAG.Enums;
using System.Linq;
using System.Text;

namespace SmartRAG.Demo.DatabaseSetup.Creators;

/// <summary>
/// PostgreSQL test database creator implementation for Northwind HR & Geography
/// Domain: Northwind HR & Geography (Employees, Region, Territories, EmployeeTerritories)
/// Follows SOLID principles - Single Responsibility Principle
/// </summary>
public class PostgreSqlTestDatabaseCreator : ITestDatabaseCreator
{
    #region Constants

    private const int DefaultMaxRetries = 3;
    private const int DatabaseCreationDelayMilliseconds = 1000;
    private const int BaseRetryDelayMilliseconds = 2000;

    #endregion

    #region Fields

    private readonly IConfiguration? _configuration;
    private readonly ILogger<PostgreSqlTestDatabaseCreator>? _logger;
    private readonly string _server;
    private readonly int _port;
    private readonly string _user;
    private readonly string _databaseName;

    #endregion

    #region Constructor

    public PostgreSqlTestDatabaseCreator(IConfiguration? configuration = null, ILogger<PostgreSqlTestDatabaseCreator>? logger = null)
    {
        _configuration = configuration;
        _logger = logger;
        string? server = null;
        int port = 5432;
        string? user = null;
        string? databaseName = null;
        
        if (_configuration != null)
        {
            var connectionString = _configuration.GetConnectionString("LogisticsManagement") ?? 
                                 _configuration["DatabaseConnections:3:ConnectionString"];
            
            if (!string.IsNullOrEmpty(connectionString))
            {
                var builder = new NpgsqlConnectionStringBuilder(connectionString);
                server = builder.Host;
                port = builder.Port;
                user = builder.Username;
                databaseName = builder.Database;
            }
        }
        
        // Fallback to defaults if not found in config
        _server = server ?? "localhost";
        _port = port;
        _user = user ?? "postgres";
        _databaseName = databaseName ?? "LogisticsManagement";
    }

        #endregion

        #region Public Methods

        public DatabaseType GetDatabaseType() => DatabaseType.PostgreSQL;

        public string GetDefaultConnectionString()
        {
            return $"Server={_server};Port={_port};Database={_databaseName};User Id={_user};Password={GetPassword()};";
        }
        
        private string GetPassword()
        {
            if (_configuration != null)
            {
                var connectionString = _configuration.GetConnectionString("LogisticsManagement") ?? 
                                     _configuration["DatabaseConnections:3:ConnectionString"];
                
                if (!string.IsNullOrEmpty(connectionString))
                {
                    var builder = new NpgsqlConnectionStringBuilder(connectionString);
                    if (!string.IsNullOrEmpty(builder.Password))
                    {
                        return builder.Password;
                    }
                }
            }
            
            var envPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
            return string.IsNullOrEmpty(envPassword)
                ? throw new InvalidOperationException("PostgreSQL password not found in configuration or environment variables")
                : envPassword;
        }

        public string GetDescription()
        {
            return "PostgreSQL - Northwind HR & Geography (Employees, Region, Territories, EmployeeTerritories - EmployeeID self-reference)";
        }

        public bool ValidateConnectionString(string connectionString)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(connectionString))
                    return false;

                var requiredParts = new[] { "Server=", "Database=" };
                return requiredParts.All(part => connectionString.Contains(part));
            }
            catch
            {
                return false;
            }
        }

    public async Task CreateSampleDatabaseAsync(string connectionString)
    {
        await CreateSampleDatabaseAsync(connectionString, CancellationToken.None);
    }

    public async Task CreateSampleDatabaseAsync(string connectionString, CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Starting Northwind PostgreSQL database creation");

        try
        {
            NpgsqlConnection.ClearAllPools();

            _logger?.LogInformation("Step 1/3: Creating database");
            await CreateDatabaseAsync(cancellationToken);
            _logger?.LogInformation("Database {DatabaseName} created successfully", _databaseName);

            await Task.Delay(DatabaseCreationDelayMilliseconds, cancellationToken);

            _logger?.LogInformation("Step 2/3: Executing SQL script");
            await ExecuteWithRetryAsync(connectionString, (conn) => ExecuteSqlScriptAsync(conn, cancellationToken), DefaultMaxRetries);
            _logger?.LogInformation("SQL script executed successfully");

            _logger?.LogInformation("Northwind PostgreSQL database created successfully");
            
            await VerifyDatabaseAsync(connectionString, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create Northwind PostgreSQL database");
            throw;
        }
    }

    public void CreateSampleDatabase(string connectionString)
    {
        CreateSampleDatabaseAsync(connectionString).GetAwaiter().GetResult();
    }

        #endregion

        #region Private Methods

    #endregion

    #region Private Methods

    /// <summary>
    /// Executes an action with retry logic for transient connection errors
    /// </summary>
    /// <param name="connectionString">Database connection string</param>
    /// <param name="action">Action to execute</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    private async Task ExecuteWithRetryAsync(string connectionString, Func<NpgsqlConnection, Task> action, int maxRetries)
    {
        int retryCount = 0;
        Exception? lastException = null;

        while (retryCount < maxRetries)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    await action(connection);
                    return;
                }
            }
            catch (NpgsqlException ex) when (ex.Message.Contains("terminating connection") || 
                                           ex.Message.Contains("57P01"))
            {
                lastException = ex;
                retryCount++;
                
                if (retryCount < maxRetries)
                {
                    var delay = BaseRetryDelayMilliseconds * retryCount;
                    _logger?.LogWarning(ex, "Connection interrupted, retrying ({RetryCount}/{MaxRetries}) after {Delay}ms", retryCount, maxRetries, delay);
                    await Task.Delay(delay);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Non-retryable error occurred during database operation");
                throw;
            }
        }

        if (lastException != null)
        {
            _logger?.LogError(lastException, "Failed after {MaxRetries} retries", maxRetries);
            throw lastException;
        }
    }

    /// <summary>
    /// Creates the PostgreSQL database, dropping it first if it exists
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    private async Task CreateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var masterConnectionString = $"Server={_server};Port={_port};User Id={_user};Password={GetPassword()};Database=postgres;";

        using (var connection = new NpgsqlConnection(masterConnectionString))
        {
            await connection.OpenAsync(cancellationToken);

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{_databaseName}'";
                var exists = await cmd.ExecuteScalarAsync(cancellationToken) != null;

                if (exists)
                {
                    using (var terminateCmd = connection.CreateCommand())
                    {
                        terminateCmd.CommandText = $@"
                            SELECT pg_terminate_backend(pg_stat_activity.pid)
                            FROM pg_stat_activity
                            WHERE pg_stat_activity.datname = '{_databaseName}'
                            AND pid <> pg_backend_pid();";
                        await terminateCmd.ExecuteNonQueryAsync(cancellationToken);
                    }

                    using (var dropCmd = connection.CreateCommand())
                    {
                        dropCmd.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\"";
                        await dropCmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"CREATE DATABASE \"{_databaseName}\" WITH ENCODING='UTF8'";
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Executes the SQL script file to create tables and insert data
    /// </summary>
    /// <param name="connection">Database connection</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    private async Task ExecuteSqlScriptAsync(NpgsqlConnection connection, CancellationToken cancellationToken = default)
    {
        var sqlFilePath = FindSqlScriptFilePath("instnwnd.postgresql.sql");
        _logger?.LogInformation("Executing SQL script: {FilePath}", sqlFilePath);
        
        var sqlContent = await File.ReadAllTextAsync(sqlFilePath, cancellationToken);
        var statements = SplitSqlStatements(sqlContent);
        
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var statement in statements)
            {
                if (string.IsNullOrWhiteSpace(statement) || statement.Trim().StartsWith("--"))
                    continue;
                
                cancellationToken.ThrowIfCancellationRequested();
                
                using var command = new NpgsqlCommand(statement, connection, transaction);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            
            await transaction.CommitAsync(cancellationToken);
            _logger?.LogInformation("SQL script executed successfully");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
    
    /// <summary>
    /// Finds the SQL script file path relative to the project root
    /// </summary>
    private static string FindSqlScriptFilePath(string fileName)
    {
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var currentDir = Path.GetDirectoryName(assemblyLocation) ?? Directory.GetCurrentDirectory();
        
        var projectRoot = FindProjectRoot(currentDir);
        if (projectRoot == null)
        {
            throw new FileNotFoundException("Could not find project root directory.");
        }
        
        var sqlFilePath = Path.Combine(projectRoot, "examples", "SmartRAG.Demo", "DatabaseScripts", fileName);
        
        if (!File.Exists(sqlFilePath))
        {
            throw new FileNotFoundException($"SQL script file not found. Searched: {sqlFilePath}");
        }
        
        return sqlFilePath;
    }
    
    /// <summary>
    /// Finds the project root directory by searching upwards from the current directory
    /// </summary>
    private static string? FindProjectRoot(string startDir)
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
        return null;
    }
    
    /// <summary>
    /// Splits SQL content into individual statements (handles semicolons and GO commands)
    /// </summary>
    private static List<string> SplitSqlStatements(string sqlContent)
    {
        var statements = new List<string>();
        var currentStatement = new StringBuilder();
        var inQuotes = false;
        var quoteChar = '\0';
        
        var lines = sqlContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (trimmedLine.StartsWith("--") || string.IsNullOrWhiteSpace(trimmedLine))
                continue;
            
            if (trimmedLine.Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                if (currentStatement.Length > 0)
                {
                    statements.Add(currentStatement.ToString().Trim());
                    currentStatement.Clear();
                }
                continue;
            }
            
            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                
                if (!inQuotes && (ch == '\'' || ch == '"'))
                {
                    inQuotes = true;
                    quoteChar = ch;
                    currentStatement.Append(ch);
                }
                else if (inQuotes && ch == quoteChar)
                {
                    if (i + 1 < line.Length && line[i + 1] == quoteChar)
                    {
                        currentStatement.Append(ch).Append(ch);
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                        quoteChar = '\0';
                        currentStatement.Append(ch);
                    }
                }
                else if (!inQuotes && ch == ';')
                {
                    currentStatement.Append(ch);
                    if (currentStatement.Length > 0)
                    {
                        statements.Add(currentStatement.ToString().Trim());
                        currentStatement.Clear();
                    }
                }
                else
                {
                    currentStatement.Append(ch);
                }
            }
            
            currentStatement.Append('\n');
        }
        
        if (currentStatement.Length > 0)
        {
            statements.Add(currentStatement.ToString().Trim());
        }
        
        return statements.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
    }

    /// <summary>
    /// Verifies the database by querying table row counts
    /// </summary>
    /// <param name="connectionString">Database connection string</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    private async Task VerifyDatabaseAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        table_name as TableName,
                        (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = t.table_name) as TotalColumns
                    FROM information_schema.tables t
                    WHERE table_schema = 'public'
                    AND table_type = 'BASE TABLE'
                    ORDER BY table_name";

                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var tableName = reader["TableName"].ToString();
                        
                        using (var countConn = new NpgsqlConnection(connectionString))
                        {
                            await countConn.OpenAsync(cancellationToken);
                            using (var countCmd = countConn.CreateCommand())
                            {
                                countCmd.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\"";
                                var rowCount = await countCmd.ExecuteScalarAsync(cancellationToken);
                                _logger?.LogInformation("Table {TableName}: {RowCount} rows", tableName, rowCount);
                            }
                        }
                    }
                }
            }
        }
    }

    #endregion
}

