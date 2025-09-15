using System;
using System.Collections.Generic;

namespace SmartRAG.Models
{
    /// <summary>
    /// Represents the result of audio transcription processing
    /// </summary>
    public class AudioTranscriptionResult
    {
        #region Properties

        /// <summary>
        /// The transcribed text content from the audio
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// The confidence score of the transcription (0.0 to 1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// The duration of the audio file
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// The detected or specified language of the audio
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// List of audio segments with timestamps and individual confidence scores
        /// </summary>
        public List<AudioSegment> Segments { get; set; } = new List<AudioSegment>();

        /// <summary>
        /// Additional metadata from the transcription process
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        #endregion
    }
}
