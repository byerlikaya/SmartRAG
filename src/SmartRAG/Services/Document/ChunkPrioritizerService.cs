#nullable enable

using Microsoft.Extensions.Logging;
using SmartRAG.Entities;
using SmartRAG.Interfaces.Document;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartRAG.Services.Document
{
    /// <summary>
    /// Service for prioritizing and ordering document chunks
    /// </summary>
    public class ChunkPrioritizerService : IChunkPrioritizerService
    {
        private const int MinTokenLength = 3;

        private readonly ILogger<ChunkPrioritizerService> _logger;

        /// <summary>
        /// Initializes a new instance of the ChunkPrioritizerService
        /// </summary>
        /// <param name="logger">Logger instance for this service</param>
        public ChunkPrioritizerService(ILogger<ChunkPrioritizerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Prioritizes chunks by query word matches in content and fileName.
        /// Chunks whose fileName contains multi-word phrases from the query get higher priority.
        /// </summary>
        public List<DocumentChunk> PrioritizeChunksByQueryWords(List<DocumentChunk> chunks, List<string> queryWords)
        {
            if (chunks == null)
            {
                throw new ArgumentNullException(nameof(chunks));
            }

            if (queryWords == null || queryWords.Count == 0)
            {
                return chunks.OrderByDescending(c => c.RelevanceScore ?? 0.0).ToList();
            }

            var fileNamePhrases = GetTwoWordPhrases(queryWords);

            return chunks
                .OrderByDescending(c =>
                {
                    var contentLower = c.Content?.ToLowerInvariant() ?? string.Empty;
                    var fileNameLower = c.FileName?.ToLowerInvariant() ?? string.Empty;
                    var searchableText = string.Concat(contentLower, " ", fileNameLower);

                    var wordMatchCount = queryWords.Count(token =>
                        token.Length >= MinTokenLength &&
                        searchableText.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);

                    var fileNamePhraseBonus = fileNamePhrases.Count > 0 && fileNamePhrases.Any(p =>
                        fileNameLower.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0)
                        ? queryWords.Count
                        : 0;

                    return wordMatchCount + fileNamePhraseBonus;
                })
                .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                .ThenBy(c => c.ChunkIndex)
                .ToList();
        }

        private static List<string> GetTwoWordPhrases(List<string> queryWords)
        {
            var phrases = new List<string>();
            for (int i = 0; i < queryWords.Count - 1; i++)
            {
                var w1 = queryWords[i];
                var w2 = queryWords[i + 1];
                if (w1.Length >= 1 && w2.Length >= 3)
                    phrases.Add($"{w1} {w2}");
            }
            return phrases.Distinct().Take(5).ToList();
        }

        /// <summary>
        /// Prioritizes chunks by relevance score
        /// </summary>
        public List<DocumentChunk> PrioritizeChunksByRelevanceScore(List<DocumentChunk> chunks)
        {
            if (chunks == null)
            {
                throw new ArgumentNullException(nameof(chunks));
            }

            return chunks
                .OrderByDescending(c => c.ChunkIndex == 0)
                .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                .ThenBy(c => c.ChunkIndex)
                .ToList();
        }

        /// <summary>
        /// Merges chunks with preserved chunk 0 (document header/title chunk)
        /// </summary>
        public List<DocumentChunk> MergeChunksWithPreservedChunk0(List<DocumentChunk> chunks, DocumentChunk? chunk0)
        {
            if (chunks == null)
            {
                throw new ArgumentNullException(nameof(chunks));
            }

            if (chunk0 == null)
            {
                return chunks;
            }

            var chunk0InList = chunks.Any(c => c.Id == chunk0.Id);
            if (chunk0InList)
            {
                return chunks;
            }

            return new List<DocumentChunk> { chunk0 }.Concat(chunks).ToList();
        }
    }
}

