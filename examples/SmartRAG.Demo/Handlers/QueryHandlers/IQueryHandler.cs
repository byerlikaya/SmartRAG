
namespace SmartRAG.Demo.Handlers.QueryHandlers;

/// <summary>
/// Interface for query operation handlers
/// </summary>
public interface IQueryHandler
{
    Task RunMultiDatabaseQueryAsync(string language, CancellationToken cancellationToken = default);
    Task AnalyzeQueryIntentAsync(string language, CancellationToken cancellationToken = default);
    Task RunTestQueriesAsync(string language, CancellationToken cancellationToken = default);
    Task RunConversationalChatAsync(string language, bool useLocalEnvironment, string aiProvider, CancellationToken cancellationToken = default);
    Task RunMcpQueryAsync(string language, CancellationToken cancellationToken = default);
    Task ClearConversationHistoryAsync(CancellationToken cancellationToken = default);
}

