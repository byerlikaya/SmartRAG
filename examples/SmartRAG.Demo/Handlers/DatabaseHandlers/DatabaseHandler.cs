using Microsoft.Extensions.Configuration;
using SmartRAG.Demo.DatabaseSetup.Creators;
using SmartRAG.Demo.Models;
using SmartRAG.Demo.Services;
using SmartRAG.Demo.Services.Console;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;

namespace SmartRAG.Demo.Handlers.DatabaseHandlers;

/// <summary>
/// Handler for database-related operations
/// </summary>
public class DatabaseHandler(
    IConsoleService console,
    IConfiguration configuration,
    IDatabaseConnectionManager connectionManager,
    IDatabaseSchemaAnalyzer schemaAnalyzer) : IDatabaseHandler
{
    #region Fields

    private readonly IConsoleService _console = console;
    private readonly IConfiguration _configuration = configuration;
    private readonly IDatabaseConnectionManager _connectionManager = connectionManager;
    private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer = schemaAnalyzer;

    #endregion

    #region Public Methods

    public async Task RunHealthCheckAsync()
    {
        _console.WriteSectionHeader("🔧 System Health Check");

        var healthCheck = new HealthCheckService();

        System.Console.WriteLine("Checking all services...");
        System.Console.WriteLine();

        System.Console.Write($"📦 Storage ({_configuration["SmartRAG:StorageProvider"] ?? "Redis"}).... ");
        var redisStatus = await healthCheck.CheckRedisAsync();
        _console.WriteHealthStatus(redisStatus, inline: true);

        System.Console.WriteLine();
        System.Console.WriteLine("Databases:");

        var connections = await _connectionManager.GetAllConnectionsAsync();
        foreach (var conn in connections)
        {
            System.Console.Write($"  • {conn.Name} ({conn.DatabaseType})... ");

            HealthStatus? dbStatus = null;
            try
            {
                dbStatus = conn.DatabaseType switch
                {
                    DatabaseType.SQLite => await healthCheck.CheckSqliteAsync(conn.ConnectionString),
                    DatabaseType.SqlServer => await healthCheck.CheckSqlServerAsync(conn.ConnectionString),
                    DatabaseType.MySQL => await healthCheck.CheckMySqlAsync(conn.ConnectionString),
                    DatabaseType.PostgreSQL => await healthCheck.CheckPostgreSqlAsync(conn.ConnectionString),
                    _ => null
                };

                if (dbStatus != null)
                {
                    _console.WriteHealthStatus(dbStatus, inline: true);
                }
            }
            catch
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("✗ Error");
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

        var connections = await _connectionManager.GetAllConnectionsAsync();
        var needsSetup = new List<string>();

        foreach (var conn in connections)
        {
            var dbId = await _connectionManager.GetDatabaseIdAsync(conn);

            bool isValid;
            try
            {
                isValid = await _connectionManager.ValidateConnectionAsync(dbId);
            }
            catch
            {
                isValid = false;
            }

            var schema = await _schemaAnalyzer.GetSchemaAsync(dbId);

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

            if (!string.IsNullOrEmpty(conn.Description))
            {
                System.Console.ForegroundColor = ConsoleColor.DarkGray;
                System.Console.WriteLine($"   Description: {conn.Description}");
                System.Console.ResetColor();
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

        var schemas = await _schemaAnalyzer.GetAllSchemasAsync();

        foreach (var schema in schemas)
        {
            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine($"🗄️  {schema.DatabaseName} ({schema.DatabaseType})");
            System.Console.ResetColor();
            System.Console.WriteLine($"    Status: {schema.Status}");
            System.Console.WriteLine($"    Total Rows: {schema.TotalRowCount:N0}");

            if (!string.IsNullOrEmpty(schema.AISummary))
            {
                System.Console.ForegroundColor = ConsoleColor.Cyan;
                System.Console.WriteLine($"    AI Summary: {schema.AISummary}");
                System.Console.ResetColor();
            }

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
        }
    }

    #endregion

    #region Private Methods

    private static string GetSetupInstruction(DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.SqlServer => "Select option 3 → Create SQL Server Test Database",
            DatabaseType.MySQL => "Select option 4 → Create MySQL Test Database",
            DatabaseType.PostgreSQL => "Select option 5 → Create PostgreSQL Test Database",
            _ => string.Empty
        };
    }

    private async Task CreateSqlServerDatabaseAsync()
    {
        _console.WriteSectionHeader("🗄️ Create SQL Server Test Database");

        try
        {
            var sqlServerCreator = new SqlServerTestDatabaseCreator(_configuration);
            var connectionString = sqlServerCreator.GetDefaultConnectionString();

            System.Console.WriteLine("SQL Server test database will be created:");
            System.Console.WriteLine($"Server: localhost,1433 (Docker)");
            System.Console.WriteLine($"Database: SalesManagement");
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
            System.Console.WriteLine("💡 Database created! Now:");
            System.Console.WriteLine("   1. Return to main menu");
            System.Console.WriteLine("   2. Check connections with option 1");
            System.Console.WriteLine("   3. Test multi-database queries");
            System.Console.ResetColor();

            System.Console.WriteLine();
            System.Console.WriteLine("🔄 Refreshing schema analysis...");

            var connections = await _connectionManager.GetAllConnectionsAsync();
            var sqlServerConn = connections.FirstOrDefault(c => c.DatabaseType == DatabaseType.SqlServer);

            if (sqlServerConn != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _schemaAnalyzer.AnalyzeDatabaseSchemaAsync(sqlServerConn);
                    }
                    catch { }
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

    private async Task CreateMySqlDatabaseAsync()
    {
        _console.WriteSectionHeader("🐬 Create MySQL Test Database");

        try
        {
            var creator = TestDatabaseFactory.GetCreator(DatabaseType.MySQL, _configuration);
            var connectionString = creator.GetDefaultConnectionString();

            System.Console.WriteLine();
            System.Console.WriteLine($"📝 Database: InventoryManagement");
            System.Console.WriteLine($"📝 Connection: {connectionString.Replace("Password=mysql123", "Password=***")}");
            System.Console.WriteLine();

            creator.CreateSampleDatabase(connectionString);

            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine("💡 Next Steps:");
            System.Console.WriteLine("   1. Verify connection with option 1");
            System.Console.WriteLine("   2. Check schema details with option 6");
            System.Console.WriteLine("   3. Test multi-database queries");
            System.Console.ResetColor();

            System.Console.WriteLine();
            System.Console.WriteLine("🔄 Refreshing schema analysis...");

            var connections = await _connectionManager.GetAllConnectionsAsync();
            var mySqlConn = connections.FirstOrDefault(c => c.DatabaseType == DatabaseType.MySQL);

            if (mySqlConn != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _schemaAnalyzer.AnalyzeDatabaseSchemaAsync(mySqlConn);
                    }
                    catch { }
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

    private async Task CreatePostgreSqlDatabaseAsync()
    {
        _console.WriteSectionHeader("🐘 Create PostgreSQL Test Database");

        try
        {
            var creator = TestDatabaseFactory.GetCreator(DatabaseType.PostgreSQL, _configuration);
            var connectionString = creator.GetDefaultConnectionString();

            System.Console.WriteLine();
            System.Console.WriteLine($"📝 Database: LogisticsManagement");
            System.Console.WriteLine($"📝 Connection: {connectionString.Replace("Password=postgres123", "Password=***")}");
            System.Console.WriteLine();

            creator.CreateSampleDatabase(connectionString);

            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine("💡 Next Steps:");
            System.Console.WriteLine("   1. Verify connection with option 1");
            System.Console.WriteLine("   2. Check schema details with option 6");
            System.Console.WriteLine("   3. Test multi-database queries");
            System.Console.ResetColor();

            System.Console.WriteLine();
            System.Console.WriteLine("🔄 Refreshing schema analysis...");

            var connections = await _connectionManager.GetAllConnectionsAsync();
            var postgresConn = connections.FirstOrDefault(c => c.DatabaseType == DatabaseType.PostgreSQL);

            if (postgresConn != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _schemaAnalyzer.AnalyzeDatabaseSchemaAsync(postgresConn);
                    }
                    catch { }
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

    #endregion
}

