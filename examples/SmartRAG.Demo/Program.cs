using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartRAG.Demo.Handlers.DatabaseHandlers;
using SmartRAG.Demo.Handlers.DocumentHandlers;
using SmartRAG.Demo.Handlers.OllamaHandlers;
using SmartRAG.Demo.Handlers.QueryHandlers;
using SmartRAG.Demo.Services.Console;
using SmartRAG.Demo.Services.Initialization;
using SmartRAG.Demo.Services.Menu;
using SmartRAG.Demo.Services.TestQuery;
using SmartRAG.Demo.Services.Translation;
using SmartRAG.Enums;
using SmartRAG.Extensions;
using SmartRAG.Interfaces;

namespace SmartRAG.Demo;

/// <summary>
/// Main entry point for SmartRAG Demo application
/// </summary>
internal class Program
{
    #region Fields

    private static IServiceProvider? _serviceProvider;
    private static ILogger<Program>? _logger;
    private static string _selectedLanguage = "English";
    private static bool _useLocalEnvironment = true;
    private static AIProvider _selectedAIProvider = AIProvider.Custom;
    private static StorageProvider _selectedStorageProvider = StorageProvider.Redis;

    #endregion

    #region Main Method

    private static async Task Main(string[] args)
    {
        var consoleAvailable = ConsoleHelper.IsConsoleAvailable();

        if (consoleAvailable)
        {
            ConsoleHelper.ConfigureForAnimations();
        }

        try
        {
            if (consoleAvailable)
            {
                await ShowWelcomeAnimationAsync();
                
                ConsoleHelper.ResetCursor();
                
                // Clear console after animation but AFTER buffer is restored
                System.Console.Clear();
            }

            var configuration = LoadConfiguration();
            var consoleService = new ConsoleService();
            
            var initService = new InitializationService(configuration, consoleService);

            await initService.SetupTestDatabasesAsync();

            var (useLocal, aiProvider, storageProvider, audioProvider) = await initService.SelectEnvironmentAsync();
            _useLocalEnvironment = useLocal;
            _selectedAIProvider = aiProvider;
            _selectedStorageProvider = storageProvider;

            _selectedLanguage = await initService.SelectLanguageAsync();

            await initService.InitializeServicesAsync(aiProvider, storageProvider, audioProvider);
            _serviceProvider = initService.GetServiceProvider();

            if (_serviceProvider == null)
            {
                throw new InvalidOperationException("Service provider initialization failed");
            }

            _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();

            await RunMainMenuAsync(_serviceProvider, consoleService);
        }
        catch (Exception ex)
        {
            HandleFatalError(ex);
        }
        finally
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    #endregion

    #region Private Methods

    private static async Task ShowWelcomeAnimationAsync()
    {
        var animation = new AnimationService();
        await animation.ShowWelcomeAnimationAsync();
    }

    private static IConfiguration LoadConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .Build();
    }

    private static async Task RunMainMenuAsync(IServiceProvider serviceProvider, IConsoleService console)
    {
        var menuService = new MenuService(console);
        var databaseHandler = CreateDatabaseHandler(serviceProvider, console);
        var documentHandler = CreateDocumentHandler(serviceProvider, console);
        var queryHandler = CreateQueryHandler(serviceProvider, console);
        var ollamaHandler = new OllamaHandler(console);

        while (true)
        {
            menuService.ShowMainMenu();
            var choice = await menuService.GetMenuChoiceAsync();

            var shouldExit = await HandleMenuChoice(
                choice,
                databaseHandler,
                documentHandler,
                queryHandler,
                ollamaHandler,
                console);

            if (shouldExit)
            {
                System.Console.WriteLine("\n👋 Goodbye!");
                return;
            }

            menuService.PauseForUser();
        }
    }

    private static DatabaseHandler CreateDatabaseHandler(IServiceProvider serviceProvider, IConsoleService console)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var connectionManager = serviceProvider.GetRequiredService<IDatabaseConnectionManager>();
        var schemaAnalyzer = serviceProvider.GetRequiredService<IDatabaseSchemaAnalyzer>();

