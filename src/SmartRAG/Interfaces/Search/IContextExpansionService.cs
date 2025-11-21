using SmartRAG.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Search
{
    /// <summary>
    /// Service for expanding document chunk context by including adjacent chunks
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
    }
}

