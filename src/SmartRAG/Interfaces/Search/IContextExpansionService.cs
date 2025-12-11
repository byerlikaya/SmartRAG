using SmartRAG.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Search
{
    /// <summary>
    /// Service for expanding document chunk context by including adjacent chunks and building context strings
    /// </summary>
    public interface IContextExpansionService
    {
        /// <summary>
        /// Expands context by including adjacent chunks from the same document
        /// </summary>
        /// <param name="chunks">Initial chunks found by search</param>
        /// <param name="contextWindow">Number of adjacent chunks to include before and after each found chunk</param>
        /// <returns>Expanded list of chunks with context</returns>
        Task<List<DocumentChunk>> ExpandContextAsync(List<DocumentChunk> chunks, int contextWindow = 2);

        /// <summary>
        /// Determines appropriate context window based on query structure using language-agnostic pattern detection
        /// </summary>
        /// <param name="chunks">List of document chunks</param>
        /// <param name="query">User query</param>
        /// <returns>Context window size (number of adjacent chunks to include)</returns>
        int DetermineContextWindow(List<DocumentChunk> chunks, string query);

        /// <summary>
        /// Builds context string from chunks with size limit to prevent timeout
        /// </summary>
        /// <param name="chunks">List of document chunks to build context from</param>
        /// <param name="maxContextSize">Maximum context size in characters</param>
        /// <returns>Context string built from chunks</returns>
        string BuildLimitedContext(List<DocumentChunk> chunks, int maxContextSize);
    }
}

