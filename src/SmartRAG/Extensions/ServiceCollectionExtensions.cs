using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
using SmartRAG.Models;
using SmartRAG.Services.AI;
using SmartRAG.Services.Document;
using SmartRAG.Services.Search;
using SmartRAG.Services.Database;
using SmartRAG.Services.Parser;
using SmartRAG.Services.Document.Parsers;
using SmartRAG.Services.Support;
using SmartRAG.Services.Database.Strategies;
using SmartRAG.Services.Search.Strategies;
using SmartRAG.Interfaces.Database.Strategies;
using SmartRAG.Interfaces.Search.Strategies;
using System;
using System.Collections.Generic;


namespace SmartRAG.Extensions
{

    /// <summary>
    /// Extension methods for configuring SmartRag services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds SmartRag services with default configuration
        /// </summary>
        /// <param name="services">Service collection to add services to</param>
        /// <param name="configuration">Application configuration</param>
        /// <returns>Service collection for method chaining</returns>
        public static IServiceCollection AddSmartRag(this IServiceCollection services, IConfiguration configuration)
            => services.AddSmartRag(configuration, options => { });

        /// <summary>
        /// Adds SmartRag services with custom configuration
        /// </summary>
        /// <param name="services">Service collection to add services to</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="configureOptions">Action to configure SmartRag options</param>
        /// <returns>Service collection for method chaining</returns>
        public static IServiceCollection AddSmartRag(this IServiceCollection services, IConfiguration configuration, Action<SmartRagOptions> configureOptions)
        {
            // Configure SmartRagOptions using Options Pattern for non-provider settings
            services.Configure<SmartRagOptions>(options =>
            {
                // Bind from configuration first
                configuration.GetSection("SmartRAG").Bind(options);
                
                // Bind DatabaseConnections from root level
                var dbConnectionsSection = configuration.GetSection("DatabaseConnections");
                if (dbConnectionsSection.Exists())
                {
                    options.DatabaseConnections = dbConnectionsSection.Get<List<DatabaseConnectionConfig>>() ?? new List<DatabaseConnectionConfig>();
                }
                
                // Then apply custom configuration
                configureOptions(options);
            });

            // Also register as legacy singleton for backward compatibility during transition
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<SmartRagOptions>>().Value);

            services.AddSingleton<IAIProviderFactory, AIProviderFactory>();
            
            // Register IAIProvider based on configuration
            services.AddSingleton<IAIProvider>(sp => {
                var factory = sp.GetRequiredService<IAIProviderFactory>();
                var options = sp.GetRequiredService<IOptions<SmartRagOptions>>().Value;
                return factory.CreateProvider(options.AIProvider);
            });

            services.AddScoped<IAIService, AIService>();
            services.AddSingleton<IStorageFactory, StorageFactory>();
            services.AddScoped<ISemanticSearchService, SemanticSearchService>();
            services.AddScoped<ITextNormalizationService, TextNormalizationService>();
            services.AddScoped<IAIConfigurationService, AIConfigurationService>();
            services.AddScoped<IPromptBuilderService, PromptBuilderService>();
            services.AddScoped<IDocumentScoringService, DocumentScoringService>();
            services.AddScoped<ISourceBuilderService, SourceBuilderService>();
            services.AddScoped<IContextExpansionService, ContextExpansionService>();
            services.AddScoped<IQueryIntentClassifierService, QueryIntentClassifierService>();
            services.AddScoped<IConversationManagerService, ConversationManagerService>();
            services.AddScoped<IDocumentService, DocumentService>();
            
            // Register File Parsers
            services.AddScoped<IFileParser, TextFileParser>();
            services.AddScoped<IFileParser, PdfFileParser>();
            services.AddScoped<IFileParser, WordFileParser>();
            services.AddScoped<IFileParser, ExcelFileParser>();

            // Conditional registration based on features
            // Create a temporary options object to read feature flags for conditional registration
            var options = new SmartRagOptions();
            configuration.GetSection("SmartRAG").Bind(options);
            
            // Apply custom configuration to ensure we get the final feature state
            configureOptions(options);

            if (options.Features.EnableImageParsing)
            {
                services.AddScoped<IFileParser, ImageFileParser>();
                services.AddScoped<IImageParserService, ImageParserService>();
            }

