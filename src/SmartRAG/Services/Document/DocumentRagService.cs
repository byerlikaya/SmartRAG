using SmartRAG.Interfaces.Search;
using SmartRAG.Models.Schema;

namespace SmartRAG.Services.Document;


/// <summary>
/// Service for document RAG pipeline: search, chunk prioritization, context building, and answer generation.
/// Implements IRagAnswerGeneratorService (used by strategy executor) and IRagContextAnswerGenerator (used by MCP handler).
/// </summary>
public class DocumentRagService : IRagAnswerGeneratorService, IRagContextAnswerGenerator
{
    private const int MinSearchResultsCount = 0;
    private const int FallbackSearchMaxResults = 30;
    private const int MinSubstantialContentLength = 30;
    private const int MaxExpandedChunks = 50;
    private const int MaxContextSize = 18000;
    private const double DocumentBoostThreshold = 4.5;
    private const int TopChunksPerDocument = 5;
    private const int ChunksToCheckForKeywords = 30;
    private const double DocumentScoreThreshold = 0.8;
    private const double NumberedListBonusPerItem = 100.0;
    private const double NumberedListWordMatchBonus = 10.0;
    private const int PreviousQuerySearchMaxResults = 15;
    private const double PreviousQueryChunkScoreBoost = 0.5;
    private const int MinSingleWordLengthForFileNameMatch = 4;
    private const int EntityDocTailChunkCount = 25;

    private readonly IDocumentRepository _documentRepository;
    private readonly IAIService _aiService;
    private readonly SmartRagOptions _options;
    private readonly ILogger<DocumentRagService> _logger;
    private readonly IQueryIntentClassifierService _queryIntentClassifier;
    private readonly IConversationManagerService _conversationManager;
    private readonly IPromptBuilderService _promptBuilder;
    private readonly IDocumentScoringService _documentScoring;
    private readonly ISourceBuilderService _sourceBuilder;
    private readonly IContextExpansionService _contextExpansion;
    private readonly IDocumentRelevanceCalculatorService _relevanceCalculator;
    private readonly IQueryWordMatcherService _queryWordMatcher;
    private readonly IQueryPatternAnalyzerService _queryPatternAnalyzer;
    private readonly IChunkPrioritizerService _chunkPrioritizer;
    private readonly IDocumentService _documentService;
    private readonly IQueryAnalysisService _queryAnalysis;
    private readonly IResponseBuilderService _responseBuilder;
    private readonly IDocumentSearchStrategyService _documentSearchStrategy;

