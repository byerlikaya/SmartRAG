using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tesseract;
using SkiaSharp;

namespace SmartRAG.Services
{
    /// <summary>
    /// Implementation of image parsing and OCR service using OCR engine
    /// </summary>
    public class ImageParserService : IImageParserService, IDisposable
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
        
        // WebP header constants
        private const int WebPHeaderSize = 12;
        private const byte RIFFByte1 = 0x52; // R
        private const byte RIFFByte2 = 0x49; // I
        private const byte RIFFByte3 = 0x46; // F
        private const byte RIFFByte4 = 0x46; // F
        private const byte WEBPByte1 = 0x57; // W
        private const byte WEBPByte2 = 0x45; // E
        private const byte WEBPByte3 = 0x42; // B
        private const byte WEBPByte4 = 0x50; // P
        
        // PNG encoding constants
        private const int PNGQuality = 100;
        
        // Tesseract path constants
        private const string TesseractPath40 = "/usr/share/tesseract-ocr/4.00/tessdata";
        private const string TesseractPathDefault = "/usr/share/tesseract-ocr/tessdata";
        private const string TesseractPathWindows = "C:\\Program Files\\Tesseract-OCR\\tessdata";

        // Character whitelist for OCR
        private const string OcrCharacterWhitelist = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.,!?;:()[]{}\"'-/\\@#$%&*+=<>|~`";

        // Table detection patterns
        private static readonly string[] TablePatterns = { "|", "  ", "\t\t", "   ", "    " };

        #endregion

        #region Fields

        private readonly ILogger<ImageParserService> _logger;
        private readonly string _ocrEngineDataPath;
        private bool _disposed = false;

        #endregion

        #region Constructor

        public ImageParserService(ILogger<ImageParserService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ocrEngineDataPath = FindOcrEngineDataPath(_logger);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the list of supported image file extensions
        /// </summary>
        public IEnumerable<string> GetSupportedImageExtensions() => SupportedExtensions;

        /// <summary>
        /// Gets the list of supported image content types
        /// </summary>
        public IEnumerable<string> GetSupportedImageContentTypes() => SupportedContentTypes;

        #endregion

        #region Public Methods

        /// <summary>
        /// Parses an image stream and extracts text using OCR
        /// </summary>
        public async Task<string> ExtractTextFromImageAsync(Stream imageStream, string language = DefaultLanguage)
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
                using (var preprocessedStream = await PreprocessImageAsync(imageStream))
                {
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
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogImageOcrFailed(_logger, ex);
                
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
                var text = await ExtractTextFromImageAsync(imageStream, language);
                
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
                ServiceLogMessages.LogTableDetectionFailed(_logger, ex);
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
                ServiceLogMessages.LogImageProcessingStarted(_logger, (int)imageStream.Length, null);

                // Convert WebP to PNG for Tesseract compatibility
                var convertedStream = await ConvertToPngIfNeededAsync(imageStream);
                
                ServiceLogMessages.LogImageProcessingCompleted(_logger, (int)imageStream.Length, (int)convertedStream.Length, null);
                return convertedStream;
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogImageProcessingFailed(_logger, ex);
                
                // Return original stream if preprocessing fails
                imageStream.Position = 0;
                return imageStream;
            }
        }

        /// <summary>
        /// Converts WebP images to PNG format for Tesseract compatibility using SkiaSharp
        /// </summary>
        private async Task<Stream> ConvertToPngIfNeededAsync(Stream imageStream)
        {
            try
            {
                imageStream.Position = 0;
                
                // Try to detect if it's a WebP image by reading the header
                var header = new byte[WebPHeaderSize];
                await imageStream.ReadAsync(header, 0, header.Length);
                imageStream.Position = 0;

                // Check for WebP signature (RIFF....WEBP)
                bool isWebP = header.Length >= WebPHeaderSize && 
                             header[0] == RIFFByte1 && header[1] == RIFFByte2 && header[2] == RIFFByte3 && header[3] == RIFFByte4 && // RIFF
                             header[8] == WEBPByte1 && header[9] == WEBPByte2 && header[10] == WEBPByte3 && header[11] == WEBPByte4; // WEBP

                if (!isWebP)
                {
                    // Not WebP, return original stream
                    return imageStream;
                }

                // Convert WebP to PNG using SkiaSharp
                using (var skImage = SKImage.FromEncodedData(imageStream))
                {
                    if (skImage == null)
                    {
                        _logger.LogWarning("Failed to decode WebP image with SkiaSharp");
                        imageStream.Position = 0;
                        return imageStream;
                    }

                    var pngData = skImage.Encode(SKEncodedImageFormat.Png, PNGQuality);
                    var pngStream = new MemoryStream(pngData.ToArray());
                    pngStream.Position = 0;
                    
                    _logger.LogInformation("Successfully converted WebP to PNG using SkiaSharp");
                    return pngStream;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert WebP image format with SkiaSharp, using original stream");
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
                    // Use current directory for tessdata if no path is provided
                    var tessdataPath = string.IsNullOrEmpty(_ocrEngineDataPath) ? "." : _ocrEngineDataPath;
                    
                    using (var engine = new TesseractEngine(tessdataPath, language, EngineMode.Default))
                    {
                        // Configure OCR settings
                        engine.SetVariable("tessedit_char_whitelist", OcrCharacterWhitelist);
                        
                        using (var img = Pix.LoadFromMemory(ReadStreamToByteArray(imageStream)))
                        using (var page = engine.Process(img))
                        {
                            var text = page.GetText();
                            var confidence = page.GetMeanConfidence();
                            
                            ServiceLogMessages.LogImageOcrSuccess(_logger, text.Length, null);
                            
                            return (text?.Trim() ?? string.Empty, confidence);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ServiceLogMessages.LogImageOcrFailed(_logger, ex);
                    return (string.Empty, 0f);
                }
            });
        }

        /// <summary>
        /// Finds the OCR engine data path
        /// </summary>
        private static string FindOcrEngineDataPath(ILogger _logger)
        {
            var possiblePaths = new[]
            {
                // Local tessdata folder (from Content)
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"),
                Path.Combine(Environment.CurrentDirectory, "tessdata"),
                Path.Combine(Path.GetTempPath(), "tessdata"),
                TesseractPath40,
                TesseractPathDefault,
                TesseractPathWindows
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    // Check if eng.traineddata exists in this path
                    var engTrainedDataPath = Path.Combine(path, "eng.traineddata");
                    if (File.Exists(engTrainedDataPath))
                    {
                        ServiceLogMessages.LogOcrDataPathFound(_logger, path, null);
                        return path;
                    }
                }
            }

            // Fallback: Try to find tessdata in any subdirectory
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var tessdataDirs = Directory.GetDirectories(baseDir, "*tessdata*", SearchOption.AllDirectories);
            
            foreach (var dir in tessdataDirs)
            {
                if (Directory.Exists(dir))
                {
                    var engTrainedDataPath = Path.Combine(dir, "eng.traineddata");
                    if (File.Exists(engTrainedDataPath))
                    {
                        ServiceLogMessages.LogOcrDataPathFound(_logger, dir, null);
                        return dir;
                    }
                }
            }

            ServiceLogMessages.LogOcrDataPathNotFound(_logger, "No tessdata with eng.traineddata found, using current directory", null);
            return ".";
        }

        /// <summary>
        /// Reads stream to byte array
        /// </summary>
        private static byte[] ReadStreamToByteArray(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
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
