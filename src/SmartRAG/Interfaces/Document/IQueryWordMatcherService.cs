using SmartRAG.Entities;
using System;
using System.Collections.Generic;

namespace SmartRAG.Interfaces.Document
{
    /// <summary>
    /// Service interface for query word matching operations
    /// </summary>
    public interface IQueryWordMatcherService
    {
        /// <summary>
        /// Maps query words to documents that contain them
        /// </summary>
        /// <param name="queryWords">List of query words to map</param>
        /// <param name="documents">List of documents to check</param>
        /// <param name="scoredChunks">List of scored document chunks</param>
        /// <param name="chunksToCheckPerDocument">Number of top chunks to check per document</param>
        /// <returns>Dictionary mapping query words to sets of document IDs that contain them</returns>
        Dictionary<string, HashSet<Guid>> MapQueryWordsToDocuments(
            List<string> queryWords,
            List<Entities.Document> documents,
            List<DocumentChunk> scoredChunks,
            int chunksToCheckPerDocument);

        /// <summary>
        /// Counts query word matches in content
        /// </summary>
        /// <param name="content">Content to search in</param>
        /// <param name="queryWords">List of query words to match</param>
        /// <returns>Tuple containing match count and total occurrences</returns>
        (int MatchCount, int TotalOccurrences) CountQueryWordMatches(string content, List<string> queryWords);

        /// <summary>
        /// Finds unique keywords (keywords that appear only in one document)
        /// </summary>
        /// <param name="wordDocumentMap">Map of query words to documents that contain them</param>
        /// <param name="documentId">Document ID to check</param>
        /// <returns>Number of unique keywords for this document</returns>
        int FindUniqueKeywords(Dictionary<string, HashSet<Guid>> wordDocumentMap, Guid documentId);

        /// <summary>
        /// Checks if a word exists in text with word boundaries (not as substring)
        /// </summary>
        /// <param name="text">Text to search in</param>
        /// <param name="word">Word to find</param>
        /// <returns>True if word is found as whole word</returns>
        bool IsWordInText(string text, string word);
    }
}

