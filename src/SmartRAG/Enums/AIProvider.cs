namespace SmartRAG.Enums
{
    /// <summary>
    /// Supported AI providers for text generation and embeddings
    /// </summary>
    public enum AIProvider
    {
        /// <summary>
        /// Google's Gemini AI model
        /// </summary>
        Gemini,

        /// <summary>
        /// OpenAI's GPT models and embeddings
        /// </summary>
        OpenAI,

        /// <summary>
        /// Microsoft Azure OpenAI service
        /// </summary>
        AzureOpenAI,

        /// <summary>
        /// Anthropic's Claude models
        /// </summary>
        Anthropic,

        /// <summary>
        /// Custom AI provider implementation
        /// </summary>
        Custom
    }
}
