
namespace SmartRAG.Demo.Services.Initialization;

/// <summary>
/// Handles application initialization
/// </summary>
public class InitializationService(
    IConfiguration configuration,
    IConsoleService console,
    ILogger<InitializationService>? logger = null) : IInitializationService
{
    #region Fields

    private readonly IConfiguration _configuration = configuration;
    private readonly IConsoleService _console = console;
    private readonly ILogger<InitializationService>? _logger = logger;
    private IServiceProvider? _serviceProvider;

    #endregion

    #region Public Methods

    public async Task SetupTestDatabasesAsync()
    {
        var enableAutoSchemaAnalysis = _configuration.GetValue<bool>("SmartRAG:EnableAutoSchemaAnalysis", false);

        if (!enableAutoSchemaAnalysis)
            return;

        System.Console.Write("📁 Creating test databases... ");
        System.Console.WriteLine();

        var databasesCreated = 0;
        var databasesSkipped = 0;

        try
        {
            var sqlServerCreator = new SqlServerTestDatabaseCreator(_configuration);
            var sqlServerConnectionString = sqlServerCreator.GetDefaultConnectionString();
            System.Console.Write("  • SQL Server (SalesManagement)... ");
            try
            {
                if (await sqlServerCreator.DatabaseExistsAsync())
                {
                    _console.WriteInfo("✓ Exists");
                    databasesSkipped++;
                }
                else
                {
                    await sqlServerCreator.CreateSampleDatabaseAsync(sqlServerConnectionString);
                    _console.WriteSuccess("✓ Created");
                    databasesCreated++;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "SQL Server database creation skipped");
                _console.WriteWarning("⚠ Skipped");
                databasesSkipped++;
            }

            var mysqlCreator = new MySqlTestDatabaseCreator(_configuration);
            var mysqlConnectionString = mysqlCreator.GetDefaultConnectionString();
            System.Console.Write("  • MySQL (InventoryManagement)... ");
            try
            {
                if (await mysqlCreator.DatabaseExistsAsync())
                {
                    _console.WriteInfo("✓ Exists");
                    databasesSkipped++;
                }
                else
                {
                    await mysqlCreator.CreateSampleDatabaseAsync(mysqlConnectionString);
                    _console.WriteSuccess("✓ Created");
                    databasesCreated++;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "MySQL database creation skipped");
                _console.WriteWarning("⚠ Skipped");
                databasesSkipped++;
            }

            var postgresqlCreator = new PostgreSqlTestDatabaseCreator(_configuration);
            var postgresqlConnectionString = postgresqlCreator.GetDefaultConnectionString();
            System.Console.Write("  • PostgreSQL (PersonManagement)... ");
            try
            {
                if (await postgresqlCreator.DatabaseExistsAsync())
                {
                    _console.WriteInfo("✓ Exists");
                    databasesSkipped++;
                }
                else
                {
                    await postgresqlCreator.CreateSampleDatabaseAsync(postgresqlConnectionString);
                    _console.WriteSuccess("✓ Created");
                    databasesCreated++;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "PostgreSQL database creation skipped");
                _console.WriteWarning("⚠ Skipped");
                databasesSkipped++;
            }

            var sqliteCreator = new SqliteTestDatabaseCreator(_configuration);
            var sqliteConnectionString = sqliteCreator.GetDefaultConnectionString();
            System.Console.Write("  • SQLite (LogisticsManagement)... ");
            try
            {
                if (await sqliteCreator.DatabaseExistsAsync())
                {
                    _console.WriteInfo("✓ Exists");
                    databasesSkipped++;
                }
                else
                {
                    await sqliteCreator.CreateSampleDatabaseAsync(sqliteConnectionString);
                    _console.WriteSuccess("✓ Created");
                    databasesCreated++;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "SQLite database creation skipped");
                _console.WriteWarning("⚠ Skipped");
                databasesSkipped++;
            }

            System.Console.WriteLine();
            if (databasesCreated > 0)
            {
                _console.WriteSuccess($"✓ Created {databasesCreated} database(s)");
            }
            if (databasesSkipped > 0)
            {
                _console.WriteWarning($"⚠ Skipped {databasesSkipped} database(s) (may already exist or services not available)");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during database setup");
            System.Console.WriteLine();
            _console.WriteWarning("⚠ Some databases could not be created");
        }
    }

    public async Task<(bool UseLocal, AIProvider AIProvider, StorageProvider StorageProvider, ConversationStorageProvider ConversationStorageProvider)> SelectEnvironmentAsync()
    {
        await Task.CompletedTask;

        _console.WriteSectionHeader("🚀 ENVIRONMENT SELECTION");

        System.Console.WriteLine("Choose your deployment environment:");
        System.Console.WriteLine();
        System.Console.WriteLine("1. 🏠 LOCAL Environment (100% Local - No Cloud Required)");
        System.Console.WriteLine("   • AI: Ollama (running on localhost)");
        System.Console.WriteLine("   • Vector Store: Qdrant (local docker container)");
        System.Console.WriteLine("   • Document Cache: Redis (optional - local docker container)");
        System.Console.WriteLine("   • Databases: Local SQL Server, MySQL, PostgreSQL, SQLite");
        System.Console.WriteLine("   ✅ GDPR/KVKK compliant - All data stays on your machine");
        System.Console.WriteLine();
        System.Console.WriteLine("2. ☁️  CLOUD Environment (Cloud Services)");
        System.Console.WriteLine("   • AI: Gemini / OpenAI / AzureOpenAI / Anthropic / Custom");
        System.Console.WriteLine("   • Vector Store: Qdrant Cloud");
        System.Console.WriteLine("   • Cache: Redis Cloud");
        System.Console.WriteLine("   • Databases: Can use cloud or local databases");
        System.Console.WriteLine("   ⚡ High performance with cloud AI models");
        System.Console.WriteLine();

        var choice = _console.ReadLine("Selection (default: Local): ");

        var useLocalEnvironment = choice switch
        {
            "1" or "" => true,
            "2" => false,
            _ => true
        };

        if (useLocalEnvironment)
        {
            System.Console.WriteLine();
            _console.WriteSuccess("✓ LOCAL Environment selected");
            System.Console.WriteLine("  AI Provider: Ollama (via Custom provider)");

            System.Console.WriteLine();
            System.Console.WriteLine("Select Storage Provider:");
            System.Console.WriteLine("1. Qdrant (Vector Database)");
            System.Console.WriteLine("2. Redis (Key-Value + Vector Search)");
            System.Console.WriteLine("3. InMemory (Non-persistent, for testing)");

            var storageChoice = _console.ReadLine("Selection (default: Qdrant): ");
            var selectedStorage = storageChoice switch
            {
                "1" or "" => StorageProvider.Qdrant,
                "2" => StorageProvider.Redis,
                "3" => StorageProvider.InMemory,
                _ => StorageProvider.Qdrant
            };

            System.Console.WriteLine($"  Storage: {selectedStorage}");
            System.Console.WriteLine("  Audio: Whisper.net (Local transcription)");
            System.Console.WriteLine();

            if (selectedStorage == StorageProvider.Redis)
            {
                _console.WriteWarning("⚠️  IMPORTANT: Redis with RediSearch module required for vector search");
                System.Console.WriteLine("     • Use 'redis/redis-stack-server:latest' Docker image");
                System.Console.WriteLine("     • Or install RediSearch module on your Redis server");
                System.Console.WriteLine("     • Without RediSearch: Only text search will work (no vector search)");
                System.Console.WriteLine();
            }

            System.Console.WriteLine("Select Conversation History Storage:");
            System.Console.WriteLine("1. Redis (Persistent, recommended)");
            System.Console.WriteLine("2. SQLite (Local database file)");
            System.Console.WriteLine("3. FileSystem (File-based storage)");
            System.Console.WriteLine("4. InMemory (Non-persistent, for testing)");

            var conversationStorageChoice = _console.ReadLine("Selection (default: Redis): ");
            var selectedConversationStorage = conversationStorageChoice switch
            {
                "1" or "" => ConversationStorageProvider.Redis,
                "2" => ConversationStorageProvider.SQLite,
                "3" => ConversationStorageProvider.FileSystem,
                "4" => ConversationStorageProvider.InMemory,
                _ => ConversationStorageProvider.Redis
            };

            System.Console.WriteLine($"  Conversation History Storage: {selectedConversationStorage}");
            System.Console.WriteLine();

            _console.WriteWarning("⚠️  Note: Make sure Ollama endpoint is configured in appsettings");
            System.Console.WriteLine("     (AI:Custom:Endpoint = http://localhost:11434)");
            System.Console.WriteLine();

            return (true, AIProvider.Custom, selectedStorage, selectedConversationStorage);
        }

        System.Console.WriteLine();
        System.Console.WriteLine("Select AI Provider:");
        System.Console.WriteLine("1. Google Gemini");
        System.Console.WriteLine("2. OpenAI GPT");
        System.Console.WriteLine("3. Azure OpenAI");
        System.Console.WriteLine("4. Anthropic Claude");
        System.Console.WriteLine("5. Custom Provider");

        var aiChoice = _console.ReadLine("Selection (default: Anthropic): ");
        var selectedAIProvider = aiChoice switch
        {
            "1" => AIProvider.Gemini,
            "2" => AIProvider.OpenAI,
            "3" => AIProvider.AzureOpenAI,
            "4" or "" => AIProvider.Anthropic,
            "5" => AIProvider.Custom,
            _ => AIProvider.Anthropic
        };

        System.Console.WriteLine();
        _console.WriteInfo("CLOUD Environment selected");
        System.Console.WriteLine($"  AI Provider: {selectedAIProvider}");
        System.Console.WriteLine("  Storage: Redis (Document storage)");
        System.Console.WriteLine("  Audio: Whisper.net (Local transcription)");
        System.Console.WriteLine();

        System.Console.WriteLine("Select Conversation History Storage:");
        System.Console.WriteLine("1. Redis (Persistent, recommended)");
        System.Console.WriteLine("2. SQLite (Local database file)");
        System.Console.WriteLine("3. FileSystem (File-based storage)");
        System.Console.WriteLine("4. InMemory (Non-persistent, for testing)");

        var cloudConversationStorageChoice = _console.ReadLine("Selection (default: Redis): ");
        var cloudSelectedConversationStorage = cloudConversationStorageChoice switch
        {
            "1" or "" => ConversationStorageProvider.Redis,
            "2" => ConversationStorageProvider.SQLite,
            "3" => ConversationStorageProvider.FileSystem,
            "4" => ConversationStorageProvider.InMemory,
            _ => ConversationStorageProvider.Redis
        };

        System.Console.WriteLine($"  Conversation History Storage: {cloudSelectedConversationStorage}");
        System.Console.WriteLine();

        return (false, selectedAIProvider, StorageProvider.Redis, cloudSelectedConversationStorage);
    }

    public async Task<string> SelectLanguageAsync()
    {
        await Task.CompletedTask;

        _console.WriteSectionHeader("🌍 LANGUAGE SELECTION");

        System.Console.WriteLine("Please select the language for test queries and AI responses:");
        System.Console.WriteLine();
        System.Console.WriteLine("1. 🇬🇧 English");
        System.Console.WriteLine("2. 🇩🇪 German");
        System.Console.WriteLine("3. 🇹🇷 Turkish");
        System.Console.WriteLine("4. 🇷🇺 Russian");
        System.Console.WriteLine("5. 🌐 Other (specify ISO code)");
        System.Console.WriteLine();

        var choice = _console.ReadLine("Selection (default: Turkish): ");

        var selectedLanguageCode = choice switch
        {
            "1" => "en",
            "2" => "de",
            "3" or "" => "tr",
            "4" => "ru",
            "5" => GetCustomLanguageCode(),
            _ => "tr"
        };

        var displayName = choice switch
        {
            "1" => "English",
            "2" => "German",
            "3" or "" => "Turkish",
            "4" => "Russian",
            "5" => selectedLanguageCode,
            _ => "Turkish"
        };

        System.Console.WriteLine();
        _console.WriteSuccess($"Language set to: {displayName} (code: {selectedLanguageCode})");
        System.Console.WriteLine();

        return selectedLanguageCode;
    }

    public async Task InitializeServicesAsync(AIProvider aiProvider, StorageProvider storageProvider, ConversationStorageProvider conversationStorageProvider, string? defaultLanguage = null)
    {
        System.Console.WriteLine("🔧 Initializing SmartRAG...");
        System.Console.WriteLine();

        try
        {
            var services = new ServiceCollection();
            services.AddSingleton(_configuration);

            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddConfiguration(_configuration.GetSection("Logging"));
            });

            System.Console.WriteLine($"   → Configuring {aiProvider} provider...");
            System.Console.WriteLine($"   → Configuring {storageProvider} storage...");
            System.Console.WriteLine($"   → Configuring {conversationStorageProvider} conversation history storage...");

            _serviceProvider = services.UseSmartRag(_configuration, storageProvider, aiProvider, conversationStorageProvider, defaultLanguage);

            System.Console.WriteLine();
            _console.WriteSuccess("Services initialized successfully");
            System.Console.WriteLine($"  AI Provider: {aiProvider}");
            System.Console.WriteLine($"  Storage Provider: {storageProvider}");
            System.Console.WriteLine($"  Conversation History Storage: {conversationStorageProvider}");
            System.Console.WriteLine($"  Audio Provider: Whisper (only supported provider)");

            await DisplayDatabaseStatus();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Service initialization failed");
            _console.WriteError($"Service initialization failed: {ex.Message}");
            System.Console.WriteLine($"   Type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                System.Console.WriteLine($"   Inner: {ex.InnerException.Message}");
            }
            throw;
        }
    }

    #endregion

    #region Public Properties

    public IServiceProvider? GetServiceProvider() => _serviceProvider;

    #endregion

    #region Private Methods

    /// <summary>
    /// Prompts user to enter custom ISO 639-1 language code
    /// </summary>
    /// <returns>Language code or default 'en' if empty</returns>
    private string GetCustomLanguageCode()
    {
        System.Console.WriteLine();
        var customCode = _console.ReadLine("Enter ISO 639-1 language code (e.g., fr, es, it, ja, zh): ");
        return string.IsNullOrWhiteSpace(customCode) ? "en" : customCode.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Displays database connection and schema status
    /// </summary>
    private async Task DisplayDatabaseStatus()
    {
        var connectionManager = _serviceProvider?.GetService<IDatabaseConnectionManager>();
        var schemaAnalyzer = _serviceProvider?.GetService<IDatabaseSchemaAnalyzer>();

        if (connectionManager == null || schemaAnalyzer == null)
            return;

        await connectionManager.InitializeAsync(CancellationToken.None);

        var schemas = await schemaAnalyzer.GetAllSchemasAsync(CancellationToken.None);
        var completed = schemas.Where(s => s.Status == SchemaAnalysisStatus.Completed && s.Tables.Count > 0).ToList();
        var needsSetup = schemas.Where(s => s.Tables.Count == 0).ToList();

        System.Console.WriteLine($"📊 Ready: {completed.Count} database(s) with data");

        foreach (var schema in completed)
        {
            _console.WriteSuccess($"{schema.DatabaseName}: {schema.Tables.Count} tables, {schema.TotalRowCount} total rows");
        }

        System.Console.WriteLine();

        if (needsSetup.Any())
        {
            _console.WriteWarning("SETUP REQUIRED");
            System.Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            System.Console.WriteLine();
            System.Console.WriteLine("Some databases are not created yet. Please follow these steps:");
            System.Console.WriteLine();

            var instructionsShown = new HashSet<string>();

            foreach (var schema in needsSetup)
            {
                var instruction = schema.DatabaseType switch
                {
                    DatabaseType.SqlServer => "Select option 3 → 🗄️ Create SQL Server Test Database → SalesManagement",
                    DatabaseType.MySQL => "Select option 4 → 🐬 Create MySQL Test Database → InventoryManagement",
                    DatabaseType.PostgreSQL => "Select option 5 → 🐘 Create PostgreSQL Test Database → PersonManagement",
                    _ => null
                };

                if (instruction != null && !instructionsShown.Contains(instruction))
                {
                    System.Console.WriteLine($"   {instruction}");
                    instructionsShown.Add(instruction);
                }
            }

            if (instructionsShown.Any())
            {
                System.Console.WriteLine();
                _console.WriteInfo("💡 TIP: Use the menu options above to create databases automatically!");
                System.Console.WriteLine();
            }
        }
    }

    #endregion
}

