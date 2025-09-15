using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SmartRAG.Models;

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
}
