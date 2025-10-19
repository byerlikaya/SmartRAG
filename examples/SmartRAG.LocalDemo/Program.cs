using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartRAG.Enums;
using SmartRAG.Extensions;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System.Text;
using System.Text.Json;

namespace SmartRAG.LocalDemo
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
        private static IDocumentService? _documentService;
        private static IDocumentSearchService? _documentSearchService;
        private static string _selectedLanguage = "English";
        private static bool _useLocalEnvironment = true;
        private static AIProvider _selectedAIProvider = AIProvider.Custom;
        private static StorageProvider _selectedStorageProvider = StorageProvider.Qdrant;
        private static AudioProvider _selectedAudioProvider = AudioProvider.Whisper;

        private static async Task Main(string[] args)
        {
            // Enable UTF-8 encoding for console to display emojis correctly
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   SmartRAG Local Demo - Fully Local RAG System                    ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            try
            {
                // Setup test databases
                await SetupTestDatabases();

                // Load configuration
                LoadConfiguration();

                // Select environment (Local or Cloud)
                SelectEnvironment();

                // Select language for queries and responses
                SelectLanguage();

                // Initialize services
                await InitializeServices();

                // Run main menu
                await RunMainMenu();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ ERROR: {ex.Message}");
                Console.WriteLine($"\nError Type: {ex.GetType().Name}");
                Console.WriteLine($"\nStack Trace:");
                Console.WriteLine(ex.StackTrace);

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"\nInner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Type: {ex.InnerException.GetType().Name}");
                    if (ex.InnerException.StackTrace != null)
                    {
                        Console.WriteLine($"Inner Stack:\n{ex.InnerException.StackTrace}");
                    }
                }
                Console.ResetColor();

                _logger?.LogError(ex, "Error starting application");
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
                Console.Write("📁 Creating SQLite test database... ");
                sqliteCreator.CreateSampleDatabase($"Data Source={sqliteDbPath}");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓");
                Console.ResetColor();
            }

            await Task.CompletedTask;
        }

        private static void LoadConfiguration()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();
        }

        private static void SelectEnvironment()
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("🚀 ENVIRONMENT SELECTION");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine("Choose your deployment environment:");
            Console.WriteLine();
            Console.WriteLine("1. ☁️  CLOUD Environment (Cloud Services)");
            Console.WriteLine("   • AI: Gemini / OpenAI / AzureOpenAI / Anthropic / Custom");
            Console.WriteLine("   • Vector Store: Qdrant Cloud");
            Console.WriteLine("   • Cache: Redis Cloud");
            Console.WriteLine("   • Databases: Can use cloud or local databases");
            Console.WriteLine("   ⚡ High performance with cloud AI models");
            Console.WriteLine();
            Console.WriteLine("2. 🏠 LOCAL Environment (100% Local - No Cloud Required)");
            Console.WriteLine("   • AI: Ollama (running on localhost)");
            Console.WriteLine("   • Vector Store: Qdrant (local docker container)");
            Console.WriteLine("   • Cache: Redis (local docker container)");
            Console.WriteLine("   • Databases: Local SQL Server, MySQL, PostgreSQL, SQLite");
            Console.WriteLine("   ✅ GDPR/KVKK compliant - All data stays on your machine");
            Console.WriteLine();
            Console.Write("Selection (default: Local): ");

            var choice = Console.ReadLine();

            _useLocalEnvironment = choice switch
            {
                "1" => false,
                "2" or "" => true,
                _ => true
            };

            if (_useLocalEnvironment)
            {
                _selectedAIProvider = AIProvider.Custom;  // Custom provider configured for Ollama
                _selectedStorageProvider = StorageProvider.Redis;
                _selectedAudioProvider = AudioProvider.Whisper;  // Local audio transcription

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ LOCAL Environment selected");
                Console.WriteLine("  AI Provider: Ollama (via Custom provider)");
                Console.WriteLine("  Storage: Redis (Document storage)");
                Console.WriteLine("  Audio: Whisper.net (Local transcription)");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  ⚠️  Note: Make sure Ollama endpoint is configured in appsettings");
                Console.WriteLine("     (AI:Custom:Endpoint = http://localhost:11434)");
                Console.ResetColor();
            }
            else
            {
                // Cloud environment - let user choose provider
                Console.WriteLine();
                Console.WriteLine("Select AI Provider:");
                Console.WriteLine("1. Google Gemini");
                Console.WriteLine("2. OpenAI GPT");
                Console.WriteLine("3. Azure OpenAI");
                Console.WriteLine("4. Anthropic Claude");
                Console.WriteLine("5. Custom Provider");
                Console.Write("Selection (default: Anthropic): ");

                var aiChoice = Console.ReadLine();
                _selectedAIProvider = aiChoice switch
                {
                    "1" => AIProvider.Gemini,
                    "2" => AIProvider.OpenAI,
                    "3" => AIProvider.AzureOpenAI,
                    "4" or "" => AIProvider.Anthropic,
                    "5" => AIProvider.Custom,
                    _ => AIProvider.Anthropic
                };

                // For document storage, use Redis (more reliable for GetAllDocuments)
                _selectedStorageProvider = StorageProvider.Redis;
                _selectedAudioProvider = AudioProvider.GoogleCloud;  // Cloud audio transcription

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("✓ CLOUD Environment selected");
                Console.WriteLine($"  AI Provider: {_selectedAIProvider}");
                Console.WriteLine("  Storage: Redis (Document storage)");
                Console.WriteLine("  Audio: Google Cloud Speech-to-Text");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        private static void SelectLanguage()
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("🌍 LANGUAGE SELECTION");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine("Please select the language for test queries and AI responses:");
            Console.WriteLine();
            Console.WriteLine("1. 🇬🇧 English");
            Console.WriteLine("2. 🇩🇪 German (Deutsch)");
            Console.WriteLine("3. 🇹🇷 Turkish (Türkçe)");
            Console.WriteLine("4. 🇷🇺 Russian (Русский)");
            Console.WriteLine("5. 🌐 Other (specify)");
            Console.WriteLine();
            Console.Write("Selection (default: English): ");

            var choice = Console.ReadLine();

            _selectedLanguage = choice switch
            {
                "1" or "" => "English",
                "2" => "German",
                "3" => "Turkish",
                "4" => "Russian",
                "5" => GetCustomLanguage(),
                _ => "English"
            };

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Language set to: {_selectedLanguage}");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static string GetCustomLanguage()
        {
            Console.WriteLine();
            Console.Write("Enter language name (e.g., French, Spanish, Italian): ");
            var customLang = Console.ReadLine();
            return string.IsNullOrWhiteSpace(customLang) ? "English" : customLang.Trim();
        }

        private static async Task InitializeServices()
        {
            Console.WriteLine("🔧 Initializing SmartRAG...");
            Console.WriteLine();

            if (_configuration == null)
            {
                throw new InvalidOperationException("Configuration not loaded yet!");
            }

            try
            {
                var services = new ServiceCollection();
                services.AddSingleton<IConfiguration>(_configuration);

                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddConfiguration(_configuration.GetSection("Logging"));
                });

                Console.WriteLine($"   → Configuring {_selectedAIProvider} provider...");
                Console.WriteLine($"   → Configuring {_selectedStorageProvider} storage...");

                services.AddSmartRag(_configuration, options =>
                {
                    options.StorageProvider = _selectedStorageProvider;
                    options.AIProvider = _selectedAIProvider;
                    options.AudioProvider = _selectedAudioProvider;
                });

                Console.WriteLine("   → Building service provider...");
                _serviceProvider = services.BuildServiceProvider();

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Services initialized successfully");
                Console.WriteLine($"  AI Provider: {_selectedAIProvider}");
                Console.WriteLine($"  Storage Provider: {_selectedStorageProvider}");
                Console.WriteLine($"  Audio Provider: {_selectedAudioProvider}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Service initialization failed: {ex.Message}");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
                Console.ResetColor();
                throw;
            }

            _connectionManager = _serviceProvider.GetService<IDatabaseConnectionManager>();
            _schemaAnalyzer = _serviceProvider.GetService<IDatabaseSchemaAnalyzer>();
            _multiDbCoordinator = _serviceProvider.GetService<IMultiDatabaseQueryCoordinator>();
            _aiService = _serviceProvider.GetService<IAIService>();
            _documentService = _serviceProvider.GetService<IDocumentService>();
            _documentSearchService = _serviceProvider.GetService<IDocumentSearchService>();
            _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();

            // Initialize database connections
            if (_connectionManager != null)
            {
                await _connectionManager.InitializeAsync();

                var connections = await _connectionManager.GetAllConnectionsAsync();
                // Display schema analysis results
                var schemas = await _schemaAnalyzer!.GetAllSchemasAsync();
                var completed = schemas.Where(s => s.Status == SchemaAnalysisStatus.Completed && s.Tables.Count > 0).ToList();
                var needsSetup = schemas.Where(s => s.Tables.Count == 0).ToList();

                Console.WriteLine($"📊 Ready: {completed.Count} database(s) with data");

                foreach (var schema in completed)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"   ✓ {schema.DatabaseName}: {schema.Tables.Count} tables, {schema.TotalRowCount} total rows");
                    Console.ResetColor();
                }

                Console.WriteLine();

                // Show setup instructions if databases need creation
                if (needsSetup.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️  SETUP REQUIRED");
                    Console.WriteLine("═══════════════════════════════════════════════════════════════════");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.WriteLine("Some databases are not created yet. Please follow these steps:");
                    Console.WriteLine();

                    var instructionsShown = new HashSet<string>();

                    foreach (var schema in needsSetup)
                    {
                        var instruction = schema.DatabaseType switch
                        {
                            DatabaseType.SqlServer => "6. 🗄️  Create SQL Server Test Database → SalesManagement",
                            DatabaseType.MySQL => "7. 🐬 Create MySQL Test Database → InventoryManagement",
                            DatabaseType.PostgreSQL => "8. 🐘 Create PostgreSQL Test Database → LogisticsManagement",
                            _ => null
                        };

                        if (instruction != null && !instructionsShown.Contains(instruction))
                        {
                            Console.WriteLine($"   {instruction}");
                            instructionsShown.Add(instruction);
                        }
                    }

                    if (instructionsShown.Any())
                    {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("💡 TIP: Use the menu options above to create databases automatically!");
                        Console.ResetColor();
                        Console.WriteLine();
                    }
                }
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
                        await RunSystemHealthCheck();
                        break;
                    case "3":
                        await CreateSqlServerDatabase();
                        break;
                    case "4":
                        await CreateMySqlDatabase();
                        break;
                    case "5":
                        await CreatePostgreSqlDatabase();
                        break;
                    case "6":
                        await ShowDatabaseSchemas();
                        break;
                    case "7":
                        await AnalyzeQueryIntent();
                        break;
                    case "8":
                        await RunTestQueries();
                        break;
                    case "9":
                        await RunMultiDatabaseQuery();
                        break;
                    case "10":
                        await SetupOllamaModels();
                        break;
                    case "11":
                        await TestVectorStore();
                        break;
                    case "12":
                        await UploadDocuments();
                        break;
                    case "13":
                        await ListDocuments();
                        break;
                    case "14":
                        await RunMultiModalQuery();
                        break;
                    case "15":
                        await ClearAllDocuments();
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
            Console.WriteLine("1.  🔗 Show Database Connections");
            Console.WriteLine("2.  🔧 System Health Check");
            Console.WriteLine("3.  🗄️ Create SQL Server Test Database");
            Console.WriteLine("4.  🐬 Create MySQL Test Database");
            Console.WriteLine("5.  🐘 Create PostgreSQL Test Database");
            Console.WriteLine("6.  📊 Show Database Schemas");
            Console.WriteLine("7.  🔬 Query Analysis (SQL Generation)");
            Console.WriteLine("8.  🧪 Automatic Test Queries");
            Console.WriteLine("9.  🤖 Multi-Database Query (AI)");
            Console.WriteLine("10. 🤖 Setup Ollama Models");
            Console.WriteLine("11. 📦 Test Vector Store (InMemory/FileSystem/Redis/SQLite/Qdrant)");
            Console.WriteLine("12. 📄 Upload Documents (PDF, Word, Excel, Images, Audio)");
            Console.WriteLine("13. 📚 List Uploaded Documents");
            Console.WriteLine("14. 🎯 Multi-Modal RAG (Documents + Databases)");
            Console.WriteLine("15. 🗑️ Clear All Documents");
            Console.WriteLine("0.  🚪 Exit");
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
            var needsSetup = new List<string>();

            foreach (var conn in connections)
            {
                var dbId = await _connectionManager.GetDatabaseIdAsync(conn);

                // Suppress validation warnings by catching them
                bool isValid;
                try
                {
                    var originalLogLevel = _logger?.IsEnabled(LogLevel.Warning) ?? false;
                    isValid = await _connectionManager.ValidateConnectionAsync(dbId);
                }
                catch
                {
                    isValid = false;
                }

                var schema = await _schemaAnalyzer!.GetSchemaAsync(dbId);

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"📂 {conn.Name ?? dbId}");
                Console.ResetColor();
                Console.WriteLine($"   Type: {conn.DatabaseType}");
                Console.WriteLine($"   Connection: {(isValid ? "✓ Active" : "✗ Inactive")}");

                if (schema != null)
                {
                    Console.WriteLine($"   Schema: {schema.Status}");
                    Console.WriteLine($"   Tables: {schema.Tables.Count}");
                    Console.WriteLine($"   Total Rows: {schema.TotalRowCount:N0}");

                    // Track databases that need setup
                    if (schema.Tables.Count == 0 && conn.DatabaseType != DatabaseType.SQLite)
                    {
                        needsSetup.Add(GetSetupInstruction(conn.DatabaseType));
                    }
                }

                if (!string.IsNullOrEmpty(conn.Description))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"   Description: {conn.Description}");
                    Console.ResetColor();
                }
            }

            // Show setup instructions if needed
            if (needsSetup.Any())
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  ACTION REQUIRED");
                Console.WriteLine("───────────────────────────────────────────────────────────────────");
                Console.ResetColor();
                Console.WriteLine("The following databases need to be created:");
                Console.WriteLine();

                foreach (var instruction in needsSetup.Distinct())
                {
                    Console.WriteLine($"   {instruction}");
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("💡 Use the menu options above to create databases automatically!");
                Console.ResetColor();
            }
        }

        private static string GetSetupInstruction(DatabaseType databaseType)
        {
            return databaseType switch
            {
                DatabaseType.SqlServer => "Select option 6 → Create SQL Server Test Database",
                DatabaseType.MySQL => "Select option 7 → Create MySQL Test Database",
                DatabaseType.PostgreSQL => "Select option 8 → Create PostgreSQL Test Database",
                _ => string.Empty
            };
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
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Language: {_selectedLanguage}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Sample Questions:");
            Console.WriteLine("  • Show me the items with highest values");
            Console.WriteLine("  • Find records where foreign key ID is 1 and show related data");
            Console.WriteLine("  • How many records match the criteria from both databases?");
            Console.WriteLine("  • What is the total of all numeric values?");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"Your question ({_selectedLanguage}): ");
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

                // Add language instruction to query
                var languageInstructedQuery = $"{query}\n\n[IMPORTANT: Respond in {_selectedLanguage} language]";

                var response = await _multiDbCoordinator!.QueryMultipleDatabasesAsync(languageInstructedQuery, maxResults: 10);

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
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Language: {_selectedLanguage}");
            Console.ResetColor();
            Console.WriteLine();
            Console.Write($"Question to analyze ({_selectedLanguage}): ");

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

                // Add language instruction to query
                var languageInstructedQuery = $"{query}\n\n[IMPORTANT: Analyze and respond in {_selectedLanguage} language]";

                var intent = await _multiDbCoordinator!.AnalyzeQueryIntentAsync(languageInstructedQuery);
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
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"🌍 Query Language: {_selectedLanguage}");
            Console.ResetColor();
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
                if (!string.IsNullOrEmpty(testQuery.DatabaseTypes))
                {
                    Console.WriteLine($"  Types: {testQuery.DatabaseTypes}");
                }
                Console.ResetColor();

                try
                {
                    // Add language instruction for test queries
                    var languageInstructedQuery = $"{testQuery.Query}\n\n[IMPORTANT: Respond in {_selectedLanguage} language]";

                    var response = await _multiDbCoordinator!.QueryMultipleDatabasesAsync(languageInstructedQuery, maxResults: 5);

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
                        var schemas = await _schemaAnalyzer!.GetAllSchemasAsync();
                        var sqlInfo = ExtractSQLFromError(response.Answer, schemas);
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
                    if (!string.IsNullOrEmpty(failed.Query.DatabaseTypes))
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"Database Types: {failed.Query.DatabaseTypes}");
                        Console.ResetColor();
                    }
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

