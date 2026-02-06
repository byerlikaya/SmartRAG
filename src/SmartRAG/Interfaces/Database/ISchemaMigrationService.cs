using System.Threading;
using System.Threading.Tasks;

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

    /// <summary>
    /// Migrates a specific database schema to vectorized chunks
    /// </summary>
    /// <param name="databaseId">Database identifier</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if migration was successful</returns>
    Task<bool> MigrateSchemaAsync(string databaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates schema chunks for a database (deletes old and creates new)
    /// </summary>
    /// <param name="databaseId">Database identifier</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if update was successful</returns>
    Task<bool> UpdateSchemaAsync(string databaseId, CancellationToken cancellationToken = default);
}

