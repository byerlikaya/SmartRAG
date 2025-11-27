using SmartRAG.Models;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Database
{
    /// <summary>
    /// Generates optimized SQL queries for databases based on query intent
    /// </summary>
    public interface ISQLQueryGenerator
    {
        /// <summary>
        /// Generates optimized SQL queries for each database based on intent
        /// </summary>
        /// <param name="queryIntent">Query intent</param>
        /// <returns>Updated query intent with generated SQL</returns>
        Task<QueryIntent> GenerateDatabaseQueriesAsync(QueryIntent queryIntent);
    }
}

