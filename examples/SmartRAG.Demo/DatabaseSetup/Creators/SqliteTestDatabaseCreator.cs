using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartRAG.Demo.DatabaseSetup.Interfaces;
using SmartRAG.Enums;
using System.Security.Cryptography;
using System.Text;

namespace SmartRAG.Demo.DatabaseSetup.Creators;

/// <summary>
/// SQLite test database creator implementation
/// Restores database from backup file based on configuration
/// Follows SOLID principles - Single Responsibility Principle
/// </summary>
public class SqliteTestDatabaseCreator : ITestDatabaseCreator
{
    #region Constants
    #endregion

    #region Fields

    private readonly IConfiguration? _configuration;
    private readonly ILogger<SqliteTestDatabaseCreator>? _logger;
    private readonly string? _configurationName;
    private readonly string _databaseName;

    #endregion

    #region Constructor

    public SqliteTestDatabaseCreator(IConfiguration? configuration = null, ILogger<SqliteTestDatabaseCreator>? logger = null)
    {
        _configuration = configuration;
        _logger = logger;
        
        string? databaseName = null;
        if (_configuration != null)
        {
            _configurationName = FindConfigurationNameByType(DatabaseType.SQLite);
            if (!string.IsNullOrEmpty(_configurationName))
            {
                databaseName = _configurationName;
            }
        }
        
        _databaseName = databaseName ?? "SqliteTestDatabase";
    }

    #endregion

    #region Public Methods

    public DatabaseType GetDatabaseType() => DatabaseType.SQLite;

