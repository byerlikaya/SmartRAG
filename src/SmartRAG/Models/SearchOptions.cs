#nullable enable

namespace SmartRAG.Models
{
    /// <summary>
    /// Options for configuring a specific search request
    /// </summary>
    public class SearchOptions
    {
        /// <summary>
        /// Enable searching in databases
        /// </summary>
        public bool EnableDatabaseSearch { get; set; } = true;

        /// <summary>
        /// Enable searching in documents
        /// </summary>
        public bool EnableDocumentSearch { get; set; } = true;

        /// <summary>
        /// Enable searching in audio files (transcriptions)
        /// </summary>
        public bool EnableAudioSearch { get; set; } = true;

        /// <summary>
        /// Enable searching in images (OCR)
        /// </summary>
        public bool EnableImageSearch { get; set; } = true;

        /// <summary>
        /// Preferred language for AI responses (ISO 639-1 language code, e.g., "tr", "en", "de")
        /// If not specified, AI will attempt to detect language from the query
        /// </summary>
        public string? PreferredLanguage { get; set; }

        /// <summary>
        /// Creates default search options with all features enabled
        /// </summary>
        public static SearchOptions Default => new SearchOptions();

        /// <summary>
        /// Creates search options from global configuration
        /// </summary>
        public static SearchOptions FromConfig(SmartRagOptions options)
        {
            return new SearchOptions
            {
                EnableDatabaseSearch = options.Features.EnableDatabaseSearch,
                EnableDocumentSearch = options.Features.EnableDocumentSearch,
                EnableAudioSearch = options.Features.EnableAudioParsing, 
                EnableImageSearch = options.Features.EnableImageParsing
            };
        }
    }
}
