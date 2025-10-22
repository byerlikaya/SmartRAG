using SmartRAG.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces
{
    /// <summary>
    /// Coordinates intelligent multi-database queries using AI
    /// </summary>
    public interface IMultiDatabaseQueryCoordinator
    {
        /// <summary>
        /// Analyzes user query and determines which databases/tables to query
        /// </summary>
        /// <param name="userQuery">Natural language user query</param>
        /// <returns>Query intent with database routing information</returns>
        Task<QueryIntent> AnalyzeQueryIntentAsync(string userQuery);

        /// <summary>
        /// Executes queries across multiple databases based on query intent
        /// </summary>
        /// <param name="queryIntent">Analyzed query intent</param>
        /// <returns>Combined results from all databases</returns>
        Task<MultiDatabaseQueryResult> ExecuteMultiDatabaseQueryAsync(QueryIntent queryIntent);

        /// <summary>
        /// Executes a full intelligent query: analyze intent + execute + merge results
        /// </summary>
        /// <param name="userQuery">Natural language user query</param>
        /// <param name="maxResults">Maximum number of results</param>
        /// <returns>RAG response with data from multiple databases</returns>
        Task<RagResponse> QueryMultipleDatabasesAsync(string userQuery, int maxResults = 5);

        /// <summary>
        /// Generates optimized SQL queries for each database based on intent
        /// </summary>
        /// <param name="queryIntent">Query intent</param>
        /// <returns>Updated query intent with generated SQL</returns>
        Task<QueryIntent> GenerateDatabaseQueriesAsync(QueryIntent queryIntent);

        /// <summary>
        /// Merges results from multiple databases into a coherent response
        /// </summary>
        /// <param name="queryResults">Results from multiple databases</param>
        /// <param name="originalQuery">Original user query</param>
        /// <returns>Merged and ranked results</returns>
        Task<string> MergeResultsAsync(MultiDatabaseQueryResult queryResults, string originalQuery);
    }

    /// <summary>
    /// Results from multi-database query execution
    /// </summary>
    public class MultiDatabaseQueryResult
    {
        /// <summary>
        /// Results per database
        /// </summary>
        public Dictionary<string, DatabaseQueryResult> DatabaseResults { get; set; } = new Dictionary<string, DatabaseQueryResult>();

        /// <summary>
        /// Overall success status
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Any errors encountered
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Total execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// Results from a single database query
    /// </summary>
    public class DatabaseQueryResult
    {
        /// <summary>
        /// Database ID
        /// </summary>
        public string DatabaseId { get; set; } = string.Empty;

        /// <summary>
        /// Database name
        /// </summary>
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
        /// Query that was executed
        /// </summary>
        public string ExecutedQuery { get; set; }

        /// <summary>
        /// Query results as formatted text
        /// </summary>
        public string ResultData { get; set; } = string.Empty;

        /// <summary>
        /// Number of rows returned
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// Success status
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if any
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }
    }
}

