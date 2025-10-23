namespace SmartRAG.Models
{
    /// <summary>
    /// Configuration options for Google Speech-to-Text service
    /// </summary>
    public class GoogleSpeechConfig
    {
        /// <summary>
        /// Google Cloud Speech-to-Text API key or service account JSON path
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Default language for speech recognition (e.g., "tr-TR", "en-US")
        /// </summary>
        public string DefaultLanguage { get; set; } = "tr-TR";

        /// <summary>
        /// Minimum confidence threshold for results (0.0 - 1.0)
        /// </summary>
        public double MinConfidenceThreshold { get; set; } = 0.5;

        /// <summary>
        /// Include word-level timestamps in results
        /// </summary>
        public bool IncludeWordTimestamps { get; set; } = false;

        /// <summary>
        /// Enable automatic punctuation
        /// </summary>
        public bool EnableAutomaticPunctuation { get; set; } = true;

        /// <summary>
        /// Enable speaker diarization (speaker identification)
        /// </summary>
        public bool EnableSpeakerDiarization { get; set; } = false;

        /// <summary>
        /// Maximum number of speakers to detect
        /// </summary>
        public int MaxSpeakerCount { get; set; } = 2;
    }
}