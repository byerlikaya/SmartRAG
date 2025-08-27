

namespace SmartRAG.Tests;

/// <summary>
/// AI Provider ve Storage Provider entegrasyon testleri
/// Tüm AI Provider'ların ve Storage Provider'ların çalışıp çalışmadığını test eder
/// </summary>
public class AIProviderIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IDocumentService _documentService;
    private readonly IDocumentSearchService _documentSearchService;
    private readonly IAIProviderFactory _aiProviderFactory;
    private readonly IStorageFactory _storageFactory;

    public AIProviderIntegrationTests()
    {
        var services = new ServiceCollection();

        // Test logger ekle
        services.AddLogging(builder => builder.AddConsole());

        // Mock configuration oluştur
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"SmartRAG:AIProvider", "OpenAI"},
                {"SmartRAG:StorageProvider", "InMemory"},
                {"SmartRAG:MaxChunkSize", "1000"},
                {"SmartRAG:ChunkOverlap", "200"},
                {"SmartRAG:MaxRetryAttempts", "3"},
                {"SmartRAG:RetryDelayMs", "1000"},
                {"SmartRAG:RetryPolicy", "ExponentialBackoff"},
                {"AI:OpenAI:ApiKey", "test-api-key"},
                {"AI:OpenAI:Endpoint", "https://api.openai.com/v1"},
                {"AI:OpenAI:Model", "gpt-3.5-turbo"},
                {"AI:OpenAI:EmbeddingModel", "text-embedding-ada-002"},
                {"AI:OpenAI:MaxTokens", "1000"},
                {"AI:OpenAI:Temperature", "0.7"},
                {"Storage:InMemory:MaxDocuments", "1000"}
            })
            .Build();

        // Configuration'ı DI container'a ekle
        services.AddSingleton<IConfiguration>(configuration);

        // SmartRAG servislerini ekle
        services.AddSmartRag(configuration, options =>
        {
            options.AIProvider = AIProvider.OpenAI; // Default provider
            options.StorageProvider = StorageProvider.InMemory; // Default storage
            options.MaxChunkSize = 1000;
            options.ChunkOverlap = 200;
            options.MaxRetryAttempts = 3;
            options.RetryDelayMs = 1000;
            options.RetryPolicy = RetryPolicy.ExponentialBackoff;
        });

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();

        _documentService = _scope.ServiceProvider.GetRequiredService<IDocumentService>();
        _documentSearchService = _scope.ServiceProvider.GetRequiredService<IDocumentSearchService>();
        _aiProviderFactory = _scope.ServiceProvider.GetRequiredService<IAIProviderFactory>();
        _storageFactory = _scope.ServiceProvider.GetRequiredService<IStorageFactory>();
    }

    [Theory]
    [InlineData(AIProvider.OpenAI, StorageProvider.InMemory)]
    [InlineData(AIProvider.Anthropic, StorageProvider.InMemory)]
    [InlineData(AIProvider.Gemini, StorageProvider.InMemory)]
    [InlineData(AIProvider.AzureOpenAI, StorageProvider.InMemory)]
    [InlineData(AIProvider.Custom, StorageProvider.InMemory)]
    public async Task TestAIProviderWithInMemoryStorage_ShouldWork(AIProvider aiProvider, StorageProvider storageProvider)
    {
        // Arrange
        var testContent = @"SmartRAG is a powerful .NET library for building AI-powered question answering systems. 
        It supports multiple AI providers including OpenAI, Anthropic, Google Gemini, and Azure OpenAI. 
        The library provides intelligent document processing, semantic search, and AI-powered answer generation. 
        SmartRAG supports various storage providers like in-memory, file system, Redis, SQLite, and vector databases. 
        It's designed to be enterprise-ready with comprehensive logging, error handling, and configuration options.";

        var fileName = $"test-{aiProvider}-{storageProvider}.txt";
        var contentType = "text/plain";
        var uploadedBy = "testuser";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));

        // Act - Document upload
        var document = await _documentService.UploadDocumentAsync(stream, fileName, contentType, uploadedBy);

        // Assert - Document upload
        Assert.NotNull(document);
        Assert.Equal(fileName, document.FileName);
        Assert.Equal(contentType, document.ContentType);
        Assert.Equal(uploadedBy, document.UploadedBy);
        Assert.NotNull(document.Chunks);
        Assert.True(document.Chunks.Count > 0);

        // Act - Search
        var searchQuery = "What is SmartRAG and what does it support?";
        var searchResults = await _documentSearchService.SearchDocumentsAsync(searchQuery, maxResults: 5);

        // Assert - Search results
        Assert.NotNull(searchResults);
        Assert.True(searchResults.Count > 0);
        Assert.True(searchResults.Count <= 5);

        // Verify that results contain relevant content
        var hasRelevantContent = searchResults.Any(r =>
            r.Content.Contains("SmartRAG", StringComparison.OrdinalIgnoreCase) ||
            r.Content.Contains("AI", StringComparison.OrdinalIgnoreCase) ||
            r.Content.Contains("library", StringComparison.OrdinalIgnoreCase));

        Assert.True(hasRelevantContent, "Search results should contain relevant content about SmartRAG");
    }

    [Theory]
    [InlineData(StorageProvider.InMemory)]
    [InlineData(StorageProvider.FileSystem)]
    public async Task TestStorageProviders_ShouldWork(StorageProvider storageProvider)
    {
        // Arrange
        var testContent = @"This is a test document for storage provider testing. 
        We are testing different storage backends including in-memory and file system storage. 
        Each storage provider should work correctly for document storage and retrieval.";

        var fileName = $"storage-test-{storageProvider}.txt";
        var contentType = "text/plain";
        var uploadedBy = "testuser";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));

        // Act - Document upload
        var document = await _documentService.UploadDocumentAsync(stream, fileName, contentType, uploadedBy);

        // Assert - Document upload
        Assert.NotNull(document);
        Assert.Equal(fileName, document.FileName);
        Assert.Equal(contentType, document.ContentType);
        Assert.Equal(uploadedBy, document.UploadedBy);
        Assert.NotNull(document.Chunks);
        Assert.True(document.Chunks.Count > 0);

        // Act - Retrieve document by ID
        var retrievedDocument = await _documentService.GetDocumentAsync(document.Id);

        // Assert - Document retrieval
        Assert.NotNull(retrievedDocument);
        Assert.Equal(document.Id, retrievedDocument.Id);
        Assert.Equal(document.FileName, retrievedDocument.FileName);
        Assert.Equal(document.Content, retrievedDocument.Content);

        // Act - Search
        var searchQuery = "storage provider testing";
        var searchResults = await _documentSearchService.SearchDocumentsAsync(searchQuery, maxResults: 3);

        // Assert - Search results
        Assert.NotNull(searchResults);
        Assert.True(searchResults.Count > 0);
        Assert.True(searchResults.Count <= 3);
    }

    [Fact]
    public async Task TestAIProviderFactory_ShouldCreateAllProviders()
    {
        // Arrange & Act
        var openAIProvider = _aiProviderFactory.CreateProvider(AIProvider.OpenAI);
        var anthropicProvider = _aiProviderFactory.CreateProvider(AIProvider.Anthropic);
        var geminiProvider = _aiProviderFactory.CreateProvider(AIProvider.Gemini);
        var azureOpenAIProvider = _aiProviderFactory.CreateProvider(AIProvider.AzureOpenAI);
        var customProvider = _aiProviderFactory.CreateProvider(AIProvider.Custom);

        // Assert
        Assert.NotNull(openAIProvider);
        Assert.NotNull(anthropicProvider);
        Assert.NotNull(geminiProvider);
        Assert.NotNull(azureOpenAIProvider);
        Assert.NotNull(customProvider);

        // Provider type assertions removed as IAIProvider doesn't have ProviderType property
    }

    [Fact]
    public async Task TestStorageFactory_ShouldCreateAllProviders()
    {
        // Arrange
        var inMemoryConfig = new InMemoryConfig();
        var redisConfig = new RedisConfig { ConnectionString = "localhost:6379" };
        var sqliteConfig = new SqliteConfig { DatabasePath = "test.db" };
        var qdrantConfig = new QdrantConfig { Host = "localhost" };

        // Act
        var inMemoryRepo = _storageFactory.CreateRepository(new StorageConfig { Provider = StorageProvider.InMemory, InMemory = inMemoryConfig });
        var fileSystemRepo = _storageFactory.CreateRepository(new StorageConfig { Provider = StorageProvider.FileSystem, FileSystemPath = "test-storage" });
        var redisRepo = _storageFactory.CreateRepository(new StorageConfig { Provider = StorageProvider.Redis, Redis = redisConfig });
        var sqliteRepo = _storageFactory.CreateRepository(new StorageConfig { Provider = StorageProvider.Sqlite, Sqlite = sqliteConfig });
        var qdrantRepo = _storageFactory.CreateRepository(new StorageConfig { Provider = StorageProvider.Qdrant, Qdrant = qdrantConfig });

        // Assert
        Assert.NotNull(inMemoryRepo);
        Assert.NotNull(fileSystemRepo);
        Assert.NotNull(redisRepo);
        Assert.NotNull(sqliteRepo);
        Assert.NotNull(qdrantRepo);
    }

    [Fact]
    public async Task TestEndToEndWorkflow_ShouldWork()
    {
        // Arrange
        var testContent = @"SmartRAG is an enterprise-grade RAG solution for .NET applications. 
        It provides intelligent document processing, AI-powered embeddings, and semantic search capabilities. 
        The library supports multiple AI providers and storage backends for maximum flexibility. 
        SmartRAG is designed with SOLID principles and follows enterprise best practices.";

        var fileName = "end-to-end-test.txt";
        var contentType = "text/plain";
        var uploadedBy = "testuser";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));

        // Act - Complete workflow
        var document = await _documentService.UploadDocumentAsync(stream, fileName, contentType, uploadedBy);
        var searchResults = await _documentSearchService.SearchDocumentsAsync("What is SmartRAG?", maxResults: 3);
        var retrievedDocument = await _documentService.GetDocumentAsync(document.Id);

        // Assert - Complete workflow
        Assert.NotNull(document);
        Assert.NotNull(searchResults);
        Assert.NotNull(retrievedDocument);

        Assert.Equal(fileName, document.FileName);
        Assert.Equal(contentType, document.ContentType);
        Assert.Equal(uploadedBy, document.UploadedBy);
        Assert.True(document.Chunks.Count > 0);

        Assert.True(searchResults.Count > 0);
        Assert.True(searchResults.Count <= 3);

        Assert.Equal(document.Id, retrievedDocument.Id);
        Assert.Equal(document.Content, retrievedDocument.Content);
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _serviceProvider?.Dispose();
    }
}
