namespace SmartRAG.Interfaces.Support
{
    /// <summary>
    /// Service interface for text normalization operations
    /// </summary>
    public interface ITextNormalizationService
    {
        /// <summary>
        /// Normalizes text for matching purposes (removes control characters and normalizes whitespace)
        /// </summary>
        /// <param name="value">Text to normalize</param>
        /// <returns>Normalized text</returns>
        string NormalizeForMatching(string value);

        /// <summary>
        /// Checks if content contains normalized name (handles encoding issues)
        /// </summary>
        /// <param name="content">Content to search in</param>
        /// <param name="searchName">Name to search for</param>
        /// <returns>True if name is found in content</returns>
        bool ContainsNormalizedName(string content, string searchName);

        /// <summary>
        /// Sanitizes user input for safe logging by removing control characters and limiting length
        /// </summary>
        /// <param name="input">Input to sanitize</param>
        /// <returns>Sanitized input</returns>
        string SanitizeForLog(string input);
    }
}

