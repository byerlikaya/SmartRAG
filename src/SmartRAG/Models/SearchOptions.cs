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
                EnableAudioSearch = options.Features.EnableAudioParsing, // Mapping parsing toggle to search capability
                EnableImageSearch = options.Features.EnableImageParsing // Mapping parsing toggle to search capability
            };
        }
    }
}
