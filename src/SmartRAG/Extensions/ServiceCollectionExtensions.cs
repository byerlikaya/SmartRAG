using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartRAG.Enums;
using SmartRAG.Factories;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using SmartRAG.Services;
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
        public static IServiceCollection AddSmartRag(this IServiceCollection services, IConfiguration configuration)
            => services.AddSmartRag(configuration, options => { });

        /// <summary>
        /// Adds SmartRag services with custom configuration
        /// </summary>
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
            services.AddSingleton<IAIService, AIService>();
            services.AddSingleton<IStorageFactory, StorageFactory>();
            services.AddScoped<SemanticSearchService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IDocumentParserService, DocumentParserService>();
            services.AddScoped<IDocumentSearchService, DocumentSearchService>();
            services.AddScoped<IImageParserService, ImageParserService>();
            
            // Audio conversion service - shared by audio parser
            services.AddScoped<AudioConversionService>();
            
            // Audio parser service - only Whisper.net
            services.AddScoped<WhisperAudioParserService>();
            services.AddScoped<IAudioParserFactory, AudioParserFactory>();
            
            // IAudioParserService registration - factory creates based on configuration
            services.AddScoped<IAudioParserService>(sp =>
            {
                var factory = sp.GetRequiredService<IAudioParserFactory>();
                var options = sp.GetRequiredService<IOptions<SmartRagOptions>>();
                return factory.CreateAudioParser(options.Value.AudioProvider);
            });
            
            services.AddScoped<IDatabaseParserService, DatabaseParserService>();
            
            // Multi-database services
            services.AddScoped<IDatabaseSchemaAnalyzer, DatabaseSchemaAnalyzer>();
            services.AddScoped<IDatabaseConnectionManager, DatabaseConnectionManager>();
            services.AddScoped<IMultiDatabaseQueryCoordinator, MultiDatabaseQueryCoordinator>();
            
            // Add memory cache for database operations
            services.AddMemoryCache();

            ConfigureStorageProvider(services, configuration);

            services.AddSingleton(sp => sp.GetRequiredService<IStorageFactory>().GetCurrentRepository());

            return services;
        }

        /// <summary>
        /// Adds SmartRag services with minimal configuration (just specify providers)
        /// </summary>
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
