#nullable enable

using Microsoft.Extensions.Logging;
using SmartRAG.Entities;
using SmartRAG.Helpers;
using SmartRAG.Interfaces.Document;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="logger">Logger instance</param>
        public DocumentSearchStrategyService(
            IDocumentRepository documentRepository,
            IDocumentService documentService,
            IDocumentScoringService documentScoring,
            IQueryWordMatcherService queryWordMatcher,
            IDocumentRelevanceCalculatorService relevanceCalculator,
            IChunkPrioritizerService chunkPrioritizer,
            IQueryPatternAnalyzerService queryPatternAnalyzer,
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
        }

        /// <summary>
        /// Searches for relevant document chunks using repository's optimized search with keyword-based fallback
        /// </summary>
        public async Task<List<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults, SearchOptions? options = null, List<string>? queryTokens = null)
        {
            try
            {
                _logger.LogInformation("[QueryOperation] Chunk Search: DocumentRepository.SearchAsync (line 80) - Method: DocumentSearchStrategyService.SearchDocumentsAsync");
                var searchResults = await _documentRepository.SearchAsync(query, maxResults * InitialSearchMultiplier);

                if (searchResults.Count > 0)
                {
                    var filteredResults = searchResults;

                    if (options != null)
                    {
                        // Filter chunks by document type directly (more efficient than document-level filtering)
                        var beforeCount = searchResults.Count;
                        
                        // Count document types for debugging
                        var documentTypeCounts = searchResults
                            .GroupBy(c => c.DocumentType ?? "Document")
                            .ToDictionary(g => g.Key, g => g.Count());
                        
                        filteredResults = searchResults.Where(chunk =>
                        {
                            var documentType = chunk.DocumentType ?? "Document";
                            
                            // Check if this document type is enabled
                            if (documentType.Equals("Audio", StringComparison.OrdinalIgnoreCase))
                                return options.EnableAudioSearch;
                            
                            if (documentType.Equals("Image", StringComparison.OrdinalIgnoreCase))
                                return options.EnableImageSearch;
                            
                            // Default to Document type
                            return options.EnableDocumentSearch;
                        }).ToList();
                        
                        var afterCount = filteredResults.Count;
                        
                        _logger.LogInformation("📊 Document Type Filter Applied: {Filtered}/{Total} results kept → Documents: {Doc}, Audio: {Audio}, Image: {Image}",
                            afterCount, beforeCount, 
                            documentTypeCounts.GetValueOrDefault("Document", 0), 
                            documentTypeCounts.GetValueOrDefault("Audio", 0), 
                            documentTypeCounts.GetValueOrDefault("Image", 0));
                        
                        if (afterCount == 0 && beforeCount > 0)
                        {
                            _logger.LogWarning("All search results were filtered out by document type. This may indicate a filtering issue.");
                        }
                    }

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

            var allDocuments = await _documentService.GetAllDocumentsFilteredAsync(options);
            var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();

            var queryWords = queryTokens ?? QueryTokenizer.TokenizeQuery(query);
            var potentialNames = QueryTokenizer.ExtractPotentialNames(query);

            var scoredChunks = _documentScoring.ScoreChunks(allChunks, query, queryWords, potentialNames);

            var queryWordDocumentMap = _queryWordMatcher.MapQueryWordsToDocuments(
                queryWords,
                allDocuments,
                scoredChunks,
                ChunksToCheckForKeywords);

            var documentScores = _relevanceCalculator.CalculateDocumentScores(
                allDocuments,
                scoredChunks,
                queryWords,
                queryWordDocumentMap,
                TopChunksPerDocument);

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

            if (_queryPatternAnalyzer.RequiresComprehensiveSearch(query))
            {
                var comprehensiveQueryWords = QueryTokenizer.TokenizeQuery(query);
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

            return prioritizedChunks.Take(maxResults).ToList();
        }
    }
}