🌍 CRITICAL LANGUAGE REQUIREMENT - READ CAREFULLY:
YOU MUST write EVERY SINGLE query in {_selectedLanguage} language!

If {_selectedLanguage} is Turkish:
- Write: """"İki veritabanındaki toplam değer nedir?""""
- NOT: """"What is the total value across both databases?""""

If {_selectedLanguage} is German:
- Write: """"Was ist der Gesamtwert in beiden Datenbanken?""""
- NOT: """"What is the total value across both databases?""""

If {_selectedLanguage} is Russian:
- Write: """"Какова общая стоимость в обеих базах данных?""""
- NOT: """"What is the total value across both databases?""""

ONLY category field stays in English (with emoji).
Query field MUST be in {_selectedLanguage}.

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

Category options (use emoji prefix - category in English, query in {_selectedLanguage}):
- Cross-DB Join
- Cross-DB Calculation
- Cross-DB Filter
- Cross-DB Temporal
- Cross-DB Search
- Coverage Test
- Multi-DB Coverage

REQUIRED JSON FORMAT EXAMPLES:

If {_selectedLanguage} is Turkish:
[
  {{
    """"category"""": """"💰 Cross-DB Calculation"""",
    """"query"""": """"İki veritabanındaki toplam değer nedir?"""",
    """"databases"""": """"Database1 + Database2""""
  }},
  {{
    """"category"""": """"🔗 Cross-DB Join"""",
    """"query"""": """"Hangi kayıtlar birbiriyle ilişkilidir?"""",
    """"databases"""": """"Database1 + Database2""""
  }}
]