            if (options.Features.EnableAudioParsing)
            {
                services.AddScoped<IFileParser, AudioFileParser>();
                // Audio conversion service - shared by audio parser
                services.AddScoped<AudioConversionService>();
                
                // Audio parser service - only Whisper.net
                services.AddScoped<WhisperAudioParserService>();
                services.AddScoped<IAudioParserFactory, AudioParserFactory>();
                
                // IAudioParserService registration - factory creates based on configuration
                services.AddScoped<IAudioParserService>(sp =>
                {
                    var factory = sp.GetRequiredService<IAudioParserFactory>();
                    var opts = sp.GetRequiredService<IOptions<SmartRagOptions>>();
                    return factory.CreateAudioParser(opts.Value.AudioProvider);
                });
            }

                // Database services - Always register to allow runtime enabling via flags
                services.AddScoped<IFileParser, DatabaseFileParser>();
                services.AddScoped<IDatabaseParserService, DatabaseParserService>();
                
                // Multi-database services
                services.AddScoped<IDatabaseSchemaAnalyzer, DatabaseSchemaAnalyzer>();
                services.AddScoped<IDatabaseConnectionManager, DatabaseConnectionManager>();
                services.AddScoped<IQueryIntentAnalyzer, QueryIntentAnalyzer>();
                
                // Register SQL Strategies
                services.AddScoped<ISqlDialectStrategy, SqliteDialectStrategy>();
                services.AddScoped<ISqlDialectStrategy, PostgreSqlDialectStrategy>();
                services.AddScoped<ISqlDialectStrategy, MySqlDialectStrategy>();
                services.AddScoped<ISqlDialectStrategy, SqlServerDialectStrategy>();
                services.AddScoped<ISqlDialectStrategyFactory, SqlDialectStrategyFactory>();
                
                services.AddScoped<ISQLQueryGenerator, SQLQueryGenerator>();
                services.AddScoped<IDatabaseQueryExecutor, DatabaseQueryExecutor>();
                services.AddScoped<IResultMerger, ResultMerger>();
                services.AddScoped<IMultiDatabaseQueryCoordinator, MultiDatabaseQueryCoordinator>();

            services.AddScoped<IDocumentParserService, DocumentParserService>();
            services.AddScoped<IDocumentSearchService, DocumentSearchService>();
            
            // Register AI Request Executor
            services.AddScoped<IAIRequestExecutor, AIRequestExecutor>();
            
            // Register Scoring Strategy
            services.AddScoped<IScoringStrategy, HybridScoringStrategy>();
            
            // Add memory cache for database operations
            services.AddMemoryCache();

            ConfigureStorageProvider(services, configuration);

            services.AddSingleton(sp => sp.GetRequiredService<IStorageFactory>().GetCurrentRepository());
            services.AddSingleton(sp => sp.GetRequiredService<IStorageFactory>().GetCurrentConversationRepository());

            return services;
        }

        /// <summary>
        /// Adds SmartRag services with minimal configuration (just specify providers)
        /// </summary>
        /// <param name="services">Service collection to add services to</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="storageProvider">Storage provider to use</param>
        /// <param name="aiProvider">AI provider to use</param>
        /// <returns>Service collection for method chaining</returns>
        public static IServiceCollection UseSmartRag(this IServiceCollection services,
                                                     IConfiguration configuration,
                                                     StorageProvider storageProvider = StorageProvider.InMemory,
                                                     AIProvider aiProvider = AIProvider.OpenAI)
            => services.AddSmartRag(configuration, options =>
            {
                options.StorageProvider = storageProvider;
                options.AIProvider = aiProvider;
            });

        private static void ConfigureStorageProvider(IServiceCollection services, IConfiguration configuration)
        {
            // Configure Redis storage
            services.Configure<RedisConfig>(options => configuration.GetSection("Storage:Redis").Bind(options));

            // Configure Qdrant storage
            services.Configure<QdrantConfig>(options => configuration.GetSection("Storage:Qdrant").Bind(options));

            // Configure SQLite storage
            services.Configure<SqliteConfig>(options => configuration.GetSection("Storage:Sqlite").Bind(options));

            // Configure InMemory storage
            services.Configure<InMemoryConfig>(options => configuration.GetSection("Storage:InMemory").Bind(options));

            // Configure FileSystem storage
            services.Configure<StorageConfig>(options => configuration.GetSection("Storage:FileSystem").Bind(options));
        }
    }
}