    public DocumentRagService(
        IDocumentRepository documentRepository,
        IAIService aiService,
        IOptions<SmartRagOptions> options,
        ILogger<DocumentRagService> logger,
        IQueryIntentClassifierService queryIntentClassifier,
        IConversationManagerService conversationManager,
        IPromptBuilderService promptBuilder,
        IDocumentScoringService documentScoring,
        ISourceBuilderService sourceBuilder,
        IContextExpansionService contextExpansion,
        IDocumentRelevanceCalculatorService relevanceCalculator,
        IQueryWordMatcherService queryWordMatcher,
        IQueryPatternAnalyzerService queryPatternAnalyzer,
        IChunkPrioritizerService chunkPrioritizer,
        IDocumentService documentService,
        IQueryAnalysisService queryAnalysis,
        IResponseBuilderService responseBuilder,
        IDocumentSearchStrategyService documentSearchStrategy)
    {
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _queryIntentClassifier = queryIntentClassifier ?? throw new ArgumentNullException(nameof(queryIntentClassifier));
        _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));
        _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        _documentScoring = documentScoring ?? throw new ArgumentNullException(nameof(documentScoring));
        _sourceBuilder = sourceBuilder ?? throw new ArgumentNullException(nameof(sourceBuilder));
        _contextExpansion = contextExpansion ?? throw new ArgumentNullException(nameof(contextExpansion));
        _relevanceCalculator = relevanceCalculator ?? throw new ArgumentNullException(nameof(relevanceCalculator));
        _queryWordMatcher = queryWordMatcher ?? throw new ArgumentNullException(nameof(queryWordMatcher));
        _queryPatternAnalyzer = queryPatternAnalyzer ?? throw new ArgumentNullException(nameof(queryPatternAnalyzer));
        _chunkPrioritizer = chunkPrioritizer ?? throw new ArgumentNullException(nameof(chunkPrioritizer));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _queryAnalysis = queryAnalysis ?? throw new ArgumentNullException(nameof(queryAnalysis));
        _responseBuilder = responseBuilder ?? throw new ArgumentNullException(nameof(responseBuilder));
        _documentSearchStrategy = documentSearchStrategy ?? throw new ArgumentNullException(nameof(documentSearchStrategy));
    }

    /// <inheritdoc />
    public async Task<string> GenerateRagAnswerFromContextAsync(string query, string context, string? conversationHistory, CancellationToken cancellationToken = default)
    {
        var prompt = _promptBuilder.BuildDocumentRagPrompt(query, context, conversationHistory);
        return await _aiService.GenerateResponseAsync(prompt, new List<string> { context }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RagResponse> GenerateBasicRagAnswerAsync(GenerateRagAnswerRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var trimmedQuery = request.Query.Trim();

        var filenameMatchWords = QueryTokenizer.GetWordsForPhraseExtraction(trimmedQuery).Where(w => w.Length >= 4).ToList();
        var allDocsForFilename = await _documentService.GetAllDocumentsFilteredAsync(request.Options, cancellationToken);
        var filenameMatchedDocs = allDocsForFilename
            .Where(d => MatchesFilenameForQuery(filenameMatchWords, d.FileName))
            .ToList();
        if (filenameMatchedDocs.Count > 0 && !string.IsNullOrWhiteSpace(trimmedQuery) &&
            (request.PreCalculatedResults == null || request.PreCalculatedResults.Count == 0))
        {
            var directChunks = await LoadChunksFromEntityMatchedDocumentsAsync(
                filenameMatchedDocs.Select(d => d.Id).ToHashSet(),
                80,
                cancellationToken);
            if (directChunks.Count > 0)
            {
                foreach (var c in directChunks)
                    c.RelevanceScore = DocumentBoostThreshold + 2.0;
                var directContext = _contextExpansion.BuildLimitedContext(directChunks, MaxContextSize);
                var directAnswer = await GenerateRagAnswerFromContextAsync(trimmedQuery, directContext, request.ConversationHistory, cancellationToken);
                var directSources = await _sourceBuilder.BuildSourcesAsync(directChunks, _documentRepository);
                return _responseBuilder.CreateRagResponse(trimmedQuery, directAnswer, directSources);
            }
        }

        if (!string.IsNullOrWhiteSpace(trimmedQuery))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var intentAnalysis = await _queryIntentClassifier.AnalyzeQueryAsync(trimmedQuery, request.ConversationHistory, cancellationToken).ConfigureAwait(false);
            if (intentAnalysis.IsConversation)
            {
                var conversationAnswer = !string.IsNullOrWhiteSpace(intentAnalysis.Answer)
                    ? intentAnalysis.Answer
                    : await _conversationManager.HandleGeneralConversationAsync(trimmedQuery, request.ConversationHistory, cancellationToken).ConfigureAwait(false);
                return _responseBuilder.CreateRagResponse(trimmedQuery, conversationAnswer ?? string.Empty, new List<SearchSource>());
            }
        }

        var baseOptions = request.Options ?? SearchOptions.FromConfig(_options);
        var queryForSearch = request.Query;
        var searchMaxResults = _queryAnalysis.DetermineInitialSearchCount(queryForSearch, request.MaxResults);

        List<DocumentChunk> chunks;
        var queryTokens = request.QueryTokens ?? QueryTokenizer.TokenizeQuery(queryForSearch);
        var previousQueryChunkIds = new HashSet<Guid>();

        DocumentChunk? preservedChunk0 = null;
        List<Entities.Document>? allDocuments = null;
        var effectiveOptions = request.Options ?? baseOptions;

        if (request.PreCalculatedResults is { Count: > 0 })
        {
            var filteredPreCalculatedResults = request.PreCalculatedResults;
            if (request.Options != null)
            {
                allDocuments = await _documentService.GetAllDocumentsFilteredAsync(request.Options, cancellationToken);
                var allowedDocIds = new HashSet<Guid>(allDocuments.Select(d => d.Id));
                var optionFiltered = request.PreCalculatedResults.Where(c => allowedDocIds.Contains(c.DocumentId)).ToList();
                if (optionFiltered.Count > 0)
                    filteredPreCalculatedResults = optionFiltered;
            }

            chunks = filteredPreCalculatedResults
                .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                .ThenBy(c => c.ChunkIndex)
                .ToList();
        }
        else
        {
            var (cleanedQuery, searchOptions) = SourceTagParser.Parse(queryForSearch, baseOptions);
            effectiveOptions = searchOptions;

            if (string.IsNullOrWhiteSpace(cleanedQuery))
                throw new ArgumentException("Query cannot be empty", nameof(request.Query));

            cancellationToken.ThrowIfCancellationRequested();
            var searchResults = await _documentSearchStrategy.SearchDocumentsAsync(cleanedQuery, searchMaxResults, searchOptions, request.QueryTokens, cancellationToken);
            chunks = searchResults.ToList();

            var previousUserQuery = GetLastUserQueryDistinctFrom(request.ConversationHistory, cleanedQuery);
            const int minSubstantiveQueryLength = 12;
            var isSubstantivePreviousQuery = !string.IsNullOrWhiteSpace(previousUserQuery) &&
                previousUserQuery.Trim().Length >= minSubstantiveQueryLength &&
                !string.Equals(previousUserQuery.Trim(), cleanedQuery.Trim(), StringComparison.OrdinalIgnoreCase);

            if (isSubstantivePreviousQuery)
            {
                var extraResults = await _documentSearchStrategy.SearchDocumentsAsync(
                    previousUserQuery!.Trim(),
                    Math.Min(searchMaxResults, PreviousQuerySearchMaxResults),
                    searchOptions,
                    null,
                    cancellationToken);
                var currentIds = new HashSet<Guid>(chunks.Select(c => c.Id));
                var maxScore = chunks.Count > 0 ? chunks.Max(c => c.RelevanceScore ?? 0.0) : 0.0;
                foreach (var c in extraResults.Where(c => !currentIds.Contains(c.Id)))
                {
                    c.RelevanceScore = maxScore + PreviousQueryChunkScoreBoost;
                    previousQueryChunkIds.Add(c.Id);
                    chunks.Add(c);
                    currentIds.Add(c.Id);
                }
            }

            var earlyPotentialNames = QueryTokenizer.ExtractPotentialNames(queryForSearch);
            preservedChunk0 = chunks.FirstOrDefault(c => c.ChunkIndex == 0);
            if (preservedChunk0 != null && !Chunk0IsQueryRelevant(preservedChunk0, queryTokens, earlyPotentialNames))
                preservedChunk0 = null;
            var nonZeroChunksForSearch = chunks.Where(c => c.ChunkIndex != 0).ToList();
            var earlyPhraseWords = QueryTokenizer.GetWordsForPhraseExtraction(queryForSearch);
            chunks = _chunkPrioritizer.PrioritizeChunksByQueryWords(nonZeroChunksForSearch, queryTokens, earlyPhraseWords);
            chunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(chunks, preservedChunk0);

            if (previousQueryChunkIds.Count > 0)
            {
                var previousQueryChunks = chunks.Where(c => previousQueryChunkIds.Contains(c.Id)).ToList();
                var prevTopDocIds = previousQueryChunks
                    .GroupBy(c => c.DocumentId)
                    .Select(g => new { DocumentId = g.Key, Score = g.Sum(c => c.RelevanceScore ?? 0.0) })
                    .OrderByDescending(x => x.Score)
                    .Take(1)
                    .Select(x => x.DocumentId)
                    .ToHashSet();

                var currentMainChunks = chunks.Where(c => !previousQueryChunkIds.Contains(c.Id)).ToList();
                var currentTopDocId = currentMainChunks
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .Select(c => c.DocumentId)
                    .FirstOrDefault();
                var prevTopDocId = prevTopDocIds.FirstOrDefault();
                var sameTopic = prevTopDocIds.Count > 0 && currentTopDocId != Guid.Empty &&
                    currentTopDocId == prevTopDocId;
                if (sameTopic)
                {
                    chunks = chunks.Where(c => prevTopDocIds.Contains(c.DocumentId)).ToList();
                }
            }
        }

        var currentTopDocIdForOrdering = Guid.Empty;
        if (chunks.Count > 0)
        {
            var mainChunks = previousQueryChunkIds.Count > 0
                ? chunks.Where(c => !previousQueryChunkIds.Contains(c.Id)).ToList()
                : chunks;
            currentTopDocIdForOrdering = mainChunks
                .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                .Select(c => c.DocumentId)
                .FirstOrDefault();
        }

        var preferPreviousDoc = previousQueryChunkIds.Count > 0 && currentTopDocIdForOrdering != Guid.Empty &&
            chunks.Any(c => previousQueryChunkIds.Contains(c.Id) && c.DocumentId == currentTopDocIdForOrdering);

        var topOriginalChunks = chunks
            .OrderByDescending(c => preferPreviousDoc && previousQueryChunkIds.Contains(c.Id))
            .ThenByDescending(c => c.ChunkIndex == 0)
            .ThenByDescending(c => c.RelevanceScore ?? 0.0)
            .ThenBy(c => c.ChunkIndex)
            .Take(Math.Min(chunks.Count, Math.Max(10, request.MaxResults * 2)))
            .ToList();

        var originalChunkIds = new HashSet<Guid>(topOriginalChunks.Select(c => c.Id));
        HashSet<Guid>? overlapChunkIds = null;
        HashSet<Guid>? entityChunkIdsFromComprehensive = null;

        var isFollowUpWithContext = previousQueryChunkIds.Count > 0;
        var requiresComprehensive = _queryPatternAnalyzer.RequiresComprehensiveSearch(queryForSearch);
        var needsAggressiveSearch = !isFollowUpWithContext &&
            (chunks.Count < 5 || requiresComprehensive);
        if (needsAggressiveSearch)
        {
            var potentialNames = QueryTokenizer.ExtractPotentialNames(queryForSearch);
            preservedChunk0 ??= chunks.FirstOrDefault(c => c.ChunkIndex == 0);
            if (preservedChunk0 != null && !Chunk0IsQueryRelevant(preservedChunk0, queryTokens, potentialNames))
                preservedChunk0 = null;

            allDocuments = await EnsureAllDocumentsLoadedAsync(allDocuments, effectiveOptions, cancellationToken);
            var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();
            var queryWords = queryTokens;
            var scoredChunks = _documentScoring.ScoreChunks(allChunks, queryWords, potentialNames);

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
                TopChunksPerDocument,
                queryForSearch,
                potentialNames);

            var relevantDocuments = _relevanceCalculator.IdentifyRelevantDocuments(
                documentScores,
                DocumentScoreThreshold);

            var relevantDocumentChunks = relevantDocuments
                .SelectMany(d => scoredChunks.Where(c => c.DocumentId == d.Id))
                .ToList();

            var docScoreMap = relevantDocuments.ToDictionary(
                d => d.Id,
                d => documentScores.First(ds => ds.Document.Id == d.Id).Score);

            foreach (var chunk in relevantDocumentChunks)
            {
                if (docScoreMap.TryGetValue(chunk.DocumentId, out var docScore))
                {
                    chunk.RelevanceScore = (chunk.RelevanceScore ?? 0.0) + docScore;
                }
            }

            allChunks = relevantDocumentChunks.Concat(
                allDocuments.Except(relevantDocuments)
                    .SelectMany(d => scoredChunks.Where(c => c.DocumentId == d.Id))
            ).ToList();

            if (preservedChunk0 != null && allChunks.All(c => c.Id != preservedChunk0.Id))
            {
                allChunks.Insert(0, preservedChunk0);
            }

            var numberedListChunks = _queryPatternAnalyzer.ScoreChunksByNumberedLists(
                allChunks,
                queryWords,
                NumberedListBonusPerItem,
                NumberedListWordMatchBonus);

            if (_queryPatternAnalyzer.RequiresComprehensiveSearch(queryForSearch))
            {
                var words = queryWords ?? new List<string>();
                var phraseWords = QueryTokenizer.GetWordsForPhraseExtraction(queryForSearch);
                var docIdsFromFilenameMatch = GetEntityMatchedDocumentIds(allDocuments, words, potentialNames, phraseWords);
                var entityFileNameChunks = GetEntityFileNameChunks(allChunks, words, potentialNames, phraseWords)
                    .Take(Math.Max(searchMaxResults * 2, 50))
                    .ToList();

                var entityMatchedDocIds = entityFileNameChunks.Select(c => c.DocumentId).Distinct().ToHashSet();
                if (entityMatchedDocIds.Count == 0 && docIdsFromFilenameMatch.Count > 0)
                {
                    var loadedFromMatch = await LoadChunksFromEntityMatchedDocumentsAsync(
                        docIdsFromFilenameMatch,
                        Math.Max(searchMaxResults * 2, 50),
                        cancellationToken);
                    entityFileNameChunks = loadedFromMatch;
                    entityMatchedDocIds = docIdsFromFilenameMatch;
                }
                else if (docIdsFromFilenameMatch.Count > 0)
                {
                    var loadedWithTail = await LoadChunksFromEntityMatchedDocumentsAsync(
                        docIdsFromFilenameMatch,
                        Math.Max(searchMaxResults * 2, 50),
                        cancellationToken);
                    var existingIds = new HashSet<Guid>(entityFileNameChunks.Select(c => c.Id));
                    var tailChunks = loadedWithTail.Where(c => !existingIds.Contains(c.Id)).ToList();
                    entityFileNameChunks = entityFileNameChunks.Concat(tailChunks).ToList();
                    entityMatchedDocIds = entityMatchedDocIds.Union(docIdsFromFilenameMatch).ToHashSet();
                }

                var entityHeaderChunks = new List<DocumentChunk>();

                foreach (var docId in entityMatchedDocIds)
                {
                    var longestHeader = allChunks
                        .Where(c => c.DocumentId == docId && c.ChunkIndex == 0)
                        .OrderByDescending(c => c.Content.Length)
                        .FirstOrDefault();
                    if (longestHeader != null)
                        entityHeaderChunks.Add(longestHeader);
                }

                var existingChunkIds = new HashSet<Guid>(entityFileNameChunks.Concat(chunks).Concat(entityHeaderChunks).Select(c => c.Id));
                var overlapChunks = new List<DocumentChunk>();
                if (entityMatchedDocIds.Count > 0 && words.Count > 0)
                {
                    overlapChunks = await LoadChunksWithQueryWordOverlapAsync(
                        entityMatchedDocIds,
                        words,
                        existingChunkIds,
                        Math.Min(10, searchMaxResults),
                        cancellationToken);
                    overlapChunkIds = new HashSet<Guid>(overlapChunks.Concat(entityHeaderChunks).Select(c => c.Id));
                }
                else if (entityHeaderChunks.Any())
                {
                    overlapChunkIds = new HashSet<Guid>(entityHeaderChunks.Select(c => c.Id));
                }

                var numberedListWithQueryWords = GetFilteredAndSortedNumberedListChunks(
                    numberedListChunks, words, hasQueryWords: true, takeCount: searchMaxResults * 3);

                var numberedListOnly = GetFilteredAndSortedNumberedListChunks(
                    numberedListChunks, words, hasQueryWords: false, takeCount: searchMaxResults * 2);

                var queryWordsOnly = _chunkPrioritizer.PrioritizeChunksByQueryWords(
                    allChunks.Where(c => !_queryPatternAnalyzer.DetectNumberedLists(c.Content)).ToList(),
                    words)
                    .Take(searchMaxResults * 2)
                    .ToList();

                var mergedChunks = new List<DocumentChunk>();
                var seenIds = new HashSet<Guid>();

                foreach (var chunk in entityHeaderChunks.Concat(overlapChunks).Concat(entityFileNameChunks).Concat(queryWordsOnly).Concat(numberedListWithQueryWords).Concat(numberedListOnly).Concat(chunks))
                {
                    if (seenIds.Contains(chunk.Id) || mergedChunks.Count >= searchMaxResults * 4)
                        continue;
                    mergedChunks.Add(chunk);
                    seenIds.Add(chunk.Id);
                }

                mergedChunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(mergedChunks, preservedChunk0);

                if (mergedChunks.Count > 0 && (overlapChunks.Count > 0 || entityFileNameChunks.Count > 0 || mergedChunks.Count > chunks.Count))
                {
                    chunks = mergedChunks;
                    var entityChunkIds = new HashSet<Guid>(
                        entityHeaderChunks.Concat(overlapChunks).Concat(entityFileNameChunks).Select(c => c.Id));
                    if (entityChunkIds.Count > 0)
                        entityChunkIdsFromComprehensive = entityChunkIds;
                }
            }
            else
            {
                var prioritizedChunksForFallback = _chunkPrioritizer.PrioritizeChunksByQueryWords(
                    numberedListChunks.Where(c => queryWords.Any(word => c.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)).ToList(),
                    queryWords)
                    .Take(searchMaxResults * 4)
                    .ToList();

                if (prioritizedChunksForFallback.Count > chunks.Count)
                {
                    chunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(prioritizedChunksForFallback, preservedChunk0);
                }
            }
        }
        else if (requiresComprehensive)
        {
            allDocuments = await EnsureAllDocumentsLoadedAsync(allDocuments, effectiveOptions, cancellationToken);
            var potentialNames = QueryTokenizer.ExtractPotentialNames(queryForSearch);
            var words = queryTokens ?? new List<string>();
            var phraseWords = QueryTokenizer.GetWordsForPhraseExtraction(queryForSearch);
            var docIdsFromFilenameMatch = GetEntityMatchedDocumentIds(allDocuments, words, potentialNames, phraseWords);
            if (docIdsFromFilenameMatch.Count > 0)
            {
                var entityChunks = await LoadChunksFromEntityMatchedDocumentsAsync(
                    docIdsFromFilenameMatch,
                    Math.Max(searchMaxResults * 2, 50),
                    cancellationToken);
                var existingIds = new HashSet<Guid>(chunks.Select(c => c.Id));
                var newEntityChunks = entityChunks.Where(c => !existingIds.Contains(c.Id)).ToList();
                if (newEntityChunks.Count > 0)
                {
                    const double entityChunkScore = 150.0;
                    foreach (var c in newEntityChunks)
                    {
                        c.RelevanceScore = entityChunkScore;
                    }
                    entityChunkIdsFromComprehensive = new HashSet<Guid>(newEntityChunks.Select(c => c.Id));
                    chunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(
                        newEntityChunks.Concat(chunks).ToList(),
                        preservedChunk0);
                }
            }
        }

        if (entityChunkIdsFromComprehensive is { Count: > 0 })
        {
            foreach (var c in chunks.Where(c => entityChunkIdsFromComprehensive.Contains(c.Id)))
            {
                var current = c.RelevanceScore ?? 0.0;
                if (current < DocumentBoostThreshold)
                    c.RelevanceScore = DocumentBoostThreshold + 1.0;
            }
        }

        if (chunks.Count > 0)
        {
            var contextExpansionThreshold = CalculateAdaptiveThreshold(
                chunks,
                highScoreThreshold: 3.0,
                highScorePercentile: 1.0,
                lowScorePercentile: 0.4,
                useScoreRangeCheck: false,
                fixedHighScoreThreshold: DocumentBoostThreshold);

            var relevantDocumentChunks = chunks
                .Where(c => (c.RelevanceScore ?? 0.0) >= contextExpansionThreshold)
                .ToList();

            var otherChunks = chunks
                .Where(c => (c.RelevanceScore ?? 0.0) < contextExpansionThreshold)
                .ToList();

            if (relevantDocumentChunks.Count > 0)
            {
                var originalScores = relevantDocumentChunks.ToDictionary(c => c.Id, c => c.RelevanceScore ?? 0.0);

                var topChunk = relevantDocumentChunks
                    .OrderByDescending(c => c.ChunkIndex == 0)
                    .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                    .ThenBy(c => c.ChunkIndex)
                    .FirstOrDefault();

                var topChunksForExpansion = topChunk != null
                    ? relevantDocumentChunks
                        .Where(c => c.DocumentId == topChunk.DocumentId)
                        .OrderByDescending(c => c.ChunkIndex == 0)
                        .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                        .ThenBy(c => c.ChunkIndex)
                        .Take(Math.Min(5, relevantDocumentChunks.Count(c => c.DocumentId == topChunk.DocumentId)))
                        .ToList()
                    : relevantDocumentChunks
                        .OrderByDescending(c => c.ChunkIndex == 0)
                        .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                        .ThenBy(c => c.ChunkIndex)
                        .Take(Math.Min(5, relevantDocumentChunks.Count))
                        .ToList();

                var contextWindow = _contextExpansion.DetermineContextWindow(topChunksForExpansion, queryForSearch);
                var expandedChunks = await _contextExpansion.ExpandContextAsync(topChunksForExpansion, contextWindow);

                var queryWords = queryTokens;

                var maxOriginalScore = originalScores.Values.Any() ? originalScores.Values.Max() : 0.0;
                var minOriginalScore = originalScores.Values.Any() ? originalScores.Values.Min() : 0.0;

                foreach (var chunk in expandedChunks)
                {
                    if (originalScores.TryGetValue(chunk.Id, out var score))
                    {
                        chunk.RelevanceScore = score;
                    }
                    else
                    {
                        var content = chunk.Content.ToLowerInvariant();
                        var wordMatches = (queryWords ?? Enumerable.Empty<string>()).Count(word =>
                            content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);

                        var expandedScore = wordMatches * 0.05;
                        var maxAllowedScore = minOriginalScore > 0 ? minOriginalScore * 0.3 : maxOriginalScore * 0.05;
                        chunk.RelevanceScore = Math.Min(expandedScore, maxAllowedScore);
                    }
                }

                var relevantNotExpanded = relevantDocumentChunks
                    .Where(c => expandedChunks.All(e => e.Id != c.Id))
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .ThenBy(c => c.ChunkIndex)
                    .ToList();
                chunks = expandedChunks
                    .OrderByDescending(c => originalChunkIds != null && originalChunkIds.Contains(c.Id))
                    .ThenByDescending(c => c.ChunkIndex == 0)
                    .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                    .ThenBy(c => c.ChunkIndex)
                    .Concat(relevantNotExpanded)
                    .Concat(otherChunks
                        .OrderByDescending(c => c.ChunkIndex == 0)
                        .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                        .ThenBy(c => c.ChunkIndex))
                    .ToList();

                if (chunks.Count > MaxExpandedChunks)
                {
                    chunks = chunks.Take(MaxExpandedChunks).ToList();
                }
            }
            else
            {
                chunks = _chunkPrioritizer.PrioritizeChunksByRelevanceScore(chunks);
            }
        }

        if (preservedChunk0 == null)
        {
            var topOriginalDocumentId = chunks
                .Where(c => (c.RelevanceScore ?? 0.0) >= DocumentBoostThreshold)
                .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                .Select(c => c.DocumentId)
                .FirstOrDefault();

            if (topOriginalDocumentId != Guid.Empty)
            {
                preservedChunk0 = chunks.FirstOrDefault(c => c.ChunkIndex == 0 && c.DocumentId == topOriginalDocumentId);
            }
        }

        chunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(chunks, preservedChunk0);

        var documentChunks = chunks.Where(c => c.DocumentType == "Document" || c.DocumentType == "Audio").ToList();
        var imageChunks = chunks.Where(c => c.DocumentType == "Image").ToList();

        var potentialNamesForOrdering = QueryTokenizer.ExtractPotentialNames(queryForSearch);
        var entityHeaderChunkIds = GetEntityHeaderChunkIds(documentChunks.Concat(imageChunks).ToList(), potentialNamesForOrdering);
        var entityMatchedChunkIds = entityChunkIdsFromComprehensive ?? new HashSet<Guid>();

        if (entityMatchedChunkIds.Count > 0)
        {
            var entityMatchedDocumentIds = documentChunks
                .Where(c => entityMatchedChunkIds.Contains(c.Id))
                .Select(c => c.DocumentId)
                .Distinct()
                .ToHashSet();

            if (entityMatchedDocumentIds.Count > 0)
            {
                documentChunks = documentChunks
                    .Where(c => entityMatchedDocumentIds.Contains(c.DocumentId))
                    .ToList();

                foreach (var c in documentChunks)
                {
                    var currentScore = c.RelevanceScore ?? 0.0;
                    if (currentScore < DocumentBoostThreshold)
                    {
                        c.RelevanceScore = DocumentBoostThreshold + 1.0;
                    }
                }

                chunks = documentChunks
                    .Concat(imageChunks)
                    .ToList();
            }
        }

        var allContentChunks = documentChunks.Concat(imageChunks).ToList();
        var orderedAllChunks = allContentChunks
            .OrderByDescending(c => entityMatchedChunkIds.Contains(c.Id))
            .ThenByDescending(c => entityHeaderChunkIds.Contains(c.Id))
            .ThenByDescending(c => originalChunkIds != null && originalChunkIds.Contains(c.Id))
            .ThenByDescending(c => c.RelevanceScore ?? 0.0)
            .ToList();

        var finalQueryTokens = queryTokens ?? QueryTokenizer.TokenizeQuery(queryForSearch);
        var finalPhraseWords = QueryTokenizer.GetWordsForPhraseExtraction(queryForSearch);
        chunks = _chunkPrioritizer.PrioritizeChunksByQueryWords(
            orderedAllChunks,
            finalQueryTokens,
            finalPhraseWords);

        if (chunks.Count == 0)
        {
            var fallbackWords = QueryTokenizer.GetWordsForPhraseExtraction(queryForSearch)
                .Where(w => w.Length >= 4).ToList();
            var fallbackDocs = await _documentService.GetAllDocumentsFilteredAsync(effectiveOptions, cancellationToken);
            var filenameMatched = fallbackDocs
                .Where(d => fallbackWords.Any(w =>
                    (d.FileName ?? string.Empty).IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (w.Length > 4 && Enumerable.Range(4, w.Length - 3).Any(len =>
                        (d.FileName ?? string.Empty).IndexOf(w.Substring(0, len), StringComparison.OrdinalIgnoreCase) >= 0))))
                .ToList();
            if (filenameMatched.Count > 0)
            {
                var fallbackChunks = await LoadChunksFromEntityMatchedDocumentsAsync(
                    filenameMatched.Select(d => d.Id).ToHashSet(),
                    50,
                    cancellationToken);
                if (fallbackChunks.Count > 0)
                    chunks = fallbackChunks;
            }
        }

        var context = _contextExpansion.BuildLimitedContext(chunks, MaxContextSize);

        if (previousQueryChunkIds.Count > 0 && !string.IsNullOrWhiteSpace(request.ConversationHistory))
        {
            var lastAssistantAnswer = ExtractLastAssistantAnswer(request.ConversationHistory);
            if (!string.IsNullOrWhiteSpace(lastAssistantAnswer))
                context = "[Previous turn answer from conversation]\n" + lastAssistantAnswer + "\n\n" + context;
        }

        var answer = await GenerateRagAnswerFromContextAsync(queryForSearch, context, request.ConversationHistory, cancellationToken);

        var sourcesChunkIds = originalChunkIds != null ? new HashSet<Guid>(originalChunkIds) : new HashSet<Guid>();
        foreach (var id in entityHeaderChunkIds)
            sourcesChunkIds.Add(id);
        if (overlapChunkIds != null)
            foreach (var id in overlapChunkIds)
                sourcesChunkIds.Add(id);
        if (entityChunkIdsFromComprehensive != null)
            foreach (var id in entityChunkIdsFromComprehensive)
                sourcesChunkIds.Add(id);
        var sourcesChunks = sourcesChunkIds.Count > 0
            ? chunks.Where(c => sourcesChunkIds.Contains(c.Id)).ToList()
            : chunks;
        if (sourcesChunks.Count == 0 && chunks.Count > 0)
            sourcesChunks = chunks;

        var sources = await _sourceBuilder.BuildSourcesAsync(sourcesChunks, _documentRepository);
        return _responseBuilder.CreateRagResponse(queryForSearch, answer, sources);
    }

    /// <inheritdoc />
    public async Task<RagResponse?> TryAnswerFromFilenameMatchAsync(string query, SearchOptions searchOptions, string? conversationHistory, CancellationToken cancellationToken = default)
    {
        if (!searchOptions.EnableDocumentSearch && !searchOptions.EnableImageSearch && !searchOptions.EnableAudioSearch)
            return null;

        var filenameMatchWords = QueryTokenizer.GetWordsForPhraseExtraction(query).Where(w => w.Length >= 4).ToList();
        var allDocs = await _documentService.GetAllDocumentsFilteredAsync(searchOptions, cancellationToken);
        var filenameMatched = allDocs
            .Where(d => MatchesFilenameForQuery(filenameMatchWords, d.FileName))
            .ToList();

        if (filenameMatched.Count == 0)
            return null;

        var chunks = await LoadChunksFromEntityMatchedDocumentsAsync(
            filenameMatched.Select(d => d.Id).ToHashSet(),
            80,
            cancellationToken);
        if (chunks.Count == 0)
            return null;

        foreach (var c in chunks)
            c.RelevanceScore = DocumentBoostThreshold + 2.0;
        var context = _contextExpansion.BuildLimitedContext(chunks, MaxContextSize);
        var answer = await GenerateRagAnswerFromContextAsync(query, context, conversationHistory, cancellationToken);
        var sources = await _sourceBuilder.BuildSourcesAsync(chunks, _documentRepository);
        return _responseBuilder.CreateRagResponse(query, answer, sources);
    }

    /// <inheritdoc />
    public async Task<(bool CanAnswer, List<DocumentChunk> Results)> CanAnswerFromDocumentsAsync(string query, SearchOptions searchOptions, List<string>? queryTokens = null, CancellationToken cancellationToken = default)
    {
        if (!searchOptions.EnableDocumentSearch)
        {
            return (false, new List<DocumentChunk>());
        }

        try
        {
            var searchResults = await _documentSearchStrategy.SearchDocumentsAsync(query, FallbackSearchMaxResults, searchOptions, queryTokens, cancellationToken);

            if (searchResults.Count is MinSearchResultsCount)
            {
                return (false, searchResults);
            }

            var totalContentLength = searchResults.Sum(c => c.Content.Length);
            var hasSubstantialContent = totalContentLength > MinSubstantialContentLength;

            return (hasSubstantialContent, searchResults);
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogCanAnswerFromDocumentsError(_logger, ex);
            return (true, new List<DocumentChunk>());
        }
    }

    private static bool MatchesFilenameForQuery(List<string> queryWords, string? fileName)
    {
        var fn = (fileName ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(fn))
            return false;
        var fnTokens = fn.Split(new[] { ' ', '.', '-', '_', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 4).ToHashSet();
        foreach (var wl in queryWords.Select(w => w.ToLowerInvariant()))
        {
            if (fn.IndexOf(wl, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            for (var len = 4; len < wl.Length; len++)
            {
                var prefix = wl[..len];
                if (fnTokens.Contains(prefix))
                    return true;
            }
        }
        return false;
    }

    private static bool Chunk0IsQueryRelevant(DocumentChunk? chunk0, List<string>? queryTokens, List<string>? potentialNames = null)
    {
        if (chunk0 == null)
            return false;

        if (queryTokens == null || queryTokens.Count == 0)
            return true;

        var searchableText = string.Concat(chunk0.Content, " ", chunk0.FileName).ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(searchableText.Trim()))
            return false;

        if (potentialNames is { Count: >= 2 })
        {
            var entityPhrase = string.Join(" ", potentialNames.Select(n => n.ToLowerInvariant()));
            if (searchableText.Contains(entityPhrase))
                return true;
        }
        var significantWords = queryTokens.Where(w => w.Length >= 4).ToList();
        return significantWords.Count == 0 || significantWords.Any(w => searchableText.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static string? GetLastUserQueryDistinctFrom(string? conversationHistory, string? currentQuery)
    {
        if (string.IsNullOrWhiteSpace(conversationHistory))
            return null;
        var current = currentQuery?.Trim();
        const string userPrefix = "User: ";
        var lines = conversationHistory.Split('\n');
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var line = lines[i];
            if (!line.StartsWith(userPrefix, StringComparison.OrdinalIgnoreCase))
                continue;
            var question = line[userPrefix.Length..].Trim();
            if (string.IsNullOrWhiteSpace(question))
                continue;
            if (!string.Equals(question, current, StringComparison.OrdinalIgnoreCase))
                return question;
        }
        return null;
    }

    private static string? ExtractLastAssistantAnswer(string? conversationHistory)
    {
        if (string.IsNullOrWhiteSpace(conversationHistory))
            return null;
        const string assistantPrefix = "Assistant: ";
        var lines = conversationHistory.Split('\n');
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var line = lines[i];
            if (!line.StartsWith(assistantPrefix, StringComparison.OrdinalIgnoreCase))
                continue;
            var answer = line[assistantPrefix.Length..].Trim();
            return string.IsNullOrWhiteSpace(answer) ? null : answer;
        }
        return null;
    }

    private async Task<List<Entities.Document>> EnsureAllDocumentsLoadedAsync(List<Entities.Document>? allDocuments, SearchOptions? options, CancellationToken cancellationToken = default)
    {
        allDocuments ??= await _documentService.GetAllDocumentsFilteredAsync(options, cancellationToken);

        var loaded = new List<Entities.Document>();
        foreach (var doc in allDocuments)
        {
            var fullDoc = await _documentService.GetDocumentAsync(doc.Id, cancellationToken);
            loaded.Add(fullDoc.Chunks is { Count: > 0 } ? fullDoc : doc);
        }
        return loaded;
    }

    private static double CalculateAdaptiveThreshold(
        List<DocumentChunk>? chunks,
        double highScoreThreshold = 3.0,
        double highScorePercentile = 0.7,
        double lowScorePercentile = 0.4,
        bool useScoreRangeCheck = true,
        double? fixedHighScoreThreshold = null)
    {
        if (chunks == null || chunks.Count == 0)
            return 0.0;

        var sortedByScore = chunks.OrderByDescending(c => c.RelevanceScore ?? 0.0).ToList();
        var maxScore = sortedByScore.FirstOrDefault()?.RelevanceScore ?? 0.0;

        if (maxScore > highScoreThreshold)
        {
            if (fixedHighScoreThreshold.HasValue)
                return fixedHighScoreThreshold.Value;

            if (useScoreRangeCheck)
            {
                var topPercentileCount = Math.Max(1, (int)(sortedByScore.Count * highScorePercentile));
                var percentileScore = sortedByScore.Skip(topPercentileCount - 1).FirstOrDefault()?.RelevanceScore ?? 4.0;

                var minScore = sortedByScore.LastOrDefault()?.RelevanceScore ?? 0.0;
                var scoreRange = maxScore - minScore;

                if (scoreRange < 0.5 && sortedByScore.Count > 1)
                {
                    return Math.Max(4.5, maxScore - 0.5);
                }

                return Math.Max(4.0, percentileScore - 0.01);
            }
            else
            {
                var topPercentileCount = Math.Max(1, (int)(sortedByScore.Count * highScorePercentile));
                var percentileScore = sortedByScore.Skip(topPercentileCount - 1).FirstOrDefault()?.RelevanceScore ?? 4.0;
                return Math.Max(4.0, percentileScore - 0.01);
            }
        }
        else
        {
            var topPercentileCount = Math.Max(1, (int)(sortedByScore.Count * lowScorePercentile));
            return Math.Max(0.01, sortedByScore.Skip(topPercentileCount - 1).FirstOrDefault()?.RelevanceScore ?? 0.01);
        }
    }

    private static HashSet<Guid> GetEntityHeaderChunkIds(List<DocumentChunk> chunks, List<string> potentialNames)
    {
        var ids = new HashSet<Guid>();
        if (potentialNames.Count < 2)
            return ids;
        var directPhrases = new List<string>();
        for (var i = 0; i < potentialNames.Count - 1; i++)
        {
            var phrase = $"{potentialNames[i].ToLowerInvariant()} {potentialNames[i + 1].ToLowerInvariant()}";
            if (phrase.Length >= 4)
                directPhrases.Add(phrase);
        }
        if (directPhrases.Count == 0)
            return ids;
        foreach (var c in chunks)
        {
            if (c.ChunkIndex > 2)
                continue;
            var fn = (c.FileName ?? string.Empty).ToLowerInvariant();
            if (directPhrases.Any(p => fn.Contains(p)))
                ids.Add(c.Id);
        }
        return ids;
    }

    private static HashSet<Guid> GetEntityMatchedDocumentIds(
        List<Entities.Document>? documents,
        List<string>? queryWords,
        List<string>? potentialNames,
        List<string>? phraseWords)
    {
        if (documents == null || documents.Count == 0)
            return new HashSet<Guid>();

        var phrases = new List<(string W1, string W2)>();
        if (potentialNames is { Count: >= 2 })
        {
            for (var i = 0; i < potentialNames.Count - 1; i++)
            {
                var w1 = potentialNames[i].ToLowerInvariant();
                var w2 = potentialNames[i + 1].ToLowerInvariant();
                if (w1.Length >= 1 && w2.Length >= 3)
                    phrases.Add((w1, w2));
            }
        }
        if (phraseWords is { Count: >= 2 })
        {
            for (var i = 0; i < phraseWords.Count - 1; i++)
            {
                var w1 = phraseWords[i].ToLowerInvariant();
                var w2 = phraseWords[i + 1].ToLowerInvariant();
                if (w1.Length >= 1 && w2.Length >= 3)
                    phrases.Add((w1, w2));
            }
        }
        if (queryWords is { Count: >= 2 })
        {
            for (var i = 0; i < queryWords.Count - 1; i++)
            {
                var w1 = queryWords[i].ToLowerInvariant();
                var w2 = queryWords[i + 1].ToLowerInvariant();
                if (w1.Length >= 1 && w2.Length >= 3)
                    phrases.Add((w1, w2));
            }
        }
        phrases = phrases.Distinct().ToList();

        var singleWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (phraseWords != null)
        {
            foreach (var w in phraseWords)
            {
                var lower = w.ToLowerInvariant();
                if (lower.Length >= MinSingleWordLengthForFileNameMatch)
                    singleWords.Add(lower);
            }
        }
        if (queryWords != null)
        {
            foreach (var w in queryWords)
            {
                var lower = w.ToLowerInvariant();
                if (lower.Length >= MinSingleWordLengthForFileNameMatch)
                    singleWords.Add(lower);
            }
        }

        var result = new HashSet<Guid>();
        foreach (var doc in documents)
        {
            var fn = (doc.FileName ?? string.Empty).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(fn))
                continue;

            var phraseMatch = phrases.Count > 0 && phrases.Any(p =>
            {
                if (fn.IndexOf(p.W1, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
                if (fn.IndexOf(p.W2, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                for (var len = Math.Min(4, p.W2.Length); len < p.W2.Length; len++)
                {
                    var prefix = p.W2[..len];
                    if (fn.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                return false;
            });

            var singleWordMatch = singleWords.Count > 0 && singleWords.Any(word =>
            {
                if (fn.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                for (var len = MinSingleWordLengthForFileNameMatch; len < word.Length; len++)
                {
                    var prefix = word[..len];
                    if (fn.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                return false;
            });

            if (phraseMatch || singleWordMatch)
                result.Add(doc.Id);
        }
        return result;
    }

    private async Task<List<DocumentChunk>> LoadChunksFromEntityMatchedDocumentsAsync(
        HashSet<Guid>? documentIds,
        int maxChunks,
        CancellationToken cancellationToken)
    {
        if (documentIds == null || documentIds.Count == 0)
            return new List<DocumentChunk>();

        var result = new List<DocumentChunk>();
        var seenIds = new HashSet<Guid>();
        foreach (var docId in documentIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var doc = await _documentRepository.GetByIdAsync(docId, cancellationToken);
            if (doc.Chunks == null || doc.Chunks.Count == 0)
                continue;

            var ordered = doc.Chunks
                .OrderBy(c => c.ChunkIndex)
                .ToList();

            var headerChunks = ordered
                .Where(c => c.ChunkIndex is <= 2 or < 0)
                .OrderBy(c => c.ChunkIndex)
                .ThenByDescending(c => c.Content.Length)
                .ToList();
            var restChunks = ordered
                .Where(c => c.ChunkIndex > 2)
                .ToList();

            var headLimit = Math.Max(0, maxChunks - EntityDocTailChunkCount);
            var headChunks = headerChunks.Concat(restChunks).Take(headLimit).ToList();
            var tailChunks = restChunks.Count > EntityDocTailChunkCount
                ? restChunks.Skip(restChunks.Count - EntityDocTailChunkCount).ToList()
                : new List<DocumentChunk>();

            foreach (var c in headChunks.Concat(tailChunks))
            {
                if (!seenIds.Add(c.Id))
                    continue;
                result.Add(c);
                if (result.Count >= maxChunks)
                    break;
            }
            if (result.Count >= maxChunks)
                break;
        }
        return result.Take(maxChunks).ToList();
    }

    private static List<DocumentChunk> GetEntityFileNameChunks(List<DocumentChunk>? chunks, List<string>? queryWords, List<string>? potentialNames, List<string>? phraseWords = null)
    {
        if (chunks == null || chunks.Count == 0)
            return new List<DocumentChunk>();

        var entityFileNameChunks = new List<DocumentChunk>();
        var phrases = new List<(string W1, string W2)>();

        if (potentialNames is { Count: >= 2 })
        {
            for (var i = 0; i < potentialNames.Count - 1; i++)
            {
                var w1 = potentialNames[i].ToLowerInvariant();
                var w2 = potentialNames[i + 1].ToLowerInvariant();
                if (w1.Length >= 1 && w2.Length >= 3)
                    phrases.Add((w1, w2));
            }
        }
        if (phraseWords is { Count: >= 2 })
        {
            for (var i = 0; i < phraseWords.Count - 1; i++)
            {
                var w1 = phraseWords[i].ToLowerInvariant();
                var w2 = phraseWords[i + 1].ToLowerInvariant();
                if (w1.Length >= 1 && w2.Length >= 3)
                    phrases.Add((w1, w2));
            }
        }
        if (queryWords is { Count: >= 2 })
        {
            for (var i = 0; i < queryWords.Count - 1; i++)
            {
                var w1 = queryWords[i].ToLowerInvariant();
                var w2 = queryWords[i + 1].ToLowerInvariant();
                if (w1.Length >= 1 && w2.Length >= 3)
                    phrases.Add((w1, w2));
            }
        }
        phrases = phrases.Distinct().ToList();

        var singleWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (phraseWords != null)
        {
            foreach (var w in phraseWords)
            {
                var lower = w.ToLowerInvariant();
                if (lower.Length >= MinSingleWordLengthForFileNameMatch)
                    singleWords.Add(lower);
            }
        }
        if (queryWords != null)
        {
            foreach (var w in queryWords)
            {
                var lower = w.ToLowerInvariant();
                if (lower.Length >= MinSingleWordLengthForFileNameMatch)
                    singleWords.Add(lower);
            }
        }

        foreach (var c in chunks)
        {
            var fn = (c.FileName ?? string.Empty).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(fn))
                continue;

            var phraseMatch = phrases.Count > 0 && phrases.Any(p =>
            {
                if (fn.IndexOf(p.W1, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
                if (fn.IndexOf(p.W2, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                for (var len = Math.Min(4, p.W2.Length); len < p.W2.Length; len++)
                {
                    var prefix = p.W2[..len];
                    if (fn.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                return false;
            });

            var singleWordMatch = singleWords.Count > 0 && singleWords.Any(word =>
            {
                if (fn.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                for (var len = MinSingleWordLengthForFileNameMatch; len < word.Length; len++)
                {
                    var prefix = word.Substring(0, len);
                    if (fn.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                return false;
            });

            if (phraseMatch || singleWordMatch)
                entityFileNameChunks.Add(c);
        }
        var deduped = entityFileNameChunks.GroupBy(c => c.Id).Select(g => g.First()).ToList();
        var headerChunks = deduped.Where(c => c.ChunkIndex <= 2 || c.ChunkIndex < 0)
            .OrderBy(c => c.DocumentId)
            .ThenByDescending(c => c.Content.Length)
            .ThenBy(c => c.ChunkIndex)
            .ToList();
        var rest = deduped.Where(c => c.ChunkIndex > 2).OrderByDescending(c => c.RelevanceScore ?? 0.0).ThenBy(c => c.ChunkIndex).ToList();
        return headerChunks.Concat(rest).ToList();
    }

    private static int CountQueryWordMatches(string content, List<string>? queryWords)
    {
        if (string.IsNullOrEmpty(content) || queryWords == null || queryWords.Count == 0)
            return 0;
        var contentLower = content.ToLowerInvariant();
        var count = 0;

        foreach (var w in queryWords.Where(w => w.Length >= 3))
        {
            if (contentLower.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                count++;
                continue;
            }
            var prefixLen = Math.Min(5, w.Length);
            if (prefixLen < 4 ||
                contentLower.IndexOf(w.Substring(0, prefixLen), StringComparison.OrdinalIgnoreCase) < 0) continue;
            count++;
        }
        return count;
    }

    private async Task<List<DocumentChunk>> LoadChunksWithQueryWordOverlapAsync(
        HashSet<Guid>? documentIds,
        List<string>? queryWords,
        HashSet<Guid> existingChunkIds,
        int maxChunksPerDocument,
        CancellationToken cancellationToken)
    {
        if (documentIds == null || documentIds.Count == 0 || queryWords == null || queryWords.Count == 0)
            return new List<DocumentChunk>();
        var result = new List<DocumentChunk>();
        foreach (var docId in documentIds)
        {
            var document = await _documentRepository.GetByIdAsync(docId, cancellationToken);
            if (document?.Chunks == null || document.Chunks.Count == 0)
                continue;
            var scoredWithAll = document.Chunks
                .Where(c => !existingChunkIds.Contains(c.Id))
                .Select(c => new { Chunk = c, MatchCount = CountQueryWordMatches(c.Content, queryWords) })
                .OrderByDescending(x => x.MatchCount)
                .ThenBy(x => x.Chunk.ChunkIndex)
                .ToList();

            var scored = scoredWithAll
                .Where(x => x.MatchCount > 0)
                .Take(maxChunksPerDocument)
                .Select(x => x.Chunk)
                .ToList();
            foreach (var c in scored)
            {
                c.RelevanceScore = (c.RelevanceScore ?? 0.0) + 5.0;
                result.Add(c);
            }
        }
        return result.OrderBy(c => c.DocumentId).ThenBy(c => c.ChunkIndex).ToList();
    }

    private List<DocumentChunk> GetFilteredAndSortedNumberedListChunks(
        List<DocumentChunk> numberedListChunks,
        List<string> queryWords,
        bool hasQueryWords,
        int takeCount)
    {
        return numberedListChunks
            .Where(c => _queryPatternAnalyzer.DetectNumberedLists(c.Content) &&
                (hasQueryWords
                    ? queryWords.Any(word => c.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                    : !queryWords.Any(word => c.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)))
            .OrderByDescending(c => c.RelevanceScore ?? 0.0)
            .ThenByDescending(c => _queryPatternAnalyzer.CountNumberedListItems(c.Content))
            .Take(takeCount)
            .ToList();
    }
}
