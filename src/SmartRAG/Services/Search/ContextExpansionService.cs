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

        private const int DefaultContextWindow = 3; // Balanced window for good context coverage
        private const int MaxContextWindow = 15; // Maximum context window size for comprehensive searches
        private const int ComprehensiveWindow = 8; // Balanced window for comprehensive searches
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
                        _logger.LogDebug(
                            "Document {DocumentId} not found or has no chunks (Chunks: {ChunkCount}), skipping expansion",
                            documentId, document?.Chunks?.Count ?? 0);
                        foreach (var chunk in documentChunks)
                        {
                            expandedChunks.Add(chunk);
                        }
                        continue;
                    }

                    var sortedDocumentChunks = document.Chunks.OrderBy(c => c.ChunkIndex).ToList();
                    _logger.LogDebug(
                        "Expanding context for document {DocumentId}: {FoundChunks} found chunks, {TotalChunks} total chunks in document, window: {Window}",
                        documentId, documentChunks.Count, sortedDocumentChunks.Count, effectiveWindow);

                    foreach (var foundChunk in documentChunks)
                    {
                        expandedChunks.Add(foundChunk);

                        var chunkIndex = sortedDocumentChunks.FindIndex(c => c.Id == foundChunk.Id);
                        if (chunkIndex < 0)
                        {
                            _logger.LogDebug(
                                "Chunk {ChunkId} (Index: {ChunkIndex}) not found in document chunks, skipping expansion",
                                foundChunk.Id, foundChunk.ChunkIndex);
                            continue;
                        }

                        // Special handling for Chunk 0 in image documents
                        // Image OCR often separates headers/labels from values across many chunks
                        // If Chunk 0 is short and lacks numeric values, it's likely a header chunk needing broader context
                        var adjustedWindow = effectiveWindow;
                        if (foundChunk.ChunkIndex == 0 && 
                            foundChunk.DocumentType == "Image" && 
                            foundChunk.Content != null &&
                            foundChunk.Content.Length < 500 && 
                            !ContainsNumericValues(foundChunk.Content))
                        {
                            // Expand window significantly for image header chunks to capture associated values
                            // This helps connect item names with their prices/values that may be many chunks away
                            adjustedWindow = Math.Min(40, sortedDocumentChunks.Count);
                            _logger.LogDebug(
                                "Chunk 0 detected as image header chunk (length: {Length}, no numeric values), expanding window from {OldWindow} to {NewWindow}",
                                foundChunk.Content.Length, effectiveWindow, adjustedWindow);
                        }

                        var startIndex = Math.Max(0, chunkIndex - adjustedWindow);
                        var endIndex = Math.Min(sortedDocumentChunks.Count - 1, chunkIndex + adjustedWindow);
                        var addedBefore = 0;
                        var addedAfter = 0;

                        for (int i = startIndex; i < chunkIndex; i++)
                        {
                            expandedChunks.Add(sortedDocumentChunks[i]);
                            addedBefore++;
                        }

                        for (int i = chunkIndex + 1; i <= endIndex; i++)
                        {
                            expandedChunks.Add(sortedDocumentChunks[i]);
                            addedAfter++;
                        }

                        _logger.LogDebug(
                            "Expanded Chunk {ChunkIndex}: added {Before} chunks before, {After} chunks after (range: {StartIndex}-{EndIndex})",
                            foundChunk.ChunkIndex, addedBefore, addedAfter, startIndex, endIndex);
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

            // Use wider context window if there are few chunks (to ensure comprehensive coverage)
            // No document type prioritization - all document types are treated equally
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

            // DO NOT re-sort chunks here - they are already correctly sorted by DocumentSearchService
            // Sorting logic in DocumentSearchService:
            // 1. Original chunks (from initial search) are prioritized
            // 2. Chunk 0 (document header) is prioritized
            // 3. Then by relevance score
            // Re-sorting here would break this carefully constructed order
            var sortedChunks = chunks;

            // Log top chunk information (sorted by relevance score)
            if (sortedChunks.Count > 0)
            {
                var topChunk = sortedChunks[0];
                var chunk0Included = sortedChunks.Any(c => c.ChunkIndex == 0);
                var chunk0Position = chunk0Included ? sortedChunks.FindIndex(c => c.ChunkIndex == 0) : -1;
                
                _logger.LogDebug(
                    "Top chunk in context: Chunk {ChunkIndex} (Type: {DocumentType}, Score: {Score:F4}, Position: 0/{Total}). Chunk 0 position: {Chunk0Position}",
                    topChunk.ChunkIndex,
                    topChunk.DocumentType ?? "Document",
                    topChunk.RelevanceScore ?? 0.0,
                    sortedChunks.Count,
                    chunk0Position >= 0 ? chunk0Position.ToString() : "not found");
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

        /// <summary>
        /// Checks if content contains numeric values (prices, quantities, etc.)
        /// Used to detect header-only chunks that need broader context
        /// </summary>
        private static bool ContainsNumericValues(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            // Check for common value patterns: numbers with currency symbols, percentages, or standalone numbers
            // This is language-agnostic and works for all currency symbols
            foreach (var c in content)
            {
                if (char.IsDigit(c))
                {
                    // Found a digit - check surrounding context for value patterns
                    var index = content.IndexOf(c);
                    if (index > 0)
                    {
                        var prev = content[index - 1];
                        // Currency symbols, percentage, decimal point indicate numeric value
                        if (prev == '$' || prev == '€' || prev == '£' || prev == '¥' || prev == '₺' || 
                            prev == '%' || prev == '.' || prev == ',')
                            return true;
                    }
                    if (index < content.Length - 1)
                    {
                        var next = content[index + 1];
                        // Currency symbols, percentage after number
                        if (next == '$' || next == '€' || next == '£' || next == '¥' || next == '₺' || 
                            next == '%' || next == '.' || next == ',')
                            return true;
                    }
                    // Standalone multi-digit number (likely quantity or price without symbol)
                    var digitCount = 0;
                    for (int i = index; i < content.Length && char.IsDigit(content[i]); i++)
                        digitCount++;
                    if (digitCount >= 2) // 2+ digits = likely a value
                        return true;
                }
            }

            return false;
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

