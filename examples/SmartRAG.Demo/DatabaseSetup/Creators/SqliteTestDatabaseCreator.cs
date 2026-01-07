using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartRAG.Demo.DatabaseSetup.Interfaces;
using SmartRAG.Enums;
using System.Linq;
using System.Text;

namespace SmartRAG.Demo.DatabaseSetup.Creators;

/// <summary>
/// SQLite test database creator implementation for Northwind master data
/// Domain: Northwind Master Data (Customers, Categories, Suppliers, Products)
/// Follows SOLID principles - Single Responsibility Principle
/// </summary>
public class SqliteTestDatabaseCreator : ITestDatabaseCreator
{
    #region Constants
    #endregion

    #region Fields

    private readonly IConfiguration? _configuration;
    private readonly ILogger<SqliteTestDatabaseCreator>? _logger;

    #endregion

    #region Constructor

    public SqliteTestDatabaseCreator(IConfiguration? configuration = null, ILogger<SqliteTestDatabaseCreator>? logger = null)
    {
        _configuration = configuration;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    public DatabaseType GetDatabaseType() => DatabaseType.SQLite;

    public string GetDescription() => "SQLite - Northwind Master Data (Suppliers, Products)";

        public string GetDefaultConnectionString()
        {
            if (_configuration != null)
            {
                // First check SQLite specific path
                var dbPath = _configuration["DatabaseTests:Sqlite:DatabasePath"];
                
                // If SQLite specific path not found, use DefaultDatabasePath
                if (string.IsNullOrEmpty(dbPath))
                {
                    dbPath = _configuration["DatabaseTests:DefaultDatabasePath"];
                }

                if (!string.IsNullOrEmpty(dbPath))
                {
                    // If relative path, add to current directory
                    if (!Path.IsPathRooted(dbPath))
                    {
                        var currentDir = Directory.GetCurrentDirectory();
                        dbPath = Path.Combine(currentDir, dbPath);
                    }

                    // Create connection string
                    var connectionString = $"Data Source={dbPath}";
                    
                    // Add SQLite specific settings
                    var enableForeignKeys = _configuration["DatabaseTests:Sqlite:EnableForeignKeys"];
                    var connectionTimeout = _configuration["DatabaseTests:Sqlite:ConnectionTimeout"];
                    
                    if (!string.IsNullOrEmpty(enableForeignKeys) && bool.Parse(enableForeignKeys))
                    {
                        connectionString += ";Foreign Keys=True";
                    }
                    
                    if (!string.IsNullOrEmpty(connectionTimeout))
                    {
                        connectionString += $";Connection Timeout={connectionTimeout}";
                    }
                    
                    return connectionString;
                }
            }

            // Fallback: Default path
            var fallbackPath = Path.Combine(Directory.GetCurrentDirectory(), "test_company.db");
            return $"Data Source={fallbackPath};Foreign Keys=True";
        }

        public bool ValidateConnectionString(string connectionString)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(connectionString))
                    return false;

                // Extract file path from connection string
                if (connectionString.Contains("Data Source="))
                {
                    var dataSource = connectionString.Split("Data Source=")[1].Split(';')[0];
                    var directory = Path.GetDirectoryName(dataSource);
                    
                    // Check if directory exists or can be created
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    return true;
                }
                return false;
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
        _logger?.LogInformation("Starting Northwind SQLite database creation");

        try
        {
            var dbPath = ExtractFilePath(connectionString);
            
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
                _logger?.LogInformation("Existing database deleted");
            }

            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await ExecuteSqlScriptAsync(connection, cancellationToken);

            var fileSize = new FileInfo(dbPath).Length / 1024.0;
            _logger?.LogInformation("Northwind SQLite database created successfully, Size: {FileSize:F2} KB", fileSize);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Northwind SQLite database creation failed");
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
    /// Extracts the file path from SQLite connection string
    /// </summary>
    /// <param name="connectionString">SQLite connection string</param>
    /// <returns>Database file path</returns>
    private string ExtractFilePath(string connectionString)
    {
        if (connectionString.Contains("Data Source="))
        {
            return connectionString.Split("Data Source=")[1].Split(';')[0];
        }
        throw new ArgumentException("Invalid SQLite connection string");
    }

    /// <summary>
    /// Executes the SQL script file to create tables and insert data
    /// </summary>
    /// <param name="connection">Database connection</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    private async Task ExecuteSqlScriptAsync(SqliteConnection connection, CancellationToken cancellationToken = default)
    {
        var sqlFilePath = FindSqlScriptFilePath("instnwnd.sqlite.sql");
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
                
                using var command = new SqliteCommand(statement, connection, (SqliteTransaction)transaction);
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
    /// <param name="fileName">SQL script file name</param>
    /// <returns>Full path to the SQL script file</returns>
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


    #endregion
}

