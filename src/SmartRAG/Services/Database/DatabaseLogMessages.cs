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

    public static readonly Action<ILogger, string, string, string, Exception> LogRemovingInvalidJoinClause = LoggerMessage.Define<string, string, string>(
        LogLevel.Warning,
        new EventId(36068, "RemovingInvalidJoinClause"),
        "Removing invalid JOIN clause: {TableRef} (table not in schema for {Database}). Alias {Alias} references removed.");

    public static readonly Action<ILogger, Exception> LogAnswerFallbackToRawData = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(36069, "AnswerFallbackToRawData"),
        "AI returned failure message but merged data contains rows; returning raw data as answer");

    public static readonly Action<ILogger, string, string, string, Exception> LogInjectedMissingMappingColumn = LoggerMessage.Define<string, string, string>(
        LogLevel.Debug,
        new EventId(36070, "InjectedMissingMappingColumn"),
        "Injected missing mapping column {Column} into SQL for database {DatabaseName}. SQL: {Sql}");

    public static readonly Action<ILogger, string, string, Exception> LogSkippedMappingColumnNotInSchema = LoggerMessage.Define<string, string>(
        LogLevel.Information,
        new EventId(36071, "SkippedMappingColumnNotInSchema"),
        "Skipped mapping columns not in schema for database {DatabaseName}: {Columns}");

    public static readonly Action<ILogger, string, string, string, int, Exception> LogInjectedSourceValuesIntoTargetQuery = LoggerMessage.Define<string, string, string, int>(
        LogLevel.Debug,
        new EventId(36071, "InjectedSourceValuesIntoTargetQuery"),
        "Two-phase execution: Injected {Count} values from {SourceDb}.{SourceColumn} into {TargetDb} WHERE clause");

    public static readonly Action<ILogger, string, Exception> LogExecutingSqlForDatabase = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(36072, "ExecutingSqlForDatabase"),
        "Executing SQL for database {DatabaseName}");

    public static readonly Action<ILogger, string, Exception> LogExecutingSqlWithQuery = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(36092, "ExecutingSqlWithQuery"),
        "Executing SQL for database {DatabaseName}");

    public static readonly Action<ILogger, string, Exception> LogQueryExecutionFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(36073, "QueryExecutionFailed"),
        "Error executing query on database {DatabaseName}");

    public static readonly Action<ILogger, string, string, string, Exception> LogRemovingInvalidSubqueryReference = LoggerMessage.Define<string, string, string>(
        LogLevel.Warning,
        new EventId(36074, "RemovingInvalidSubqueryReference"),
        "Removing invalid subquery: {Column} IN (SELECT ... FROM {TableRef}) - table not in schema for {DatabaseName}");

    public static readonly Action<ILogger, bool, int, Exception> LogTwoPhaseMappingCheck = LoggerMessage.Define<bool, int>(
        LogLevel.Debug,
        new EventId(36075, "TwoPhaseMappingCheck"),
        "[INS] HasMappingDependency={HasMapping}, AllMappingsCount={Count}");

    public static readonly Action<ILogger, int, string, Exception> LogTwoPhaseLoopStart = LoggerMessage.Define<int, string>(
        LogLevel.Debug,
        new EventId(36076, "TwoPhaseLoopStart"),
        "[INS] Two-phase loop iteration {Index}: database {DatabaseName}");

    public static readonly Action<ILogger, bool, int, Exception> LogTwoPhaseInjectDecision = LoggerMessage.Define<bool, int>(
        LogLevel.Debug,
        new EventId(36077, "TwoPhaseInjectDecision"),
        "[INS] Will inject: {WillInject}, sourceValues key count={Count}");

    public static readonly Action<ILogger, string, bool, int, Exception> LogTwoPhaseAfterExecution = LoggerMessage.Define<string, bool, int>(
        LogLevel.Debug,
        new EventId(36078, "TwoPhaseAfterExecution"),
        "[INS] After execution {DatabaseName}: Success={Success}, RowCount={RowCount}");

    public static readonly Action<ILogger, string, int, Exception> LogTwoPhaseAfterExtract = LoggerMessage.Define<string, int>(
        LogLevel.Debug,
        new EventId(36079, "TwoPhaseAfterExtract"),
        "[INS] After ExtractMappingColumnValues for {DatabaseName}: sourceValues has {KeyCount} keys");

    public static readonly Action<ILogger, string, int, int, Exception> LogExtractMappingEntry = LoggerMessage.Define<string, int, int>(
        LogLevel.Debug,
        new EventId(36080, "ExtractMappingEntry"),
        "[INS] ExtractMappingColumnValues: db={DatabaseName}, resultDataLen={Len}, mappingsCount={Count}");

    public static readonly Action<ILogger, string, int, Exception> LogExtractMappingHeaders = LoggerMessage.Define<string, int>(
        LogLevel.Debug,
        new EventId(36081, "ExtractMappingHeaders"),
        "[INS] ExtractMapping headers: {Headers}, headerIndex={Index}");

    public static readonly Action<ILogger, string, string, int, Exception> LogExtractMappingColumnsToExtract = LoggerMessage.Define<string, string, int>(
        LogLevel.Debug,
        new EventId(36082, "ExtractMappingColumnsToExtract"),
        "[INS] ExtractMapping sourceMappings for {DatabaseName}, columnsToExtract=[{Columns}], count={Count}");

    public static readonly Action<ILogger, string, int, bool, Exception> LogExtractMappingColumnLookup = LoggerMessage.Define<string, int, bool>(
        LogLevel.Debug,
        new EventId(36083, "ExtractMappingColumnLookup"),
        "[INS] ExtractMapping column {ColName}: colIndex={Index}, found={Found}");

    public static readonly Action<ILogger, string, int, Exception> LogExtractMappingValuesExtracted = LoggerMessage.Define<string, int>(
        LogLevel.Debug,
        new EventId(36084, "ExtractMappingValuesExtracted"),
        "[INS] ExtractMapping key {Key}: extracted {Count} values");

    public static readonly Action<ILogger, string, Exception> LogExtractMappingNoHeaders = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(36085, "ExtractMappingNoHeaders"),
        "[INS] ExtractMapping for {DatabaseName}: no headers found, aborting");

    public static readonly Action<ILogger, string, int, Exception> LogInjectEntry = LoggerMessage.Define<string, int>(
        LogLevel.Debug,
        new EventId(36086, "InjectEntry"),
        "[INS] InjectSourceValuesIntoTargetQuery: targetDb={TargetDb}, targetMappingsCount={Count}");

    public static readonly Action<ILogger, string, Exception> LogInjectSourceValuesKeys = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(36087, "InjectSourceValuesKeys"),
        "[INS] Inject sourceValues keys: [{Keys}]");

    public static readonly Action<ILogger, string, bool, string, int, Exception> LogInjectKeyLookup = LoggerMessage.Define<string, bool, string, int>(
        LogLevel.Debug,
        new EventId(36088, "InjectKeyLookup"),
        "[INS] Inject key {Key}: found={Found}, altKey={AltKey}, valuesCount={Count}");

    public static readonly Action<ILogger, bool, Exception> LogInjectRegexMatch = LoggerMessage.Define<bool>(
        LogLevel.Debug,
        new EventId(36089, "InjectRegexMatch"),
        "[INS] Inject IN clause regex match: {Success}");

    public static readonly Action<ILogger, Exception> LogInjectNoMatchSkipped = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(36090, "InjectNoMatchSkipped"),
        "[INS] Inject: no IN clause match, mapping skipped");

    public static readonly Action<ILogger, string, Exception> LogDatabaseFileParsingStarted = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(36093, "DatabaseFileParsingStarted"),
        "Starting database file parsing for: {FileName}");

    public static readonly Action<ILogger, string, Exception> LogDatabaseFileParsingCompleted = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(36094, "DatabaseFileParsingCompleted"),
        "Database file parsing completed for: {FileName}");

    public static readonly Action<ILogger, string, Exception> LogDatabaseFileParsingFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(36095, "DatabaseFileParsingFailed"),
        "Error parsing database file: {FileName}");

    public static readonly Action<ILogger, string, Exception> LogTempFileDeleteFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(36096, "TempFileDeleteFailed"),
        "Failed to delete temporary file: {TempPath}");

    public static readonly Action<ILogger, string, Exception> LogDatabaseConnectionParsingStarted = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(36097, "DatabaseConnectionParsingStarted"),
        "Starting database connection parsing for type: {DatabaseType}");

    public static readonly Action<ILogger, string, Exception> LogDatabaseConnectionParsingFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(36098, "DatabaseConnectionParsingFailed"),
        "Error parsing database connection for type: {DatabaseType}");

    public static readonly Action<ILogger, string, Exception> LogDatabaseConnectionValidationFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(36099, "DatabaseConnectionValidationFailed"),
        "Database connection validation failed for type: {DatabaseType}");

    public static readonly Action<ILogger, int, Exception> LogProcessingTablesSqlite = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(36100, "ProcessingTablesSqlite"),
        "Processing {TableCount} tables from SQLite database");

    public static readonly Action<ILogger, string, Exception> LogErrorProcessingTable = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(36101, "ErrorProcessingTable"),
        "Error processing table: {TableName}");

    public static readonly Action<ILogger, int, Exception> LogProcessingTablesSqlServer = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(36102, "ProcessingTablesSqlServer"),
        "Processing {TableCount} tables from SQL Server database");

    public static readonly Action<ILogger, Exception> LogSqlServerDatabaseNotExistYet = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36103, "SqlServerDatabaseNotExistYet"),
        "SQL Server database does not exist yet");

    public static readonly Action<ILogger, Exception> LogSqlServerParsingFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36104, "SqlServerParsingFailed"),
        "Error parsing SQL Server database");

    public static readonly Action<ILogger, Exception> LogDatabaseNotExistEmptyTableList = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(36105, "DatabaseNotExistEmptyTableList"),
        "Database does not exist yet, returning empty table list");

    public static readonly Action<ILogger, Exception> LogSqlServerTableNamesFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36106, "SqlServerTableNamesFailed"),
        "Error getting SQL Server table names");

    public static readonly Action<ILogger, Exception> LogSqlServerQueryExecutionNotExist = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36107, "SqlServerQueryExecutionNotExist"),
        "SQL Server database does not exist yet for query execution");

    public static readonly Action<ILogger, Exception> LogSqlServerQueryExecutionFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36108, "SqlServerQueryExecutionFailed"),
        "Error executing SQL Server query");

    public static readonly Action<ILogger, string, Exception> LogSqlServerAccessibleDbNotExist = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(36109, "SqlServerAccessibleDbNotExist"),
        "SQL Server is accessible but database '{TargetDatabase}' does not exist yet. This is expected for first-time setup.");

    public static readonly Action<ILogger, Exception> LogSqlServerValidationNotAccessible = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36110, "SqlServerValidationNotAccessible"),
        "SQL Server validation failed - server not accessible");

    public static readonly Action<ILogger, int, string, Exception> LogSqlServerConnectionValidationFailed = LoggerMessage.Define<int, string>(
        LogLevel.Warning,
        new EventId(36111, "SqlServerConnectionValidationFailed"),
        "SQL Server connection validation failed with error {ErrorNumber}: {ErrorMessage}");

    public static readonly Action<ILogger, Exception> LogSqlServerConnectionValidationFailedGeneric = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36112, "SqlServerConnectionValidationFailedGeneric"),
        "SQL Server connection validation failed");

    public static readonly Action<ILogger, int, Exception> LogProcessingTablesMySql = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(36113, "ProcessingTablesMySql"),
        "Processing {TableCount} tables from MySQL database");

    public static readonly Action<ILogger, int, Exception> LogProcessingTablesPostgres = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(36114, "ProcessingTablesPostgres"),
        "Processing {TableCount} tables from PostgreSQL database");

    public static readonly Action<ILogger, Exception> LogNoSchemasToMigrate = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(36115, "NoSchemasToMigrate"),
        "No schemas found to migrate");

    public static readonly Action<ILogger, string, string, Exception> LogSchemaChunksExistSkipping = LoggerMessage.Define<string, string>(
        LogLevel.Debug,
        new EventId(36116, "SchemaChunksExistSkipping"),
        "Schema chunks already exist for database {DatabaseName} ({DatabaseId}), skipping");

    public static readonly Action<ILogger, string, string, Exception> LogMigratingSchema = LoggerMessage.Define<string, string>(
        LogLevel.Information,
        new EventId(36117, "MigratingSchema"),
        "Migrating schema for database {DatabaseName} ({DatabaseId})");

    public static readonly Action<ILogger, string, string, Exception> LogNoChunksGenerated = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(36118, "NoChunksGenerated"),
        "No chunks generated for database {DatabaseName} ({DatabaseId})");

    public static readonly Action<ILogger, string, string, int, Exception> LogSuccessfullyMigratedSchema = LoggerMessage.Define<string, string, int>(
        LogLevel.Information,
        new EventId(36119, "SuccessfullyMigratedSchema"),
        "Successfully migrated schema for database {DatabaseName} ({DatabaseId}) - {ChunkCount} chunks");

    public static readonly Action<ILogger, string, string, Exception> LogFailedToMigrateSchema = LoggerMessage.Define<string, string>(
        LogLevel.Error,
        new EventId(36120, "FailedToMigrateSchema"),
        "Failed to migrate schema for database {DatabaseName} ({DatabaseId})");

    public static readonly Action<ILogger, int, int, Exception> LogSchemaMigrationCompletedCount = LoggerMessage.Define<int, int>(
        LogLevel.Information,
        new EventId(36121, "SchemaMigrationCompletedCount"),
        "Schema migration completed: {MigratedCount} out of {TotalCount} schemas migrated");

    public static readonly Action<ILogger, Exception> LogFailedToMigrateSchemas = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36122, "FailedToMigrateSchemas"),
        "Failed to migrate schemas");

    public static readonly Action<ILogger, string, Exception> LogFailedToCheckSchemaExists = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(36123, "FailedToCheckSchemaExists"),
        "Failed to check if schema exists for database {DatabaseId}");

    public static readonly Action<ILogger, string, string, Exception> LogCrossDatabaseTableReference = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(36124, "CrossDatabaseTableReference"),
        "Detected cross-database table reference: {Reference} in database {Database}");

    public static readonly Action<ILogger, string, string, string, Exception> LogCrossDatabaseReferenceDetected = LoggerMessage.Define<string, string, string>(
        LogLevel.Warning,
        new EventId(36125, "CrossDatabaseReferenceDetected"),
        "Detected potential cross-database reference: {Reference} in database {Database}, possibly referencing {OtherDatabase}");

    public static readonly Action<ILogger, string, string, Exception> LogTableNotInRequiredList = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(36126, "TableNotInRequiredList"),
        "Table '{Table}' exists in database '{Database}' but was not in the required tables list.");

    public static readonly Action<ILogger, string, string, Exception> LogTableNotInRequiredListAllowProceed = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(36127, "TableNotInRequiredListAllowProceed"),
        "Table '{Table}' exists in database '{Database}' but was not in the required tables list. Allowing query to proceed. This may indicate QueryIntentAnalyzer needs improvement.");

    public static readonly Action<ILogger, Exception> LogNoDatabaseSchemasAvailable = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36128, "NoDatabaseSchemasAvailable"),
        "No database schemas available for query analysis");

    public static readonly Action<ILogger, Exception> LogCouldNotFindJsonInAiResponse = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36129, "CouldNotFindJsonInAiResponse"),
        "Could not find JSON in AI response");

    public static readonly Action<ILogger, Exception> LogLessThanTwoDatabasesNoMappings = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(36130, "LessThanTwoDatabasesNoMappings"),
        "Less than 2 databases configured, no cross-database mappings to detect");

    public static readonly Action<ILogger, Exception> LogLessThanTwoSchemasNoMappings = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(36131, "LessThanTwoSchemasNoMappings"),
        "Less than 2 schemas available, no cross-database mappings to detect");

    public static readonly Action<ILogger, int, Exception> LogDetectedCrossDatabaseMappings = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(36132, "DetectedCrossDatabaseMappings"),
        "Detected {Count} cross-database mappings");

    public static readonly Action<ILogger, string, string, string, string, string, string, Exception> LogDetectedMapping = LoggerMessage.Define<string, string, string, string, string, string>(
        LogLevel.Debug,
        new EventId(36133, "DetectedMapping"),
        "Detected mapping: {SourceDB}.{SourceTable}.{SourceColumn} -> {TargetDB}.{TargetTable}.{TargetColumn}");

    public static readonly Action<ILogger, string, string, string, string, string, string, Exception> LogDetectedFkMapping = LoggerMessage.Define<string, string, string, string, string, string>(
        LogLevel.Debug,
        new EventId(36134, "DetectedFkMapping"),
        "Detected FK mapping: {SourceDB}.{SourceTable}.{SourceColumn} -> {TargetDB}.{TargetTable}.{TargetColumn}");

    public static readonly Action<ILogger, Exception> LogSchemaNotFoundForDatabase = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36135, "SchemaNotFoundForDatabase"),
        "Schema not found for database");

    public static readonly Action<ILogger, string, string, Exception> LogAiAttemptedAddTableToDatabase = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(36136, "AiAttemptedAddTableToDatabase"),
        "AI attempted to add table '{Table}' to '{Database}', but it doesn't exist there. Skipping.");

    public static readonly Action<ILogger, Exception> LogAiSelectedNonExistentDatabase = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36137, "AiSelectedNonExistentDatabase"),
        "AI selected non-existent database");

    public static readonly Action<ILogger, Exception> LogErrorParsingAiResponseJson = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36138, "ErrorParsingAiResponseJson"),
        "Error parsing AI response JSON");

    public static readonly Action<ILogger, Exception> LogUnexpectedErrorParsingAiResponse = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36139, "UnexpectedErrorParsingAiResponse"),
        "Unexpected error parsing AI response");

    public static readonly Action<ILogger, Exception> LogFailedToRetrieveCrossDatabaseMappings = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36140, "FailedToRetrieveCrossDatabaseMappings"),
        "Failed to retrieve cross-database mappings");

    public static readonly Action<ILogger, Exception> LogErrorProcessingMultiDatabaseQuery = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36141, "ErrorProcessingMultiDatabaseQuery"),
        "Error processing multi-database query");

    public static readonly Action<ILogger, string, Exception> LogAiSelectedInvalidTables = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(36142, "AiSelectedInvalidTables"),
        "AI selected invalid tables: {InvalidTables}");

    public static readonly Action<ILogger, Exception> LogNoValidTablesRemoving = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36143, "NoValidTablesRemoving"),
        "No valid tables, removing");

    public static readonly Action<ILogger, string, Exception> LogTableNotFoundInAnyDatabase = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(36144, "TableNotFoundInAnyDatabase"),
        "Table '{Table}' not found in any database");

    public static readonly Action<ILogger, string, Exception> LogSchemaNoTablesToConvert = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(36145, "SchemaNoTablesToConvert"),
        "Schema {DatabaseName} has no tables to convert");

    public static readonly Action<ILogger, int, string, Exception> LogCreatedSchemaChunks = LoggerMessage.Define<int, string>(
        LogLevel.Information,
        new EventId(36146, "CreatedSchemaChunks"),
        "Created {Count} schema chunks for database {DatabaseName}");

    public static readonly Action<ILogger, int, int, Exception> LogSchemaChunksEmbeddingMismatch = LoggerMessage.Define<int, int>(
        LogLevel.Error,
        new EventId(36147, "SchemaChunksEmbeddingMismatch"),
        "Failed to generate embeddings for schema chunks. Expected {Expected}, got {Actual}");

    public static readonly Action<ILogger, int, Exception> LogGeneratedEmbeddingsForSchemaChunks = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(36148, "GeneratedEmbeddingsForSchemaChunks"),
        "Generated embeddings for {Count} schema chunks");

    public static readonly Action<ILogger, Exception> LogErrorAnalyzingSchemaForDatabase = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36149, "ErrorAnalyzingSchemaForDatabase"),
        "Error analyzing schema for database");

    public static readonly Action<ILogger, Exception> LogFailedToAnalyzeSchemaForConnection = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36150, "FailedToAnalyzeSchemaForConnection"),
        "Failed to analyze schema for configured database connection");

    public static readonly Action<ILogger, Exception> LogCouldNotOpenConnectionToExtractDbName = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36151, "CouldNotOpenConnectionToExtractDbName"),
        "Could not open connection to extract database name, using connection string info");

    public static readonly Action<ILogger, Exception> LogCouldNotExtractDatabaseNameFromConnection = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36152, "CouldNotExtractDatabaseNameFromConnection"),
        "Could not extract database name from connection string");

    public static readonly Action<ILogger, string, Exception> LogSqlServerDatabaseNotExistForTable = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(36153, "SqlServerDatabaseNotExistForTable"),
        "SQL Server database does not exist yet for table {TableName}");

    public static readonly Action<ILogger, string, Exception> LogErrorAnalyzingSqlServerTable = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(36154, "ErrorAnalyzingSqlServerTable"),
        "Error analyzing SQL Server table {TableName}");

    public static readonly Action<ILogger, string, Exception> LogErrorAnalyzingTable = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(36155, "ErrorAnalyzingTable"),
        "Error analyzing table {TableName}");

    public static readonly Action<ILogger, string, Exception> LogCouldNotRetrievePrimaryKeyForTable = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(36156, "CouldNotRetrievePrimaryKeyForTable"),
        "Could not retrieve primary key information for table {TableName}");

    public static readonly Action<ILogger, string, Exception> LogErrorGettingSqliteColumnsForTable = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(36157, "ErrorGettingSqliteColumnsForTable"),
        "Error getting SQLite columns for table {TableName}");

    public static readonly Action<ILogger, string, Exception> LogCouldNotRetrieveForeignKeysForTable = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(36158, "CouldNotRetrieveForeignKeysForTable"),
        "Could not retrieve foreign keys for table {TableName}");

    public static readonly Action<ILogger, string, Exception> LogCouldNotRetrieveSqliteForeignKeysForTable = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(36159, "CouldNotRetrieveSqliteForeignKeysForTable"),
        "Could not retrieve SQLite foreign keys for table {TableName}");

    public static readonly Action<ILogger, string, Exception> LogCouldNotGetRowCountForTable = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(36160, "CouldNotGetRowCountForTable"),
        "Could not get row count for table {TableName}");

    public static readonly Action<ILogger, string, Exception> LogCouldNotGetSampleDataForTable = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(36161, "CouldNotGetSampleDataForTable"),
        "Could not get sample data for table {TableName}");

}
