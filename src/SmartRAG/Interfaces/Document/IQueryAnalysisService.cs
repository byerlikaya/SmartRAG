namespace SmartRAG.Interfaces.Document;


/// <summary>
/// Service interface for analyzing queries and determining search parameters
/// </summary>
public interface IQueryAnalysisService
{
    /// <summary>
    /// Determines initial search count based on query characteristics
    /// </summary>
    /// <param name="query">User query to analyze</param>
    /// <param name="defaultMaxResults">Default maximum results count</param>
    /// <returns>Adjusted search count based on query analysis</returns>
    int DetermineInitialSearchCount(string query, int defaultMaxResults);
}


