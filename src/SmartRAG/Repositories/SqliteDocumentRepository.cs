using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartRAG.Repositories
{

    /// <summary>
    /// SQLite document repository implementation
    /// </summary>
    public class SqliteDocumentRepository : IDocumentRepository, IDisposable
    {
        #region Constants
        private const int MaxChunkContentLength = 1000000; // 1MB limit for chunk content
        private const int MaxChunkIndex = 10000; // Reasonable limit for chunk index
        private const int MinRelevanceScore = -1;
        private const int MaxRelevanceScore = 1;
        private const int MaxEmbeddingVectorSize = 10000; // Reasonable limit for embedding vector size
        private const int MinEmbeddingValue = -1000;
        private const int MaxEmbeddingValue = 1000;
        private const int MinSubnormalThreshold = 0;
        private const int MaxAbsoluteValue = 100;
        private const int MinValueThreshold = 0;
        private const int DefaultMaxSearchResults = 5;
        private const int OpenAIVectorDimension = 1536;
        private const int SentenceTransformersDimension = 768;
        private const string DateTimeFormat = "O";
        private const string ForeignKeysEnabled = "Foreign Keys=True";
        #endregion

        #region Fields
        private readonly string _connectionString;
        private readonly SqliteConfig _config;
        private readonly SqliteConnection _connection;
        private readonly ILogger<SqliteDocumentRepository> _logger;
        #endregion

        #region Properties
        protected ILogger Logger => _logger;
        public string DatabasePath => _config.DatabasePath;
        #endregion

        #region Constructor
        public SqliteDocumentRepository(IOptions<SqliteConfig> config, ILogger<SqliteDocumentRepository> logger)
        {
            _config = config.Value;
            _logger = logger;
            _connectionString = $"Data Source={_config.DatabasePath};{(_config.EnableForeignKeys ? ForeignKeysEnabled : "")};";
            _connection = new SqliteConnection(_connectionString);

            try
            {
                InitializeDatabase();
                RepositoryLogMessages.LogSqliteRepositoryInitialized(Logger, _config.DatabasePath, null);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogSqliteRepositoryInitFailed(Logger, _config.DatabasePath, ex);
                throw;
            }
        }
        #endregion

        #region Private Helper Methods
        private void InitializeDatabase()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open();
            }

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = GetCreateTablesSql();

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    RepositoryLogMessages.LogSqliteDatabaseInitFailed(Logger, ex);
                    throw;
                }
            }
        }

        private static string GetCreateTablesSql() => @"
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

        #region Public Methods
        public Task<SmartRAG.Entities.Document> AddAsync(SmartRAG.Entities.Document document)
        {
            try
            {
                ValidateDocument(document);
                ValidateChunks(document);

                if (_connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }

                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        InsertDocument(document);
                        InsertChunks(document);

                        transaction.Commit();
                        RepositoryLogMessages.LogSqliteDocumentAdded(Logger, document.FileName, document.Id, null);
                        return Task.FromResult(document);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        RepositoryLogMessages.LogSqliteDocumentAddFailed(Logger, document.FileName, ex);
                        throw new InvalidOperationException($"Failed to add document '{document.FileName}': {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogSqliteDocumentAddFailed(Logger, document?.FileName ?? "Unknown", ex);
                throw;
            }
        }

        private static void ValidateDocument(SmartRAG.Entities.Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (string.IsNullOrEmpty(document.FileName))
                throw new ArgumentException("FileName cannot be null or empty", nameof(document));

            if (string.IsNullOrEmpty(document.Content))
                throw new ArgumentException("Content cannot be null or empty", nameof(document));

            if (document.Chunks == null || document.Chunks.Count == 0)
                throw new ArgumentException("Document must have at least one chunk", nameof(document));
        }

        private static void ValidateChunks(SmartRAG.Entities.Document document)
        {
            foreach (var chunk in document.Chunks)
            {
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

                if (chunk.Content.Length > MaxChunkContentLength)
                    throw new ArgumentException($"Chunk content too large ({chunk.Content.Length} characters) for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.ChunkIndex > MaxChunkIndex)
                    throw new ArgumentException($"Chunk index too large ({chunk.ChunkIndex}) for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.RelevanceScore < MinRelevanceScore || chunk.RelevanceScore > MaxRelevanceScore)
                    throw new ArgumentException($"Chunk RelevanceScore must be between {MinRelevanceScore} and {MaxRelevanceScore} for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                ValidateChunkEmbedding(chunk, document);
            }
        }

        private static void ValidateChunkEmbedding(DocumentChunk chunk, SmartRAG.Entities.Document document)
        {
            if (chunk.Embedding == null) return;

            if (chunk.Embedding.Count > MaxEmbeddingVectorSize)
                throw new ArgumentException($"Chunk embedding vector too large ({chunk.Embedding.Count} dimensions) for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

            if (chunk.Embedding.Any(f => float.IsNaN(f) || float.IsInfinity(f)))
                throw new ArgumentException($"Chunk embedding contains invalid values (NaN or Infinity) for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

            if (chunk.Embedding.Any(f => f < MinEmbeddingValue || f > MaxEmbeddingValue))
                throw new ArgumentException($"Chunk embedding contains values outside reasonable range [{MinEmbeddingValue}, {MaxEmbeddingValue}] for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

            if (chunk.Embedding.Count != 0 && chunk.Embedding.Count != SentenceTransformersDimension && chunk.Embedding.Count != OpenAIVectorDimension)
                throw new ArgumentException($"Chunk embedding vector size must be 0, {SentenceTransformersDimension}, or {OpenAIVectorDimension} dimensions, got {chunk.Embedding.Count} for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

            if (chunk.Embedding.Count > 0 && chunk.Embedding.All(f => f == 0))
                throw new ArgumentException($"Chunk embedding vector contains only zeros for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

            if (chunk.Embedding.Count > 0 && chunk.Embedding.All(f => Math.Abs(f) < MinValueThreshold))
                throw new ArgumentException($"Chunk embedding vector contains only very small values (< {MinValueThreshold}) for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

            if (chunk.Embedding.Count > 0 && chunk.Embedding.Any(f => Math.Abs(f) > MaxAbsoluteValue))
                throw new ArgumentException($"Chunk embedding vector contains values with absolute value > {MaxAbsoluteValue} for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

            if (chunk.Embedding.Count > 0 && chunk.Embedding.Any(f => f != 0 && Math.Abs(f) < float.Epsilon))
                throw new ArgumentException($"Chunk embedding vector contains subnormal values for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

            if (chunk.Embedding.Count > 0 && chunk.Embedding.Any(f => float.IsNegativeInfinity(f) || float.IsPositiveInfinity(f)))
                throw new ArgumentException($"Chunk embedding vector contains infinity values for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");
        }

        private void InsertDocument(SmartRAG.Entities.Document document)
        {
            using (var docCommand = _connection.CreateCommand())
            {
                docCommand.CommandText = @"
            INSERT INTO Documents (Id, FileName, Content, ContentType, FileSize, UploadedAt, UploadedBy)
            VALUES (@Id, @FileName, @Content, @ContentType, @FileSize, @UploadedAt, @UploadedBy)
        ";

                docCommand.Parameters.AddWithValue("@Id", document.Id.ToString());
                docCommand.Parameters.AddWithValue("@FileName", document.FileName ?? string.Empty);
                docCommand.Parameters.AddWithValue("@Content", document.Content ?? string.Empty);
                docCommand.Parameters.AddWithValue("@ContentType", document.ContentType ?? string.Empty);
                docCommand.Parameters.AddWithValue("@FileSize", document.FileSize);
                docCommand.Parameters.AddWithValue("@UploadedAt", document.UploadedAt.ToString(DateTimeFormat));
                docCommand.Parameters.AddWithValue("@UploadedBy", document.UploadedBy ?? string.Empty);

                docCommand.ExecuteNonQuery();
            }
        }

        private void InsertChunks(SmartRAG.Entities.Document document)
        {
            foreach (var chunk in document.Chunks)
            {
                using (var chunkCommand = _connection.CreateCommand())
                {
                    chunkCommand.CommandText = @"
                INSERT INTO DocumentChunks (Id, DocumentId, Content, ChunkIndex, CreatedAt, RelevanceScore, Embedding)
                VALUES (@Id, @DocumentId, @Content, @ChunkIndex, @CreatedAt, @RelevanceScore, @Embedding)
            ";

                    chunkCommand.Parameters.AddWithValue("@Id", chunk.Id.ToString());
                    chunkCommand.Parameters.AddWithValue("@DocumentId", document.Id.ToString());
                    chunkCommand.Parameters.AddWithValue("@Content", chunk.Content ?? string.Empty);
                    chunkCommand.Parameters.AddWithValue("@ChunkIndex", chunk.ChunkIndex);
                    chunkCommand.Parameters.AddWithValue("@CreatedAt", chunk.CreatedAt.ToString(DateTimeFormat));
                    chunkCommand.Parameters.AddWithValue("@RelevanceScore", chunk.RelevanceScore ?? 0.0);
                    chunkCommand.Parameters.AddWithValue("@Embedding", (chunk.Embedding?.Count ?? 0) > 0 ? (object)JsonSerializer.Serialize(chunk.Embedding) : (object)DBNull.Value);

                    chunkCommand.ExecuteNonQuery();
                }
            }
        }

        private static string GetDocumentByIdSql() => @"
        SELECT d.Id, d.FileName, d.Content, d.ContentType, d.FileSize, d.UploadedAt, d.UploadedBy,
               c.Id as ChunkId, c.Content as ChunkContent, c.ChunkIndex, c.CreatedAt, c.RelevanceScore, c.Embedding
        FROM Documents d
        LEFT JOIN DocumentChunks c ON d.Id = c.DocumentId
        WHERE d.Id = @Id
        ORDER BY c.ChunkIndex
    ";

        private static SmartRAG.Entities.Document CreateDocumentFromReader(SqliteDataReader reader) => new SmartRAG.Entities.Document()
        {
            Id = Guid.Parse(GetStringSafe(reader, "Id")),
            FileName = GetStringSafe(reader, "FileName"),
            Content = GetStringSafe(reader, "Content"),
            ContentType = GetStringSafe(reader, "ContentType"),
            FileSize = GetInt64Safe(reader, "FileSize"),
            UploadedAt = DateTime.Parse(GetStringSafe(reader, "UploadedAt"), CultureInfo.InvariantCulture),
            UploadedBy = GetStringSafe(reader, "UploadedBy"),
            Chunks = new List<DocumentChunk>()
        };

        private static DocumentChunk CreateChunkFromReader(SqliteDataReader reader, Guid documentId) => new DocumentChunk()
        {
            Id = Guid.Parse(GetStringSafe(reader, "ChunkId")),
            DocumentId = documentId,
            Content = GetStringSafe(reader, "ChunkContent"),
            ChunkIndex = GetInt32Safe(reader, "ChunkIndex"),
            CreatedAt = DateTime.Parse(GetStringSafe(reader, "CreatedAt"), CultureInfo.InvariantCulture),
            RelevanceScore = IsDBNullSafe(reader, "RelevanceScore") ? 0.0 : GetDoubleSafe(reader, "RelevanceScore"),
            Embedding = IsDBNullSafe(reader, "Embedding")
                ? new List<float>()
                : JsonSerializer.Deserialize<List<float>>(GetStringSafe(reader, "Embedding")) ?? new List<float>()
        };

        private static DocumentChunk CreateChunkFromSearchReader(SqliteDataReader reader) => new DocumentChunk()
        {
            Id = Guid.Parse(GetStringSafe(reader, "Id")),
            DocumentId = Guid.Parse(GetStringSafe(reader, "DocumentId")),
            Content = GetStringSafe(reader, "Content"),
            ChunkIndex = GetInt32Safe(reader, "ChunkIndex"),
            CreatedAt = DateTime.Parse(GetStringSafe(reader, "CreatedAt"), CultureInfo.InvariantCulture),
            RelevanceScore = IsDBNullSafe(reader, "RelevanceScore") ? 0.0 : GetDoubleSafe(reader, "RelevanceScore"),
            Embedding = IsDBNullSafe(reader, "Embedding")
                ? new List<float>()
                : JsonSerializer.Deserialize<List<float>>(GetStringSafe(reader, "Embedding")) ?? new List<float>()
        };

        // Helper methods for safe reading from SQLite reader
        private static string GetStringSafe(SqliteDataReader reader, string columnName)
        {
            try
            {
                return reader.GetString(reader.GetOrdinal(columnName));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static int GetInt32Safe(SqliteDataReader reader, string columnName)
        {
            try
            {
                return reader.GetInt32(reader.GetOrdinal(columnName));
            }
            catch
            {
                return 0;
            }
        }

        private static long GetInt64Safe(SqliteDataReader reader, string columnName)
        {
            try
            {
                return reader.GetInt64(reader.GetOrdinal(columnName));
            }
            catch
            {
                return 0L;
            }
        }

        private static double GetDoubleSafe(SqliteDataReader reader, string columnName)
        {
            try
            {
                return reader.GetDouble(reader.GetOrdinal(columnName));
            }
            catch
            {
                return 0.0;
            }
        }

        private static bool IsDBNullSafe(SqliteDataReader reader, string columnName)
        {
            try
            {
                return reader.IsDBNull(reader.GetOrdinal(columnName));
            }
            catch
            {
                return true;
            }
        }

        private static string GetDocumentsWithChunksSql() => @"
        SELECT d.Id, d.FileName, d.Content, d.ContentType, d.FileSize, d.UploadedAt, d.UploadedBy,
               c.Id as ChunkId, c.Content as ChunkContent, c.ChunkIndex, c.CreatedAt, c.RelevanceScore, c.Embedding
        FROM Documents d
        LEFT JOIN DocumentChunks c ON d.Id = c.DocumentId
        ORDER BY d.UploadedAt DESC, c.ChunkIndex
    ";

        private void DeleteChunks(Guid documentId)
        {
            using (var deleteChunksCommand = _connection.CreateCommand())
            {
                deleteChunksCommand.CommandText = "DELETE FROM DocumentChunks WHERE DocumentId = @DocumentId";
                deleteChunksCommand.Parameters.AddWithValue("@DocumentId", documentId.ToString());
                deleteChunksCommand.ExecuteNonQuery();
            }
        }

        private int DeleteDocument(Guid id)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM Documents WHERE Id = @Id";
                command.Parameters.AddWithValue("@Id", id.ToString());
                return command.ExecuteNonQuery();
            }
        }

        private static string GetSearchSql() => @"
        SELECT c.Id, c.DocumentId, c.Content, c.ChunkIndex, c.CreatedAt, c.RelevanceScore, c.Embedding
        FROM DocumentChunks c
        INNER JOIN Documents d ON c.DocumentId = d.Id
        WHERE REPLACE(REPLACE(REPLACE(LOWER(c.Content), '.', ' '), ',', ' '), ';', ' ') LIKE @Query
        ORDER BY c.RelevanceScore DESC, c.CreatedAt DESC
        LIMIT @MaxResults
    ";

        public async Task<SmartRAG.Entities.Document> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    RepositoryLogMessages.LogSqliteDocumentNotFound(Logger, id, null);
                    return null;
                }

                if (_connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = GetDocumentByIdSql();

                    command.Parameters.AddWithValue("@Id", id.ToString());

                    using (var reader = command.ExecuteReader())
                    {
                SmartRAG.Entities.Document document = null;
                var chunks = new List<DocumentChunk>();

                while (await reader.ReadAsync())
                {
                    if (document == null)
                    {
                        document = CreateDocumentFromReader(reader);
                    }

                    if (!IsDBNullSafe(reader, "ChunkId"))
                    {
                        var chunk = CreateChunkFromReader(reader, id);
                        chunks.Add(chunk);
                    }
                }

                if (document != null)
                {
                    document.Chunks = chunks;
                    RepositoryLogMessages.LogSqliteDocumentRetrieved(Logger, id, null);
                }

                return document;
                    }
                }
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogSqliteDocumentRetrievalFailed(Logger, id, ex);
                return null;
            }
        }

        public async Task<List<SmartRAG.Entities.Document>> GetAllAsync()
        {
            try
            {
                if (_connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }

                var documents = new List<SmartRAG.Entities.Document>();

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = GetDocumentsWithChunksSql();

                    using (var reader = command.ExecuteReader())
                    {
                SmartRAG.Entities.Document currentDocument = null;
                var chunks = new List<DocumentChunk>();

                while (await reader.ReadAsync())
                {
                    var documentId = Guid.Parse(GetStringSafe(reader, "Id"));

                    if (currentDocument == null || currentDocument.Id != documentId)
                    {
                        if (currentDocument != null)
                        {
                            currentDocument.Chunks = chunks;
                            documents.Add(currentDocument);
                        }

                        currentDocument = CreateDocumentFromReader(reader);
                        chunks = new List<DocumentChunk>();
                    }

                    if (!IsDBNullSafe(reader, "ChunkId"))
                    {
                        var chunk = CreateChunkFromReader(reader, documentId);
                        chunks.Add(chunk);
                    }
                }

                if (currentDocument != null)
                {
                    currentDocument.Chunks = chunks;
                    documents.Add(currentDocument);
                }

                RepositoryLogMessages.LogSqliteDocumentsRetrieved(Logger, documents.Count, null);
                return documents;
                    }
                }
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogSqliteDocumentsRetrievalFailed(Logger, ex);
                return new List<SmartRAG.Entities.Document>();
            }
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return Task.FromResult(false);

                if (_connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }

                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        DeleteChunks(id);
                        var rowsAffected = DeleteDocument(id);

                        transaction.Commit();

                        if (rowsAffected > 0)
                        {
                            RepositoryLogMessages.LogSqliteDocumentDeleted(Logger, id, null);
                            return Task.FromResult(true);
                        }

                        return Task.FromResult(false);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        RepositoryLogMessages.LogSqliteDocumentDeleteFailed(Logger, id, ex);
                        return Task.FromResult(false);
                    }
                }
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogSqliteDocumentDeleteFailed(Logger, id, ex);
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

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM Documents";

                    var result = command.ExecuteScalar();
                    var count = Convert.ToInt32(result, CultureInfo.InvariantCulture);

                    RepositoryLogMessages.LogSqliteDocumentCountRetrieved(Logger, count, null);
                    return Task.FromResult(count);
                }
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogSqliteDocumentCountRetrievalFailed(Logger, ex);
                return Task.FromResult(0);
            }
        }

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

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = @"
                SELECT 
                    (SELECT COUNT(*) FROM Documents) as DocumentCount,
                    (SELECT COUNT(*) FROM DocumentChunks) as ChunkCount,
                    (SELECT SUM(FileSize) FROM Documents) as TotalSize
            ";

                    using (var reader = command.ExecuteReader())
                    {
                        if (await reader.ReadAsync())
                        {
                            stats["DocumentCount"] = GetInt32Safe(reader, "DocumentCount");
                            stats["ChunkCount"] = GetInt32Safe(reader, "ChunkCount");
                            stats["TotalSize"] = IsDBNullSafe(reader, "TotalSize") ? 0L : GetInt64Safe(reader, "TotalSize");
                        }
                    }
                }

                RepositoryLogMessages.LogSqliteStatisticsRetrieved(Logger, null);
                return stats;
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogSqliteStatisticsRetrievalFailed(Logger, ex);
                return new Dictionary<string, object>
                {
                    ["DocumentCount"] = 0,
                    ["ChunkCount"] = 0,
                    ["TotalSize"] = 0L,
                    ["Error"] = ex.Message
                };
            }
        }

        public Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = DefaultMaxSearchResults)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(query))
                    return Task.FromResult(new List<DocumentChunk>());

                if (maxResults <= 0)
                    maxResults = DefaultMaxSearchResults;

                if (_connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }

                var normalizedQuery = SmartRAG.Extensions.SearchTextExtensions.NormalizeForSearch(query);
                var relevantChunks = new List<DocumentChunk>();

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = GetSearchSql();

                    command.Parameters.AddWithValue("@Query", $"%{normalizedQuery}%");
                    command.Parameters.AddWithValue("@MaxResults", maxResults);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var chunk = CreateChunkFromSearchReader(reader);
                            relevantChunks.Add(chunk);
                        }
                    }
                }

                RepositoryLogMessages.LogSqliteSearchCompleted(Logger, query, relevantChunks.Count, maxResults, null);
                return Task.FromResult(relevantChunks);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogSqliteSearchFailed(Logger, query, ex);
                return Task.FromResult(new List<DocumentChunk>());
            }
        }
        #endregion

        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            if (_connection.State == System.Data.ConnectionState.Open)
            {
                _connection.Close();
            }
            _connection.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
