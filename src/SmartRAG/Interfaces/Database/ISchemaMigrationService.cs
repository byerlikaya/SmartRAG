namespace SmartRAG.Interfaces.Database;


/// <summary>
/// Service for migrating database schemas to vectorized chunks
/// </summary>
public interface ISchemaMigrationService
{
    /// <summary>
    /// Migrates all database schemas to vectorized chunks
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Number of schemas migrated</returns>
    Task<int> MigrateAllSchemasAsync(CancellationToken cancellationToken = default);
}

