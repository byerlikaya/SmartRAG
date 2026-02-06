using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Parser.Strategies;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartRAG.Services.Document;


/// <summary>
/// Service for parsing different document formats and extracting text content using Strategy Pattern
/// </summary>
public class DocumentParserService : IDocumentParserService
{
    private const int DefaultDynamicSearchRange = 500;
    private const int DynamicSearchRangeDivisor = 10;
    private const int UltimateSearchRange = 1000;

    private static readonly char[] SentenceEndings = new char[] { '.', '!', '?', ';' };
    private static readonly string[] ParagraphEndings = new string[] { "\n\n", "\r\n\r\n" };
    private static readonly char[] WordBoundaries = new char[] { ' ', '\t', '\n', '\r' };
    private static readonly char[] PunctuationBoundaries = new char[] { ',', ':', ';', '-', '–', '—' };
    private static readonly char[] ExtendedWordBoundaries = new char[] { ' ', '\t', '\n', '\r', '.', '!', '?', ';', ',', ':', '-', '–', '—' };
    private static readonly char[] UltimateBoundaries = new char[] { ' ', '\t', '\n', '\r', '.', '!', '?', ';', ',', ':', '-', '–', '—', '(', ')', '[', ']', '{', '}' };

    private readonly SmartRagOptions _options;
    private readonly IEnumerable<IFileParser> _parsers;
    private readonly ILogger<DocumentParserService> _logger;

    public DocumentParserService(
        IOptions<SmartRagOptions> options,
        IEnumerable<IFileParser> parsers,
        ILogger<DocumentParserService> logger)
    {
        _options = options.Value;
        _parsers = parsers;
        _logger = logger;
    }

