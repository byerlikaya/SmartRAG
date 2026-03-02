using SmartRAG.Interfaces.Search;

namespace SmartRAG.Services.Document;


/// <summary>
/// Query source handler for MCP (Model Context Protocol) search. Delegates to IMcpIntegrationService and IRagContextAnswerGenerator.
/// </summary>
public class McpSearchHandler : IQuerySourceHandler
{
    private readonly IMcpIntegrationService _mcpIntegration;
    private readonly IRagContextAnswerGenerator _ragContextAnswerGenerator;
    private readonly IConversationManagerService _conversationManager;
    private readonly IResponseBuilderService _responseBuilder;
    private readonly ILogger<McpSearchHandler> _logger;

    public McpSearchHandler(
        IMcpIntegrationService mcpIntegration,
        IRagContextAnswerGenerator ragContextAnswerGenerator,
        IConversationManagerService conversationManager,
        IResponseBuilderService responseBuilder,
        ILogger<McpSearchHandler> logger)
    {
        _mcpIntegration = mcpIntegration ?? throw new ArgumentNullException(nameof(mcpIntegration));
        _ragContextAnswerGenerator = ragContextAnswerGenerator ?? throw new ArgumentNullException(nameof(ragContextAnswerGenerator));
        _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));
        _responseBuilder = responseBuilder ?? throw new ArgumentNullException(nameof(responseBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool CanHandle(SearchOptions options)
    {
        return options.EnableMcpSearch;
    }

    /// <inheritdoc />
    public async Task<RagResponse?> ExecuteAsync(QuerySourceHandlerRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var searchMetadata = request.SearchMetadata ?? new SearchMetadata();

        try
        {
            var mcpResults = await _mcpIntegration.QueryWithMcpAsync(
                request.Query,
                request.MaxResults,
                request.ConversationHistory ?? string.Empty,
                cancellationToken);
            searchMetadata.McpSearchPerformed = true;
            searchMetadata.McpResultsFound = mcpResults.Count(r => r.IsSuccess && !string.IsNullOrWhiteSpace(r.Content));

            if (mcpResults.Count == 0)
                return request.ExistingResponse;

            var mcpSources = mcpResults
                .Where(r => r.IsSuccess && !string.IsNullOrWhiteSpace(r.Content))
                .Select(r => new SearchSource
                {
                    SourceType = "MCP",
                    FileName = $"{r.ServerId}:{r.ToolName}",
                    RelevantContent = r.Content,
                    RelevanceScore = 1.0
                })
                .ToList();

            if (mcpSources.Count == 0)
                return request.ExistingResponse;

            var mcpContext = string.Join("\n\n", mcpResults.Where(r => r.IsSuccess).Select(r => r.Content));

            if (request.ExistingResponse != null)
            {
                request.ExistingResponse.Sources.AddRange(mcpSources);

                if (string.IsNullOrWhiteSpace(mcpContext))
                    return request.ExistingResponse;

                var existingContext = request.ExistingResponse.Sources
                    .Where(s => s.SourceType != "MCP")
                    .Select(s => s.RelevantContent)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .ToList();

                var combinedContext = existingContext.Count > 0
                    ? string.Join("\n\n", existingContext) + "\n\n[MCP Results]\n" + mcpContext
                    : mcpContext;

                var mergedAnswer = await _ragContextAnswerGenerator.GenerateRagAnswerFromContextAsync(
                    request.Query,
                    combinedContext,
                    request.ConversationHistory,
                    cancellationToken);
                if (!string.IsNullOrWhiteSpace(mergedAnswer))
                    request.ExistingResponse.Answer = mergedAnswer;

                return request.ExistingResponse;
            }

            var chatResponse = await _conversationManager.HandleGeneralConversationAsync(
                request.Query,
                request.ConversationHistory ?? string.Empty,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(mcpContext))
                return _responseBuilder.CreateRagResponse(request.Query, chatResponse, new List<SearchSource>(), searchMetadata);

            var mcpAnswer = await _ragContextAnswerGenerator.GenerateRagAnswerFromContextAsync(
                request.Query,
                mcpContext,
                request.ConversationHistory,
                cancellationToken);
            return _responseBuilder.CreateRagResponse(
                request.Query,
                !string.IsNullOrWhiteSpace(mcpAnswer) ? mcpAnswer : chatResponse,
                mcpSources,
                searchMetadata);
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogMcpQueryError(_logger, ex);
            return request.ExistingResponse;
        }
    }
}
