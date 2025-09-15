using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces
{
    /// <summary>
    /// Interface for image parsing and OCR operations
    /// </summary>
    public interface IImageParserService
    {
        #region Properties

        /// <summary>
        /// Gets the list of supported image file extensions
        /// </summary>
        IEnumerable<string> GetSupportedImageExtensions();

        /// <summary>
        /// Gets the list of supported image content types
        /// </summary>
        IEnumerable<string> GetSupportedImageContentTypes();

        #endregion

        #region Public Methods

        /// <summary>
        /// Extracts text from an image using OCR
        /// </summary>
        /// <param name="imageStream">The image stream to process</param>
        /// <param name="language">The language code for OCR (e.g., "eng", "tur")</param>
        /// <returns>The extracted text from the image</returns>
        Task<string> ExtractTextFromImageAsync(Stream imageStream, string language = "eng");

        /// <summary>
        /// Extracts text from an image with confidence scores
        /// </summary>
        /// <param name="imageStream">The image stream to process</param>
        /// <param name="language">The language code for OCR (e.g., "eng", "tur")</param>
        /// <returns>OCR result with text and confidence scores</returns>
        Task<OcrResult> ExtractTextWithConfidenceAsync(Stream imageStream, string language = "eng");

        /// <summary>
        /// Extracts tables from an image
        /// </summary>
        /// <param name="imageStream">The image stream to process</param>
        /// <param name="language">The language code for OCR</param>
        /// <returns>List of extracted tables</returns>
        Task<List<ExtractedTable>> ExtractTablesAsync(Stream imageStream, string language = "eng");

        /// <summary>
        /// Checks if the image format is supported
        /// </summary>
        /// <param name="fileName">The image file name</param>
        /// <param name="contentType">The content type of the image</param>
        /// <returns>True if the format is supported</returns>
        bool IsImageFormatSupported(string fileName, string contentType);

        /// <summary>
        /// Preprocesses an image for better OCR results
        /// </summary>
        /// <param name="imageStream">The input image stream</param>
        /// <returns>Preprocessed image stream</returns>
        Task<Stream> PreprocessImageAsync(Stream imageStream);

        #endregion
    }

    /// <summary>
    /// Result of OCR operation with confidence scores
    /// </summary>
    public class OcrResult
    {
        /// <summary>
        /// The extracted text
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Average confidence score (0-100)
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Number of words detected
        /// </summary>
        public int WordCount { get; set; }

        /// <summary>
        /// Language used for OCR
        /// </summary>
        public string Language { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents an extracted table from an image
    /// </summary>
    public class ExtractedTable
    {
        /// <summary>
        /// Table content as structured text
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Number of rows in the table
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// Number of columns in the table
        /// </summary>
        public int ColumnCount { get; set; }

        /// <summary>
        /// Confidence score for table extraction
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Table data as structured rows and columns
        /// </summary>
        public List<List<string>> Data { get; set; } = new List<List<string>>();
    }
}