    public async Task<SmartRAG.Entities.Document> ParseDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy, string language = null)
    {
        try
        {
            long fileSize = 0;
            var originalPosition = 0L;
            var canSeek = fileStream.CanSeek;

            if (canSeek)
            {
                originalPosition = fileStream.Position;
                fileStream.Position = 0;
                fileSize = fileStream.Length;
            }
            else if (fileStream.CanRead)
            {
                fileSize = fileStream.Length;
            }

            var parser = _parsers.FirstOrDefault(p => p.CanParse(fileName, contentType)) ?? throw new NotSupportedException($"No parser found for file {fileName} with content type {contentType}");
            var result = await parser.ParseAsync(fileStream, fileName, language);
            var content = result.Content;

            if (canSeek && fileStream.CanSeek)
            {
                fileStream.Position = originalPosition;
                if (fileSize == 0)
                {
                    fileSize = fileStream.Length;
                }
            }

            var documentId = Guid.NewGuid();
            var documentType = DetermineDocumentType(fileName, contentType);
            var chunks = CreateChunks(content, documentId, documentType, fileName);
            chunks = DeduplicateChunksByContent(chunks);

            var document = CreateDocument(documentId, fileName, contentType, content, uploadedBy, chunks);
            document.FileSize = fileSize;

            if (result.Metadata != null && result.Metadata.Count > 0)
            {
                document.Metadata = new Dictionary<string, object>(result.Metadata);
            }

            PopulateMetadata(document);
            AnnotateDocumentMetadata(document, content);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document upload failed for {FileName}", fileName);
            throw;
        }
    }

    public IEnumerable<string> GetSupportedFileTypes()
    {
        return new[] { ".txt", ".md", ".json", ".xml", ".csv", ".html", ".docx", ".doc", ".pdf", ".xlsx", ".xls", ".jpg", ".png", ".wav", ".mp3", ".m4a", ".db" };
    }

    public IEnumerable<string> GetSupportedContentTypes()
    {
        return new[] { "text/", "application/", "audio/", "image/" };
    }

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

    private static void PopulateMetadata(SmartRAG.Entities.Document document)
    {
        document.Metadata ??= new Dictionary<string, object>();

        document.Metadata["FileName"] = document.FileName;
        document.Metadata["ContentType"] = document.ContentType;
        document.Metadata["UploadedBy"] = document.UploadedBy;
        document.Metadata["UploadedAt"] = document.UploadedAt;
        document.Metadata["ContentLength"] = document.Content?.Length ?? 0;
        document.Metadata["ChunkCount"] = document.Chunks?.Count ?? 0;
    }

    private void AnnotateDocumentMetadata(SmartRAG.Entities.Document document, string content)
    {
        if (document == null || document.Metadata == null) return;

        AnnotateAudioMetadata(document, content);
    }

    private void AnnotateAudioMetadata(SmartRAG.Entities.Document document, string content)
    {
        if (!document.Metadata.TryGetValue("Segments", out var segmentsObj)) return;

        var segments = ConvertToAudioSegments(segmentsObj);
        if (segments.Count == 0) return;

        var normalizedContent = NormalizeForMatching(content);
        var searchIndex = 0;

        foreach (var segment in segments)
        {
            var normalizedSegment = NormalizeForMatching(segment.Text);
            segment.NormalizedText = normalizedSegment;

            if (string.IsNullOrEmpty(normalizedSegment)) continue;

            var index = FindSegmentPosition(normalizedContent, normalizedSegment, searchIndex);
            if (index < 0) continue;

            segment.StartCharIndex = index;
            segment.EndCharIndex = index + normalizedSegment.Length;
            searchIndex = segment.EndCharIndex;
        }

        document.Metadata["Segments"] = segments;
    }

    private static List<AudioSegmentMetadata> ConvertToAudioSegments(object segmentsObj)
    {
        if (segmentsObj is List<AudioSegmentMetadata> typedList)
        {
            return typedList;
        }

        if (segmentsObj is AudioSegmentMetadata[] typedArray)
        {
            return new List<AudioSegmentMetadata>(typedArray);
        }

        if (segmentsObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            try
            {
                var json = jsonElement.GetRawText();
                var deserialized = JsonSerializer.Deserialize<List<AudioSegmentMetadata>>(json);
                return deserialized ?? new List<AudioSegmentMetadata>();
            }
            catch
            {
                return new List<AudioSegmentMetadata>();
            }
        }

        return new List<AudioSegmentMetadata>();
    }

    private static string NormalizeForMatching(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = Regex.Replace(value, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", ""); // Remove binary chars
        normalized = Regex.Replace(normalized, @"\s+", " ");
        return normalized.Trim();
    }

    private static int FindSegmentPosition(string content, string segment, int startingIndex)
    {
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(segment))
        {
            return -1;
        }

        var index = content.IndexOf(segment, startingIndex, StringComparison.OrdinalIgnoreCase);

        if (index < 0)
        {
            index = content.IndexOf(segment, StringComparison.OrdinalIgnoreCase);
        }

        return index;
    }

    /// <summary>
    /// Determines document type from ContentType and file extension
    /// </summary>
    private static string DetermineDocumentType(string fileName, string contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType) &&
            contentType.StartsWith("audio", StringComparison.OrdinalIgnoreCase))
        {
            return "Audio";
        }

        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(extension))
        {
            switch (extension)
            {
                case ".wav":
                case ".mp3":
                case ".m4a":
                case ".flac":
                case ".ogg":
                    return "Audio";
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".bmp":
                case ".tiff":
                case ".webp":
                    return "Image";
            }
        }

        return "Document";
    }

    private List<DocumentChunk> CreateChunks(string content, Guid documentId, string documentType, string fileName)
    {
        var chunks = new List<DocumentChunk>();
        var maxChunkSize = Math.Max(1, _options.MaxChunkSize);
        var chunkOverlap = Math.Max(0, _options.ChunkOverlap);
        var minChunkSize = Math.Max(1, _options.MinChunkSize);

        if (content.Length <= maxChunkSize)
        {
            chunks.Add(CreateSingleChunk(content, documentId, 0, 0, content.Length, documentType, fileName));
            return chunks;
        }

        return CreateMultipleChunks(content, documentId, maxChunkSize, chunkOverlap, minChunkSize, documentType, fileName);
    }

    private static DocumentChunk CreateSingleChunk(string content, Guid documentId, int chunkIndex, int startPosition, int endPosition, string documentType, string fileName)
    {
        return new SmartRAG.Entities.DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            FileName = fileName ?? string.Empty,
            Content = content,
            ChunkIndex = chunkIndex,
            StartPosition = startPosition,
            EndPosition = endPosition,
            CreatedAt = DateTime.UtcNow,
            RelevanceScore = 0.0,
            DocumentType = documentType
        };
    }

    private static List<DocumentChunk> CreateMultipleChunks(string content, Guid documentId, int maxChunkSize, int chunkOverlap, int minChunkSize, string documentType, string fileName)
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
            var chunkContent = content[validatedStart..validatedEnd].Trim();

            if (!string.IsNullOrWhiteSpace(chunkContent))
            {
                chunks.Add(CreateSingleChunk(chunkContent, documentId, chunkIndex, validatedStart, validatedEnd, documentType, fileName));
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
    /// Removes chunks with duplicate content (keeps first occurrence by order) and re-indexes ChunkIndex.
    /// Overlap and repeated phrases in source (e.g. audio) can produce many identical chunks; dedup reduces redundancy.
    /// </summary>
    private static List<DocumentChunk> DeduplicateChunksByContent(List<DocumentChunk> chunks)
    {
        if (chunks == null || chunks.Count == 0)
            return chunks;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var deduped = new List<DocumentChunk>();

        foreach (var chunk in chunks)
        {
            var key = chunk.Content?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(key))
                continue;
            if (seen.Add(key))
                deduped.Add(chunk);
        }

        if (deduped.Count == chunks.Count)
            return chunks;

        for (var i = 0; i < deduped.Count; i++)
        {
            deduped[i].ChunkIndex = i;
        }

        return deduped;
    }

    private static int FindOptimalBreakPoint(string content, int startIndex, int currentEndIndex, int minChunkSize)
    {
        var searchStartFromStart = startIndex + minChunkSize;
        var searchEndFromEnd = currentEndIndex;
        var contentLength = content.Length;
        var dynamicSearchRange = Math.Min(DefaultDynamicSearchRange, contentLength / DynamicSearchRangeDivisor);

        var sentenceEndIndex = FindLastSentenceEnd(content, searchStartFromStart, searchEndFromEnd);
        if (sentenceEndIndex > searchStartFromStart)
        {
            var validatedIndex = ValidateWordBoundary(content, sentenceEndIndex);
            return validatedIndex + 1;
        }

        var paragraphEndIndex = FindLastParagraphEnd(content, searchStartFromStart, searchEndFromEnd);
        if (paragraphEndIndex > searchStartFromStart)
        {
            var validatedIndex = ValidateWordBoundary(content, paragraphEndIndex);
            return validatedIndex;
        }

        var wordBoundaryIndex = FindLastWordBoundary(content, searchStartFromStart, searchEndFromEnd);
        if (wordBoundaryIndex > searchStartFromStart)
        {
            var validatedIndex = ValidateWordBoundary(content, wordBoundaryIndex);
            return validatedIndex;
        }

        var punctuationIndex = FindLastPunctuationBoundary(content, searchStartFromStart, searchEndFromEnd);
        if (punctuationIndex > searchStartFromStart)
        {
            return punctuationIndex + 1;
        }

        var intelligentSearchStart = Math.Max(startIndex, currentEndIndex - dynamicSearchRange);
        var intelligentSearchEnd = Math.Min(contentLength, currentEndIndex + dynamicSearchRange);

        var anyWordBoundary = FindAnyWordBoundary(content, intelligentSearchStart, intelligentSearchEnd);
        if (anyWordBoundary > intelligentSearchStart)
        {
            return anyWordBoundary;
        }

        var ultimateSearchStart = Math.Max(startIndex, currentEndIndex - UltimateSearchRange);
        var ultimateWordBoundary = FindUltimateWordBoundary(content, ultimateSearchStart, currentEndIndex);
        if (ultimateWordBoundary > ultimateSearchStart)
        {
            return ultimateWordBoundary;
        }

        return currentEndIndex;
    }

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
}

