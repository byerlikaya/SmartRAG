namespace SmartRAG.Demo.Handlers.QueryHandlers;

/// <summary>
/// Interface for query operation handlers
/// </summary>
public interface IQueryHandler
{
    Task RunMultiDatabaseQueryAsync(string language);
    Task AnalyzeQueryIntentAsync(string language);
    Task RunTestQueriesAsync(string language);
    Task RunConversationalChatAsync(string language, bool useLocalEnvironment, string aiProvider);
    Task RunMcpQueryAsync(string language);
    Task ClearConversationHistoryAsync();
}

