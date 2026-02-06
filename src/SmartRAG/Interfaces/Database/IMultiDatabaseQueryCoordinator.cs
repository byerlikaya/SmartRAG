using SmartRAG.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Database
{
    /// <summary>
    /// Coordinates intelligent multi-database queries using AI
    /// </summary>
    public interface IMultiDatabaseQueryCoordinator
    {
        /// <summary>
        /// Executes a full intelligent query: analyze intent + execute + merge results
        /// </summary>
        /// <param name="userQuery">Natural language user query</param>
        /// <param name="maxResults">Maximum number of results</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>RAG response with data from multiple databases</returns>
        Task<RagResponse> QueryMultipleDatabasesAsync(string userQuery, int maxResults = 5, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a full intelligent query using pre-analyzed query intent (avoids redundant AI calls)
        /// </summary>
        /// <param name="userQuery">Natural language user query</param>
        /// <param name="preAnalyzedIntent">Pre-analyzed query intent to avoid redundant AI calls</param>
        /// <param name="maxResults">Maximum number of results</param>
        /// <param name="preferredLanguage">Preferred language for the response (ISO code, e.g., "tr", "en")</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>RAG response with data from multiple databases</returns>
        Task<RagResponse> QueryMultipleDatabasesAsync(string userQuery, QueryIntent preAnalyzedIntent, int maxResults = 5, string preferredLanguage = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates optimized SQL queries for each database based on intent
        /// </summary>
        /// <param name="queryIntent">Query intent</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Updated query intent with generated SQL</returns>
        Task<QueryIntent> GenerateDatabaseQueriesAsync(QueryIntent queryIntent, CancellationToken cancellationToken = default);
    }
}

