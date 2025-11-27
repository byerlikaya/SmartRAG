#nullable enable

using Microsoft.Extensions.Logging;
using SmartRAG.Entities;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Search;
using SmartRAG.Interfaces.Support;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DocumentEntity = SmartRAG.Entities.Document;

namespace SmartRAG.Services.Search
{
    /// <summary>
    /// Service for building search sources from document chunks
    /// </summary>
    public class SourceBuilderService : ISourceBuilderService
    {
        private readonly ITextNormalizationService _textNormalizationService;
        private readonly ILogger<SourceBuilderService> _logger;

        /// <summary>
        /// Initializes a new instance of the SourceBuilderService
        /// </summary>
        /// <param name="textNormalizationService">Service for text normalization operations</param>
        /// <param name="logger">Logger instance for this service</param>
        public SourceBuilderService(
            ITextNormalizationService textNormalizationService,
            ILogger<SourceBuilderService> logger)
        {
            _textNormalizationService = textNormalizationService;
            _logger = logger;
        }

        /// <summary>
        /// [Document Query] Builds search sources from document chunks
        /// </summary>
        public async Task<List<SearchSource>> BuildSourcesAsync(List<DocumentChunk> chunks, IDocumentRepository documentRepository)
        {
            var sources = new List<SearchSource>();
            if (chunks.Count == 0)
            {
                return sources;
            }

            var documentCache = new Dictionary<Guid, DocumentEntity?>();

            foreach (var chunk in chunks)
            {
                var document = await GetDocumentForChunkAsync(chunk.DocumentId, documentCache, documentRepository);
                var sourceType = DetermineDocumentSourceType(document);
                var (startTime, endTime) = CalculateAudioTimestampRange(document, chunk);
                var location = BuildDocumentLocationDescription(chunk, document, startTime, endTime);

                sources.Add(new SearchSource
                {
                    SourceType = sourceType,
                    DocumentId = chunk.DocumentId,
                    FileName = document?.FileName ?? "Document",
                    RelevantContent = chunk.Content,
                    RelevanceScore = chunk.RelevanceScore ?? 0.0,
                    ChunkIndex = chunk.ChunkIndex,
                    StartPosition = chunk.StartPosition,
                    EndPosition = chunk.EndPosition,
                    StartTimeSeconds = startTime,
                    EndTimeSeconds = endTime,
                    Location = location
                });
            }

            return sources;
        }

        private async Task<DocumentEntity?> GetDocumentForChunkAsync(Guid documentId, Dictionary<Guid, DocumentEntity?> cache, IDocumentRepository documentRepository)
        {
            if (cache.TryGetValue(documentId, out var cachedDocument))
            {
                return cachedDocument;
            }

            var document = await documentRepository.GetByIdAsync(documentId);
            cache[documentId] = document;
            return document;
        }

        private static string DetermineDocumentSourceType(DocumentEntity? document)
        {
            if (document == null)
            {
                return "Document";
            }

            if (!string.IsNullOrWhiteSpace(document.ContentType) &&
                document.ContentType.StartsWith("audio", StringComparison.OrdinalIgnoreCase))
            {
                return "Audio";
            }

            var extension = Path.GetExtension(document.FileName)?.ToLowerInvariant();
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

        private (double? Start, double? End) CalculateAudioTimestampRange(DocumentEntity? document, DocumentChunk chunk)
        {
            var segments = ExtractAudioSegments(document);
            if (segments.Count == 0)
            {
                return (null, null);
            }

            var chunkStart = chunk.StartPosition;
            var chunkEnd = chunk.EndPosition;
            var chunkNormalized = _textNormalizationService.NormalizeForMatching(chunk.Content);

            var overlappingSegments = new List<AudioSegmentMetadata>();

            foreach (var segment in segments)
            {
                var hasCharacterMapping = segment.EndCharIndex > 0;
                if (hasCharacterMapping &&
                    segment.StartCharIndex < chunkEnd &&
                    segment.EndCharIndex > chunkStart)
                {
                    overlappingSegments.Add(segment);
                    continue;
                }

                var normalizedSegment = !string.IsNullOrWhiteSpace(segment.NormalizedText)
                    ? segment.NormalizedText
                    : _textNormalizationService.NormalizeForMatching(segment.Text);

                if (string.IsNullOrWhiteSpace(normalizedSegment) || string.IsNullOrWhiteSpace(chunkNormalized))
                {
                    continue;
                }

                if (chunkNormalized.IndexOf(normalizedSegment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    overlappingSegments.Add(segment);
                }
            }

            if (overlappingSegments.Count == 0)
            {
                return (null, null);
            }

            var start = overlappingSegments.First().Start;
            var end = overlappingSegments.Last().End;

            return (start, end);
        }

        private static List<AudioSegmentMetadata> ExtractAudioSegments(DocumentEntity? document)
        {
            if (document?.Metadata == null)
            {
                return new List<AudioSegmentMetadata>();
            }

            if (!document.Metadata.TryGetValue("Segments", out var segmentsObj))
            {
                return new List<AudioSegmentMetadata>();
            }

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

        private static string BuildDocumentLocationDescription(DocumentChunk chunk, DocumentEntity? document, double? startTimeSeconds, double? endTimeSeconds)
        {
            var builder = new StringBuilder();
            builder.Append($"Chunk #{chunk.ChunkIndex + 1}");
            builder.Append($" | Characters {chunk.StartPosition}-{chunk.EndPosition}");

            if (startTimeSeconds.HasValue || endTimeSeconds.HasValue)
            {
                builder.Append(" | Audio ");
                builder.Append(FormatTimeRange(startTimeSeconds, endTimeSeconds));
            }

            if (document != null && !string.IsNullOrWhiteSpace(document.FileName))
            {
                builder.Append($" | Source: {document.FileName}");
            }

            return builder.ToString();
        }

        private static string FormatTimeRange(double? startSeconds, double? endSeconds)
        {
            if (!startSeconds.HasValue && !endSeconds.HasValue)
            {
                return "timestamp unavailable";
            }

            if (startSeconds.HasValue && endSeconds.HasValue)
            {
                return $"{FormatSeconds(startSeconds.Value)} - {FormatSeconds(endSeconds.Value)}";
            }

            if (startSeconds.HasValue)
            {
                return $"{FormatSeconds(startSeconds.Value)} →";
            }

            return $"← {FormatSeconds(endSeconds!.Value)}";
        }

        private static string FormatSeconds(double seconds)
        {
            if (seconds < 0)
            {
                seconds = 0;
            }

            var timeSpan = TimeSpan.FromSeconds(seconds);

            if (timeSpan.TotalHours >= 1)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}", (int)timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds);
            }

            return string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
        }
    }
}

