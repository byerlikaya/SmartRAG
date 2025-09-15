

using DocumentFormat.OpenXml;
using iText.Kernel.Pdf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartRAG.Services
{

    /// <summary>
    /// Service for parsing different document formats and extracting text content
    /// </summary>
    public class DocumentParserService : IDocumentParserService
    {
        #region Constants

        // Content validation constants
        private const int MinContentLength = 5; // Reduced for Excel files
        private const double MinMeaningfulTextRatio = 0.1; // Reduced for Excel files

        // Chunk boundary search constants
        private const int DefaultDynamicSearchRange = 500;
        private const int DynamicSearchRangeDivisor = 10;
        private const int UltimateSearchRange = 1000;
        
        // Regex pattern constants
        private const string BinaryCharactersPattern = @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]";
        
        // String format constants
        private const string WorksheetFormat = "Worksheet: {0}";
        private const string EmptyWorksheetFormat = "Worksheet: {0} (empty)";

        // File extension constants
        private static readonly string[] WordExtensions = new string[] { ".docx", ".doc" };
        private static readonly string[] PdfExtensions = new string[] { ".pdf" };
        private static readonly string[] ExcelExtensions = new string[] { ".xlsx", ".xls" };
        private static readonly string[] TextExtensions = new string[] { ".txt", ".md", ".json", ".xml", ".csv", ".html", ".htm" };
        private static readonly string[] ImageExtensions = new string[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };
        private static readonly string[] AudioExtensions = new string[] { ".mp3", ".wav", ".m4a", ".aac", ".ogg", ".flac", ".wma" };

        // Content type constants
        private static readonly string[] WordContentTypes = new string[] {
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/msword",
        "application/vnd.ms-word"
    };

        private static readonly string[] ExcelContentTypes = new string[] {
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel",
        "application/vnd.ms-excel.sheet.macroEnabled.12"
    };

        private static readonly string[] TextContentTypes = new string[] { "text/", "application/json", "application/xml", "application/csv" };
        private static readonly string[] ImageContentTypes = new string[] { 
            "image/jpeg", "image/jpg", "image/png", "image/gif", 
            "image/bmp", "image/tiff", "image/webp" 
        };
        private static readonly string[] AudioContentTypes = new string[] { 
            "audio/mpeg", "audio/wav", "audio/mp4", "audio/aac", 
            "audio/ogg", "audio/flac", "audio/x-ms-wma" 
        };

        // Sentence ending constants
        private static readonly char[] SentenceEndings = new char[] { '.', '!', '?', ';' };
        private static readonly string[] ParagraphEndings = new string[] { "\n\n", "\r\n\r\n" };
        private static readonly char[] WordBoundaries = new char[] { ' ', '\t', '\n', '\r' };
        private static readonly char[] PunctuationBoundaries = new char[] { ',', ':', ';', '-', '–', '—' };
        private static readonly char[] ExtendedWordBoundaries = new char[] { ' ', '\t', '\n', '\r', '.', '!', '?', ';', ',', ':', '-', '–', '—' };
        private static readonly char[] UltimateBoundaries = new char[] { ' ', '\t', '\n', '\r', '.', '!', '?', ';', ',', ':', '-', '–', '—', '(', ')', '[', ']', '{', '}' };

        #endregion

        #region Fields

        private readonly SmartRagOptions _options;
        private readonly IImageParserService _imageParserService;
        private readonly IAudioParserService _audioParserService;
        private readonly ILogger<DocumentParserService> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Static constructor to set EPPlus license once for the application
        /// </summary>
        static DocumentParserService()
        {
            // EPPlus 6.x doesn't require explicit license setting for non-commercial use
            // License is automatically set to NonCommercial
        }

        public DocumentParserService(
            IOptions<SmartRagOptions> options,
            IImageParserService imageParserService,
            IAudioParserService audioParserService,
            ILogger<DocumentParserService> logger)
        {
            _options = options.Value;
            _imageParserService = imageParserService;
            _audioParserService = audioParserService;
            _logger = logger;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Parses document content based on file type
        /// </summary>
        public async Task<SmartRAG.Entities.Document> ParseDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy)
        {
            try
            {
                var content = await ExtractTextAsync(fileStream, fileName, contentType);
                var documentId = Guid.NewGuid();
                var chunks = CreateChunks(content, documentId);

                var document = CreateDocument(documentId, fileName, contentType, content, uploadedBy, chunks);
                PopulateMetadata(document);

                return document;
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogDocumentUploadFailed(_logger, fileName, ex);
                throw;
            }
        }

        /// <summary>
        /// Gets supported file types
        /// </summary>
        public IEnumerable<string> GetSupportedFileTypes() => TextExtensions.Concat(WordExtensions).Concat(PdfExtensions).Concat(ExcelExtensions).Concat(ImageExtensions).Concat(AudioExtensions);

        /// <summary>
        /// Gets supported content types
        /// </summary>
        public IEnumerable<string> GetSupportedContentTypes() => TextContentTypes.Concat(WordContentTypes).Append("application/pdf").Concat(ExcelContentTypes).Concat(ImageContentTypes).Concat(AudioContentTypes);

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates document object with basic properties
        /// </summary>
        private static SmartRAG.Entities.Document CreateDocument(Guid documentId, string fileName, string contentType, string content, string uploadedBy, List<DocumentChunk> chunks)
        {
            return new SmartRAG.Entities.Document
            {
                Id = documentId,
                FileName = fileName,
                ContentType = contentType,
                Content = content,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow,
                Chunks = chunks
            };
        }

        /// <summary>
        /// Populates document metadata
        /// </summary>
        private static void PopulateMetadata(SmartRAG.Entities.Document document)
        {
            document.Metadata = new Dictionary<string, object>
            {
                ["FileName"] = document.FileName,
                ["ContentType"] = document.ContentType,
                ["UploadedBy"] = document.UploadedBy,
                ["UploadedAt"] = document.UploadedAt,
                ["ContentLength"] = document.Content?.Length ?? 0,
                ["ChunkCount"] = document.Chunks?.Count ?? 0
            };
        }

        /// <summary>
        /// Checks if file is a Word document
        /// </summary>
        private static bool IsWordDocument(string fileName, string contentType)
        {
            return WordExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) ||
                   WordContentTypes.Any(ct => contentType.Contains(ct));
        }

        /// <summary>
        /// Checks if file is a PDF document
        /// </summary>
        private static bool IsPdfDocument(string fileName, string contentType)
        {
            return PdfExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) ||
                   contentType == "application/pdf";
        }

        /// <summary>
        /// Checks if file is an Excel document
        /// </summary>
        private static bool IsExcelDocument(string fileName, string contentType)
        {
            return ExcelExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) ||
                   ExcelContentTypes.Any(ct => contentType.Contains(ct));
        }

        /// <summary>
        /// Checks if file is an image document
        /// </summary>
        private static bool IsImageDocument(string fileName, string contentType)
        {
            return ImageExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) ||
                   ImageContentTypes.Any(ct => contentType.Contains(ct));
        }

        /// <summary>
        /// Checks if file is an audio document
        /// </summary>
        private static bool IsAudioDocument(string fileName, string contentType)
        {
            return AudioExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) ||
                   AudioContentTypes.Any(ct => contentType.Contains(ct));
        }

        /// <summary>
        /// Checks if file is text-based
        /// </summary>
        private static bool IsTextBasedFile(string fileName, string contentType)
        {
            return TextExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) ||
                   TextContentTypes.Any(contentType.StartsWith);
        }

        /// <summary>
        /// Extracts text based on file type
        /// </summary>
        private async Task<string> ExtractTextAsync(Stream fileStream, string fileName, string contentType)
        {
            if (IsWordDocument(fileName, contentType))
            {
                return await ParseWordDocumentAsync(fileStream);
            }
            else if (IsPdfDocument(fileName, contentType))
            {
                return await ParsePdfDocumentAsync(fileStream);
            }
            else if (IsExcelDocument(fileName, contentType))
            {
                return await ParseExcelDocumentAsync(fileStream);
            }
            else if (IsImageDocument(fileName, contentType))
            {
                return await ParseImageDocumentAsync(fileStream);
            }
            else if (IsAudioDocument(fileName, contentType))
            {
                return await ParseAudioDocumentAsync(fileStream, fileName);
            }
            else if (IsTextBasedFile(fileName, contentType))
            {
                return await ParseTextDocumentAsync(fileStream);
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Parses Word document and extracts text content
        /// </summary>
        private static async Task<string> ParseWordDocumentAsync(Stream fileStream)
        {
            try
            {
                var memoryStream = await CreateMemoryStreamCopy(fileStream);
                using (var document = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(memoryStream, false))
                {
                    var body = document.MainDocumentPart?.Document?.Body;

                    if (body == null)
                    {
                        return string.Empty;
                    }

                    var textBuilder = new StringBuilder();
                    ExtractTextFromElement(body, textBuilder);

                    var content = textBuilder.ToString();
                    return CleanContent(content);
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Parses Excel document and extracts text content
        /// </summary>
        private static async Task<string> ParseExcelDocumentAsync(Stream fileStream)
        {
            try
            {
                var memoryStream = await CreateMemoryStreamCopy(fileStream);

                // EPPlus license already set in static constructor
                using (var package = new ExcelPackage(memoryStream))
                {
                    var textBuilder = new StringBuilder();

                    // Check if workbook has any worksheets
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        return "Excel file contains no worksheets";
                    }

                    foreach (var worksheet in package.Workbook.Worksheets)
                    {
                        if (worksheet.Dimension != null)
                        {
                            textBuilder.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, WorksheetFormat, worksheet.Name));

                            var rowCount = worksheet.Dimension.Rows;
                            var colCount = worksheet.Dimension.Columns;

                            // Add header row if exists
                            var hasData = false;
                            for (int row = 1; row <= rowCount; row++)
                            {
                                var rowBuilder = new StringBuilder();
                                var rowHasData = false;

                                for (int col = 1; col <= colCount; col++)
                                {
                                    var cellValue = worksheet.Cells[row, col].Value;
                                    if (cellValue != null)
                                    {
                                        var cellText = cellValue.ToString();
                                        if (!string.IsNullOrWhiteSpace(cellText))
                                        {
                                            rowBuilder.Append(cellText);
                                            rowHasData = true;
                                            if (col < colCount) rowBuilder.Append('\t');
                                        }
                                        else
                                        {
                                            rowBuilder.Append(' '); // Empty cell gets space
                                            if (col < colCount) rowBuilder.Append('\t');
                                        }
                                    }
                                    else
                                    {
                                        rowBuilder.Append(' '); // Null cell gets space
                                        if (col < colCount) rowBuilder.Append('\t');
                                    }
                                }

                                if (rowHasData)
                                {
                                    textBuilder.AppendLine(rowBuilder.ToString());
                                    hasData = true;
                                }
                            }

                            if (!hasData)
                            {
                                textBuilder.AppendLine("Worksheet contains no data");
                            }

                            textBuilder.AppendLine();
                        }
                        else
                        {
                            textBuilder.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, EmptyWorksheetFormat, worksheet.Name));
                        }
                    }

                    var content = textBuilder.ToString();
                    var cleanedContent = CleanContent(content);

                    // If content is still empty after cleaning, return a fallback message
                    if (string.IsNullOrWhiteSpace(cleanedContent))
                    {
                        return "Excel file processed but no text content extracted";
                    }

                    return cleanedContent;
                }
            }
            catch (Exception ex)
            {
                // Return error message instead of empty string for debugging
                return $"Error parsing Excel file: {ex.Message}";
            }
        }

        /// <summary>
        /// Creates a memory stream copy for processing
        /// </summary>
        private static async Task<MemoryStream> CreateMemoryStreamCopy(Stream fileStream)
        {
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        /// <summary>
        /// Recursively extracts text from Word document elements
        /// </summary>
        private static void ExtractTextFromElement(OpenXmlElement element, StringBuilder textBuilder)
        {
            foreach (var child in element.Elements())
            {
                if (child is DocumentFormat.OpenXml.Wordprocessing.Text text)
                {
                    textBuilder.Append(text.Text);
                }
                else if (child is DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph)
                {
                    ExtractTextFromElement(paragraph, textBuilder);
                    textBuilder.AppendLine();
                }
                else if (child is DocumentFormat.OpenXml.Wordprocessing.Table table)
                {
                    ExtractTextFromElement(table, textBuilder);
                    textBuilder.AppendLine();
                }
                else
                {
                    ExtractTextFromElement(child, textBuilder);
                }
            }
        }

        /// <summary>
        /// Parses PDF document and extracts text content
        /// </summary>
        private static async Task<string> ParsePdfDocumentAsync(Stream fileStream)
        {
            try
            {
                var memoryStream = await CreateMemoryStreamCopy(fileStream);
                var bytes = memoryStream.ToArray();

                using (var pdfReader = new iText.Kernel.Pdf.PdfReader(new MemoryStream(bytes)))
                {
                    using (var pdfDocument = new iText.Kernel.Pdf.PdfDocument(pdfReader))
                    {
                        var textBuilder = new StringBuilder();
                        ExtractTextFromPdfPages(pdfDocument, textBuilder);

                        var content = textBuilder.ToString();
                        return CleanContent(content);
                    }
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Extracts text from all PDF pages
        /// </summary>
        private static void ExtractTextFromPdfPages(PdfDocument pdfDocument, StringBuilder textBuilder)
        {
            var pageCount = pdfDocument.GetNumberOfPages();

            for (int i = 1; i <= pageCount; i++)
            {
                var page = pdfDocument.GetPage(i);
                var strategy = new iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy();
                var text = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, strategy);

                if (!string.IsNullOrWhiteSpace(text))
                {
                    textBuilder.AppendLine(text);
                }
            }
        }

        /// <summary>
        /// Parses text-based document
        /// </summary>
        private static async Task<string> ParseTextDocumentAsync(Stream fileStream)
        {
            try
            {
                using (var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    var content = await reader.ReadToEndAsync();
                    return CleanContent(content);
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Cleans and validates document content
        /// </summary>
        private static string CleanContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            var cleaned = RemoveBinaryCharacters(content);
            cleaned = RemoveExcessiveWhitespace(cleaned);
            cleaned = RemoveExcessiveLineBreaks(cleaned);
            cleaned = cleaned.Trim();

            if (!IsContentValid(cleaned))
            {
                return string.Empty;
            }

            return cleaned;
        }

        /// <summary>
        /// Removes binary and control characters
        /// </summary>
        private static string RemoveBinaryCharacters(string content)
        {
            return Regex.Replace(content, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");
        }

        /// <summary>
        /// Removes excessive whitespace
        /// </summary>
        private static string RemoveExcessiveWhitespace(string content)
        {
            return Regex.Replace(content, @"\s+", " ");
        }

        /// <summary>
        /// Removes excessive line breaks
        /// </summary>
        private static string RemoveExcessiveLineBreaks(string content)
        {
            return Regex.Replace(content, @"\n\s*\n", "\n\n");
        }

        /// <summary>
        /// Validates content length and quality
        /// </summary>
        private static bool IsContentValid(string content)
        {
            if (content.Length < MinContentLength)
            {
                return false;
            }

            // For Excel files, be more lenient with content validation
            var meaningfulTextRatio = content.Count(c => char.IsLetterOrDigit(c)) / (double)content.Length;

            // If content contains worksheet markers, it's likely valid Excel content
            if (content.Contains("Worksheet:") || content.Contains("Excel file"))
            {
                return true;
            }

            return meaningfulTextRatio >= MinMeaningfulTextRatio;
        }

        /// <summary>
        /// Creates document chunks with smart boundary detection
        /// </summary>
        private List<DocumentChunk> CreateChunks(string content, Guid documentId)
        {
            var chunks = new List<DocumentChunk>();
            var maxChunkSize = Math.Max(1, _options.MaxChunkSize);
            var chunkOverlap = Math.Max(0, _options.ChunkOverlap);
            var minChunkSize = Math.Max(1, _options.MinChunkSize);

            if (content.Length <= maxChunkSize)
            {
                chunks.Add(CreateSingleChunk(content, documentId, 0, 0, content.Length));
                return chunks;
            }

            return CreateMultipleChunks(content, documentId, maxChunkSize, chunkOverlap, minChunkSize);
        }

        /// <summary>
        /// Creates a single chunk for small content
        /// </summary>
        private static DocumentChunk CreateSingleChunk(string content, Guid documentId, int chunkIndex, int startPosition, int endPosition)
        {
            return new SmartRAG.Entities.DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                Content = content,
                ChunkIndex = chunkIndex,
                StartPosition = startPosition,
                EndPosition = endPosition,
                CreatedAt = DateTime.UtcNow,
                RelevanceScore = 0.0
            };
        }

        /// <summary>
        /// Creates multiple chunks with smart boundary detection
        /// </summary>
        private static List<DocumentChunk> CreateMultipleChunks(string content, Guid documentId, int maxChunkSize, int chunkOverlap, int minChunkSize)
        {
            var chunks = new List<DocumentChunk>();
            var startIndex = 0;
            var chunkIndex = 0;

            while (startIndex < content.Length)
            {
                var endIndex = Math.Min(startIndex + maxChunkSize, content.Length);

                if (endIndex < content.Length)
                {
                    endIndex = FindOptimalBreakPoint(content, startIndex, endIndex, minChunkSize);
                }

                var (validatedStart, validatedEnd) = ValidateChunkBoundaries(content, startIndex, endIndex);
                var chunkContent = content.Substring(validatedStart, validatedEnd - validatedStart).Trim();

                if (!string.IsNullOrWhiteSpace(chunkContent))
                {
                    chunks.Add(CreateSingleChunk(chunkContent, documentId, chunkIndex, validatedStart, validatedEnd));
                    chunkIndex++;
                }

                var nextStartIndex = CalculateNextStartPosition(content, startIndex, endIndex, chunkOverlap);

                if (nextStartIndex <= startIndex)
                {
                    startIndex = endIndex;
                }
                else
                {
                    startIndex = nextStartIndex;
                }

                if (startIndex >= content.Length)
                {
                    break;
                }
            }

            return chunks;
        }

        /// <summary>
        /// Finds the optimal break point to avoid cutting words in the middle
        /// </summary>
        private static int FindOptimalBreakPoint(string content, int startIndex, int currentEndIndex, int minChunkSize)
        {
            var searchStartFromStart = startIndex + minChunkSize;
            var searchEndFromEnd = currentEndIndex;
            var contentLength = content.Length;
            var dynamicSearchRange = Math.Min(DefaultDynamicSearchRange, contentLength / DynamicSearchRangeDivisor);

            // Priority 1: End of sentence
            var sentenceEndIndex = FindLastSentenceEnd(content, searchStartFromStart, searchEndFromEnd);
            if (sentenceEndIndex > searchStartFromStart)
            {
                var validatedIndex = ValidateWordBoundary(content, sentenceEndIndex);
                return validatedIndex + 1;
            }

            // Priority 2: End of paragraph
            var paragraphEndIndex = FindLastParagraphEnd(content, searchStartFromStart, searchEndFromEnd);
            if (paragraphEndIndex > searchStartFromStart)
            {
                var validatedIndex = ValidateWordBoundary(content, paragraphEndIndex);
                return validatedIndex;
            }

            // Priority 3: Word boundary
            var wordBoundaryIndex = FindLastWordBoundary(content, searchStartFromStart, searchEndFromEnd);
            if (wordBoundaryIndex > searchStartFromStart)
            {
                var validatedIndex = ValidateWordBoundary(content, wordBoundaryIndex);
                return validatedIndex;
            }

            // Priority 4: Punctuation boundary
            var punctuationIndex = FindLastPunctuationBoundary(content, searchStartFromStart, searchEndFromEnd);
            if (punctuationIndex > searchStartFromStart)
            {
                return punctuationIndex + 1;
            }

            // Priority 5: Dynamic search
            var intelligentSearchStart = Math.Max(startIndex, currentEndIndex - dynamicSearchRange);
            var intelligentSearchEnd = Math.Min(contentLength, currentEndIndex + dynamicSearchRange);

            var anyWordBoundary = FindAnyWordBoundary(content, intelligentSearchStart, intelligentSearchEnd);
            if (anyWordBoundary > intelligentSearchStart)
            {
                return anyWordBoundary;
            }

            // Priority 6: Ultimate fallback
            var ultimateSearchStart = Math.Max(startIndex, currentEndIndex - UltimateSearchRange);
            var ultimateWordBoundary = FindUltimateWordBoundary(content, ultimateSearchStart, currentEndIndex);
            if (ultimateWordBoundary > ultimateSearchStart)
            {
                return ultimateWordBoundary;
            }

            return currentEndIndex;
        }

        /// <summary>
        /// Validates that the break point doesn't cut words in the middle
        /// </summary>
        private static int ValidateWordBoundary(string content, int breakPoint)
        {
            if (breakPoint > 0 && breakPoint < content.Length)
            {
                var currentChar = content[breakPoint];
                var previousChar = content[breakPoint - 1];

                if (char.IsLetterOrDigit(currentChar) && char.IsLetterOrDigit(previousChar))
                {
                    for (int i = breakPoint - 1; i >= 0; i--)
                    {
                        if (char.IsWhiteSpace(content[i]) || char.IsPunctuation(content[i]))
                        {
                            return i;
                        }
                    }
                    return 0;
                }
            }

            return breakPoint;
        }

        /// <summary>
        /// Validates both start and end boundaries to ensure complete words
        /// </summary>
        private static (int start, int end) ValidateChunkBoundaries(string content, int startIndex, int endIndex)
        {
            var validatedStart = ValidateWordBoundary(content, startIndex);
            var validatedEnd = ValidateWordBoundary(content, endIndex);

            if (validatedEnd < content.Length)
            {
                var nextChar = content[validatedEnd];
                if (char.IsLetterOrDigit(nextChar))
                {
                    for (int i = validatedEnd; i < content.Length; i++)
                    {
                        if (char.IsWhiteSpace(content[i]) || char.IsPunctuation(content[i]))
                        {
                            validatedEnd = i;
                            break;
                        }
                    }
                    if (validatedEnd == endIndex)
                    {
                        validatedEnd = content.Length;
                    }
                }
            }

            return (validatedStart, validatedEnd);
        }

        /// <summary>
        /// Finds the last sentence end within the search range
        /// </summary>
        private static int FindLastSentenceEnd(string content, int searchStart, int searchEnd)
        {
            var maxIndex = -1;

            foreach (var ending in SentenceEndings)
            {
                var index = content.LastIndexOf(ending, searchEnd - 1, searchEnd - searchStart);
                if (index > maxIndex)
                {
                    maxIndex = index;
                }
            }

            return maxIndex;
        }

        /// <summary>
        /// Finds the last paragraph end within the search range
        /// </summary>
        private static int FindLastParagraphEnd(string content, int searchStart, int searchEnd)
        {
            var maxIndex = -1;

            foreach (var ending in ParagraphEndings)
            {
                var index = content.LastIndexOf(ending, searchEnd - ending.Length, searchEnd - searchStart, StringComparison.Ordinal);
                if (index > maxIndex)
                {
                    maxIndex = index + ending.Length;
                }
            }

            return maxIndex;
        }

        /// <summary>
        /// Finds the last word boundary within the search range
        /// </summary>
        private static int FindLastWordBoundary(string content, int searchStart, int searchEnd)
        {
            var maxIndex = -1;

            foreach (var boundary in WordBoundaries)
            {
                var index = content.LastIndexOf(boundary, searchEnd - 1, searchEnd - searchStart);
                if (index > maxIndex)
                {
                    maxIndex = index;
                }
            }

            if (maxIndex > searchStart)
            {
                if (maxIndex + 1 < content.Length && char.IsLetter(content[maxIndex + 1]))
                {
                    var prevBoundary = FindPreviousCompleteWordBoundary(content, searchStart, maxIndex);
                    if (prevBoundary > searchStart)
                    {
                        maxIndex = prevBoundary;
                    }
                }
            }

            return maxIndex;
        }

        /// <summary>
        /// Finds the previous complete word boundary to avoid cutting words
        /// </summary>
        private static int FindPreviousCompleteWordBoundary(string content, int searchStart, int currentBoundary)
        {
            for (int i = currentBoundary - 1; i >= searchStart; i--)
            {
                if (char.IsWhiteSpace(content[i]) || char.IsPunctuation(content[i]))
                {
                    if (i + 1 < content.Length && char.IsLetterOrDigit(content[i + 1]))
                    {
                        return i;
                    }
                }
            }
            return searchStart;
        }

        /// <summary>
        /// Finds the last punctuation boundary within the search range
        /// </summary>
        private static int FindLastPunctuationBoundary(string content, int searchStart, int searchEnd)
        {
            var maxIndex = -1;

            foreach (var boundary in PunctuationBoundaries)
            {
                var index = content.LastIndexOf(boundary, searchEnd - 1, searchEnd - searchStart);
                if (index > maxIndex)
                {
                    maxIndex = index;
                }
            }

            return maxIndex;
        }

        /// <summary>
        /// Finds any word boundary in a wider search range
        /// </summary>
        private static int FindAnyWordBoundary(string content, int searchStart, int searchEnd)
        {
            var maxIndex = -1;

            for (int i = searchEnd - 1; i >= searchStart; i--)
            {
                if (ExtendedWordBoundaries.Contains(content[i]))
                {
                    maxIndex = i;
                    break;
                }
            }

            return maxIndex;
        }

        /// <summary>
        /// Ultimate fallback - Search entire remaining content intelligently
        /// </summary>
        private static int FindUltimateWordBoundary(string content, int searchStart, int searchEnd)
        {
            var maxIndex = -1;

            for (int i = searchEnd - 1; i >= searchStart; i--)
            {
                if (UltimateBoundaries.Contains(content[i]))
                {
                    maxIndex = i;
                    break;
                }
            }

            return maxIndex;
        }

        /// <summary>
        /// Calculates the next start position with smart overlap
        /// </summary>
        private static int CalculateNextStartPosition(string content, int currentStart, int currentEnd, int overlap)
        {
            if (overlap <= 0)
            {
                return currentEnd;
            }

            var nextStart = currentEnd - overlap;

            if (nextStart <= currentStart)
            {
                nextStart = currentStart + 1;
            }

            if (nextStart >= content.Length)
            {
                return content.Length;
            }

            return nextStart;
        }

        /// <summary>
        /// Parses image document using OCR and extracts text content
        /// </summary>
        private async Task<string> ParseImageDocumentAsync(Stream fileStream)
        {
            try
            {
                // Use OCR to extract text from image
                var extractedText = await _imageParserService.ExtractTextFromImageAsync(fileStream);
                
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return string.Empty;
                }

                return CleanContent(extractedText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse image document with OCR");
                return string.Empty;
            }
        }

        /// <summary>
        /// Parses audio document using Speech-to-Text and extracts text content
        /// </summary>
        private async Task<string> ParseAudioDocumentAsync(Stream fileStream, string fileName)
        {
            try
            {
                // Use Speech-to-Text to extract text from audio
                var transcriptionResult = await _audioParserService.TranscribeAudioAsync(fileStream, fileName);
                
                if (string.IsNullOrWhiteSpace(transcriptionResult.Text))
                {
                    return string.Empty;
                }

                return CleanContent(transcriptionResult.Text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse audio document with Speech-to-Text");
                return string.Empty;
            }
        }

        #endregion
    }

}
