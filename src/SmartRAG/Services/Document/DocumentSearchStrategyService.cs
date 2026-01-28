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
        // Adaptive threshold strategy: Start with industry standard, fallback to lower if no results
        // Redis returns 0-100 scale (similarity * 100), Qdrant returns 0-1 scale (cosine similarity) - normalize to 0-100
        private const double PreferredVectorSearchThreshold = 50.0; // 0.5 similarity (preferred, industry standard)
        private const double FallbackVectorSearchThreshold = 30.0; // 0.3 similarity (fallback if no results with preferred)
        private const double MinTextSearchRelevanceThreshold = 3.0; // Text search uses different scale (4.0-6.0+)
        private const double MinChunk0RelevanceThreshold = 25.0; // Lower threshold for document introduction chunks
        private const int ReciprocalRankFusionK = 60; // RRF k parameter (industry standard: 50-60, R2R uses 50)
        private const double KeywordSearchWeight = 1.0; // Weight for keyword search results (R2R standard)
        private const double VectorSearchWeight = 5.0; // Weight for vector search results (R2R standard)

        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentSearchStrategyService> _logger;
        private readonly SearchOptions _defaultSearchOptions;

        /// <summary>
        /// Initializes a new instance of the DocumentSearchStrategyService
        /// </summary>
        /// <param name="documentRepository">Repository for document operations</param>
        /// <param name="documentService">Service for document operations</param>
        /// <param name="options">SmartRAG configuration options</param>
        /// <param name="logger">Logger instance</param>
        public DocumentSearchStrategyService(
            IDocumentRepository documentRepository,
            IDocumentService documentService,
            IOptions<SmartRagOptions> options,
            ILogger<DocumentSearchStrategyService> logger)
        {
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
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
            
            var requiresNumericContext = RequiresNumericContext(query);
            var potentialNames = QueryTokenizer.ExtractPotentialNames(query);
            var hasNameQuery = potentialNames.Count >= 2;
            var isVagueQuery = IsVagueQuery(query, hasNameQuery, requiresNumericContext);
            var queryWords = queryTokens ?? QueryTokenizer.TokenizeQuery(query);
            var queryWordsLower = queryWords.Select(w => w.ToLowerInvariant()).ToList();
            
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
                    // Normalize scores to 0-100 scale for consistent threshold comparison
                    // Redis returns 0-100 (similarity * 100), Qdrant returns 0-1 (cosine similarity) - normalize Qdrant to 0-100
                    var normalizedResults = searchResults.Select(chunk =>
                    {
                        var score = chunk.RelevanceScore ?? 0.0;
                        // If score is <= 1.0, it's likely Qdrant (0-1 scale), normalize to 0-100
                        if (score <= 1.0 && score > 0)
                        {
                            chunk.RelevanceScore = score * 100.0;
                        }
                        return chunk;
                    }).ToList();
                    
                    // Determine search type based on top score (vector vs text search)
                    var topScore = normalizedResults.FirstOrDefault()?.RelevanceScore ?? 0.0;
                    var isTextSearch = topScore > 300.0; // Text search uses 300+ scale
                    
                    // Filter chunks by document type first
                    var typeFilteredResults = normalizedResults.Where(chunk =>
                    {
                        var documentType = chunk.DocumentType ?? "Document";
                        return documentType.Equals("Audio", StringComparison.OrdinalIgnoreCase)
                            ? searchOptions.EnableAudioSearch
                            : documentType.Equals("Image", StringComparison.OrdinalIgnoreCase)
                                ? searchOptions.EnableImageSearch
                                : searchOptions.EnableDocumentSearch;
                    }).ToList();
                    
                    if (typeFilteredResults.Count == 0)
                    {
                        return new List<DocumentChunk>();
                    }
                    
                    // Adaptive threshold strategy: Try preferred threshold first, fallback if no results
                    var minRelevanceThreshold = isTextSearch ? MinTextSearchRelevanceThreshold : PreferredVectorSearchThreshold;
                    
                    var filteredResults = typeFilteredResults.Where(chunk =>
                    {
                        var relevanceScore = chunk.RelevanceScore ?? 0.0;
                        return relevanceScore >= minRelevanceThreshold ||
                               (chunk.ChunkIndex == 0 && relevanceScore >= MinChunk0RelevanceThreshold);
                    }).ToList();

                    // If no results with preferred threshold, try fallback threshold (adaptive)
                    if (filteredResults.Count == 0 && !isTextSearch)
                    {
                        filteredResults = typeFilteredResults.Where(chunk =>
                        {
                            var relevanceScore = chunk.RelevanceScore ?? 0.0;
                            return relevanceScore >= FallbackVectorSearchThreshold ||
                                   (chunk.ChunkIndex == 0 && relevanceScore >= MinChunk0RelevanceThreshold);
                        }).ToList();
                    }
                    
                    // If still no results, return top-K by score (better than nothing)
                    if (filteredResults.Count == 0)
                    {
                        // Return top-K results sorted by score (adaptive fallback)
                        return typeFilteredResults
                            .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                            .ThenBy(c => c.ChunkIndex)
                            .Take(maxResults)
                            .ToList();
                    }

                    var finalResults = filteredResults
                        .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                        .ThenBy(c => c.ChunkIndex)
                        .Take(maxResults)
                        .ToList();

                    var highestScore = finalResults.FirstOrDefault()?.RelevanceScore ?? 0.0;
                    
                    if (finalResults.Count > 0)
                    {
                        var topChunkContent = finalResults.FirstOrDefault()?.Content?.ToLowerInvariant() ?? string.Empty;
                        var queryLower = query.ToLowerInvariant();
                        
                        var criticalPhrases = ExtractCriticalPhrases(queryLower, queryWordsLower);
                        var significantQueryWords = queryWordsLower.Where(w => w.Length >= 4).ToList();
                        
                        var shouldTriggerKeywordFallback = false;
                        var reason = string.Empty;
                        
                        if (criticalPhrases.Count > 0)
                        {
                            var criticalPhrasesInTopChunk = criticalPhrases.Count(phrase => topChunkContent.Contains(phrase));
                            var criticalPhraseMatchRatio = (double)criticalPhrasesInTopChunk / criticalPhrases.Count;
                            
                            if (criticalPhraseMatchRatio < 0.5)
                            {
                                shouldTriggerKeywordFallback = true;
                                reason = $"only {criticalPhrasesInTopChunk}/{criticalPhrases.Count} critical phrases";
                            }
                        }
                        else if (significantQueryWords.Count > 0)
                        {
                            var significantWordsInTopChunk = significantQueryWords.Count(word => topChunkContent.Contains(word));
                            var significantWordMatchRatio = (double)significantWordsInTopChunk / significantQueryWords.Count;
                            
                            if (significantWordMatchRatio < 0.5)
                            {
                                shouldTriggerKeywordFallback = true;
                                reason = $"only {significantWordsInTopChunk}/{significantQueryWords.Count} significant query words ({significantWordMatchRatio:P0})";
                            }
                        }
                        else
                        {
                            var hasQueryWordsInTopChunk = queryWordsLower.Any(word => topChunkContent.Contains(word));
                            
                            if (!hasQueryWordsInTopChunk)
                            {
                                shouldTriggerKeywordFallback = true;
                                reason = "no query words";
                            }
                        }
                        
                        if (shouldTriggerKeywordFallback)
                        {
                            var keywordResults = await PerformKeywordFallbackAsync(query, maxResults, searchOptions, queryTokens, cancellationToken);
                            if (keywordResults.Count > 0)
                            {
                                var combinedResults = CombineSearchResultsWithRRF(finalResults, keywordResults, maxResults);
                                return combinedResults;
                            }
                        }
                    }
                    
                    if (hasNameQuery && finalResults.Count > 0)
                    {
                        var keywordResults = await PerformKeywordFallbackAsync(query, maxResults, searchOptions, queryTokens, cancellationToken);
                        if (keywordResults.Count > 0)
                        {
                            var combinedResults = CombineSearchResultsWithRRF(finalResults, keywordResults, maxResults);
                            return combinedResults;
                        }
                    }
                    
                    if (requiresNumericContext && finalResults.Count > 0 && highestScore < PreferredVectorSearchThreshold)
                    {
                        var keywordResults = await PerformKeywordFallbackAsync(query, maxResults, searchOptions, queryTokens, cancellationToken);
                        if (keywordResults.Count > 0)
                        {
                            var combinedResults = CombineSearchResultsWithRRF(finalResults, keywordResults, maxResults);
                            return combinedResults;
                        }
                    }
                    
                    if (isVagueQuery && finalResults.Count > 0)
                    {
                        var keywordResults = await PerformKeywordFallbackAsync(query, maxResults, searchOptions, queryTokens, cancellationToken);
                        if (keywordResults.Count > 0)
                        {
                            var combinedResults = CombineSearchResultsWithRRF(finalResults, keywordResults, maxResults);
                            return combinedResults;
                        }
                    }

                    return finalResults;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Repository search failed, falling back to keyword scoring");
            }

            // Stage 2: Simple keyword-based fallback strategy
            // Only used when vector search completely fails or returns no results
            // Simple word matching - if vector search doesn't work, basic keyword search is better than nothing
            
            cancellationToken.ThrowIfCancellationRequested();
            var allDocuments = await _documentService.GetAllDocumentsFilteredAsync(searchOptions);
            var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();

            var potentialNamesLower = potentialNames.Select(n => n.ToLowerInvariant()).ToList();

            var matchingChunks = allChunks.Where(chunk =>
            {
                var contentLower = chunk.Content.ToLowerInvariant();
                var matchCount = queryWordsLower.Count(word => contentLower.Contains(word));
                
                if (potentialNamesLower.Count >= 2)
                {
                    var fullNameLower = string.Join(" ", potentialNamesLower);
                    var hasFullName = contentLower.Contains(fullNameLower);
                    var hasPartialName = potentialNamesLower.Any(name => contentLower.Contains(name));
                    
                    if (hasFullName || hasPartialName)
                    {
                        matchCount += potentialNamesLower.Count;
                    }
                }
                
                if (requiresNumericContext)
                {
                    var hasNumericValue = contentLower.Any(char.IsDigit);
                    return matchCount >= Math.Max(1, queryWordsLower.Count / 3) && hasNumericValue;
                }
                return matchCount >= Math.Max(1, queryWordsLower.Count / 2);
            }).ToList();

            if (matchingChunks.Count == 0)
            {
                return new List<DocumentChunk>();
            }

            var scoredChunks = matchingChunks.Select(chunk =>
            {
                var contentLower = chunk.Content.ToLowerInvariant();
                var matchCount = queryWordsLower.Count(word => contentLower.Contains(word));
                var score = matchCount * 10.0;
                
                if (potentialNamesLower.Count >= 2)
                {
                    var fullNameLower = string.Join(" ", potentialNamesLower);
                    if (contentLower.Contains(fullNameLower))
                    {
                        score += 50.0;
                    }
                    else if (potentialNamesLower.Any(name => contentLower.Contains(name)))
                    {
                        score += 25.0;
                    }
                }
                
                if (matchCount == queryWordsLower.Count)
                {
                    score += 20.0;
                }
                if (requiresNumericContext)
                {
                    var hasPercentage = contentLower.Contains('%');
                    var hasNumericValue = contentLower.Any(char.IsDigit);
                    if (hasPercentage)
                    {
                        score += 15.0;
                    }
                    else if (hasNumericValue)
                    {
                        score += 10.0;
                    }
                }
                chunk.RelevanceScore = score;
                return chunk;
            }).ToList();

            // Return top chunks by score
            return scoredChunks
                .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                .ThenBy(c => c.ChunkIndex)
                .Take(maxResults)
                .ToList();
        }

        private async Task<List<DocumentChunk>> PerformKeywordFallbackAsync(string query, int maxResults, SearchOptions searchOptions, List<string>? queryTokens, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var allDocuments = await _documentService.GetAllDocumentsFilteredAsync(searchOptions);
            
            var queryWords = queryTokens ?? QueryTokenizer.TokenizeQuery(query);
            var queryWordsLower = queryWords.Select(w => w.ToLowerInvariant()).ToList();
            var queryLower = query.ToLowerInvariant();
            var criticalPhrases = ExtractCriticalPhrases(queryLower, queryWordsLower);
            var potentialNames = QueryTokenizer.ExtractPotentialNames(query);
            var potentialNamesLower = potentialNames.Select(n => n.ToLowerInvariant()).ToList();
            

            var requiresNumericContext = RequiresNumericContext(query);
            
            var chunksWithContent = new List<DocumentChunk>();
            foreach (var document in allDocuments)
            {
                var fullDocument = await _documentService.GetDocumentAsync(document.Id);
                if (fullDocument != null && fullDocument.Chunks != null)
                {
                    chunksWithContent.AddRange(fullDocument.Chunks);
                }
            }

            var matchingChunks = chunksWithContent.Where(chunk =>
            {
                if (string.IsNullOrWhiteSpace(chunk.Content))
                    return false;
                    
                var contentLower = chunk.Content.ToLowerInvariant();
                
                var hasCriticalPhrase = criticalPhrases.Count > 0 && criticalPhrases.Any(phrase => contentLower.Contains(phrase));
                if (hasCriticalPhrase)
                {
                    return true;
                }
                
                var matchCount = queryWordsLower.Count(word => contentLower.Contains(word));
                
                var significantWords = queryWordsLower.Where(w => w.Length >= 4).ToList();
                if (significantWords.Count > 0)
                {
                    var significantMatchCount = significantWords.Count(word => contentLower.Contains(word));
                    if (significantMatchCount > 0)
                    {
                        matchCount = Math.Max(matchCount, significantMatchCount * 2);
                    }
                }
                
                if (potentialNamesLower.Count >= 2)
                {
                    var fullNameLower = string.Join(" ", potentialNamesLower);
                    var hasFullName = contentLower.Contains(fullNameLower);
                    var hasPartialName = potentialNamesLower.Any(name => contentLower.Contains(name));
                    
                    if (hasFullName || hasPartialName)
                    {
                        matchCount += potentialNamesLower.Count;
                    }
                }
                
                if (requiresNumericContext)
                {
                    var hasNumericValue = contentLower.Any(char.IsDigit);
                    return matchCount >= Math.Max(1, queryWordsLower.Count / 3) && hasNumericValue;
                }
                return matchCount >= Math.Max(1, queryWordsLower.Count / 3);
            }).ToList();

            if (matchingChunks.Count == 0)
            {
                return new List<DocumentChunk>();
            }

            var scoredChunks = matchingChunks.Select(chunk =>
            {
                var contentLower = chunk.Content.ToLowerInvariant();
                var score = 0.0;
                
                var criticalPhraseMatches = criticalPhrases.Count(phrase => contentLower.Contains(phrase));
                if (criticalPhraseMatches > 0)
                {
                    score += criticalPhraseMatches * 100.0;
                }
                
                var matchCount = queryWordsLower.Count(word => contentLower.Contains(word));
                
                var significantWords = queryWordsLower.Where(w => w.Length >= 4).ToList();
                var significantMatchCount = significantWords.Count > 0 
                    ? significantWords.Count(word => contentLower.Contains(word))
                    : 0;
                
                var totalMatchCount = Math.Max(matchCount, significantMatchCount * 2);
                score += totalMatchCount * 10.0;
                
                if (totalMatchCount == queryWordsLower.Count)
                {
                    score += 20.0;
                }
                
                if (significantMatchCount == significantWords.Count && significantWords.Count > 0)
                {
                    score += 30.0;
                }

                if (potentialNamesLower.Count >= 2)
                {
                    var fullNameLower = string.Join(" ", potentialNamesLower);
                    if (contentLower.Contains(fullNameLower))
                    {
                        score += 50.0;
                    }
                    else if (potentialNamesLower.Any(name => contentLower.Contains(name)))
                    {
                        score += 25.0;
                    }
                }

                if (requiresNumericContext)
                {
                    var hasPercentage = contentLower.Contains('%');
                    var hasNumericValue = contentLower.Any(char.IsDigit);
                    if (hasPercentage)
                    {
                        score += 15.0;
                    }
                    else if (hasNumericValue)
                    {
                        score += 10.0;
                    }
                }
                
                if (chunk.ChunkIndex == 0)
                {
                    score += 10.0;
                }
                
                chunk.RelevanceScore = score;
                return chunk;
            }).ToList();

            var topScoredChunks = scoredChunks
                .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                .ThenBy(c => c.ChunkIndex)
                .Take(maxResults)
                .ToList();

            return topScoredChunks;
        }

        private static bool RequiresNumericContext(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return false;

            if (query.Contains('%'))
                return true;

            if (System.Text.RegularExpressions.Regex.IsMatch(query, @"\d+\s*%|\d+\s+[a-z]{2,}"))
            {
                var words = query.ToLowerInvariant().Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
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


                var hasNumericQuestionPattern = System.Text.RegularExpressions.Regex.IsMatch(query.ToLowerInvariant(),
                    @"\b\p{L}{2,5}\s+\p{L}{3,}\b",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (hasNumericQuestionPattern && QueryTokenizer.ContainsNumericIndicators(query))
                    return true;
            }

            return false;
        }

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

        private static List<string> ExtractCriticalPhrases(string queryLower, List<string> queryWordsLower)
        {
            var criticalPhrases = new List<string>();
            
            if (queryWordsLower.Count < 2)
                return criticalPhrases;
            
            for (int i = 0; i < queryWordsLower.Count - 1; i++)
            {
                var word1 = queryWordsLower[i];
                var word2 = queryWordsLower[i + 1];
                var totalLength = word1.Length + word2.Length;
                
                if (totalLength >= 7 && word1.Length >= 2 && word2.Length >= 3)
                {
                    var phrase = $"{word1} {word2}";
                    if (queryLower.Contains(phrase))
                    {
                        criticalPhrases.Add(phrase);
                    }
                }
            }
            
            if (criticalPhrases.Count == 0)
            {
                var significantWords = queryWordsLower.Where(w => w.Length >= 5).ToList();
                criticalPhrases.AddRange(significantWords);
            }
            
            return criticalPhrases.Distinct().Take(5).ToList();
        }

        private static bool IsVagueQuery(string query, bool hasNameQuery, bool requiresNumericContext)
        {
            if (string.IsNullOrWhiteSpace(query))
                return false;

            if (hasNameQuery || requiresNumericContext)
                return false;

            if (!ContainsQuestionIndicators(query))
                return false;

            var queryLower = query.ToLowerInvariant();
            var queryWords = queryLower.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (queryWords.Length < 3 || queryWords.Length > 6)
                return false;

            var hasPossessivePattern = System.Text.RegularExpressions.Regex.IsMatch(queryLower,
                @"\b\p{L}{3,}\p{M}?n\s+\p{L}{3,}\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (hasPossessivePattern)
                return true;

            var hasPossessiveSuffixPattern = System.Text.RegularExpressions.Regex.IsMatch(queryLower,
                @"\b\p{L}{3,}(?:\p{M}?n|[''']?\p{M}?n\p{M}?|[''']?\p{M}?n\p{M}?n)\s+\p{L}{3,}\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (hasPossessiveSuffixPattern)
                return true;

            var hasGenericQuestionPattern = System.Text.RegularExpressions.Regex.IsMatch(queryLower,
                @"\b\p{L}{2,5}\s+\p{L}{3,}\s+\p{L}{3,}\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (hasGenericQuestionPattern)
                return true;

            var hasTwoNounsPattern = System.Text.RegularExpressions.Regex.IsMatch(queryLower,
                @"\b\p{L}{4,}\s+\p{L}{4,}\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (hasTwoNounsPattern)
                return true;

            var hasNounWithSuffixPattern = System.Text.RegularExpressions.Regex.IsMatch(queryLower,
                @"\b\p{L}{4,}\p{M}?\s+\p{L}{4,}\p{M}?\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (hasNounWithSuffixPattern)
                return true;

            return false;
        }

        /// <summary>
        /// Combines vector and keyword search results using Reciprocal Rank Fusion (RRF) algorithm
        /// Based on industry standards (R2R, RAGFlow): RRF(k) = sum(1 / (k + rank)) for each result set
        /// Preserves original RelevanceScore for keyword results to maintain high scores (e.g., 110.0)
        /// Prioritizes high-scoring keyword chunks to ensure they are included in results
        /// </summary>
        private List<DocumentChunk> CombineSearchResultsWithRRF(
            List<DocumentChunk> vectorResults,
            List<DocumentChunk> keywordResults,
            int maxResults)
        {
            var chunkScores = new Dictionary<Guid, (double RRFScore, DocumentChunk Chunk, double? OriginalScore)>();

            for (int i = 0; i < vectorResults.Count; i++)
            {
                var chunk = vectorResults[i];
                var rank = i + 1;
                var rrfScore = VectorSearchWeight / (ReciprocalRankFusionK + rank);
                
                if (chunkScores.TryGetValue(chunk.Id, out var existing))
                {
                    chunkScores[chunk.Id] = (existing.RRFScore + rrfScore, existing.Chunk, existing.OriginalScore);
                }
                else
                {
                    chunkScores[chunk.Id] = (rrfScore, chunk, chunk.RelevanceScore);
                }
            }

            for (int i = 0; i < keywordResults.Count; i++)
            {
                var chunk = keywordResults[i];
                var rank = i + 1;
                var rrfScore = KeywordSearchWeight / (ReciprocalRankFusionK + rank);
                
                if (chunkScores.TryGetValue(chunk.Id, out var existing))
                {
                    var keywordScore = chunk.RelevanceScore ?? 0.0;
                    var existingScore = existing.OriginalScore ?? 0.0;
                    var preservedOriginalScore = keywordScore > existingScore ? keywordScore : existingScore;
                    chunkScores[chunk.Id] = (existing.RRFScore + rrfScore, existing.Chunk, preservedOriginalScore);
                }
                else
                {
                    chunkScores[chunk.Id] = (rrfScore, chunk, chunk.RelevanceScore);
                }
            }

            var maxRRFScore = chunkScores.Values.Any() ? chunkScores.Values.Max(v => v.RRFScore) : 1.0;
            var minRRFScore = chunkScores.Values.Any() ? chunkScores.Values.Min(v => v.RRFScore) : 0.0;
            var rrfScoreRange = maxRRFScore - minRRFScore;

            var highScoringKeywordChunks = chunkScores.Values
                .Where(x => x.OriginalScore.HasValue && x.OriginalScore.Value > 4.5)
                .OrderByDescending(x => x.OriginalScore!.Value)
                .ThenBy(x => x.Chunk.ChunkIndex)
                .Select(x =>
                {
                    x.Chunk.RelevanceScore = x.OriginalScore!.Value;
                    return x.Chunk;
                })
                .ToList();

            var otherChunks = chunkScores.Values
                .Where(x => !x.OriginalScore.HasValue || x.OriginalScore.Value <= 4.5)
                .OrderByDescending(x => x.RRFScore)
                .ThenBy(x => x.Chunk.ChunkIndex)
                .Select(x =>
                {
                    double normalizedScore;
                    if (rrfScoreRange > 0)
                    {
                        normalizedScore = ((x.RRFScore - minRRFScore) / rrfScoreRange) * 100.0;
                    }
                    else
                    {
                        normalizedScore = x.RRFScore * 100.0;
                    }
                    
                    x.Chunk.RelevanceScore = normalizedScore;
                    return x.Chunk;
                })
                .ToList();

            var result = new List<DocumentChunk>();
            var seenIds = new HashSet<Guid>();

            foreach (var chunk in highScoringKeywordChunks)
            {
                if (!seenIds.Contains(chunk.Id) && result.Count < maxResults)
                {
                    result.Add(chunk);
                    seenIds.Add(chunk.Id);
                }
            }

            foreach (var chunk in otherChunks)
            {
                if (!seenIds.Contains(chunk.Id) && result.Count < maxResults)
                {
                    result.Add(chunk);
                    seenIds.Add(chunk.Id);
                }
            }
            
            var finalResult = result.Take(maxResults).ToList();
            return finalResult;
        }
    }
}

