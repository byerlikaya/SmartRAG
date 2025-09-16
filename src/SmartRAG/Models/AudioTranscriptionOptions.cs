using System;

namespace SmartRAG.Models
{
    /// <summary>
    /// Configuration options for audio transcription processing
    /// </summary>
    public class AudioTranscriptionOptions
    {
        #region Properties

        /// <summary>
        /// The language code for transcription (e.g., "tr-TR", "en-US")
        /// </summary>
        public string Language { get; set; } = "tr-TR";

        /// <summary>
        /// The minimum confidence threshold for segments (0.0 to 1.0)
        /// </summary>
        public double MinConfidenceThreshold { get; set; } = 0.5;

        /// <summary>
        /// Whether to include word-level timestamps
        /// </summary>
        public bool IncludeWordTimestamps { get; set; } = false;

        #endregion
    }

}
