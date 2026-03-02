using SmartRAG.Interfaces.Search;
using SmartRAG.Models.Schema;

namespace SmartRAG.Services.Document;


/// <summary>
/// Query source handler for document/audio/image RAG. Delegates to DocumentRagService (IRagAnswerGeneratorService).
/// </summary>
public class DocumentRagSourceHandler : IQuerySourceHandler
{
    private readonly IRagAnswerGeneratorService _ragAnswerGenerator;

    public DocumentRagSourceHandler(IRagAnswerGeneratorService ragAnswerGenerator)
    {
        _ragAnswerGenerator = ragAnswerGenerator ?? throw new ArgumentNullException(nameof(ragAnswerGenerator));
    }

    /// <inheritdoc />
    public bool CanHandle(SearchOptions options)
    {
        return options.EnableDocumentSearch || options.EnableImageSearch || options.EnableAudioSearch;
    }

    /// <inheritdoc />
    public async Task<RagResponse?> ExecuteAsync(QuerySourceHandlerRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ragRequest = new GenerateRagAnswerRequest
        {
            Query = request.Query,
            MaxResults = request.MaxResults,
            ConversationHistory = request.ConversationHistory ?? string.Empty,
            PreferredLanguage = request.PreferredLanguage,
            Options = request.Options,
            PreCalculatedResults = request.PreCalculatedResults,
            QueryTokens = request.QueryTokens
        };

        var response = await _ragAnswerGenerator.GenerateBasicRagAnswerAsync(ragRequest, cancellationToken);
        if (request.SearchMetadata != null)
            response.SearchMetadata = request.SearchMetadata;
        return response;
    }
}
