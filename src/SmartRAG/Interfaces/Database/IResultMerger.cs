using SmartRAG.Models;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Database;


/// <summary>
/// Merges results from multiple databases into coherent responses
/// </summary>
public interface IResultMerger
{
    /// <summary>
    /// Merges results from multiple databases into a coherent response
    /// </summary>
    /// <param name="queryResults">Results from multiple databases</param>
    /// <param name="originalQuery">Original user query</param>
    /// <returns>Merged and ranked results</returns>
    Task<string> MergeResultsAsync(MultiDatabaseQueryResult queryResults, string originalQuery);

    /// <summary>
    /// Generates final AI answer from merged database results
    /// </summary>
    /// <param name="userQuery">Original user query</param>
    /// <param name="mergedData">Merged data from databases</param>
    /// <param name="queryResults">Query results</param>
    /// <param name="preferredLanguage">Preferred language for the response (ISO code, e.g., "tr", "en")</param>
    /// <returns>RAG response with AI-generated answer</returns>
    Task<RagResponse> GenerateFinalAnswerAsync(string userQuery, string mergedData, MultiDatabaseQueryResult queryResults, string preferredLanguage = null);
}


