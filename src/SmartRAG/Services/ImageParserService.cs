using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tesseract;

namespace SmartRAG.Services
{
    /// <summary>
    /// Implementation of image parsing and OCR service using OCR engine
    /// </summary>
    public class ImageParserService(ILogger<ImageParserService> logger) : IImageParserService, IDisposable
    {
        #region Constants

        // Supported image extensions
        private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };

        // Supported content types
        private static readonly string[] SupportedContentTypes = 
        {
            "image/jpeg", "image/jpg", "image/png", "image/gif", 
            "image/bmp", "image/tiff", "image/webp"
        };

        // Language codes
        private const string DefaultLanguage = "eng";
        private const string TurkishLanguage = "tur";
        private const string EnglishLanguage = "eng";

        // Image processing constants
        private const int MinImageSize = 100;
        private const int MaxImageSize = 4096;
        private const float DefaultDpi = 300.0f;
        private const float DefaultConfidence = 0.7f;
        private const int MinWordLength = 1;
        private const int DefaultColumnCount = 1;
        private const int MaxRetryAttempts = 3;
        private const int DefaultTimeoutMs = 30000;

        // Character whitelist for OCR
        private const string OcrCharacterWhitelist = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.,!?;:()[]{}\"'-/\\@#$%&*+=<>|~`";

        // Table detection patterns
        private static readonly string[] TablePatterns = { "|", "  ", "\t\t", "   ", "    " };

        #endregion

        #region Fields

        private readonly string _ocrEngineDataPath = FindOcrEngineDataPath(logger);
        private bool _disposed = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the list of supported image file extensions
        /// </summary>
        public IReadOnlyList<string> GetSupportedFileTypes() => SupportedExtensions;

        /// <summary>
        /// Gets the list of supported image content types
        /// </summary>
        public IReadOnlyList<string> GetSupportedContentTypes() => SupportedContentTypes;

        #endregion

        #region Public Methods

