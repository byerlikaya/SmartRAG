using SmartRAG.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces
{
    /// <summary>
    /// Service for analyzing database schemas and generating intelligent metadata
    /// </summary>
    public interface IDatabaseSchemaAnalyzer
    {
        /// <summary>
        /// Analyzes a database connection and extracts comprehensive schema information
        /// </summary>
        /// <param name="connectionConfig">Database connection configuration</param>
        /// <returns>Complete schema information including tables, columns, relationships</returns>
        Task<DatabaseSchemaInfo> AnalyzeDatabaseSchemaAsync(DatabaseConnectionConfig connectionConfig);

        /// <summary>
        /// Refreshes schema information for a specific database
        /// </summary>
        /// <param name="databaseId">Database identifier</param>
        /// <returns>Updated schema information</returns>
        Task<DatabaseSchemaInfo> RefreshSchemaAsync(string databaseId);

        /// <summary>
        /// Gets all analyzed database schemas
        /// </summary>
        /// <returns>List of all database schemas</returns>
        Task<List<DatabaseSchemaInfo>> GetAllSchemasAsync();

        /// <summary>
        /// Gets schema for a specific database
        /// </summary>
        /// <param name="databaseId">Database identifier</param>
        /// <returns>Database schema information</returns>
        Task<DatabaseSchemaInfo> GetSchemaAsync(string databaseId);

        /// <summary>
        /// Checks if any schemas need refresh based on configured intervals
        /// </summary>
        /// <returns>List of database IDs that need refresh</returns>
        Task<List<string>> GetSchemasNeedingRefreshAsync();

        /// <summary>
        /// Generates AI-powered summary of database content
        /// </summary>
        /// <param name="schemaInfo">Schema information</param>
        /// <returns>AI-generated summary</returns>
        Task<string> GenerateAISummaryAsync(DatabaseSchemaInfo schemaInfo);
    }
}

