using SmartRAG.Enums;

namespace SmartRAG.Demo.Handlers.DatabaseHandlers;

/// <summary>
/// Interface for database operation handlers
/// </summary>
public interface IDatabaseHandler
{
    Task ShowConnectionsAsync();
    Task ShowSchemasAsync();
    Task CreateDatabaseAsync(DatabaseType databaseType);
    Task RunHealthCheckAsync();
}

