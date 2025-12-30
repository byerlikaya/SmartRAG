using SmartRAG.Enums;
using SmartRAG.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Database
{
    /// <summary>
    /// Service for parsing database files and live database connections
    /// </summary>
    public interface IDatabaseParserService
    {     

        /// <summary>
        /// Parses a database file (SQLite) and extracts content for RAG processing
        /// </summary>
        /// <param name="dbStream">Database file stream</param>
        /// <param name="fileName">Name of the database file</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Extracted text content from all tables</returns>
        Task<string> ParseDatabaseFileAsync(Stream dbStream, string fileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Connects to a live database and extracts content based on configuration
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="config">Database extraction configuration</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Extracted text content from specified tables</returns>
        Task<string> ParseDatabaseConnectionAsync(string connectionString, DatabaseConfig config, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a custom SQL query and returns results
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="query">SQL query to execute</param>
        /// <param name="databaseType">Type of database</param>
        /// <param name="maxRows">Maximum number of rows to return</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Query results as formatted text</returns>
        Task<string> ExecuteQueryAsync(string connectionString, string query, DatabaseType databaseType, int maxRows = 1000, CancellationToken cancellationToken = default);
    

        /// <summary>
        /// Gets list of table names from the database
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="databaseType">Type of database</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>List of table names</returns>
        Task<List<string>> GetTableNamesAsync(string connectionString, DatabaseType databaseType, CancellationToken cancellationToken = default);
    
        /// <summary>
        /// Gets supported database types
        /// </summary>
        /// <returns>List of supported database types</returns>
        IEnumerable<DatabaseType> GetSupportedDatabaseTypes();

        /// <summary>
        /// Validates database connection
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="databaseType">Type of database</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if connection is valid</returns>
        Task<bool> ValidateConnectionAsync(string connectionString, DatabaseType databaseType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets supported file extensions for database files
        /// </summary>
        /// <returns>List of supported file extensions</returns>
        IEnumerable<string> GetSupportedDatabaseFileExtensions();

    }
}
