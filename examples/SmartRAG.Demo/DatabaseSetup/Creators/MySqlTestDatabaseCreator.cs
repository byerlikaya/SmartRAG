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

    private static string ValidateContainerName(string containerName)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));
        
        if (!System.Text.RegularExpressions.Regex.IsMatch(containerName, @"^[a-zA-Z0-9_\-]+$"))
            throw new ArgumentException("Container name contains invalid characters. Only alphanumeric characters, underscores, and hyphens are allowed.", nameof(containerName));
        
        return containerName;
    }

    private static string ValidatePath(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException($"{parameterName} cannot be null or empty", parameterName);
        
        if (path.Contains("..") || path.Contains("//") || path.Contains("\\\\"))
            throw new ArgumentException($"{parameterName} contains invalid path characters. Path traversal is not allowed.", parameterName);
        
        if (path.Contains(";") || path.Contains("&") || path.Contains("|") || path.Contains("`") || path.Contains("$"))
            throw new ArgumentException($"{parameterName} contains invalid characters that could be used for command injection.", parameterName);
        
        return path;
    }

    private static string ValidateServer(string server)
    {
        if (string.IsNullOrWhiteSpace(server))
            throw new ArgumentException("Server cannot be null or empty", nameof(server));
        
        if (!System.Text.RegularExpressions.Regex.IsMatch(server, @"^[a-zA-Z0-9._\-]+$"))
            throw new ArgumentException("Server contains invalid characters. Only alphanumeric characters, dots, underscores, and hyphens are allowed.", nameof(server));
        
        return server;
    }

    private static string ValidateUser(string user)
    {
        if (string.IsNullOrWhiteSpace(user))
            throw new ArgumentException("User cannot be null or empty", nameof(user));
        
        if (!System.Text.RegularExpressions.Regex.IsMatch(user, @"^[a-zA-Z0-9_]+$"))
            throw new ArgumentException("User contains invalid characters. Only alphanumeric characters and underscores are allowed.", nameof(user));
        
        return user;
    }

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
        var validatedContainerName = ValidateContainerName(containerName);
        var validatedBackupPath = ValidatePath(backupFilePath, nameof(backupFilePath));
        var validatedUser = ValidateUser(_user);
        var validatedDatabaseName = ValidateDatabaseName(_databaseName);
        var containerBackupPath = "/tmp/restore.sql";
        
        var copyResult = await CopyBackupToContainerAsync(validatedBackupPath, validatedContainerName, containerBackupPath);
        if (!copyResult)
        {
            throw new InvalidOperationException($"Failed to copy backup file to container: {validatedContainerName}");
        }
        
        await Task.Run(async () =>
        {
            var grepProcessInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                ArgumentList = { "exec", validatedContainerName, "grep", "-v", "^mysqldump:", containerBackupPath },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var grepProcess = System.Diagnostics.Process.Start(grepProcessInfo);
            if (grepProcess == null)
            {
                throw new InvalidOperationException("Failed to start grep process");
            }
            
            var grepOutput = await grepProcess.StandardOutput.ReadToEndAsync();
            await grepProcess.WaitForExitAsync();
            
            var mysqlProcessInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                ArgumentList = { "exec", "-i", validatedContainerName, "mysql", "-u", validatedUser, "-p" + password, validatedDatabaseName },
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var mysqlProcess = System.Diagnostics.Process.Start(mysqlProcessInfo);
            if (mysqlProcess == null)
            {
                throw new InvalidOperationException("Failed to start mysql process");
            }
            
            await mysqlProcess.StandardInput.WriteAsync(grepOutput);
            mysqlProcess.StandardInput.Close();
            
            var processInfo = mysqlProcessInfo;
            
            var output = await mysqlProcess.StandardOutput.ReadToEndAsync();
            var error = await mysqlProcess.StandardError.ReadToEndAsync();
            
            await mysqlProcess.WaitForExitAsync();
            
            if (mysqlProcess.ExitCode != 0)
            {
                var errorMessage = string.IsNullOrWhiteSpace(error) ? output : error;
                if (!errorMessage.Contains("[Warning] Using a password"))
                {
                    throw new InvalidOperationException($"MySQL restore failed with exit code {mysqlProcess.ExitCode}: {errorMessage}");
                }
            }
        }, cancellationToken);
    }
    
    /// <summary>
    /// Restores MySQL database using local mysql command
    /// </summary>
    private async Task RestoreLocalAsync(string backupFilePath, string password, CancellationToken cancellationToken)
    {
        var validatedBackupPath = ValidatePath(backupFilePath, nameof(backupFilePath));
        var validatedServer = ValidateServer(_server);
        var validatedUser = ValidateUser(_user);
        var validatedDatabaseName = ValidateDatabaseName(_databaseName);
        
        await Task.Run(async () =>
        {
            var grepProcessInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "grep",
                ArgumentList = { "-v", "^mysqldump:", validatedBackupPath },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var grepProcess = System.Diagnostics.Process.Start(grepProcessInfo);
            if (grepProcess == null)
            {
                throw new InvalidOperationException("Failed to start grep process. Make sure grep is installed and in PATH.");
            }
            
            var grepOutput = await grepProcess.StandardOutput.ReadToEndAsync();
            await grepProcess.WaitForExitAsync();
            
            var mysqlProcessInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "mysql",
                ArgumentList = { "-h", validatedServer, "-P", _port.ToString(), "-u", validatedUser, "-p" + password, validatedDatabaseName },
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var mysqlProcess = System.Diagnostics.Process.Start(mysqlProcessInfo);
            if (mysqlProcess == null)
            {
                throw new InvalidOperationException("Failed to start mysql restore process. Make sure MySQL client tools are installed and mysql is in PATH.");
            }
            
            await mysqlProcess.StandardInput.WriteAsync(grepOutput);
            mysqlProcess.StandardInput.Close();
            
            var output = await mysqlProcess.StandardOutput.ReadToEndAsync();
            var error = await mysqlProcess.StandardError.ReadToEndAsync();
            
            await mysqlProcess.WaitForExitAsync();
            
            if (mysqlProcess.ExitCode != 0)
            {
                var errorMessage = string.IsNullOrWhiteSpace(error) ? output : error;
                if (!errorMessage.Contains("[Warning] Using a password"))
                {
                    throw new InvalidOperationException($"MySQL restore failed with exit code {mysqlProcess.ExitCode}: {errorMessage}");
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
        var validatedContainerName = ValidateContainerName(containerName);
        var validatedLocalPath = ValidatePath(localBackupPath, nameof(localBackupPath));
        var validatedContainerPath = ValidatePath(containerPath, nameof(containerPath));
        
        return await Task.Run(() =>
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    ArgumentList = { "cp", validatedLocalPath, $"{validatedContainerName}:{validatedContainerPath}" },
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

