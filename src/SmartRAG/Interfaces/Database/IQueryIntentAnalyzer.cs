using SmartRAG.Models;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Database
{
    /// <summary>
    /// Analyzes user queries and determines which databases/tables to query
    /// </summary>
    public interface IQueryIntentAnalyzer
    {
        /// <summary>
        /// Analyzes user query and determines which databases/tables to query
        /// </summary>
        /// <param name="userQuery">Natural language user query</param>
        /// <returns>Query intent with database routing information</returns>
        Task<QueryIntent> AnalyzeQueryIntentAsync(string userQuery);
    }
}

