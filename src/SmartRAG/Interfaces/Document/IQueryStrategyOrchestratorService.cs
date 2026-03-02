
namespace SmartRAG.Interfaces.Document;


/// <summary>
/// Service interface for determining query execution strategy
/// </summary>
public interface IQueryStrategyOrchestratorService
{
    /// <summary>
    /// Determines the appropriate query strategy based on confidence and available data sources
    /// </summary>
    /// <param name="confidence">Query intent confidence score (0.0 to 1.0)</param>
    /// <param name="hasDatabaseQueries">Whether database queries are available</param>
    /// <param name="hasDocumentMatches">Whether document matches are available</param>
    /// <returns>Query strategy to use</returns>
    QueryStrategy DetermineQueryStrategy(double confidence, bool hasDatabaseQueries, bool hasDocumentMatches);
}