        /// <summary>
        /// Parses an image stream and extracts text using OCR
        /// </summary>
        public async Task<string> ParseImageAsync(Stream imageStream, string language = DefaultLanguage)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ImageParserService));

            if (imageStream == null)
                throw new ArgumentNullException(nameof(imageStream));

            if (!imageStream.CanRead)
                throw new ArgumentException("Stream cannot be read", nameof(imageStream));

            var result = await ExtractTextWithConfidenceAsync(imageStream, language);
            return result.Text;
        }

        /// <summary>
        /// Extracts text from an image with confidence scores
        /// </summary>
        public async Task<OcrResult> ExtractTextWithConfidenceAsync(Stream imageStream, string language = DefaultLanguage)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ImageParserService));

            if (imageStream == null)
                throw new ArgumentNullException(nameof(imageStream));

            if (!imageStream.CanRead)
                throw new ArgumentException("Stream cannot be read", nameof(imageStream));

            var startTime = DateTime.UtcNow;
            
            try
            {
                // Preprocess the image for better OCR results
                using var preprocessedStream = await PreprocessImageAsync(imageStream);
                
                // Reset stream position
                preprocessedStream.Position = 0;

                // Perform OCR
                var extractedText = await PerformOcrAsync(preprocessedStream, language);
                
                var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                return new OcrResult
                {
                    Text = extractedText.Text,
                    Confidence = extractedText.Confidence,
                    ProcessingTimeMs = (long)processingTime,
                    WordCount = CountWords(extractedText.Text),
                    Language = language
                };
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogImageOcrFailed(logger, ex);
                
                return new OcrResult
                {
                    Text = string.Empty,
                    Confidence = 0,
                    ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                    WordCount = 0,
                    Language = language
                };
            }
        }

        /// <summary>
        /// Extracts tables from an image
        /// </summary>
        public async Task<List<ExtractedTable>> ExtractTablesAsync(Stream imageStream, string language = DefaultLanguage)
        {
            try
            {
                // For now, return basic table extraction
                // Advanced table detection can be implemented later
                var text = await ParseImageAsync(imageStream, language);
                
                var tables = new List<ExtractedTable>();
                
                // Simple table detection based on text patterns
                if (ContainsTablePattern(text))
                {
                    tables.Add(new ExtractedTable
                    {
                        Content = text,
                        RowCount = CountTableRows(text),
                        ColumnCount = CountTableColumns(text),
                        Confidence = DefaultConfidence,
                        Data = ParseTableData(text)
                    });
                }
                
                return tables;
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogTableDetectionFailed(logger, ex);
                return new List<ExtractedTable>();
            }
        }

        /// <summary>
        /// Checks if the image format is supported
        /// </summary>
        public bool IsImageFormatSupported(string fileName, string contentType)
        {
            if (string.IsNullOrEmpty(fileName) && string.IsNullOrEmpty(contentType))
                return false;

            // Check file extension
            if (!string.IsNullOrEmpty(fileName))
            {
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                if (SupportedExtensions.Contains(extension))
                    return true;
            }

            // Check content type
            if (!string.IsNullOrEmpty(contentType))
            {
                return SupportedContentTypes.Any(ct => contentType.StartsWith(ct, StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }

        /// <summary>
        /// Preprocesses an image for better OCR results
        /// </summary>
        public async Task<Stream> PreprocessImageAsync(Stream imageStream)
        {
            try
            {
                var outputStream = new MemoryStream();
                
                using (var image = await Image.LoadAsync(imageStream))
                {
                    // Resize image if too small or too large
                    if (image.Width < MinImageSize || image.Height < MinImageSize)
                    {
                        var scaleFactor = Math.Max((float)MinImageSize / image.Width, (float)MinImageSize / image.Height);
                        var newWidth = (int)(image.Width * scaleFactor);
                        var newHeight = (int)(image.Height * scaleFactor);
                        
                        image.Mutate(x => x.Resize(newWidth, newHeight));
                    }
                    else if (image.Width > MaxImageSize || image.Height > MaxImageSize)
                    {
                        var scaleFactor = Math.Min((float)MaxImageSize / image.Width, (float)MaxImageSize / image.Height);
                        var newWidth = (int)(image.Width * scaleFactor);
                        var newHeight = (int)(image.Height * scaleFactor);
                        
                        image.Mutate(x => x.Resize(newWidth, newHeight));
                    }

                    // Convert to grayscale for better OCR
                    image.Mutate(x => x.Grayscale());

                    // Save processed image
                    await image.SaveAsPngAsync(outputStream);
                }

                outputStream.Position = 0;
                return outputStream;
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogImageProcessingFailed(logger, ex);
                
                // Return original stream if preprocessing fails
                imageStream.Position = 0;
                return imageStream;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Performs OCR on the image stream
        /// </summary>
        private async Task<(string Text, float Confidence)> PerformOcrAsync(Stream imageStream, string language)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrEmpty(_ocrEngineDataPath))
                    {
                        ServiceLogMessages.LogOcrDataPathNotFound(logger, "OCR engine data path", null);
                        return (string.Empty, 0f);
                    }

                    using var engine = new TesseractEngine(_ocrEngineDataPath, language, EngineMode.Default);
                    
                    // Configure OCR settings
                    engine.SetVariable("tessedit_char_whitelist", OcrCharacterWhitelist);
                    
                    using var img = Pix.LoadFromMemory(ReadStreamToByteArray(imageStream));
                    using var page = engine.Process(img);
                    
                    var text = page.GetText();
                    var confidence = page.GetMeanConfidence();
                    
                    ServiceLogMessages.LogImageOcrSuccess(logger, text.Length, null);
                    
                    return (text?.Trim() ?? string.Empty, confidence);
                }
                catch (Exception ex)
                {
                    ServiceLogMessages.LogImageOcrFailed(logger, ex);
                    return (string.Empty, 0f);
                }
            });
        }

        /// <summary>
        /// Finds the OCR engine data path
        /// </summary>
        private static string FindOcrEngineDataPath(ILogger logger)
        {
            var possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"),
                Path.Combine(Environment.CurrentDirectory, "tessdata"),
                Path.Combine(Path.GetTempPath(), "tessdata"),
                "/usr/share/tesseract-ocr/4.00/tessdata",
                "/usr/share/tesseract-ocr/tessdata",
                "C:\\Program Files\\Tesseract-OCR\\tessdata"
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    ServiceLogMessages.LogOcrDataPathFound(logger, path, null);
                    return path;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Reads stream to byte array
        /// </summary>
        private static byte[] ReadStreamToByteArray(Stream stream)
        {
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Counts words in text
        /// </summary>
        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        /// <summary>
        /// Checks if text contains table patterns
        /// </summary>
        private static bool ContainsTablePattern(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return TablePatterns.Any(pattern => text.Contains(pattern));
        }

        /// <summary>
        /// Counts table rows
        /// </summary>
        private static int CountTableRows(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        /// <summary>
        /// Counts table columns
        /// </summary>
        private static int CountTableColumns(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            var lines = text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
                return 0;

            // Find the line with most separators
            var maxColumns = 1;
            foreach (var line in lines)
            {
                var columnCount = line.Split(new char[] { '|', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                maxColumns = Math.Max(maxColumns, columnCount);
            }

            return maxColumns;
        }

        /// <summary>
        /// Parses table data into structured format
        /// </summary>
        private static List<List<string>> ParseTableData(string text)
        {
            var result = new List<List<string>>();
            
            if (string.IsNullOrWhiteSpace(text))
                return result;

            var lines = text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var columns = line.Split(new char[] { '|', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(col => col.Trim())
                    .ToList();
                
                if (columns.Count > 0)
                {
                    result.Add(columns);
                }
            }

            return result;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the service and releases resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here if any
                }

                _disposed = true;
            }
        }

        #endregion
    }
}
