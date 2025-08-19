using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartRAG.Services;

/// <summary>
/// Service for parsing different document formats and extracting text content
/// </summary>
public class DocumentParserService(SmartRagOptions options) : IDocumentParserService
{
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
        
        // Basit ve güvenilir chunk boyutları
        var maxChunkSize = Math.Max(1000, options.MaxChunkSize);
        var minChunkSize = Math.Max(300, options.MinChunkSize);
        var chunkOverlap = Math.Max(100, options.ChunkOverlap); // Overlap ekle

        // Tek chunk yeterliyse
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
            });
            return chunks;
        }

        var startIndex = 0;
        var chunkIndex = 0;

        // Overlap ile chunking - önemli bilgiler kaybolmasın
        while (startIndex < content.Length)
        {
            var endIndex = Math.Min(startIndex + maxChunkSize, content.Length);
            
            // Kelime sınırında böl
            if (endIndex < content.Length)
            {
                var wordBoundaryIndex = FindWordBoundary(content, startIndex, endIndex);
                
                // Sadece geçerli sınır bulunduysa kullan
                if (wordBoundaryIndex > startIndex + minChunkSize && wordBoundaryIndex < endIndex + 50)
                {
                    endIndex = wordBoundaryIndex;
                }
            }

            var chunkContent = content.Substring(startIndex, endIndex - startIndex).Trim();

            // Chunk'ı ekle
            if (!string.IsNullOrWhiteSpace(chunkContent) && chunkContent.Length >= minChunkSize)
            {
                chunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    Content = chunkContent,
                    ChunkIndex = chunkIndex,
                    StartPosition = startIndex,
                    EndPosition = endIndex,
                    CreatedAt = DateTime.UtcNow,
                    RelevanceScore = 0.0
                });
                chunkIndex++;
            }

            // Sonraki chunk için başlangıç - OVERLAP ile
            startIndex = Math.Max(startIndex + 1, endIndex - chunkOverlap);
            
            // Güvenlik kontrolü - startIndex her zaman ilerlemeli
            if (startIndex >= content.Length)
            {
                break;
            }
        }

        return chunks;
    }

    /// <summary>
    /// Paragraf sonunu bul (\n\n)
    /// </summary>
    private static int FindParagraphEnd(string content, int searchStart, int searchEnd)
    {
        // searchStart'tan searchEnd'e kadar olan aralıkta arama yap
        if (searchStart >= searchEnd || searchStart < 0 || searchEnd > content.Length)
            return searchEnd;

        // Çift newline ara (\n\n) - searchStart'tan searchEnd'e kadar
        var doubleNewlineIndex = content.LastIndexOf("\n\n", searchEnd - 1, searchEnd - searchStart);
        if (doubleNewlineIndex >= searchStart)
        {
            return doubleNewlineIndex + 2; // \n\n'i dahil et
        }

        // Tek newline ara
        var newlineIndex = content.LastIndexOf('\n', searchEnd - 1, searchEnd - searchStart);
        if (newlineIndex >= searchStart)
        {
            return newlineIndex + 1; // \n'i dahil et
        }

        return searchEnd;
    }

    /// <summary>
    /// Cümle sonunu bul (., !, ?)
    /// </summary>
    private static int FindSentenceEnd(string content, int searchStart, int searchEnd)
    {
        // searchStart'tan searchEnd'e kadar olan aralıkta arama yap
        if (searchStart >= searchEnd || searchStart < 0 || searchEnd > content.Length)
            return searchEnd;

        // Cümle sonu işaretlerini ara - searchStart'tan searchEnd'e kadar
        var periodIndex = content.LastIndexOf('.', searchEnd - 1, searchEnd - searchStart);
        var exclamationIndex = content.LastIndexOf('!', searchEnd - 1, searchEnd - searchStart);
        var questionIndex = content.LastIndexOf('?', searchEnd - 1, searchEnd - searchStart);

        // En son cümle sonunu bul
        var maxIndex = Math.Max(Math.Max(periodIndex, exclamationIndex), questionIndex);
        
        if (maxIndex >= searchStart)
        {
            return maxIndex + 1; // Cümle sonu işaretini dahil et
        }

        return searchEnd;
    }

    /// <summary>
    /// Kelime sınırını bul - basit ve güvenilir
    /// </summary>
    private static int FindWordBoundary(string content, int searchStart, int searchEnd)
    {
        if (searchStart >= searchEnd || searchStart < 0 || searchEnd > content.Length)
            return searchEnd;

        // Sadece geriye doğru arama - daha güvenilir
        var spaceIndex = content.LastIndexOf(' ', searchEnd - 1);
        var newlineIndex = content.LastIndexOf('\n', searchEnd - 1);
        var periodIndex = content.LastIndexOf('.', searchEnd - 1);
        var commaIndex = content.LastIndexOf(',', searchEnd - 1);

        // En son geçerli sınırı bul
        var maxIndex = Math.Max(Math.Max(Math.Max(spaceIndex, newlineIndex), periodIndex), commaIndex);
        
        // Sadece searchStart'tan sonraki sınırları kabul et
        if (maxIndex >= searchStart)
        {
            return maxIndex + 1; // Sınır karakterini dahil et
        }

        return searchEnd;
    }
}
