using SmartRAG.Entities;
using System.Collections.Generic;

namespace SmartRAG.Interfaces.Document
{
    /// <summary>
    /// Service interface for analyzing query patterns and detecting numbered lists in document chunks
    /// </summary>
    public interface IQueryPatternAnalyzerService
    {
        /// <summary>
        /// Detects if content contains numbered lists
        /// </summary>
        /// <param name="content">Content to check</param>
        /// <returns>True if numbered lists are detected</returns>
        bool DetectNumberedLists(string content);

        /// <summary>
        /// Counts numbered list items in content
        /// </summary>
        /// <param name="content">Content to analyze</param>
        /// <returns>Number of numbered list items found</returns>
        int CountNumberedListItems(string content);

        /// <summary>
        /// Scores chunks based on numbered list presence and query word matches
        /// </summary>
        /// <param name="chunks">List of document chunks to score</param>
        /// <param name="queryWords">List of query words to match</param>
        /// <param name="numberedListBonus">Bonus score per numbered list item</param>
        /// <param name="wordMatchBonus">Bonus score per query word match</param>
        /// <returns>List of chunks with updated scores</returns>
        List<DocumentChunk> ScoreChunksByNumberedLists(
            List<DocumentChunk> chunks,
            List<string> queryWords,
            double numberedListBonus,
            double wordMatchBonus);

        /// <summary>
        /// Determines if query requires comprehensive search based on pattern analysis
        /// </summary>
        /// <param name="query">Query text to analyze</param>
        /// <returns>True if query requires comprehensive search</returns>
        bool RequiresComprehensiveSearch(string query);

        /// <summary>
        /// Checks if query contains question punctuation (language-agnostic)
        /// </summary>
        /// <param name="input">Query text to check</param>
        /// <returns>True if query contains question punctuation</returns>
        bool HasQuestionPunctuation(string input);

        /// <summary>
        /// Checks if query contains numeric patterns using Unicode digit detection
        /// </summary>
        /// <param name="input">Query text to check</param>
        /// <returns>True if query contains numeric patterns</returns>
        bool HasNumericPattern(string input);

        /// <summary>
        /// Checks if query has structural patterns indicating list/enumeration needs
        /// </summary>
        /// <param name="input">Query text to check</param>
        /// <returns>True if query indicates list/enumeration needs</returns>
        bool HasListIndicators(string input);
    }
}

