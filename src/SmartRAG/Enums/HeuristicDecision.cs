namespace SmartRAG.Enums
{
    /// <summary>
    /// Heuristic decision for query classification
    /// </summary>
    public enum HeuristicDecision
    {
        /// <summary>
        /// Decision could not be determined
        /// </summary>
        Unknown,

        /// <summary>
        /// Query is classified as conversation
        /// </summary>
        Conversation,

        /// <summary>
        /// Query is classified as information request
        /// </summary>
        Information
    }
}

