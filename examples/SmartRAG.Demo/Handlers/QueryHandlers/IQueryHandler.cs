namespace SmartRAG.Demo.Handlers.QueryHandlers;

/// <summary>
/// Interface for query operation handlers
/// </summary>
public interface IQueryHandler
{
    Task RunMultiDatabaseQueryAsync(string language);
    Task AnalyzeQueryIntentAsync(string language);
    Task RunTestQueriesAsync(string language);
    Task RunMultiModalQueryAsync(string language, bool useLocalEnvironment, string aiProvider);
}

