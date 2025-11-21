using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Interfaces.Database;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Parser;
using SmartRAG.Interfaces.Parser.Strategies;
using SmartRAG.Models;
using SmartRAG.Services.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartRAG.Services.Document
{
    /// <summary>
    /// Service for parsing different document formats and extracting text content using Strategy Pattern
    /// </summary>
    public class DocumentParserService : IDocumentParserService
    {
        private readonly SmartRagOptions _options;
        private readonly IEnumerable<IFileParser> _parsers;
        private readonly ILogger<DocumentParserService> _logger;

        #region Constants
        // Chunk boundary search constants
        private const int DefaultDynamicSearchRange = 500;
        private const int DynamicSearchRangeDivisor = 10;
        private const int UltimateSearchRange = 1000;

        // Sentence ending constants
        private static readonly char[] SentenceEndings = new char[] { '.', '!', '?', ';' };
        private static readonly string[] ParagraphEndings = new string[] { "\n\n", "\r\n\r\n" };
        private static readonly char[] WordBoundaries = new char[] { ' ', '\t', '\n', '\r' };
        private static readonly char[] PunctuationBoundaries = new char[] { ',', ':', ';', '-', '–', '—' };
        private static readonly char[] ExtendedWordBoundaries = new char[] { ' ', '\t', '\n', '\r', '.', '!', '?', ';', ',', ':', '-', '–', '—' };
        private static readonly char[] UltimateBoundaries = new char[] { ' ', '\t', '\n', '\r', '.', '!', '?', ';', ',', ':', '-', '–', '—', '(', ')', '[', ']', '{', '}' };
        #endregion

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
                var parser = _parsers.FirstOrDefault(p => p.CanParse(fileName, contentType));
                if (parser == null)
                {
                    throw new NotSupportedException($"No parser found for file {fileName} with content type {contentType}");
                }

                var result = await parser.ParseAsync(fileStream, fileName);
                var content = result.Content;

                var documentId = Guid.NewGuid();
                var chunks = CreateChunks(content, documentId);

                var document = CreateDocument(documentId, fileName, contentType, content, uploadedBy, chunks);
                
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

        #region Private Methods

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
            if (document.Metadata == null)
            {
                document.Metadata = new Dictionary<string, object>();
            }

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
        #endregion
    }
}
