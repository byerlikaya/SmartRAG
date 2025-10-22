using SmartRAG.Enums;
using SmartRAG.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces
{
    /// <summary>
    /// Service for parsing database files and live database connections
    /// </summary>
    public interface IDatabaseParserService
    {
        #region File-based Database Operations

        /// <summary>
        /// Parses a database file (SQLite) and extracts content for RAG processing
        /// </summary>
        /// <param name="dbStream">Database file stream</param>
        /// <param name="fileName">Name of the database file</param>
        /// <returns>Extracted text content from all tables</returns>
        Task<string> ParseDatabaseFileAsync(Stream dbStream, string fileName);

        #endregion

        #region Live Database Connection Operations

        /// <summary>
        /// Connects to a live database and extracts content based on configuration
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="config">Database extraction configuration</param>
        /// <returns>Extracted text content from specified tables</returns>
        Task<string> ParseDatabaseConnectionAsync(string connectionString, DatabaseConfig config);

        /// <summary>
        /// Extracts data from a specific table
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="tableName">Name of the table to extract</param>
        /// <param name="databaseType">Type of database</param>
        /// <param name="maxRows">Maximum number of rows to extract</param>
        /// <returns>Extracted table content as text</returns>
        Task<string> ExtractTableDataAsync(string connectionString, string tableName, DatabaseType databaseType, int maxRows = 1000);

        /// <summary>
        /// Executes a custom SQL query and returns results
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="query">SQL query to execute</param>
        /// <param name="databaseType">Type of database</param>
        /// <param name="maxRows">Maximum number of rows to return</param>
        /// <returns>Query results as formatted text</returns>
        Task<string> ExecuteQueryAsync(string connectionString, string query, DatabaseType databaseType, int maxRows = 1000);

        #endregion

        #region Schema Operations

        /// <summary>
        /// Gets list of table names from the database
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="databaseType">Type of database</param>
        /// <returns>List of table names</returns>
        Task<List<string>> GetTableNamesAsync(string connectionString, DatabaseType databaseType);

        /// <summary>
        /// Gets schema information for a specific table
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="databaseType">Type of database</param>
        /// <returns>Table schema as formatted text</returns>
        Task<string> GetTableSchemaAsync(string connectionString, string tableName, DatabaseType databaseType);

        #endregion

        #region Utility Methods

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
        /// <returns>True if connection is valid</returns>
        Task<bool> ValidateConnectionAsync(string connectionString, DatabaseType databaseType);

        /// <summary>
        /// Gets supported file extensions for database files
        /// </summary>
        /// <returns>List of supported file extensions</returns>
        IEnumerable<string> GetSupportedDatabaseFileExtensions();

        /// <summary>
        /// Clears memory cache and disposes resources for memory optimization
        /// </summary>
        void ClearMemoryCache();

        #endregion
    }
}
