namespace SmartRAG.Repositories;

/// <summary>
/// File system document repository implementation
/// </summary>
public class FileSystemDocumentRepository : IDocumentRepository
{
    #region Constants

    // File and path constants
    private const string MetadataFileName = "metadata.json";
    private const string DocumentFileExtension = ".json";

    // Search constants
    private const int DefaultMaxSearchResults = 5;

    // JSON serialization constants
    private const bool WriteIndented = true;

    #endregion

    #region Fields

    private readonly string _basePath;
    private readonly string _metadataFile;
    private readonly System.Threading.Lock _lock = new();
    private readonly ILogger<FileSystemDocumentRepository> _logger;

    #endregion

    #region Properties

    protected ILogger Logger => _logger;

    public string StoragePath => _basePath;

    #endregion

    #region Constructor

    /// <summary>
    /// Shared JsonSerializerOptions for consistent serialization
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = WriteIndented,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Shared JsonSerializerOptions for deserialization
    /// </summary>
    private static readonly JsonSerializerOptions _jsonDeserializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FileSystemDocumentRepository(string basePath, ILogger<FileSystemDocumentRepository> logger)
    {
        _basePath = Path.GetFullPath(basePath);
        _metadataFile = Path.Combine(_basePath, MetadataFileName);
        _logger = logger;

        Directory.CreateDirectory(_basePath);

        if (!File.Exists(_metadataFile))
        {
            SaveMetadata([]);
        }
    }

    #endregion

    #region Public Methods

    public Task<SmartRAG.Entities.Document> AddAsync(SmartRAG.Entities.Document document)
    {
        lock (_lock)
        {
            try
            {
                var documents = LoadMetadata();

                if (documents.Any(d => d.Id == document.Id))
                {
                    RepositoryLogMessages.LogDocumentAlreadyExists(Logger, document.Id, null);
                    throw new InvalidOperationException($"Document with ID {document.Id} already exists");
                }

                var documentPath = GetDocumentPath(document.Id);
                var documentData = CreateDocumentData(document);
                var json = JsonSerializer.Serialize(documentData, _jsonOptions);

                File.WriteAllText(documentPath, json);
                documents.Add(document);
                SaveMetadata(documents);

                RepositoryLogMessages.LogDocumentAdded(Logger, document.FileName, document.Id, null);
                return Task.FromResult(document);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogDocumentAddFailed(Logger, document.FileName, ex);
                throw;
            }
        }
    }

