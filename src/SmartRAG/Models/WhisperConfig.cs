namespace SmartRAG.Models
{
    /// <summary>
    /// Configuration for Whisper audio transcription
    /// </summary>
    public class WhisperConfig
    {
        /// <summary>
        /// Path to Whisper GGML model file (e.g., "ggml-base.bin")
        /// </summary>
        public string ModelPath { get; set; } = "models/ggml-base.bin";
        
        /// <summary>
        /// Default language for transcription (e.g., "en", "tr", "auto")
        /// </summary>
        public string DefaultLanguage { get; set; } = "auto";
        
        /// <summary>
        /// Minimum confidence threshold (0.0 - 1.0)
        /// </summary>
        public double MinConfidenceThreshold { get; set; } = 0.5;
        
        /// <summary>
        /// Enable word-level timestamps
        /// </summary>
        public bool IncludeWordTimestamps { get; set; } = false;
    }
}

