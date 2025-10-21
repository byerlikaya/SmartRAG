using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartRAG.Demo.DatabaseSetup.Creators;
using SmartRAG.Demo.Services.Console;
using SmartRAG.Enums;
using SmartRAG.Extensions;
using SmartRAG.Interfaces;
using SmartRAG.Models;

namespace SmartRAG.Demo.Services.Initialization;

/// <summary>
/// Handles application initialization
/// </summary>
public class InitializationService(
    IConfiguration configuration,
    IConsoleService console) : IInitializationService
{
    #region Fields

    private readonly IConfiguration _configuration = configuration;
    private readonly IConsoleService _console = console;
    private IServiceProvider? _serviceProvider;

    #endregion

    #region Public Methods

    public async Task SetupTestDatabasesAsync()
    {
        var sqliteDbPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "ProductCatalog.db");
        var sqliteDir = Path.GetDirectoryName(sqliteDbPath);

        if (!string.IsNullOrEmpty(sqliteDir) && !Directory.Exists(sqliteDir))
        {
            Directory.CreateDirectory(sqliteDir);
        }

        var sqliteCreator = new SqliteTestDatabaseCreator();

        if (!File.Exists(sqliteDbPath))
        {
            System.Console.Write("📁 Creating SQLite test database... ");
            sqliteCreator.CreateSampleDatabase($"Data Source={sqliteDbPath}");
            _console.WriteSuccess("✓");
        }

        await Task.CompletedTask;
    }

    public async Task<(bool UseLocal, AIProvider AIProvider, StorageProvider StorageProvider, AudioProvider AudioProvider)> SelectEnvironmentAsync()
    {
        await Task.CompletedTask;
        
        _console.WriteSectionHeader("🚀 ENVIRONMENT SELECTION");
        
        System.Console.WriteLine("Choose your deployment environment:");
        System.Console.WriteLine();
        System.Console.WriteLine("1. ☁️  CLOUD Environment (Cloud Services)");
        System.Console.WriteLine("   • AI: Gemini / OpenAI / AzureOpenAI / Anthropic / Custom");
        System.Console.WriteLine("   • Vector Store: Qdrant Cloud");
        System.Console.WriteLine("   • Cache: Redis Cloud");
        System.Console.WriteLine("   • Databases: Can use cloud or local databases");
        System.Console.WriteLine("   ⚡ High performance with cloud AI models");
        System.Console.WriteLine();
        System.Console.WriteLine("2. 🏠 LOCAL Environment (100% Local - No Cloud Required)");
        System.Console.WriteLine("   • AI: Ollama (running on localhost)");
        System.Console.WriteLine("   • Vector Store: Qdrant (local docker container)");
        System.Console.WriteLine("   • Cache: Redis (local docker container)");
        System.Console.WriteLine("   • Databases: Local SQL Server, MySQL, PostgreSQL, SQLite");
        System.Console.WriteLine("   ✅ GDPR/KVKK compliant - All data stays on your machine");
        System.Console.WriteLine();

        var choice = _console.ReadLine("Selection (default: Local): ");

        var useLocalEnvironment = choice switch
        {
            "1" => false,
            "2" or "" => true,
            _ => true
        };

        if (useLocalEnvironment)
        {
            System.Console.WriteLine();
            _console.WriteSuccess("LOCAL Environment selected");
            System.Console.WriteLine("  AI Provider: Ollama (via Custom provider)");
            System.Console.WriteLine("  Storage: Redis (Document storage)");
            System.Console.WriteLine("  Audio: Whisper.net (Local transcription)");
            System.Console.WriteLine();
            _console.WriteWarning("Note: Make sure Ollama endpoint is configured in appsettings");
            System.Console.WriteLine("     (AI:Custom:Endpoint = http://localhost:11434)");
            System.Console.WriteLine();

            return (true, AIProvider.Custom, StorageProvider.Redis, AudioProvider.Whisper);
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
        System.Console.WriteLine("  Audio: Google Cloud Speech-to-Text");
        System.Console.WriteLine();

        return (false, selectedAIProvider, StorageProvider.Redis, AudioProvider.GoogleCloud);
    }

    public async Task<string> SelectLanguageAsync()
    {
        await Task.CompletedTask;
        
        _console.WriteSectionHeader("🌍 LANGUAGE SELECTION");
        
        System.Console.WriteLine("Please select the language for test queries and AI responses:");
        System.Console.WriteLine();
        System.Console.WriteLine("1. 🇬🇧 English");
        System.Console.WriteLine("2. 🇩🇪 German (Deutsch)");
        System.Console.WriteLine("3. 🇹🇷 Turkish (Türkçe)");
        System.Console.WriteLine("4. 🇷🇺 Russian (Русский)");
        System.Console.WriteLine("5. 🌐 Other (specify)");
        System.Console.WriteLine();

        var choice = _console.ReadLine("Selection (default: English): ");

        var selectedLanguage = choice switch
        {
            "1" or "" => "English",
            "2" => "German",
            "3" => "Turkish",
            "4" => "Russian",
            "5" => GetCustomLanguage(),
            _ => "English"
        };

        System.Console.WriteLine();
        _console.WriteSuccess($"Language set to: {selectedLanguage}");
        System.Console.WriteLine();

        return selectedLanguage;
    }

    public async Task InitializeServicesAsync(AIProvider aiProvider, StorageProvider storageProvider, AudioProvider audioProvider)
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

            services.AddSmartRag(_configuration, options =>
            {
                options.StorageProvider = storageProvider;
                options.AIProvider = aiProvider;
                options.AudioProvider = audioProvider;
            });

            System.Console.WriteLine("   → Building service provider...");
            _serviceProvider = services.BuildServiceProvider();

            System.Console.WriteLine();
            _console.WriteSuccess("Services initialized successfully");
            System.Console.WriteLine($"  AI Provider: {aiProvider}");
            System.Console.WriteLine($"  Storage Provider: {storageProvider}");
            System.Console.WriteLine($"  Audio Provider: {audioProvider}");

            await DisplayDatabaseStatus();
        }
        catch (Exception ex)
        {
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

    private string GetCustomLanguage()
    {
        System.Console.WriteLine();
        var customLang = _console.ReadLine("Enter language name (e.g., French, Spanish, Italian): ");
        return string.IsNullOrWhiteSpace(customLang) ? "English" : customLang.Trim();
    }

    private async Task DisplayDatabaseStatus()
    {
        var connectionManager = _serviceProvider?.GetService<IDatabaseConnectionManager>();
        var schemaAnalyzer = _serviceProvider?.GetService<IDatabaseSchemaAnalyzer>();

        if (connectionManager == null || schemaAnalyzer == null)
            return;

        await connectionManager.InitializeAsync();

        var schemas = await schemaAnalyzer.GetAllSchemasAsync();
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
                    DatabaseType.SqlServer => "3. 🗄️  Create SQL Server Test Database → SalesManagement",
                    DatabaseType.MySQL => "4. 🐬 Create MySQL Test Database → InventoryManagement",
                    DatabaseType.PostgreSQL => "5. 🐘 Create PostgreSQL Test Database → LogisticsManagement",
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

