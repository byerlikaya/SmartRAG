using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using SmartRAG.Providers;
using SmartRAG.Repositories;
using SmartRAG.Services;
using SmartRAG.Factories;
using Microsoft.Extensions.Options;

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
