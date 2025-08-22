using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using SmartRAG.Repositories;

namespace SmartRAG.Factories;

/// <summary>
/// Factory implementation for creating document storage repositories
/// </summary>
public class StorageFactory : IStorageFactory
{
    private readonly IConfiguration _configuration;
    private readonly StorageProvider _currentProvider;
    private readonly SmartRagOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private IDocumentRepository? _currentRepository;

    public StorageFactory(IConfiguration configuration, IOptions<SmartRagOptions> options, ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _options = options.Value;
        _loggerFactory = loggerFactory;

        if (Enum.IsDefined(_options.StorageProvider))
        {
            _currentProvider = _options.StorageProvider;
        }
        else
        {
            var providerString = _configuration["Storage:Provider"] ?? "InMemory";

            if (Enum.TryParse<StorageProvider>(providerString, true, out var provider))
            {
                _currentProvider = provider;
            }
            else
            {
                _currentProvider = StorageProvider.InMemory;
            }
        }
    }

    public IDocumentRepository CreateRepository(StorageConfig config)
        => config.Provider switch
        {
            StorageProvider.InMemory => new InMemoryDocumentRepository(config.InMemory, _loggerFactory.CreateLogger<InMemoryDocumentRepository>()),
            StorageProvider.FileSystem => new FileSystemDocumentRepository(config.FileSystemPath, _loggerFactory.CreateLogger<FileSystemDocumentRepository>()),
            StorageProvider.Redis => new RedisDocumentRepository(Options.Create(config.Redis), _loggerFactory.CreateLogger<RedisDocumentRepository>()),
            StorageProvider.Sqlite => new SqliteDocumentRepository(Options.Create(config.Sqlite), _loggerFactory.CreateLogger<SqliteDocumentRepository>()),
            StorageProvider.Qdrant => new QdrantDocumentRepository(Options.Create(config.Qdrant), _loggerFactory.CreateLogger<QdrantDocumentRepository>()),
            _ => throw new ArgumentException($"Unsupported storage provider: {config.Provider}")
        };

    public IDocumentRepository CreateRepository(StorageProvider provider)
    {
        var config = GetStorageConfig();
        config.Provider = provider;
        return CreateRepository(config);
    }

    public StorageProvider GetCurrentProvider() => _currentProvider;

    private StorageConfig GetStorageConfig()
    {
        var config = new StorageConfig();

        _configuration.GetSection("Storage:Redis").Bind(config.Redis);
        _configuration.GetSection("Storage:Sqlite").Bind(config.Sqlite);
        _configuration.GetSection("Storage:InMemory").Bind(config.InMemory);
        _configuration.GetSection("Storage:Qdrant").Bind(config.Qdrant);

        config.FileSystemPath = _configuration["Storage:FileSystemPath"] ?? "Documents";

        return config;
    }

    public IDocumentRepository GetCurrentRepository() => _currentRepository ??= CreateRepository(_currentProvider);
}