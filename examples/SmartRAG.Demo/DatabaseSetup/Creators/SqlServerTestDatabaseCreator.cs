using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartRAG.Demo.DatabaseSetup.Interfaces;
using SmartRAG.Enums;
using System.Linq;
using System.Text;

namespace SmartRAG.Demo.DatabaseSetup.Creators;

/// <summary>
/// SQL Server test database creator implementation for Northwind sales transactions
/// Domain: Northwind Sales Transactions (Orders, Order Details)
/// Follows SOLID principles - Single Responsibility Principle
/// </summary>
public class SqlServerTestDatabaseCreator : ITestDatabaseCreator
{
    #region Constants

    private const int DefaultMaxRetries = 3;

    #endregion

    #region Fields

    private readonly IConfiguration? _configuration;
    private readonly ILogger<SqlServerTestDatabaseCreator>? _logger;
    private readonly string _server;
    private readonly string _databaseName;

    #endregion

    #region Constructor

    public SqlServerTestDatabaseCreator(IConfiguration? configuration = null, ILogger<SqlServerTestDatabaseCreator>? logger = null)
    {
        _configuration = configuration;
        _logger = logger;
        string? server = null;
        string? databaseName = null;

        if (_configuration != null)
        {
            var connectionString = _configuration.GetConnectionString("SalesManagement") ??
                                 _configuration["DatabaseConnections:1:ConnectionString"];

            if (!string.IsNullOrEmpty(connectionString))
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                server = builder.DataSource;
                databaseName = builder.InitialCatalog;
            }
        }

        // Fallback to defaults if not found in config
        _server = server ?? "localhost,1433";
        _databaseName = databaseName ?? "SalesManagement";
    }

    #endregion

    #region Public Methods

    public DatabaseType GetDatabaseType() => DatabaseType.SqlServer;

    public string GetDefaultConnectionString()
    {
        return $"Server={_server};Database={_databaseName};User Id=sa;Password={GetPassword()};TrustServerCertificate=true;";
    }
    
    private string GetPassword()
    {
        if (_configuration != null)
        {
            var connectionString = _configuration.GetConnectionString("SalesManagement") ??
                                 _configuration["DatabaseConnections:1:ConnectionString"];

            if (!string.IsNullOrEmpty(connectionString))
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                if (!string.IsNullOrEmpty(builder.Password))
                {
                    return builder.Password;
                }
            }
        }

        var envPassword = Environment.GetEnvironmentVariable("SQLSERVER_SA_PASSWORD");
        return string.IsNullOrEmpty(envPassword) 
            ? throw new InvalidOperationException("SQL Server password not found in configuration or environment variables")
            : envPassword;
    }

    public string GetDescription()
    {
        return "SQL Server - Northwind Sales Transactions (Orders, Order Details - CustomerID, ProductID, EmployeeID, ShipperID reference other databases)";
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
        _logger?.LogInformation("Starting Northwind SQL Server database creation");

        try
        {
            _logger?.LogInformation("Step 1/3: Creating database");
            await CreateDatabaseAsync(cancellationToken);
            _logger?.LogInformation("Database {DatabaseName} created successfully", _databaseName);

            _logger?.LogInformation("Step 2/3: Executing SQL script");
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                await ExecuteSqlScriptAsync(connection, cancellationToken);
            }
            _logger?.LogInformation("SQL script executed successfully");

            _logger?.LogInformation("Northwind SQL Server database created successfully");

            await VerifyDatabaseAsync(connectionString, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create Northwind SQL Server database");
            throw;
        }
    }

    public void CreateSampleDatabase(string connectionString)
    {
        CreateSampleDatabaseAsync(connectionString).GetAwaiter().GetResult();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates the SQL Server database, dropping it first if it exists
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    private async Task CreateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var masterConnectionString = $"Server={_server};Database=master;User Id=sa;Password={GetPassword()};TrustServerCertificate=true;";

        using (var connection = new SqlConnection(masterConnectionString))
        {
            await connection.OpenAsync(cancellationToken);

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $@"
                    IF EXISTS (SELECT name FROM sys.databases WHERE name = '{_databaseName}')
                    BEGIN
                        ALTER DATABASE {_databaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE {_databaseName};
                    END";
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"CREATE DATABASE {_databaseName}";
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Executes the SQL script file to create tables and insert data
    /// </summary>
    /// <param name="connection">Database connection</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    private async Task ExecuteSqlScriptAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        var sqlFilePath = FindSqlScriptFilePath("instnwnd.sqlserver.sql");
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
                
                using var command = new SqlCommand(statement, connection, (SqlTransaction)transaction);
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
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        t.name as TableName,
                        SUM(p.rows) as TotalRows
                    FROM sys.tables t
                    INNER JOIN sys.partitions p ON t.object_id = p.object_id
                    WHERE p.index_id IN (0,1)
                    GROUP BY t.name
                    ORDER BY t.name";

                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        _logger?.LogInformation("Table {TableName}: {TotalRows} rows", reader["TableName"], reader["TotalRows"]);
                    }
                }
            }
        }
    }

    #endregion
}
