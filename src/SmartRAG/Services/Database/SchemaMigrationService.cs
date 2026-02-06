
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

