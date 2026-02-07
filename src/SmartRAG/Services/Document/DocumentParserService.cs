
namespace SmartRAG.Services.Document;


/// <summary>
/// Service for parsing different document formats and extracting text content using Strategy Pattern
/// </summary>
public class DocumentParserService : IDocumentParserService
{
    private const int DefaultDynamicSearchRange = 500;
    private const int DynamicSearchRangeDivisor = 10;
    private const int UltimateSearchRange = 1000;

    private static readonly char[] SentenceEndings = { '.', '!', '?', ';' };
    private static readonly string[] ParagraphEndings = { "\n\n", "\r\n\r\n" };
    private static readonly char[] WordBoundaries = { ' ', '\t', '\n', '\r' };
    private static readonly char[] PunctuationBoundaries = { ',', ':', ';', '-', '–', '—' };
    private static readonly char[] ExtendedWordBoundaries = { ' ', '\t', '\n', '\r', '.', '!', '?', ';', ',', ':', '-', '–', '—' };
    private static readonly char[] UltimateBoundaries = { ' ', '\t', '\n', '\r', '.', '!', '?', ';', ',', ':', '-', '–', '—', '(', ')', '[', ']', '{', '}' };

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

    public async Task<SmartRAG.Entities.Document> ParseDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy, string? language = null)
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

            if (result.Metadata.Count > 0)
            {
                document.Metadata = new Dictionary<string, object>(result.Metadata);
            }

            PopulateMetadata(document);
            AnnotateAudioMetadata(document, content);

            return document;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogDocumentUploadFailed(_logger, fileName, ex);
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

    private static SmartRAG.Entities.Document CreateDocument(Guid documentId, string fileName, string contentType, string content, string uploadedBy, List<DocumentChunk>? chunks)
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
        document.Metadata["ContentLength"] = document.Content.Length;
        document.Metadata["ChunkCount"] = document.Chunks?.Count ?? 0;
    }

    private static void AnnotateAudioMetadata(SmartRAG.Entities.Document document, string content)
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
        switch (segmentsObj)
        {
            case List<AudioSegmentMetadata> typedList:
                return typedList;
            case AudioSegmentMetadata[] typedArray:
                return new List<AudioSegmentMetadata>(typedArray);
        }

        if (segmentsObj is not JsonElement { ValueKind: JsonValueKind.Array } jsonElement)
            return new List<AudioSegmentMetadata>();
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

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension))
            return "Document";

        return extension switch
        {
            ".wav" or ".mp3" or ".m4a" or ".flac" or ".ogg" => "Audio",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".tiff" or ".webp" => "Image",
            _ => "Document"
        };
    }

    private List<DocumentChunk> CreateChunks(string content, Guid documentId, string documentType, string fileName)
    {
        var chunks = new List<DocumentChunk>();
        var maxChunkSize = Math.Max(1, _options.MaxChunkSize);
        var chunkOverlap = Math.Max(0, _options.ChunkOverlap);
        var minChunkSize = Math.Max(1, _options.MinChunkSize);

        if (content.Length > maxChunkSize)
            return CreateMultipleChunks(content, documentId, maxChunkSize, chunkOverlap, minChunkSize, documentType, fileName);
        chunks.Add(CreateSingleChunk(content, documentId, 0, 0, content.Length, documentType, fileName));
        return chunks;

    }

    private static DocumentChunk CreateSingleChunk(string content, Guid documentId, int chunkIndex, int startPosition, int endPosition, string documentType, string fileName)
    {
        return new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            FileName = fileName,
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

            startIndex = nextStartIndex <= startIndex ? endIndex : nextStartIndex;

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
    private static List<DocumentChunk>? DeduplicateChunksByContent(List<DocumentChunk>? chunks)
    {
        if (chunks == null || chunks.Count == 0)
            return chunks;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var deduped = new List<DocumentChunk>();

        foreach (var chunk in chunks)
        {
            var key = chunk.Content.Trim();
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
        var contentLength = content.Length;
        var dynamicSearchRange = Math.Min(DefaultDynamicSearchRange, contentLength / DynamicSearchRangeDivisor);

        var sentenceEndIndex = FindLastSentenceEnd(content, searchStartFromStart, currentEndIndex);
        if (sentenceEndIndex > searchStartFromStart)
        {
            var validatedIndex = ValidateWordBoundary(content, sentenceEndIndex);
            return validatedIndex + 1;
        }

        var paragraphEndIndex = FindLastParagraphEnd(content, searchStartFromStart, currentEndIndex);
        if (paragraphEndIndex > searchStartFromStart)
        {
            var validatedIndex = ValidateWordBoundary(content, paragraphEndIndex);
            return validatedIndex;
        }

        var wordBoundaryIndex = FindLastWordBoundary(content, searchStartFromStart, currentEndIndex);
        if (wordBoundaryIndex > searchStartFromStart)
        {
            var validatedIndex = ValidateWordBoundary(content, wordBoundaryIndex);
            return validatedIndex;
        }

        var punctuationIndex = FindLastPunctuationBoundary(content, searchStartFromStart, currentEndIndex);
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
        return ultimateWordBoundary > ultimateSearchStart ? ultimateWordBoundary : currentEndIndex;
    }

    private static int ValidateWordBoundary(string content, int breakPoint)
    {
        if (breakPoint <= 0 || breakPoint >= content.Length)
            return breakPoint;

        var currentChar = content[breakPoint];
        var previousChar = content[breakPoint - 1];

        if (!char.IsLetterOrDigit(currentChar) || !char.IsLetterOrDigit(previousChar))
            return breakPoint;

        for (var i = breakPoint - 1; i >= 0; i--)
        {
            if (char.IsWhiteSpace(content[i]) || char.IsPunctuation(content[i]))
            {
                return i;
            }
        }
        return 0;

    }

    private static (int start, int end) ValidateChunkBoundaries(string content, int startIndex, int endIndex)
    {
        var validatedStart = ValidateWordBoundary(content, startIndex);
        var validatedEnd = ValidateWordBoundary(content, endIndex);

        if (validatedEnd >= content.Length)
            return (validatedStart, validatedEnd);

        var nextChar = content[validatedEnd];
        if (!char.IsLetterOrDigit(nextChar))
            return (validatedStart, validatedEnd);

        for (var i = validatedEnd; i < content.Length; i++)
        {
            if (!char.IsWhiteSpace(content[i]) && !char.IsPunctuation(content[i])) continue;
            validatedEnd = i;
            break;
        }
        if (validatedEnd == endIndex)
        {
            validatedEnd = content.Length;
        }

        return (validatedStart, validatedEnd);
    }

    private static int FindLastSentenceEnd(string content, int searchStart, int searchEnd)
    {
        return SentenceEndings.Select(ending => content.LastIndexOf(ending, searchEnd - 1, searchEnd - searchStart)).Prepend(-1).Max();
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

        if (maxIndex <= searchStart || maxIndex + 1 >= content.Length || !char.IsLetter(content[maxIndex + 1]))
            return maxIndex;

        var prevBoundary = FindPreviousCompleteWordBoundary(content, searchStart, maxIndex);
        if (prevBoundary > searchStart)
        {
            maxIndex = prevBoundary;
        }

        return maxIndex;
    }

    private static int FindPreviousCompleteWordBoundary(string content, int searchStart, int currentBoundary)
    {
        for (var i = currentBoundary - 1; i >= searchStart; i--)
        {
            if (!char.IsWhiteSpace(content[i]) && !char.IsPunctuation(content[i]))
                continue;
            if (i + 1 < content.Length && char.IsLetterOrDigit(content[i + 1]))
            {
                return i;
            }
        }
        return searchStart;
    }

    private static int FindLastPunctuationBoundary(string content, int searchStart, int searchEnd)
    {
        return PunctuationBoundaries.Select(boundary => content.LastIndexOf(boundary, searchEnd - 1, searchEnd - searchStart)).Prepend(-1).Max();
    }

    private static int FindAnyWordBoundary(string content, int searchStart, int searchEnd)
    {
        var maxIndex = -1;

        for (var i = searchEnd - 1; i >= searchStart; i--)
        {
            if (!ExtendedWordBoundaries.Contains(content[i])) continue;
            maxIndex = i;
            break;
        }

        return maxIndex;
    }

    private static int FindUltimateWordBoundary(string content, int searchStart, int searchEnd)
    {
        var maxIndex = -1;

        for (var i = searchEnd - 1; i >= searchStart; i--)
        {
            if (!UltimateBoundaries.Contains(content[i])) continue;
            maxIndex = i;
            break;
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

        return nextStart >= content.Length ? content.Length : nextStart;
    }
}

