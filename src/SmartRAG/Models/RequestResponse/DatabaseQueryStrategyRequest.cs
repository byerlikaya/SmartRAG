#nullable enable

using System.Collections.Generic;

namespace SmartRAG.Models.RequestResponse
{
    /// <summary>
    /// Request DTO for database-only query strategy execution
    /// </summary>
    public class DatabaseQueryStrategyRequest
    {
        /// <summary>
        /// The natural language query to execute
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Maximum number of results to return
        /// </summary>
        public int MaxResults { get; set; }

        /// <summary>
        /// Conversation history for context
        /// </summary>
        public string ConversationHistory { get; set; } = string.Empty;

        /// <summary>
        /// Whether documents can be used to answer the query
        /// </summary>
        public bool CanAnswerFromDocuments { get; set; }

        /// <summary>
        /// Query intent analysis result
        /// </summary>
        public QueryIntent? QueryIntent { get; set; }

        /// <summary>
        /// Preferred language for the response
        /// </summary>
        public string? PreferredLanguage { get; set; }

        /// <summary>
        /// Search options for filtering
        /// </summary>
        public SearchOptions? Options { get; set; }

        /// <summary>
        /// Pre-tokenized query tokens
        /// </summary>
        public List<string>? QueryTokens { get; set; }
    }
}
