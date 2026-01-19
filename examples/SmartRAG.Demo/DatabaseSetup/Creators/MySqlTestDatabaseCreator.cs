using MySqlConnector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartRAG.Demo.DatabaseSetup.Interfaces;
using SmartRAG.Enums;
using System.Linq;

namespace SmartRAG.Demo.DatabaseSetup.Creators;

/// <summary>
/// MySQL test database creator implementation
/// Restores database from backup file based on configuration
/// Follows SOLID principles - Single Responsibility Principle
/// </summary>
public class MySqlTestDatabaseCreator : ITestDatabaseCreator
{
    #region Constants

    private const int DefaultMaxRetries = 3;
    private const int DatabaseCreationDelayMilliseconds = 500;
    private const int BaseRetryDelayMilliseconds = 1000;

    #endregion

    #region Fields

    private readonly IConfiguration? _configuration;
    private readonly ILogger<MySqlTestDatabaseCreator>? _logger;
    private readonly string _server;
    private readonly int _port;
    private readonly string _user;
    private readonly string _databaseName;

    #endregion

    #region Constructor

    public MySqlTestDatabaseCreator(IConfiguration? configuration = null, ILogger<MySqlTestDatabaseCreator>? logger = null)
    {
        _configuration = configuration;
        _logger = logger;
        
        string? server = null;
        int port = 3306;
        string? user = null;
        string? databaseName = null;
        
        if (_configuration != null)
        {
            var connectionString = FindConnectionStringByType(DatabaseType.MySQL);
            
            if (!string.IsNullOrEmpty(connectionString))
            {
                var builder = new MySqlConnectionStringBuilder(connectionString);
                server = builder.Server;
                port = (int)builder.Port;
                user = builder.UserID;
                databaseName = builder.Database;
            }
        }
        
        _server = server ?? "localhost";
        _port = port;
        _user = user ?? "root";
        _databaseName = databaseName ?? "TestDatabase";
    }

    #endregion

    #region Public Methods

    public DatabaseType GetDatabaseType() => DatabaseType.MySQL;

        public string GetDefaultConnectionString()
        {
            return $"Server={_server};Port={_port};Database={_databaseName};User={_user};Password={GetPassword()};";
        }
        
