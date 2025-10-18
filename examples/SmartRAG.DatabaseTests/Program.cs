using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartRAG.Enums;
using SmartRAG.Extensions;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
    private static IAIService? _aiService;

        private static async Task Main(string[] args)
        {
            // Enable UTF-8 encoding for console to display emojis correctly
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
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
            var sqliteDbPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "TestDatabase.db");
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
            _aiService = _serviceProvider.GetService<IAIService>();
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
                    case "7":
                        await CreateMySqlDatabase();
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
            Console.WriteLine("7. 🐬 Create MySQL Test Database");
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
            Console.WriteLine("  • Show me the items with highest values");
            Console.WriteLine("  • Find records where foreign key ID is 1 and show related data");
            Console.WriteLine("  • How many records match the criteria from both databases?");
            Console.WriteLine("  • What is the total of all numeric values?");
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
                Console.WriteLine("📝 ANSWER:");
                Console.WriteLine("─────────────────────────────────────────────────────────────────");
                Console.ResetColor();
                Console.WriteLine(response.Answer);
                Console.WriteLine();

                if (response.Sources.Any())
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("📚 Sources:");
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
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("🧪 Automatic Test Queries");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();

            // Generate dynamic test queries based on current database schemas
            Console.WriteLine("📊 Analyzing database schemas to generate test queries...");
            var testQueries = await GenerateTestQueries();

            if (testQueries.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  No test queries could be generated. Please ensure databases are connected.");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Generated {testQueries.Count} cross-database test queries");
            Console.ResetColor();
            
            // Show query categories breakdown
            var categoryBreakdown = testQueries.GroupBy(q => q.Category.Split(' ')[0]).Select(g => $"{g.Key} ({g.Count()})");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  Categories: {string.Join(", ", categoryBreakdown)}");
            Console.ResetColor();
            Console.WriteLine();

            // Ask user how many tests to run
            Console.Write($"How many tests to run? (1-{testQueries.Count}, Enter for all): ");
            var input = Console.ReadLine();
            var testCount = testQueries.Count;
            
            if (!string.IsNullOrWhiteSpace(input) && int.TryParse(input, out var parsed) && parsed > 0 && parsed <= testQueries.Count)
            {
                testCount = parsed;
            }

            Console.WriteLine();

            // Track failed queries with SQL details
            var failedQueries = new List<(TestQuery Query, string Error, string GeneratedSQL)>();
            var successCount = 0;

            for (int i = 0; i < testCount; i++)
            {
                var testQuery = testQueries[i];
                
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[{i + 1}/{testCount}] {testQuery.Category}");
                Console.WriteLine($"  Query: {testQuery.Query}");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine($"  Databases: {testQuery.DatabaseName}");
                Console.ResetColor();

                try
                {
                    var response = await _multiDbCoordinator!.QueryMultipleDatabasesAsync(testQuery.Query, maxResults: 5);

                    // Check if the response indicates an error
                    if (response.Answer.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
                        response.Answer.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                        response.Answer.Contains("SQLite Error", StringComparison.OrdinalIgnoreCase) ||
                        response.Answer.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"⚠️  Query Failed: {response.Answer}");
                        Console.ResetColor();
                        
                        // Extract SQL info from error message if available
                        var sqlInfo = ExtractSQLFromError(response.Answer);
                        failedQueries.Add((testQuery, response.Answer, sqlInfo));
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✓ Answer: {response.Answer}");
                        Console.ResetColor();
                        successCount++;
                    }

                    if (response.Sources.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"  Source: {string.Join(", ", response.Sources.Select(s => s.FileName))}");
                        Console.ResetColor();
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ Exception: {ex.Message}");
                    Console.ResetColor();
                    
                    failedQueries.Add((testQuery, ex.Message, string.Empty));
                }

                Console.WriteLine();
                if (i < testCount - 1)
                {
                    await Task.Delay(500);
                }
            }

            // Display summary
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("📊 TEST SUMMARY");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ Successful: {successCount}/{testCount}");
            Console.ResetColor();
            
            if (failedQueries.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Failed: {failedQueries.Count}/{testCount}");
                Console.ResetColor();
                Console.WriteLine();
                
                Console.WriteLine("═══════════════════════════════════════════════════════════════════");
                Console.WriteLine("🔴 FAILED QUERIES (for analysis):");
                Console.WriteLine("═══════════════════════════════════════════════════════════════════");
                
                for (int i = 0; i < failedQueries.Count; i++)
                {
                    var failed = failedQueries[i];
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[FAIL #{i + 1}]");
                    Console.ResetColor();
                    Console.WriteLine($"Category: {failed.Query.Category}");
                    Console.WriteLine($"Databases: {failed.Query.DatabaseName}");
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"User Query:");
                    Console.WriteLine($"  {failed.Query.Query}");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error Details:");
                    Console.WriteLine($"  {failed.Error}");
                    Console.ResetColor();
                    
                    if (!string.IsNullOrEmpty(failed.GeneratedSQL))
                    {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"SQL Analysis:");
                        Console.WriteLine($"  {failed.GeneratedSQL}");
                        Console.ResetColor();
                    }
                    
                    Console.WriteLine("─────────────────────────────────────────────────────────────────");
                }
                
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("💡 Copy the failed queries above to share for troubleshooting.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("🎉 All tests passed successfully!");
                Console.ResetColor();
            }
        }

        private static async Task<List<TestQuery>> GenerateTestQueries()
        {
            var testQueries = new List<TestQuery>();

            try
            {
                var schemas = await _schemaAnalyzer!.GetAllSchemasAsync();
                
                if (schemas.Count < 2)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⚠️  Need at least 2 databases for cross-database tests. Currently have: {schemas.Count}");
                    Console.ResetColor();
                    return testQueries;
                }

                // Ask AI to generate intelligent test queries based on schema
                var aiGeneratedQueries = await GenerateAITestQueries(schemas);
                if (aiGeneratedQueries.Count > 0)
                {
                    testQueries.AddRange(aiGeneratedQueries);
                }

                // Also add some schema-based queries as fallback
                testQueries.AddRange(await GenerateSchemaBasedQueries(schemas));

                // Shuffle for variety
                var random = new Random();
                testQueries = testQueries.OrderBy(x => random.Next()).ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating test queries");
            }

            return testQueries;
        }

        private static async Task<List<TestQuery>> GenerateAITestQueries(List<DatabaseSchemaInfo> schemas)
        {
            var queries = new List<TestQuery>();

            try
            {
                // Build schema summary for AI
                var schemaPrompt = BuildSchemaPromptForAI(schemas);
                
                // Add randomization to get different queries each time
                var random = new Random();
                var queryCountVariation = random.Next(8, 15); // 8-14 queries each time
                var focusAreas = new[] 
                { 
                    "aggregations and calculations",
                    "data correlations and relationships", 
                    "temporal comparisons and trends",
                    "filtering and grouping across databases",
                    "comprehensive data analysis"
                };
                var selectedFocus = focusAreas[random.Next(focusAreas.Length)];
                
                var aiPrompt = $@"{schemaPrompt}

Based on the database schemas above, generate {queryCountVariation} intelligent, MEANINGFUL cross-database test queries.

🎯 YOUR TASK:
Analyze the schemas and generate REALISTIC questions that require data from MULTIPLE databases.

CRITICAL REQUIREMENTS:
1. EVERY query MUST use at least 2 databases
2. Queries should be SPECIFIC and MEANINGFUL (not generic calculations)
3. Focus on foreign key relationships between databases
4. Generate questions based on actual table/column names
5. Think about data relationships and correlations
6. Each database should be used in at least one query

📊 HOW TO GENERATE MEANINGFUL QUERIES:

Step 1: Identify relationships
  - Look for FK columns (ID columns referencing other tables)
  - Find which tables connect across databases

Step 2: Analyze columns to understand data
  - Numeric columns (decimal/int) → ask about calculations and aggregations
  - Text columns with categories → ask about grouping and filtering
  - Date/Time columns → ask about temporal analysis
  - Status/Type/Category columns → ask about grouping
  - ID columns → usually foreign keys for relationships
  
Step 3: Create meaningful questions (NOT technical column references)
  GOOD examples:
   - What is the total value by grouping column from both databases?
   - Which records have values below threshold when comparing databases?
   - What is the average of numeric column per category?
   - Which items show highest values when combining data sources?
  
  BAD examples to AVOID:
   - Calculate combined totals using ColumnA and ColumnB
   - Compare data between Table1 and Table2
   - Show all records with foreign key relationships
   - Calculate totals using generic column references
  
CRITICAL RULES:
  1. Questions must be about DATA RELATIONSHIPS not technical names
  2. NEVER mention specific database names in questions
  3. NEVER mention specific column names in questions  
  4. Use generic terms: items, records, data, values, totals, categories

Good question: What is the total value for items in first category?
Bad question: Calculate combined values from Database1 and Database2
Bad question: Join TableA with TableB using ForeignKeyID

Step 4: Ensure cross-database requirement
  - Question should NEED data from multiple DBs
  - Can't be answered from a single database

EXAMPLE THOUGHT PROCESS:

If schema has tables with:
  - Database1: TableA with ForeignKeyColumn linking to Database2
  - Database1: TableA with NumericColumn1 (numeric data type)
  - Database2: TableB with NumericColumn2 (numeric data type)
  - Database2: TableB with CategoryColumn (text grouping)

Good question examples:
  - What is the total value combining numeric columns from both databases?
  - Which categories have the highest totals when combining data sources?
  - Which items show discrepancy between databases?
  - What is the aggregated value grouped by location or category?

Return ONLY a JSON array in this exact format:
[
  {{
    """"category"""": """"Cross-DB Calculation"""",
    """"query"""": """"Specific meaningful question here"""",
    """"databases"""": """"Database1 + Database2""""
  }}
]

Category options (use emoji prefix):
- Cross-DB Join (showing related data)
- Cross-DB Calculation (value/total calculations)
- Cross-DB Filter (filtering by criteria)
- Cross-DB Temporal (time-based analysis)
- Cross-DB Search (finding specific items)
- Coverage Test (validation queries)
- Multi-DB Coverage (comprehensive reports)

Respond ONLY with the JSON array, no other text.";

                var response = await _aiService!.GenerateResponseAsync(aiPrompt, new List<string>());
                
                // Parse AI response
                var jsonStart = response.IndexOf('[');
                var jsonEnd = response.LastIndexOf(']');
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var aiQueries = JsonSerializer.Deserialize<List<JsonElement>>(json);
                    
                    if (aiQueries != null)
                    {
                        foreach (var item in aiQueries)
                        {
                            if (item.TryGetProperty("category", out var cat) && 
                                item.TryGetProperty("query", out var q) && 
                                item.TryGetProperty("databases", out var dbs))
                            {
                                var dbList = dbs.GetString() ?? "";
                                // Ensure it's actually cross-database
                                if (dbList.Contains("+") || dbList.Contains(","))
                                {
                                    queries.Add(new TestQuery
                                    {
                                        Category = cat.GetString() ?? "🧪 Test",
                                        Query = q.GetString() ?? "",
                                        DatabaseName = dbList
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to generate AI test queries, will use schema-based fallback");
            }

            return queries;
        }

        private static string BuildSchemaPromptForAI(List<DatabaseSchemaInfo> schemas)
        {
            var sb = new StringBuilder();
            sb.AppendLine("AVAILABLE DATABASES:");
            sb.AppendLine();

            foreach (var schema in schemas)
            {
                sb.AppendLine($"DATABASE: {schema.DatabaseName} ({schema.DatabaseType})");
                sb.AppendLine($"Description: {schema.Description}");
                sb.AppendLine("TABLES:");
                
                foreach (var table in schema.Tables.Take(5))
                {
                    sb.AppendLine($"  - {schema.DatabaseName}.{table.TableName} ({table.RowCount} rows)");
                    sb.AppendLine($"    Columns: {string.Join(", ", table.Columns.Select(c => $"{c.ColumnName} ({c.DataType})"))}");
                    
                    if (table.ForeignKeys.Any())
                    {
                        foreach (var fk in table.ForeignKeys.Take(3))
                        {
                            sb.AppendLine($"    FK: {fk.ColumnName} → {fk.ReferencedTable}.{fk.ReferencedColumn}");
                        }
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static Task<List<TestQuery>> GenerateSchemaBasedQueries(List<DatabaseSchemaInfo> schemas)
        {
            var queries = new List<TestQuery>();

            // Build database combinations for comprehensive testing
            var databasePairs = new List<(DatabaseSchemaInfo Db1, DatabaseSchemaInfo Db2)>();
            for (int i = 0; i < schemas.Count; i++)
            {
                for (int j = i + 1; j < schemas.Count; j++)
                {
                    databasePairs.Add((schemas[i], schemas[j]));
                }
            }

            // 1. Cross-database queries using foreign key relationships
            var tablesWithForeignKeys = schemas
                .SelectMany(s => s.Tables.Where(t => t.ForeignKeys.Any()).Select(t => new { Schema = s, Table = t }))
                .ToList();

            foreach (var item in tablesWithForeignKeys)
            {
                foreach (var fk in item.Table.ForeignKeys.Take(2))
                {
                    // Find which database has the referenced table
                    var referencedDb = schemas.FirstOrDefault(s => 
                        s.Tables.Any(t => t.TableName.Equals(fk.ReferencedTable, StringComparison.OrdinalIgnoreCase)));

                    if (referencedDb != null && referencedDb.DatabaseId != item.Schema.DatabaseId)
                    {
                        // Create generic cross-database join question
                        var genericQuery = $"Show all records from {item.Table.TableName} with their related {fk.ReferencedTable} information";
                        
                        queries.Add(new TestQuery
                        {
                            Category = "🔗 Cross-DB Join",
                            Query = genericQuery,
                            DatabaseName = $"{item.Schema.DatabaseName} + {referencedDb.DatabaseName}"
                        });
                    }
                }
            }

            // 2. Cross-database aggregation queries
            foreach (var pair in databasePairs)
            {
                // Find tables with numeric columns (generic approach - no specific column name requirements)
                var table1WithNumeric = pair.Db1.Tables.FirstOrDefault(t => 
                    t.Columns.Any(c => IsNumericType(c.DataType) && !c.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase)));
                
                var table2WithNumeric = pair.Db2.Tables.FirstOrDefault(t => 
                    t.Columns.Any(c => IsNumericType(c.DataType) && !c.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase)));

                // Find FK relationship between them
                if (table1WithNumeric != null && table2WithNumeric != null)
                {
                    var hasFkRelation = table1WithNumeric.ForeignKeys.Any(fk => 
                        fk.ReferencedTable.Equals(table2WithNumeric.TableName, StringComparison.OrdinalIgnoreCase));
                    
                    if (hasFkRelation)
                    {
                        // Create generic cross-database calculation question
                        var numericCol1 = table1WithNumeric.Columns.FirstOrDefault(c => IsNumericType(c.DataType) && !c.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase))?.ColumnName;
                        var numericCol2 = table2WithNumeric.Columns.FirstOrDefault(c => IsNumericType(c.DataType) && !c.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase))?.ColumnName;
                        
                        if (!string.IsNullOrEmpty(numericCol1) && !string.IsNullOrEmpty(numericCol2))
                        {
                            queries.Add(new TestQuery
                            {
                                Category = "💰 Cross-DB Calculation",
                                Query = $"Calculate the combined value using {numericCol1} from {table1WithNumeric.TableName} and {numericCol2} from {table2WithNumeric.TableName}",
                                DatabaseName = $"{pair.Db1.DatabaseName} + {pair.Db2.DatabaseName}"
                            });
                        }
                    }
                }
            }

            // 3. Multi-database coverage query
            if (schemas.Count >= 2)
            {
                var allDbNames = string.Join(" + ", schemas.Select(s => s.DatabaseName));
                queries.Add(new TestQuery
                {
                    Category = "🌐 Multi-DB Coverage",
                    Query = "Analyze all available data to find correlations and patterns across all databases",
                    DatabaseName = allDbNames
                });
            }

            // 4. Cross-database temporal analysis
            var tablesWithDates = schemas
                .SelectMany(s => s.Tables.Where(t => 
                    t.Columns.Any(c => c.DataType.Contains("date", StringComparison.OrdinalIgnoreCase) ||
                                      c.DataType.Contains("time", StringComparison.OrdinalIgnoreCase)))
                    .Select(t => new { Schema = s, Table = t }))
                .ToList();

            if (tablesWithDates.Count >= 2)
            {
                var dateTable1 = tablesWithDates[0];
                var dateTable2 = tablesWithDates.FirstOrDefault(t => t.Schema.DatabaseId != dateTable1.Schema.DatabaseId);

                if (dateTable2 != null)
                {
                    queries.Add(new TestQuery
                    {
                        Category = "📅 Cross-DB Temporal",
                        Query = $"What is the timeline correlation between {dateTable1.Table.TableName} and {dateTable2.Table.TableName} records?",
                        DatabaseName = $"{dateTable1.Schema.DatabaseName} + {dateTable2.Schema.DatabaseName}"
                    });
                }
            }

            // 5. Ensure every database is tested at least once
            var databasesUsed = new HashSet<string>();
            foreach (var query in queries)
            {
                var dbNames = query.DatabaseName.Split(new[] { " + " }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var db in dbNames)
                {
                    databasesUsed.Add(db);
                }
            }

            // Add queries for databases not yet covered
            foreach (var schema in schemas)
            {
                if (!databasesUsed.Contains(schema.DatabaseName))
                {
                    var otherDb = schemas.FirstOrDefault(s => s.DatabaseId != schema.DatabaseId);
                    if (otherDb != null)
                    {
                        var table1 = schema.Tables.FirstOrDefault();
                        var table2 = otherDb.Tables.FirstOrDefault();

                        if (table1 != null && table2 != null)
                        {
                            queries.Add(new TestQuery
                            {
                                Category = "✅ Coverage Test",
                                Query = $"Analyze relationship between {table1.TableName} and {table2.TableName}",
                                DatabaseName = $"{schema.DatabaseName} + {otherDb.DatabaseName}"
                            });
                        }
                    }
                }
            }

            return Task.FromResult(queries);
        }

        private static bool IsNumericType(string dataType)
        {
            return dataType.Contains("int", StringComparison.OrdinalIgnoreCase) ||
                   dataType.Contains("decimal", StringComparison.OrdinalIgnoreCase) ||
                   dataType.Contains("numeric", StringComparison.OrdinalIgnoreCase) ||
                   dataType.Contains("float", StringComparison.OrdinalIgnoreCase) ||
                   dataType.Contains("double", StringComparison.OrdinalIgnoreCase) ||
                   dataType.Contains("money", StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractSQLFromError(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
                return string.Empty;

            var sqlInfo = new StringBuilder();
            var issues = new List<string>();

            // Extract "no such column" errors
            if (errorMessage.Contains("no such column", StringComparison.OrdinalIgnoreCase))
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    errorMessage, 
                    @"no such column:\s*'?([^'.\s]+(?:\.[^'.\s]+)?)'?",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (match.Success)
                {
                    issues.Add($"Missing column: {match.Groups[1].Value}");
                }
            }

            // Extract "does not exist" errors
            if (errorMessage.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    errorMessage,
                    @"(Column|Table)\s+'?([^']+)'?\s+does not exist",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    issues.Add($"{match.Groups[1].Value} '{match.Groups[2].Value}' not found in schema");
                }
            }

            // Extract "HAVING clause" errors (SQL syntax)
            if (errorMessage.Contains("HAVING clause", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add("SQL Syntax: HAVING clause used incorrectly (probably on non-aggregate query)");
            }

            // Extract "aggregate" errors
            if (errorMessage.Contains("aggregate", StringComparison.OrdinalIgnoreCase) && 
                errorMessage.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add("SQL Syntax: Aggregate function in WHERE clause (should use HAVING)");
            }

            // Extract "GROUP BY" errors
            if (errorMessage.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase) && 
                errorMessage.Contains("not in", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add("SQL Syntax: Column in SELECT not in GROUP BY clause");
            }

            // Extract "No query generated" errors
            if (errorMessage.Contains("No query generated", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add("Validation failed after 3 retry attempts - could not generate valid SQL");
            }

            // Extract database names from error
            var dbMatches = System.Text.RegularExpressions.Regex.Matches(
                errorMessage,
                @"Database\s+([a-zA-Z0-9_]+):",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Build final message
            if (issues.Any())
            {
                if (dbMatches.Count > 0)
                {
                    foreach (System.Text.RegularExpressions.Match dbMatch in dbMatches)
                    {
                        var dbName = dbMatch.Groups[1].Value;
                        sqlInfo.AppendLine($"[{dbName}]");
                        
                        // Find issues related to this database
                        var relevantIssues = issues.Where(issue => 
                            errorMessage.Substring(dbMatch.Index).Contains(issue, StringComparison.OrdinalIgnoreCase) ||
                            dbMatch.Index == 0 || 
                            issues.Count == 1).ToList();
                        
                        foreach (var issue in relevantIssues.Any() ? relevantIssues : issues)
                        {
                            sqlInfo.AppendLine($"  • {issue}");
                        }
                    }
                }
                else
                {
                    foreach (var issue in issues)
                    {
                        sqlInfo.AppendLine($"• {issue}");
                    }
                }
            }

            // If no specific SQL info found
            if (sqlInfo.Length == 0)
            {
                sqlInfo.AppendLine("Error details not parsed. Check application logs for:");
                sqlInfo.AppendLine("  • Generated SQL queries");
                sqlInfo.AppendLine("  • Validation errors");
                sqlInfo.AppendLine("  • Retry attempts");
            }

            return sqlInfo.ToString().TrimEnd();
        }

        private class TestQuery
        {
            public string Category { get; set; } = string.Empty;
            public string Query { get; set; } = string.Empty;
            public string DatabaseName { get; set; } = string.Empty;
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

                Console.WriteLine("SQL Server test database will be created:");
                Console.WriteLine($"Server: (localdb)\\MSSQLLocalDB");
                Console.WriteLine($"Database: TestDatabaseSql");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  WARNING: If database exists, it will be dropped and recreated!");
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

        private static async Task CreateMySqlDatabase()
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("🐬 Create MySQL Test Database");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");

            try
            {
                var creator = TestDatabaseFactory.GetCreator(SmartRAG.Enums.DatabaseType.MySQL, _configuration);
                var connectionString = creator.GetDefaultConnectionString();

                Console.WriteLine();
                Console.WriteLine($"📝 Database: TestDatabaseMySql");
                Console.WriteLine($"📝 Connection: {connectionString.Replace("Password=2059680", "Password=***")}");
                Console.WriteLine();

                // Create database
                creator.CreateSampleDatabase(connectionString);

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("💡 Next Steps:");
                Console.WriteLine("   1. Verify connection with option 1 (Show Database Connections)");
                Console.WriteLine("   2. Check schema details with option 2 (Show Database Schemas)");
                Console.WriteLine("   3. Test multi-database queries with option 3 or 5");
                Console.ResetColor();

                // Trigger schema refresh
                if (_connectionManager != null && _schemaAnalyzer != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("🔄 Refreshing schema analysis...");
                    
                    var connections = await _connectionManager.GetAllConnectionsAsync();
                    var mySqlConn = connections.FirstOrDefault(c => c.DatabaseType == SmartRAG.Enums.DatabaseType.MySQL);
                    
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
                    Console.WriteLine("   ✓ Schema analysis initiated");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Possible causes:");
                Console.WriteLine("  • MySQL server not installed");
                Console.WriteLine("  • MySQL service not running");
                Console.WriteLine("  • Incorrect username or password");
                Console.WriteLine("  • Port 3306 blocked or in use");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Solution:");
                Console.WriteLine("  1. Install MySQL Server");
                Console.WriteLine("  2. Ensure MySQL service is running");
                Console.WriteLine("  3. Verify credentials (User: root, Password: 2059680)");
                Console.WriteLine("  4. Or set MySQL to 'Enabled: false' in appsettings.json");
            }
        }
    }
}


