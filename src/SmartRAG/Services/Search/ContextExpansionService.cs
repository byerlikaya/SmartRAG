#nullable enable

using Microsoft.Extensions.Logging;
using SmartRAG.Entities;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Search;
using SmartRAG.Services.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IQueryPatternAnalyzerService = SmartRAG.Interfaces.Document.IQueryPatternAnalyzerService;

namespace SmartRAG.Services.Search
{
    /// <summary>
    /// Service for expanding document chunk context by including adjacent chunks
    /// </summary>
    public class ContextExpansionService : IContextExpansionService
    {
        #region Constants

        private const int DefaultContextWindow = 2;
        private const int MaxContextWindow = 5;
        private const int ComprehensiveWindow = 8;
        private const int SmallChunkCountThreshold = 3;
        private const int SmallChunkWindow = 3;

        #endregion

        #region Fields

        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<ContextExpansionService> _logger;
        private readonly IQueryPatternAnalyzerService? _queryPatternAnalyzer;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the ContextExpansionService
        /// </summary>
        /// <param name="documentRepository">Repository for document operations</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="queryPatternAnalyzer">Optional service for analyzing query patterns</param>
        public ContextExpansionService(
            IDocumentRepository documentRepository,
            ILogger<ContextExpansionService> logger,
            IQueryPatternAnalyzerService? queryPatternAnalyzer = null)
        {
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queryPatternAnalyzer = queryPatternAnalyzer;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Expands context by including adjacent chunks from the same document
        /// </summary>
        /// <param name="chunks">Initial chunks found by search</param>
        /// <param name="contextWindow">Number of adjacent chunks to include before and after each found chunk</param>
        /// <returns>Expanded list of chunks with context</returns>
        public async Task<List<DocumentChunk>> ExpandContextAsync(List<DocumentChunk> chunks, int contextWindow = DefaultContextWindow)
        {
            if (chunks == null || chunks.Count == 0)
            {
                return new List<DocumentChunk>();
            }

            var effectiveWindow = Math.Min(Math.Max(1, contextWindow), MaxContextWindow);

            try
            {
                var chunksByDocument = chunks.GroupBy(c => c.DocumentId).ToList();

                var expandedChunks = new HashSet<DocumentChunk>(new DocumentChunkComparer());

                foreach (var documentGroup in chunksByDocument)
                {
                    var documentId = documentGroup.Key;
                    var documentChunks = documentGroup.ToList();

                    var document = await _documentRepository.GetByIdAsync(documentId);
                    if (document == null || document.Chunks == null || document.Chunks.Count == 0)
                    {
                        foreach (var chunk in documentChunks)
                        {
                            expandedChunks.Add(chunk);
                        }
                        continue;
                    }

                    var sortedDocumentChunks = document.Chunks.OrderBy(c => c.ChunkIndex).ToList();

                    foreach (var foundChunk in documentChunks)
                    {
                        expandedChunks.Add(foundChunk);

                        var chunkIndex = sortedDocumentChunks.FindIndex(c => c.Id == foundChunk.Id);
                        if (chunkIndex < 0)
                        {
                            continue;
                        }

                        var startIndex = Math.Max(0, chunkIndex - effectiveWindow);
                        for (int i = startIndex; i < chunkIndex; i++)
                        {
                            expandedChunks.Add(sortedDocumentChunks[i]);
                        }

                        var endIndex = Math.Min(sortedDocumentChunks.Count - 1, chunkIndex + effectiveWindow);
                        for (int i = chunkIndex + 1; i <= endIndex; i++)
                        {
                            expandedChunks.Add(sortedDocumentChunks[i]);
                        }
                    }
                }

                var result = expandedChunks.ToList();
                result = result
                    .OrderBy(c => c.DocumentId)
                    .ThenBy(c => c.ChunkIndex)
                    .ToList();

                ServiceLogMessages.LogContextExpansionCompleted(_logger, chunks.Count, result.Count, null);

                return result;
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogContextExpansionError(_logger, ex);
                return chunks;
            }
        }

        /// <summary>
        /// Determines appropriate context window based on query structure using language-agnostic pattern detection
        /// </summary>
        /// <param name="chunks">List of document chunks</param>
        /// <param name="query">User query</param>
        /// <returns>Context window size (number of adjacent chunks to include)</returns>
        public int DetermineContextWindow(List<DocumentChunk> chunks, string query)
        {
            if (_queryPatternAnalyzer != null && _queryPatternAnalyzer.RequiresComprehensiveSearch(query))
            {
                return ComprehensiveWindow;
            }

            if (chunks != null && chunks.Count <= SmallChunkCountThreshold)
            {
                return SmallChunkWindow;
            }

            return DefaultContextWindow;
        }

        /// <summary>
        /// Builds context string from chunks with size limit to prevent timeout
        /// </summary>
        /// <param name="chunks">List of document chunks to build context from</param>
        /// <param name="maxContextSize">Maximum context size in characters</param>
        /// <returns>Context string built from chunks</returns>
        public string BuildLimitedContext(List<DocumentChunk> chunks, int maxContextSize)
        {
            if (chunks == null || chunks.Count == 0)
            {
                return string.Empty;
            }

            var sortedChunks = chunks
                .OrderByDescending(c => c.ChunkIndex == 0)
                .ThenBy(c => c.ChunkIndex)
                .ToList();

            var chunk0Included = sortedChunks.Any(c => c.ChunkIndex == 0);
            if (chunk0Included)
            {
                var chunk0 = sortedChunks.First(c => c.ChunkIndex == 0);
                _logger.LogDebug("Chunk 0 included in context (first position). Content preview: {Preview}",
                    chunk0.Content?[..Math.Min(200, chunk0.Content?.Length ?? 0)] ?? "empty");
            }
            else
            {
                _logger.LogWarning("Chunk 0 NOT found in chunks list! Total chunks: {Count}", sortedChunks.Count);
            }

            var contextBuilder = new StringBuilder();
            var totalSize = 0;

            foreach (var chunk in sortedChunks)
            {
                if (chunk?.Content == null)
                {
                    continue;
                }

                var chunkSize = chunk.Content.Length;
                const int separatorSize = 2;

                if (totalSize + chunkSize + separatorSize > maxContextSize)
                {
                    var remainingSize = maxContextSize - totalSize - separatorSize;
                    if (remainingSize > 100)
                    {
                        var partialContent = chunk.Content[..Math.Min(remainingSize, chunk.Content.Length)];
                        if (contextBuilder.Length > 0)
                        {
                            contextBuilder.Append("\n\n");
                        }
                        contextBuilder.Append(partialContent);
                        ServiceLogMessages.LogContextSizeLimited(_logger, chunks.Count, totalSize + partialContent.Length, maxContextSize, null);
                    }
                    break;
                }

                if (contextBuilder.Length > 0)
                {
                    contextBuilder.Append("\n\n");
                    totalSize += separatorSize;
                }

                contextBuilder.Append(chunk.Content);
                totalSize += chunkSize;
            }

            return contextBuilder.ToString();
        }

        #endregion

        #region Private Helper Classes

        /// <summary>
        /// Comparer for DocumentChunk to use in HashSet
        /// </summary>
        private class DocumentChunkComparer : IEqualityComparer<DocumentChunk>
        {
            public bool Equals(DocumentChunk? x, DocumentChunk? y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x == null || y == null) return false;
                return x.Id == y.Id;
            }

            public int GetHashCode(DocumentChunk obj)
            {
                return obj.Id.GetHashCode();
            }
        }

        #endregion
    }
}

