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
        
        // Türkçe metinler için optimize edilmiş chunk boyutları
        var maxChunkSize = Math.Max(1200, options.MaxChunkSize); // Daha küçük chunk'lar semantic search için daha iyi
        var chunkOverlap = Math.Max(250, options.ChunkOverlap);  // Overlap artırıldı - daha iyi context
        var minChunkSize = Math.Max(400, options.MinChunkSize);  // Minimum boyut artırıldı - daha anlamlı chunk'lar

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

        while (startIndex < content.Length)
        {
            var endIndex = Math.Min(startIndex + maxChunkSize, content.Length);

            // BASİT ve ETKİLİ chunking - kelime sınırlarında böl
            if (endIndex < content.Length)
            {
                // Sadece kelime sınırında böl - çok karmaşık olmasın
                var wordBoundaryIndex = FindWordBoundary(content, startIndex, endIndex);
                if (wordBoundaryIndex > startIndex + minChunkSize && wordBoundaryIndex < endIndex + 100)
                {
                    endIndex = wordBoundaryIndex;
                }
            }

            var chunkContent = content.Substring(startIndex, endIndex - startIndex).Trim();

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

            // Overlap ile sonraki başlangıç pozisyonunu hesapla
            startIndex = Math.Max(startIndex + 1, endIndex - chunkOverlap);
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
    /// Kelime sınırını bul - HEM İLERİYE HEM GERİYE doğru arama yaparak
    /// </summary>
    private static int FindWordBoundary(string content, int searchStart, int searchEnd)
    {
        if (searchStart >= searchEnd || searchStart < 0 || searchEnd > content.Length)
            return searchEnd;

        // İLERİYE DOĞRU arama - searchEnd'den sonraki ilk kelime sınırı
        var forwardSpaceIndex = content.IndexOf(' ', searchEnd);
        var forwardTabIndex = content.IndexOf('\t', searchEnd);
        var forwardNewlineIndex = content.IndexOf('\n', searchEnd);
        var forwardReturnIndex = content.IndexOf('\r', searchEnd);
        
        // GERİYE DOĞRU arama - searchEnd'den önceki son kelime sınırı
        var backwardSpaceIndex = content.LastIndexOf(' ', searchEnd - 1);
        var backwardTabIndex = content.LastIndexOf('\t', searchEnd - 1);
        var backwardNewlineIndex = content.LastIndexOf('\n', searchEnd - 1);
        var backwardReturnIndex = content.LastIndexOf('\r', searchEnd - 1);

        // Noktalama işaretleri - her iki yönde
        var forwardCommaIndex = content.IndexOf(',', searchEnd);
        var backwardCommaIndex = content.LastIndexOf(',', searchEnd - 1);
        var forwardPeriodIndex = content.IndexOf('.', searchEnd);
        var backwardPeriodIndex = content.LastIndexOf('.', searchEnd - 1);

        // En iyi sınırı bul - hem ileriye hem geriye
        var bestIndex = GetBestBoundaryIndex(searchEnd, searchStart, 
            new[] { forwardSpaceIndex, forwardTabIndex, forwardNewlineIndex, forwardReturnIndex, 
                    forwardCommaIndex, forwardPeriodIndex },
            new[] { backwardSpaceIndex, backwardTabIndex, backwardNewlineIndex, backwardReturnIndex, 
                    backwardCommaIndex, backwardPeriodIndex });

        return bestIndex;
    }

    /// <summary>
    /// En iyi kelime sınırını bul - ileriye ve geriye aramaları karşılaştır
    /// </summary>
    private static int GetBestBoundaryIndex(int currentPosition, int minPosition, int[] forwardIndexes, int[] backwardIndexes)
    {
        // İleriye doğru en yakın geçerli index
        var validForwardIndexes = forwardIndexes
            .Where(idx => idx > 0 && idx < int.MaxValue)
            .ToList();

        // Geriye doğru en yakın geçerli index
        var validBackwardIndexes = backwardIndexes
            .Where(idx => idx >= minPosition && idx < currentPosition)
            .ToList();

        if (!validForwardIndexes.Any() && !validBackwardIndexes.Any())
            return currentPosition;

        // Mesafeleri hesapla
        var forwardDistance = validForwardIndexes.Any() ? validForwardIndexes.Min() - currentPosition : int.MaxValue;
        var backwardDistance = validBackwardIndexes.Any() ? currentPosition - validBackwardIndexes.Max() : int.MaxValue;

        // En yakın olanı seç (100 karakter sınırı ile)
        if (forwardDistance <= 100 && backwardDistance <= 100)
        {
            return forwardDistance <= backwardDistance 
                ? validForwardIndexes.Min() 
                : validBackwardIndexes.Max();
        }
        else if (forwardDistance <= 100)
        {
            return validForwardIndexes.Min();
        }
        else if (backwardDistance <= 100)
        {
            return validBackwardIndexes.Max();
        }

        return currentPosition;
    }
}
