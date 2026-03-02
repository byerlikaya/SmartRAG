namespace SmartRAG.Services.Document;


/// <summary>
/// Orchestrates query intelligence: parses source tags, resolves session/intent, and delegates to source handlers (database, document, MCP).
/// </summary>
public class DocumentSearchService : IDocumentSearchService
{
    private const double SkipEagerDocumentAnswerConfidenceThreshold = 0.85;
    private const double DatabaseQueryConfidenceThreshold = 0.5;
    private const double StrongDocumentMatchThreshold = 4.8;

    private readonly SmartRagOptions _options;
    private readonly ILogger<DocumentSearchService> _logger;
    private readonly IRagAnswerGeneratorService _ragAnswerGenerator;
    private readonly IReadOnlyList<IQuerySourceHandler> _sourceHandlers;
    private readonly IConversationManagerService _conversationManager;
    private readonly IQueryIntentClassifierService _queryIntentClassifier;
    private readonly IQueryIntentAnalyzer _queryIntentAnalyzer;
    private readonly IResponseBuilderService _responseBuilder;
    private readonly IPromptBuilderService _promptBuilder;
    private readonly IAIService _aiService;

    public DocumentSearchService(
        IOptions<SmartRagOptions> options,
        ILogger<DocumentSearchService> logger,
        IRagAnswerGeneratorService ragAnswerGenerator,
        IEnumerable<IQuerySourceHandler> sourceHandlers,
        IConversationManagerService conversationManager,
        IQueryIntentClassifierService queryIntentClassifier,
        IQueryIntentAnalyzer queryIntentAnalyzer,
        IResponseBuilderService responseBuilder,
        IPromptBuilderService promptBuilder,
        IAIService aiService)
    {
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ragAnswerGenerator = ragAnswerGenerator ?? throw new ArgumentNullException(nameof(ragAnswerGenerator));
        _sourceHandlers = (sourceHandlers ?? throw new ArgumentNullException(nameof(sourceHandlers))).ToList();
        _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));
        _queryIntentClassifier = queryIntentClassifier ?? throw new ArgumentNullException(nameof(queryIntentClassifier));
        _queryIntentAnalyzer = queryIntentAnalyzer ?? throw new ArgumentNullException(nameof(queryIntentAnalyzer));
        _responseBuilder = responseBuilder ?? throw new ArgumentNullException(nameof(responseBuilder));
        _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
    }

    /// <inheritdoc />
    public async Task<RagResponse> QueryIntelligenceAsync(QueryIntelligenceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sessionId = request.SessionId;
        var conversationHistory = request.ConversationHistory;
        if (sessionId == null)
        {
            sessionId = await _conversationManager.GetOrCreateSessionIdAsync(cancellationToken);
            conversationHistory = await _conversationManager.GetConversationHistoryAsync(sessionId, cancellationToken);
        }
        else if (conversationHistory == null)
        {
            conversationHistory = string.Empty;
        }

        var query = request.Query;
        var maxResults = request.MaxResults;

        var baseOptions = SearchOptions.FromConfig(_options);
        var (cleanedQuery, searchOptions) = SourceTagParser.Parse(query, baseOptions);
        query = cleanedQuery;

        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be empty", nameof(request));

        var contentSearchEnabled = searchOptions.EnableDocumentSearch || searchOptions.EnableImageSearch || searchOptions.EnableAudioSearch;
        var documentSearchEnabled = searchOptions.EnableDocumentSearch;
        var databaseSearchEnabled = searchOptions.EnableDatabaseSearch;
        var mcpSearchEnabled = searchOptions.EnableMcpSearch;

        if (contentSearchEnabled)
        {
            var filenameResponse = await _ragAnswerGenerator.TryAnswerFromFilenameMatchAsync(query, searchOptions, conversationHistory, cancellationToken);
            if (filenameResponse != null)
            {
                await _conversationManager.AddToConversationAsync(sessionId, query, filenameResponse.Answer, cancellationToken);
                return filenameResponse;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var intentAnalysis = await _queryIntentClassifier.AnalyzeQueryAsync(query, conversationHistory, cancellationToken);

        if (intentAnalysis.IsConversation)
        {
            if (string.IsNullOrWhiteSpace(intentAnalysis.Answer))
                return await HandleConversationQueryAsync(query, sessionId, conversationHistory, cancellationToken);

            await _conversationManager.AddToConversationAsync(sessionId, query, intentAnalysis.Answer, cancellationToken);
            return _responseBuilder.CreateRagResponse(query, intentAnalysis.Answer, new List<SearchSource>());
        }

        RagResponse? response;
        var searchMetadata = new SearchMetadata();

        var queryTokens = intentAnalysis.Tokens.ToList();

        cancellationToken.ThrowIfCancellationRequested();
        bool canAnswer;
        List<DocumentChunk> results;
        if (documentSearchEnabled)
        {
            var canAnswerResult = await _ragAnswerGenerator.CanAnswerFromDocumentsAsync(query, searchOptions, queryTokens, cancellationToken);
            canAnswer = canAnswerResult.CanAnswer;
            results = canAnswerResult.Results;
        }
        else
        {
            canAnswer = false;
            results = new List<DocumentChunk>();
        }

        QueryIntent? preAnalyzedQueryIntent = null;
        if (databaseSearchEnabled)
            preAnalyzedQueryIntent = await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(query, cancellationToken);

        if (documentSearchEnabled)
        {
            searchMetadata.DocumentSearchPerformed = true;
            searchMetadata.DocumentChunksFound = results.Count;
        }

        var topScore = results.Count > 0 ? results.Max(r => r.RelevanceScore ?? 0) : 0;
        var hasStrongDocumentMatch = topScore > StrongDocumentMatchThreshold;

        var hasHighConfidenceForSkip = preAnalyzedQueryIntent?.Confidence > SkipEagerDocumentAnswerConfidenceThreshold;
        var hasDatabaseQueriesForSkip = preAnalyzedQueryIntent?.DatabaseQueries.Count > 0 && preAnalyzedQueryIntent.Confidence > DatabaseQueryConfidenceThreshold;
        var skipEagerDocumentAnswer = (!hasStrongDocumentMatch && hasHighConfidenceForSkip) || hasDatabaseQueriesForSkip;

        RagResponse? earlyDocumentResponse = null;

        var documentOnlyOptions = new SearchOptions
        {
            EnableDocumentSearch = true,
            EnableDatabaseSearch = false,
            EnableMcpSearch = false,
            EnableImageSearch = searchOptions.EnableImageSearch,
            EnableAudioSearch = searchOptions.EnableAudioSearch
        };

        var documentHandler = _sourceHandlers.FirstOrDefault(h => h.CanHandle(documentOnlyOptions));
        if (documentHandler != null && documentSearchEnabled && canAnswer && results.Count > 0 && !skipEagerDocumentAnswer)
        {
            var docHandlerRequest = CreateHandlerRequest(query, maxResults, conversationHistory, sessionId, searchOptions, searchMetadata, queryTokens, results, canAnswerFromDocuments: true);
            earlyDocumentResponse = await documentHandler.ExecuteAsync(docHandlerRequest, cancellationToken);

            if (preAnalyzedQueryIntent != null && preAnalyzedQueryIntent.DatabaseQueries.Count == 0)
                preAnalyzedQueryIntent = await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(query, cancellationToken);

            var indicatesMissingData = _responseBuilder.IndicatesMissingData(
                earlyDocumentResponse?.Answer ?? string.Empty,
                query,
                earlyDocumentResponse?.Sources ?? new List<SearchSource>());

            if (earlyDocumentResponse != null && !indicatesMissingData)
            {
                earlyDocumentResponse.SearchMetadata = searchMetadata;
                await _conversationManager.AddToConversationAsync(sessionId, query, earlyDocumentResponse.Answer, cancellationToken);
                return earlyDocumentResponse;
            }

            var documentSourcesWithContent = earlyDocumentResponse?.Sources?
                .Where(s => SearchSourceHelper.HasContentBearingSource(s) && !string.IsNullOrWhiteSpace(s.RelevantContent))
                .ToList() ?? new List<SearchSource>();
            var totalSourceContentLength = documentSourcesWithContent.Sum(s => s.RelevantContent?.Length ?? 0);

            if (documentSourcesWithContent.Count > 0 && totalSourceContentLength >= 50)
            {
                var extractionContext = string.Join("\n\n", documentSourcesWithContent.Select(s => s.RelevantContent));
                var extractionPrompt = _promptBuilder.BuildDocumentRagPrompt(query, extractionContext, extractionRetryMode: true);
                var retryAnswer = await _aiService.GenerateResponseAsync(extractionPrompt, new List<string> { extractionContext }, cancellationToken);
                if (!string.IsNullOrWhiteSpace(retryAnswer) &&
                    earlyDocumentResponse != null &&
                    !_responseBuilder.IndicatesMissingData(retryAnswer, query, earlyDocumentResponse.Sources))
                {
                    var retryResponse = _responseBuilder.CreateRagResponse(query, retryAnswer.Trim(), earlyDocumentResponse.Sources, searchMetadata);
                    await _conversationManager.AddToConversationAsync(sessionId, query, retryResponse.Answer, cancellationToken);
                    return retryResponse;
                }
            }

            if (!databaseSearchEnabled)
            {
                var hasNoDocumentContext = documentSourcesWithContent.Count == 0;
                RagResponse fallbackResponse;
                if (hasNoDocumentContext)
                {
                    fallbackResponse = _responseBuilder.CreateRagResponse(query, RagMessages.NoDocumentContext, new List<SearchSource>(), searchMetadata);
                }
                else
                {
                    var fallbackAnswer = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory, cancellationToken);
                    fallbackResponse = _responseBuilder.CreateRagResponse(query, fallbackAnswer, new List<SearchSource>(), searchMetadata);
                }
                await _conversationManager.AddToConversationAsync(sessionId, query, fallbackResponse.Answer, cancellationToken);
                return fallbackResponse;
            }
        }

        var dbOnlyOptions = SearchOptions.CreateDatabaseOnly(searchOptions);
        var dbHandler = _sourceHandlers.FirstOrDefault(h => h.CanHandle(dbOnlyOptions));
        if (dbHandler != null && databaseSearchEnabled)
        {
            try
            {
                var queryIntent = preAnalyzedQueryIntent ?? await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(query, cancellationToken);
                var handlerRequest = CreateHandlerRequest(query, maxResults, conversationHistory, sessionId, searchOptions, searchMetadata, queryTokens, results, canAnswer, queryIntent, queryIntent.DatabaseQueries.Count > 0);
                response = await dbHandler.ExecuteAsync(handlerRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogQueryIntentAnalysisError(_logger, ex);
                response = earlyDocumentResponse;
                if (response == null && documentHandler != null)
                {
                    var fallbackRequest = CreateHandlerRequest(query, maxResults, conversationHistory, sessionId, searchOptions, searchMetadata, queryTokens, results, canAnswer);
                    response = await documentHandler.ExecuteAsync(fallbackRequest, cancellationToken);
                }
            }

            if (response is { SearchMetadata: null })
                response.SearchMetadata = searchMetadata;
        }
        else
        {
            if (documentHandler != null)
            {
                var docRequest = CreateHandlerRequest(query, maxResults, conversationHistory, sessionId, searchOptions, searchMetadata, queryTokens, results, canAnswer);
                response = await documentHandler.ExecuteAsync(docRequest, cancellationToken);
            }
            else
            {
                var mcpOnlyOptions = SearchOptions.CreateMcpOnly(searchOptions);
                var mcpHandler = _sourceHandlers.FirstOrDefault(h => h.CanHandle(mcpOnlyOptions));
                if (mcpHandler != null)
                {
                    var mcpRequest = CreateHandlerRequest(query, maxResults, conversationHistory, sessionId, searchOptions, searchMetadata);
                    response = await mcpHandler.ExecuteAsync(mcpRequest, cancellationToken);
                    if (response != null)
                    {
                        await _conversationManager.AddToConversationAsync(sessionId, query, response.Answer, cancellationToken);
                        return response;
                    }
                }
                else
                {
                    var chatResponse = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory, cancellationToken);
                    response = _responseBuilder.CreateRagResponse(query, chatResponse, new List<SearchSource>());
                }
            }
        }

        var hasMeaningfulDatabaseData = response != null &&
            (response.Sources?.Any(s => s.SourceType == "Database") ?? false) &&
            (!string.IsNullOrWhiteSpace(response.Answer) || (response.Sources?.Any(s => s.SourceType == "Database" && !string.IsNullOrWhiteSpace(s.RelevantContent)) ?? false));

        var databaseAnswerIsSufficient = hasMeaningfulDatabaseData && response != null && !string.IsNullOrWhiteSpace(response.Answer);

        var hasDocumentAnswer = response != null && !string.IsNullOrWhiteSpace(response.Answer) &&
            (response.Sources?.Any(SearchSourceHelper.HasContentBearingSource) ?? false);

        var documentAnswerIsSufficient = hasDocumentAnswer && response != null &&
            !_responseBuilder.IndicatesMissingData(response.Answer, query);

        var answerIsSufficient = databaseAnswerIsSufficient || documentAnswerIsSufficient;
        var mcpAlreadyPerformed = searchMetadata.McpSearchPerformed;

        if (mcpSearchEnabled && !mcpAlreadyPerformed && !answerIsSufficient)
        {
            var mcpOnlyOpts = SearchOptions.CreateMcpOnly(searchOptions);
            var mcpHandler = _sourceHandlers.FirstOrDefault(h => h.CanHandle(mcpOnlyOpts));
            if (mcpHandler != null)
            {
                var mcpRequest = CreateHandlerRequest(query, maxResults, conversationHistory, sessionId, searchOptions, searchMetadata, existingResponse: response);
                var mcpResponse = await mcpHandler.ExecuteAsync(mcpRequest, cancellationToken);
                if (mcpResponse != null)
                    response = mcpResponse;
            }
        }

        if (response != null)
        {
            response.SearchMetadata ??= searchMetadata;
            await _conversationManager.AddToConversationAsync(sessionId, query, response.Answer, cancellationToken);
            return response;
        }

        var finalFallbackResponse = _responseBuilder.CreateRagResponse(query, RagMessages.NoDocumentContext, new List<SearchSource>(), searchMetadata);
        await _conversationManager.AddToConversationAsync(sessionId, query, finalFallbackResponse.Answer, cancellationToken);
        return finalFallbackResponse;
    }

    private QuerySourceHandlerRequest CreateHandlerRequest(
        string query,
        int maxResults,
        string? conversationHistory,
        string? sessionId,
        SearchOptions searchOptions,
        SearchMetadata searchMetadata,
        List<string>? queryTokens = null,
        List<DocumentChunk>? preCalculatedResults = null,
        bool? canAnswerFromDocuments = null,
        QueryIntent? queryIntent = null,
        bool? hasDatabaseQueries = null,
        RagResponse? existingResponse = null)
    {
        return new QuerySourceHandlerRequest
        {
            Query = query,
            MaxResults = maxResults,
            ConversationHistory = conversationHistory,
            SessionId = sessionId,
            Options = searchOptions,
            SearchMetadata = searchMetadata,
            PreferredLanguage = _options.DefaultLanguage,
            QueryTokens = queryTokens,
            PreCalculatedResults = preCalculatedResults,
            CanAnswerFromDocuments = canAnswerFromDocuments,
            QueryIntent = queryIntent,
            HasDatabaseQueries = hasDatabaseQueries,
            ExistingResponse = existingResponse
        };
    }

    private async Task<RagResponse> HandleConversationQueryAsync(string query, string sessionId, string? conversationHistory, CancellationToken cancellationToken = default)
    {
        var conversationAnswer = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory, cancellationToken);
        await _conversationManager.AddToConversationAsync(sessionId, query, conversationAnswer, cancellationToken);
        return _responseBuilder.CreateRagResponse(query, conversationAnswer, new List<SearchSource>());
    }
}
