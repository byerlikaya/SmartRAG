using SmartRAG.Interfaces.Parser;
using SmartRAG.Models;
using SmartRAG.Services.Shared;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Tesseract;
using SkiaSharp;

namespace SmartRAG.Services.Parser;


/// <summary>
/// Implementation of image parsing and OCR service using OCR engine
/// </summary>
public class ImageParserService : IImageParserService, IDisposable
{
    private const string DefaultLanguage = "eng";

    private const int WebPHeaderSize = 12;
    private const byte RIFFByte1 = 0x52; // R
    private const byte RIFFByte2 = 0x49; // I
    private const byte RIFFByte3 = 0x46; // F
    private const byte RIFFByte4 = 0x46; // F
    private const byte WEBPByte1 = 0x57; // W
    private const byte WEBPByte2 = 0x45; // E
    private const byte WEBPByte3 = 0x42; // B
    private const byte WEBPByte4 = 0x50; // P

    private const int PNGQuality = 100;

    private const string TesseractPath40 = "/usr/share/tesseract-ocr/4.00/tessdata";
    private const string TesseractPathDefault = "/usr/share/tesseract-ocr/tessdata";
    private const string TesseractPathWindows = "C:\\Program Files\\Tesseract-OCR\\tessdata";

    private const string CurrencyMisreadPatternMain = @"(\d+)\s*%(?=\s*(?:\p{Lu}|\d|$))";
    private const string CurrencyMisreadPatternCompact = @"(\d+)%(?=\p{Lu}|\s+\p{Lu}|$)";
    private const string CurrencyMisreadPattern6 = @"(\d+)\s*6(?=\s*(?:\p{Lu}|\d|$))";
    private const string CurrencyMisreadPattern6Compact = @"(\d+)6(?=\s+\p{Lu}|\s+$|$)";
    private const string CurrencyMisreadPatternT = @"(\d+)\s*t(?=\s*(?:\p{Lu}|$))";
    private const string CurrencyMisreadPatternTCompact = @"(\d+)t(?=\s+\p{Lu}|\s+$|$)";
    private const string CurrencyMisreadPatternAmpersand = @"(\d+)\s*&(?=\s*(?:\p{Lu}|\d|$))";
    private const string CurrencyMisreadPatternAmpersandCompact = @"(\d+)&(?=\p{Lu}|\s+\p{Lu}|$)";
    private const string CurrencyMisreadPatternCent = @"(\d+)\s*¢";
    private const string CurrencyMisreadPatternCentCompact = @"(\d+)¢";
    private const string CurrencyMisreadPatternPercentCent = @"(\d+)%¢";

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

