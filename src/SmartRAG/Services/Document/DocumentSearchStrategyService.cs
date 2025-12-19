#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Helpers;
using SmartRAG.Interfaces.Document;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Services.Document
{
    /// <summary>
    /// Service for executing document search strategies
    /// </summary>
    public class DocumentSearchStrategyService : IDocumentSearchStrategyService
    {
        private const int InitialSearchMultiplier = 2;
        private const int MinWordCountThreshold = 0;
        private const int MinPotentialNamesCount = 2;
        private const int MinNameChunksCount = 0;
        private const double NumberedListBonusPerItem = 100.0;
        private const double NumberedListWordMatchBonus = 10.0;
        private const int TopChunksPerDocument = 5;
        private const int ChunksToCheckForKeywords = 30;
        private const int CandidateMultiplier = 20;
        private const int CandidateMinCount = 200;
        private const double DocumentScoreThreshold = 0.8;
        private const double DocumentRelevanceBoost = 200.0;

        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentService _documentService;
        private readonly IDocumentScoringService _documentScoring;
        private readonly IQueryWordMatcherService _queryWordMatcher;
        private readonly IDocumentRelevanceCalculatorService _relevanceCalculator;
        private readonly IChunkPrioritizerService _chunkPrioritizer;
        private readonly IQueryPatternAnalyzerService _queryPatternAnalyzer;
        private readonly ILogger<DocumentSearchStrategyService> _logger;
        private readonly SearchOptions _defaultSearchOptions;

        /// <summary>
        /// Initializes a new instance of the DocumentSearchStrategyService
        /// </summary>
        /// <param name="documentRepository">Repository for document operations</param>
        /// <param name="documentService">Service for document operations</param>
        /// <param name="documentScoring">Service for scoring documents</param>
        /// <param name="queryWordMatcher">Service for matching query words</param>
        /// <param name="relevanceCalculator">Service for calculating document relevance</param>
        /// <param name="chunkPrioritizer">Service for prioritizing chunks</param>
        /// <param name="queryPatternAnalyzer">Service for analyzing query patterns</param>
        /// <param name="options">SmartRAG configuration options</param>
        /// <param name="logger">Logger instance</param>
        public DocumentSearchStrategyService(
            IDocumentRepository documentRepository,
            IDocumentService documentService,
            IDocumentScoringService documentScoring,
            IQueryWordMatcherService queryWordMatcher,
            IDocumentRelevanceCalculatorService relevanceCalculator,
            IChunkPrioritizerService chunkPrioritizer,
            IQueryPatternAnalyzerService queryPatternAnalyzer,
            IOptions<SmartRagOptions> options,
            ILogger<DocumentSearchStrategyService> logger)
        {
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
            _documentScoring = documentScoring ?? throw new ArgumentNullException(nameof(documentScoring));
            _queryWordMatcher = queryWordMatcher ?? throw new ArgumentNullException(nameof(queryWordMatcher));
            _relevanceCalculator = relevanceCalculator ?? throw new ArgumentNullException(nameof(relevanceCalculator));
            _chunkPrioritizer = chunkPrioritizer ?? throw new ArgumentNullException(nameof(chunkPrioritizer));
            _queryPatternAnalyzer = queryPatternAnalyzer ?? throw new ArgumentNullException(nameof(queryPatternAnalyzer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            var smartRagOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _defaultSearchOptions = SearchOptions.FromConfig(smartRagOptions);
        }

        /// <summary>
        /// Searches for relevant document chunks using repository's optimized search with keyword-based fallback
        /// </summary>
        /// <param name="query">Search query string</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <param name="options">Optional search options to filter documents. If null, default options from configuration are used.</param>
        /// <param name="queryTokens">Pre-computed query tokens for performance</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>List of relevant document chunks</returns>
        public async Task<List<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults, SearchOptions? options = null, List<string>? queryTokens = null, CancellationToken cancellationToken = default)
        {
            var searchOptions = options ?? _defaultSearchOptions;
            
            // Stage 1: Try fast repository search (vector similarity search)
            // This is the primary strategy - fast and efficient for semantic queries
            // If successful, return immediately. If not, fall back to keyword-based search.
            try
            {
                // Use repository directly. For Qdrant, repository's SearchAsync delegates to orchestration service internally.
                // For InMemory/Redis, repository handles search directly. No unnecessary wrapper needed.
                var searchResults = await _documentRepository.SearchAsync(query, maxResults * InitialSearchMultiplier);
                cancellationToken.ThrowIfCancellationRequested();

                if (searchResults.Count > 0)
                {
                    // Filter chunks by document type directly (more efficient than document-level filtering)
                    var filteredResults = searchResults.Where(chunk =>
                    {
                        var documentType = chunk.DocumentType ?? "Document";
                        
                        // Check if this document type is enabled
                        if (documentType.Equals("Audio", StringComparison.OrdinalIgnoreCase))
                            return searchOptions.EnableAudioSearch;
                        
                        if (documentType.Equals("Image", StringComparison.OrdinalIgnoreCase))
                            return searchOptions.EnableImageSearch;
                        
                        // Default to Document type
                        return searchOptions.EnableDocumentSearch;
                    }).ToList();

                    return filteredResults
                        .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                        .ThenBy(c => c.ChunkIndex)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Repository search failed, falling back to keyword scoring");
            }

            // Stage 2: Keyword-based fallback strategy
            // Used when repository search fails or returns no results
            // This strategy loads all documents and uses sophisticated scoring algorithms
            
            cancellationToken.ThrowIfCancellationRequested();
            var allDocuments = await _documentService.GetAllDocumentsFilteredAsync(searchOptions);
            var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();

            var queryWords = queryTokens ?? QueryTokenizer.TokenizeQuery(query);
            var potentialNames = QueryTokenizer.ExtractPotentialNames(query);

            // Score all chunks based on word matching and potential name matching
            var scoredChunks = _documentScoring.ScoreChunks(allChunks, query, queryWords, potentialNames);

            // Map query words to documents to identify which documents contain query terms
            // This helps prioritize documents that have multiple query word matches
            var queryWordDocumentMap = _queryWordMatcher.MapQueryWordsToDocuments(
                queryWords,
                allDocuments,
                scoredChunks,
                ChunksToCheckForKeywords);

            // Calculate document-level relevance scores
            // Documents with more query word matches and higher chunk scores get higher document scores
            var documentScores = _relevanceCalculator.CalculateDocumentScores(
                allDocuments,
                scoredChunks,
                queryWords,
                queryWordDocumentMap,
                TopChunksPerDocument);

            // Identify documents that meet the relevance threshold
            // This filters out documents with low overall relevance
            var relevantDocuments = _relevanceCalculator.IdentifyRelevantDocuments(
                documentScores,
                DocumentScoreThreshold);

            var relevantDocumentChunks = relevantDocuments
                .SelectMany(d => scoredChunks.Where(c => c.DocumentId == d.Id))
                .ToList();

            var otherDocumentChunks = allDocuments
                .Except(relevantDocuments)
                .SelectMany(d => scoredChunks.Where(c => c.DocumentId == d.Id))
                .ToList();

            // Apply document-level boost to chunks from relevant documents
            // This ensures chunks from highly relevant documents rank higher than chunks from less relevant documents
            // Boost value (200.0) is significant to create clear separation between relevant and irrelevant documents
            var relevantDocumentIds = new HashSet<Guid>(relevantDocuments.Select(d => d.Id));
            _relevanceCalculator.ApplyDocumentBoost(
                relevantDocumentChunks,
                relevantDocumentIds,
                DocumentRelevanceBoost);

            var finalScoredChunks = relevantDocumentChunks.Concat(otherDocumentChunks).ToList();

            var relevantChunks = finalScoredChunks
                .Where(c => c.RelevanceScore > MinWordCountThreshold)
                .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                .ThenBy(c => c.ChunkIndex)
                .Take(Math.Max(maxResults * CandidateMultiplier, CandidateMinCount))
                .ToList();

            // Special handling for queries containing potential names (person names, company names, etc.)
            // When query contains 2+ potential names, prioritize chunks that contain those names
            // This improves accuracy for queries like "John Smith salary" or "Microsoft revenue"
            if (potentialNames.Count >= MinPotentialNamesCount)
            {
                // Pre-compute lowercase names once to avoid repeated conversions in the loop
                var lowerNames = potentialNames.Select(n => n.ToLowerInvariant()).ToList();

                var nameChunks = relevantChunks.Where(c =>
                {
                    var contentLower = c.Content.ToLowerInvariant();
                    return lowerNames.Any(name => contentLower.Contains(name));
                }).ToList();

                if (nameChunks.Count > MinNameChunksCount)
                {
                    // Prioritize chunk 0 (document introduction/header) if it contains the name
                    // This ensures document context is preserved when returning name-specific results
                    var chunk0 = nameChunks.FirstOrDefault(c => c.ChunkIndex == 0);
                    var otherChunks = nameChunks.Where(c => c.ChunkIndex != 0).Take(maxResults - (chunk0 != null ? 1 : 0)).ToList();

                    if (chunk0 != null)
                    {
                        return new List<DocumentChunk> { chunk0 }.Concat(otherChunks).ToList();
                    }

                    return otherChunks;
                }
            }

            var prioritizedChunks = _chunkPrioritizer.PrioritizeChunksByRelevanceScore(relevantChunks);

            // Special handling for comprehensive queries (e.g., "list all products", "show all features")
            // These queries benefit from numbered lists which often contain complete information
            if (_queryPatternAnalyzer.RequiresComprehensiveSearch(query))
            {
                var comprehensiveQueryWords = QueryTokenizer.TokenizeQuery(query);
                
                // Score chunks containing numbered lists with significant bonuses
                // Each numbered list item gets +100.0 bonus, and query word matches get +10.0 bonus
                // This ensures numbered lists rank higher for comprehensive queries
                var numberedListChunks = _queryPatternAnalyzer.ScoreChunksByNumberedLists(
                    prioritizedChunks,
                    comprehensiveQueryWords,
                    NumberedListBonusPerItem,
                    NumberedListWordMatchBonus);

                var topNumberedChunks = numberedListChunks
                    .Where(c => _queryPatternAnalyzer.DetectNumberedLists(c.Content))
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .ThenByDescending(c => _queryPatternAnalyzer.CountNumberedListItems(c.Content))
                    .Take(maxResults * 2)
                    .ToList();

                if (topNumberedChunks.Count > 0)
                {
                    // Preserve chunk 0 (document introduction) if available
                    // This maintains document context while prioritizing numbered list content
                    var chunk0 = topNumberedChunks.FirstOrDefault(c => c.ChunkIndex == 0)
                        ?? prioritizedChunks.FirstOrDefault(c => c.ChunkIndex == 0);
                    var otherChunks = topNumberedChunks
                        .Where(c => c.ChunkIndex != 0)
                        .Concat(prioritizedChunks.Except(topNumberedChunks).Where(c => c.ChunkIndex != 0))
                        .Take(maxResults - (chunk0 != null ? 1 : 0))
                        .ToList();

                    return _chunkPrioritizer.MergeChunksWithPreservedChunk0(otherChunks, chunk0);
                }
            }

            // Default: Return top chunks by relevance score
            return prioritizedChunks.Take(maxResults).ToList();
        }
    }
}

