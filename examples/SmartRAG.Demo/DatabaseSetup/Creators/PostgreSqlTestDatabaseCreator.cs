using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartRAG.Demo.DatabaseSetup.Interfaces;
using SmartRAG.Enums;
using System.Linq;

namespace SmartRAG.Demo.DatabaseSetup.Creators;

/// <summary>
/// PostgreSQL test database creator implementation
/// Restores database from backup file based on configuration
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
            var connectionString = FindConnectionStringByType(DatabaseType.PostgreSQL);
            
            if (!string.IsNullOrEmpty(connectionString))
            {
                var builder = new NpgsqlConnectionStringBuilder(connectionString);
                server = builder.Host;
                port = builder.Port;
                user = builder.Username;
                databaseName = builder.Database;
            }
        }
        
        _server = server ?? "localhost";
        _port = port;
        _user = user ?? "postgres";
        _databaseName = databaseName ?? "TestDatabase";
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
                var connectionString = FindConnectionStringByType(DatabaseType.PostgreSQL);
                
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
        try
        {
            NpgsqlConnection.ClearAllPools();

            await CreateDatabaseAsync(cancellationToken);
            await Task.Delay(DatabaseCreationDelayMilliseconds, cancellationToken);
            await RestoreFromBackupAsync(connectionString, cancellationToken);
            _logger?.LogInformation("PostgreSQL database {DatabaseName} created successfully", _databaseName);
            
            await VerifyDatabaseAsync(connectionString, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create PostgreSQL database {DatabaseName}", _databaseName);
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

                    await Task.Delay(1000, cancellationToken);

                    using (var dropCmd = connection.CreateCommand())
                    {
                        dropCmd.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\"";
                        await dropCmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                    
                    await Task.Delay(500, cancellationToken);
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
    /// Restores PostgreSQL database from backup file using psql command (supports both Docker and local installations)
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
            var containerName = await FindPostgreSqlContainerNameAsync();
            
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
    /// Restores PostgreSQL database in Docker container
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
            var escapedPath = containerBackupPath.Replace("'", "'\"'\"'");
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"exec {containerName} sh -c \"sed '/^\\\\\\\\restrict/d' {escapedPath} | sed -E 's/^CREATE SCHEMA (\\\"[^\\\"]+\\\"|\\w+)/CREATE SCHEMA IF NOT EXISTS \\1/i' > /tmp/restore_cleaned.sql && PGPASSWORD='{password}' psql -U {_user} -d {_databaseName} -f /tmp/restore_cleaned.sql 2>&1 | grep -v 'already exists' || true\"",
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
            
            var allOutput = output + error;
            
            var ignoreableErrors = new[]
            {
                "already exists",
                "duplicate key",
                "multiple primary keys",
                "constraint.*already exists"
            };
            
            var hasIgnorableError = ignoreableErrors.Any(err => 
                allOutput.Contains(err, StringComparison.OrdinalIgnoreCase));
            
            var hasFatalError = allOutput.Contains("FATAL:", StringComparison.OrdinalIgnoreCase) ||
                               (process.ExitCode != 0 && !hasIgnorableError && 
                                !string.IsNullOrWhiteSpace(allOutput.Trim()));
            
            if (hasFatalError)
            {
                var errorMessage = string.IsNullOrWhiteSpace(error) ? output : error;
                if (!string.IsNullOrWhiteSpace(allOutput))
                {
                    errorMessage = allOutput;
                }
                throw new InvalidOperationException($"PostgreSQL restore failed with exit code {process.ExitCode}: {errorMessage}");
            }
        }, cancellationToken);
    }
    
    /// <summary>
    /// Restores PostgreSQL database using local psql command
    /// </summary>
    private async Task RestoreLocalAsync(string backupFilePath, string password, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            var tempCleanedPath = Path.Combine(Path.GetTempPath(), $"restore_cleaned_{Guid.NewGuid():N}.sql");
            
            try
            {
                var cleanedContent = File.ReadAllLines(backupFilePath)
                    .Where(line => !line.StartsWith("\\restrict", StringComparison.OrdinalIgnoreCase))
                    .Select(line => 
                    {
                        if (line.TrimStart().StartsWith("CREATE SCHEMA", StringComparison.OrdinalIgnoreCase) &&
                            !line.Contains("IF NOT EXISTS", StringComparison.OrdinalIgnoreCase))
                        {
                            return line.Replace("CREATE SCHEMA", "CREATE SCHEMA IF NOT EXISTS", StringComparison.OrdinalIgnoreCase);
                        }
                        return line;
                    })
                    .ToArray();
                File.WriteAllLines(tempCleanedPath, cleanedContent);
                
                var escapedPath = tempCleanedPath.Replace("'", "'\"'\"'");
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "psql",
                    Arguments = $"-h {_server} -p {_port} -U {_user} -d {_databaseName} -f \"{escapedPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                processInfo.Environment["PGPASSWORD"] = password;
                
                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start psql process. Make sure PostgreSQL client tools are installed and psql is in PATH.");
                }
                
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                
                process.WaitForExit(600000);
                
                var allOutput = output + error;
                
                var ignoreableErrors = new[]
                {
                    "already exists",
                    "duplicate key",
                    "multiple primary keys",
                    "constraint.*already exists"
                };
                
                var hasIgnorableError = ignoreableErrors.Any(err => 
                    allOutput.Contains(err, StringComparison.OrdinalIgnoreCase));
                
                var hasFatalError = allOutput.Contains("FATAL:", StringComparison.OrdinalIgnoreCase) ||
                                   (process.ExitCode != 0 && !hasIgnorableError && 
                                    !string.IsNullOrWhiteSpace(allOutput.Trim()));
                
                if (hasFatalError)
                {
                    var errorMessage = string.IsNullOrWhiteSpace(error) ? output : error;
                    if (!string.IsNullOrWhiteSpace(allOutput))
                    {
                        errorMessage = allOutput;
                    }
                    throw new InvalidOperationException($"PostgreSQL restore failed with exit code {process.ExitCode}: {errorMessage}");
                }
            }
            finally
            {
                if (File.Exists(tempCleanedPath))
                {
                    try { File.Delete(tempCleanedPath); } catch { }
                }
            }
        }, cancellationToken);
    }
    
    /// <summary>
    /// Finds PostgreSQL Docker container name by checking port 5432
    /// </summary>
    private async Task<string?> FindPostgreSqlContainerNameAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "ps --filter \"publish=5432\" --format \"{{.Names}}\"",
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
                _logger?.LogError(ex, "Failed to find PostgreSQL container: {Error}", ex.Message);
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
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(*) 
            FROM information_schema.tables 
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema') 
            AND table_type = 'BASE TABLE'";
        
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

