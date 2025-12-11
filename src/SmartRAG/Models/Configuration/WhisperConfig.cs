namespace SmartRAG.Models
{
    /// <summary>
    /// Configuration for Whisper audio transcription
    /// </summary>
    public class WhisperConfig
    {
        /// <summary>
        /// Path to Whisper GGML model file (e.g., "ggml-base.bin", "ggml-medium.bin")
        /// Larger models provide better accuracy: tiny (75MB), base (142MB), small (466MB), medium (1.5GB), large-v3 (2.9GB)
        /// </summary>
        public string ModelPath { get; set; } = "models/ggml-base.bin";

        /// <summary>
        /// Default language for transcription (e.g., "en", "tr", "auto")
        /// Use "auto" for automatic language detection (multi-language support)
        /// </summary>
        public string DefaultLanguage { get; set; } = "auto";

        /// <summary>
        /// Minimum confidence threshold (0.0 - 1.0)
        /// Lower values accept more transcriptions but may include errors
        /// </summary>
        public double MinConfidenceThreshold { get; set; } = 0.3;

        /// <summary>
        /// Optional prompt/context hint for better transcription accuracy
        /// Example: "Natural conversation", "Business meeting", "Phone call"
        /// Providing context significantly improves accuracy (20-30% better)
        /// </summary>
        public string PromptHint { get; set; } = string.Empty;

        /// <summary>
        /// Maximum number of threads to use for processing
        /// 0 = auto-detect based on CPU cores (recommended for best performance)
        /// </summary>
        public int MaxThreads { get; set; } = 0;
    }
}

