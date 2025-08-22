using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using SmartRAG.Services;
using System.Text;
using System.Text.RegularExpressions;
using OfficeOpenXml;

namespace SmartRAG.Services;

/// <summary>
/// Service for parsing different document formats and extracting text content
/// </summary>
public class DocumentParserService(
    IOptions<SmartRagOptions> options,
    ILogger<DocumentParserService> logger) : IDocumentParserService
{
    /// <summary>
    /// Static constructor to set EPPlus license once for the application
    /// </summary>
    static DocumentParserService()
    {
        // Set EPPlus 8+ license for non-commercial organization use
        ExcelPackage.License.SetNonCommercialOrganization("SmartRAG");
    }
    #region Constants

    // Content validation constants
    private const int MinContentLength = 5; // Reduced for Excel files
    private const double MinMeaningfulTextRatio = 0.1; // Reduced for Excel files
    
    // Chunk boundary search constants
    private const int DefaultDynamicSearchRange = 500;
    private const int DynamicSearchRangeDivisor = 10;
    private const int UltimateSearchRange = 1000;
    
    // File extension constants
    private static readonly string[] WordExtensions = [".docx", ".doc"];
    private static readonly string[] PdfExtensions = [".pdf"];
    private static readonly string[] ExcelExtensions = [".xlsx", ".xls"];
    private static readonly string[] TextExtensions = [".txt", ".md", ".json", ".xml", ".csv", ".html", ".htm"];
    
    // Content type constants
    private static readonly string[] WordContentTypes = [
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/msword",
        "application/vnd.ms-word"
    ];
    
    private static readonly string[] ExcelContentTypes = [
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel",
        "application/vnd.ms-excel.sheet.macroEnabled.12"
    ];
    
    private static readonly string[] TextContentTypes = ["text/", "application/json", "application/xml", "application/csv"];
    
    // Sentence ending constants
    private static readonly char[] SentenceEndings = ['.', '!', '?', ';'];
    private static readonly string[] ParagraphEndings = ["\n\n", "\r\n\r\n"];
    private static readonly char[] WordBoundaries = [' ', '\t', '\n', '\r'];
    private static readonly char[] PunctuationBoundaries = [',', ':', ';', '-', '–', '—'];
    private static readonly char[] ExtendedWordBoundaries = [' ', '\t', '\n', '\r', '.', '!', '?', ';', ',', ':', '-', '–', '—'];
    private static readonly char[] UltimateBoundaries = [' ', '\t', '\n', '\r', '.', '!', '?', ';', ',', ':', '-', '–', '—', '(', ')', '[', ']', '{', '}'];

    #endregion

    #region Fields

    private readonly SmartRagOptions _options = options.Value;

    #endregion

    #region Public Methods

    /// <summary>
    /// Parses document content based on file type
    /// </summary>
    public async Task<Entities.Document> ParseDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy)
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
            ServiceLogMessages.LogDocumentUploadFailed(logger, fileName, ex);
            throw;
        }
    }

    /// <summary>
    /// Gets supported file types
    /// </summary>
    public IEnumerable<string> GetSupportedFileTypes() => TextExtensions.Concat(WordExtensions).Concat(PdfExtensions).Concat(ExcelExtensions);

    /// <summary>
    /// Gets supported content types
    /// </summary>
    public IEnumerable<string> GetSupportedContentTypes() => TextContentTypes.Concat(WordContentTypes).Append("application/pdf").Concat(ExcelContentTypes);

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates document object with basic properties
    /// </summary>
    private static Entities.Document CreateDocument(Guid documentId, string fileName, string contentType, string content, string uploadedBy, List<DocumentChunk> chunks)
    {
        return new Entities.Document
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
    private static void PopulateMetadata(Entities.Document document)
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
    private static async Task<string> ExtractTextAsync(Stream fileStream, string fileName, string contentType)
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
            using var document = WordprocessingDocument.Open(memoryStream, false);
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
            using var package = new ExcelPackage(memoryStream);
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
                    textBuilder.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Worksheet: {0}", worksheet.Name));
                    
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
                    textBuilder.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Worksheet: {0} (empty)", worksheet.Name));
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
            if (child is Text text)
            {
                textBuilder.Append(text.Text);
            }
            else if (child is Paragraph paragraph)
            {
                ExtractTextFromElement(paragraph, textBuilder);
                textBuilder.AppendLine();
            }
            else if (child is Table table)
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
            
            using var pdfReader = new PdfReader(new MemoryStream(bytes));
            using var pdfDocument = new PdfDocument(pdfReader);

            var textBuilder = new StringBuilder();
            ExtractTextFromPdfPages(pdfDocument, textBuilder);

            var content = textBuilder.ToString();
            return CleanContent(content);
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
            var strategy = new LocationTextExtractionStrategy();
            var text = PdfTextExtractor.GetTextFromPage(page, strategy);

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
            using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync();
            return CleanContent(content);
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
        return new Entities.DocumentChunk
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

    #endregion
}

