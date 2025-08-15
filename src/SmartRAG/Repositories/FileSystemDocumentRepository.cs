using SmartRAG.Entities;
using SmartRAG.Interfaces;
using System.Text.Json;

namespace SmartRAG.Repositories;

/// <summary>
/// File system document repository implementation
/// </summary>
public class FileSystemDocumentRepository : IDocumentRepository
{
    private readonly string _basePath;
    private readonly string _metadataFile;
    private readonly Lock _lock = new();

    public FileSystemDocumentRepository(string basePath)
    {
        _basePath = Path.GetFullPath(basePath);
        _metadataFile = Path.Combine(_basePath, "metadata.json");

        Directory.CreateDirectory(_basePath);

        if (!File.Exists(_metadataFile))
        {
            SaveMetadata([]);
        }
    }

    public Task<Document> AddAsync(Document document)
    {
        lock (_lock)
        {
            var documents = LoadMetadata();

            if (documents.Any(d => d.Id == document.Id))
            {
                throw new InvalidOperationException($"Document with ID {document.Id} already exists");
            }

            var documentPath = GetDocumentPath(document.Id);

            var documentData = new
            {
                Id = document.Id,
                FileName = document.FileName,
                ContentType = document.ContentType,
                FileSize = document.FileSize,
                UploadedAt = document.UploadedAt,
                UploadedBy = document.UploadedBy,
                Chunks = document.Chunks
            };

            var json = JsonSerializer.Serialize(documentData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            File.WriteAllText(documentPath, json);

            documents.Add(document);

            SaveMetadata(documents);

            return Task.FromResult(document);
        }
    }

    public Task<Document?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            var documents = LoadMetadata();
            var document = documents.FirstOrDefault(d => d.Id == id);
            return Task.FromResult(document);
        }
    }

    public Task<List<Document>> GetAllAsync()
    {
        lock (_lock)
        {
            var documents = LoadMetadata();
            return Task.FromResult(documents.ToList());
        }
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        lock (_lock)
        {
            var documents = LoadMetadata();
            var document = documents.FirstOrDefault(d => d.Id == id);

            if (document == null)
                return Task.FromResult(false);

            var documentPath = GetDocumentPath(id);

            if (File.Exists(documentPath))
            {
                File.Delete(documentPath);
            }

            documents.Remove(document);
            SaveMetadata(documents);

            return Task.FromResult(true);
        }
    }

    public Task<int> GetCountAsync()
    {
        lock (_lock)
        {
            var documents = LoadMetadata();
            return Task.FromResult(documents.Count);
        }
    }

    private List<Document> LoadMetadata()
    {
        try
        {
            if (!File.Exists(_metadataFile))
                return [];

            var json = File.ReadAllText(_metadataFile);
            var documents = JsonSerializer.Deserialize<List<Document>>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return documents ?? [];
        }
        catch (Exception)
        {
            // If metadata file is corrupted, return empty list
            return [];
        }
    }

    private void SaveMetadata(List<Document> documents)
    {
        var json = JsonSerializer.Serialize(documents, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        File.WriteAllText(_metadataFile, json);
    }

    private string GetDocumentPath(Guid id)
    {
        return Path.Combine(_basePath, $"{id}.json");
    }

    public string StoragePath => _basePath;

    public long GetTotalSize()
    {
        lock (_lock)
        {
            var documents = LoadMetadata();
            return documents.Sum(d => d.FileSize);
        }
    }

    public Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = 5)
    {
        lock (_lock)
        {
            var documents = LoadMetadata();
            var normalizedQuery = SmartRAG.Extensions.SearchTextExtensions.NormalizeForSearch(query);
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

            return Task.FromResult(relevantChunks);
        }
    }
}