If {_selectedLanguage} is English:
[
  {{
    """"category"""": """"💰 Cross-DB Calculation"""",
    """"query"""": """"What is the total value across both databases?"""",
    """"databases"""": """"Database1 + Database2""""
  }}
]

If {_selectedLanguage} is German:
[
  {{
    """"category"""": """"💰 Cross-DB Calculation"""",
    """"query"""": """"Was ist der Gesamtwert über beide Datenbanken?"""",
    """"databases"""": """"Database1 + Database2""""
  }}
]

If {_selectedLanguage} is Russian:
[
  {{
    """"category"""": """"💰 Cross-DB Calculation"""",
    """"query"""": """"Какова общая стоимость в обеих базах данных?"""",
    """"databases"""": """"Database1 + Database2""""
  }}
]

CRITICAL: Category stays emoji + English. Query MUST be 100% {_selectedLanguage}.

🚨 FINAL CHECK:
Before responding, verify that EVERY """"query"""" value is 100% in {_selectedLanguage} language!

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
                                    // Extract database types from schema
                                    var dbTypes = ExtractDatabaseTypes(dbList, schemas);

                                    queries.Add(new TestQuery
                                    {
                                        Category = cat.GetString() ?? "🧪 Test",
                                        Query = q.GetString() ?? "",
                                        DatabaseName = dbList,
                                        DatabaseTypes = dbTypes
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
                        // Create generic cross-database join question in selected language
                        var genericQuery = TranslateQuery(
                            $"Show all records from {item.Table.TableName} with their related {fk.ReferencedTable} information",
                            _selectedLanguage,
                            item.Table.TableName,
                            fk.ReferencedTable);

                        queries.Add(new TestQuery
                        {
                            Category = "🔗 Cross-DB Join",
                            Query = genericQuery,
                            DatabaseName = $"{item.Schema.DatabaseName} + {referencedDb.DatabaseName}",
                            DatabaseTypes = $"{item.Schema.DatabaseType} + {referencedDb.DatabaseType}"
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
                            var calculationQuery = _selectedLanguage switch
                            {
                                "Turkish" => $"{table1WithNumeric.TableName} tablosundaki {numericCol1} ve {table2WithNumeric.TableName} tablosundaki {numericCol2} değerlerini kullanarak toplam değeri hesapla",
                                "German" => $"Berechne den Gesamtwert mit {numericCol1} aus {table1WithNumeric.TableName} und {numericCol2} aus {table2WithNumeric.TableName}",
                                "Russian" => $"Рассчитайте общее значение используя {numericCol1} из {table1WithNumeric.TableName} и {numericCol2} из {table2WithNumeric.TableName}",
                                _ => $"Calculate the combined value using {numericCol1} from {table1WithNumeric.TableName} and {numericCol2} from {table2WithNumeric.TableName}"
                            };

                            queries.Add(new TestQuery
                            {
                                Category = "💰 Cross-DB Calculation",
                                Query = calculationQuery,
                                DatabaseName = $"{pair.Db1.DatabaseName} + {pair.Db2.DatabaseName}",
                                DatabaseTypes = $"{pair.Db1.DatabaseType} + {pair.Db2.DatabaseType}"
                            });
                        }
                    }
                }
            }

            // 3. Multi-database coverage query
            if (schemas.Count >= 2)
            {
                var allDbNames = string.Join(" + ", schemas.Select(s => s.DatabaseName));
                var allDbTypes = string.Join(" + ", schemas.Select(s => s.DatabaseType));

                var coverageQuery = _selectedLanguage switch
                {
                    "Turkish" => "Tüm veritabanlarındaki mevcut verileri analiz ederek korelasyonları ve kalıpları bul",
                    "German" => "Analysiere alle verfügbaren Daten um Korrelationen und Muster über alle Datenbanken zu finden",
                    "Russian" => "Проанализируйте все доступные данные чтобы найти корреляции и паттерны во всех базах данных",
                    _ => "Analyze all available data to find correlations and patterns across all databases"
                };

                queries.Add(new TestQuery
                {
                    Category = "🌐 Multi-DB Coverage",
                    Query = coverageQuery,
                    DatabaseName = allDbNames,
                    DatabaseTypes = allDbTypes
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
                    var temporalQuery = _selectedLanguage switch
                    {
                        "Turkish" => $"{dateTable1.Table.TableName} ve {dateTable2.Table.TableName} kayıtları arasındaki zaman çizelgesi korelasyonu nedir?",
                        "German" => $"Was ist die zeitliche Korrelation zwischen {dateTable1.Table.TableName} und {dateTable2.Table.TableName} Datensätzen?",
                        "Russian" => $"Какова временная корреляция между записями {dateTable1.Table.TableName} и {dateTable2.Table.TableName}?",
                        _ => $"What is the timeline correlation between {dateTable1.Table.TableName} and {dateTable2.Table.TableName} records?"
                    };

                    queries.Add(new TestQuery
                    {
                        Category = "📅 Cross-DB Temporal",
                        Query = temporalQuery,
                        DatabaseName = $"{dateTable1.Schema.DatabaseName} + {dateTable2.Schema.DatabaseName}",
                        DatabaseTypes = $"{dateTable1.Schema.DatabaseType} + {dateTable2.Schema.DatabaseType}"
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
                            var relationshipQuery = _selectedLanguage switch
                            {
                                "Turkish" => $"{table1.TableName} ve {table2.TableName} arasındaki ilişkiyi analiz et",
                                "German" => $"Analysiere die Beziehung zwischen {table1.TableName} und {table2.TableName}",
                                "Russian" => $"Проанализируйте связь между {table1.TableName} и {table2.TableName}",
                                _ => $"Analyze relationship between {table1.TableName} and {table2.TableName}"
                            };

                            queries.Add(new TestQuery
                            {
                                Category = "✅ Coverage Test",
                                Query = relationshipQuery,
                                DatabaseName = $"{schema.DatabaseName} + {otherDb.DatabaseName}",
                                DatabaseTypes = $"{schema.DatabaseType} + {otherDb.DatabaseType}"
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

        private static string ExtractDatabaseTypes(string databaseNames, List<DatabaseSchemaInfo> schemas)
        {
            var dbNames = databaseNames.Split(new[] { " + ", ", " }, StringSplitOptions.RemoveEmptyEntries);
            var dbTypes = new List<string>();

            foreach (var dbName in dbNames)
            {
                var schema = schemas.FirstOrDefault(s => s.DatabaseName.Equals(dbName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (schema != null)
                {
                    dbTypes.Add(schema.DatabaseType.ToString());
                }
            }

            return string.Join(" + ", dbTypes);
        }

        private static string TranslateQuery(string template, string language, string table1, string table2)
        {
            return language switch
            {
                "Turkish" => $"{table1} tablosundaki tüm kayıtları ilgili {table2} bilgileriyle birlikte göster",
                "German" => $"Zeige alle Datensätze aus {table1} mit ihren zugehörigen {table2} Informationen",
                "Russian" => $"Показать все записи из {table1} с соответствующей информацией {table2}",
                _ => template
            };
        }

        private static string ExtractSQLFromError(string errorMessage, List<DatabaseSchemaInfo> schemas)
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

                        // Find database type from schemas
                        var schema = schemas.FirstOrDefault(s => s.DatabaseName.Equals(dbName, StringComparison.OrdinalIgnoreCase));
                        var dbType = schema != null ? $" ({schema.DatabaseType})" : "";

                        sqlInfo.AppendLine($"[{dbName}{dbType}]");

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
            public string DatabaseTypes { get; set; } = string.Empty;
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
                Console.WriteLine($"Server: localhost,1433 (Docker)");
                Console.WriteLine($"Database: SalesManagement");
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
                Console.WriteLine("  • SQL Server Docker container not running");
                Console.WriteLine("  • Connection permission denied");
                Console.WriteLine("  • Port 1433 blocked or in use");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Solution:");
                Console.WriteLine("  1. Start SQL Server using Docker: cd examples/SmartRAG.DatabaseTests && docker-compose up -d sqlserver");
                Console.WriteLine("  2. Or install SQL Server manually");
                Console.WriteLine("  3. Verify credentials (User: sa, Password: SmartRAG@2024)");
                Console.WriteLine("  4. Or set SQL Server to 'Enabled: false' in appsettings.json");
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
                Console.WriteLine($"📝 Database: InventoryManagement");
                Console.WriteLine($"📝 Connection: {connectionString.Replace("Password=mysql123", "Password=***")}");
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
                Console.WriteLine("  • MySQL Docker container not running");
                Console.WriteLine("  • MySQL service not running");
                Console.WriteLine("  • Incorrect username or password");
                Console.WriteLine("  • Port 3306 blocked or in use");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Solution:");
                Console.WriteLine("  1. Start MySQL using Docker: cd examples/SmartRAG.DatabaseTests && docker-compose up -d mysql");
                Console.WriteLine("  2. Or install MySQL Server manually");
                Console.WriteLine("  3. Verify credentials (User: root, Password: mysql123)");
                Console.WriteLine("  4. Or set MySQL to 'Enabled: false' in appsettings.json");
            }
        }

        private static async Task CreatePostgreSqlDatabase()
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("🐘 Create PostgreSQL Test Database");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");

            try
            {
                var creator = TestDatabaseFactory.GetCreator(SmartRAG.Enums.DatabaseType.PostgreSQL, _configuration);
                var connectionString = creator.GetDefaultConnectionString();

                Console.WriteLine();
                Console.WriteLine($"📝 Database: LogisticsManagement");
                Console.WriteLine($"📝 Connection: {connectionString.Replace("Password=postgres123", "Password=***")}");
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
                    var postgresConn = connections.FirstOrDefault(c => c.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL);

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
                    Console.WriteLine("   ✓ Schema analysis initiated");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Possible causes:");
                Console.WriteLine("  • PostgreSQL server not installed");
                Console.WriteLine("  • PostgreSQL service not running");
                Console.WriteLine("  • Incorrect username or password");
                Console.WriteLine("  • Port 5432 blocked or in use");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Solution:");
                Console.WriteLine("  1. Start PostgreSQL using Docker: cd examples/SmartRAG.DatabaseTests && docker-compose up -d");
                Console.WriteLine("  2. Or install PostgreSQL Server manually");
                Console.WriteLine("  3. Verify credentials (User: postgres, Password: postgres123)");
                Console.WriteLine("  4. Or set PostgreSQL to 'Enabled: false' in appsettings.json");
            }
        }

        private static async Task SetupOllamaModels()
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("🤖 Setup Ollama Models");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();

            var ollamaManager = new OllamaModelManager();

            // Check if Ollama is running
            var isAvailable = await ollamaManager.IsServiceAvailableAsync();
            if (!isAvailable)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Ollama service is not running!");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Please start Ollama:");
                Console.WriteLine("  • Docker: cd examples/SmartRAG.LocalDemo && docker-compose up -d ollama");
                Console.WriteLine("  • Or download from: https://ollama.ai");
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Ollama service is running");
            Console.ResetColor();
            Console.WriteLine();

            // List installed models
            var installedModels = await ollamaManager.ListInstalledModelsAsync();
            Console.WriteLine($"Installed models: {installedModels.Count}");
            foreach (var model in installedModels)
            {
                Console.WriteLine($"  • {model}");
            }
            Console.WriteLine();

            // Show recommended models
            Console.WriteLine("Recommended models for SmartRAG:");
            var recommended = OllamaModelManager.GetRecommendedModels();
            var index = 1;
            foreach (var kvp in recommended)
            {
                var isInstalled = installedModels.Any(m => m.Contains(kvp.Key));
                var status = isInstalled ? "✓ Installed" : "  Not installed";
                Console.WriteLine($"{index}. {kvp.Key} - {kvp.Value} [{status}]");
                index++;
            }
            Console.WriteLine();

            Console.Write("Enter model number to install (0 to skip): ");
            var choice = Console.ReadLine();

            if (int.TryParse(choice, out var modelIndex) && modelIndex > 0 && modelIndex <= recommended.Count)
            {
                var modelToInstall = recommended.Keys.ElementAt(modelIndex - 1);
                Console.WriteLine();
                Console.WriteLine($"Downloading {modelToInstall}... (This may take several minutes)");
                Console.WriteLine();

                await ollamaManager.DownloadModelAsync(modelToInstall, (progress) =>
                {
                    Console.WriteLine($"  {progress}");
                });

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Model {modelToInstall} installed successfully!");
                Console.ResetColor();
            }
        }

        private static async Task TestVectorStore()
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("📦 Test Vector Store");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();

            var healthCheck = new HealthCheckService();

            Console.WriteLine($"Testing {_selectedStorageProvider} vector store...");
            Console.WriteLine();

            HealthStatus status;
            if (_selectedStorageProvider == StorageProvider.Qdrant)
            {
                status = await healthCheck.CheckQdrantAsync();
            }
            else if (_selectedStorageProvider == StorageProvider.Redis)
            {
                status = await healthCheck.CheckRedisAsync();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️  {_selectedStorageProvider} does not require health check (file-based or in-memory)");
                Console.ResetColor();
                return;
            }

            if (status.IsHealthy)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ {status.ServiceName} is healthy");
                Console.ResetColor();
                Console.WriteLine($"  {status.Message}");
                Console.WriteLine($"  {status.Details}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ {status.ServiceName} is not available");
                Console.ResetColor();
                Console.WriteLine($"  {status.Message}");
                Console.WriteLine($"  {status.Details}");
                Console.WriteLine();
                Console.WriteLine("Solution:");
                Console.WriteLine("  • Start services: cd examples/SmartRAG.LocalDemo && docker-compose up -d");
            }
        }

        private static async Task RunSystemHealthCheck()
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("🔧 System Health Check");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();

            var healthCheck = new HealthCheckService();

            Console.WriteLine("Checking all services...");
            Console.WriteLine();

            // Check AI Service
            if (_useLocalEnvironment)
            {
                Console.Write("🤖 Ollama (AI)................. ");
                var ollamaStatus = await healthCheck.CheckOllamaAsync();
                PrintHealthStatus(ollamaStatus);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"☁️  {_selectedAIProvider} (Cloud AI)");
                Console.ResetColor();
            }

            // Check Vector Store
            Console.Write($"📦 {_selectedStorageProvider} (Vector Store).... ");
            HealthStatus? vectorStatus = null;
            if (_selectedStorageProvider == StorageProvider.Qdrant)
            {
                vectorStatus = await healthCheck.CheckQdrantAsync();
            }
            else if (_selectedStorageProvider == StorageProvider.Redis)
            {
                vectorStatus = await healthCheck.CheckRedisAsync();
            }
            if (vectorStatus != null)
            {
                PrintHealthStatus(vectorStatus);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("N/A (file-based)");
                Console.ResetColor();
            }

            // Check Databases
            Console.WriteLine();
            Console.WriteLine("Databases:");

            var connections = await _connectionManager!.GetAllConnectionsAsync();
            foreach (var conn in connections)
            {
                Console.Write($"  • {conn.Name} ({conn.DatabaseType})... ");

                HealthStatus? dbStatus = null;
                try
                {
                    dbStatus = conn.DatabaseType switch
                    {
                        SmartRAG.Enums.DatabaseType.SQLite => await healthCheck.CheckSqliteAsync(conn.ConnectionString),
                        SmartRAG.Enums.DatabaseType.SqlServer => await healthCheck.CheckSqlServerAsync(conn.ConnectionString),
                        SmartRAG.Enums.DatabaseType.MySQL => await healthCheck.CheckMySqlAsync(conn.ConnectionString),
                        SmartRAG.Enums.DatabaseType.PostgreSQL => await healthCheck.CheckPostgreSqlAsync(conn.ConnectionString),
                        _ => null
                    };

                    if (dbStatus != null)
                    {
                        PrintHealthStatus(dbStatus, inline: true);
                    }
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("✗ Error");
                    Console.ResetColor();
                }
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("💡 TIP: Start all services with: docker-compose up -d");
            Console.ResetColor();
        }

        private static string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".bmp" => "image/bmp",
                ".txt" => "text/plain",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".m4a" => "audio/mp4",
                _ => "application/octet-stream"
            };
        }

        private static void PrintHealthStatus(HealthStatus status, bool inline = false)
        {
            if (status.IsHealthy)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Healthy");
                Console.ResetColor();
                if (!inline)
                {
                    Console.WriteLine($"   {status.Message}");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ Unavailable");
                Console.ResetColor();
                if (!inline)
                {
                    Console.WriteLine($"   {status.Message}");
                    Console.WriteLine($"   {status.Details}");
                }
            }
        }

        private static async Task UploadDocuments()
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("📄 Upload Documents");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();

            Console.WriteLine("Supported file types:");
            Console.WriteLine("  • PDF documents");
            Console.WriteLine("  • Word documents (.docx)");
            Console.WriteLine("  • Excel spreadsheets (.xlsx)");
            Console.WriteLine("  • Images (.jpg, .png, .bmp - OCR)");
            Console.WriteLine("  • Text files (.txt)");

            Console.ForegroundColor = ConsoleColor.Cyan;
            if (_useLocalEnvironment)
            {
                Console.WriteLine("  • Audio files (.mp3, .wav, .m4a - Whisper.net local)");
            }
            else
            {
                Console.WriteLine("  • Audio files (.mp3, .wav, .m4a - Google Cloud)");
            }
            Console.ResetColor();

            Console.WriteLine();
            Console.Write("Enter file path (or drag & drop file here): ");

            var filePath = Console.ReadLine()?.Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ File not found!");
                Console.ResetColor();
                return;
            }

            try
            {
                Console.WriteLine();
                Console.WriteLine($"⏳ Processing file: {Path.GetFileName(filePath)}");
                Console.WriteLine();

                using var fileStream = File.OpenRead(filePath);
                var fileName = Path.GetFileName(filePath);
                var contentType = GetContentType(filePath);

                var document = await _documentService!.UploadDocumentAsync(
                    fileStream,
                    fileName,
                    contentType,
                    "LocalDemo",
                    _selectedLanguage
                );

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Document uploaded successfully!");
                Console.ResetColor();
                Console.WriteLine($"  ID: {document.Id}");
                Console.WriteLine($"  Name: {document.FileName}");
                Console.WriteLine($"  Size: {document.Content.Length:N0} bytes");
                Console.WriteLine($"  Chunks: {document.Chunks?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error uploading document");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static async Task ListDocuments()
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("📚 Uploaded Documents");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();

            try
            {
                var documents = await _documentService!.GetAllDocumentsAsync();

                if (!documents.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️  No documents uploaded yet");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.WriteLine("💡 Use option 13 to upload documents");
                    return;
                }

                Console.WriteLine($"Total documents: {documents.Count}");
                Console.WriteLine();

                foreach (var doc in documents)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"📄 {doc.FileName}");
                    Console.ResetColor();
                    Console.WriteLine($"   ID: {doc.Id}");
                    Console.WriteLine($"   Type: {doc.ContentType}");
                    Console.WriteLine($"   Size: {doc.Content.Length:N0} bytes");
                    Console.WriteLine($"   Chunks: {doc.Chunks?.Count ?? 0}");
                    Console.WriteLine($"   Uploaded: {doc.UploadedAt:yyyy-MM-dd HH:mm}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error listing documents");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static async Task ClearAllDocuments()
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("🗑️  Clear All Documents");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();

            var documents = await _documentService!.GetAllDocumentsAsync();
            Console.WriteLine($"Total documents in storage: {documents.Count}");
            Console.WriteLine();

            if (documents.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  No documents to clear!");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  WARNING: This will permanently delete ALL documents and their embeddings!");
            Console.ResetColor();
            Console.WriteLine();
            Console.Write("Are you sure? Type 'yes' to confirm: ");

            var confirmation = Console.ReadLine();
            if (confirmation?.ToLower() != "yes")
            {
                Console.WriteLine("❌ Operation cancelled.");
                return;
            }

            try
            {
                Console.WriteLine();
                Console.WriteLine("🗑️  Clearing all documents...");

                var success = await _documentService.ClearAllDocumentsAsync();

                if (success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Successfully cleared {documents.Count} documents!");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Failed to clear documents!");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error clearing documents");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static async Task RunMultiModalQuery()
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("🎯 Multi-Modal RAG Query");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("This feature searches BOTH:");
            Console.WriteLine("  1. 📄 Uploaded documents (PDF, Word, Excel, Images)");
            Console.WriteLine("  2. 🗄️  Connected databases (SQL Server, MySQL, PostgreSQL, SQLite)");
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Language: {_selectedLanguage}");
            Console.WriteLine($"Environment: {(_useLocalEnvironment ? "LOCAL (Ollama)" : $"CLOUD ({_selectedAIProvider})")}");
            Console.ResetColor();
            Console.WriteLine();

            // Check if documents are available
            var documents = await _documentService!.GetAllDocumentsAsync();
            Console.WriteLine($"📄 Documents available: {documents.Count}");

            var schemas = await _schemaAnalyzer!.GetAllSchemasAsync();
            var totalTables = schemas.Sum(s => s.Tables.Count);
            Console.WriteLine($"🗄️  Database tables: {totalTables} across {schemas.Count} databases");
            Console.WriteLine();

            if (documents.Count == 0 && totalTables == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  No data sources available!");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Please:");
                Console.WriteLine("  • Upload documents (option 13)");
                Console.WriteLine("  • Create test databases (options 6-8)");
                return;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"Your question ({_selectedLanguage}): ");
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
                Console.WriteLine("⏳ Searching documents and databases...");
                Console.ResetColor();

                // Add language instruction
                var languageInstructedQuery = $"{query}\n\n[IMPORTANT: Respond in {_selectedLanguage} language]";

                // Search documents (if available)
                List<string> documentContext = new List<string>();
                if (documents.Count > 0)
                {
                    Console.WriteLine("   → Searching documents...");
                    var docResults = await _documentSearchService!.SearchDocumentsAsync(languageInstructedQuery, maxResults: 5);

                    foreach (var result in docResults)
                    {
                        var sourceDoc = documents.FirstOrDefault(d => d.Id == result.DocumentId);
                        var docName = sourceDoc?.FileName ?? "Unknown";
                        documentContext.Add($"[Document: {docName}]\n{result.Content}");
                    }

                    Console.WriteLine($"   ✓ Found {docResults.Count} relevant document chunks");
                }

                // Search databases (if available)
                string? databaseAnswer = null;
                if (totalTables > 0)
                {
                    Console.WriteLine("   → Querying databases...");
                    var dbResponse = await _multiDbCoordinator!.QueryMultipleDatabasesAsync(languageInstructedQuery, maxResults: 10);
                    databaseAnswer = dbResponse.Answer;
                    Console.WriteLine($"   ✓ Database query completed");
                }

                // Combine results and generate final answer
                Console.WriteLine("   → Generating combined answer...");
                Console.WriteLine();

                var combinedContext = new List<string>();

                if (documentContext.Any())
                {
                    combinedContext.Add("=== DOCUMENT INFORMATION ===");
                    combinedContext.AddRange(documentContext);
                }

                if (!string.IsNullOrEmpty(databaseAnswer))
                {
                    combinedContext.Add("=== DATABASE INFORMATION ===");
                    combinedContext.Add(databaseAnswer);
                }

                var finalPrompt = $@"User Question: {query}

Available Information:
{string.Join("\n\n", combinedContext)}

Instructions:
- Analyze both document and database information
- Provide a comprehensive answer combining insights from both sources
- If information conflicts, mention both perspectives
- Respond in {_selectedLanguage} language
- Be clear about which source provided which information";

                var finalAnswer = await _aiService!.GenerateResponseAsync(finalPrompt, new List<string>());

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("📝 COMBINED ANSWER:");
                Console.WriteLine("─────────────────────────────────────────────────────────────────");
                Console.ResetColor();
                Console.WriteLine(finalAnswer);
                Console.WriteLine();

                // Show sources
                if (documentContext.Any() || !string.IsNullOrEmpty(databaseAnswer))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("📚 Sources used:");
                    if (documentContext.Any())
                    {
                        Console.WriteLine($"   • {documentContext.Count} document(s)");
                    }
                    if (!string.IsNullOrEmpty(databaseAnswer))
                    {
                        Console.WriteLine($"   • {schemas.Count} database(s)");
                    }
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during multi-modal query");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}


