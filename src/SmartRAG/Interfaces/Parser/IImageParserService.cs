using System.IO;
using System.Threading.Tasks;
using SmartRAG.Models;

namespace SmartRAG.Interfaces.Parser
{
    /// <summary>
    /// Interface for image parsing and OCR operations
    /// </summary>
    public interface IImageParserService
    {
            /// <summary>
        /// Extracts text from an image using OCR
        /// </summary>
        /// <param name="imageStream">The image stream to process</param>
        /// <param name="language">The language code for OCR (e.g., "eng", "tur"). If null, uses system locale automatically.</param>
        /// <returns>The extracted text from the image</returns>
        Task<string> ExtractTextFromImageAsync(Stream imageStream, string language = null);

        /// <summary>
        /// Extracts text from an image with confidence scores
        /// </summary>
        /// <param name="imageStream">The image stream to process</param>
        /// <param name="language">The language code for OCR (e.g., "eng", "tur"). If null, uses system locale automatically.</param>
        /// <returns>OCR result with text and confidence scores</returns>
        Task<OcrResult> ExtractTextWithConfidenceAsync(Stream imageStream, string language = null);

        /// <summary>
        /// Preprocesses an image for better OCR results
        /// </summary>
        /// <param name="imageStream">The input image stream</param>
        /// <returns>Preprocessed image stream</returns>
        Task<Stream> PreprocessImageAsync(Stream imageStream);

        /// <summary>
        /// Corrects currency symbol misreads in text (e.g., % → ₺, $, €)
        /// This method applies the same currency correction logic used in OCR results to any text
        /// </summary>
        /// <param name="text">Text to correct</param>
        /// <param name="language">Language code for context (optional, used for logging)</param>
        /// <returns>Text with corrected currency symbols</returns>
        string CorrectCurrencySymbols(string text, string language = null);


    }
}
