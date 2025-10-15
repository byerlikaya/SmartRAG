using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartRAG.Enums;
using SmartRAG.Extensions;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.DatabaseTests
{
    internal class Program
    {
    private static IServiceProvider? _serviceProvider;
    private static ILogger<Program>? _logger;
    private static IConfiguration? _configuration;
        private static IDatabaseConnectionManager? _connectionManager;
        private static IDatabaseSchemaAnalyzer? _schemaAnalyzer;
        private static IMultiDatabaseQueryCoordinator? _multiDbCoordinator;

        private static async Task Main(string[] args)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   SmartRAG Multi-Database RAG Test System                        ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            try
            {
                // Setup test databases
                Console.WriteLine("📁 Preparing test databases...");
                await SetupTestDatabases();
                Console.WriteLine();

                // Load configuration
                LoadConfiguration();

                // Initialize services
                await InitializeServices();

                // Run main menu
                await RunMainMenu();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error starting application");
                Console.WriteLine($"❌ ERROR: {ex.Message}");
            }
            finally
            {
                if (_serviceProvider is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        private static async Task SetupTestDatabases()
        {
            // Create SQLite database
            var sqliteDbPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "ProductCatalog.db");
            var sqliteDir = Path.GetDirectoryName(sqliteDbPath);
            
            if (!string.IsNullOrEmpty(sqliteDir) && !Directory.Exists(sqliteDir))
            {
                Directory.CreateDirectory(sqliteDir);
            }

            var sqliteCreator = new SqliteTestDatabaseCreator();
            
            if (!File.Exists(sqliteDbPath))
            {
                Console.WriteLine("   Creating SQLite test database...");
                sqliteCreator.CreateSampleDatabase($"Data Source={sqliteDbPath}");
                Console.WriteLine("   ✓ SQLite database created");
            }
            else
            {
                Console.WriteLine("   ✓ SQLite test database exists");
            }

            Console.WriteLine();

            await Task.CompletedTask;
        }

        private static void LoadConfiguration()
        {
            Console.WriteLine("⚙️  Loading configuration...");

            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();

            Console.WriteLine("   ✓ Configuration loaded");
        }

        private static async Task InitializeServices()
        {
            Console.WriteLine("🔧 Starting SmartRAG services...");

            if (_configuration == null)
            {
                throw new InvalidOperationException("Configuration not loaded yet!");
            }

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(_configuration);

            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddFilter("SmartRAG", LogLevel.Debug);
            });

            services.AddSmartRag(_configuration, options =>
            {
                options.StorageProvider = StorageProvider.InMemory;
                options.AIProvider = AIProvider.Anthropic;
            });

            _serviceProvider = services.BuildServiceProvider();

            _connectionManager = _serviceProvider.GetService<IDatabaseConnectionManager>();
            _schemaAnalyzer = _serviceProvider.GetService<IDatabaseSchemaAnalyzer>();
            _multiDbCoordinator = _serviceProvider.GetService<IMultiDatabaseQueryCoordinator>();
            _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();

            Console.WriteLine("   ✓ Services loaded");
            Console.WriteLine();

            // Initialize database connections
            if (_connectionManager != null)
            {
                Console.WriteLine("🔗 Initializing database connections...");
                await _connectionManager.InitializeAsync();

                var connections = await _connectionManager.GetAllConnectionsAsync();
                Console.WriteLine($"   ✓ {connections.Count} database connections loaded");
                Console.WriteLine();

                // Validate connections
                foreach (var conn in connections)
                {
                    var dbId = await _connectionManager.GetDatabaseIdAsync(conn);
                    var isValid = await _connectionManager.ValidateConnectionAsync(dbId);
                    
                    Console.Write($"   • {conn.Name ?? dbId} ({conn.DatabaseType}): ");
                    
                    if (isValid)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✓");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("✗");
                        Console.ResetColor();
                    }
                }
                Console.WriteLine();

                // Display schema analysis results
                var schemas = await _schemaAnalyzer!.GetAllSchemasAsync();
                var completed = schemas.Where(s => s.Status == SchemaAnalysisStatus.Completed).ToList();
                Console.WriteLine($"📊 Schema Analysis: {completed.Count}/{schemas.Count} databases analyzed successfully");
                
                foreach (var schema in schemas)
                {
                    Console.WriteLine($"   • {schema.DatabaseName}: {schema.Tables.Count} tables, {schema.TotalRowCount} total rows");
                }
                
            Console.WriteLine();
            }
        }

        private static async Task RunMainMenu()
        {
            while (true)
            {
                ShowMainMenu();
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await ShowDatabaseConnections();
                        break;
                    case "2":
                        await ShowDatabaseSchemas();
                        break;
                    case "3":
                        await RunMultiDatabaseQuery();
                        break;
                    case "4":
                        await AnalyzeQueryIntent();
                        break;
                    case "5":
                        await RunTestQueries();
                        break;
                    case "6":
                        await CreateSqlServerDatabase();
                        break;
                    case "0":
                        Console.WriteLine("\n👋 Goodbye!");
                        return;
                    default:
                        Console.WriteLine("❌ Invalid selection!");
                        break;
                }

                Console.WriteLine();
                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            }
        }

        private static void ShowMainMenu()
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("📋 MAIN MENU");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("1. 🔗 Show Database Connections");
            Console.WriteLine("2. 📊 Show Database Schemas");
            Console.WriteLine("3. 🤖 Multi-Database Query (AI)");
            Console.WriteLine("4. 🔬 Query Analysis (SQL Generation)");
            Console.WriteLine("5. 🧪 Automatic Test Queries");
            Console.WriteLine("6. 🗄️  Create SQL Server Test Database");
            Console.WriteLine("0. 🚪 Exit");
            Console.WriteLine();
            Console.Write("Selection: ");
        }

        private static async Task ShowDatabaseConnections()
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("🔗 Database Connection Status");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");

            var connections = await _connectionManager!.GetAllConnectionsAsync();
            
            foreach (var conn in connections)
            {
                var dbId = await _connectionManager.GetDatabaseIdAsync(conn);
                var isValid = await _connectionManager.ValidateConnectionAsync(dbId);
                var schema = await _schemaAnalyzer!.GetSchemaAsync(dbId);

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"📂 {conn.Name ?? dbId}");
                Console.ResetColor();
                Console.WriteLine($"   Tip: {conn.DatabaseType}");
                Console.WriteLine($"   Connection: {(isValid ? "✓ Active" : "✗ Inactive")}");
                
                if (schema != null)
                {
                    Console.WriteLine($"   Schema: {schema.Status}");
                    Console.WriteLine($"   Tables: {schema.Tables.Count}");
                    Console.WriteLine($"   Total Rows: {schema.TotalRowCount:N0}");
                }

                if (!string.IsNullOrEmpty(conn.Description))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"   Description: {conn.Description}");
                    Console.ResetColor();
                }
            }
        }

        private static async Task ShowDatabaseSchemas()
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("📊 Detailed Database Schemas");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");

            var schemas = await _schemaAnalyzer!.GetAllSchemasAsync();

            foreach (var schema in schemas)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"🗄️  {schema.DatabaseName} ({schema.DatabaseType})");
                Console.ResetColor();
                Console.WriteLine($"    Status: {schema.Status}");
                Console.WriteLine($"    Total Rows: {schema.TotalRowCount:N0}");

                if (!string.IsNullOrEmpty(schema.AISummary))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"    AI Summary: {schema.AISummary}");
                    Console.ResetColor();
                }

                Console.WriteLine($"\n    Tables ({schema.Tables.Count}):");
                foreach (var table in schema.Tables)
                {
                    Console.WriteLine($"      📋 {table.TableName} ({table.RowCount} rows, {table.Columns.Count} columns)");
                    Console.WriteLine($"         Columns: {string.Join(", ", table.Columns.Select(c => c.ColumnName))}");

                    if (table.ForeignKeys.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"         Relationships: {string.Join(", ", table.ForeignKeys.Select(fk => $"{fk.ColumnName} → {fk.ReferencedTable}"))}");
                        Console.ResetColor();
                    }
                }
            }
        }

        private static async Task RunMultiDatabaseQuery()
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("🤖 Multi-Database Smart Query");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine("Sample Questions:");
            Console.WriteLine("  • What is the best-selling product?");
            Console.WriteLine("  • What products did customer 1 buy and at what price?");
            Console.WriteLine("  • How many customers from Istanbul placed orders?");
            Console.WriteLine("  • What is the total sales amount?");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Your question: ");
            Console.ResetColor();

            var query = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(query))
            {
                Console.WriteLine("❌ Empty query entered!");
                return;
            }

            try
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("⏳ Analyzing databases and preparing query...");
                Console.ResetColor();

                var response = await _multiDbCoordinator!.QueryMultipleDatabasesAsync(query, maxResults: 10);

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("📝 CEVAP:");
                Console.WriteLine("─────────────────────────────────────────────────────────────────");
                Console.ResetColor();
                Console.WriteLine(response.Answer);
                Console.WriteLine();

                if (response.Sources.Any())
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("📚 Kaynaklar:");
                    foreach (var source in response.Sources)
                    {
                        Console.WriteLine($"   • {source.FileName}");
                    }
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during multi-database query");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static async Task AnalyzeQueryIntent()
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("🔬 Query Analysis (SQL Generation - Without Execution)");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.Write("Question to analyze: ");

            var query = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(query))
            {
                Console.WriteLine("❌ Empty query entered!");
                return;
            }

            try
            {
                Console.WriteLine();
                Console.WriteLine("⏳ AI analyzing...");
                
                var intent = await _multiDbCoordinator!.AnalyzeQueryIntentAsync(query);
                intent = await _multiDbCoordinator.GenerateDatabaseQueriesAsync(intent);

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"AI Understanding: {intent.QueryUnderstanding}");
                Console.WriteLine($"Confidence Level: {intent.Confidence:P0}");
                Console.ResetColor();

                if (!string.IsNullOrEmpty(intent.Reasoning))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"Reasoning: {intent.Reasoning}");
                    Console.ResetColor();
                }

                Console.WriteLine();
                Console.WriteLine($"Databases to Query: {intent.DatabaseQueries.Count}");
                Console.WriteLine();

                foreach (var dbQuery in intent.DatabaseQueries)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"📊 {dbQuery.DatabaseName}");
                    Console.ResetColor();
                    Console.WriteLine($"   Tables: {string.Join(", ", dbQuery.RequiredTables)}");
                    Console.WriteLine($"   Purpose: {dbQuery.Purpose}");
                    Console.WriteLine($"   Priority: {dbQuery.Priority}");

                    if (!string.IsNullOrEmpty(dbQuery.GeneratedQuery))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"\n   Generated SQL:");
                        Console.WriteLine($"   {dbQuery.GeneratedQuery.Replace("\n", "\n   ")}");
                        Console.ResetColor();
                    }

                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during query analysis");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static async Task RunTestQueries()
        {
            var testQueries = new[]
            {
                "What is the best-selling product?",
                "What products did customer 1 buy?",
                "How many customers are from Istanbul?",
                "What is the total sales amount?",
                "Which orders are pending?",
                "What products are in the electronics category?"
            };

            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("🧪 Automatic Test Queries");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();

            for (int i = 0; i < testQueries.Length; i++)
            {
                var query = testQueries[i];
                
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[{i + 1}/{testQueries.Length}] Question: {query}");
                Console.ResetColor();

                try
                {
                    var response = await _multiDbCoordinator!.QueryMultipleDatabasesAsync(query, maxResults: 5);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Cevap: {response.Answer}");
                    Console.ResetColor();

                    if (response.Sources.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"Kaynak: {string.Join(", ", response.Sources.Select(s => s.FileName))}");
                        Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.ResetColor();
                }

                Console.WriteLine();
                if (i < testQueries.Length - 1)
                {
                    await Task.Delay(500);
                }
            }

            Console.WriteLine("✅ Test queries completed!");
        }

        private static async Task CreateSqlServerDatabase()
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("🗄️  Create SQL Server Test Database");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();

            try
            {
                var sqlServerCreator = new SqlServerTestDatabaseCreator(_configuration!);
                var connectionString = sqlServerCreator.GetDefaultConnectionString();

                Console.WriteLine("SalesManagement database will be created:");
                Console.WriteLine($"Server: (localdb)\\MSSQLLocalDB");
                Console.WriteLine($"Database: SalesManagement");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  WARNING: If SalesManagement database exists, it will be dropped and recreated!");
                Console.ResetColor();
            Console.WriteLine();
                Console.Write("Do you want to continue? (Y/N): ");
                
                var confirm = Console.ReadLine();
                if (confirm?.ToUpper() != "Y")
                {
                    Console.WriteLine("❌ Cancelled.");
                    return;
            }
            
            Console.WriteLine();
                sqlServerCreator.CreateSampleDatabase(connectionString);
            
            Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("💡 Database created! Now:");
                Console.WriteLine("   1. Return to main menu");
                Console.WriteLine("   2. Check connections with option 1");
                Console.WriteLine("   3. Test multi-database queries with option 3 or 5");
                Console.ResetColor();

                // Trigger schema refresh
                if (_connectionManager != null && _schemaAnalyzer != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("🔄 Refreshing schema analysis...");
                    
                    var connections = await _connectionManager.GetAllConnectionsAsync();
                    var sqlServerConn = connections.FirstOrDefault(c => c.DatabaseType == SmartRAG.Enums.DatabaseType.SqlServer);
                    
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
                    Console.WriteLine("   ✓ Schema analysis initiated");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine();
                Console.WriteLine("Possible causes:");
                Console.WriteLine("  • SQL Server LocalDB not installed");
                Console.WriteLine("  • LocalDB service not running");
                Console.WriteLine("  • Connection permission denied");
                Console.ResetColor();
            Console.WriteLine();
                Console.WriteLine("Solution:");
                Console.WriteLine("  1. Install SQL Server Express + LocalDB");
                Console.WriteLine("  2. Run 'sqllocaldb start MSSQLLocalDB' command");
                Console.WriteLine("  3. Or set SQL Server to 'Enabled: false' in appsettings.json");
            }
        }
    }
}

