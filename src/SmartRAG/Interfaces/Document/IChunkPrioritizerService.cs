using SmartRAG.Entities;

namespace SmartRAG.Interfaces.Document;


/// <summary>
/// Service interface for prioritizing and ordering document chunks
/// </summary>
public interface IChunkPrioritizerService
{
    /// <summary>
    /// Prioritizes chunks by query word matches
    /// </summary>
    /// <param name="chunks">List of chunks to prioritize</param>
    /// <param name="queryWords">List of query words to match</param>
    /// <param name="phraseWords">Optional words for phrase extraction including short tokens filtered by TokenizeQuery</param>
    /// <returns>Prioritized list of chunks</returns>
    List<DocumentChunk> PrioritizeChunksByQueryWords(List<DocumentChunk> chunks, List<string> queryWords, List<string>? phraseWords = null);

    /// <summary>
    /// Prioritizes chunks by relevance score
    /// </summary>
    /// <param name="chunks">List of chunks to prioritize</param>
    /// <returns>Prioritized list of chunks</returns>
    List<DocumentChunk> PrioritizeChunksByRelevanceScore(List<DocumentChunk> chunks);

    /// <summary>
    /// Merges chunks with preserved chunk 0 (document header/title chunk)
    /// </summary>
    /// <param name="chunks">List of chunks to merge</param>
    /// <param name="chunk0">Chunk with index 0 to preserve at the beginning</param>
    /// <returns>Merged list with chunk0 at the beginning if available</returns>
    List<DocumentChunk> MergeChunksWithPreservedChunk0(List<DocumentChunk> chunks, DocumentChunk? chunk0);
}


