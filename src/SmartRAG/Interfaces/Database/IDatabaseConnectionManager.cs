using SmartRAG.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Database;


/// <summary>
/// Manages database connections from configuration
/// </summary>
public interface IDatabaseConnectionManager
{
    /// <summary>
    /// Initializes all database connections from configuration
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configured database connections
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>List of all database connection configurations</returns>
    Task<List<DatabaseConnectionConfig>> GetAllConnectionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific connection by ID
    /// </summary>
    /// <param name="databaseId">Database identifier</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Connection configuration or null</returns>
    Task<DatabaseConnectionConfig?> GetConnectionAsync(string databaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a specific connection
    /// </summary>
    /// <param name="databaseId">Database identifier</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if connection is valid</returns>
    Task<bool> ValidateConnectionAsync(string databaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets database identifier from connection (auto-generates if Name not provided)
    /// </summary>
    /// <param name="connectionConfig">Connection configuration</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Unique database identifier</returns>
    Task<string> GetDatabaseIdAsync(DatabaseConnectionConfig connectionConfig, CancellationToken cancellationToken = default);
}


