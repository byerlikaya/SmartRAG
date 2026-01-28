#nullable enable

using Microsoft.Extensions.Logging;
using SmartRAG.Entities;
using SmartRAG.Helpers;
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
        private const int DefaultContextWindow = 3;
        private const int MaxContextWindow = 15;
        private const int ComprehensiveWindow = 8;
        private const int NumericQueryWindow = 10;
        private const int SmallChunkCountThreshold = 3;
        private const int SmallChunkWindow = 3;

        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<ContextExpansionService> _logger;
        private readonly IQueryPatternAnalyzerService? _queryPatternAnalyzer;

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

                        var adjustedWindow = effectiveWindow;
                        if (foundChunk.ChunkIndex == 0 && 
                            foundChunk.DocumentType == "Image" && 
                            foundChunk.Content != null &&
                            foundChunk.Content.Length < 500 && 
                            !ContainsNumericValues(foundChunk.Content))
                        {
                            adjustedWindow = Math.Min(40, sortedDocumentChunks.Count);
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
                    }
                }

                var result = expandedChunks.ToList();
                result = result
                    .OrderBy(c => c.DocumentId)
                    .ThenBy(c => c.ChunkIndex)
                    .ToList();

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

            if (RequiresNumericContext(query))
            {
                return NumericQueryWindow;
            }

            if (chunks != null && chunks.Count <= SmallChunkCountThreshold)
            {
                return SmallChunkWindow;
            }

            return DefaultContextWindow;
        }

        /// <summary>
        /// Detects if query asks for numeric values, percentages, or specific numbers
        /// Uses language-agnostic pattern detection based on structural patterns and Unicode characters
        /// Works for all languages without hardcoding specific language terms
        /// </summary>
        private static bool RequiresNumericContext(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return false;

            var queryLower = query.ToLowerInvariant();

            if (query.Contains('%'))
                return true;

            if (System.Text.RegularExpressions.Regex.IsMatch(query, @"\d+\s*%|\d+\s+[a-z]{2,}"))
            {
                var words = queryLower.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (word.Length >= 4 && word.Length <= 12 && 
                        word.Any(char.IsLetter) && 
                        (word.Any(char.IsDigit) || System.Text.RegularExpressions.Regex.IsMatch(word, @"^[a-z]{4,}$")))
                    {
                        if (ContainsQuestionIndicators(query))
                            return true;
                    }
                }
            }

            if (ContainsQuestionIndicators(query))
            {
                var hasNumericContext = 
                    query.Any(char.IsDigit) ||
                    query.Contains('.') || query.Contains(',') ||
                    query.Contains('+') || query.Contains('-') || query.Contains('×') || query.Contains('*') ||
                    query.Contains('>') || query.Contains('<') || query.Contains('=');

                if (hasNumericContext)
                    return true;


                var hasNumericQuestionPattern = System.Text.RegularExpressions.Regex.IsMatch(queryLower,
                    @"\b\p{L}{2,5}\s+\p{L}{3,}\b",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (hasNumericQuestionPattern && QueryTokenizer.ContainsNumericIndicators(query))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Detects question indicators using language-agnostic structural patterns
        /// Works for all languages by detecting question marks, question word patterns, and sentence structure
        /// </summary>
        private static bool ContainsQuestionIndicators(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return false;

            if (query.Contains('?') || query.Contains('？'))
                return true;

            var trimmedQuery = query.TrimStart();
            if (trimmedQuery.Length > 0)
            {
                var firstWord = trimmedQuery.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrEmpty(firstWord))
                {
                    var firstWordLower = firstWord.ToLowerInvariant();
                    if (firstWordLower.Length >= 2 && firstWordLower.Length <= 5)
                    {
                        if (firstWordLower.All(char.IsLetter))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
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

            var sortedChunks = chunks;

                if (sortedChunks.Count > 0)
                {
                    var topChunk = sortedChunks[0];
                    var chunk0Included = sortedChunks.Any(c => c.ChunkIndex == 0);
                    var chunk0Position = chunk0Included ? sortedChunks.FindIndex(c => c.ChunkIndex == 0) : -1;
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

            foreach (var c in content)
            {
                if (char.IsDigit(c))
                {
                    // Found a digit - check surrounding context for value patterns
                    var index = content.IndexOf(c);
                    if (index > 0)
                    {
                        var prev = content[index - 1];
                        if (prev == '$' || prev == '€' || prev == '£' || prev == '¥' || prev == '₺' || 
                            prev == '%' || prev == '.' || prev == ',')
                            return true;
                    }
                    if (index < content.Length - 1)
                    {
                        var next = content[index + 1];
                        if (next == '$' || next == '€' || next == '£' || next == '¥' || next == '₺' || 
                            next == '%' || next == '.' || next == ',')
                            return true;
                    }
                    var digitCount = 0;
                    for (int i = index; i < content.Length && char.IsDigit(content[i]); i++)
                        digitCount++;
                    if (digitCount >= 2)
                        return true;
                }
            }

            return false;
        }

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
    }
}

