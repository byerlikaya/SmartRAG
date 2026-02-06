namespace SmartRAG.Models.Results;


/// <summary>
/// Represents the result of audio transcription processing
/// </summary>
public class AudioTranscriptionResult
{
    /// <summary>
    /// The transcribed text content from the audio
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The confidence score of the transcription (0.0 to 1.0)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// The detected or specified language of the audio
    /// </summary>
    public string Language { get; set; } = string.Empty;


    /// <summary>
    /// Additional metadata from the transcription process
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

