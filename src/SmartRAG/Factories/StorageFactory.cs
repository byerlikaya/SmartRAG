using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Enums;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Storage;
using SmartRAG.Models;
using SmartRAG.Repositories;
using SmartRAG.Services.Storage.Qdrant;
using SmartRAG.Interfaces.AI;
using System;

namespace SmartRAG.Factories
{

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
        private readonly IServiceProvider _serviceProvider;
        private IDocumentRepository _currentRepository;

        public StorageFactory(
            IConfiguration configuration,
            IOptions<SmartRagOptions> options,
            ILoggerFactory loggerFactory,
            IAIProvider aiProvider,
            IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _options = options.Value;
            _loggerFactory = loggerFactory;
            _aiProvider = aiProvider;
            _serviceProvider = serviceProvider;

            if (Enum.IsDefined(typeof(StorageProvider), _options.StorageProvider))
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
        {
            switch (config.Provider)
            {
                case StorageProvider.InMemory:
                    return new InMemoryDocumentRepository(config.InMemory, _loggerFactory.CreateLogger<InMemoryDocumentRepository>());
                case StorageProvider.Redis:
                    var aiConfigService = _serviceProvider.GetRequiredService<IAIConfigurationService>();
                    return new RedisDocumentRepository(Options.Create(config.Redis), _loggerFactory.CreateLogger<RedisDocumentRepository>(), _aiProvider, aiConfigService);
                case StorageProvider.Qdrant:
                    // Create required services for QdrantDocumentRepository
                    var collectionManager = new QdrantCollectionManager(Options.Create(config.Qdrant), _loggerFactory.CreateLogger<QdrantCollectionManager>());
                    var embeddingService = new QdrantEmbeddingService(Options.Create(config.Qdrant), _loggerFactory.CreateLogger<QdrantEmbeddingService>());
                    var cacheManager = new QdrantCacheManager(_loggerFactory.CreateLogger<QdrantCacheManager>());
                    var searchService = new QdrantSearchService(Options.Create(config.Qdrant), _loggerFactory.CreateLogger<QdrantSearchService>(), embeddingService);
                    return new QdrantDocumentRepository(Options.Create(config.Qdrant), _loggerFactory.CreateLogger<QdrantDocumentRepository>(), collectionManager, embeddingService, cacheManager, searchService);
                default:
                    throw new ArgumentException($"Unsupported storage provider: {config.Provider}");
            }
        }

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
            _configuration.GetSection("Storage:InMemory").Bind(config.InMemory);
            _configuration.GetSection("Storage:Qdrant").Bind(config.Qdrant);

            return config;
        }

        public IDocumentRepository GetCurrentRepository()
        {
            if (_currentRepository == null)
            {
                _currentRepository = CreateRepository(_currentProvider);
            }
            return _currentRepository;
        }

        public IConversationRepository CreateConversationRepository(StorageConfig config)
        {
            switch (config.Provider)
            {
                case StorageProvider.InMemory:
                    return new InMemoryConversationRepository(_loggerFactory.CreateLogger<InMemoryConversationRepository>());
                case StorageProvider.Redis:
                    return new RedisConversationRepository(Options.Create(config.Redis), _loggerFactory.CreateLogger<RedisConversationRepository>());
                case StorageProvider.Qdrant:
                    // Qdrant doesn't support conversation storage natively in this implementation, fallback to InMemory
                    return new InMemoryConversationRepository(_loggerFactory.CreateLogger<InMemoryConversationRepository>());
                default:
                    throw new ArgumentException($"Unsupported storage provider for conversation: {config.Provider}");
            }
        }

        public IConversationRepository CreateConversationRepository(StorageProvider provider)
        {
            var config = GetStorageConfig();
            config.Provider = provider;
            return CreateConversationRepository(config);
        }

        private IConversationRepository _currentConversationRepository;

        public IConversationRepository GetCurrentConversationRepository()
        {
            if (_currentConversationRepository == null)
            {
                _currentConversationRepository = CreateConversationRepository(_currentProvider);
            }
            return _currentConversationRepository;
        }
    }
}
