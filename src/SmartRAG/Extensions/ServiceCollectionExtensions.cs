#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading;
using SmartRAG.Enums;
using SmartRAG.Factories;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Search;
using SmartRAG.Interfaces.Database;
using SmartRAG.Interfaces.Storage;
using SmartRAG.Interfaces.Parser;
using SmartRAG.Interfaces.Parser.Strategies;
using SmartRAG.Interfaces.Support;
using SmartRAG.Services.Database.Validation;
using SmartRAG.Services.Database.Prompts;
using SmartRAG.Models;
using SmartRAG.Services.AI;
using SmartRAG.Services.Document;
using SmartRAG.Services.Search;
using SmartRAG.Services.Database;
using SmartRAG.Services.Parser;
using SmartRAG.Services.Document.Parsers;
using SmartRAG.Services.Support;
using SmartRAG.Services.Database.Strategies;
using SmartRAG.Interfaces.Database.Strategies;
using SmartRAG.Interfaces.Mcp;
using SmartRAG.Services.Mcp;
using SmartRAG.Interfaces.FileWatcher;
using SmartRAG.Services.FileWatcher;
using SmartRAG.Services.Startup;
using System;
using System.Collections.Generic;

namespace SmartRAG.Extensions
{
    /// <summary>
    /// Extension methods for configuring SmartRag services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSmartRag(this IServiceCollection services, IConfiguration configuration)
            => services.AddSmartRag(configuration, options => { });

        /// <summary>
        /// Adds SmartRag services with custom configuration.
        /// For Web API: IHost automatically starts hosted services.
        /// For Console: Use UseSmartRag() instead - returns IServiceProvider with auto-started services.
        /// </summary>
        public static IServiceCollection AddSmartRag(this IServiceCollection services, IConfiguration configuration, Action<SmartRagOptions> configureOptions)
        {
            RegisterConfiguration(services, configuration, configureOptions);

            var options = BuildOptions(configuration, configureOptions);

            RegisterCoreServices(services);
            RegisterDocumentServices(services);
            RegisterDatabaseServices(services);
            RegisterParserServices(services, options);
            RegisterFeatureBasedServices(services, options);
            RegisterStorageServices(services, configuration);
            RegisterStartupServices(services, options);

            return services;
        }

        /// <summary>
        /// Quick setup for console applications - configures services and returns ready-to-use service provider.
        /// Automatically starts hosted services (MCP client, file watcher) if enabled.
        /// For Web API: Use AddSmartRag() instead - IHost manages lifecycle.
        /// </summary>
        public static IServiceProvider UseSmartRag(this IServiceCollection services,
                                                   IConfiguration configuration,
                                                   StorageProvider storageProvider = StorageProvider.InMemory,
                                                   AIProvider aiProvider = AIProvider.OpenAI,
                                                   ConversationStorageProvider? conversationStorageProvider = null,
                                                   string? defaultLanguage = null)
        {
            services.AddSmartRag(configuration, options =>
            {
                options.StorageProvider = storageProvider;
                options.AIProvider = aiProvider;
                if (conversationStorageProvider.HasValue)
                {
                    options.ConversationStorageProvider = conversationStorageProvider.Value;
                }
                if (!string.IsNullOrEmpty(defaultLanguage))
                {
                    options.DefaultLanguage = defaultLanguage;
                }
            });

            return services.BuildServiceProviderWithSmartRag();
        }

