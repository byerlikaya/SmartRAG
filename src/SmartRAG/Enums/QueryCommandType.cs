namespace SmartRAG.Enums
{
    /// <summary>
    /// Command types that can be parsed from user input
    /// </summary>
    public enum QueryCommandType
    {
        /// <summary>
        /// No command detected
        /// </summary>
        None,

        /// <summary>
        /// Start a new conversation
        /// </summary>
        NewConversation,

        /// <summary>
        /// Force conversation mode regardless of query content
        /// </summary>
        ForceConversation
    }
}

