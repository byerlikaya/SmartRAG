using SmartRAG.Interfaces.Search;
using SmartRAG.Models.Schema;

namespace SmartRAG.Services.Document;


/// <summary>
/// Query source handler for database search. Delegates to QueryStrategyOrchestrator and QueryStrategyExecutor.
/// </summary>
public class DatabaseQuerySourceHandler : IQuerySourceHandler
{
    private readonly IQueryIntentAnalyzer _queryIntentAnalyzer;
    private readonly IQueryStrategyOrchestratorService _strategyOrchestrator;
    private readonly IQueryStrategyExecutorService _strategyExecutor;
    private readonly SmartRagOptions _options;

    public DatabaseQuerySourceHandler(
        IQueryIntentAnalyzer queryIntentAnalyzer,
        IQueryStrategyOrchestratorService strategyOrchestrator,
        IQueryStrategyExecutorService strategyExecutor,
        IOptions<SmartRagOptions> options)
    {
        _queryIntentAnalyzer = queryIntentAnalyzer ?? throw new ArgumentNullException(nameof(queryIntentAnalyzer));
        _strategyOrchestrator = strategyOrchestrator ?? throw new ArgumentNullException(nameof(strategyOrchestrator));
        _strategyExecutor = strategyExecutor ?? throw new ArgumentNullException(nameof(strategyExecutor));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public bool CanHandle(SearchOptions options)
    {
        return options.EnableDatabaseSearch;
    }

    /// <inheritdoc />
    public async Task<RagResponse?> ExecuteAsync(QuerySourceHandlerRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var queryIntent = request.QueryIntent ?? await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(request.Query, cancellationToken);
        var confidence = queryIntent.Confidence;
        var hasDatabaseQueries = queryIntent.DatabaseQueries.Count > 0;
        var canAnswer = request.CanAnswerFromDocuments ?? true;
        var strategy = _strategyOrchestrator.DetermineQueryStrategy(confidence, hasDatabaseQueries, canAnswer);
        var hasDatabaseQueriesForRequest = strategy == QueryStrategy.Hybrid ? (bool?)hasDatabaseQueries : null;

        var strategyRequest = new QueryStrategyRequest
        {
            Query = request.Query,
            MaxResults = request.MaxResults,
            ConversationHistory = request.ConversationHistory ?? string.Empty,
            CanAnswerFromDocuments = canAnswer,
            HasDatabaseQueries = hasDatabaseQueriesForRequest,
            QueryIntent = queryIntent,
            PreferredLanguage = request.PreferredLanguage ?? _options.DefaultLanguage,
            Options = request.Options,
            PreCalculatedResults = request.PreCalculatedResults,
            QueryTokens = request.QueryTokens
        };

        var response = strategy switch
        {
            QueryStrategy.DatabaseOnly => await _strategyExecutor.ExecuteDatabaseOnlyStrategyAsync(strategyRequest, cancellationToken),
            QueryStrategy.DocumentOnly => await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(strategyRequest, cancellationToken),
            QueryStrategy.Hybrid => await _strategyExecutor.ExecuteHybridStrategyAsync(strategyRequest, cancellationToken),
            _ => await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(strategyRequest, cancellationToken)
        };

        if (request.SearchMetadata != null)
        {
            response.SearchMetadata ??= request.SearchMetadata;
            if (strategy is QueryStrategy.DatabaseOnly or QueryStrategy.Hybrid)
            {
                request.SearchMetadata.DatabaseSearchPerformed = true;
                request.SearchMetadata.DatabaseResultsFound = response.Sources.Count(s => s.SourceType == "Database");
            }
        }

        return response;
    }
}
