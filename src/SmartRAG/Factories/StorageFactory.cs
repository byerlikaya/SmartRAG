
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
    private readonly IAIProvider _aiProvider;

    public StorageFactory(
        IConfiguration configuration,
        IOptions<SmartRagOptions> options,
        ILoggerFactory loggerFactory,
        IAIProvider aiProvider)
    {
        _configuration = configuration;
        _options = options.Value;
        _loggerFactory = loggerFactory;
        _aiProvider = aiProvider;

        if (Enum.IsDefined(typeof(StorageProvider), _options.StorageProvider))
        {
            _currentProvider = _options.StorageProvider;
        }
        else
        {
            var providerString = _configuration["Storage:Provider"] ?? "InMemory";

            _currentProvider = Enum.TryParse<StorageProvider>(providerString, true, out var provider) ?
                provider :
                StorageProvider.InMemory;
        }
    }

    private IDocumentRepository CreateRepository(StorageConfig config, IServiceProvider scope)
    {
        switch (config.Provider)
        {
            case StorageProvider.InMemory:
                return new InMemoryDocumentRepository(config.InMemory, _loggerFactory.CreateLogger<InMemoryDocumentRepository>());
            case StorageProvider.Redis:
                var aiConfigService = scope.GetRequiredService<IAIConfigurationService>();
                return new RedisDocumentRepository(Options.Create(config.Redis), _loggerFactory.CreateLogger<RedisDocumentRepository>(), _aiProvider, aiConfigService);
            case StorageProvider.Qdrant:
                var collectionManager = new QdrantCollectionManager(Options.Create(config.Qdrant), _loggerFactory.CreateLogger<QdrantCollectionManager>());
                var embeddingService = new QdrantEmbeddingService(Options.Create(config.Qdrant), _loggerFactory.CreateLogger<QdrantEmbeddingService>());
                var aiService = scope.GetRequiredService<IAIService>();
                var searchService = new QdrantSearchService(Options.Create(config.Qdrant), _loggerFactory.CreateLogger<QdrantSearchService>());
                return new QdrantDocumentRepository(Options.Create(config.Qdrant), _loggerFactory.CreateLogger<QdrantDocumentRepository>(), collectionManager, embeddingService, aiService, searchService);
            default:
                throw new ArgumentException($"Unsupported storage provider: {config.Provider}");
        }
    }

    private IDocumentRepository CreateRepository(StorageProvider provider, IServiceProvider scope)
    {
        var config = GetStorageConfig();
        config.Provider = provider;
        return CreateRepository(config, scope);
    }

    public StorageProvider GetCurrentProvider() => _currentProvider;

    private StorageConfig GetStorageConfig()
    {
        var config = new StorageConfig();

        _configuration.GetSection("Storage:Redis").Bind(config.Redis);
        _configuration.GetSection("Storage:InMemory").Bind(config.InMemory);
        _configuration.GetSection("Storage:Qdrant").Bind(config.Qdrant);

        return config;
    }

    public IDocumentRepository GetCurrentRepository(IServiceProvider scopedProvider)
        => scopedProvider == null
            ? throw new ArgumentNullException(nameof(scopedProvider))
            : CreateRepository(_currentProvider, scopedProvider);

    private IConversationRepository CreateConversationRepository(ConversationStorageProvider provider)
    {
        switch (provider)
        {
            case ConversationStorageProvider.InMemory:
                return new InMemoryConversationRepository(_loggerFactory.CreateLogger<InMemoryConversationRepository>());
            case ConversationStorageProvider.Redis:
                var redisConfig = new RedisConfig();
                _configuration.GetSection("Storage:Redis").Bind(redisConfig);
                return new RedisConversationRepository(Options.Create(redisConfig), _loggerFactory.CreateLogger<RedisConversationRepository>());
            case ConversationStorageProvider.SQLite:
                var sqliteConfig = new SqliteConfig();
                _configuration.GetSection("Storage:Sqlite").Bind(sqliteConfig);
                return new SqliteConversationRepository(Options.Create(sqliteConfig), _loggerFactory.CreateLogger<SqliteConversationRepository>());
            case ConversationStorageProvider.FileSystem:
                var fileSystemPath = _configuration["Storage:FileSystem:BasePath"] ?? "Documents";
                return new FileSystemConversationRepository(fileSystemPath, _loggerFactory.CreateLogger<FileSystemConversationRepository>());
            default:
                throw new ArgumentException($"Unsupported conversation storage provider: {provider}");
        }
    }

    private IConversationRepository _currentConversationRepository;

    public IConversationRepository GetCurrentConversationRepository()
    {
        if (_currentConversationRepository != null)
            return _currentConversationRepository;

        var conversationProvider = _options.ConversationStorageProvider ?? ConversationStorageProvider.InMemory;
        _currentConversationRepository = CreateConversationRepository(conversationProvider);
        return _currentConversationRepository;
    }
}

