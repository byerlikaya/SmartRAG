
namespace SmartRAG.Demo.DatabaseSetup.Creators;

/// <summary>
/// SQL Server test database creator implementation
/// Restores database from backup file based on configuration
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
    private readonly string _configurationName;

    #endregion

    #region Constructor

    public SqlServerTestDatabaseCreator(IConfiguration? configuration = null, ILogger<SqlServerTestDatabaseCreator>? logger = null)
    {
        _configuration = configuration;
        _logger = logger;
        string? server = null;
        string? databaseName = null;
        string? configurationName = null;

        if (_configuration != null)
        {
            var connectionString = FindConnectionStringByType(DatabaseType.SqlServer);

            if (!string.IsNullOrEmpty(connectionString))
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                server = builder.DataSource;
                databaseName = builder.InitialCatalog;
            }
            
            configurationName = FindConfigurationNameByType(DatabaseType.SqlServer);
        }

        _server = server ?? "localhost,1433";
        _databaseName = databaseName ?? "TestDatabase";
        _configurationName = configurationName ?? _databaseName;
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
            var connectionString = FindConnectionStringByType(DatabaseType.SqlServer);

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
            var masterConnectionString = $"Server={_server};Database=master;User Id=sa;Password={GetPassword()};TrustServerCertificate=true;";
            using var connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync(cancellationToken);

            var validatedName = ValidateDatabaseName(_databaseName);
            var escapedNameForString = validatedName.Replace("'", "''");
            
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM sys.databases WHERE name = '{escapedNameForString}'";
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
            _logger?.LogInformation("SQL Server database {DatabaseName} already exists, skipping creation", _databaseName);
            return;
        }

        try
        {
            await CreateDatabaseAsync(cancellationToken);
            await RestoreFromBackupAsync(connectionString, cancellationToken);
            _logger?.LogInformation("SQL Server database {DatabaseName} created successfully", _databaseName);
            await VerifyDatabaseAsync(connectionString, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create SQL Server database {DatabaseName}", _databaseName);
            throw;
        }
    }

    public void CreateSampleDatabase(string connectionString)
    {
        CreateSampleDatabaseAsync(connectionString).GetAwaiter().GetResult();
    }

    #endregion

    #region Private Methods

    private static string EscapeSqlServerIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be null or empty", nameof(identifier));
        
        return $"[{identifier.Replace("]", "]]")}]";
    }

    private static string ValidateDatabaseName(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("Database name cannot be null or empty", nameof(databaseName));
        
        if (!System.Text.RegularExpressions.Regex.IsMatch(databaseName, @"^[a-zA-Z0-9_]+$"))
            throw new ArgumentException("Database name contains invalid characters. Only alphanumeric characters and underscores are allowed.", nameof(databaseName));
        
        return databaseName;
    }

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

    /// <summary>
    /// Creates the SQL Server database, dropping it first if it exists
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    private async Task CreateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var validatedName = ValidateDatabaseName(_databaseName);
        var escapedName = EscapeSqlServerIdentifier(validatedName);
        var escapedNameForString = validatedName.Replace("'", "''");
        
        var masterConnectionString = $"Server={_server};Database=master;User Id=sa;Password={GetPassword()};TrustServerCertificate=true;";

        using (var connection = new SqlConnection(masterConnectionString))
        {
            await connection.OpenAsync(cancellationToken);

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $@"
                    IF EXISTS (SELECT name FROM sys.databases WHERE name = '{escapedNameForString}')
                    BEGIN
                        ALTER DATABASE {escapedName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE {escapedName};
                    END";
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"CREATE DATABASE {escapedName}";
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Restores SQL Server database from backup file
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
        
        var masterConnectionString = $"Server={_server};Database=master;User Id=sa;Password={GetPassword()};TrustServerCertificate=true;";
        
        using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync(cancellationToken);
        
        try
        {
            var sanitizedConfigName = _configurationName.ToLowerInvariant()
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "");
            
            var containerName = await FindSqlServerContainerNameAsync();
            if (string.IsNullOrEmpty(containerName))
            {
                throw new InvalidOperationException("Could not find SQL Server Docker container. Make sure container is running on port 1433.");
            }
            
            var finalBackupPath = $"/var/opt/mssql/data/{sanitizedConfigName}_restore.bak";
            
            var copyResult = await CopyBackupToContainerAsync(backupFilePath, containerName, finalBackupPath);
            if (!copyResult)
            {
                throw new InvalidOperationException($"Failed to copy backup file to container: {containerName}");
            }
            
            await SetFilePermissionsAsync(containerName, finalBackupPath);
            
            var validatedName = ValidateDatabaseName(_databaseName);
            var escapedName = EscapeSqlServerIdentifier(validatedName);
            var escapedPath = finalBackupPath.Replace("'", "''");
            
            var restoreCommand = $@"
                ALTER DATABASE {escapedName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                RESTORE DATABASE {escapedName}
                FROM DISK = N'{escapedPath}'
                WITH REPLACE, RECOVERY, STATS = 10;
                ALTER DATABASE {escapedName} SET MULTI_USER;
            ";
            
            using var command = new SqlCommand(restoreCommand, connection);
            command.CommandTimeout = 600;
            
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error restoring backup file: {BackupPath}. Error: {Error}", backupFilePath, ex.Message);
            throw;
        }
    }
    
    /// <summary>
    /// Finds SQL Server Docker container name by checking port 1433
    /// </summary>
    private async Task<string?> FindSqlServerContainerNameAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "ps --filter \"publish=1433\" --format \"{{.Names}}\"",
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
                _logger?.LogError(ex, "Failed to find SQL Server container: {Error}", ex.Message);
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
    /// Sets file permissions in container so SQL Server can read the backup file
    /// </summary>
    private async Task SetFilePermissionsAsync(string containerName, string containerPath)
    {
        var validatedContainerName = ValidateContainerName(containerName);
        var validatedContainerPath = ValidatePath(containerPath, nameof(containerPath));
        
        await Task.Run(async () =>
        {
            try
            {
                var chownProcessInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    ArgumentList = { "exec", "-u", "root", validatedContainerName, "chown", "mssql:mssql", validatedContainerPath },
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using var chownProcess = System.Diagnostics.Process.Start(chownProcessInfo);
                if (chownProcess != null)
                {
                    await chownProcess.WaitForExitAsync();
                }
                
                var chmodProcessInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    ArgumentList = { "exec", "-u", "root", validatedContainerName, "chmod", "644", validatedContainerPath },
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using var chmodProcess = System.Diagnostics.Process.Start(chmodProcessInfo);
                if (chmodProcess == null)
                {
                    throw new InvalidOperationException("Failed to start docker exec process for setting file permissions");
                }
                
                var output = chmodProcess.StandardOutput.ReadToEnd();
                var error = chmodProcess.StandardError.ReadToEnd();
                
                await chmodProcess.WaitForExitAsync();
                
                if (chmodProcess.ExitCode != 0)
                {
                    var errorMessage = string.IsNullOrWhiteSpace(error) ? output : error;
                    _logger?.LogWarning("Failed to set file permissions in container: {Error}. This may not be critical if SQL Server can still read the file.", errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to set file permissions in container: {Error}. This may not be critical if SQL Server can still read the file.", ex.Message);
            }
        });
    }
    
    /// <summary>
    /// Finds the backup file path relative to the project root
    /// </summary>
    /// <param name="fileName">Backup file name</param>
    /// <returns>Full path to the backup file</returns>
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
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sys.tables";
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
    /// Generates backup file name from configuration name (lowercase, sanitized)
    /// </summary>
    private string GetBackupFileName()
    {
        var sanitizedName = _configurationName.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");
        return $"{sanitizedName}.backup.bak";
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
