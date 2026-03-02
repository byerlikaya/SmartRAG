
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
            if (schemas.Count == 0)
            {
                DatabaseLogMessages.LogNoSchemasToMigrate(_logger, null);
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
                        DatabaseLogMessages.LogSchemaChunksExistSkipping(_logger, schema.DatabaseName, schema.DatabaseId, null);
                        continue;
                    }

                    DatabaseLogMessages.LogMigratingSchema(_logger, schema.DatabaseName, schema.DatabaseId, null);

                    var document = await _schemaChunkService.ConvertSchemaToDocumentAsync(schema, cancellationToken);
                    if (document == null || document.Chunks.Count == 0)
                    {
                        DatabaseLogMessages.LogNoChunksGenerated(_logger, schema.DatabaseName, schema.DatabaseId, null);
                        continue;
                    }

                    await _documentRepository.AddAsync(document, cancellationToken);

                    migratedCount++;
                    DatabaseLogMessages.LogSuccessfullyMigratedSchema(_logger, schema.DatabaseName, schema.DatabaseId, document.Chunks.Count, null);
                }
                catch (Exception ex)
                {
                    DatabaseLogMessages.LogFailedToMigrateSchema(_logger, schema.DatabaseName, schema.DatabaseId, ex);
                }
            }

            DatabaseLogMessages.LogSchemaMigrationCompletedCount(_logger, migratedCount, schemas.Count, null);
            return migratedCount;
        }
        catch (Exception ex)
        {
            DatabaseLogMessages.LogFailedToMigrateSchemas(_logger, ex);
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
            DatabaseLogMessages.LogFailedToCheckSchemaExists(_logger, databaseId, ex);
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
        return d => d.Metadata.TryGetValue("databaseId", out var id) &&
                   id?.ToString() == databaseId &&
                   d.Metadata.TryGetValue("documentType", out var docType) &&
                   docType?.ToString() == "Schema";
    }
}

