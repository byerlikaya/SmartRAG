using System;
using System.Collections.Generic;

namespace SmartRAG.Models
{
    /// <summary>
    /// Represents a segment of audio with transcription and timing information
    /// </summary>
    public class AudioSegment
    {
        #region Properties

        /// <summary>
        /// The start time of this audio segment
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// The end time of this audio segment
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// The transcribed text for this segment
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// The confidence score for this segment (0.0 to 1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Additional metadata for this segment
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        #endregion
    }
}
