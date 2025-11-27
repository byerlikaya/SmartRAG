using SmartRAG.Models;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Database
{
    /// <summary>
    /// Executes queries across multiple databases
    /// </summary>
    public interface IDatabaseQueryExecutor
    {
        /// <summary>
        /// Executes queries across multiple databases based on query intent
        /// </summary>
        /// <param name="queryIntent">Analyzed query intent</param>
        /// <returns>Combined results from all databases</returns>
        Task<MultiDatabaseQueryResult> ExecuteMultiDatabaseQueryAsync(QueryIntent queryIntent);
    }
}

