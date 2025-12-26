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
        /// Prioritizes chunks by query word matches
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

            return chunks
                .OrderByDescending(c =>
                {
                    var contentLower = c.Content?.ToLowerInvariant() ?? string.Empty;
                    return queryWords.Count(token =>
                        token.Length >= MinTokenLength &&
                        contentLower.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
                })
                .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                .ThenBy(c => c.ChunkIndex)
                .ToList();
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

