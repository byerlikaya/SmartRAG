
namespace SmartRAG.Interfaces.Database;


/// <summary>
/// Service for analyzing database schemas and generating intelligent metadata
/// </summary>
public interface IDatabaseSchemaAnalyzer
{
    /// <summary>
    /// Analyzes a database connection and extracts comprehensive schema information
    /// </summary>
    /// <param name="connectionConfig">Database connection configuration</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Complete schema information including tables, columns, relationships</returns>
    Task<DatabaseSchemaInfo> AnalyzeDatabaseSchemaAsync(DatabaseConnectionConfig connectionConfig, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all analyzed database schemas
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>List of all database schemas</returns>
    Task<List<DatabaseSchemaInfo>> GetAllSchemasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets schema for a specific database
    /// </summary>
    /// <param name="databaseId">Database identifier</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Database schema information</returns>
    Task<DatabaseSchemaInfo> GetSchemaAsync(string databaseId, CancellationToken cancellationToken = default);
}


