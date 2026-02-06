
namespace SmartRAG.Demo.Services.TestQuery;

/// <summary>
/// Service for generating test queries
/// </summary>
public interface ITestQueryGenerator
{
    Task<List<Models.TestQuery>> GenerateTestQueriesAsync(string language);
    Task<List<Models.TestQuery>> GenerateAITestQueriesAsync(List<DatabaseSchemaInfo> schemas, string language);
    Task<List<Models.TestQuery>> GenerateSchemaBasedQueriesAsync(List<DatabaseSchemaInfo> schemas, string language);
}