        public string GetDefaultConnectionString()
        {
            if (_configuration != null)
            {
                var connectionString = FindConnectionStringByType(DatabaseType.SQLite);
                if (!string.IsNullOrEmpty(connectionString))
                {
                    return connectionString;
                }

                var dbPath = _configuration["DatabaseTests:Sqlite:DatabasePath"];
                
                if (string.IsNullOrEmpty(dbPath))
                {
                    dbPath = _configuration["DatabaseTests:DefaultDatabasePath"];
                }

                if (!string.IsNullOrEmpty(dbPath))
                {
                    if (!Path.IsPathRooted(dbPath))
                    {
                        var projectRoot = FindProjectRoot();
                        if (projectRoot != null)
                        {
                            dbPath = Path.Combine(projectRoot, dbPath);
                        }
                        else
                        {
                            var currentDir = Directory.GetCurrentDirectory();
                            dbPath = Path.Combine(currentDir, dbPath);
                        }
                    }

                    var cs = $"Data Source={dbPath}";
                    
                    var enableForeignKeys = _configuration["DatabaseTests:Sqlite:EnableForeignKeys"];
                    var connectionTimeout = _configuration["DatabaseTests:Sqlite:ConnectionTimeout"];
                    
                    if (!string.IsNullOrEmpty(enableForeignKeys) && bool.Parse(enableForeignKeys))
                    {
                        cs += ";Foreign Keys=True";
                    }
                    
                    if (!string.IsNullOrEmpty(connectionTimeout))
                    {
                        cs += $";Connection Timeout={connectionTimeout}";
                    }
                    
                    return cs;
                }
            }

            var fallbackProjectRoot = FindProjectRoot();
            var fallbackPath = fallbackProjectRoot != null
                ? Path.Combine(fallbackProjectRoot, "TestSQLiteData", "LogisticsManagement.db")
                : Path.Combine(Directory.GetCurrentDirectory(), "LogisticsManagement.db");
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

    public async Task<bool> DatabaseExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = GetDefaultConnectionString();
            var dbPath = ExtractFilePath(connectionString);
            
            if (!Path.IsPathRooted(dbPath))
            {
                var projectRoot = FindProjectRoot();
                if (projectRoot != null)
                {
                    dbPath = Path.Combine(projectRoot, dbPath);
                }
                else
                {
                    dbPath = Path.Combine(Directory.GetCurrentDirectory(), dbPath);
                }
            }
            
            dbPath = Path.GetFullPath(dbPath);
            return File.Exists(dbPath);
        }
        catch
        {
            return false;
        }
    }

    public async Task CreateSampleDatabaseAsync(string connectionString, CancellationToken cancellationToken)
    {
        var dbPath = ExtractFilePath(connectionString);

        if (await DatabaseExistsAsync(cancellationToken))
        {
            _logger?.LogInformation("SQLite database {DatabaseName} already exists, skipping creation", _databaseName);
            return;
        }

        try
        {
            if (!Path.IsPathRooted(dbPath))
            {
                var projectRoot = FindProjectRoot();
                if (projectRoot != null)
                {
                    dbPath = Path.Combine(projectRoot, dbPath);
                }
                else
                {
                    dbPath = Path.Combine(Directory.GetCurrentDirectory(), dbPath);
                }
            }
            
            dbPath = Path.GetFullPath(dbPath);
            var directory = Path.GetDirectoryName(dbPath);
            
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidOperationException($"Cannot determine directory for database path for '{_databaseName}'");
            }
            
            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception ex)
                {
                    var directoryHash = ComputeSafeHash(directory);
                    _logger?.LogError(ex, "Failed to create directory (hash: {DirectoryHash}). Error: {Error}", directoryHash, ex.Message);
                    throw new InvalidOperationException($"Failed to create directory. Error: {ex.Message}", ex);
                }
            }

            var resolvedConnectionString = $"Data Source={dbPath};Mode=ReadWriteCreate;Foreign Keys=True";
            
            using var connection = new SqliteConnection(resolvedConnectionString);
            await connection.OpenAsync(cancellationToken);
            
            using var pragmaCommand = connection.CreateCommand();
            pragmaCommand.CommandText = "PRAGMA journal_mode=WAL;";
            await pragmaCommand.ExecuteNonQueryAsync(cancellationToken);

            await connection.CloseAsync();

            await RestoreFromBackupAsync(dbPath, cancellationToken);

            if (File.Exists(dbPath))
            {
                var fileSize = new FileInfo(dbPath).Length / 1024.0;
                _logger?.LogInformation("SQLite database {DatabaseName} created successfully, Size: {FileSize:F2} KB", _databaseName, fileSize);
            }
            else
            {
                _logger?.LogWarning("Database file for {DatabaseName} was not created at the expected path", _databaseName);
                throw new InvalidOperationException($"Database file for '{_databaseName}' was not created at the expected location.");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SQLite database {DatabaseName} creation failed. Error: {Error}", _databaseName, ex.Message);
            throw;
        }
    }

    public void CreateSampleDatabase(string connectionString)
    {
        CreateSampleDatabaseAsync(connectionString).GetAwaiter().GetResult();
    }

    #endregion

    #region Private Methods

    private static string ComputeSafeHash(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "empty";
        
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hashBytes).Substring(0, 8);
    }

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
    /// Restores SQLite database from backup file
    /// </summary>
    /// <param name="targetDbPath">Target database file path</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    private async Task RestoreFromBackupAsync(string targetDbPath, CancellationToken cancellationToken = default)
    {
        string backupFileName;
        if (!string.IsNullOrEmpty(_configurationName))
        {
            backupFileName = GetBackupFileName(_configurationName);
        }
        else
        {
            var dbName = Path.GetFileNameWithoutExtension(targetDbPath);
            backupFileName = GetBackupFileName(dbName);
        }
        
        var backupFilePath = FindBackupFilePath(backupFileName);
        
        if (!File.Exists(backupFilePath))
        {
            throw new FileNotFoundException($"Backup file not found. Expected: {backupFilePath}");
        }
        
        cancellationToken.ThrowIfCancellationRequested();
        
        try
        {
            await Task.Run(() =>
            {
                File.Copy(backupFilePath, targetDbPath, overwrite: true);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            var safeBackupFileName = Path.GetFileName(backupFilePath);
            _logger?.LogError(ex, "Error restoring backup file: {BackupFileName}. Error: {Error}", safeBackupFileName, ex.Message);
            throw;
        }
    }
    
    /// <summary>
    /// Finds the backup file path relative to the project root
    /// </summary>
    /// <param name="fileName">Backup file name</param>
    /// <returns>Full path to the backup file</returns>
    private static string FindBackupFilePath(string fileName)
    {
        var projectRoot = FindProjectRoot();
        if (projectRoot == null)
        {
            throw new FileNotFoundException("Could not find project root directory.");
        }
        
        var backupFilePath = Path.Combine(projectRoot, "DatabaseBackups", fileName);
        
        if (!File.Exists(backupFilePath))
        {
            var alternativePath = Path.Combine(Directory.GetCurrentDirectory(), "DatabaseBackups", fileName);
            if (File.Exists(alternativePath))
            {
                return alternativePath;
            }
            throw new FileNotFoundException($"Backup file not found. Searched: {backupFilePath}");
        }
        
        return backupFilePath;
    }
    
    /// <summary>
    /// Finds the project root directory by searching upwards from the current directory
    /// </summary>
    private static string? FindProjectRoot(string? startDir = null)
    {
        var currentDir = startDir ?? Directory.GetCurrentDirectory();
        var dir = new DirectoryInfo(currentDir);
        
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
    
    /// <summary>
    /// Generates backup file name from database name (lowercase, sanitized)
    /// </summary>
    private static string GetBackupFileName(string databaseName)
    {
        var sanitizedName = databaseName.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");
        return $"{sanitizedName}.backup.db";
    }
    
    /// <summary>
    /// Finds connection string from DatabaseConnections array by database type
    /// </summary>
    private string? FindConnectionStringByType(DatabaseType databaseType)
    {
        if (_configuration == null)
            return null;
        
        var databaseTypeStr = databaseType.ToString();
        var connectionsSection = _configuration.GetSection("DatabaseConnections");
        
        if (!connectionsSection.Exists())
            return null;
        
        var connections = connectionsSection.GetChildren();
        foreach (var connection in connections)
        {
            var type = connection["DatabaseType"];
            if (string.Equals(type, databaseTypeStr, StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = connection["ConnectionString"];
                if (!string.IsNullOrEmpty(connectionString))
                {
                    return connectionString;
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// Finds configuration name from DatabaseConnections array by database type
    /// </summary>
    private string? FindConfigurationNameByType(DatabaseType databaseType)
    {
        if (_configuration == null)
            return null;
        
        var databaseTypeStr = databaseType.ToString();
        var connectionsSection = _configuration.GetSection("DatabaseConnections");
        
        if (!connectionsSection.Exists())
            return null;
        
        var connections = connectionsSection.GetChildren();
        foreach (var connection in connections)
        {
            var type = connection["DatabaseType"];
            if (string.Equals(type, databaseTypeStr, StringComparison.OrdinalIgnoreCase))
            {
                var name = connection["Name"];
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
        }
        
        return null;
    }

    #endregion
}

