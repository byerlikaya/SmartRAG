using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System.Data;
using System.Globalization;
using System.Text.Json;

namespace SmartRAG.Repositories;

/// <summary>
/// SQLite document repository implementation
/// </summary>
public class SqliteDocumentRepository : IDocumentRepository, IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConfig _config;
    private readonly SqliteConnection _connection;

    public SqliteDocumentRepository(IOptions<SqliteConfig> config)
    {
        _config = config.Value;
        _connectionString = $"Data Source={_config.DatabasePath};Foreign Keys={_config.EnableForeignKeys};";
        _connection = new SqliteConnection(_connectionString);

        try
        {
            InitializeDatabase();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SQLite Repository Initialization Error: {ex.Message}");
            throw;
        }
    }

    private void InitializeDatabase()
    {
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            _connection.Open();
        }

        using var command = _connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Documents (
                Id TEXT PRIMARY KEY,
                FileName TEXT NOT NULL,
                Content TEXT NOT NULL,
                ContentType TEXT NOT NULL,
                FileSize BIGINT NOT NULL,
                UploadedAt TEXT NOT NULL,
                UploadedBy TEXT NOT NULL
            );
            
            CREATE TABLE IF NOT EXISTS DocumentChunks (
                Id TEXT PRIMARY KEY,
                DocumentId TEXT NOT NULL,
                Content TEXT NOT NULL,
                ChunkIndex INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                RelevanceScore REAL NOT NULL DEFAULT 0.0,
                Embedding TEXT,
                FOREIGN KEY (DocumentId) REFERENCES Documents(Id) ON DELETE CASCADE
            );
            
            CREATE INDEX IF NOT EXISTS IX_Documents_UploadedBy ON Documents(UploadedBy);
            CREATE INDEX IF NOT EXISTS IX_Documents_ContentType ON Documents(ContentType);
            CREATE INDEX IF NOT EXISTS IX_Documents_UploadedAt ON Documents(UploadedAt);
            CREATE INDEX IF NOT EXISTS IX_Chunks_DocumentId ON DocumentChunks(DocumentId);
        ";

        try
        {
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SQLite InitializeDatabase Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public Task<Document> AddAsync(Document document)
    {
        // Basic validation
        ArgumentNullException.ThrowIfNull(document);

        if (string.IsNullOrEmpty(document.FileName))
            throw new ArgumentException("FileName cannot be null or empty", nameof(document));

        if (string.IsNullOrEmpty(document.Content))
            throw new ArgumentException("Content cannot be null or empty", nameof(document));

        if (document.Chunks == null || document.Chunks.Count == 0)
            throw new ArgumentException("Document must have at least one chunk", nameof(document));

        if (_connection.State != System.Data.ConnectionState.Open)
        {
            _connection.Open();
        }

        using var transaction = _connection.BeginTransaction();

        try
        {
            using var docCommand = _connection.CreateCommand();

            docCommand.CommandText = @"
                        INSERT INTO Documents (Id, FileName, Content, ContentType, FileSize, UploadedAt, UploadedBy)
        VALUES (@Id, @FileName, @Content, @ContentType, @FileSize, @UploadedAt, @UploadedBy)
            ";

            docCommand.Parameters.AddWithValue("@Id", document.Id.ToString());
            docCommand.Parameters.AddWithValue("@FileName", document.FileName ?? string.Empty);
            docCommand.Parameters.AddWithValue("@Content", document.Content ?? string.Empty);
            docCommand.Parameters.AddWithValue("@ContentType", document.ContentType ?? string.Empty);
            docCommand.Parameters.AddWithValue("@FileSize", document.FileSize);
            docCommand.Parameters.AddWithValue("@UploadedAt", document.UploadedAt.ToString("O"));
            docCommand.Parameters.AddWithValue("@UploadedBy", document.UploadedBy ?? string.Empty);

            docCommand.ExecuteNonQuery();

            foreach (var chunk in document.Chunks)
            {
                // Validate chunk
                if (chunk == null)
                    throw new ArgumentException($"Chunk cannot be null for document {document.FileName} (ID: {document.Id})");

                if (string.IsNullOrEmpty(chunk.Content))
                    throw new ArgumentException($"Chunk content cannot be null or empty for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.ChunkIndex < 0)
                    throw new ArgumentException($"Chunk index cannot be negative for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.Id == Guid.Empty)
                    throw new ArgumentException($"Chunk ID cannot be empty for chunk in document {document.FileName} (ID: {document.Id})");

                if (chunk.DocumentId != document.Id)
                    throw new ArgumentException($"Chunk DocumentId mismatch: chunk has {chunk.DocumentId}, document has {document.Id}");

                if (chunk.CreatedAt == default)
                    throw new ArgumentException($"Chunk CreatedAt cannot be default for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.Content.Length > 1000000) // 1MB limit for chunk content
                    throw new ArgumentException($"Chunk content too large ({chunk.Content.Length} characters) for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.ChunkIndex > 10000) // Reasonable limit for chunk index
                    throw new ArgumentException($"Chunk index too large ({chunk.ChunkIndex}) for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.RelevanceScore < -1.0 || chunk.RelevanceScore > 1.0)
                    throw new ArgumentException($"Chunk RelevanceScore must be between -1.0 and 1.0 for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.Embedding != null && chunk.Embedding.Count > 10000) // Reasonable limit for embedding vector size
                    throw new ArgumentException($"Chunk embedding vector too large ({chunk.Embedding.Count} dimensions) for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.Embedding != null && chunk.Embedding.Any(f => float.IsNaN(f) || float.IsInfinity(f)))
                    throw new ArgumentException($"Chunk embedding contains invalid values (NaN or Infinity) for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.Embedding != null && chunk.Embedding.Any(f => f < -1000 || f > 1000))
                    throw new ArgumentException($"Chunk embedding contains values outside reasonable range [-1000, 1000] for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.Embedding != null && chunk.Embedding.Count != 0 && chunk.Embedding.Count != 768 && chunk.Embedding.Count != 1536)
                    throw new ArgumentException($"Chunk embedding vector size must be 0, 768, or 1536 dimensions, got {chunk.Embedding.Count} for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.Embedding != null && chunk.Embedding.Count > 0 && chunk.Embedding.All(f => f == 0))
                    throw new ArgumentException($"Chunk embedding vector contains only zeros for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.Embedding != null && chunk.Embedding.Count > 0 && chunk.Embedding.All(f => Math.Abs(f) < 0.0001))
                    throw new ArgumentException($"Chunk embedding vector contains only very small values (< 0.0001) for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.Embedding != null && chunk.Embedding.Count > 0 && chunk.Embedding.Any(f => Math.Abs(f) > 100))
                    throw new ArgumentException($"Chunk embedding vector contains values with absolute value > 100 for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.Embedding != null && chunk.Embedding.Count > 0 && chunk.Embedding.Any(f => float.IsSubnormal(f)))
                    throw new ArgumentException($"Chunk embedding vector contains subnormal values for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.Embedding != null && chunk.Embedding.Count > 0 && chunk.Embedding.Any(f => float.IsNegativeInfinity(f) || float.IsPositiveInfinity(f)))
                    throw new ArgumentException($"Chunk embedding vector contains infinity values for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.Embedding != null && chunk.Embedding.Count > 0 && chunk.Embedding.Any(f => float.IsNaN(f)))
                    throw new ArgumentException($"Chunk embedding vector contains NaN values for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.Embedding != null && chunk.Embedding.Count > 0 && chunk.Embedding.Any(f => float.IsInfinity(f)))
                    throw new ArgumentException($"Chunk embedding vector contains infinity values for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.Embedding != null && chunk.Embedding.Count > 0 && chunk.Embedding.Any(f => float.IsSubnormal(f)))
                    throw new ArgumentException($"Chunk embedding vector contains subnormal values for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                using var chunkCommand = _connection.CreateCommand();
                chunkCommand.CommandText = @"
                    INSERT INTO DocumentChunks (Id, DocumentId, Content, ChunkIndex, CreatedAt, RelevanceScore, Embedding)
                    VALUES (@Id, @DocumentId, @Content, @ChunkIndex, @CreatedAt, @RelevanceScore, @Embedding)
                ";

                chunkCommand.Parameters.AddWithValue("@Id", chunk.Id.ToString());
                chunkCommand.Parameters.AddWithValue("@DocumentId", document.Id.ToString());
                chunkCommand.Parameters.AddWithValue("@Content", chunk.Content ?? string.Empty);
                chunkCommand.Parameters.AddWithValue("@ChunkIndex", chunk.ChunkIndex);
                chunkCommand.Parameters.AddWithValue("@CreatedAt", chunk.CreatedAt.ToString("O"));
                chunkCommand.Parameters.AddWithValue("@RelevanceScore", chunk.RelevanceScore ?? 0.0);
                chunkCommand.Parameters.AddWithValue("@Embedding", (chunk.Embedding?.Count ?? 0) > 0 ? JsonSerializer.Serialize(chunk.Embedding) : DBNull.Value);

                chunkCommand.ExecuteNonQuery();
            }

            transaction.Commit();

            return Task.FromResult(document);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine($"SQLite AddAsync Error: {ex.Message}");
            Console.WriteLine($"Document ID: {document.Id}, FileName: {document.FileName}");
            Console.WriteLine($"Chunks count: {document.Chunks?.Count ?? 0}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw new InvalidOperationException($"Failed to add document '{document.FileName}': {ex.Message}", ex);
        }
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
                return null;

            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open();
            }

            using var command = _connection.CreateCommand();
            command.CommandText = @"
                SELECT d.Id, d.FileName, d.Content, d.ContentType, d.FileSize, d.UploadedAt, d.UploadedBy,
                       c.Id as ChunkId, c.Content as ChunkContent, c.ChunkIndex, c.CreatedAt, c.RelevanceScore, c.Embedding
                FROM Documents d
                LEFT JOIN DocumentChunks c ON d.Id = c.DocumentId
                WHERE d.Id = @Id
                ORDER BY c.ChunkIndex
            ";

            command.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = command.ExecuteReader();
            Document? document = null;
            var chunks = new List<DocumentChunk>();

            while (await reader.ReadAsync())
            {
                if (document == null)
                {
                    document = new Document
                    {
                        Id = Guid.Parse(reader.GetString("Id")),
                        FileName = reader.GetString("FileName"),
                        Content = reader.GetString("Content"),
                        ContentType = reader.GetString("ContentType"),
                        FileSize = reader.GetInt64("FileSize"),
                        UploadedAt = DateTime.Parse(reader.GetString("UploadedAt"), CultureInfo.InvariantCulture),
                        UploadedBy = reader.GetString("UploadedBy"),
                        Chunks = []
                    };
                }

                if (!reader.IsDBNull("ChunkId"))
                {
                    var chunk = new DocumentChunk
                    {
                        Id = Guid.Parse(reader.GetString("ChunkId")),
                        DocumentId = id,
                        Content = reader.GetString("ChunkContent"),
                        ChunkIndex = reader.GetInt32("ChunkIndex"),
                        CreatedAt = DateTime.Parse(reader.GetString("CreatedAt"), CultureInfo.InvariantCulture),
                        RelevanceScore = reader.IsDBNull("RelevanceScore") ? 0.0 : reader.GetDouble("RelevanceScore"),
                        Embedding = reader.IsDBNull("Embedding")
                        ? []
                        : JsonSerializer.Deserialize<List<float>>(reader.GetString("Embedding")) ?? new List<float>()
                    };
                    chunks.Add(chunk);
                }
            }

            if (document != null)
            {
                document.Chunks = chunks;
            }

            return document;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SQLite GetByIdAsync Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    public async Task<List<Document>> GetAllAsync()
    {
        try
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open();
            }

            var documents = new List<Document>();

            using var command = _connection.CreateCommand();
            command.CommandText = @"
                SELECT d.Id, d.FileName, d.Content, d.ContentType, d.FileSize, d.UploadedAt, d.UploadedBy,
                       c.Id as ChunkId, c.Content as ChunkContent, c.ChunkIndex, c.CreatedAt, c.RelevanceScore, c.Embedding
                FROM Documents d
                LEFT JOIN DocumentChunks c ON d.Id = c.DocumentId
                ORDER BY d.UploadedAt DESC, c.ChunkIndex
            ";

            using var reader = command.ExecuteReader();
            Document? currentDocument = null;
            var chunks = new List<DocumentChunk>();

            while (await reader.ReadAsync())
            {
                var documentId = Guid.Parse(reader.GetString("Id"));

                if (currentDocument == null || currentDocument.Id != documentId)
                {
                    if (currentDocument != null)
                    {
                        currentDocument.Chunks = chunks;
                        documents.Add(currentDocument);
                    }

                    currentDocument = new Document
                    {
                        Id = documentId,
                        FileName = reader.GetString("FileName"),
                        Content = reader.GetString("Content"),
                        ContentType = reader.GetString("ContentType"),
                        FileSize = reader.GetInt64("FileSize"),
                        UploadedAt = DateTime.Parse(reader.GetString("UploadedAt"), CultureInfo.InvariantCulture),
                        UploadedBy = reader.GetString("UploadedBy"),
                        Chunks = []
                    };
                    chunks = new List<DocumentChunk>();
                }

                if (!reader.IsDBNull("ChunkId"))
                {
                    var chunk = new DocumentChunk
                    {
                        Id = Guid.Parse(reader.GetString("ChunkId")),
                        DocumentId = documentId,
                        Content = reader.GetString("ChunkContent"),
                        ChunkIndex = reader.GetInt32("ChunkIndex"),
                        CreatedAt = DateTime.Parse(reader.GetString("CreatedAt"), CultureInfo.InvariantCulture),
                        RelevanceScore = reader.IsDBNull("RelevanceScore") ? 0.0 : reader.GetDouble("RelevanceScore"),
                        Embedding = reader.IsDBNull("Embedding")
                        ? []
                        : JsonSerializer.Deserialize<List<float>>(reader.GetString("Embedding")) ?? []
                    };
                    chunks.Add(chunk);
                }
            }

            if (currentDocument != null)
            {
                currentDocument.Chunks = chunks;
                documents.Add(currentDocument);
            }

            return documents;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SQLite GetAllAsync Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return new List<Document>();
        }
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        if (id == Guid.Empty)
            return Task.FromResult(false);

        if (_connection.State != System.Data.ConnectionState.Open)
        {
            _connection.Open();
        }

        using var transaction = _connection.BeginTransaction();

        try
        {
            // Delete chunks first (foreign key constraint will handle this automatically)
            using var deleteChunksCommand = _connection.CreateCommand();
            deleteChunksCommand.CommandText = "DELETE FROM DocumentChunks WHERE DocumentId = @DocumentId";
            deleteChunksCommand.Parameters.AddWithValue("@DocumentId", id.ToString());
            deleteChunksCommand.ExecuteNonQuery();

            // Delete document
            using var command = _connection.CreateCommand();
            command.CommandText = "DELETE FROM Documents WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id.ToString());

            var rowsAffected = command.ExecuteNonQuery();

            transaction.Commit();
            return Task.FromResult(rowsAffected > 0);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine($"SQLite DeleteAsync Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return Task.FromResult(false);
        }
    }

    public Task<int> GetCountAsync()
    {
        try
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open();
            }

            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Documents";

            var result = command.ExecuteScalar();
            return Task.FromResult(Convert.ToInt32(result, CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SQLite GetCountAsync Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return Task.FromResult(0);
        }
    }

    public string DatabasePath => _config.DatabasePath;

    public long GetDatabaseSize()
    {
        if (File.Exists(_config.DatabasePath))
        {
            return new FileInfo(_config.DatabasePath).Length;
        }
        return 0;
    }
    public async Task<Dictionary<string, object>> GetStatisticsAsync()
    {
        try
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open();
            }

            var stats = new Dictionary<string, object>();

            using var command = _connection.CreateCommand();
            command.CommandText = @"
                SELECT 
                    (SELECT COUNT(*) FROM Documents) as DocumentCount,
                    (SELECT COUNT(*) FROM DocumentChunks) as ChunkCount,
                    (SELECT SUM(FileSize) FROM Documents) as TotalSize
            ";

            using var reader = command.ExecuteReader();
            if (await reader.ReadAsync())
            {
                stats["DocumentCount"] = reader.GetInt32("DocumentCount");
                stats["ChunkCount"] = reader.GetInt32("ChunkCount");
                stats["TotalSize"] = reader.IsDBNull("TotalSize") ? 0L : reader.GetInt64("TotalSize");
            }

            return stats;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SQLite GetStatisticsAsync Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return new Dictionary<string, object>
            {
                ["DocumentCount"] = 0,
                ["ChunkCount"] = 0,
                ["TotalSize"] = 0L,
                ["Error"] = ex.Message
            };
        }
    }

    public Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = 5)
    {
        try
        {
            // Validate input
            if (string.IsNullOrEmpty(query))
                return Task.FromResult(new List<DocumentChunk>());

            if (maxResults <= 0)
                maxResults = 5;

            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open();
            }

            var normalizedQuery = SmartRAG.Extensions.SearchTextExtensions.NormalizeForSearch(query);
            var relevantChunks = new List<DocumentChunk>();

            using var command = _connection.CreateCommand();
            // Simple LIKE search on normalized content
            command.CommandText = @"
                    SELECT c.Id, c.DocumentId, c.Content, c.ChunkIndex, c.CreatedAt, c.RelevanceScore, c.Embedding
                    FROM DocumentChunks c
                    INNER JOIN Documents d ON c.DocumentId = d.Id
                    WHERE REPLACE(REPLACE(REPLACE(LOWER(c.Content), '.', ' '), ',', ' '), ';', ' ') LIKE @Query
                    ORDER BY c.RelevanceScore DESC, c.CreatedAt DESC
                    LIMIT @MaxResults
                ";

            command.Parameters.AddWithValue("@Query", $"%{normalizedQuery}%");
            command.Parameters.AddWithValue("@MaxResults", maxResults);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var chunk = new DocumentChunk
                {
                    Id = Guid.Parse(reader.GetString("Id")),
                    DocumentId = Guid.Parse(reader.GetString("DocumentId")),
                    Content = reader.GetString("Content"),
                    ChunkIndex = reader.GetInt32("ChunkIndex"),
                    CreatedAt = DateTime.Parse(reader.GetString("CreatedAt"), CultureInfo.InvariantCulture),
                    RelevanceScore = reader.IsDBNull("RelevanceScore") ? 0.0 : reader.GetDouble("RelevanceScore"),
                    Embedding = reader.IsDBNull("Embedding") ? new List<float>() : JsonSerializer.Deserialize<List<float>>(reader.GetString("Embedding")) ?? new List<float>()
                };
                relevantChunks.Add(chunk);
            }

            return Task.FromResult(relevantChunks);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SQLite SearchAsync Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return Task.FromResult(new List<DocumentChunk>());
        }
    }

    public void Dispose()
    {
        if (_connection.State == ConnectionState.Open)
        {
            _connection.Close();
        }
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}