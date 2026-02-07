using SmartRAG.Interfaces.Health;
using SmartRAG.Models.Health;

namespace SmartRAG.Demo.Handlers.DatabaseHandlers;

/// <summary>
/// Handler for database-related operations
/// </summary>
public class DatabaseHandler(
    IConsoleService console,
    IConfiguration configuration,
    IDatabaseConnectionManager connectionManager,
    IDatabaseSchemaAnalyzer schemaAnalyzer,
    IHealthCheckService healthCheckService,
    ILogger<DatabaseHandler>? logger = null) : IDatabaseHandler
{
    #region Fields

    private readonly IConsoleService _console = console;
    private readonly IConfiguration _configuration = configuration;
    private readonly IDatabaseConnectionManager _connectionManager = connectionManager;
    private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer = schemaAnalyzer;
    private readonly IHealthCheckService _healthCheckService = healthCheckService;
    private readonly ILogger<DatabaseHandler>? _logger = logger;

    #endregion

    #region Public Methods

    public async Task RunHealthCheckAsync()
    {
        _console.WriteSectionHeader("🔧 System Health Check");

        System.Console.WriteLine("Checking all services...");
        System.Console.WriteLine();

        var result = await _healthCheckService.RunFullHealthCheckAsync(CancellationToken.None);

        System.Console.WriteLine("AI Services:");
        System.Console.Write($"  • {result.Ai.ServiceName} (AI Provider)... ");
        _console.WriteHealthStatus(result.Ai, inline: true);
        if (!result.Ai.IsHealthy && !string.IsNullOrEmpty(result.Ai.Details))
        {
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine($"     {result.Ai.Details}");
            System.Console.ResetColor();
        }

        System.Console.WriteLine();
        System.Console.WriteLine("Storage Services:");
        if (result.Storage != null)
        {
            System.Console.Write($"  • {result.Storage.ServiceName} (Vector Store)... ");
            _console.WriteHealthStatus(result.Storage, inline: true);
            if (!result.Storage.IsHealthy && !string.IsNullOrEmpty(result.Storage.Details))
            {
                System.Console.ForegroundColor = ConsoleColor.DarkGray;
                System.Console.WriteLine($"     {result.Storage.Details}");
                System.Console.ResetColor();
            }
        }

        System.Console.Write($"  • {result.Conversation.ServiceName} (Cache/Conversation History)... ");
        _console.WriteHealthStatus(result.Conversation, inline: true);
        if (!result.Conversation.IsHealthy && !string.IsNullOrEmpty(result.Conversation.Details))
        {
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine($"     {result.Conversation.Details}");
            System.Console.ResetColor();
        }

        System.Console.WriteLine();
        System.Console.WriteLine("Databases:");
        foreach (var dbStatus in result.Databases)
        {
            System.Console.Write($"  • {dbStatus.ServiceName}... ");
            _console.WriteHealthStatus(dbStatus, inline: true);
            if (!dbStatus.IsHealthy && !string.IsNullOrEmpty(dbStatus.Details))
            {
                System.Console.ForegroundColor = ConsoleColor.DarkGray;
                System.Console.WriteLine($"     {dbStatus.Details}");
                System.Console.ResetColor();
            }
        }

        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine("💡 TIP: Start all services with: docker-compose up -d");
        System.Console.ResetColor();
    }

    public async Task ShowConnectionsAsync()
    {
        _console.WriteSectionHeader("🔗 Database Connection Status");

        var connections = await _connectionManager.GetAllConnectionsAsync(CancellationToken.None);
        var needsSetup = new List<string>();

        foreach (var conn in connections)
        {
            var dbId = await _connectionManager.GetDatabaseIdAsync(conn, CancellationToken.None);

            bool isValid;
            try
            {
                isValid = await _connectionManager.ValidateConnectionAsync(dbId, CancellationToken.None);
            }
            catch
            {
                isValid = false;
            }

            var schema = await _schemaAnalyzer.GetSchemaAsync(dbId, CancellationToken.None);

            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine($"📂 {conn.Name ?? dbId}");
            System.Console.ResetColor();
            System.Console.WriteLine($"   Type: {conn.DatabaseType}");
            System.Console.WriteLine($"   Connection: {(isValid ? "✓ Active" : "✗ Inactive")}");

            if (schema != null)
            {
                System.Console.WriteLine($"   Schema: {schema.Status}");
                System.Console.WriteLine($"   Tables: {schema.Tables.Count}");
                System.Console.WriteLine($"   Total Rows: {schema.TotalRowCount:N0}");

                if (schema.Tables.Count == 0 && conn.DatabaseType != DatabaseType.SQLite)
                {
                    needsSetup.Add(GetSetupInstruction(conn.DatabaseType));
                }
            }
        }

        if (needsSetup.Any())
        {
            System.Console.WriteLine();
            _console.WriteWarning("ACTION REQUIRED");
            System.Console.WriteLine("───────────────────────────────────────────────────────────────────");
            System.Console.WriteLine("The following databases need to be created:");
            System.Console.WriteLine();

            foreach (var instruction in needsSetup.Distinct())
            {
                System.Console.WriteLine($"   {instruction}");
            }

            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine("💡 Use the menu options above to create databases automatically!");
            System.Console.ResetColor();
        }
    }

    public async Task ShowSchemasAsync()
    {
        _console.WriteSectionHeader("📊 Detailed Database Schemas");

        var schemas = await _schemaAnalyzer.GetAllSchemasAsync(CancellationToken.None);

        foreach (var schema in schemas)
        {
            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine($"🗄️  {schema.DatabaseName} ({schema.DatabaseType})");
            System.Console.ResetColor();
            System.Console.WriteLine($"    Status: {schema.Status}");
            System.Console.WriteLine($"    Total Rows: {schema.TotalRowCount:N0}");

            System.Console.WriteLine($"\n    Tables ({schema.Tables.Count}):");
            foreach (var table in schema.Tables)
            {
                System.Console.WriteLine($"      📋 {table.TableName} ({table.RowCount} rows, {table.Columns.Count} columns)");
                System.Console.WriteLine($"         Columns: {string.Join(", ", table.Columns.Select(c => c.ColumnName))}");

                if (table.ForeignKeys.Any())
                {
                    System.Console.ForegroundColor = ConsoleColor.DarkGray;
                    System.Console.WriteLine($"         Relationships: {string.Join(", ", table.ForeignKeys.Select(fk => $"{fk.ColumnName} → {fk.ReferencedTable}"))}");
                    System.Console.ResetColor();
                }
            }
        }
    }

    public async Task CreateDatabaseAsync(DatabaseType databaseType)
    {
        switch (databaseType)
        {
            case DatabaseType.SqlServer:
                await CreateSqlServerDatabaseAsync();
                break;
            case DatabaseType.MySQL:
                await CreateMySqlDatabaseAsync();
                break;
            case DatabaseType.PostgreSQL:
                await CreatePostgreSqlDatabaseAsync();
                break;
            case DatabaseType.SQLite:
                await CreateSqliteDatabaseAsync();
                break;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Gets setup instruction message for the specified database type
    /// </summary>
    /// <param name="databaseType">Database type</param>
    /// <returns>Setup instruction message</returns>
    private static string GetSetupInstruction(DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.SqlServer => "Select option 3 → Create SQL Server Test Database",
            DatabaseType.MySQL => "Select option 4 → Create MySQL Test Database",
            DatabaseType.PostgreSQL => "Select option 5 → Create PostgreSQL Test Database",
            DatabaseType.SQLite => "Select option 6 → Create SQLite Test Database",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Creates SQLite test database using the configured creator
    /// </summary>
    private async Task CreateSqliteDatabaseAsync()
    {
        _console.WriteSectionHeader("📦 Create SQLite Test Database");

        try
        {
            var creator = new SqliteTestDatabaseCreator(_configuration);
            var connectionString = creator.GetDefaultConnectionString();
            
            var databaseName = ExtractDatabaseName(connectionString, DatabaseType.SQLite);

            System.Console.WriteLine("SQLite test database will be created:");
            System.Console.WriteLine($"Database: {databaseName}");
            System.Console.WriteLine();
            _console.WriteWarning("WARNING: If database exists, it will be dropped and recreated!");
            System.Console.WriteLine();

            var confirm = _console.ReadConfirmation("Do you want to continue?", "Y");
            if (confirm?.ToUpper() != "Y")
            {
                _console.WriteInfo("Cancelled");
                return;
            }

            System.Console.WriteLine();
            creator.CreateSampleDatabase(connectionString);

            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine("💡 Next Steps:");
            System.Console.WriteLine("   1. Verify connection with option 1 (Show Database Connections)");
            System.Console.WriteLine("   2. Check schema details with option 7 (Show Database Schemas)");
            System.Console.WriteLine("   3. Test multi-database queries with option 10 (Multi-Database Query)");
            System.Console.ResetColor();

            System.Console.WriteLine();
            System.Console.WriteLine("🔄 Refreshing schema analysis...");

            var connections = await _connectionManager.GetAllConnectionsAsync(CancellationToken.None);
            var sqliteConn = connections.FirstOrDefault(c => c.DatabaseType == DatabaseType.SQLite);

            if (sqliteConn != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _schemaAnalyzer.AnalyzeDatabaseSchemaAsync(sqliteConn, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error analyzing SQLite database schema");
                    }
                });
            }

            await Task.Delay(2000);
            System.Console.WriteLine("   ✓ Schema analysis initiated");
        }
        catch (Exception ex)
        {
            _console.WriteError($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates SQL Server test database using the configured creator
    /// </summary>
    private async Task CreateSqlServerDatabaseAsync()
    {
        _console.WriteSectionHeader("🗄️ Create SQL Server Test Database");

        try
        {
            var sqlServerCreator = new SqlServerTestDatabaseCreator(_configuration);
            var connectionString = sqlServerCreator.GetDefaultConnectionString();
            
            var databaseName = ExtractDatabaseName(connectionString, DatabaseType.SqlServer);

            System.Console.WriteLine("SQL Server test database will be created:");
            System.Console.WriteLine($"Server: localhost,1433 (Docker)");
            System.Console.WriteLine($"Database: {databaseName}");
            System.Console.WriteLine();
            _console.WriteWarning("WARNING: If database exists, it will be dropped and recreated!");
            System.Console.WriteLine();

            var confirm = _console.ReadConfirmation("Do you want to continue?", "Y");
            if (confirm?.ToUpper() != "Y")
            {
                _console.WriteInfo("Cancelled");
                return;
            }

            System.Console.WriteLine();
            sqlServerCreator.CreateSampleDatabase(connectionString);

            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine("💡 Next Steps:");
            System.Console.WriteLine("   1. Verify connection with option 1 (Show Database Connections)");
            System.Console.WriteLine("   2. Check schema details with option 7 (Show Database Schemas)");
            System.Console.WriteLine("   3. Test multi-database queries with option 10 (Multi-Database Query)");
            System.Console.ResetColor();

            System.Console.WriteLine();
            System.Console.WriteLine("🔄 Refreshing schema analysis...");

            var connections = await _connectionManager.GetAllConnectionsAsync(CancellationToken.None);
            var sqlServerConn = connections.FirstOrDefault(c => c.DatabaseType == DatabaseType.SqlServer);

            if (sqlServerConn != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _schemaAnalyzer.AnalyzeDatabaseSchemaAsync(sqlServerConn, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error analyzing SQL Server database schema");
                    }
                });
            }

            await Task.Delay(2000);
            System.Console.WriteLine("   ✓ Schema analysis initiated");
        }
        catch (Exception ex)
        {
            _console.WriteError($"Error: {ex.Message}");
            System.Console.WriteLine();
            System.Console.WriteLine("Possible causes:");
            System.Console.WriteLine("  • SQL Server Docker container not running");
            System.Console.WriteLine("  • Port 1433 blocked or in use");
        }
    }

    /// <summary>
    /// Creates MySQL test database using the configured creator
    /// </summary>
    private async Task CreateMySqlDatabaseAsync()
    {
        _console.WriteSectionHeader("🐬 Create MySQL Test Database");

        try
        {
            var creator = new MySqlTestDatabaseCreator(_configuration);
            var connectionString = creator.GetDefaultConnectionString();
            
            var databaseName = ExtractDatabaseName(connectionString, DatabaseType.MySQL);

            System.Console.WriteLine("MySQL test database will be created:");
            System.Console.WriteLine($"Database: {databaseName}");
            System.Console.WriteLine();
            _console.WriteWarning("WARNING: If database exists, it will be dropped and recreated!");
            System.Console.WriteLine();

            var confirm = _console.ReadConfirmation("Do you want to continue?", "Y");
            if (confirm?.ToUpper() != "Y")
            {
                _console.WriteInfo("Cancelled");
                return;
            }

            System.Console.WriteLine();
            creator.CreateSampleDatabase(connectionString);

            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine("💡 Next Steps:");
            System.Console.WriteLine("   1. Verify connection with option 1 (Show Database Connections)");
            System.Console.WriteLine("   2. Check schema details with option 7 (Show Database Schemas)");
            System.Console.WriteLine("   3. Test multi-database queries with option 10 (Multi-Database Query)");
            System.Console.ResetColor();

            System.Console.WriteLine();
            System.Console.WriteLine("🔄 Refreshing schema analysis...");

            var connections = await _connectionManager.GetAllConnectionsAsync(CancellationToken.None);
            var mySqlConn = connections.FirstOrDefault(c => c.DatabaseType == DatabaseType.MySQL);

            if (mySqlConn != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _schemaAnalyzer.AnalyzeDatabaseSchemaAsync(mySqlConn, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error analyzing MySQL database schema");
                    }
                });
            }

            await Task.Delay(2000);
            System.Console.WriteLine("   ✓ Schema analysis initiated");
        }
        catch (Exception ex)
        {
            _console.WriteError($"Error: {ex.Message}");
            System.Console.WriteLine();
            System.Console.WriteLine("Possible causes:");
            System.Console.WriteLine("  • MySQL Docker container not running");
            System.Console.WriteLine("  • Port 3306 blocked or in use");
        }
    }

    /// <summary>
    /// Creates PostgreSQL test database using the configured creator
    /// </summary>
    private async Task CreatePostgreSqlDatabaseAsync()
    {
        _console.WriteSectionHeader("🐘 Create PostgreSQL Test Database");

        try
        {
            var creator = TestDatabaseFactory.GetCreator(DatabaseType.PostgreSQL, _configuration);
            var connectionString = creator.GetDefaultConnectionString();
            
            var databaseName = ExtractDatabaseName(connectionString, DatabaseType.PostgreSQL);

            System.Console.WriteLine("PostgreSQL test database will be created:");
            System.Console.WriteLine($"Database: {databaseName}");
            System.Console.WriteLine();
            _console.WriteWarning("WARNING: If database exists, it will be dropped and recreated!");
            System.Console.WriteLine();

            var confirm = _console.ReadConfirmation("Do you want to continue?", "Y");
            if (confirm?.ToUpper() != "Y")
            {
                _console.WriteInfo("Cancelled");
                return;
            }

            System.Console.WriteLine();
            creator.CreateSampleDatabase(connectionString);

            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine("💡 Next Steps:");
            System.Console.WriteLine("   1. Verify connection with option 1 (Show Database Connections)");
            System.Console.WriteLine("   2. Check schema details with option 7 (Show Database Schemas)");
            System.Console.WriteLine("   3. Test multi-database queries with option 10 (Multi-Database Query)");
            System.Console.ResetColor();

            System.Console.WriteLine();
            System.Console.WriteLine("🔄 Refreshing schema analysis...");

            var connections = await _connectionManager.GetAllConnectionsAsync(CancellationToken.None);
            var postgresConn = connections.FirstOrDefault(c => c.DatabaseType == DatabaseType.PostgreSQL);

            if (postgresConn != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _schemaAnalyzer.AnalyzeDatabaseSchemaAsync(postgresConn, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error analyzing PostgreSQL database schema");
                    }
                });
            }

            await Task.Delay(2000);
            System.Console.WriteLine("   ✓ Schema analysis initiated");
        }
        catch (Exception ex)
        {
            _console.WriteError($"Error: {ex.Message}");
            System.Console.WriteLine();
            System.Console.WriteLine("Possible causes:");
            System.Console.WriteLine("  • PostgreSQL server not running");
            System.Console.WriteLine("  • Port 5432 blocked or in use");
        }
    }

    /// <summary>
    /// Extracts database name from connection string based on database type
    /// </summary>
    private static string ExtractDatabaseName(string connectionString, DatabaseType databaseType)
    {
        try
        {
            return databaseType switch
            {
                DatabaseType.SqlServer => ExtractSqlServerDatabaseName(connectionString),
                DatabaseType.MySQL => ExtractMySqlDatabaseName(connectionString),
                DatabaseType.PostgreSQL => ExtractPostgreSqlDatabaseName(connectionString),
                DatabaseType.SQLite => ExtractSqliteDatabaseName(connectionString),
                _ => "TestDatabase"
            };
        }
        catch
        {
            return "TestDatabase";
        }
    }
    
    private static string ExtractSqlServerDatabaseName(string connectionString)
    {
        try
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
            return !string.IsNullOrEmpty(builder.InitialCatalog) ? builder.InitialCatalog : "TestDatabase";
        }
        catch
        {
            return ExtractDatabaseNameFromString(connectionString, "Database=") ?? "TestDatabase";
        }
    }
    
    private static string ExtractMySqlDatabaseName(string connectionString)
    {
        try
        {
            var builder = new MySqlConnector.MySqlConnectionStringBuilder(connectionString);
            return !string.IsNullOrEmpty(builder.Database) ? builder.Database : "TestDatabase";
        }
        catch
        {
            return ExtractDatabaseNameFromString(connectionString, "Database=") ?? "TestDatabase";
        }
    }
    
    private static string ExtractPostgreSqlDatabaseName(string connectionString)
    {
        try
        {
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
            return !string.IsNullOrEmpty(builder.Database) ? builder.Database : "TestDatabase";
        }
        catch
        {
            return ExtractDatabaseNameFromString(connectionString, "Database=") ?? "TestDatabase";
        }
    }
    
    private static string ExtractSqliteDatabaseName(string connectionString)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            connectionString,
            @"Data Source=([^;]+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (match.Success)
        {
            var path = match.Groups[1].Value.Trim();
            var fileName = System.IO.Path.GetFileName(path);
            return !string.IsNullOrEmpty(fileName) ? System.IO.Path.GetFileNameWithoutExtension(fileName) : "TestDatabase";
        }
        
        return "TestDatabase";
    }
    
    private static string? ExtractDatabaseNameFromString(string connectionString, string key)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            connectionString,
            $@"{key}([^;]+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    #endregion
}

