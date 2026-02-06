using SmartRAG.Models;

namespace SmartRAG.Interfaces.Database;


/// <summary>
/// Executes queries across multiple databases
/// </summary>
public interface IDatabaseQueryExecutor
{
    /// <summary>
    /// Executes queries across multiple databases based on query intent
    /// </summary>
    /// <param name="queryIntent">Analyzed query intent</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Combined results from all databases</returns>
    Task<MultiDatabaseQueryResult> ExecuteMultiDatabaseQueryAsync(QueryIntent queryIntent, CancellationToken cancellationToken = default);
}


