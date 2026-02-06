namespace SmartRAG.Models;


/// <summary>
/// Represents the result of an OCR operation
/// </summary>
public class OcrResult
{
    /// <summary>
    /// The extracted text
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score of the OCR result
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Number of words extracted
    /// </summary>
    public int WordCount { get; set; }

    /// <summary>
    /// Language used for OCR
    /// </summary>
    public string Language { get; set; } = string.Empty;
}

