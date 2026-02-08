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

    public static readonly Action<ILogger, Exception> LogSmartMergeFallbackToSeparate = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36021, "SmartMergeFallbackToSeparate"),
        "Smart merge failed, falling back to separate results");

    public static readonly Action<ILogger, Exception> LogFinalAnswerGenerationFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36022, "FinalAnswerGenerationFailed"),
        "Error generating final answer");

    public static readonly Action<ILogger, Exception> LogQueryResultParseNoHeader = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36023, "QueryResultParseNoHeader"),
        "Could not parse query result - no header found");

    public static readonly Action<ILogger, Exception> LogQueryResultParseFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36024, "QueryResultParseFailed"),
        "Error parsing query result");

    public static readonly Action<ILogger, Exception> LogMergeSuccessfulMappingTargetMissing = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(36025, "MergeSuccessfulMappingTargetMissing"),
        "Merge successful using mapping when target database result was missing");

    public static readonly Action<ILogger, Exception> LogNoJoinableRelationships = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36026, "NoJoinableRelationships"),
        "No joinable relationships found between databases");

    public static readonly Action<ILogger, Exception> LogSmartMergeNoMatchingRows = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36027, "SmartMergeNoMatchingRows"),
        "Smart merge failed: No matching rows found after join attempt");

    public static readonly Action<ILogger, Exception> LogRetryMergeSuccessful = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(36028, "RetryMergeSuccessful"),
        "Retry merge successful with filtered query");

    public static readonly Action<ILogger, Exception> LogSmartMergeFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36029, "SmartMergeFailed"),
        "Error in smart merge");

    public static readonly Action<ILogger, int, Exception> LogMappingBasedMatchesFound = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(36030, "MappingBasedMatchesFound"),
        "Found mapping-based matches across {Count} databases");

    public static readonly Action<ILogger, string, int, Exception> LogCommonJoinColumnFound = LoggerMessage.Define<string, int>(
        LogLevel.Information,
        new EventId(36031, "CommonJoinColumnFound"),
        "Found common join column: {JoinColumn} across {Count} databases");

    public static readonly Action<ILogger, int, Exception> LogValueBasedMatchesFound = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(36032, "ValueBasedMatchesFound"),
        "Found value-based matches across {Count} databases");

    public static readonly Action<ILogger, string, string, Exception> LogSourceColumnNotFound = LoggerMessage.Define<string, string>(
        LogLevel.Debug,
        new EventId(36033, "SourceColumnNotFound"),
        "FindMappingBasedMatchesAsync: Source column '{SourceColumn}' not found in result columns: {AvailableColumns}");

    public static readonly Action<ILogger, string, string, Exception> LogNoMappingsFound = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(36034, "NoMappingsFound"),
        "FindMappingBasedMatchesAsync: No mappings found. Source columns available: {SourceColumns}, Target columns available: {TargetColumns}");

    public static readonly Action<ILogger, int, Exception> LogPotentialMappingsFound = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(36035, "PotentialMappingsFound"),
        "FindMappingBasedMatchesAsync: Found {Count} potential mappings");

    public static readonly Action<ILogger, string, string, string, string, Exception> LogMappingBasedMappingUsed = LoggerMessage.Define<string, string, string, string>(
        LogLevel.Information,
        new EventId(36036, "MappingBasedMappingUsed"),
        "FindMappingBasedMatchesAsync: Using mapping {SourceDb}.[{SourceCol}] → {TargetDb}.[{TargetCol}]");

    public static readonly Action<ILogger, Exception> LogMappingBasedMatchesFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36037, "MappingBasedMatchesFailed"),
        "Error finding mapping-based matches");

    public static readonly Action<ILogger, int, string, string, Exception> LogValueBasedMatchFound = LoggerMessage.Define<int, string, string>(
        LogLevel.Information,
        new EventId(36038, "ValueBasedMatchFound"),
        "Found value-based match: {MatchCount} matching values between {Col1} and {Col2}");

    public static readonly Action<ILogger, int, int, int, int, Exception> LogPerformInMemoryJoinCompleted = LoggerMessage.Define<int, int, int, int>(
        LogLevel.Information,
        new EventId(36039, "PerformInMemoryJoinCompleted"),
        "PerformInMemoryJoin completed: Processed={Processed}, Matched={Matched}, Skipped={Skipped}, Final rows={FinalRows}");

    public static readonly Action<ILogger, string, Exception> LogPerformInMemoryJoinNoMatchingRows = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(36040, "PerformInMemoryJoinNoMatchingRows"),
        "PerformInMemoryJoin: No matching rows found. Base values sample: {SampleValues}");

    public static readonly Action<ILogger, int, string, Exception> LogRetryMergeIdValuesFound = LoggerMessage.Define<int, string>(
        LogLevel.Information,
        new EventId(36041, "RetryMergeIdValuesFound"),
        "RetryMergeWithFilteredQueryAsync: Found {Count} ID values: {Ids}");

    public static readonly Action<ILogger, string, Exception> LogRetryMergeConnectionNotFound = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(36042, "RetryMergeConnectionNotFound"),
        "RetryMergeWithFilteredQueryAsync: Connection not found for {Database}");

    public static readonly Action<ILogger, string, string, Exception> LogRetryMergeMappingNotFound = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(36043, "RetryMergeMappingNotFound"),
        "RetryMergeWithFilteredQueryAsync: Mapping not found for {Database}.{Column} in any connection");

    public static readonly Action<ILogger, string, string, string, string, string, string, Exception> LogRetryMergeMappingFound = LoggerMessage.Define<string, string, string, string, string, string>(
        LogLevel.Information,
        new EventId(36044, "RetryMergeMappingFound"),
        "RetryMergeWithFilteredQueryAsync: Found mapping {SourceDb}.{SourceTable}.{SourceCol} → {TargetDb}.{TargetTable}.{TargetCol}");

    public static readonly Action<ILogger, Exception> LogRetryingMergeWithFilteredQuery = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(36045, "RetryingMergeWithFilteredQuery"),
        "Retrying merge with filtered query");

    public static readonly Action<ILogger, Exception> LogRetryMergeFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36046, "RetryMergeFailed"),
        "Error in retry merge with filtered query");

    public static readonly Action<ILogger, string, string, string, string, string, string, Exception> LogMappingTargetMissingGeneratingQuery = LoggerMessage.Define<string, string, string, string, string, string>(
        LogLevel.Information,
        new EventId(36047, "MappingTargetMissingGeneratingQuery"),
        "Found mapping {SourceDb}.{SourceTable}.{SourceCol} → {TargetDb}.{TargetTable}.{TargetCol}, but target database result is missing. Generating filtered query...");

    public static readonly Action<ILogger, string, Exception> LogDescriptiveColumnsFallback = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(36048, "DescriptiveColumnsFallback"),
        "Could not determine descriptive columns for table {Table}, using join column only");

    public static readonly Action<ILogger, Exception> LogExecutingFilteredQuery = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(36049, "ExecutingFilteredQuery"),
        "Executing filtered query for missing target database");

    public static readonly Action<ILogger, int, Exception> LogMergedResultsMappingTargetMissing = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(36050, "MergedResultsMappingTargetMissing"),
        "Successfully merged results using mapping when target database was missing. Merged {RowCount} rows.");

    public static readonly Action<ILogger, Exception> LogMergeWithMappingTargetMissingFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36051, "MergeWithMappingTargetMissingFailed"),
        "Error trying merge with mapping when target missing");

    public static readonly Action<ILogger, string, string, Exception> LogSchemaColumnCheckFailed = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(36052, "SchemaColumnCheckFailed"),
        "Error checking schema for column {ColumnName} in database {DatabaseId}");

    public static readonly Action<ILogger, int, string, Exception> LogExecutingDatabaseQueries = LoggerMessage.Define<int, string>(
        LogLevel.Information,
        new EventId(36053, "ExecutingDatabaseQueries"),
        "Executing {Count} database queries in priority order: {Order}");

    public static readonly Action<ILogger, string, string, Exception> LogDatabaseRequiresMappingColumns = LoggerMessage.Define<string, string>(
        LogLevel.Information,
        new EventId(36054, "DatabaseRequiresMappingColumns"),
        "Database {DatabaseName} requires mapping columns: {Columns}");

    public static readonly Action<ILogger, Exception> LogNoValidSchemasFound = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36055, "NoValidSchemasFound"),
        "No valid schemas found for any database");

    public static readonly Action<ILogger, int, Exception> LogSendingMultiDatabasePrompt = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(36056, "SendingMultiDatabasePrompt"),
        "Sending separated multi-database prompt to AI for {DatabaseCount} databases");

    public static readonly Action<ILogger, Exception> LogAIResponseReceived = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(36057, "AIResponseReceived"),
        "AI response received for multi-database SQL generation");

    public static readonly Action<ILogger, string, Exception> LogExtractSqlFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(36058, "ExtractSqlFailed"),
        "Failed to extract SQL for database {DatabaseId}");

    public static readonly Action<ILogger, string, string, string, Exception> LogAIGeneratedInvalidSql = LoggerMessage.Define<string, string, string>(
        LogLevel.Error,
        new EventId(36059, "AIGeneratedInvalidSql"),
        "AI generated invalid SQL for database {DatabaseName}. SQL: {Sql}. Errors: {Errors}");

    public static readonly Action<ILogger, string, string, string, Exception> LogSqlMissingMappingColumns = LoggerMessage.Define<string, string, string>(
        LogLevel.Warning,
        new EventId(36060, "SqlMissingMappingColumns"),
        "Generated SQL missing required mapping columns for database {DatabaseName}. SQL: {Sql}. Missing: {Missing}");

    public static readonly Action<ILogger, Exception> LogAIResponseEmpty = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36061, "AIResponseEmpty"),
        "AI response is empty, cannot extract SQL");

    public static readonly Action<ILogger, string, Exception> LogFailedToExtractAnySql = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(36062, "FailedToExtractAnySql"),
        "Failed to extract any SQL from AI response. Response preview: {Preview}");

    public static readonly Action<ILogger, string, Exception> LogErrorGettingRequiredMappingColumns = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(36063, "ErrorGettingRequiredMappingColumns"),
        "Error getting required mapping columns for database {DatabaseName}");

    public static readonly Action<ILogger, string, Exception> LogErrorValidatingMappingColumns = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(36064, "ErrorValidatingMappingColumns"),
        "Error validating cross-database mapping columns for database {DatabaseName}");

    public static readonly Action<ILogger, string, string, Exception> LogRemovingDatabasePrefix = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(36065, "RemovingDatabasePrefix"),
        "Removing database prefix from table reference: {Full} -> {Table}");

    public static readonly Action<ILogger, string, string, Exception> LogRemovingCrossDatabaseReference = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(36066, "RemovingCrossDatabaseReference"),
        "Removing entire cross-database table reference: {Full} (table not found in {Database})");

    public static readonly Action<ILogger, Exception> LogDetectedInvalidFromClause = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36067, "DetectedInvalidFromClause"),
        "Detected invalid FROM clause after removing cross-database reference. Attempting to fix SQL.");

}
