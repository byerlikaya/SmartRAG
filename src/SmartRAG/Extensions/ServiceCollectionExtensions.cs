using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartRAG.Enums;
using SmartRAG.Factories;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using SmartRAG.Repositories;
using SmartRAG.Services;

namespace SmartRAG.Extensions;

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
        var options = new SmartRagOptions();

        configureOptions(options);


        services.AddSingleton<IAIProviderFactory, AIProviderFactory>();
        services.AddSingleton<IAIService, AIService>();
        services.AddSingleton<IStorageFactory, StorageFactory>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentParserService, DocumentParserService>();

        services.AddSingleton(options);

        ConfigureStorageProvider(services, configuration, options);

        services.AddSingleton(sp => sp.GetRequiredService<IStorageFactory>().GetCurrentRepository());

        return services;
    }

    /// <summary>
    /// Adds SmartRag services with minimal configuration (just specify providers)
    /// </summary>
    public static IServiceCollection UseSmartRag(this IServiceCollection services,
                                                 IConfiguration configuration,
                                                 StorageProvider storageProvider = StorageProvider.InMemory,
                                                 AIProvider aiProvider = AIProvider.Gemini)
        => services.AddSmartRag(configuration, options =>
        {
            options.StorageProvider = storageProvider;
            options.AIProvider = aiProvider;
        });

    private static void ConfigureStorageProvider(IServiceCollection services, IConfiguration configuration, SmartRagOptions options)
    {
        var storageProvider = options.StorageProvider.ToString();

        if (storageProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        {
            ConfigureRedisStorage(services, configuration);
        }
        else if (storageProvider.Equals("Qdrant", StringComparison.OrdinalIgnoreCase))
        {
            ConfigureQdrantStorage(services, configuration);
        }
        else if (storageProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            ConfigureSQLiteStorage(services, configuration);
        }
        else if (storageProvider.Equals("FileSystem", StringComparison.OrdinalIgnoreCase))
        {
            ConfigureFileSystemStorage(services, configuration);
        }
    }


    private static void ConfigureRedisStorage(IServiceCollection services, IConfiguration configuration)
    {
        var redisConfig = new RedisConfig();
        configuration.GetSection("Storage:Redis").Bind(redisConfig);

        services.AddSingleton(redisConfig);
        services.AddSingleton<RedisDocumentRepository>();
    }

    private static void ConfigureQdrantStorage(IServiceCollection services, IConfiguration configuration)
    {
        var qdrantConfig = new QdrantConfig();
        configuration.GetSection("Storage:Qdrant").Bind(qdrantConfig);

        services.AddSingleton(qdrantConfig);
        services.AddSingleton<QdrantDocumentRepository>();
    }

    private static void ConfigureSQLiteStorage(IServiceCollection services, IConfiguration configuration)
    {
        var sqliteConfig = new SqliteConfig();
        configuration.GetSection("Storage:Sqlite").Bind(sqliteConfig);

        services.AddSingleton(sqliteConfig);
        services.AddSingleton<SqliteDocumentRepository>();
    }

    private static void ConfigureFileSystemStorage(IServiceCollection services, IConfiguration configuration)
    {
        var fileSystemPath = configuration["Storage:FileSystemPath"] ?? "Documents";
        services.AddSingleton(new { FileSystemPath = fileSystemPath });
    }
}