        private string GetPassword()
        {
            if (_configuration != null)
            {
                var connectionString = FindConnectionStringByType(DatabaseType.MySQL);
                
                if (!string.IsNullOrEmpty(connectionString))
                {
                    var builder = new MySqlConnectionStringBuilder(connectionString);
                    if (!string.IsNullOrEmpty(builder.Password))
                    {
                        return builder.Password;
                    }
                }
            }
            
            var envPassword = Environment.GetEnvironmentVariable("MYSQL_ROOT_PASSWORD");
            return string.IsNullOrEmpty(envPassword)
                ? throw new InvalidOperationException("MySQL password not found in configuration or environment variables")
                : envPassword;
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

    public async Task<bool> DatabaseExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var masterConnectionString = $"Server={_server};Port={_port};User={_user};Password={GetPassword()};";
            var validatedName = ValidateDatabaseName(_databaseName);
            var escapedNameForString = validatedName.Replace("'", "''");
            
            using var connection = new MySqlConnection(masterConnectionString);
            await connection.OpenAsync(cancellationToken);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM information_schema.SCHEMATA WHERE SCHEMA_NAME = '{escapedNameForString}'";
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task CreateSampleDatabaseAsync(string connectionString, CancellationToken cancellationToken)
    {
        if (await DatabaseExistsAsync(cancellationToken))
        {
            _logger?.LogInformation("MySQL database {DatabaseName} already exists, skipping creation", _databaseName);
            return;
        }

        try
        {
            await CreateDatabaseAsync(cancellationToken);
            await Task.Delay(DatabaseCreationDelayMilliseconds, cancellationToken);
            await RestoreFromBackupAsync(connectionString, cancellationToken);
            _logger?.LogInformation("MySQL database {DatabaseName} created successfully", _databaseName);
            await VerifyDatabaseAsync(connectionString, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create MySQL database {DatabaseName}", _databaseName);
            throw;
        }
    }

    public void CreateSampleDatabase(string connectionString)
    {
        CreateSampleDatabaseAsync(connectionString).GetAwaiter().GetResult();
    }

    #endregion

    #region Private Methods

    private static string EscapeMySqlIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be null or empty", nameof(identifier));
        
        return $"`{identifier.Replace("`", "``")}`";
    }

    private static string ValidateDatabaseName(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("Database name cannot be null or empty", nameof(databaseName));
        
        if (!System.Text.RegularExpressions.Regex.IsMatch(databaseName, @"^[a-zA-Z0-9_]+$"))
            throw new ArgumentException("Database name contains invalid characters. Only alphanumeric characters and underscores are allowed.", nameof(databaseName));
        
        return databaseName;
    }

    /// <summary>
    /// Executes an action with retry logic for transient connection errors
    /// </summary>
    /// <param name="connectionString">Database connection string</param>
    /// <param name="action">Action to execute</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    private async Task ExecuteWithRetryAsync(string connectionString, Func<MySqlConnection, Task> action, int maxRetries)
    {
        int retryCount = 0;
        Exception? lastException = null;

        while (retryCount < maxRetries)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    await action(connection);
                    return;
                }
            }
            catch (MySqlException ex) when (ex.Message.Contains("Lost connection") || 
                                            ex.Message.Contains("aborted"))
            {
                lastException = ex;
                retryCount++;
                
                if (retryCount < maxRetries)
                {
                    var delay = BaseRetryDelayMilliseconds * retryCount;
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
    /// Creates the MySQL database, dropping it first if it exists
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    private async Task CreateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var validatedName = ValidateDatabaseName(_databaseName);
        var escapedName = EscapeMySqlIdentifier(validatedName);
        
        var masterConnectionString = $"Server={_server};Port={_port};User={_user};Password={GetPassword()};";

        using (var connection = new MySqlConnection(masterConnectionString))
        {
            await connection.OpenAsync(cancellationToken);

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"DROP DATABASE IF EXISTS {escapedName}";
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"CREATE DATABASE {escapedName} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Restores MySQL database from backup file
    /// </summary>
    /// <param name="connectionString">Database connection string</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <summary>
    /// Restores MySQL database from backup file using mysql command (supports both Docker and local installations)
    /// </summary>
    /// <param name="connectionString">Database connection string</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    private async Task RestoreFromBackupAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var backupFileName = GetBackupFileName();
        var backupFilePath = FindBackupFilePath(backupFileName);
        
        if (!File.Exists(backupFilePath))
        {
            throw new FileNotFoundException($"Backup file not found. Expected: {backupFilePath}");
        }
        
        cancellationToken.ThrowIfCancellationRequested();
        
        try
        {
            var password = GetPassword();
            var containerName = await FindMySqlContainerNameAsync();
            
            if (!string.IsNullOrEmpty(containerName))
            {
                await RestoreInDockerContainerAsync(containerName, backupFilePath, password, cancellationToken);
            }
            else
            {
                await RestoreLocalAsync(backupFilePath, password, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error restoring backup file: {BackupPath}. Error: {Error}", backupFilePath, ex.Message);
            throw;
        }
    }
    
    /// <summary>
    /// Restores MySQL database in Docker container
    /// </summary>
    private async Task RestoreInDockerContainerAsync(string containerName, string backupFilePath, string password, CancellationToken cancellationToken)
    {
        var containerBackupPath = "/tmp/restore.sql";
        
        var copyResult = await CopyBackupToContainerAsync(backupFilePath, containerName, containerBackupPath);
        if (!copyResult)
        {
            throw new InvalidOperationException($"Failed to copy backup file to container: {containerName}");
        }
        
        await Task.Run(() =>
        {
            var validatedName = ValidateDatabaseName(_databaseName);
            var escapedPath = containerBackupPath.Replace("'", "'\"'\"'");
            var escapedPassword = password.Replace("'", "'\"'\"'");
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"exec {containerName} sh -c \"grep -v '^mysqldump:' {escapedPath} | mysql -u {_user} -p'{escapedPassword}' {validatedName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start docker exec process");
            }
            
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            
            process.WaitForExit(600000);
            
            if (process.ExitCode != 0)
            {
                var errorMessage = string.IsNullOrWhiteSpace(error) ? output : error;
                if (!errorMessage.Contains("[Warning] Using a password"))
                {
                    throw new InvalidOperationException($"MySQL restore failed with exit code {process.ExitCode}: {errorMessage}");
                }
            }
        }, cancellationToken);
    }
    
    /// <summary>
    /// Restores MySQL database using local mysql command
    /// </summary>
    private async Task RestoreLocalAsync(string backupFilePath, string password, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            var validatedName = ValidateDatabaseName(_databaseName);
            var escapedPath = backupFilePath.Replace("'", "'\"'\"'");
            var escapedPassword = password.Replace("'", "'\"'\"'");
            var shellCommand = "/bin/sh";
            var shellArgs = $"-c \"grep -v '^mysqldump:' '{escapedPath}' | mysql -h {_server} -P {_port} -u {_user} -p'{escapedPassword}' {validatedName}\"";
            
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = shellCommand,
                Arguments = shellArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start mysql restore process. Make sure MySQL client tools are installed and mysql is in PATH.");
            }
            
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            
            process.WaitForExit(600000);
            
            if (process.ExitCode != 0)
            {
                var errorMessage = string.IsNullOrWhiteSpace(error) ? output : error;
                if (!errorMessage.Contains("[Warning] Using a password"))
                {
                    throw new InvalidOperationException($"MySQL restore failed with exit code {process.ExitCode}: {errorMessage}");
                }
            }
        }, cancellationToken);
    }
    
    /// <summary>
    /// Finds MySQL Docker container name by checking port 3306
    /// </summary>
    private async Task<string?> FindMySqlContainerNameAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "ps --filter \"publish=3306\" --format \"{{.Names}}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process == null) return null;
                
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(5000);
                
                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    var containerName = output.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    return containerName;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to find MySQL container: {Error}", ex.Message);
                return null;
            }
        });
    }
    
    /// <summary>
    /// Copies backup file to Docker container
    /// </summary>
    private async Task<bool> CopyBackupToContainerAsync(string localBackupPath, string containerName, string containerPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"cp \"{localBackupPath}\" {containerName}:{containerPath}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process == null) return false;
                
                process.WaitForExit(300000);
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to copy backup to container: {Error}", ex.Message);
                return false;
            }
        });
    }
    
    /// <summary>
    /// Finds the backup file path relative to the project root
    /// </summary>
    private static string FindBackupFilePath(string fileName)
    {
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var currentDir = Path.GetDirectoryName(assemblyLocation) ?? Directory.GetCurrentDirectory();
        
        var projectRoot = FindProjectRoot(currentDir);
        if (projectRoot == null)
        {
            throw new FileNotFoundException("Could not find project root directory.");
        }
        
        var backupFilePath = Path.Combine(projectRoot, "examples", "SmartRAG.Demo", "DatabaseBackups", fileName);
        
        if (!File.Exists(backupFilePath))
        {
            throw new FileNotFoundException($"Backup file not found. Searched: {backupFilePath}");
        }
        
        return backupFilePath;
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
    /// Verifies the database by checking table existence
    /// </summary>
    /// <param name="connectionString">Database connection string</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    private async Task VerifyDatabaseAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(*) 
            FROM information_schema.TABLES 
            WHERE TABLE_SCHEMA = @dbName";
        
        cmd.Parameters.AddWithValue("@dbName", _databaseName);
        var tableCount = await cmd.ExecuteScalarAsync(cancellationToken);
        
        if (tableCount == null || Convert.ToInt32(tableCount) == 0)
        {
            throw new InvalidOperationException("Database verification failed: No tables found");
        }
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
    /// Generates backup file name from database name (lowercase, sanitized)
    /// </summary>
    private string GetBackupFileName()
    {
        var sanitizedName = _databaseName.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");
        return $"{sanitizedName}.backup.sql";
    }

    #endregion
}

