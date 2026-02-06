namespace SmartRAG.Models.Results;


/// <summary>
/// Represents metadata for a single audio transcription segment.
/// </summary>
public class AudioSegmentMetadata
{
    /// <summary>
    /// Start time of the segment in seconds.
    /// </summary>
    public double Start { get; set; }

    /// <summary>
    /// End time of the segment in seconds.
    /// </summary>
    public double End { get; set; }

    /// <summary>
    /// Transcribed text for the segment.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Confidence probability for the segment.
    /// </summary>
    public double Probability { get; set; }

    /// <summary>
    /// Normalized transcription text aligned with document content.
    /// </summary>
    public string NormalizedText { get; set; } = string.Empty;

    /// <summary>
    /// Character index in the normalized document content where this segment starts.
    /// </summary>
    public int StartCharIndex { get; set; }

    /// <summary>
    /// Character index in the normalized document content where this segment ends.
    /// </summary>
    public int EndCharIndex { get; set; }
}