        return new DatabaseHandler(console, configuration, connectionManager, schemaAnalyzer);
    }

    private static DocumentHandler CreateDocumentHandler(IServiceProvider serviceProvider, IConsoleService console)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<DocumentHandler>>();
        var documentService = serviceProvider.GetRequiredService<IDocumentService>();

        return new DocumentHandler(logger, console, documentService);
    }

    private static QueryHandler CreateQueryHandler(IServiceProvider serviceProvider, IConsoleService console)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<QueryHandler>>();
        var multiDbCoordinator = serviceProvider.GetRequiredService<IMultiDatabaseQueryCoordinator>();
        var aiService = serviceProvider.GetRequiredService<IAIService>();
        var documentService = serviceProvider.GetRequiredService<IDocumentService>();
        var documentSearchService = serviceProvider.GetRequiredService<IDocumentSearchService>();
        var schemaAnalyzer = serviceProvider.GetRequiredService<IDatabaseSchemaAnalyzer>();
        var testQueryGenerator = CreateTestQueryGenerator(serviceProvider);

        return new QueryHandler(
            logger,
            console,
            multiDbCoordinator,
            aiService,
            documentService,
            documentSearchService,
            schemaAnalyzer,
            testQueryGenerator);
    }

    private static TestQueryGenerator CreateTestQueryGenerator(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<TestQueryGenerator>>();
        var schemaAnalyzer = serviceProvider.GetRequiredService<IDatabaseSchemaAnalyzer>();
        var aiService = serviceProvider.GetRequiredService<IAIService>();
        var translationService = new TranslationService();

        return new TestQueryGenerator(logger, schemaAnalyzer, aiService, translationService);
    }

    private static async Task<bool> HandleMenuChoice(
        string? choice,
        IDatabaseHandler databaseHandler,
        IDocumentHandler documentHandler,
        IQueryHandler queryHandler,
        IOllamaHandler ollamaHandler,
        IConsoleService console)
    {
        try
        {
            switch (choice)
            {
                case "1":
                    await databaseHandler.ShowConnectionsAsync();
                    break;

                case "2":
                    await databaseHandler.RunHealthCheckAsync();
                    break;

                case "3":
                    await databaseHandler.CreateDatabaseAsync(DatabaseType.SqlServer);
                    break;

                case "4":
                    await databaseHandler.CreateDatabaseAsync(DatabaseType.MySQL);
                    break;

                case "5":
                    await databaseHandler.CreateDatabaseAsync(DatabaseType.PostgreSQL);
                    break;

                case "6":
                    await databaseHandler.ShowSchemasAsync();
                    break;

                case "7":
                    await queryHandler.AnalyzeQueryIntentAsync(_selectedLanguage);
                    break;

                case "8":
                    await queryHandler.RunTestQueriesAsync(_selectedLanguage);
                    break;

                case "9":
                    await queryHandler.RunMultiDatabaseQueryAsync(_selectedLanguage);
                    break;

                case "10":
                    await ollamaHandler.SetupModelsAsync();
                    break;

                case "11":
                    await ollamaHandler.TestVectorStoreAsync(_selectedStorageProvider.ToString());
                    break;

                case "12":
                    await documentHandler.UploadDocumentsAsync(_selectedLanguage);
                    break;

                case "13":
                    await documentHandler.ListDocumentsAsync();
                    break;

                case "14":
                    await documentHandler.ClearAllDocumentsAsync();
                    break;

                case "15":
                    await queryHandler.RunConversationalChatAsync(
                        _selectedLanguage,
                        _useLocalEnvironment,
                        _selectedAIProvider.ToString());
                    break;

                case "0":
                    return true;

                default:
                    console.WriteError("Invalid selection!");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling menu choice: {Choice}", choice);
            console.WriteError($"An error occurred: {ex.Message}", ex);
        }

        return false;
    }

    private static void HandleFatalError(Exception ex)
    {
        System.Console.ForegroundColor = ConsoleColor.Red;
        System.Console.WriteLine($"\n❌ FATAL ERROR: {ex.Message}");
        System.Console.WriteLine($"\nError Type: {ex.GetType().Name}");
        System.Console.WriteLine($"\nStack Trace:");
        System.Console.WriteLine(ex.StackTrace);

        if (ex.InnerException != null)
        {
            System.Console.WriteLine($"\nInner Exception: {ex.InnerException.Message}");
            System.Console.WriteLine($"Inner Type: {ex.InnerException.GetType().Name}");
            if (ex.InnerException.StackTrace != null)
            {
                System.Console.WriteLine($"Inner Stack:\n{ex.InnerException.StackTrace}");
            }
        }
        System.Console.ResetColor();

        _logger?.LogError(ex, "Fatal error occurred during application startup");
    }

    #endregion
}
