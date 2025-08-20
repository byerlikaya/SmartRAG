using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartRAG.Services;

/// <summary>
/// Service for parsing different document formats and extracting text content
/// </summary>
public class DocumentParserService(IOptions<SmartRagOptions> options) : IDocumentParserService
{
    private readonly SmartRagOptions _options = options.Value;
    /// <summary>
    /// Parses document content based on file type
    /// </summary>
    public async Task<Entities.Document> ParseDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy)
    {
        try
        {
            var content = await DocumentParserService.ExtractTextAsync(fileStream, fileName, contentType);

            var documentId = Guid.NewGuid();
            var chunks = CreateChunks(content, documentId);

            var document = new SmartRAG.Entities.Document
            {
                Id = documentId,
                FileName = fileName,
                ContentType = contentType,
                Content = content,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow,
                Chunks = chunks
            };

            // Populate metadata
            document.Metadata = new Dictionary<string, object>
            {
                ["FileName"] = document.FileName,
                ["ContentType"] = document.ContentType,
                ["UploadedBy"] = document.UploadedBy,
                ["UploadedAt"] = document.UploadedAt,
                ["ContentLength"] = document.Content?.Length ?? 0,
                ["ChunkCount"] = document.Chunks?.Count ?? 0
            };

            return document;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Checks if file is a Word document
    /// </summary>
    private static bool IsWordDocument(string fileName, string contentType)
    {
        var wordExtensions = new[] { ".docx", ".doc" };
        var wordContentTypes = new[] {
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/msword",
            "application/vnd.ms-word"
        };

        return wordExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) || wordContentTypes.Any(ct => contentType.Contains(ct));
    }

    /// <summary>
    /// Checks if file is a PDF document
    /// </summary>
    private static bool IsPdfDocument(string fileName, string contentType)
    {
        return fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || contentType == "application/pdf";
    }

    /// <summary>
    /// Checks if file is text-based
    /// </summary>
    private static bool IsTextBasedFile(string fileName, string contentType)
    {
        var textExtensions = new[] { ".txt", ".md", ".json", ".xml", ".csv", ".html", ".htm" };
        var textContentTypes = new[] { "text/", "application/json", "application/xml", "application/csv" };

        return textExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) ||
               textContentTypes.Any(contentType.StartsWith);
    }

    /// <summary>
    /// Parses Word document and extracts text content
    /// </summary>
    private static async Task<string> ParseWordDocumentAsync(Stream fileStream)
    {
        try
        {
            // Create a copy of the stream for OpenXML
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

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
                textBuilder.AppendLine(); // Add line break after paragraphs
            }
            else if (child is Table table)
            {
                ExtractTextFromElement(table, textBuilder);
                textBuilder.AppendLine(); // Add line break after tables
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
            // Create a copy of the stream for iText
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var bytes = memoryStream.ToArray();
            using var pdfReader = new PdfReader(new MemoryStream(bytes));
            using var pdfDocument = new PdfDocument(pdfReader);

            var textBuilder = new StringBuilder();

            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var page = pdfDocument.GetPage(i);
                var strategy = new LocationTextExtractionStrategy();
                var text = PdfTextExtractor.GetTextFromPage(page, strategy);

                if (!string.IsNullOrWhiteSpace(text))
                {
                    textBuilder.AppendLine(text);
                }
            }

            var content = textBuilder.ToString();
            return CleanContent(content);
        }
        catch (Exception)
        {
            return string.Empty;
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

        // Remove binary characters and control characters
        var cleaned = Regex.Replace(content, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");

        // Remove excessive whitespace
        cleaned = Regex.Replace(cleaned, @"\s+", " ");

        // Remove excessive line breaks
        cleaned = Regex.Replace(cleaned, @"\n\s*\n", "\n\n");

        // Trim whitespace
        cleaned = cleaned.Trim();

        // Validate content length and quality
        if (cleaned.Length < 10)
        {
            return string.Empty;
        }

        // Check if content contains meaningful text (not just binary data)
        var meaningfulTextRatio = cleaned.Count(c => char.IsLetterOrDigit(c)) / (double)cleaned.Length;
        if (meaningfulTextRatio < 0.3) // Less than 30% meaningful text
        {
            return string.Empty;
        }

        return cleaned;
    }

    /// <summary>
    /// Gets supported file types
    /// </summary>
    public IEnumerable<string> GetSupportedFileTypes() => [
            ".txt", ".md", ".json", ".xml", ".csv", ".html", ".htm",
            ".docx", ".doc", ".pdf"
        ];

    /// <summary>
    /// Gets supported content types
    /// </summary>
    public IEnumerable<string> GetSupportedContentTypes() => [
            "text/plain", "text/markdown", "text/html",
            "application/json", "application/xml", "application/csv",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/msword", "application/pdf"
        ];

    private static async Task<string> ExtractTextAsync(Stream fileStream, string fileName, string contentType)
    {
        if (IsWordDocument(fileName, contentType))
        {
            return await DocumentParserService.ParseWordDocumentAsync(fileStream);
        }
        else if (IsPdfDocument(fileName, contentType))
        {
            return await DocumentParserService.ParsePdfDocumentAsync(fileStream);
        }
        else if (IsTextBasedFile(fileName, contentType))
        {
            return await DocumentParserService.ParseTextDocumentAsync(fileStream);
        }
        else
        {
            return string.Empty;
        }
    }

    private List<DocumentChunk> CreateChunks(string content, Guid documentId)
    {
        var chunks = new List<DocumentChunk>();
        var maxChunkSize = Math.Max(1, _options.MaxChunkSize);
        var chunkOverlap = Math.Max(0, _options.ChunkOverlap);
        var minChunkSize = Math.Max(1, _options.MinChunkSize);

        if (content.Length <= maxChunkSize)
        {
            chunks.Add(new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                Content = content,
                ChunkIndex = 0,
                StartPosition = 0,
                EndPosition = content.Length,
                CreatedAt = DateTime.UtcNow,
                RelevanceScore = 0.0
                // Embedding will be set by DocumentService after AI processing
            });
            return chunks;
        }

        var startIndex = 0;
        var chunkIndex = 0;

        while (startIndex < content.Length)
        {
            var endIndex = Math.Min(startIndex + maxChunkSize, content.Length);

            // Smart boundary detection to avoid cutting words in the middle
            if (endIndex < content.Length)
            {
                endIndex = FindOptimalBreakPoint(content, startIndex, endIndex, minChunkSize);
            }

            // Validate both start and end boundaries to ensure complete words
            var (validatedStart, validatedEnd) = ValidateChunkBoundaries(content, startIndex, endIndex);
            var chunkContent = content.Substring(validatedStart, validatedEnd - validatedStart).Trim();

            if (!string.IsNullOrWhiteSpace(chunkContent))
            {
                chunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    Content = chunkContent,
                    ChunkIndex = chunkIndex,
                    StartPosition = validatedStart,
                    EndPosition = validatedEnd,
                    CreatedAt = DateTime.UtcNow,
                    RelevanceScore = 0.0
                    // Embedding will be set by DocumentService after AI processing
                });
                chunkIndex++;
            }

            // Smart overlap calculation to ensure meaningful context
            var nextStartIndex = CalculateNextStartPosition(content, startIndex, endIndex, chunkOverlap);

            // Safety check to prevent infinite loops
            if (nextStartIndex <= startIndex)
            {
                startIndex = endIndex; // Force progression
            }
            else
            {
                startIndex = nextStartIndex;
            }

            // Additional safety check
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
        // DYNAMIC CHUNK CREATION: Search from both ends intelligently
        var searchStartFromStart = startIndex + minChunkSize;
        var searchEndFromEnd = currentEndIndex;

        // Calculate dynamic search range based on content length
        var contentLength = content.Length;
        var dynamicSearchRange = Math.Min(500, contentLength / 10); // Dynamic range: 500 chars or 10% of content

        // Priority 1: End of sentence (period, exclamation, question mark) - Search from both ends
        var sentenceEndIndex = FindLastSentenceEnd(content, searchStartFromStart, searchEndFromEnd);
        if (sentenceEndIndex > searchStartFromStart)
        {
            // Additional check: Ensure we don't cut words in the middle
            var validatedIndex = ValidateWordBoundary(content, sentenceEndIndex);
            return validatedIndex + 1;
        }

        // Priority 2: End of paragraph (double newline) - Search from both ends
        var paragraphEndIndex = FindLastParagraphEnd(content, searchStartFromStart, searchEndFromEnd);
        if (paragraphEndIndex > searchStartFromStart)
        {
            var validatedIndex = ValidateWordBoundary(content, paragraphEndIndex);
            return validatedIndex;
        }

        // Priority 3: Word boundary (space, tab, newline) - Search from both ends
        var wordBoundaryIndex = FindLastWordBoundary(content, searchStartFromStart, searchEndFromEnd);
        if (wordBoundaryIndex > searchStartFromStart)
        {
            var validatedIndex = ValidateWordBoundary(content, wordBoundaryIndex);
            return validatedIndex;
        }

        // Priority 4: Punctuation boundary (comma, semicolon, colon) - Search from both ends
        var punctuationIndex = FindLastPunctuationBoundary(content, searchStartFromStart, searchEndFromEnd);
        if (punctuationIndex > searchStartFromStart)
        {
            return punctuationIndex + 1;
        }

        // Priority 5: DYNAMIC SEARCH - Find any word boundary in intelligent range
        var intelligentSearchStart = Math.Max(startIndex, currentEndIndex - dynamicSearchRange);
        var intelligentSearchEnd = Math.Min(contentLength, currentEndIndex + dynamicSearchRange);

        var anyWordBoundary = FindAnyWordBoundary(content, intelligentSearchStart, intelligentSearchEnd);
        if (anyWordBoundary > intelligentSearchStart)
        {
            return anyWordBoundary;
        }

        // Priority 6: ULTIMATE FALLBACK - Search entire remaining content intelligently
        var ultimateSearchStart = Math.Max(startIndex, currentEndIndex - 1000);
        var ultimateWordBoundary = FindUltimateWordBoundary(content, ultimateSearchStart, currentEndIndex);
        if (ultimateWordBoundary > ultimateSearchStart)
        {
            return ultimateWordBoundary;
        }

        // Final fallback: Use current end index
        return currentEndIndex;
    }

    /// <summary>
    /// Validates that the break point doesn't cut words in the middle
    /// </summary>
    private static int ValidateWordBoundary(string content, int breakPoint)
    {
        // Check if we're in the middle of a word
        if (breakPoint > 0 && breakPoint < content.Length)
        {
            var currentChar = content[breakPoint];
            var previousChar = content[breakPoint - 1];

            // If we're in the middle of a word, find the previous word boundary
            if (char.IsLetterOrDigit(currentChar) && char.IsLetterOrDigit(previousChar))
            {
                // Look backwards for the last word boundary
                for (int i = breakPoint - 1; i >= 0; i--)
                {
                    if (char.IsWhiteSpace(content[i]) || char.IsPunctuation(content[i]))
                    {
                        // Found a word boundary
                        return i;
                    }
                }
                // If no boundary found, return start of content
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

        // Additional check: Ensure end boundary doesn't cut words
        if (validatedEnd < content.Length)
        {
            var nextChar = content[validatedEnd];
            if (char.IsLetterOrDigit(nextChar))
            {
                // Find the next word boundary
                for (int i = validatedEnd; i < content.Length; i++)
                {
                    if (char.IsWhiteSpace(content[i]) || char.IsPunctuation(content[i]))
                    {
                        validatedEnd = i;
                        break;
                    }
                }
                // If no boundary found, use content length
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
        var sentenceEndings = new[] { '.', '!', '?', ';' };
        var maxIndex = -1;

        foreach (var ending in sentenceEndings)
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
        var paragraphEndings = new[] { "\n\n", "\r\n\r\n" };
        var maxIndex = -1;

        foreach (var ending in paragraphEndings)
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
        var wordBoundaries = new[] { ' ', '\t', '\n', '\r' };
        var maxIndex = -1;

        foreach (var boundary in wordBoundaries)
        {
            var index = content.LastIndexOf(boundary, searchEnd - 1, searchEnd - searchStart);
            if (index > maxIndex)
            {
                maxIndex = index;
            }
        }

        // Additional check: Ensure we don't cut words in the middle
        if (maxIndex > searchStart)
        {
            // Look ahead to see if the next character is a letter (indicating word continuation)
            if (maxIndex + 1 < content.Length && char.IsLetter(content[maxIndex + 1]))
            {
                // Find the previous complete word boundary
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
        // Look for the previous space or punctuation that ends a complete word
        for (int i = currentBoundary - 1; i >= searchStart; i--)
        {
            if (char.IsWhiteSpace(content[i]) || char.IsPunctuation(content[i]))
            {
                // Check if this creates a complete word
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
        var punctuationBoundaries = new[] { ',', ':', ';', '-', '–', '—' };
        var maxIndex = -1;

        foreach (var boundary in punctuationBoundaries)
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
        var wordBoundaries = new[] { ' ', '\t', '\n', '\r', '.', '!', '?', ';', ',', ':', '-', '–', '—' };
        var maxIndex = -1;

        // Search from end to start to find the closest boundary
        for (int i = searchEnd - 1; i >= searchStart; i--)
        {
            if (wordBoundaries.Contains(content[i]))
            {
                maxIndex = i;
                break;
            }
        }

        return maxIndex;
    }

    /// <summary>
    /// ULTIMATE FALLBACK - Search entire remaining content intelligently
    /// </summary>
    private static int FindUltimateWordBoundary(string content, int searchStart, int searchEnd)
    {
        var wordBoundaries = new[] { ' ', '\t', '\n', '\r', '.', '!', '?', ';', ',', ':', '-', '–', '—', '(', ')', '[', ']', '{', '}' };
        var maxIndex = -1;

        // Search from end to start to find ANY boundary
        for (int i = searchEnd - 1; i >= searchStart; i--)
        {
            if (wordBoundaries.Contains(content[i]))
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

        // Calculate next start position with overlap
        var nextStart = currentEnd - overlap;

        // Ensure we don't go backwards or create infinite loops
        if (nextStart <= currentStart)
        {
            // Force progression to avoid infinite loops
            nextStart = currentStart + 1;
        }

        // Ensure we don't exceed content length
        if (nextStart >= content.Length)
        {
            return content.Length; // End the loop
        }

        return nextStart;
    }
}
