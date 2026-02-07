namespace SmartRAG.Services.Database;


/// <summary>
/// Centralized LoggerMessage delegates for database services
/// </summary>
public static class DatabaseLogMessages
{
    public static readonly Action<ILogger, Exception> LogConnectionManagerAlreadyInitialized = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36001, "ConnectionManagerAlreadyInitialized"),
        "DatabaseConnectionManager already initialized");

    public static readonly Action<ILogger, Exception> LogNoDatabaseConnectionsConfigured = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(36002, "NoDatabaseConnectionsConfigured"),
        "No database connections configured");

    public static readonly Action<ILogger, Exception> LogRegisteredDatabaseConnection = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(36003, "RegisteredDatabaseConnection"),
        "Registered database connection");

    public static readonly Action<ILogger, Exception> LogSchemaAnalysisStarted = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(36004, "SchemaAnalysisStarted"),
        "Starting schema analysis");

    public static readonly Action<ILogger, Exception> LogSchemaAnalysisCompleted = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(36005, "SchemaAnalysisCompleted"),
        "Schema analysis completed");

    public static readonly Action<ILogger, Exception> LogSchemaAnalysisFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36006, "SchemaAnalysisFailed"),
        "Schema analysis failed");

    public static readonly Action<ILogger, Exception> LogDatabaseConnectionInitFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36007, "DatabaseConnectionInitFailed"),
        "Failed to initialize database connection");

    public static readonly Action<ILogger, Exception> LogCrossDatabaseMappingsDetectFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36008, "CrossDatabaseMappingsDetectFailed"),
        "Failed to detect cross-database mappings");

    public static readonly Action<ILogger, Exception> LogSchemaMigrationStarted = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(36009, "SchemaMigrationStarted"),
        "Starting schema migration to vector store");

    public static readonly Action<ILogger, int, Exception> LogSchemaMigrationCompleted = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(36010, "SchemaMigrationCompleted"),
        "Schema migration completed: {MigratedCount} schemas migrated");

    public static readonly Action<ILogger, Exception> LogSchemaMigrationFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36011, "SchemaMigrationFailed"),
        "Schema migration failed, continuing without schema chunks");

    public static readonly Action<ILogger, int, Exception> LogConnectionManagerInitialized = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(36012, "ConnectionManagerInitialized"),
        "Database connection manager initialized with {Count} connections");

    public static readonly Action<ILogger, int, Exception> LogManualCrossDatabaseMappingsFound = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(36013, "ManualCrossDatabaseMappingsFound"),
        "Found {Count} manually configured cross-database mappings in appsettings.json");

    public static readonly Action<ILogger, string, string, string, string, string, string, Exception> LogCrossDatabaseMappingAdded = LoggerMessage.Define<string, string, string, string, string, string>(
        LogLevel.Information,
        new EventId(36014, "CrossDatabaseMappingAdded"),
        "Auto-detected and added cross-database mapping: {SourceDB}.{SourceTable}.{SourceColumn} -> {TargetDB}.{TargetTable}.{TargetColumn}");

    public static readonly Action<ILogger, string, string, string, string, Exception> LogCrossDatabaseMappingSkipped = LoggerMessage.Define<string, string, string, string>(
        LogLevel.Debug,
        new EventId(36015, "CrossDatabaseMappingSkipped"),
        "Skipped auto-detected mapping (already exists in appsettings.json): {SourceDB}.{SourceColumn} -> {TargetDB}.{TargetColumn}");

    public static readonly Action<ILogger, int, int, int, Exception> LogTotalCrossDatabaseMappings = LoggerMessage.Define<int, int, int>(
        LogLevel.Information,
        new EventId(36016, "TotalCrossDatabaseMappings"),
        "Total cross-database mappings: {Total} ({Manual} manual + {Auto} auto-detected)");

    public static readonly Action<ILogger, string?, Exception> LogDatabaseConnectionRegisterFailed = LoggerMessage.Define<string?>(
        LogLevel.Warning,
        new EventId(36017, "DatabaseConnectionRegisterFailed"),
        "Failed to register database connection: {Name}");

    public static readonly Action<ILogger, Exception> LogDatabaseConnectionNotFound = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36018, "DatabaseConnectionNotFound"),
        "Database connection not found");

    public static readonly Action<ILogger, Exception> LogDatabaseValidationFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36019, "DatabaseValidationFailed"),
        "Validation failed for database");

    public static readonly Action<ILogger, Exception> LogDatabaseNameExtractFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36020, "DatabaseNameExtractFailed"),
        "Could not extract database name, using GUID");

}
