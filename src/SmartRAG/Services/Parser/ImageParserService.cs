using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.Parser;
using SmartRAG.Models;
using SmartRAG.Services.Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tesseract;
using SkiaSharp;

namespace SmartRAG.Services.Parser
{
    /// <summary>
    /// Implementation of image parsing and OCR service using OCR engine
    /// </summary>
    public class ImageParserService : IImageParserService, IDisposable
    {
        #region Constants

        // Supported formats are handled dynamically by Tesseract and SkiaSharp

        // Language codes
        private const string DefaultLanguage = "eng";
        
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
        
        // Universal OCR correction patterns (works for all languages)
        private const string CurrencyMisreadPatternMain = @"(\d+)\s*%(?=\s*(?:\p{Lu}|\d|$))";
        private const string CurrencyMisreadPatternCompact = @"(\d+)%(?=\p{Lu}|\s+\p{Lu}|$)";
        
        // ISO 639-2 (3-letter) to ISO 639-1 (2-letter) language code mapping
        private static readonly Dictionary<string, string> LanguageCodeMapping = new Dictionary<string, string>
        {
            { "tur", "tr" }, { "eng", "en" }, { "deu", "de" }, { "fra", "fr" },
            { "spa", "es" }, { "ita", "it" }, { "rus", "ru" }, { "jpn", "ja" },
            { "kor", "ko" }, { "zho", "zh" }, { "ara", "ar" }, { "hin", "hi" },
            { "por", "pt" }, { "nld", "nl" }, { "pol", "pl" }, { "swe", "sv" },
            { "nor", "no" }, { "dan", "da" }, { "fin", "fi" }, { "ell", "el" },
            { "heb", "he" }, { "tha", "th" }, { "vie", "vi" }, { "ind", "id" },
            { "ces", "cs" }, { "hun", "hu" }, { "ron", "ro" }, { "ukr", "uk" }
        };
        
        // Reverse mapping: ISO 639-1 (2-letter) to ISO 639-2 (3-letter) for Tesseract
        private static readonly Dictionary<string, string> ReverseLanguageCodeMapping = new Dictionary<string, string>
        {
            { "tr", "tur" }, { "en", "eng" }, { "de", "deu" }, { "fr", "fra" },
            { "es", "spa" }, { "it", "ita" }, { "ru", "rus" }, { "ja", "jpn" },
            { "ko", "kor" }, { "zh", "zho" }, { "ar", "ara" }, { "hi", "hin" },
            { "pt", "por" }, { "nl", "nld" }, { "pl", "pol" }, { "sv", "swe" },
            { "no", "nor" }, { "da", "dan" }, { "fi", "fin" }, { "el", "ell" },
            { "he", "heb" }, { "th", "tha" }, { "vi", "vie" }, { "id", "ind" },
            { "cs", "ces" }, { "hu", "hun" }, { "ro", "ron" }, { "uk", "ukr" }
        };

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
            
            // Set DYLD_LIBRARY_PATH on macOS to help Tesseract.NET find native libraries
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var currentPath = Environment.GetEnvironmentVariable("DYLD_LIBRARY_PATH") ?? string.Empty;
                
                // Try multiple possible native library paths
                var possibleNativePaths = new[]
                {
                    Path.Combine(baseDirectory, "runtimes", "osx-arm64", "native"),
                    Path.Combine(baseDirectory, "runtimes", "osx-x64", "native"),
                    Path.Combine(baseDirectory, "runtimes", "osx", "native"),
                    baseDirectory
                };
                
                var validPaths = new List<string> { baseDirectory };
                foreach (var path in possibleNativePaths)
                {
                    if (Directory.Exists(path))
                    {
                        validPaths.Add(path);
                    }
                }
                
                var newPath = string.Join(":", validPaths);
                if (!string.IsNullOrEmpty(currentPath))
                {
                    newPath = $"{newPath}:{currentPath}";
                }
                
