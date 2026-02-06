using System.IO;
using SmartRAG.Models;

namespace SmartRAG.Interfaces.Parser;


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
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The extracted text from the image</returns>
    Task<string> ExtractTextFromImageAsync(Stream imageStream, string language = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Corrects currency symbol misreads in text (e.g., % → ₺, $, €)
    /// This method applies the same currency correction logic used in OCR results to any text
    /// </summary>
    /// <param name="text">Text to correct</param>
    /// <returns>Text with corrected currency symbols</returns>
    string CorrectCurrencySymbols(string text);


}

