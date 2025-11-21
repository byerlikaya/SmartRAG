#nullable enable

using Microsoft.Extensions.Logging;
using SmartRAG.Entities;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Search;
using SmartRAG.Services.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        #endregion

        #region Fields

        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<ContextExpansionService> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the ContextExpansionService
        /// </summary>
        /// <param name="documentRepository">Repository for document operations</param>
        /// <param name="logger">Logger instance</param>
        public ContextExpansionService(
            IDocumentRepository documentRepository,
            ILogger<ContextExpansionService> logger)
        {
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            // Limit context window to prevent excessive expansion
            var effectiveWindow = Math.Min(Math.Max(1, contextWindow), MaxContextWindow);

            try
            {
                // Group chunks by document ID
                var chunksByDocument = chunks.GroupBy(c => c.DocumentId).ToList();

                var expandedChunks = new HashSet<DocumentChunk>(new DocumentChunkComparer());

                // Process each document separately
                foreach (var documentGroup in chunksByDocument)
                {
                    var documentId = documentGroup.Key;
                    var documentChunks = documentGroup.ToList();

                    // Get the full document to access all chunks
                    var document = await _documentRepository.GetByIdAsync(documentId);
                    if (document == null || document.Chunks == null || document.Chunks.Count == 0)
                    {
                        // Document not found or has no chunks, add original chunks
                        foreach (var chunk in documentChunks)
                        {
                            expandedChunks.Add(chunk);
                        }
                        continue;
                    }

                    // Sort document chunks by ChunkIndex
                    var sortedDocumentChunks = document.Chunks.OrderBy(c => c.ChunkIndex).ToList();

                    // For each found chunk, add adjacent chunks
                    foreach (var foundChunk in documentChunks)
                    {
                        // Add the found chunk itself
                        expandedChunks.Add(foundChunk);

                        // Find the index of this chunk in the sorted list
                        var chunkIndex = sortedDocumentChunks.FindIndex(c => c.Id == foundChunk.Id);
                        if (chunkIndex < 0)
                        {
                            // Chunk not found in document, skip expansion
                            continue;
                        }

                        // Add previous chunks (context before)
                        var startIndex = Math.Max(0, chunkIndex - effectiveWindow);
                        for (int i = startIndex; i < chunkIndex; i++)
                        {
                            expandedChunks.Add(sortedDocumentChunks[i]);
                        }

                        // Add next chunks (context after)
                        var endIndex = Math.Min(sortedDocumentChunks.Count - 1, chunkIndex + effectiveWindow);
                        for (int i = chunkIndex + 1; i <= endIndex; i++)
                        {
                            expandedChunks.Add(sortedDocumentChunks[i]);
                        }
                    }
                }

                // Convert to list and sort by document ID and chunk index for consistent ordering
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
                // Return original chunks if expansion fails
                return chunks;
            }
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