    public Task<SmartRAG.Entities.Document?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            try
            {
                var documents = LoadMetadata();
                var document = documents.FirstOrDefault(d => d.Id == id);

                if (document != null)
                {
                    RepositoryLogMessages.LogDocumentRetrieved(Logger, document.FileName, id, null);
                }
                else
                {
                    RepositoryLogMessages.LogDocumentNotFound(Logger, id, null);
                }

                return Task.FromResult(document);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogDocumentRetrievalFailed(Logger, id, ex);
                throw;
            }
        }
    }

    public Task<List<SmartRAG.Entities.Document>> GetAllAsync()
    {
        lock (_lock)
        {
            try
            {
                var documents = LoadMetadata();
                RepositoryLogMessages.LogDocumentsRetrieved(Logger, documents.Count, null);
                return Task.FromResult(documents.ToList());
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogDocumentsRetrievalFailed(Logger, ex);
                throw;
            }
        }
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        lock (_lock)
        {
            try
            {
                var documents = LoadMetadata();
                var document = documents.FirstOrDefault(d => d.Id == id);

                if (document == null)
                {
                    RepositoryLogMessages.LogDocumentDeleteNotFound(Logger, id, null);
                    return Task.FromResult(false);
                }

                var documentPath = GetDocumentPath(id);

                if (File.Exists(documentPath))
                {
                    File.Delete(documentPath);
                }

                documents.Remove(document);
                SaveMetadata(documents);

                RepositoryLogMessages.LogDocumentDeleted(Logger, document.FileName, id, null);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogDocumentDeleteFailed(Logger, id, ex);
                throw;
            }
        }
    }

    public Task<int> GetCountAsync()
    {
        lock (_lock)
        {
            try
            {
                var documents = LoadMetadata();
                var count = documents.Count;
                RepositoryLogMessages.LogDocumentCountRetrieved(Logger, count, null);
                return Task.FromResult(count);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogDocumentCountRetrievalFailed(Logger, ex);
                throw;
            }
        }
    }

    /// <summary>
    /// Loads metadata from file
    /// </summary>
    private List<SmartRAG.Entities.Document> LoadMetadata()
    {
        try
        {
            if (!File.Exists(_metadataFile))
                return [];

            var json = File.ReadAllText(_metadataFile);
            var documents = JsonSerializer.Deserialize<List<SmartRAG.Entities.Document>>(json, _jsonDeserializeOptions);

            return documents ?? [];
        }
        catch (Exception ex)
        {
            RepositoryLogMessages.LogMetadataLoadFailed(Logger, ex);
            // If metadata file is corrupted, return empty list
            return [];
        }
    }

    /// <summary>
    /// Saves metadata to file
    /// </summary>
    private void SaveMetadata(List<SmartRAG.Entities.Document> documents)
    {
        try
        {
            var json = JsonSerializer.Serialize(documents, _jsonOptions);
            File.WriteAllText(_metadataFile, json);
            RepositoryLogMessages.LogMetadataSaved(Logger, documents.Count, null);
        }
        catch (Exception ex)
        {
            RepositoryLogMessages.LogMetadataSaveFailed(Logger, ex);
            throw;
        }
    }

    /// <summary>
    /// Gets document file path
    /// </summary>
    private string GetDocumentPath(Guid id)
    {
        return Path.Combine(_basePath, $"{id}{DocumentFileExtension}");
    }

    public long GetTotalSize()
    {
        lock (_lock)
        {
            try
            {
                var documents = LoadMetadata();
                var totalSize = documents.Sum(d => d.FileSize);
                RepositoryLogMessages.LogTotalSizeRetrieved(Logger, totalSize, null);
                return totalSize;
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogTotalSizeRetrievalFailed(Logger, ex);
                throw;
            }
        }
    }

    public Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = DefaultMaxSearchResults)
    {
        lock (_lock)
        {
            try
            {
                var documents = LoadMetadata();
                var normalizedQuery = SmartRAG.Extensions.SearchTextExtensions.NormalizeForSearch(query);
                var relevantChunks = PerformSearch(documents, normalizedQuery, maxResults);

                RepositoryLogMessages.LogSearchCompleted(Logger, query, relevantChunks.Count, maxResults, null);
                return Task.FromResult(relevantChunks);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogSearchFailed(Logger, query, ex);
                throw;
            }
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Creates document data object for serialization
    /// </summary>
    private static object CreateDocumentData(SmartRAG.Entities.Document document)
    {
        return new
        {
            Id = document.Id,
            FileName = document.FileName,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            UploadedAt = document.UploadedAt,
            UploadedBy = document.UploadedBy,
            Chunks = document.Chunks
        };
    }

    /// <summary>
    /// Performs search operation on documents
    /// </summary>
    private static List<DocumentChunk> PerformSearch(List<SmartRAG.Entities.Document> documents, string normalizedQuery, int maxResults)
    {
        var relevantChunks = new List<DocumentChunk>();

        foreach (var document in documents)
        {
            foreach (var chunk in document.Chunks)
            {
                var normalizedChunk = SmartRAG.Extensions.SearchTextExtensions.NormalizeForSearch(chunk.Content);
                if (normalizedChunk.Contains(normalizedQuery))
                {
                    relevantChunks.Add(chunk);
                    if (relevantChunks.Count >= maxResults)
                        break;
                }
            }
            if (relevantChunks.Count >= maxResults)
                break;
        }

        return relevantChunks;
    }

    #endregion
}