namespace SmartRAG.Models.Results
{
    /// <summary>
    /// Result of query intent analysis including conversation classification and tokenized query terms.
    /// </summary>
    public sealed class QueryIntentAnalysisResult
    {
        /// <summary>
        /// Gets a value indicating whether the query should be treated as general conversation.
        /// </summary>
        public bool IsConversation { get; }

        /// <summary>
        /// Gets the tokenized representation of the query, normalized for search purposes.
        /// Empty when the query is treated as general conversation.
        /// </summary>
        public string[] Tokens { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryIntentAnalysisResult"/> class.
        /// </summary>
        /// <param name="isConversation">Indicates whether the query is general conversation.</param>
        /// <param name="tokens">Tokenized representation of the query.</param>
        public QueryIntentAnalysisResult(bool isConversation, string[] tokens)
        {
            IsConversation = isConversation;
            Tokens = tokens ?? System.Array.Empty<string>();
        }
    }
}