    /// <summary>
    /// Maps ISO 639-1/639-2 language codes to Tesseract language codes
    /// CRITICAL: Generic mapping - no specific language names (follows Generic Code rule)
    /// Supports both ISO 639-1 (2-letter: tr, en, de) and ISO 639-2/T (3-letter: tur, eng, deu)
    /// </summary>
    private static readonly Dictionary<string, string> LanguageCodeToTesseractCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "tr", "tur" }, { "en", "eng" }, { "de", "deu" }, { "fr", "fra" },
        { "es", "spa" }, { "it", "ita" }, { "ru", "rus" }, { "ja", "jpn" },
        { "ko", "kor" }, { "zh", "zho" }, { "ar", "ara" }, { "hi", "hin" },
        { "pt", "por" }, { "nl", "nld" }, { "pl", "pol" }, { "sv", "swe" },
        { "no", "nor" }, { "da", "dan" }, { "fi", "fin" }, { "el", "ell" },
        { "he", "heb" }, { "th", "tha" }, { "vi", "vie" }, { "id", "ind" },
        { "cs", "ces" }, { "hu", "hun" }, { "ro", "ron" }, { "uk", "ukr" },
        { "tur", "tur" }, { "eng", "eng" }, { "deu", "deu" }, { "fra", "fra" },
        { "spa", "spa" }, { "ita", "ita" }, { "rus", "rus" }, { "jpn", "jpn" },
        { "kor", "kor" }, { "zho", "zho" }, { "ara", "ara" }, { "hin", "hin" },
        { "por", "por" }, { "nld", "nld" }, { "pol", "pol" }, { "swe", "swe" },
        { "nor", "nor" }, { "dan", "dan" }, { "fin", "fin" }, { "ell", "ell" },
        { "heb", "heb" }, { "tha", "tha" }, { "vie", "vie" }, { "ind", "ind" },
        { "ces", "ces" }, { "hun", "hun" }, { "ron", "ron" }, { "ukr", "ukr" }
    };

    private readonly ILogger<ImageParserService> _logger;
    private string _ocrEngineDataPath;
    private readonly object _ocrEngineDataPathLock = new object();
    private bool _disposed = false;
    private static bool _dyldLibraryPathInitialized = false;
    private static readonly object _dyldLibraryPathLock = new object();

    /// <summary>
    /// Gets the OCR engine data path (lazy initialization to avoid blocking during service registration)
    /// </summary>
    private string OcrEngineDataPath
    {
        get
        {
            if (string.IsNullOrEmpty(_ocrEngineDataPath))
            {
                lock (_ocrEngineDataPathLock)
                {
                    if (string.IsNullOrEmpty(_ocrEngineDataPath))
                    {
                        _ocrEngineDataPath = FindOcrEngineDataPath(_logger);
                    }
                }
            }
            return _ocrEngineDataPath;
        }
    }

    public ImageParserService(ILogger<ImageParserService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // DYLD_LIBRARY_PATH setup moved to lazy initialization to avoid blocking during service registration
        InitializeDyldLibraryPath();
    }

    /// <summary>
    /// Initializes DYLD_LIBRARY_PATH for macOS (thread-safe, lazy initialization)
    /// </summary>
    private static void InitializeDyldLibraryPath()
    {
        if (_dyldLibraryPathInitialized || !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        lock (_dyldLibraryPathLock)
        {
            if (_dyldLibraryPathInitialized)
                return;

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var currentPath = Environment.GetEnvironmentVariable("DYLD_LIBRARY_PATH") ?? string.Empty;

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
            _dyldLibraryPathInitialized = true;
        }
    }

    /// <summary>
    /// [AI Query] Parses an image stream and extracts text using OCR
    /// </summary>
    public async Task<string> ExtractTextFromImageAsync(Stream imageStream, string language = null, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ImageParserService));

        if (imageStream == null)
            throw new ArgumentNullException(nameof(imageStream));

        if (!imageStream.CanRead)
            throw new ArgumentException("Stream cannot be read", nameof(imageStream));

        if (string.IsNullOrWhiteSpace(language) || string.Equals(language, "auto", StringComparison.OrdinalIgnoreCase))
            language = GetDefaultLanguageFromSystemLocale();

        var result = await ExtractTextWithConfidenceAsync(imageStream, language);
        return result.Text;
    }

    private async Task<OcrResult> ExtractTextWithConfidenceAsync(Stream imageStream, string language = null)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ImageParserService));

        if (imageStream == null)
            throw new ArgumentNullException(nameof(imageStream));

        if (!imageStream.CanRead)
            throw new ArgumentException("Stream cannot be read", nameof(imageStream));

        if (string.IsNullOrWhiteSpace(language) || string.Equals(language, "auto", StringComparison.OrdinalIgnoreCase))
            language = GetDefaultLanguageFromSystemLocale();

        var startTime = DateTime.UtcNow;

        try
        {
            using var preprocessedStream = await PreprocessImageAsync(imageStream);
            preprocessedStream.Position = 0;

            var (Text, Confidence) = await PerformOcrAsync(preprocessedStream, language);

            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return new OcrResult
            {
                Text = Text,
                Confidence = Confidence,
                ProcessingTimeMs = (long)processingTime,
                WordCount = CountWords(Text),
                Language = language
            };
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


    private async Task<Stream> PreprocessImageAsync(Stream imageStream)
    {
        try
        {
            var convertedStream = await ConvertToPngIfNeededAsync(imageStream);

            return convertedStream;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogImageProcessingFailed(_logger, ex);

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

            var header = new byte[WebPHeaderSize];
            await imageStream.ReadAsync(header, 0, header.Length);
            imageStream.Position = 0;

            bool isWebP = header.Length >= WebPHeaderSize &&
                         header[0] == RIFFByte1 && header[1] == RIFFByte2 && header[2] == RIFFByte3 && header[3] == RIFFByte4 && // RIFF
                         header[8] == WEBPByte1 && header[9] == WEBPByte2 && header[10] == WEBPByte3 && header[11] == WEBPByte4; // WEBP

            if (!isWebP)
            {
                return imageStream;
            }

            using var skImage = SKImage.FromEncodedData(imageStream);
            if (skImage == null)
            {
                _logger.LogWarning("Failed to decode WebP image with SkiaSharp");
                imageStream.Position = 0;
                return imageStream;
            }

            var pngData = skImage.Encode(SKEncodedImageFormat.Png, PNGQuality);
            var pngStream = new MemoryStream(pngData.ToArray()) { Position = 0 };
            return pngStream;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert WebP image format with SkiaSharp, using original stream");
            imageStream.Position = 0;
            return imageStream;
        }
    }

    /// <summary>
    /// Gets the default OCR language from system locale.
    /// Returns locale language even when traineddata is missing so GetAvailableTesseractLanguageAsync can attempt download.
    /// </summary>
    /// <returns>ISO 639-2 (3-letter) language code for Tesseract</returns>
    private string GetDefaultLanguageFromSystemLocale()
    {
        try
        {
            var currentCulture = CultureInfo.CurrentCulture;
            var twoLetterCode = currentCulture.TwoLetterISOLanguageName;

            if (ReverseLanguageCodeMapping.TryGetValue(twoLetterCode, out var threeLetterCode))
            {
                _logger.LogInformation("[OCR Language Detection] System locale: {Code}", threeLetterCode);
                return threeLetterCode;
            }

            _logger.LogWarning("[OCR Language Detection] No mapping for locale, defaulting to 'eng'");
            return DefaultLanguage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OCR Language Detection] Failed to detect system locale, defaulting to 'eng'");
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
        if (string.IsNullOrEmpty(OcrEngineDataPath))
            return false;

        var trainedDataFile = Path.Combine(OcrEngineDataPath, $"{languageCode}.traineddata");
        return File.Exists(trainedDataFile);
    }

    /// <summary>
    /// Normalizes language parameter to Tesseract language code (ISO 639-2/T)
    /// Supports: "tr" → "tur", "en" → "eng", "tur" → "tur" (pass-through)
    /// </summary>
    private string NormalizeTesseractLanguageCode(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return DefaultLanguage;
        }

        if (LanguageCodeToTesseractCode.TryGetValue(language, out var tesseractCode))
        {
            return tesseractCode;
        }

        _logger.LogWarning("Unknown language code, falling back to default");
        return DefaultLanguage;
    }

    /// <summary>
    /// Gets available Tesseract language (downloads if missing, falls back to English if download fails)
    /// Returns null if no language data is available at all
    /// </summary>
    private async Task<string> GetAvailableTesseractLanguageAsync(string requestedLanguage)
    {
        var tessdataPath = string.IsNullOrEmpty(OcrEngineDataPath) ? "." : OcrEngineDataPath;

        if (!Directory.Exists(tessdataPath))
        {
            try
            {
                Directory.CreateDirectory(tessdataPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create tessdata directory: {Path}", tessdataPath);
            }
        }

        var requestedFile = Path.Combine(tessdataPath, $"{requestedLanguage}.traineddata");
        if (File.Exists(requestedFile))
        {
            return requestedLanguage;
        }

        _logger.LogInformation("Tesseract data not found. Attempting to download...");
        var downloaded = await TryDownloadTesseractDataAsync(requestedLanguage, tessdataPath);

        if (downloaded)
        {
            _logger.LogInformation("Successfully downloaded Tesseract data");
            return requestedLanguage;
        }

        if (requestedLanguage != DefaultLanguage)
        {
            var defaultFile = Path.Combine(tessdataPath, $"{DefaultLanguage}.traineddata");

            if (File.Exists(defaultFile))
            {
                return DefaultLanguage;
            }

            _logger.LogInformation("Attempting to download fallback language...");
            var fallbackDownloaded = await TryDownloadTesseractDataAsync(DefaultLanguage, tessdataPath);

            if (fallbackDownloaded)
            {
                _logger.LogInformation("Successfully downloaded fallback Tesseract data");
                return DefaultLanguage;
            }
        }

        _logger.LogWarning("No Tesseract language data available and download failed. OCR will be skipped.");
        return null;
    }

    /// <summary>
    /// Attempts to download Tesseract traineddata file from GitHub
    /// Generic implementation that works for any language (eng, tur, deu, fra, etc.)
    /// </summary>
    private async Task<bool> TryDownloadTesseractDataAsync(string languageCode, string tessdataPath)
    {
        try
        {
            var fileName = $"{languageCode}.traineddata";
            var targetPath = Path.Combine(tessdataPath, fileName);
            var downloadUrl = $"https://github.com/tesseract-ocr/tessdata/raw/main/{fileName}";

            _logger.LogDebug("Downloading Tesseract data from: {Url}", downloadUrl);

            using var httpClient = new System.Net.Http.HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(60);

            var response = await httpClient.GetAsync(downloadUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download Tesseract data: HTTP {StatusCode}", response.StatusCode);
                return false;
            }

            var content = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(targetPath, content);

            _logger.LogDebug("Downloaded Tesseract data: {File} ({Size} bytes)", fileName, content.Length);
            return true;
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Network error while downloading Tesseract data. OCR will use fallback.");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Download timeout for Tesseract data. OCR will use fallback.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download Tesseract data. OCR will use fallback.");
            return false;
        }
    }

    /// <summary>
    /// Performs OCR on the image stream
    /// </summary>
    private async Task<(string Text, float Confidence)> PerformOcrAsync(Stream imageStream, string language)
    {
        var tesseractLanguageCode = NormalizeTesseractLanguageCode(language);
        var availableLanguage = await GetAvailableTesseractLanguageAsync(tesseractLanguageCode);

        if (string.IsNullOrEmpty(availableLanguage))
        {
            _logger.LogWarning("No Tesseract language data available. OCR cannot be performed. Skipping OCR.");
            return (string.Empty, 0f);
        }

        if (availableLanguage != tesseractLanguageCode)
        {
            _logger.LogInformation("Tesseract data not found. Using fallback instead.");
        }
        else
        {
            _logger.LogDebug("Using Tesseract language");
        }

        return await Task.Run(() =>
        {
            try
            {
                var tessdataPath = string.IsNullOrEmpty(OcrEngineDataPath) ? "." : OcrEngineDataPath;

                using var engine = new TesseractEngine(tessdataPath, availableLanguage, EngineMode.Default);
                engine.SetVariable("tessedit_pageseg_mode", "6");
                engine.SetVariable("tessedit_create_hocr", "0");

                using var img = Pix.LoadFromMemory(ReadStreamToByteArray(imageStream));
                using var page = engine.Process(img);
                var text = ExtractTableAwareText(page);
                var confidence = page.GetMeanConfidence();

                var correctedText = CorrectCommonOcrMistakes(text, _logger);
                return (correctedText?.Trim() ?? string.Empty, confidence);
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogImageOcrFailed(_logger, ex);
                return (string.Empty, 0f);
            }
        });
    }

    /// <summary>
    /// Extracts text from OCR page with table-aware spatial positioning
    /// Groups words by rows (Y-coordinate) and columns (X-coordinate) to preserve table structure
    /// </summary>
    private static string ExtractTableAwareText(Page page)
    {
        var words = new List<(string Text, int X, int Y, int Width)>();
        
        // Extract all words with their bounding boxes
        using (var iterator = page.GetIterator())
        {
            iterator.Begin();
            do
            {
                if (iterator.TryGetBoundingBox(PageIteratorLevel.Word, out var rect))
                {
                    var wordText = iterator.GetText(PageIteratorLevel.Word);
                    if (!string.IsNullOrWhiteSpace(wordText))
                    {
                        words.Add((wordText.Trim(), rect.X1, rect.Y1, rect.Width));
                    }
                }
            } while (iterator.Next(PageIteratorLevel.Word));
        }

        if (words.Count == 0)
        {
            return page.GetText(); // Fallback to normal text extraction
        }

        // Group words by rows (Y-coordinate with tolerance for same-line detection)
        const int rowTolerance = 15; // Pixels tolerance for same row
        var rows = new List<List<(string Text, int X, int Y, int Width)>>();
        
        foreach (var word in words.OrderBy(w => w.Y))
        {
            var matchingRow = rows.FirstOrDefault(r => 
                Math.Abs(r[0].Y - word.Y) <= rowTolerance);
            
            if (matchingRow != null)
            {
                matchingRow.Add(word);
            }
            else
            {
                rows.Add(new List<(string Text, int X, int Y, int Width)> { word });
            }
        }

        // Detect if this is a multi-column layout (table/menu structure)
        var hasMultipleColumns = DetectMultiColumnLayout(rows);
        
        if (hasMultipleColumns)
        {
            return ReconstructTableText(rows);
        }
        else
        {
            // Single column - use normal text extraction
            return page.GetText();
        }
    }

    /// <summary>
    /// Detects if the layout has multiple columns (table structure)
    /// </summary>
    private static bool DetectMultiColumnLayout(List<List<(string Text, int X, int Y, int Width)>> rows)
    {
        if (rows.Count < 2) return false;

        // Check if rows have consistent column structure
        // Look for rows with multiple words spaced apart (indicating columns)
        int multiColumnRows = 0;
        
        foreach (var row in rows)
        {
            if (row.Count >= 2)
            {
                var sortedByX = row.OrderBy(w => w.X).ToList();
                var gaps = new List<int>();
                
                for (int i = 1; i < sortedByX.Count; i++)
                {
                    var gap = sortedByX[i].X - (sortedByX[i - 1].X + sortedByX[i - 1].Width);
                    gaps.Add(gap);
                }
                
                // If there's a significant gap (> 50 pixels), it's likely a multi-column row
                if (gaps.Any(g => g > 50))
                {
                    multiColumnRows++;
                }
            }
        }
        
        // If more than 30% of rows have multiple columns, treat as table
        return multiColumnRows > (rows.Count * 0.3);
    }

    /// <summary>
    /// Reconstructs text from table structure, pairing items from left and right columns
    /// Example: "Product1 Price1\nProduct2 Price2" instead of "Product1 Product2... Price1 Price2..."
    /// </summary>
    private static string ReconstructTableText(List<List<(string Text, int X, int Y, int Width)>> rows)
    {
        var result = new System.Text.StringBuilder();
        
        foreach (var row in rows)
        {
            // Sort words in this row by X-coordinate (left to right)
            var sortedWords = row.OrderBy(w => w.X).ToList();
            
            if (sortedWords.Count == 0) continue;
            
            // Detect column boundaries
            if (sortedWords.Count >= 2)
            {
                // Check if this row has a clear left-right split (2 columns)
                var midPoint = (sortedWords[0].X + sortedWords[sortedWords.Count - 1].X) / 2;
                var leftColumn = sortedWords.Where(w => w.X < midPoint).ToList();
                var rightColumn = sortedWords.Where(w => w.X >= midPoint).ToList();
                
                if (leftColumn.Any() && rightColumn.Any())
                {
                    // Table structure detected: pair left and right columns
                    result.Append(string.Join(" ", leftColumn.Select(w => w.Text)));
                    result.Append(" ");
                    result.Append(string.Join(" ", rightColumn.Select(w => w.Text)));
                    result.AppendLine();
                }
                else
                {
                    // Single column or complex layout - output as-is
                    result.AppendLine(string.Join(" ", sortedWords.Select(w => w.Text)));
                }
            }
            else
            {
                // Single word row
                result.AppendLine(sortedWords[0].Text);
            }
        }
        
        return result.ToString();
    }

    /// <summary>
    /// Finds the OCR engine data path
    /// </summary>
    private static string FindOcrEngineDataPath(ILogger _logger)
    {
        var possiblePaths = new[]
        {
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
                var engTrainedDataPath = Path.Combine(path, "eng.traineddata");
                if (File.Exists(engTrainedDataPath))
                {
                    return path;
                }
            }
        }

        // Optimized search: Only check common locations instead of recursive search
        // Recursive search with AllDirectories can be very slow on large directory trees
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var commonTessdataPaths = new[]
        {
            Path.Combine(baseDir, "tessdata"),
            Path.Combine(baseDir, "bin", "Debug", "net6.0", "tessdata"),
            Path.Combine(baseDir, "bin", "Release", "net6.0", "tessdata"),
            Path.Combine(baseDir, "bin", "Debug", "net9.0", "tessdata"),
            Path.Combine(baseDir, "bin", "Release", "net9.0", "tessdata")
        };

        foreach (var dir in commonTessdataPaths)
        {
            if (Directory.Exists(dir))
            {
                var engTrainedDataPath = Path.Combine(dir, "eng.traineddata");
                if (File.Exists(engTrainedDataPath))
                {
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
    /// Corrects common OCR mistakes in the extracted text using universal patterns
    /// </summary>
    /// <param name="text">OCR extracted text</param>
    /// <param name="logger">Logger instance for logging currency detection</param>
    /// <returns>Corrected text</returns>
    private static string CorrectCommonOcrMistakes(string text, ILogger logger = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var currencySymbol = GetCurrencySymbolFromSystemLocale(logger);

        if (!string.IsNullOrEmpty(currencySymbol))
        {
            text = CorrectCurrencySymbolMisreads(text, currencySymbol);
        }

        text = CorrectPipeCharacterMisreads(text);
        text = CorrectCurrencySymbolMisplacements(text);
        text = NormalizeWhitespace(text);

        return text;
    }

    /// <summary>
    /// Corrects pipe character (|) misread as digit 1 or vice versa
    /// OCR commonly confuses | with 1 or l, especially in numeric contexts
    /// </summary>
    private static string CorrectPipeCharacterMisreads(string text)
    {
        text = Regex.Replace(text, @"(\d)\|(\d)", "$1$2");
        
        return text;
    }

    /// <summary>
    /// Corrects currency symbols appearing inside numeric sequences
    /// OCR sometimes places currency symbols in wrong positions (e.g., "120₺784" → "120 784")
    /// </summary>
    private static string CorrectCurrencySymbolMisplacements(string text)
    {
        text = Regex.Replace(text, @"(\d)([\$€£¥₺₽¢₹₩])(\d)", "$1 $3");
        
        return text;
    }

    /// <summary>
    /// Normalizes whitespace (removes multiple spaces, tabs, etc.)
    /// </summary>
    private static string NormalizeWhitespace(string text)
    {
        text = Regex.Replace(text, @"[ \t]+", " ");
        text = Regex.Replace(text, @"\s+([.,;:!?])", "$1");
        
        return text;
    }

    /// <summary>
    /// Gets the currency symbol from system locale (independent of OCR language)
    /// </summary>
    /// <param name="logger">Logger instance for logging currency detection</param>
    /// <returns>Currency symbol or null if not determinable</returns>
    private static string GetCurrencySymbolFromSystemLocale(ILogger logger = null)
    {
        try
        {
            var currentCulture = CultureInfo.CurrentCulture;

            var specificCulture = CultureInfo.CreateSpecificCulture(currentCulture.Name);
            var region = new RegionInfo(specificCulture.Name);
            var symbol = region.CurrencySymbol;

            logger?.LogDebug("[OCR Currency Detection] System locale: '{CultureName}' → Symbol: '{Symbol}'", currentCulture.Name, symbol);

            return symbol;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "[OCR Currency Detection] Failed to get currency from system locale");
            return null;
        }
    }

    /// <summary>
    /// Corrects currency symbol misreads in text (e.g., % → currency symbol, works for all currencies)
    /// This method applies the same currency correction logic used in OCR results to any text
    /// </summary>
    /// <param name="text">Text to correct</param>
    /// <returns>Text with corrected currency symbols</returns>
    public string CorrectCurrencySymbols(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var currencySymbol = GetCurrencySymbolFromSystemLocale(_logger);

        if (!string.IsNullOrEmpty(currencySymbol))
        {
            return CorrectCurrencySymbolMisreads(text, currencySymbol);
        }

        return text;
    }

    /// <summary>
    /// Corrects OCR misreading of currency symbols as various characters (%, 6, t, &amp;)
    /// Uses context-aware patterns that work across all languages
    /// Common OCR mistakes: percentage sign, digit 6, lowercase t, ampersand - all corrected to currency symbol
    /// </summary>
    /// <param name="text">Text to correct</param>
    /// <param name="currencySymbol">Currency symbol to use (determined from system locale, e.g., $, €, £, ¥, etc.)</param>
    /// <returns>Corrected text</returns>
    private static string CorrectCurrencySymbolMisreads(string text, string currencySymbol)
    {
        text = Regex.Replace(
            text,
            CurrencyMisreadPatternMain,
            $"$1{currencySymbol}",
            RegexOptions.Multiline
        );

        text = Regex.Replace(
            text,
            CurrencyMisreadPatternCompact,
            $"$1{currencySymbol}"
        );

        text = Regex.Replace(
            text,
            CurrencyMisreadPattern6,
            $"$1{currencySymbol}",
            RegexOptions.Multiline
        );

        text = Regex.Replace(
            text,
            CurrencyMisreadPattern6Compact,
            $"$1{currencySymbol}"
        );

        text = Regex.Replace(
            text,
            CurrencyMisreadPatternT,
            $"$1{currencySymbol}",
            RegexOptions.Multiline
        );

        text = Regex.Replace(
            text,
            CurrencyMisreadPatternTCompact,
            $"$1{currencySymbol}"
        );

        text = Regex.Replace(
            text,
            CurrencyMisreadPatternAmpersand,
            $"$1{currencySymbol}",
            RegexOptions.Multiline
        );

        text = Regex.Replace(
            text,
            CurrencyMisreadPatternAmpersandCompact,
            $"$1{currencySymbol}"
        );

        var hasDollarInDocument = text.IndexOf('$') >= 0;
        if (!hasDollarInDocument)
        {
            text = Regex.Replace(
                text,
                CurrencyMisreadPatternCent,
                $"$1{currencySymbol}",
                RegexOptions.Multiline
            );

            text = Regex.Replace(
                text,
                CurrencyMisreadPatternCentCompact,
                $"$1{currencySymbol}"
            );
        }

        text = Regex.Replace(
            text,
            CurrencyMisreadPatternPercentCent,
            $"$1{currencySymbol}"
        );

        return text;
    }

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
            }

            _disposed = true;
        }
    }
}