        private static void RegisterConfiguration(IServiceCollection services, IConfiguration configuration, Action<SmartRagOptions> configureOptions)
        {
            services.Configure<SmartRagOptions>(options =>
            {
                configuration.GetSection("SmartRAG").Bind(options);

                var dbConnectionsSection = configuration.GetSection("DatabaseConnections");
                if (dbConnectionsSection.Exists())
                {
                    options.DatabaseConnections = dbConnectionsSection.Get<List<DatabaseConnectionConfig>>() ?? new List<DatabaseConnectionConfig>();
                }

                configureOptions(options);
            });

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<SmartRagOptions>>().Value);
        }

        private static SmartRagOptions BuildOptions(IConfiguration configuration, Action<SmartRagOptions> configureOptions)
        {
            var options = new SmartRagOptions();
            configuration.GetSection("SmartRAG").Bind(options);

            var dbConnectionsSection = configuration.GetSection("DatabaseConnections");
            if (dbConnectionsSection.Exists())
            {
                options.DatabaseConnections = dbConnectionsSection.Get<List<DatabaseConnectionConfig>>() ?? new List<DatabaseConnectionConfig>();
            }

            // Explicitly bind Features section to ensure it's loaded correctly
            // This is necessary because nested object binding can sometimes fail
            var featuresSection = configuration.GetSection("SmartRAG:Features");
            if (featuresSection.Exists())
            {
                options.Features ??= new FeatureToggles();
                featuresSection.Bind(options.Features);
            }

            configureOptions(options);

            return options;
        }

        private static void RegisterCoreServices(IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddSingleton<IAIProviderFactory, AIProviderFactory>();

            services.AddSingleton<IAIProvider>(sp =>
            {
                var factory = sp.GetRequiredService<IAIProviderFactory>();
                var options = sp.GetRequiredService<IOptions<SmartRagOptions>>().Value;
                return factory.CreateProvider(options.AIProvider);
            });

            services.AddScoped<IAIService, AIService>();
            services.AddSingleton<IStorageFactory, StorageFactory>();
            services.AddScoped<ITextNormalizationService, TextNormalizationService>();
            services.AddScoped<IAIConfigurationService, AIConfigurationService>();
            services.AddScoped<IConversationManagerService, ConversationManagerService>();
            services.AddScoped<IPromptBuilderService>(sp =>
            {
                var conversationManagerLazy = new Lazy<IConversationManagerService>(() => sp.GetRequiredService<IConversationManagerService>());
                var options = sp.GetRequiredService<IOptions<SmartRagOptions>>();
                return new PromptBuilderService(conversationManagerLazy, options);
            });
            services.AddScoped<IDocumentScoringService, DocumentScoringService>();
            services.AddScoped<ISourceBuilderService, SourceBuilderService>();
            services.AddScoped<IContextExpansionService, ContextExpansionService>();
            services.AddScoped<IQueryIntentClassifierService, QueryIntentClassifierService>();
            services.AddScoped<IAIRequestExecutor, AIRequestExecutor>();
            services.AddMemoryCache();
        }

        private static void RegisterDocumentServices(IServiceCollection services)
        {
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IQueryWordMatcherService, QueryWordMatcherService>();
            services.AddScoped<IDocumentRelevanceCalculatorService, DocumentRelevanceCalculatorService>();
            services.AddScoped<IQueryPatternAnalyzerService, QueryPatternAnalyzerService>();
            services.AddScoped<IChunkPrioritizerService, ChunkPrioritizerService>();
            services.AddScoped<IQueryAnalysisService, QueryAnalysisService>();
            services.AddScoped<IResponseBuilderService, ResponseBuilderService>();
            services.AddScoped<IQueryStrategyOrchestratorService, QueryStrategyOrchestratorService>();
            services.AddScoped<IQueryStrategyExecutorService>(sp =>
            {
                var ragAnswerGeneratorLazy = new Lazy<IRagAnswerGeneratorService>(() => sp.GetRequiredService<IDocumentSearchService>() as IRagAnswerGeneratorService ?? throw new InvalidOperationException("DocumentSearchService must implement IRagAnswerGeneratorService"));
                return new QueryStrategyExecutorService(
                    sp.GetService<IMultiDatabaseQueryCoordinator>(),
                    sp.GetRequiredService<ILogger<QueryStrategyExecutorService>>(),
                    ragAnswerGeneratorLazy,
                    sp.GetRequiredService<IResponseBuilderService>(),
                    sp.GetService<IConversationManagerService>(),
                    sp.GetRequiredService<IOptions<SmartRagOptions>>());
            });
            services.AddScoped<IDocumentSearchStrategyService, DocumentSearchStrategyService>();
            services.AddScoped<IDocumentParserService, DocumentParserService>();
            services.AddScoped<IDocumentSearchService, DocumentSearchService>();
            services.AddScoped<IRagAnswerGeneratorService>(sp => sp.GetRequiredService<IDocumentSearchService>() as IRagAnswerGeneratorService ?? throw new InvalidOperationException("DocumentSearchService must implement IRagAnswerGeneratorService"));
        }

        private static void RegisterDatabaseServices(IServiceCollection services)
        {
            services.AddScoped<IDatabaseParserService, DatabaseParserService>();
            services.AddScoped<IDatabaseSchemaAnalyzer, DatabaseSchemaAnalyzer>();
            services.AddScoped<IDatabaseConnectionManager, DatabaseConnectionManager>();
            services.AddScoped<IQueryIntentAnalyzer, QueryIntentAnalyzer>();
            services.AddScoped<ISqlDialectStrategy, SqliteDialectStrategy>();
            services.AddScoped<ISqlDialectStrategy, PostgreSqlDialectStrategy>();
            services.AddScoped<ISqlDialectStrategy, MySqlDialectStrategy>();
            services.AddScoped<ISqlDialectStrategy, SqlServerDialectStrategy>();
            services.AddScoped<ISqlDialectStrategyFactory, SqlDialectStrategyFactory>();
            services.AddScoped<ISqlValidator, SqlValidator>();
            services.AddScoped<ISqlPromptBuilder, SqlPromptBuilder>();
            services.AddScoped<ISqlQueryGenerator, SQLQueryGenerator>();
            services.AddScoped<IDatabaseQueryExecutor, DatabaseQueryExecutor>();
            services.AddScoped<IResultMerger, ResultMerger>();
            services.AddScoped<IMultiDatabaseQueryCoordinator, MultiDatabaseQueryCoordinator>();
        }

        private static void RegisterParserServices(IServiceCollection services, SmartRagOptions options)
        {
            services.AddScoped<IFileParser, TextFileParser>();
            services.AddScoped<IFileParser, WordFileParser>();
            services.AddScoped<IFileParser, ExcelFileParser>();
            services.AddScoped<IFileParser, DatabaseFileParser>();
            services.AddScoped<IImageParserService, ImageParserService>();
            services.AddScoped<IFileParser, PdfFileParser>();

            if (options.Features.EnableImageSearch)
            {
                services.AddScoped<IFileParser, ImageFileParser>();
            }

            if (options.Features.EnableAudioSearch)
            {
                services.AddScoped<IFileParser, AudioFileParser>();
                services.AddScoped<AudioConversionService>();
                services.AddScoped<IAudioParserService, WhisperAudioParserService>();
            }
        }

        private static void RegisterFeatureBasedServices(IServiceCollection services, SmartRagOptions options)
        {
            services.AddHttpClient();
            services.AddSingleton<IMcpClient, McpClient>();
            services.AddSingleton<IMcpConnectionManager, McpConnectionManager>();
            services.AddScoped<IMcpIntegrationService, McpIntegrationService>();

            if (options.Features.EnableFileWatcher)
            {
                services.AddSingleton<IFileWatcherService, FileWatcherService>();
            }
        }

        private static void RegisterStorageServices(IServiceCollection services, IConfiguration configuration)
        {
            ConfigureStorageProvider(services, configuration);

            services.AddSingleton(sp => sp.GetRequiredService<IStorageFactory>().GetCurrentRepository());
            services.AddSingleton(sp => sp.GetRequiredService<IStorageFactory>().GetCurrentConversationRepository());
        }

        private static void RegisterStartupServices(IServiceCollection services, SmartRagOptions options)
        {
            options.Features ??= new FeatureToggles();

            var enableMcp = options.Features.EnableMcpSearch;
            var enableFileWatcher = options.Features.EnableFileWatcher;

            if (enableMcp || enableFileWatcher)
            {
                services.AddHostedService<SmartRagStartupService>();
            }
        }

        private static void ConfigureStorageProvider(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RedisConfig>(options => configuration.GetSection("Storage:Redis").Bind(options));
            services.Configure<QdrantConfig>(options => configuration.GetSection("Storage:Qdrant").Bind(options));
            services.Configure<SqliteConfig>(options => configuration.GetSection("Storage:Sqlite").Bind(options));
            services.Configure<InMemoryConfig>(options => configuration.GetSection("Storage:InMemory").Bind(options));
            services.Configure<StorageConfig>(options => configuration.GetSection("Storage:FileSystem").Bind(options));
        }

        /// <summary>
        /// Builds service provider and automatically starts SmartRAG hosted services if configured.
        /// For console applications: Automatically detects and starts hosted services (MCP client, file watcher).
        /// For Web API with IHost: Automatic startup not needed - IHost manages lifecycle.
        /// </summary>
        public static IServiceProvider BuildServiceProviderWithSmartRag(this IServiceCollection services, bool validateScopes = false)
        {
            var serviceProvider = services.BuildServiceProvider(validateScopes);

            // Find SmartRagStartupService among registered IHostedService instances
            var hostedServices = serviceProvider.GetServices<IHostedService>();
            var logger = serviceProvider.GetService<ILogger<SmartRagStartupService>>();

            var startupService = hostedServices.OfType<SmartRagStartupService>().FirstOrDefault();

            if (startupService != null)
            {
                logger?.LogInformation("SmartRagStartupService found, starting hosted services...");
                try
                {
                    // Start hosted services asynchronously with ConfigureAwait(false) to avoid deadlock
                    // This is safe for console applications where there's no SynchronizationContext
                    startupService.StartAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to start SmartRAG hosted services");
                    throw;
                }
            }
            else
            {
                logger?.LogWarning("SmartRagStartupService not found. MCP client and file watcher may not be initialized.");
            }

            return serviceProvider;
        }
    }

}
