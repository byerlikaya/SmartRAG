using SmartRAG.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Document;


/// <summary>
/// Service interface for building RAG responses
/// </summary>
public interface IResponseBuilderService
{
    /// <summary>
    /// Creates a RagResponse with standard configuration
    /// </summary>
    /// <param name="query">User query</param>
    /// <param name="answer">AI-generated answer</param>
    /// <param name="sources">List of search sources</param>
    /// <param name="searchMetadata">Optional metadata about search operations performed</param>
    /// <returns>Configured RagResponse</returns>
    RagResponse CreateRagResponse(string query, string answer, List<SearchSource> sources, SearchMetadata? searchMetadata = null);

    /// <summary>
    /// Determines if a RAG response contains meaningful data
    /// </summary>
    /// <param name="response">RAG response to check</param>
    /// <returns>True if response contains meaningful answer or sources</returns>
    bool HasMeaningfulData(RagResponse? response);

    /// <summary>
    /// Checks if an answer indicates missing data using language-agnostic patterns
    /// </summary>
    /// <param name="answer">Answer text to check</param>
    /// <param name="query">Optional original query to check for keyword repetition</param>
    /// <param name="sources">Optional sources used to generate the answer, to check if query terms are present</param>
    /// <returns>True if answer indicates missing data</returns>
    bool IndicatesMissingData(string answer, string? query = null, List<SearchSource>? sources = null);

    /// <summary>
    /// Merges results from database and document queries into a unified response
    /// </summary>
    /// <param name="query">Original user query</param>
    /// <param name="databaseResponse">Database query response</param>
    /// <param name="documentResponse">Document query response</param>
    /// <param name="conversationHistory">Conversation history</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Merged RAG response</returns>
    Task<RagResponse> MergeHybridResultsAsync(string query, RagResponse databaseResponse, RagResponse documentResponse, string conversationHistory, CancellationToken cancellationToken = default);
}


