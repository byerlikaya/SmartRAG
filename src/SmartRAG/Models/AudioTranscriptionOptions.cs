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
        /// Whether to enable detailed results with segments
        /// </summary>
        public bool EnableDetailedResults { get; set; } = true;

        /// <summary>
        /// The minimum confidence threshold for segments (0.0 to 1.0)
        /// </summary>
        public double MinConfidenceThreshold { get; set; } = 0.5;

        /// <summary>
        /// Whether to include word-level timestamps
        /// </summary>
        public bool IncludeWordTimestamps { get; set; } = false;

        /// <summary>
        /// Custom audio format settings
        /// </summary>
        public AudioFormatSettings FormatSettings { get; set; } = new AudioFormatSettings();

        #endregion
    }

    /// <summary>
    /// Audio format configuration settings
    /// </summary>
    public class AudioFormatSettings
    {
        #region Properties

        /// <summary>
        /// The sample rate in Hz (default: 16000)
        /// </summary>
        public int SampleRate { get; set; } = 16000;

        /// <summary>
        /// The number of audio channels (default: 1 for mono)
        /// </summary>
        public int Channels { get; set; } = 1;

        /// <summary>
        /// The audio format type
        /// </summary>
        public string Format { get; set; } = "WAV";

        #endregion
    }
}
