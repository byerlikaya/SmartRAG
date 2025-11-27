using SmartRAG.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Database
{
    /// <summary>
    /// Manages database connections from configuration
    /// </summary>
    public interface IDatabaseConnectionManager
    {
        /// <summary>
        /// Initializes all database connections from configuration
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Gets all configured database connections
        /// </summary>
        /// <returns>List of all database connection configurations</returns>
        Task<List<DatabaseConnectionConfig>> GetAllConnectionsAsync();

        /// <summary>
        /// Gets a specific connection by ID
        /// </summary>
        /// <param name="databaseId">Database identifier</param>
        /// <returns>Connection configuration or null</returns>
        Task<DatabaseConnectionConfig> GetConnectionAsync(string databaseId);

        /// <summary>
        /// Validates a specific connection
        /// </summary>
        /// <param name="databaseId">Database identifier</param>
        /// <returns>True if connection is valid</returns>
        Task<bool> ValidateConnectionAsync(string databaseId);

        /// <summary>
        /// Gets database identifier from connection (auto-generates if Name not provided)
        /// </summary>
        /// <param name="connectionConfig">Connection configuration</param>
        /// <returns>Unique database identifier</returns>
        Task<string> GetDatabaseIdAsync(DatabaseConnectionConfig connectionConfig);       
    }
}

