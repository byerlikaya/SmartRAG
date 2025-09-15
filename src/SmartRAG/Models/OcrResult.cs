namespace SmartRAG.Models
{
    /// <summary>
    /// Represents the result of OCR processing
    /// </summary>
    public class OcrResult
    {
        /// <summary>
        /// Extracted text from the image
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Confidence score of the OCR result (0-1)
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Number of words in the extracted text
        /// </summary>
        public int WordCount { get; set; }

        /// <summary>
        /// Language used for OCR
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if the OCR result is successful
        /// </summary>
        public bool IsSuccessful => !string.IsNullOrWhiteSpace(Text) && Confidence > 0;

        /// <summary>
        /// Indicates if the confidence is acceptable
        /// </summary>
        public bool IsConfidenceAcceptable => Confidence >= 0.7f;
    }
}
