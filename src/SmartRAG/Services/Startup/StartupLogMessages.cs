namespace SmartRAG.Services.Startup;

/// <summary>
/// LoggerMessage delegates for SmartRagStartupService
/// </summary>
public static class StartupLogMessages
{
    public static readonly Action<ILogger, Exception> LogMcpInit = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(6001, "McpInit"),
        "Initializing MCP connections...");

    public static readonly Action<ILogger, Exception> LogMcpServiceNotFound = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(6002, "McpServiceNotFound"),
        "IMcpConnectionManager service not found. MCP client may not be properly registered.");

    public static readonly Action<ILogger, Exception> LogMcpDisabled = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(6003, "McpDisabled"),
        "MCP client is disabled in configuration");

    public static readonly Action<ILogger, Exception> LogFileWatcherInit = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(6004, "FileWatcherInit"),
        "Initializing file watchers...");

    public static readonly Action<ILogger, string, Exception> LogFileWatcherFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(6005, "FileWatcherFailed"),
        "Failed to start watching folder: {FolderPath}");

    public static readonly Action<ILogger, Exception> LogDatabaseInitBackground = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(6006, "DatabaseInitBackground"),
        "Initializing database connections and schema analysis (background)...");

    public static readonly Action<ILogger, Exception> LogDatabaseInitFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(6007, "DatabaseInitFailed"),
        "Database connection manager initialization failed; schema scanning may be skipped.");
}