                Environment.SetEnvironmentVariable("DYLD_LIBRARY_PATH", newPath);
                _logger.LogDebug("Set DYLD_LIBRARY_PATH to: {Path}", newPath);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// [AI Query] Parses an image stream and extracts text using OCR
        /// </summary>
        public async Task<string> ExtractTextFromImageAsync(Stream imageStream, string language = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ImageParserService));

            if (imageStream == null)
                throw new ArgumentNullException(nameof(imageStream));

            if (!imageStream.CanRead)
                throw new ArgumentException("Stream cannot be read", nameof(imageStream));

            // Auto-detect language from system locale if not provided
            language ??= GetDefaultLanguageFromSystemLocale();

            var result = await ExtractTextWithConfidenceAsync(imageStream, language);
            return result.Text;
        }

        /// <summary>
        /// [AI Query] Extracts text from an image with confidence scores
        /// </summary>
        public async Task<OcrResult> ExtractTextWithConfidenceAsync(Stream imageStream, string language = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ImageParserService));

            if (imageStream == null)
                throw new ArgumentNullException(nameof(imageStream));

            if (!imageStream.CanRead)
                throw new ArgumentException("Stream cannot be read", nameof(imageStream));

            // Auto-detect language from system locale if not provided
            language ??= GetDefaultLanguageFromSystemLocale();

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
        /// Gets the default OCR language from system locale
        /// </summary>
        /// <returns>ISO 639-2 (3-letter) language code for Tesseract</returns>
        private string GetDefaultLanguageFromSystemLocale()
        {
            try
            {
                var currentCulture = CultureInfo.CurrentCulture;
                var twoLetterCode = currentCulture.TwoLetterISOLanguageName; // "tr", "en", "de", etc.
                
                // Convert 2-letter code to 3-letter code for Tesseract
                if (ReverseLanguageCodeMapping.TryGetValue(twoLetterCode, out var threeLetterCode))
                {
                    // Check if the language data file exists
                    if (IsLanguageDataAvailable(threeLetterCode))
                    {
                        Console.WriteLine($"[OCR Language Detection] System locale: '{currentCulture.Name}' → Language: '{threeLetterCode}' ✓");
                        return threeLetterCode;
                    }
                    else
                    {
                        Console.WriteLine($"[OCR Language Detection] System locale: '{currentCulture.Name}' → Language: '{threeLetterCode}' (not available, falling back to 'eng')");
                        return DefaultLanguage;
                    }
                }
                
                // Fallback to English if mapping not found
                Console.WriteLine($"[OCR Language Detection] System locale: '{currentCulture.Name}' → No mapping found, defaulting to 'eng'");
                return DefaultLanguage;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OCR Language Detection] Failed to detect system locale: {ex.Message}, defaulting to 'eng'");
                return DefaultLanguage;
            }
        }

        /// <summary>
        /// Checks if Tesseract language data file exists for the specified language
        /// </summary>
        /// <param name="languageCode">ISO 639-2 (3-letter) language code</param>
        /// <returns>True if language data file exists</returns>
        private bool IsLanguageDataAvailable(string languageCode)
        {
            if (string.IsNullOrEmpty(_ocrEngineDataPath))
                return false;

            var trainedDataFile = Path.Combine(_ocrEngineDataPath, $"{languageCode}.traineddata");
            return File.Exists(trainedDataFile);
        }

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
                            
                            // Apply OCR post-processing corrections
                            var correctedText = CorrectCommonOcrMistakes(text, language);
                            
                            ServiceLogMessages.LogImageOcrSuccess(_logger, correctedText.Length, null);
                            
                            return (correctedText?.Trim() ?? string.Empty, confidence);
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
        /// Corrects common OCR mistakes in the extracted text using universal patterns
        /// </summary>
        /// <param name="text">OCR extracted text</param>
        /// <param name="language">Language code for context (e.g., "tur", "en", "de")</param>
        /// <returns>Corrected text</returns>
        private static string CorrectCommonOcrMistakes(string text, string language)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Get currency symbol from system locale (NOT from OCR language)
            // This ensures Turkish system uses ₺ even if OCR is in English
            var currencySymbol = GetCurrencySymbolFromSystemLocale();
            
            if (!string.IsNullOrEmpty(currencySymbol))
            {
                // Correct % → currency symbol misreads using context-aware patterns
                text = CorrectCurrencySymbolMisreads(text, currencySymbol);
            }
            
            return text;
        }

        /// <summary>
        /// Gets the currency symbol from system locale (independent of OCR language)
        /// </summary>
        /// <returns>Currency symbol or null if not determinable</returns>
        private static string GetCurrencySymbolFromSystemLocale()
        {
            try
            {
                var currentCulture = CultureInfo.CurrentCulture;
                
                // Create specific culture for RegionInfo
                var specificCulture = CultureInfo.CreateSpecificCulture(currentCulture.Name);
                var region = new RegionInfo(specificCulture.Name);
                var symbol = region.CurrencySymbol;
                
                Console.WriteLine($"[OCR Currency Detection] System locale: '{currentCulture.Name}' → Symbol: '{symbol}'");
                
                return symbol;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OCR Currency Detection] Failed to get currency from system locale: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the currency symbol for a given language code using CultureInfo
        /// </summary>
        /// <param name="languageCode">ISO 639 language code (e.g., "tur", "en", "de")</param>
        /// <returns>Currency symbol or null if not determinable</returns>
        private static string GetCurrencySymbolForLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return null;

            try
            {                
                // Determine the culture code to use
                string cultureCode;
                
                if (languageCode.Length == 2)
                {
                    // Already 2-letter ISO 639-1 code
                    cultureCode = languageCode;
                }
                else if (languageCode.Contains("-"))
                {
                    // Already a full culture code (e.g., "en-US")
                    cultureCode = languageCode;
                }
                else if (LanguageCodeMapping.TryGetValue(languageCode.ToLowerInvariant(), out var twoLetterCode))
                {
                    // Map 3-letter ISO 639-2 to 2-letter ISO 639-1
                    cultureCode = twoLetterCode;
                }
                else
                {
                    // Try using the code as-is
                    cultureCode = languageCode;
                }
                
                // Create CultureInfo - use CreateSpecificCulture to get a specific (non-neutral) culture
                // This is required for RegionInfo which doesn't accept neutral cultures like "en" or "tr"
                var culture = CultureInfo.CreateSpecificCulture(cultureCode);
                var region = new RegionInfo(culture.Name);
                var symbol = region.CurrencySymbol;
                
                Console.WriteLine($"[OCR Currency Detection] Language: '{languageCode}' → Culture: '{culture.Name}' → Symbol: '{symbol}'");
                
                return symbol;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OCR Currency Detection] Failed for language '{languageCode}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Corrects OCR misreading of currency symbols as percentage signs
        /// Uses context-aware patterns that work across all languages
        /// </summary>
        /// <param name="text">Text to correct</param>
        /// <param name="currencySymbol">Currency symbol to use (e.g., ₺, $, €, £, ¥)</param>
        /// <returns>Corrected text</returns>
        private static string CorrectCurrencySymbolMisreads(string text, string currencySymbol)
        {
            // Pattern 1: digit + optional space + % followed by Unicode uppercase letter, digit, or end
            // Matches price lists: "25% Simit" → "25₺ Simit", "550% 650%" → "550₺ 650₺"
            // Avoids false positives: "50% indirim" (lowercase after %), "%18 KDV" (% before digit)
            // \p{Lu} = Unicode uppercase letter (works for Ç, Ö, Ş, А, Я, カ, etc.)
            text = Regex.Replace(
                text,
                CurrencyMisreadPatternMain,
                $"$1{currencySymbol}",
                RegexOptions.Multiline
            );
            
            // Pattern 2: digit + % (no space) + uppercase or end
            // Matches: "12Kisilik550%" → "12Kisilik550₺"
            text = Regex.Replace(
                text,
                CurrencyMisreadPatternCompact,
                $"$1{currencySymbol}"
            );

            return text;
        }

        // Table processing methods removed - not used in current implementation

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
