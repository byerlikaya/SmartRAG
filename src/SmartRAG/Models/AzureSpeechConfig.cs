namespace SmartRAG.Models
{
    /// <summary>
    /// Configuration for Azure Speech Services
    /// </summary>
    public class AzureSpeechConfig
    {
        /// <summary>
        /// Gets or sets the Azure Speech Services subscription key
        /// </summary>
        public string SubscriptionKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Azure Speech Services region
        /// </summary>
        public string Region { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the default language for speech recognition
        /// </summary>
        public string DefaultLanguage { get; set; } = "en-US";

        /// <summary>
        /// Gets or sets a value indicating whether to enable detailed results
        /// </summary>
        public bool EnableDetailedResults { get; set; } = false;

        /// <summary>
        /// Gets or sets the minimum confidence threshold for transcription
        /// </summary>
        public double MinConfidenceThreshold { get; set; } = 0.5;

        /// <summary>
        /// Gets or sets a value indicating whether to include word timestamps
        /// </summary>
        public bool IncludeWordTimestamps { get; set; } = false;
    }
}
