using SmartRAG.Entities;
using SmartRAG.Interfaces.Database;
using SmartRAG.Interfaces.Document;
using SmartRAG.Models;

namespace SmartRAG.Services.Database;


/// <summary>
/// Service for migrating database schemas to vectorized chunks
/// </summary>
public class SchemaMigrationService : ISchemaMigrationService
{
    private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer;
    private readonly SchemaChunkService _schemaChunkService;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<SchemaMigrationService> _logger;

    public SchemaMigrationService(
        IDatabaseSchemaAnalyzer schemaAnalyzer,
        SchemaChunkService schemaChunkService,
        IDocumentRepository documentRepository,
        ILogger<SchemaMigrationService> logger)
    {
        _schemaAnalyzer = schemaAnalyzer ?? throw new ArgumentNullException(nameof(schemaAnalyzer));
        _schemaChunkService = schemaChunkService ?? throw new ArgumentNullException(nameof(schemaChunkService));
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Migrates all database schemas to vectorized chunks
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Number of schemas migrated</returns>
    public async Task<int> MigrateAllSchemasAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var schemas = await _schemaAnalyzer.GetAllSchemasAsync(cancellationToken);
            if (schemas == null || schemas.Count == 0)
            {
                _logger.LogInformation("No schemas found to migrate");
                return 0;
            }

            var migratedCount = 0;
            foreach (var schema in schemas)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var exists = await SchemaExistsAsync(schema.DatabaseId, cancellationToken);
                    if (exists)
                    {
                        _logger.LogDebug("Schema chunks already exist for database {DatabaseName} ({DatabaseId}), skipping",
                            schema.DatabaseName, schema.DatabaseId);
                        continue;
                    }

                    _logger.LogInformation("Migrating schema for database {DatabaseName} ({DatabaseId})",
                        schema.DatabaseName, schema.DatabaseId);

                    var document = await _schemaChunkService.ConvertSchemaToDocumentAsync(schema, cancellationToken);
                    if (document == null || document.Chunks == null || document.Chunks.Count == 0)
                    {
                        _logger.LogWarning("No chunks generated for database {DatabaseName} ({DatabaseId})",
                            schema.DatabaseName, schema.DatabaseId);
                        continue;
                    }

                    var addedDocument = await _documentRepository.AddAsync(document, cancellationToken);
                    if (addedDocument != null)
                    {
                        migratedCount++;
                        _logger.LogInformation("Successfully migrated schema for database {DatabaseName} ({DatabaseId}) - {ChunkCount} chunks",
                            schema.DatabaseName, schema.DatabaseId, document.Chunks.Count);
                    }
                    else
                    {
                        _logger.LogError("Failed to store schema chunks for database {DatabaseName} ({DatabaseId})",
                            schema.DatabaseName, schema.DatabaseId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to migrate schema for database {DatabaseName} ({DatabaseId})",
                        schema.DatabaseName, schema.DatabaseId);
                }
            }

            _logger.LogInformation("Schema migration completed: {MigratedCount} out of {TotalCount} schemas migrated",
                migratedCount, schemas.Count);
            return migratedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate schemas");
            throw;
        }
    }

    /// <summary>
    /// Migrates a specific database schema to vectorized chunks
    /// </summary>
    /// <param name="databaseId">Database identifier</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if migration was successful</returns>
    public async Task<bool> MigrateSchemaAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseId))
            throw new ArgumentException("Database ID cannot be null or empty", nameof(databaseId));

        try
        {
            var schema = await _schemaAnalyzer.GetSchemaAsync(databaseId, cancellationToken);
            if (schema == null)
            {
                _logger.LogWarning("Schema not found for database {DatabaseId}", databaseId);
                return false;
            }

            _logger.LogInformation("Migrating schema for database {DatabaseName} ({DatabaseId})",
                schema.DatabaseName, schema.DatabaseId);

            var document = await _schemaChunkService.ConvertSchemaToDocumentAsync(schema, cancellationToken);
            if (document == null || document.Chunks == null || document.Chunks.Count == 0)
            {
                _logger.LogWarning("No chunks generated for database {DatabaseName} ({DatabaseId})",
                    schema.DatabaseName, schema.DatabaseId);
                return false;
            }

            var addedDocument = await _documentRepository.AddAsync(document, cancellationToken);
            if (addedDocument != null)
            {
                _logger.LogInformation("Successfully migrated schema for database {DatabaseName} ({DatabaseId}) - {ChunkCount} chunks",
                    schema.DatabaseName, schema.DatabaseId, document.Chunks.Count);
                return true;
            }
            else
            {
                _logger.LogError("Failed to store schema chunks for database {DatabaseName} ({DatabaseId})",
                    schema.DatabaseName, schema.DatabaseId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate schema for database {DatabaseId}", databaseId);
            return false;
        }
    }

    /// <summary>
    /// Updates schema chunks for a database (deletes old and creates new)
    /// </summary>
    /// <param name="databaseId">Database identifier</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if update was successful</returns>
    public async Task<bool> UpdateSchemaAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseId))
            throw new ArgumentException("Database ID cannot be null or empty", nameof(databaseId));

        try
        {
            _logger.LogInformation("Updating schema chunks for database {DatabaseId}", databaseId);

            await DeleteSchemaChunksAsync(databaseId, cancellationToken);
            return await MigrateSchemaAsync(databaseId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update schema for database {DatabaseId}", databaseId);
            return false;
        }
    }

    private async Task<bool> SchemaExistsAsync(string databaseId, CancellationToken cancellationToken)
    {
        try
        {
            var schemaDocuments = await GetSchemaDocumentsAsync(databaseId, cancellationToken);
            return schemaDocuments.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if schema exists for database {DatabaseId}", databaseId);
            return false;
        }
    }

    private async Task DeleteSchemaChunksAsync(string databaseId, CancellationToken cancellationToken)
    {
        try
        {
            var schemaDocuments = await GetSchemaDocumentsAsync(databaseId, cancellationToken);

            foreach (var doc in schemaDocuments)
            {
                await _documentRepository.DeleteAsync(doc.Id, cancellationToken);
            }

            _logger.LogInformation("Deleted {Count} schema documents for database {DatabaseId}", schemaDocuments.Count, databaseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete schema chunks for database {DatabaseId}", databaseId);
            throw;
        }
    }

    private async Task<List<Entities.Document>> GetSchemaDocumentsAsync(string databaseId, CancellationToken cancellationToken)
    {
        var allDocuments = await _documentRepository.GetAllAsync(cancellationToken);
        return allDocuments.Where(IsSchemaDocumentForDatabase(databaseId)).ToList();
    }

    private static Func<Entities.Document, bool> IsSchemaDocumentForDatabase(string databaseId)
    {
        return d => d.Metadata != null &&
                   d.Metadata.TryGetValue("databaseId", out var id) &&
                   id?.ToString() == databaseId &&
                   d.Metadata.TryGetValue("documentType", out var docType) &&
                   docType?.ToString() == "Schema";
    }
}

