
namespace SmartRAG.Interfaces.Storage.Qdrant;


/// <summary>
/// Interface for performing searches in Qdrant vector database
/// </summary>
public interface IQdrantSearchService : IDisposable
{
    /// <summary>
    /// Performs vector search across all document collections
    /// </summary>
    /// <param name="queryEmbedding">Embedding vector for the search query</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>List of relevant document chunks</returns>
    Task<List<DocumentChunk>> SearchAsync(List<float> queryEmbedding, int maxResults, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs fallback text search when vector search fails
    /// </summary>
    /// <param name="query">Text query to search for</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>List of relevant document chunks</returns>
    Task<List<DocumentChunk>> FallbackTextSearchAsync(string query, int maxResults, CancellationToken cancellationToken = default);
}

