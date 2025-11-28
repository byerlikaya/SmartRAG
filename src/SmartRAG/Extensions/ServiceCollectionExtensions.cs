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

            // Also register as legacy singleton for backward compatibility during transition
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<SmartRagOptions>>().Value);

            services.AddSingleton<IAIProviderFactory, AIProviderFactory>();
            
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
            
            services.AddScoped<IFileParser, TextFileParser>();
            services.AddScoped<IFileParser, PdfFileParser>();
            services.AddScoped<IFileParser, WordFileParser>();
            services.AddScoped<IFileParser, ExcelFileParser>();

            var options = new SmartRagOptions();
            configuration.GetSection("SmartRAG").Bind(options);
            configureOptions(options);

            if (options.Features.EnableImageParsing)
            {
                services.AddScoped<IFileParser, ImageFileParser>();
                services.AddScoped<IImageParserService, ImageParserService>();
            }

            if (options.Features.EnableAudioParsing)
            {
                services.AddScoped<IFileParser, AudioFileParser>();
                services.AddScoped<AudioConversionService>();
                services.AddScoped<WhisperAudioParserService>();
                services.AddScoped<IAudioParserFactory, AudioParserFactory>();
                
                services.AddScoped<IAudioParserService>(sp =>
                {
                    var factory = sp.GetRequiredService<IAudioParserFactory>();
                    var opts = sp.GetRequiredService<IOptions<SmartRagOptions>>();
                    return factory.CreateAudioParser(opts.Value.AudioProvider);
                });
            }

            services.AddScoped<IFileParser, DatabaseFileParser>();
            services.AddScoped<IDatabaseParserService, DatabaseParserService>();
            services.AddScoped<IDatabaseSchemaAnalyzer, DatabaseSchemaAnalyzer>();
            services.AddScoped<IDatabaseConnectionManager, DatabaseConnectionManager>();
            services.AddScoped<IQueryIntentAnalyzer, QueryIntentAnalyzer>();
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
            services.AddScoped<IAIRequestExecutor, AIRequestExecutor>();
            services.AddScoped<IScoringStrategy, HybridScoringStrategy>();
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
            services.Configure<RedisConfig>(options => configuration.GetSection("Storage:Redis").Bind(options));
            services.Configure<QdrantConfig>(options => configuration.GetSection("Storage:Qdrant").Bind(options));
            services.Configure<SqliteConfig>(options => configuration.GetSection("Storage:Sqlite").Bind(options));
            services.Configure<InMemoryConfig>(options => configuration.GetSection("Storage:InMemory").Bind(options));
            services.Configure<StorageConfig>(options => configuration.GetSection("Storage:FileSystem").Bind(options));
        }
    }
}
