namespace SmartRAG.Services.Database;


/// <summary>
/// Generates optimized SQL queries for databases based on query intent
/// </summary>
public class SQLQueryGenerator : ISqlQueryGenerator
{
    private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer;
    private readonly IAIService _aiService;
    private readonly ISqlDialectStrategyFactory _strategyFactory;
    private readonly ISqlValidator _validator;
    private readonly ISqlPromptBuilder _promptBuilder;
    private readonly IDatabaseConnectionManager _connectionManager;
    private readonly ILogger<SQLQueryGenerator> _logger;

    /// <summary>
    /// Initializes a new instance of the SQLQueryGenerator
    /// </summary>
    /// <param name="schemaAnalyzer">Database schema analyzer</param>
    /// <param name="aiService">AI service for query generation</param>
    /// <param name="strategyFactory">SQL dialect strategy factory</param>
    /// <param name="validator">SQL validator</param>
    /// <param name="promptBuilder">SQL prompt builder</param>
    /// <param name="connectionManager">Database connection manager</param>
    /// <param name="logger">Logger instance</param>
    public SQLQueryGenerator(
        IDatabaseSchemaAnalyzer schemaAnalyzer,
        IAIService aiService,
        ISqlDialectStrategyFactory strategyFactory,
        ISqlValidator validator,
        ISqlPromptBuilder promptBuilder,
        IDatabaseConnectionManager connectionManager,
        ILogger<SQLQueryGenerator> logger)
    {
        _schemaAnalyzer = schemaAnalyzer ?? throw new ArgumentNullException(nameof(schemaAnalyzer));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// [AI Query] Generates optimized SQL queries for each database based on intent
    /// </summary>
    public async Task<QueryIntent> GenerateDatabaseQueriesAsync(QueryIntent queryIntent, CancellationToken cancellationToken = default)
    {
        queryIntent.DatabaseQueries = queryIntent.DatabaseQueries.OrderBy(q => q.Priority).ToList();

        DatabaseLogMessages.LogExecutingDatabaseQueries(_logger,
            queryIntent.DatabaseQueries.Count,
            string.Join(" → ", queryIntent.DatabaseQueries.Select(q => $"{q.DatabaseName}(priority:{q.Priority})")),
            null);

        var schemas = new Dictionary<string, DatabaseSchemaInfo>();

        var strategies = new Dictionary<string, ISqlDialectStrategy>();
        var requiredMappingColumns = new Dictionary<string, List<string>>();

        var targetDatabaseNames = queryIntent.DatabaseQueries
            .Select(q => q.DatabaseName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var targetDatabaseRequiredTables = queryIntent.DatabaseQueries
            .ToDictionary(q => q.DatabaseName, q => q.RequiredTables ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

        var databaseNameToPriority = queryIntent.DatabaseQueries
            .ToDictionary(q => q.DatabaseName, q => q.Priority, StringComparer.OrdinalIgnoreCase);

        foreach (var dbQuery in queryIntent.DatabaseQueries)
        {
            var schema = await _schemaAnalyzer.GetSchemaAsync(dbQuery.DatabaseId, cancellationToken);

            schemas[dbQuery.DatabaseId] = schema;
            strategies[dbQuery.DatabaseId] = _strategyFactory.GetStrategy(schema.DatabaseType);
            var rawRequired = await GetRequiredMappingColumnsAsync(
                schema.DatabaseName,
                dbQuery.RequiredTables,
                targetDatabaseNames,
                targetDatabaseRequiredTables,
                databaseNameToPriority);
            requiredMappingColumns[dbQuery.DatabaseId] = FilterRequiredMappingColumnsBySchema(rawRequired, schema);
            if (rawRequired.Count > requiredMappingColumns[dbQuery.DatabaseId].Count)
            {
                var skipped = rawRequired.Except(requiredMappingColumns[dbQuery.DatabaseId], StringComparer.OrdinalIgnoreCase);
                DatabaseLogMessages.LogSkippedMappingColumnNotInSchema(_logger, schema.DatabaseName, string.Join(", ", skipped), null);
            }

            if (requiredMappingColumns[dbQuery.DatabaseId].Any())
            {
                DatabaseLogMessages.LogDatabaseRequiresMappingColumns(_logger,
                    schema.DatabaseName, string.Join(", ", requiredMappingColumns[dbQuery.DatabaseId]), null);
                EnsureMappingSourceTablesInRequiredTables(dbQuery, schema, requiredMappingColumns[dbQuery.DatabaseId]);
            }
        }

        if (schemas.Count == 0)
        {
            DatabaseLogMessages.LogNoValidSchemasFound(_logger, null);
            return queryIntent;
        }

        var promptParts = _promptBuilder.BuildMultiDatabaseSeparated(
            queryIntent.OriginalQuery,
            queryIntent,
            schemas,
            strategies);

        var additionalInstructions = new StringBuilder();
        foreach (var kvp in requiredMappingColumns.Where(k => k.Value.Any()))
        {
            additionalInstructions.AppendLine($"\n🚨 CRITICAL FOR {schemas[kvp.Key].DatabaseName}:");
            additionalInstructions.AppendLine("MAPPING COLUMNS REQUIRED - MUST include in SELECT and GROUP BY (if aggregating):");
            foreach (var col in kvp.Value)
            {
                additionalInstructions.AppendLine($"  • {col}");
            }
            additionalInstructions.AppendLine("  → You MUST use the table(s) containing these columns in your FROM/JOIN!");
        }

        if (additionalInstructions.Length > 0)
        {
            promptParts.UserMessage += "\n" + additionalInstructions;
        }

        DatabaseLogMessages.LogSendingMultiDatabasePrompt(_logger, queryIntent.DatabaseQueries.Count, null);

        var context = new List<string> { promptParts.SystemMessage };
        var aiResponse = await _aiService.GenerateResponseAsync(promptParts.UserMessage, context, cancellationToken);
        DatabaseLogMessages.LogAIResponseReceived(_logger, null);

        var databaseSqls = ExtractMultiDatabaseSQL(aiResponse, queryIntent.DatabaseQueries, schemas);

        foreach (var dbQuery in queryIntent.DatabaseQueries)
        {
            if (!databaseSqls.TryGetValue(dbQuery.DatabaseId, out var extractedSql) || string.IsNullOrEmpty(extractedSql))
            {
                DatabaseLogMessages.LogExtractSqlFailed(_logger, dbQuery.DatabaseId, null);
                dbQuery.GeneratedQuery = null;
                continue;
            }

            var schema = schemas[dbQuery.DatabaseId];
            var strategy = strategies[dbQuery.DatabaseId];

            extractedSql = strategy.FormatSql(extractedSql, schema);

            if (schema.DatabaseType == DatabaseType.MySQL)
            {
                extractedSql = FixMySQLCommonColumnMistakes(extractedSql, schema);
                extractedSql = FixAliasTableColumnToAliasColumn(extractedSql, schema);
            }

            extractedSql = StripDatabaseNamePrefixWhenSameDatabase(extractedSql, schema);
            extractedSql = DetectAndFixCrossDatabaseReferences(extractedSql, schema, dbQuery.RequiredTables);
            extractedSql = RemoveInvalidSubqueryReferences(extractedSql, schema);
            extractedSql = RemoveInvalidJoinClauses(extractedSql, schema);
            extractedSql = FixUndefinedAliasReferences(extractedSql, schema);
            extractedSql = FixEmptySelectList(extractedSql, schema, requiredMappingColumns[dbQuery.DatabaseId]);
            extractedSql = await ReplaceSourceColumnWithTargetInTargetDatabaseAsync(extractedSql, schema);
            extractedSql = FixConcatenatedTableReferences(extractedSql, schema);
            extractedSql = FixColumnNameUsedAsFunction(extractedSql, schema);
            extractedSql = ReplaceInvalidColumnReferences(extractedSql, schema);
            extractedSql = FixInvalidColumnsInAggregatesAndAddMissingJoins(extractedSql, schema);
            extractedSql = AddMissingSelectColumnsToGroupBy(extractedSql, schema);
            extractedSql = RemoveInvalidColumnsFromSelectAndGroupBy(extractedSql, schema);
            extractedSql = FixOrderByReferenceToRemovedAlias(extractedSql, schema);

            extractedSql = AddMissingMappingTableJoins(extractedSql, schema, requiredMappingColumns[dbQuery.DatabaseId]);
            extractedSql = InjectMissingMappingColumns(extractedSql, schema, requiredMappingColumns[dbQuery.DatabaseId]);
            extractedSql = InjectDescriptiveColumnsForTargetQuery(extractedSql, schema, requiredMappingColumns[dbQuery.DatabaseId]);
            extractedSql = FixAmbiguousColumnsInJoin(extractedSql, schema);

            if (schema.DatabaseType == DatabaseType.SqlServer)
            {
                extractedSql = FixUnboundAliasReferences(extractedSql, schema);
                extractedSql = FixAmbiguousColumnsInJoin(extractedSql, schema);
            }

            extractedSql = FixAmbiguousColumnsInJoin(extractedSql, schema);

            var allDatabaseNames = schemas.Values.Select(s => s.DatabaseName).Distinct().ToList();
            if (!ValidateSql(extractedSql, schema, dbQuery.RequiredTables, strategy, allDatabaseNames, out var validationErrors))
            {
                DatabaseLogMessages.LogAIGeneratedInvalidSql(_logger,
                    schema.DatabaseName, extractedSql, string.Join("; ", validationErrors), null);
                dbQuery.GeneratedQuery = null;
                continue;
            }

            var mappingErrors = await ValidateCrossDatabaseMappingColumnsAsync(
                extractedSql,
                schema.DatabaseName,
                dbQuery.RequiredTables,
                targetDatabaseNames,
                targetDatabaseRequiredTables,
                databaseNameToPriority);
            if (mappingErrors.Any())
            {
                var retrySql = AddMissingMappingTableJoins(extractedSql, schema, mappingErrors);
                retrySql = InjectMissingMappingColumns(retrySql, schema, mappingErrors);
                if (mappingErrors.Any(m => !Regex.IsMatch(retrySql, $@"\b{Regex.Escape(m.Split('.').Last())}\b", RegexOptions.IgnoreCase)))
                    retrySql = ForceInjectMissingMappingColumns(retrySql, schema, mappingErrors);
                var retryErrors = await ValidateCrossDatabaseMappingColumnsAsync(
                    retrySql, schema.DatabaseName, dbQuery.RequiredTables,
                    targetDatabaseNames, targetDatabaseRequiredTables, databaseNameToPriority);
                if (!retryErrors.Any())
                {
                    extractedSql = retrySql;
                    mappingErrors = retryErrors;
                }
            }
            if (mappingErrors.Any())
            {
                DatabaseLogMessages.LogSqlMissingMappingColumns(_logger,
                    schema.DatabaseName, extractedSql, string.Join(", ", mappingErrors), null);
                dbQuery.GeneratedQuery = null;
                continue;
            }

            dbQuery.GeneratedQuery = extractedSql;
        }

        return queryIntent;
    }

    private Dictionary<string, string> ExtractMultiDatabaseSQL(string response, List<DatabaseQueryIntent> databaseQueries, Dictionary<string, DatabaseSchemaInfo> schemas)
    {
        var result = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(response))
        {
            DatabaseLogMessages.LogAIResponseEmpty(_logger, null);
            return result;
        }

        response = Regex.Replace(response, @"```sql\s*", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        response = Regex.Replace(response, @"```\s*", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string currentDatabase = null;
        var currentSql = new List<string>();
        var inSql = false;
        var waitingForConfirmed = true;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            if (trimmedLine.StartsWith("```", StringComparison.OrdinalIgnoreCase))
                continue;

            if (trimmedLine.StartsWith("###", StringComparison.OrdinalIgnoreCase))
            {
                var dbHeaderMatch = Regex.Match(trimmedLine, @"###\s+DATABASE\s+(\d+):\s*(.+)", RegexOptions.IgnoreCase);
                if (dbHeaderMatch.Success)
                {
                    if (currentDatabase != null && currentSql.Any())
                    {
                        var sql = ExtractCompleteSQL(string.Join(" ", currentSql));
                        if (!string.IsNullOrWhiteSpace(sql))
                        {
                            result[currentDatabase] = sql;
                        }
                    }

                    var dbIndex = int.Parse(dbHeaderMatch.Groups[1].Value) - 1;
                    if (dbIndex >= 0 && dbIndex < databaseQueries.Count)
                    {
                        currentDatabase = databaseQueries[dbIndex].DatabaseId;
                        currentSql.Clear();
                        inSql = false;
                        waitingForConfirmed = true;
                    }
                    else
                    {
                        currentDatabase = null;
                        currentSql.Clear();
                        inSql = false;
                        waitingForConfirmed = true;
                    }
                }
                continue;
            }

            var dbMatch = Regex.Match(trimmedLine, @"^DATABASE\s+(\d+):\s*(.+)$", RegexOptions.IgnoreCase);
            if (dbMatch.Success)
            {
                    if (currentDatabase != null && currentSql.Any())
                    {
                        var sql = ExtractCompleteSQL(string.Join(" ", currentSql));
                        if (!string.IsNullOrWhiteSpace(sql))
                        {
                            result[currentDatabase] = sql;
                        }
                    }

                    var dbIndex = int.Parse(dbMatch.Groups[1].Value) - 1;
                    var dbNameFromResponse = dbMatch.Groups[2].Value.Trim();

                    if (dbIndex >= 0 && dbIndex < databaseQueries.Count)
                    {
                        currentDatabase = databaseQueries[dbIndex].DatabaseId;
                        currentSql.Clear();
                        inSql = false;
                        waitingForConfirmed = true;
                    }
                    else if (!string.IsNullOrWhiteSpace(dbNameFromResponse) && !dbNameFromResponse.Equals("DatabaseName", StringComparison.OrdinalIgnoreCase))
                    {
                        var matchingDbQuery = databaseQueries.FirstOrDefault(q =>
                            schemas[q.DatabaseId].DatabaseName.Equals(dbNameFromResponse, StringComparison.OrdinalIgnoreCase));
                        if (matchingDbQuery != null)
                        {
                            currentDatabase = matchingDbQuery.DatabaseId;
                            currentSql.Clear();
                            inSql = false;
                            waitingForConfirmed = true;
                        }
                    }
                    else
                    {
                        currentDatabase = null;
                        currentSql.Clear();
                        inSql = false;
                        waitingForConfirmed = true;
                    }
                    continue;
            }

            if (trimmedLine.Equals("CONFIRMED", StringComparison.OrdinalIgnoreCase))
            {
                if (currentDatabase != null)
                {
                    inSql = true;
                    waitingForConfirmed = false;
                }
                continue;
            }

            if (currentDatabase != null && waitingForConfirmed &&
                (trimmedLine.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                 trimmedLine.StartsWith("WITH", StringComparison.OrdinalIgnoreCase)))
            {
                inSql = true;
                waitingForConfirmed = false;
                currentSql.Clear();
                currentSql.Add(trimmedLine);
                continue;
            }

            if (trimmedLine.StartsWith("CORRECTION:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (trimmedLine.StartsWith("Explanation:", StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.StartsWith("Note:", StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.StartsWith("Note that", StringComparison.OrdinalIgnoreCase))
            {
                if (currentDatabase != null && currentSql.Any())
                {
                    inSql = false;
                }
                continue;
            }

            if (currentDatabase == null || (!inSql && waitingForConfirmed))
                continue;

            if (trimmedLine.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            {
                inSql = true;
                currentSql.Clear();
                currentSql.Add(trimmedLine);
            }
            else if (inSql && currentSql.Any())
            {
                var lowerLine = trimmedLine.ToLowerInvariant();
                if (lowerLine.StartsWith("select") ||
                    lowerLine.StartsWith("from") ||
                    lowerLine.StartsWith("where") ||
                    lowerLine.StartsWith("join") ||
                    lowerLine.StartsWith("inner") ||
                    lowerLine.StartsWith("left") ||
                    lowerLine.StartsWith("right") ||
                    lowerLine.StartsWith("on") ||
                    lowerLine.StartsWith("group") ||
                    lowerLine.StartsWith("order") ||
                    lowerLine.StartsWith("having") ||
                    lowerLine.StartsWith("limit") ||
                    lowerLine.StartsWith("top") ||
                    lowerLine.StartsWith("as") ||
                    lowerLine.StartsWith("and") ||
                    lowerLine.StartsWith("or") ||
                    lowerLine.Contains("(") ||
                    lowerLine.Contains(")") ||
                    lowerLine.Contains(",") ||
                    lowerLine.Contains("=") ||
                    lowerLine.Contains("'") ||
                    trimmedLine.EndsWith(";") ||
                    char.IsDigit(trimmedLine[0]) || trimmedLine.Length > 50 && !lowerLine.Contains("database") && !lowerLine.Contains("explanation"))
                {
                    currentSql.Add(trimmedLine);
                }
                else
                {
                    var sql = ExtractCompleteSQL(string.Join(" ", currentSql));
                    if (!string.IsNullOrWhiteSpace(sql) && sql.Length > 20)
                    {
                        result[currentDatabase] = sql;
                    }
                    inSql = false;
                }
            }
        }

        if (currentDatabase != null && currentSql.Any())
        {
            var sql = ExtractCompleteSQL(string.Join(" ", currentSql));
            if (!string.IsNullOrWhiteSpace(sql))
            {
                result[currentDatabase] = sql;
            }
        }

        if (result.Count != 0)
            return result;

        result = ExtractSqlBlocksWithoutDatabaseMarkers(response, databaseQueries, schemas);
        if (result.Count == 0)
            DatabaseLogMessages.LogFailedToExtractAnySql(_logger,
                response?.Substring(0, Math.Min(500, response?.Length ?? 0)) ?? string.Empty, null);

        return result;
    }

    private Dictionary<string, string> ExtractSqlBlocksWithoutDatabaseMarkers(string response, List<DatabaseQueryIntent>? databaseQueries, Dictionary<string, DatabaseSchemaInfo> schemas)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(response) || databaseQueries == null || databaseQueries.Count == 0)
            return result;

        response = Regex.Replace(response, @"```sql\s*", "", RegexOptions.IgnoreCase);
        response = Regex.Replace(response, @"```\s*", "", RegexOptions.IgnoreCase);

        var parts = Regex.Split(response, @"\s*;\s*", RegexOptions.Multiline);
        var selectBlocks = new List<string>();

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase)) continue;
            var sql = ExtractCompleteSQL(trimmed);
            if (!string.IsNullOrWhiteSpace(sql) && sql.Length > 20)
                selectBlocks.Add(sql);
        }

        if (selectBlocks.Count == 0)
        {
            var single = ExtractCompleteSQL(response);
            if (!string.IsNullOrWhiteSpace(single) && single.Length > 20)
                selectBlocks.Add(single);
        }

        for (var i = 0; i < selectBlocks.Count && i < databaseQueries.Count; i++)
        {
            var dbQuery = databaseQueries[i];
            var sql = selectBlocks[i];
            var dbId = dbQuery.DatabaseId;

            var bestSchema = schemas.GetValueOrDefault(dbId);
            var bestScore = bestSchema != null
                ? bestSchema.Tables.Count(t => sql.IndexOf(t.TableName, StringComparison.OrdinalIgnoreCase) >= 0)
                : 0;

            foreach (var kv in schemas)
            {
                if (kv.Key == dbId || result.ContainsKey(kv.Key)) continue;
                var score = kv.Value.Tables.Count(t => sql.IndexOf(t.TableName, StringComparison.OrdinalIgnoreCase) >= 0);
                if (score <= bestScore)
                    continue;

                bestScore = score;
                dbId = kv.Key;
            }

            result.TryAdd(dbId, sql);
        }

        return result;
    }

    private static void EnsureMappingSourceTablesInRequiredTables(DatabaseQueryIntent dbQuery, DatabaseSchemaInfo schema, List<string> requiredMappingColumns)
    {
        if (dbQuery.RequiredTables == null || requiredMappingColumns.Count == 0)
            return;

        var existingTables = new HashSet<string>(dbQuery.RequiredTables, StringComparer.OrdinalIgnoreCase);
        foreach (var colRef in requiredMappingColumns)
        {
            var parts = colRef.Split('.');
            if (parts.Length < 2)
                continue;

            var tableRef = string.Join(".", parts.Take(parts.Length - 1));
            var tableInSchema = schema.Tables.FirstOrDefault(t =>
                t.TableName.Equals(tableRef, StringComparison.OrdinalIgnoreCase) ||
                t.TableName.EndsWith("." + parts.Take(parts.Length - 1).Last(), StringComparison.OrdinalIgnoreCase));
            if (tableInSchema == null)
                continue;

            if (existingTables.Contains(tableInSchema.TableName) ||
                existingTables.Any(t => t.EndsWith("." + tableInSchema.TableName.Split('.').Last(), StringComparison.OrdinalIgnoreCase)))
                continue;

            dbQuery.RequiredTables.Add(tableInSchema.TableName);
            existingTables.Add(tableInSchema.TableName);
        }
    }

    private static List<string> FilterRequiredMappingColumnsBySchema(List<string> requiredColumns, DatabaseSchemaInfo schema)
    {
        if (requiredColumns.Count == 0)
            return requiredColumns;

        var filtered = new List<string>();
        foreach (var col in requiredColumns)
        {
            var parts = col.Split('.');
            if (parts.Length < 2)
                continue;

            var columnName = parts[^1];
            var tablePart = string.Join(".", parts.Take(parts.Length - 1));

            var table = schema.Tables.FirstOrDefault(t =>
                t.TableName.Equals(tablePart, StringComparison.OrdinalIgnoreCase) ||
                t.TableName.EndsWith($".{tablePart.Split('.').Last()}", StringComparison.OrdinalIgnoreCase) ||
                tablePart.EndsWith($".{t.TableName.Split('.').Last()}", StringComparison.OrdinalIgnoreCase));

            if (table == null || !table.Columns.Any(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase)))
                continue;

            filtered.Add(col);
        }

        return filtered;
    }

    private async Task<List<string>> GetRequiredMappingColumnsAsync(
        string databaseName,
        List<string>? requiredTables,
        HashSet<string> targetDatabaseNamesInIntent,
        IReadOnlyDictionary<string, List<string>> targetDatabaseRequiredTables,
        IReadOnlyDictionary<string, int>? databaseNameToPriority)
    {
        var requiredColumns = new List<string>();

        if (targetDatabaseNamesInIntent.Count < 2)
            return requiredColumns;

        try
        {
            var connections = await _connectionManager.GetAllConnectionsAsync();
            var connection = connections.FirstOrDefault(c =>
                (c.Name ?? string.Empty).Equals(databaseName, StringComparison.OrdinalIgnoreCase));

            if (connection?.CrossDatabaseMappings == null && connections.All(c => c.CrossDatabaseMappings == null))
                return requiredColumns;

            var currentPriority = databaseNameToPriority?.TryGetValue(databaseName, out var cp) == true ? cp : int.MaxValue;

            if (connection?.CrossDatabaseMappings != null)
            {
                foreach (var mapping in connection.CrossDatabaseMappings)
                {
                    if (!mapping.SourceDatabase.Equals(databaseName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!targetDatabaseNamesInIntent.Contains(mapping.TargetDatabase ?? string.Empty))
                        continue;

                    if (databaseNameToPriority != null &&
                        databaseNameToPriority.TryGetValue(mapping.TargetDatabase ?? string.Empty, out var targetPriority) &&
                        targetPriority <= currentPriority)
                        continue;

                    if (targetDatabaseRequiredTables.TryGetValue(mapping.TargetDatabase ?? string.Empty, out var targetTables) &&
                        targetTables.Count > 0 &&
                        !string.IsNullOrWhiteSpace(mapping.TargetTable))
                    {
                        var targetTableName = mapping.TargetTable.Contains('.')
                            ? mapping.TargetTable.Split('.')[^1]
                            : mapping.TargetTable;

                        var targetTableInQuery = targetTables.Any(t =>
                            t.Equals(mapping.TargetTable, StringComparison.OrdinalIgnoreCase) ||
                            t.Equals(targetTableName, StringComparison.OrdinalIgnoreCase) ||
                            t.EndsWith($".{targetTableName}", StringComparison.OrdinalIgnoreCase));

                        if (!targetTableInQuery)
                            continue;
                    }

                    var sourceColRef = $"{mapping.SourceTable}.{mapping.SourceColumn}";
                    if (!requiredColumns.Contains(sourceColRef, StringComparer.OrdinalIgnoreCase))
                        requiredColumns.Add(sourceColRef);
                }
            }

            foreach (var conn in connections)
            {
                if (conn?.CrossDatabaseMappings == null)
                    continue;

                foreach (var mapping in conn.CrossDatabaseMappings)
                {
                    if (!(mapping.TargetDatabase ?? string.Empty).Equals(databaseName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!targetDatabaseNamesInIntent.Contains(mapping.SourceDatabase ?? string.Empty))
                        continue;

                    if (databaseNameToPriority != null &&
                        databaseNameToPriority.TryGetValue(mapping.SourceDatabase ?? string.Empty, out var sourcePriority) &&
                        sourcePriority >= currentPriority)
                        continue;

                    if (requiredTables == null || requiredTables.Count == 0)
                        continue;

                    if (string.IsNullOrWhiteSpace(mapping.TargetTable) || string.IsNullOrWhiteSpace(mapping.TargetColumn))
                        continue;

                    var targetTableName = mapping.TargetTable.Contains('.')
                        ? mapping.TargetTable.Split('.')[^1]
                        : mapping.TargetTable;

                    var targetTableInQuery = requiredTables.Any(t =>
                        t.Equals(mapping.TargetTable, StringComparison.OrdinalIgnoreCase) ||
                        t.Equals(targetTableName, StringComparison.OrdinalIgnoreCase) ||
                        t.EndsWith($".{targetTableName}", StringComparison.OrdinalIgnoreCase));

                    if (!targetTableInQuery)
                        continue;

                    var targetColRef = $"{mapping.TargetTable}.{mapping.TargetColumn}";
                    if (!requiredColumns.Contains(targetColRef, StringComparer.OrdinalIgnoreCase))
                        requiredColumns.Add(targetColRef);
                }
            }
        }
        catch (Exception ex)
        {
            DatabaseLogMessages.LogErrorGettingRequiredMappingColumns(_logger, databaseName, ex);
        }

        return requiredColumns;
    }


    private async Task<List<string>> ValidateCrossDatabaseMappingColumnsAsync(
        string sql,
        string databaseName,
        List<string>? requiredTables,
        HashSet<string> targetDatabaseNamesInIntent,
        IReadOnlyDictionary<string, List<string>> targetDatabaseRequiredTables,
        IReadOnlyDictionary<string, int>? databaseNameToPriority)
    {
        var requiredColumns = await GetRequiredMappingColumnsAsync(
            databaseName, requiredTables, targetDatabaseNamesInIntent, targetDatabaseRequiredTables, databaseNameToPriority);

        if (requiredColumns.Count == 0)
            return new List<string>();

        var missingColumns = new List<string>();
        foreach (var requiredColumn in requiredColumns)
        {
            var parts = requiredColumn.Split('.');
            var columnName = parts.Length > 1 ? parts[^1] : requiredColumn;
            var columnPattern = $@"\b{Regex.Escape(columnName)}\b";
            if (!Regex.IsMatch(sql, columnPattern, RegexOptions.IgnoreCase))
            {
                missingColumns.Add(requiredColumn);
            }
        }

        return missingColumns;
    }

    private static readonly string[] InvalidSqlPlaceholders = { "ABOVE QUERY", "YOUR QUERY", "SUBQUERY HERE", "PLACEHOLDER", "INSERT QUERY" };

    private bool ValidateSql(string sql, DatabaseSchemaInfo schema, List<string> requiredTables, ISqlDialectStrategy strategy, List<string> allDatabaseNames, out List<string> errors)
    {
        errors = new List<string>();

        var sqlForPlaceholderCheck = StripSqlComments(sql);
        var sqlUpper = sqlForPlaceholderCheck.ToUpperInvariant();
        foreach (var placeholder in InvalidSqlPlaceholders)
        {
            if (sqlUpper.Contains(placeholder, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"SQL contains invalid placeholder: {placeholder}. AI must generate complete SQL, not placeholders.");
                break;
            }
        }

        if (errors.Count > 0)
            return false;

        if (!strategy.ValidateSyntax(sql, out var syntaxError))
        {
            errors.Add(syntaxError);
        }

        var schemaErrors = _validator.ValidateQuery(sql, schema, requiredTables, allDatabaseNames);
        errors.AddRange(schemaErrors);

        return errors.Count == 0;
    }

    private static string StripSqlComments(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;
        var s = Regex.Replace(sql, @"--[^\r\n]*", "", RegexOptions.Multiline);
        s = Regex.Replace(s, @"/\*[\s\S]*?\*/", "", RegexOptions.Singleline);
        return s.Trim();
    }

    private static string ExtractCompleteSQL(string sqlText)
    {
        if (string.IsNullOrWhiteSpace(sqlText)) return string.Empty;

        sqlText = Regex.Replace(sqlText, @"```sql\s*", "", RegexOptions.IgnoreCase);
        sqlText = Regex.Replace(sqlText, @"```\s*", "", RegexOptions.IgnoreCase);
        sqlText = Regex.Replace(sqlText, @"###\s+.*", "", RegexOptions.IgnoreCase);
        sqlText = Regex.Replace(sqlText, @"Explanation:.*", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        var lines = sqlText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var sqlLines = new List<string>();
        var foundSelect = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (trimmed.StartsWith("```", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("###", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Explanation:", StringComparison.OrdinalIgnoreCase))
                continue;

            if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                foundSelect = true;
                sqlLines.Add(trimmed);
            }
            else if (foundSelect)
            {
                if (trimmed.StartsWith("--") || trimmed.StartsWith("//") || trimmed.StartsWith("/*"))
                    continue;

                var sqlKeywords = new[] { "FROM", "WHERE", "GROUP", "ORDER", "HAVING", "LIMIT", "TOP", "JOIN", "INNER", "LEFT", "RIGHT", "ON", "AND", "OR", "AS", "UNION", "EXCEPT", "INTERSECT" };
                var isSqlKeyword = Array.Exists(sqlKeywords, kw => trimmed.StartsWith(kw, StringComparison.OrdinalIgnoreCase));

                if (!isSqlKeyword && trimmed.Length > 0 && char.IsLetter(trimmed[0]) && !trimmed.Contains("(") && !trimmed.Contains(")") && !trimmed.Contains(",") && !trimmed.Contains("=") && !trimmed.Contains("'") && !trimmed.Contains("\""))
                {
                    break;
                }

                sqlLines.Add(trimmed);
            }
        }

        return string.Join(" ", sqlLines).Trim();
    }

    private string StripDatabaseNamePrefixWhenSameDatabase(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var fixedSql = sql;
        var replacements = new List<(string Original, string Replacement)>();

        var threePartPatterns = new[]
        {
            @"(\w+)\.\[(\w+)\]\.\[(\w+)\]",
            @"\[(\w+)\]\.\[(\w+)\]\.\[(\w+)\]",
            @"\b(\w+)\.(\w+)\.(\w+)\b"
        };

        foreach (var pattern in threePartPatterns)
        {
            var matches = Regex.Matches(fixedSql, pattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                var fullMatch = match.Value;
                var databaseName = match.Groups[1].Value.Trim('[', ']');
                var schemaName = match.Groups[2].Value.Trim('[', ']');
                var tableName = match.Groups[3].Value.Trim('[', ']');

                if (!schema.DatabaseName.Equals(databaseName, StringComparison.OrdinalIgnoreCase))
                    continue;

                string replacement;
                if (pattern.Contains(@"\[(\w+)\]"))
                    replacement = $"[{schemaName}].[{tableName}]";
                else
                    replacement = $"{schemaName}.{tableName}";

                if (!replacements.Any(r => r.Original.Equals(fullMatch, StringComparison.OrdinalIgnoreCase)))
                    replacements.Add((fullMatch, replacement));
            }
        }

        var twoPartPatterns = new[]
        {
            @"\b(\w+)\.(\w+)\b",
            @"\[(\w+)\]\.\[(\w+)\]"
        };

        foreach (var pattern in twoPartPatterns)
        {
            var matches = Regex.Matches(fixedSql, pattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                var fullMatch = match.Value;
                var part1 = match.Groups[1].Value.Trim('[', ']');
                var part2 = match.Groups[2].Value.Trim('[', ']');

                if (!schema.DatabaseName.Equals(part1, StringComparison.OrdinalIgnoreCase))
                    continue;

                var foundTable = schema.Tables.FirstOrDefault(t =>
                    t.TableName.Equals(part2, StringComparison.OrdinalIgnoreCase) ||
                    t.TableName.EndsWith($".{part2}", StringComparison.OrdinalIgnoreCase));

                if (foundTable == null)
                    continue;

                string replacement;
                if (pattern.Contains(@"\[(\w+)\]"))
                {
                    var parts = foundTable.TableName.Split('.');
                    replacement = parts.Length == 2
                        ? $"[{parts[0]}].[{parts[1]}]"
                        : $"[{foundTable.TableName}]";
                }
                else
                {
                    replacement = foundTable.TableName;
                }

                if (!replacements.Any(r => r.Original.Equals(fullMatch, StringComparison.OrdinalIgnoreCase)))
                    replacements.Add((fullMatch, replacement));
            }
        }

        foreach (var (original, replacement) in replacements.OrderByDescending(r => r.Original.Length))
        {
            fixedSql = Regex.Replace(fixedSql, Regex.Escape(original), replacement, RegexOptions.IgnoreCase);
        }

        return fixedSql;
    }

    private string DetectAndFixCrossDatabaseReferences(string sql, DatabaseSchemaInfo schema, List<string> requiredTables)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var fixedSql = sql;
        var validTableNames = schema.Tables.Select(t => t.TableName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var databasePrefixPatterns = new[]
        {
            @"\b(\w+)\.(\w+)\.(\w+)\b",
            @"\""([^\""]+)\""\.\""([^\""]+)\""\.\""([^\""]+)\""",
            @"\""([^\""]+)\""\.\""([^\""]+)\""\.(\w+)",
            @"\""([^\""]+)\""\.(\w+)\.(\w+)",
            @"(\w+)\.(\w+)\.(\w+)",
            @"\[(\w+)\]\.\[(\w+)\]\.\[(\w+)\]",
            @"(\w+)\.""(\w+)""\.(\w+)"
        };

        var twoPartPatterns = new[]
        {
            @"\""([^\""]+)\""\.\""([^\""]+)\""",
            @"\[([^\]]+)\]\.\[([^\]]+)\]"
        };

        var replacements = new List<(string Original, string Replacement)>();

        var validColumnNames = schema.Tables
            .SelectMany(t => t.Columns)
            .Select(c => c.ColumnName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var validTableLastParts = schema.Tables
            .Select(t => t.TableName.Split('.').Last())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var pattern in twoPartPatterns)
        {
            var matches = Regex.Matches(fixedSql, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                var fullMatch = match.Value;
                var part1 = match.Groups[1].Value.Trim('"', '[', ']');
                var part2 = match.Groups[2].Value.Trim('"', '[', ']');
                var twoPartName = $"{part1}.{part2}";

                if (validTableNames.Contains(twoPartName, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (validColumnNames.Contains(part2) && (validTableLastParts.Contains(part1) || validTableNames.Any(vt => vt.EndsWith($".{part1}", StringComparison.OrdinalIgnoreCase))))
                    continue;

                var replacement = string.Empty;
                DatabaseLogMessages.LogRemovingCrossDatabaseReference(_logger, fullMatch, schema.DatabaseName, null);
                if (!replacements.Any(r => r.Original.Equals(fullMatch, StringComparison.OrdinalIgnoreCase)))
                    replacements.Add((fullMatch, replacement));
            }
        }

        foreach (var pattern in databasePrefixPatterns)
        {
            var matches = Regex.Matches(fixedSql, pattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                var fullMatch = match.Value;
                var part1 = match.Groups[1].Value.Trim('"', '[', ']');
                var part2 = match.Groups[2].Value.Trim('"', '[', ']');
                var part3 = match.Groups[3].Value.Trim('"', '[', ']');
                var firstTwoParts = $"{part1}.{part2}";
                var twoPartName = $"{part2}.{part3}";

                if (validTableNames.Contains(firstTwoParts, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (schema.DatabaseName.Equals(part1, StringComparison.OrdinalIgnoreCase))
                    continue;

                var tableName = part3;
                string replacement;

                var foundValidTable = validTableNames.FirstOrDefault(vt =>
                    vt.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                    vt.Equals(twoPartName, StringComparison.OrdinalIgnoreCase) ||
                    (vt.Contains('.') && vt.Split('.').Last().Equals(tableName, StringComparison.OrdinalIgnoreCase)) ||
                    (vt.Contains('.') && vt.Equals(twoPartName, StringComparison.OrdinalIgnoreCase)));

                if (foundValidTable != null)
                {
                    DatabaseLogMessages.LogRemovingDatabasePrefix(_logger, fullMatch, foundValidTable, null);
                    replacement = foundValidTable;
                }
                else
                {
                    var requiredTableMatch = requiredTables.FirstOrDefault(rt =>
                        rt.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                        rt.Equals(twoPartName, StringComparison.OrdinalIgnoreCase) ||
                        rt.EndsWith($".{tableName}", StringComparison.OrdinalIgnoreCase) ||
                        rt.EndsWith($"\".{tableName}\"", StringComparison.OrdinalIgnoreCase) ||
                        (rt.Contains('.') && rt.Split('.').Last().Equals(tableName, StringComparison.OrdinalIgnoreCase)));

                    if (requiredTableMatch != null)
                    {
                        DatabaseLogMessages.LogRemovingDatabasePrefix(_logger, fullMatch, requiredTableMatch, null);
                        replacement = requiredTableMatch;
                    }
                    else
                    {
                        DatabaseLogMessages.LogRemovingCrossDatabaseReference(_logger, fullMatch, schema.DatabaseName, null);
                        replacement = string.Empty;
                    }
                }

                if (!replacements.Any(r => r.Original.Equals(fullMatch, StringComparison.OrdinalIgnoreCase)))
                {
                    replacements.Add((fullMatch, replacement));
                }
            }
        }

        foreach (var (original, replacement) in replacements.OrderByDescending(r => r.Original.Length))
        {
            fixedSql = Regex.Replace(fixedSql, Regex.Escape(original), string.IsNullOrEmpty(replacement) ? string.Empty : replacement, RegexOptions.IgnoreCase);
        }

        fixedSql = FixBrokenJoinClauses(fixedSql);
        fixedSql = FixInvalidFromClauses(fixedSql);

        return fixedSql;
    }

    private string RemoveInvalidSubqueryReferences(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var validTableNames = schema.Tables
            .Select(t => t.TableName)
            .Concat(schema.Tables.Select(t => t.TableName.Split('.').Last()))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var subqueryPattern = @"(\bWHERE\s+)(\w+)\s+IN\s*\(\s*SELECT\s+[^)]+\s+FROM\s+([\w.""\[\].]+)\s*\)";
        var match = Regex.Match(sql, subqueryPattern, RegexOptions.IgnoreCase);
        while (match.Success)
        {
            var tableRef = match.Groups[3].Value.Replace("[", string.Empty).Replace("]", string.Empty).Replace("\"", string.Empty);
            var tablePart = tableRef.Contains('.') ? tableRef.Split('.').Last() : tableRef;

            var tableExists = validTableNames.Contains(tableRef) ||
                             validTableNames.Contains(tablePart) ||
                             schema.Tables.Any(t => t.TableName.EndsWith($".{tablePart}", StringComparison.OrdinalIgnoreCase));

            if (!tableExists)
            {
                DatabaseLogMessages.LogRemovingInvalidSubqueryReference(_logger, match.Groups[2].Value, tableRef, schema.DatabaseName, null!);
                sql = sql.Remove(match.Index, match.Length);
                sql = Regex.Replace(sql, @"\s{2,}", " ", RegexOptions.None);
                match = Regex.Match(sql, subqueryPattern, RegexOptions.IgnoreCase);
            }
            else
            {
                match = match.NextMatch();
            }
        }

        return sql;
    }

    private static string? FindSimilarTableInSchema(string schemaPart, string tablePart, HashSet<string> validTableNames)
    {
        var schemaTables = validTableNames
            .Where(vt => vt.StartsWith($"{schemaPart}.", StringComparison.OrdinalIgnoreCase))
            .ToList();
        return schemaTables.FirstOrDefault(t =>
            t.IndexOf(tablePart, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static string FixBrokenJoinClauses(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var fixedSql = sql;
        var brokenJoinPattern = @"\s+JOIN\s+[^\s]+\s+ON\s*=\s*";
        if (Regex.IsMatch(fixedSql, brokenJoinPattern, RegexOptions.IgnoreCase))
        {
            fixedSql = Regex.Replace(fixedSql, brokenJoinPattern, " ", RegexOptions.IgnoreCase);
        }

        return fixedSql;
    }

    private string RemoveInvalidJoinClausesBacktick(string sql, DatabaseSchemaInfo schema,
        HashSet<string> validTableNames, HashSet<string> validTableUnderscore)
    {
        var backtickJoinPattern = @"\s+(LEFT|RIGHT|INNER|FULL|CROSS)?\s*JOIN\s+`([^`]+)`\s+(\w+)\s+ON\s+";
        var match = Regex.Match(sql, backtickJoinPattern, RegexOptions.IgnoreCase);
        while (match.Success)
        {
            var tableRef = match.Groups[2].Value;
            var alias = match.Groups[3].Value;
            var tableExists = validTableNames.Contains(tableRef, StringComparer.OrdinalIgnoreCase) ||
                validTableUnderscore.Contains(tableRef.Replace(".", "_"), StringComparer.OrdinalIgnoreCase) ||
                validTableNames.Any(vt => vt.Replace(".", "_").Equals(tableRef.Replace(".", "_"), StringComparison.OrdinalIgnoreCase));
            if (!tableExists)
            {
                var fullMatch = match.Value;
                var onClauseEnd = FindMatchingOnClauseEnd(sql, match.Index + match.Length);
                var joinBlock = sql.Substring(match.Index, onClauseEnd - match.Index);
                DatabaseLogMessages.LogRemovingInvalidJoinClause(_logger, tableRef, schema.DatabaseName, alias, null);
                sql = sql.Remove(match.Index, joinBlock.Length);
                sql = RemoveAliasReferences(sql, alias);
                sql = FixBrokenOnClausesAfterAliasRemoval(sql, schema);
                sql = CleanupSqlAfterRemoval(sql);
                match = Regex.Match(sql, backtickJoinPattern, RegexOptions.IgnoreCase);
            }
            else
            {
                match = match.NextMatch();
            }
        }
        return sql;
    }

    private static int FindMatchingOnClauseEnd(string sql, int startIdx)
    {
        var depth = 0;
        var i = startIdx;
        while (i < sql.Length)
        {
            var c = sql[i];
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (depth == 0 && (sql[i..].StartsWith(" JOIN ", StringComparison.OrdinalIgnoreCase) ||
                     sql[i..].StartsWith(" LEFT JOIN ", StringComparison.OrdinalIgnoreCase) ||
                     sql[i..].StartsWith(" INNER JOIN ", StringComparison.OrdinalIgnoreCase) ||
                     sql[i..].StartsWith(" WHERE ", StringComparison.OrdinalIgnoreCase) ||
                     sql[i..].StartsWith(" GROUP ", StringComparison.OrdinalIgnoreCase) ||
                     sql[i..].StartsWith(" ORDER ", StringComparison.OrdinalIgnoreCase) ||
                     sql[i..].StartsWith(" HAVING ", StringComparison.OrdinalIgnoreCase) ||
                     sql[i..].StartsWith(" LIMIT ", StringComparison.OrdinalIgnoreCase)))
                return i;
            i++;
        }
        return sql.Length;
    }

    private string RemoveInvalidJoinClauses(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var validTableNames = schema.Tables.Select(t => t.TableName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var validTableUnderscore = schema.Tables
            .Select(t => t.TableName.Replace(".", "_"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        sql = RemoveInvalidJoinClausesBacktick(sql, schema, validTableNames, validTableUnderscore);

        var joinPattern = @"\s+(LEFT|RIGHT|INNER|FULL|CROSS)?\s*JOIN\s+(\w+)\.(\w+)\s+(\w+)\s+ON\s+(.+?)(?=\s+(?:LEFT|RIGHT|INNER|FULL|CROSS)?\s*JOIN\s|\s+WHERE\s|\s+GROUP\s|\s+ORDER\s|\s+HAVING\s|$)";

        var match = Regex.Match(sql, joinPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        while (match.Success)
        {
            var schemaPart = match.Groups[2].Value;
            var tablePart = match.Groups[3].Value;
            var alias = match.Groups[4].Value;
            var twoPartName = $"{schemaPart}.{tablePart}";

            var tableExists = validTableNames.Contains(twoPartName) ||
                             validTableNames.Contains(tablePart) ||
                             validTableNames.Any(vt => vt.EndsWith($".{tablePart}", StringComparison.OrdinalIgnoreCase));

            if (!tableExists)
            {
                var replacementTable = FindSimilarTableInSchema(schemaPart, tablePart, validTableNames);
                if (!string.IsNullOrEmpty(replacementTable))
                {
                    var replacementJoin = match.Value
                        .Replace(twoPartName, replacementTable, StringComparison.OrdinalIgnoreCase)
                        .Replace($"{schemaPart}.{tablePart}", replacementTable, StringComparison.OrdinalIgnoreCase);
                    sql = sql.Remove(match.Index, match.Length).Insert(match.Index, replacementJoin);
                    match = Regex.Match(sql, joinPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    continue;
                }

                DatabaseLogMessages.LogRemovingInvalidJoinClause(_logger, twoPartName, schema.DatabaseName, alias, null);

                sql = sql.Remove(match.Index, match.Length);
                sql = RemoveAliasReferences(sql, alias);
                sql = FixBrokenOnClausesAfterAliasRemoval(sql, schema);
                sql = FixBrokenAggregatesAfterAliasRemoval(sql, alias, schema);
                sql = CleanupSqlAfterRemoval(sql);

                match = Regex.Match(sql, joinPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }
            else
            {
                match = match.NextMatch();
            }
        }

        var orphanJoinPattern = @"\s+(LEFT|RIGHT|INNER|FULL|CROSS)?\s*JOIN\s+(\w+)\s+ON\s+([\s\S]+?)(?=\s+(?:LEFT|RIGHT|INNER|FULL|CROSS)?\s*JOIN\s|\s+WHERE\s|\s+GROUP\s|\s+ORDER\s|\s+HAVING\s|$)";
        match = Regex.Match(sql, orphanJoinPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        while (match.Success)
        {
            var tableOrAlias = match.Groups[2].Value;
            var tableExists = validTableNames.Contains(tableOrAlias) ||
                             validTableNames.Any(vt => vt.EndsWith($".{tableOrAlias}", StringComparison.OrdinalIgnoreCase));

            if (!tableExists)
            {
                DatabaseLogMessages.LogRemovingInvalidJoinClause(_logger, tableOrAlias, schema.DatabaseName, tableOrAlias, null);

                sql = sql.Remove(match.Index, match.Length);
                sql = RemoveAliasReferences(sql, tableOrAlias);
                sql = FixBrokenOnClausesAfterAliasRemoval(sql, schema);
                sql = FixBrokenAggregatesAfterAliasRemoval(sql, tableOrAlias, schema);
                sql = CleanupSqlAfterRemoval(sql);
                match = Regex.Match(sql, orphanJoinPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }
            else
            {
                match = match.NextMatch();
            }
        }

        return sql;
    }

    private string FixUndefinedAliasReferences(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var aliasToTable = ExtractTableAliasesStatic(sql);
        var validSchemas = schema.Tables.Select(t => t.TableName.Split('.')[0]).Distinct().ToHashSet(StringComparer.OrdinalIgnoreCase);

        var columnMatches = Regex.Matches(sql, @"\b([a-zA-Z0-9_]+)\.([a-zA-Z0-9_]+)\b", RegexOptions.IgnoreCase);
        var replacements = new List<(string Original, string Replacement)>();

        foreach (Match match in columnMatches)
        {
            var prefix = match.Groups[1].Value;
            var columnName = match.Groups[2].Value;

            if (columnName.Equals("*", StringComparison.OrdinalIgnoreCase))
                continue;

            if (validSchemas.Contains(prefix))
                continue;

            if (aliasToTable.TryGetValue(prefix, out var prefixTable))
            {
                var tbl = schema.Tables.FirstOrDefault(t =>
                    t.TableName.Equals(prefixTable, StringComparison.OrdinalIgnoreCase) ||
                    t.TableName.EndsWith("." + prefixTable.Split('.').Last(), StringComparison.OrdinalIgnoreCase));
                if (tbl != null && tbl.Columns.Any(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase)))
                    continue;
            }

            var tableWithColumn = schema.Tables.FirstOrDefault(t =>
                t.Columns.Any(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase)));
            if (tableWithColumn == null)
                continue;

            var correctAlias = aliasToTable.FirstOrDefault(kvp =>
                kvp.Value.Equals(tableWithColumn.TableName, StringComparison.OrdinalIgnoreCase) ||
                kvp.Value.EndsWith("." + tableWithColumn.TableName.Split('.').Last(), StringComparison.OrdinalIgnoreCase) ||
                kvp.Value.Replace(".", "_").Equals(tableWithColumn.TableName.Replace(".", "_"), StringComparison.OrdinalIgnoreCase)).Key;
            if (string.IsNullOrEmpty(correctAlias))
                continue;

            var original = match.Value;
            var replacement = $"{correctAlias}.{columnName}";
            if (!replacements.Any(r => r.Original.Equals(original, StringComparison.OrdinalIgnoreCase)))
            {
                replacements.Add((original, replacement));
                DatabaseLogMessages.LogRemovingDatabasePrefix(_logger, original, replacement, null);
            }
        }

        foreach (var (original, replacement) in replacements.OrderByDescending(r => r.Original.Length))
        {
            sql = Regex.Replace(sql, $@"\b{Regex.Escape(original)}\b", replacement, RegexOptions.IgnoreCase);
        }

        return sql;
    }

    private string FixEmptySelectList(string sql, DatabaseSchemaInfo schema, List<string> requiredMappingColumns)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var emptySelectMatch = Regex.Match(sql, @"SELECT\s+(?:TOP\s+\d+\s+)?(?:DISTINCT\s+)?\s*FROM\b", RegexOptions.IgnoreCase);
        if (!emptySelectMatch.Success)
            return sql;

        var fromMatch = Regex.Match(sql, @"FROM\s+(\[?[a-zA-Z0-9_.]+\]?(?:\s+(?:AS\s+)?([a-zA-Z0-9_]+))?)(?:\s|$|JOIN)", RegexOptions.IgnoreCase);
        if (!fromMatch.Success)
            return sql;

        var tableRef = NormalizeTableName(fromMatch.Groups[1].Value);
        var alias = fromMatch.Groups[2].Value;
        if (string.IsNullOrWhiteSpace(alias))
            alias = tableRef.Split('.').Last();

        string columnToAdd = null;
        if (requiredMappingColumns.Count > 0)
        {
            var firstRequired = requiredMappingColumns[0];
            var parts = firstRequired.Split('.');
            var colName = parts.Length > 1 ? parts[^1] : firstRequired;
            columnToAdd = $"{alias}.{colName}";
        }
        else
        {
            var table = schema.Tables.FirstOrDefault(t =>
                t.TableName.Equals(tableRef, StringComparison.OrdinalIgnoreCase) ||
                t.TableName.EndsWith($".{tableRef.Split('.').Last()}", StringComparison.OrdinalIgnoreCase));
            var col = table?.Columns.FirstOrDefault()?.ColumnName ?? table?.PrimaryKeys.FirstOrDefault();
            if (!string.IsNullOrEmpty(col))
                columnToAdd = $"{alias}.{col}";
        }

        if (string.IsNullOrEmpty(columnToAdd))
            return sql;

        return Regex.Replace(sql, @"(SELECT\s+(?:TOP\s+\d+\s+)?(?:DISTINCT\s+)?)\s*FROM", $"$1{columnToAdd} FROM", RegexOptions.IgnoreCase);
    }

    private string ForceInjectMissingMappingColumns(string sql, DatabaseSchemaInfo schema, List<string> missingColumns)
    {
        if (string.IsNullOrWhiteSpace(sql) || missingColumns.Count == 0)
            return sql;

        foreach (var requiredCol in missingColumns.ToList())
        {
            var parts = requiredCol.Split('.');
            var columnName = parts.Length > 1 ? parts[^1] : requiredCol;
            var tablePart = parts.Length >= 2 ? string.Join(".", parts.Take(parts.Length - 1)) : string.Empty;

            if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(tablePart))
                continue;

            var columnPattern = $@"\b{Regex.Escape(columnName)}\b";
            if (Regex.IsMatch(sql, columnPattern, RegexOptions.IgnoreCase))
                continue;

            var tableLast = tablePart.Split('.').Last();
            if (!Regex.IsMatch(sql, $@"\b{Regex.Escape(tableLast)}\b", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(sql, Regex.Escape(tablePart).Replace(@"\.", @"\s*\.\s*"), RegexOptions.IgnoreCase))
                continue;

            var table = schema.Tables.FirstOrDefault(t =>
                t.TableName.Equals(tablePart, StringComparison.OrdinalIgnoreCase) ||
                t.TableName.EndsWith($".{tableLast}", StringComparison.OrdinalIgnoreCase));
            if (table == null || !table.Columns.Any(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase)))
                continue;

            var quotedCol = schema.DatabaseType == DatabaseType.PostgreSQL ? $"\"{columnName}\"" : schema.DatabaseType == DatabaseType.SqlServer ? $"[{columnName}]" : columnName;
            var quotedTable = schema.DatabaseType == DatabaseType.PostgreSQL
                ? string.Join(".", tablePart.Split('.').Select(p => $"\"{p}\""))
                : schema.DatabaseType == DatabaseType.SqlServer
                    ? string.Join(".", tablePart.Split('.').Select(p => $"[{p}]"))
                    : tablePart;
            var columnRef = $"{quotedTable}.{quotedCol}";

            var selectMatch = Regex.Match(sql, @"(SELECT\s+(?:TOP\s+\d+\s+)?(?:DISTINCT\s+)?)([^F]+?)(\s+FROM\b)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (selectMatch.Success)
            {
                var selectList = selectMatch.Groups[2].Value.TrimEnd();
                if (selectList.EndsWith(","))
                    selectList = selectList + " " + columnRef;
                else
                    selectList = selectList + ", " + columnRef;
                sql = sql.Substring(0, selectMatch.Groups[2].Index) + selectList + selectMatch.Groups[3].Value + sql.Substring(selectMatch.Index + selectMatch.Length);
                DatabaseLogMessages.LogInjectedMissingMappingColumn(_logger, requiredCol, schema.DatabaseName, sql, null!);

                var groupByMatch = Regex.Match(sql, @"GROUP\s+BY\s+([^;]+?)(?=\s+ORDER\s+BY|\s+HAVING|\s*$)", RegexOptions.IgnoreCase);
                if (groupByMatch.Success && !Regex.IsMatch(groupByMatch.Groups[1].Value, columnPattern, RegexOptions.IgnoreCase))
                {
                    var groupByList = groupByMatch.Groups[1].Value.TrimEnd();
                    var newGroupByList = groupByList.EndsWith(",", StringComparison.Ordinal) ? groupByList + " " + columnRef : groupByList + ", " + columnRef;
                    sql = sql.Substring(0, groupByMatch.Groups[1].Index) + newGroupByList + sql.Substring(groupByMatch.Index + groupByMatch.Length);
                }
            }
        }

        return sql;
    }

    private string AddMissingMappingTableJoins(string sql, DatabaseSchemaInfo schema, List<string> requiredMappingColumns)
    {
        if (string.IsNullOrWhiteSpace(sql) || requiredMappingColumns.Count == 0 || !Regex.IsMatch(sql, @"\b(?:FROM|JOIN)\b", RegexOptions.IgnoreCase))
            return sql;

        var aliasToTable = ExtractTableAliases(sql);
        if (aliasToTable.Count == 0)
            aliasToTable = ExtractTableAliasesFallback(sql);
        if (aliasToTable.Count == 0)
            return sql;

        var tablesInQuery = aliasToTable.Values.Select(NormalizeTableName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var requiredCol in requiredMappingColumns.ToList())
        {
            var parts = requiredCol.Split('.');
            if (parts.Length < 2) continue;
            var tablePart = string.Join(".", parts.Take(parts.Length - 1));
            var targetTableInfo = schema.Tables.FirstOrDefault(t =>
                t.TableName.Equals(tablePart, StringComparison.OrdinalIgnoreCase) ||
                t.TableName.EndsWith($".{tablePart.Split('.').Last()}", StringComparison.OrdinalIgnoreCase) ||
                tablePart.EndsWith($".{t.TableName.Split('.').Last()}", StringComparison.OrdinalIgnoreCase));
            if (targetTableInfo == null) continue;

            var targetNorm = NormalizeTableName(targetTableInfo.TableName);
            var targetShort = targetTableInfo.TableName.Split('.').Last();
            if (tablesInQuery.Contains(targetNorm) || tablesInQuery.Contains(targetShort) ||
                aliasToTable.Values.Any(t => NormalizeTableName(t).Equals(targetNorm, StringComparison.OrdinalIgnoreCase)))
                continue;

            string? sourceTableRef = null;
            string? sourceAlias = null;
            string? fkColumn = null;
            string? pkColumn = null;

            foreach (var kvp in aliasToTable)
            {
                var tblRef = NormalizeTableName(kvp.Value);
                var tableInfo = schema.Tables.FirstOrDefault(t =>
                    NormalizeTableName(t.TableName).Equals(tblRef, StringComparison.OrdinalIgnoreCase) ||
                    t.TableName.EndsWith($".{tblRef.Split('.').Last()}", StringComparison.OrdinalIgnoreCase));
                if (tableInfo == null) continue;

                var fk = tableInfo.ForeignKeys.FirstOrDefault(fk =>
                    fk.ReferencedTable.Equals(targetTableInfo.TableName, StringComparison.OrdinalIgnoreCase) ||
                    fk.ReferencedTable.EndsWith($".{targetShort}", StringComparison.OrdinalIgnoreCase) ||
                    targetTableInfo.TableName.EndsWith($".{fk.ReferencedTable.Split('.').Last()}", StringComparison.OrdinalIgnoreCase));
                if (fk == null) continue;

                sourceTableRef = tblRef;
                sourceAlias = kvp.Key;
                fkColumn = fk.ColumnName;
                pkColumn = fk.ReferencedColumn;
                break;
            }

            if (string.IsNullOrEmpty(sourceAlias) || string.IsNullOrEmpty(fkColumn) || string.IsNullOrEmpty(pkColumn))
                continue;

            var newAlias = targetShort;
            var aliasCount = aliasToTable.Keys.Count(k => k.Equals(newAlias, StringComparison.OrdinalIgnoreCase));
            if (aliasCount > 0)
                newAlias = $"{newAlias}m";

            var tableRef = schema.DatabaseType == DatabaseType.SqlServer
                ? string.Join(".", targetTableInfo.TableName.Split('.').Select(p => $"[{p}]"))
                : schema.DatabaseType == DatabaseType.PostgreSQL
                    ? string.Join(".", targetTableInfo.TableName.Split('.').Select(p => $"\"{p}\""))
                    : targetTableInfo.TableName;

            var fkQualified = schema.DatabaseType == DatabaseType.SqlServer ? $"[{fkColumn}]" : schema.DatabaseType == DatabaseType.PostgreSQL ? $"\"{fkColumn}\"" : fkColumn;
            var pkQualified = schema.DatabaseType == DatabaseType.SqlServer ? $"[{pkColumn}]" : schema.DatabaseType == DatabaseType.PostgreSQL ? $"\"{pkColumn}\"" : pkColumn;

            var joinClause = $" LEFT JOIN {tableRef} {newAlias} ON {sourceAlias}.{fkQualified} = {newAlias}.{pkQualified}";

            var fromMatch = Regex.Match(sql, @"(FROM\s+[^\s]+(?:\s+(?:AS\s+)?[a-zA-Z0-9_]+)?)(\s+(?:LEFT|INNER|RIGHT|FULL|CROSS)?\s*JOIN|\s+WHERE|\s+GROUP|\s+ORDER|\s+HAVING|$)", RegexOptions.IgnoreCase);
            if (!fromMatch.Success) continue;

            var insertPos = fromMatch.Groups[1].Length;
            sql = sql.Insert(insertPos, joinClause);
            aliasToTable[newAlias] = targetTableInfo.TableName;
            tablesInQuery.Add(NormalizeTableName(targetTableInfo.TableName));
        }

        return sql;
    }

    private static readonly string[] DescriptiveColumnSubstrings = { "Name", "Title", "Description", "City", "Address", "Location", "Text", "Label", "FirstName", "LastName" };

    private string InjectDescriptiveColumnsForTargetQuery(string sql, DatabaseSchemaInfo schema, List<string> requiredMappingColumns)
    {
        if (string.IsNullOrWhiteSpace(sql) || requiredMappingColumns.Count == 0 ||
            !Regex.IsMatch(sql, @"WHERE\s+[\w\[\]""`\.]+\s+IN\s*\(", RegexOptions.IgnoreCase))
            return sql;

        var aliasToTable = ExtractTableAliases(sql);
        if (aliasToTable.Count == 0)
            aliasToTable = ExtractTableAliasesFallback(sql);
        if (aliasToTable.Count == 0)
            return sql;

        var mappingCol = requiredMappingColumns[0].Split('.')[^1];
        var tableWithMapping = schema.Tables.FirstOrDefault(t =>
            t.Columns.Any(c => c.ColumnName.Equals(mappingCol, StringComparison.OrdinalIgnoreCase)));
        if (tableWithMapping == null)
            return sql;

        var alias = aliasToTable.FirstOrDefault(kvp =>
            kvp.Value.Equals(tableWithMapping.TableName, StringComparison.OrdinalIgnoreCase) ||
            tableWithMapping.TableName.EndsWith($".{kvp.Value}", StringComparison.OrdinalIgnoreCase)).Key;
        if (string.IsNullOrEmpty(alias))
            return sql;

        var descriptiveCols = tableWithMapping.Columns
            .Where(c => !c.ColumnName.Equals(mappingCol, StringComparison.OrdinalIgnoreCase) &&
                DescriptiveColumnSubstrings.Any(sub => c.ColumnName.IndexOf(sub, StringComparison.OrdinalIgnoreCase) >= 0))
            .Take(3)
            .ToList();
        if (descriptiveCols.Count == 0)
            return sql;

        foreach (var col in descriptiveCols)
        {
            if (Regex.IsMatch(sql, $@"\b{Regex.Escape(col.ColumnName)}\b", RegexOptions.IgnoreCase))
                continue;
            var quotedCol = schema.DatabaseType == DatabaseType.PostgreSQL ? $"\"{col.ColumnName}\"" :
                schema.DatabaseType == DatabaseType.SqlServer ? $"[{col.ColumnName}]" : col.ColumnName;
            var selectMatch = Regex.Match(sql, @"(SELECT\s+(?:TOP\s+\d+\s+)?(?:DISTINCT\s+)?)(.+?)(\s+FROM\b)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!selectMatch.Success)
                continue;
            var insertPoint = selectMatch.Groups[2].Value.TrimEnd().Length + selectMatch.Groups[1].Length;
            sql = sql[..insertPoint] + ", " + $"{alias}.{quotedCol}" + sql[insertPoint..];
        }

        return sql;
    }

    private string InjectMissingMappingColumns(string sql, DatabaseSchemaInfo schema, List<string> requiredMappingColumns)
    {
        if (string.IsNullOrWhiteSpace(sql) || requiredMappingColumns.Count == 0)
            return sql;

        var aliasToTable = ExtractTableAliases(sql);
        if (aliasToTable.Count == 0)
        {
            aliasToTable = ExtractTableAliasesFallback(sql);
        }
        if (aliasToTable.Count == 0)
            return sql;

        foreach (var requiredCol in requiredMappingColumns.ToList())
        {
            var parts = requiredCol.Split('.');
            var columnName = parts.Length > 1 ? parts[^1] : requiredCol;
            var tablePart = parts.Length >= 2 ? string.Join(".", parts.Take(parts.Length - 1)) : string.Empty;

            if (string.IsNullOrEmpty(columnName))
                continue;

            var columnPattern = $@"\b{Regex.Escape(columnName)}\b";
            if (Regex.IsMatch(sql, columnPattern, RegexOptions.IgnoreCase))
                continue;

            string tableRefForColumn = null;
            string aliasForColumn = null;
            foreach (var kvp in aliasToTable)
            {
                var tblRef = kvp.Value;
                var tblAlias = kvp.Key;
                var tableMatches = string.IsNullOrEmpty(tablePart) ||
                    tblRef.Equals(tablePart, StringComparison.OrdinalIgnoreCase) ||
                    tablePart.EndsWith($".{tblRef.Split('.').Last()}", StringComparison.OrdinalIgnoreCase) ||
                    tblRef.EndsWith($".{tablePart.Split('.').Last()}", StringComparison.OrdinalIgnoreCase);
                if (!tableMatches)
                    continue;
                var table = schema.Tables.FirstOrDefault(t =>
                    t.TableName.Equals(tblRef, StringComparison.OrdinalIgnoreCase) ||
                    t.TableName.EndsWith($".{tblRef.Split('.').Last()}", StringComparison.OrdinalIgnoreCase));
                if (table == null || !table.Columns.Any(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase)))
                    continue;
                tableRefForColumn = tblRef;
                aliasForColumn = tblAlias;
                break;
            }

            if (string.IsNullOrEmpty(aliasForColumn))
                continue;

            var quotedCol = schema.DatabaseType == DatabaseType.PostgreSQL ? $"\"{columnName}\"" : schema.DatabaseType == DatabaseType.SqlServer ? $"[{columnName}]" : columnName;
            var columnRef = $"{aliasForColumn}.{quotedCol}";

            var selectInsertMatch = Regex.Match(sql, @"(SELECT\s+(?:TOP\s+\d+\s+)?(?:DISTINCT\s+)?)([^,]+?)(\s*,\s*|\s+FROM\b)", RegexOptions.IgnoreCase);
            if (selectInsertMatch.Success)
            {
                sql = sql.Substring(0, selectInsertMatch.Index) +
                    selectInsertMatch.Groups[1].Value +
                    selectInsertMatch.Groups[2].Value.TrimEnd() +
                    ", " + columnRef +
                    selectInsertMatch.Groups[3].Value +
                    sql.Substring(selectInsertMatch.Index + selectInsertMatch.Length);
                DatabaseLogMessages.LogInjectedMissingMappingColumn(_logger, requiredCol, schema.DatabaseName, sql, null!);
            }
            else
            {
                var selectFromMatch = Regex.Match(sql, @"(SELECT\s+(?:TOP\s+\d+\s+)?(?:DISTINCT\s+)?)(.+?)(\s+FROM\b)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (selectFromMatch.Success)
                {
                    sql = sql.Substring(0, selectFromMatch.Index) +
                        selectFromMatch.Groups[1].Value +
                        selectFromMatch.Groups[2].Value.TrimEnd() +
                        ", " + columnRef +
                        selectFromMatch.Groups[3].Value +
                        sql.Substring(selectFromMatch.Index + selectFromMatch.Length);
                    DatabaseLogMessages.LogInjectedMissingMappingColumn(_logger, requiredCol, schema.DatabaseName, sql, null!);
                }
                else
                {
                    var beforeFromMatch = Regex.Match(sql, @"^(.+?)(\s+FROM\b)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    if (beforeFromMatch.Success)
                    {
                        var selectPart = beforeFromMatch.Groups[1].Value.TrimEnd();
                        if (selectPart.EndsWith(",", StringComparison.Ordinal))
                            sql = selectPart + " " + columnRef + beforeFromMatch.Groups[2].Value + sql.Substring(beforeFromMatch.Index + beforeFromMatch.Length);
                        else
                            sql = selectPart + ", " + columnRef + beforeFromMatch.Groups[2].Value + sql.Substring(beforeFromMatch.Index + beforeFromMatch.Length);
                        DatabaseLogMessages.LogInjectedMissingMappingColumn(_logger, requiredCol, schema.DatabaseName, sql, null!);
                    }
                }
            }

            var groupByMatch = Regex.Match(sql, @"GROUP\s+BY\s+([^;]+?)(?=\s+ORDER\s+BY|\s+HAVING|\s*$)", RegexOptions.IgnoreCase);
            if (groupByMatch.Success && !Regex.IsMatch(groupByMatch.Groups[1].Value, columnPattern, RegexOptions.IgnoreCase))
            {
                var groupByList = groupByMatch.Groups[1].Value.TrimEnd();
                var newGroupByList = groupByList.EndsWith(",", StringComparison.Ordinal)
                    ? groupByList + " " + columnRef
                    : groupByList + ", " + columnRef;
                sql = sql.Substring(0, groupByMatch.Groups[1].Index) +
                    newGroupByList +
                    sql.Substring(groupByMatch.Index + groupByMatch.Length);
            }
        }

        return sql;
    }

    private static string RemoveAliasReferences(string sql, string alias)
    {
        if (string.IsNullOrWhiteSpace(sql) || string.IsNullOrWhiteSpace(alias))
            return sql;

        var aliasRefPattern = $@"\b{Regex.Escape(alias)}\.\w+\b";
        var selectItemWithAlias = $@",\s*[^,]*(?:\b{Regex.Escape(alias)}\.\w+\b)[^,]*";
        var groupByItemWithAlias = $@",\s*[^,]*(?:\b{Regex.Escape(alias)}\.\w+\b)[^,]*";
        var leadingAliasOnly = $@"(SELECT\s*(?:TOP\s+\d+\s+)?)\s*{aliasRefPattern}\s*,\s*";

        var previous = string.Empty;
        while (previous != sql)
        {
            previous = sql;
            sql = Regex.Replace(sql, selectItemWithAlias, "", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, groupByItemWithAlias, "", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, leadingAliasOnly, "$1", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @",\s*" + aliasRefPattern, "", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, aliasRefPattern + @"\s*,\s*", "", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, aliasRefPattern, "", RegexOptions.IgnoreCase);
        }

        return sql;
    }

    private static string FixBrokenOnClausesAfterAliasRemoval(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var aliasToTable = ExtractTableAliasesStatic(sql);
        var brokenOnPattern = @"ON\s*=\s*([a-zA-Z0-9_]+)\.([a-zA-Z0-9_]+)";
        var match = Regex.Match(sql, brokenOnPattern, RegexOptions.IgnoreCase);
        if (!match.Success)
            return sql;

        var rightAlias = match.Groups[1].Value;
        var rightColumn = match.Groups[2].Value;
        if (!aliasToTable.TryGetValue(rightAlias, out var rightTableName))
            return sql;

        var rightTable = schema.Tables.FirstOrDefault(t =>
            t.TableName.Equals(rightTableName, StringComparison.OrdinalIgnoreCase) ||
            t.TableName.EndsWith("." + rightTableName.Split('.').Last(), StringComparison.OrdinalIgnoreCase));
        if (rightTable == null)
            return sql;

        string replacement = null;
        foreach (var kvp in aliasToTable)
        {
            if (kvp.Key.Equals(rightAlias, StringComparison.OrdinalIgnoreCase))
                continue;

            var leftTable = schema.Tables.FirstOrDefault(t =>
                t.TableName.Equals(kvp.Value, StringComparison.OrdinalIgnoreCase) ||
                t.TableName.EndsWith("." + kvp.Value.Split('.').Last(), StringComparison.OrdinalIgnoreCase));
            if (leftTable == null)
                continue;

            var fkToRight = leftTable.ForeignKeys.FirstOrDefault(fk =>
                fk.ReferencedTable.Equals(rightTable.TableName.Split('.').Last(), StringComparison.OrdinalIgnoreCase) &&
                fk.ReferencedColumn.Equals(rightColumn, StringComparison.OrdinalIgnoreCase));
            if (fkToRight != null)
            {
                replacement = $"ON {kvp.Key}.{fkToRight.ColumnName} = {rightAlias}.{rightColumn}";
                break;
            }

            var pkInRight = rightTable.PrimaryKeys.Any(pk => pk.Equals(rightColumn, StringComparison.OrdinalIgnoreCase)) ||
                            rightTable.Columns.Any(c => c.ColumnName.Equals(rightColumn, StringComparison.OrdinalIgnoreCase) && c.IsPrimaryKey);
            if (pkInRight)
            {
                var fkInLeft = leftTable.ForeignKeys.FirstOrDefault(fk =>
                    fk.ReferencedTable.Equals(rightTable.TableName.Split('.').Last(), StringComparison.OrdinalIgnoreCase));
                if (fkInLeft != null)
                {
                    replacement = $"ON {kvp.Key}.{fkInLeft.ColumnName} = {rightAlias}.{fkInLeft.ReferencedColumn}";
                    break;
                }
            }
        }

        if (!string.IsNullOrEmpty(replacement))
            sql = Regex.Replace(sql, brokenOnPattern, replacement, RegexOptions.IgnoreCase);

        return sql;
    }

    private static string FixBrokenAggregatesAfterAliasRemoval(string sql, string removedAlias, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql) || !Regex.IsMatch(sql, $@"\b(COUNT|SUM|AVG|MIN|MAX)\s*\(\s*\)", RegexOptions.IgnoreCase))
            return sql;

        var aliasToTable = ExtractTableAliasesStatic(sql);
        var fallbackColumn = GetFallbackColumnForAggregate(aliasToTable, schema);
        if (string.IsNullOrEmpty(fallbackColumn))
            return sql;

        return Regex.Replace(sql, @"\b(COUNT|SUM|AVG|MIN|MAX)\s*\(\s*\)", $"$1({fallbackColumn})", RegexOptions.IgnoreCase);
    }

    private static Dictionary<string, string> ExtractTableAliasesStatic(string sql)
    {
        var aliasToTable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var tablePattern = @"(\[?[a-zA-Z0-9_]+\]?(?:\]?\.\[?[a-zA-Z0-9_]+\]?)?|""[^""]+""\.""[^""]+""|`[^`]+`(?:\.`[^`]+`)?)";
        var fromMatch = Regex.Match(sql, @"FROM\s+" + tablePattern + @"(?:\s+(?:AS\s+)?([a-zA-Z0-9_]+))?(?:\s|$|JOIN)", RegexOptions.IgnoreCase);
        if (fromMatch.Success)
        {
            var tableName = NormalizeTableName(fromMatch.Groups[1].Value);
            var alias = fromMatch.Groups[2].Value;
            if (string.IsNullOrWhiteSpace(alias))
            {
                var tableMatch = Regex.Match(tableName, @"[._]([^._]+)$");
                alias = tableMatch.Success ? tableMatch.Groups[1].Value : tableName.Split('.').Last().Split('_').Last();
            }
            if (!string.IsNullOrWhiteSpace(alias) && !IsSqlKeyword(alias))
                aliasToTable[alias] = tableName;
        }

        foreach (Match match in Regex.Matches(sql, @"(?:INNER|LEFT|RIGHT|FULL|CROSS)?\s*JOIN\s+" + tablePattern + @"(?:\s+(?:AS\s+)?([a-zA-Z0-9_]+))?(?:\s|$|ON)", RegexOptions.IgnoreCase))
        {
            var tableName = NormalizeTableName(match.Groups[1].Value);
            var alias = match.Groups[2].Value;
            if (string.IsNullOrWhiteSpace(alias))
            {
                var tableMatch = Regex.Match(tableName, @"\.([^.]+)$");
                alias = tableMatch.Success ? tableMatch.Groups[1].Value : tableName;
            }
            if (!string.IsNullOrWhiteSpace(alias) && !IsSqlKeyword(alias))
                aliasToTable[alias] = tableName;
        }

        return aliasToTable;
    }

    private static string GetFallbackColumnForAggregate(Dictionary<string, string> aliasToTable, DatabaseSchemaInfo schema)
    {
        foreach (var kvp in aliasToTable)
        {
            var table = schema.Tables.FirstOrDefault(t =>
                t.TableName.Equals(kvp.Value, StringComparison.OrdinalIgnoreCase) ||
                t.TableName.EndsWith($".{kvp.Value.Split('.').Last()}", StringComparison.OrdinalIgnoreCase));

            if (table == null)
                continue;

            var column = table.PrimaryKeys.FirstOrDefault() ?? table.Columns.FirstOrDefault()?.ColumnName;
            if (!string.IsNullOrEmpty(column))
                return $"{kvp.Key}.{column}";
        }

        return string.Empty;
    }

    private static string CleanupSqlAfterRemoval(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        sql = Regex.Replace(sql, @"SELECT\s*,\s*", "SELECT ", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @",\s*,\s*", ", ", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @",\s*(?=\s*FROM\b)", " ", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @",\s*(?=\s*ORDER\s+BY)", " ", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @",\s*(?=\s*GROUP\s+BY)", " ", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\s+(DESC|ASC)\s+(DESC|ASC)\b", " $1", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\s{2,}", " ", RegexOptions.IgnoreCase);
        return sql;
    }

    private async Task<string> ReplaceSourceColumnWithTargetInTargetDatabaseAsync(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var connections = await _connectionManager.GetAllConnectionsAsync();
        var targetMappings = connections
            .SelectMany(c => c.CrossDatabaseMappings ?? new List<CrossDatabaseMapping>())
            .Where(m => m.TargetDatabase.Equals(schema.DatabaseName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (targetMappings.Count == 0)
            return sql;

        var result = sql;
        foreach (var mapping in targetMappings)
        {
            var targetTableName = mapping.TargetTable.Contains('.')
                ? mapping.TargetTable.Split('.').Last()
                : mapping.TargetTable;

            var targetTable = schema.Tables.FirstOrDefault(t =>
                t.TableName.Equals(mapping.TargetTable, StringComparison.OrdinalIgnoreCase) ||
                t.TableName.EndsWith($".{targetTableName}", StringComparison.OrdinalIgnoreCase));

            if (targetTable == null)
                continue;

            var hasSourceColumn = targetTable.Columns.Any(c =>
                c.ColumnName.Equals(mapping.SourceColumn, StringComparison.OrdinalIgnoreCase));
            var hasTargetColumn = targetTable.Columns.Any(c =>
                c.ColumnName.Equals(mapping.TargetColumn, StringComparison.OrdinalIgnoreCase));

            if (hasSourceColumn || !hasTargetColumn)
                continue;

            var sourcePatterns = new[]
            {
                $@"\b{Regex.Escape(mapping.SourceColumn)}\b",
                $@"""{Regex.Escape(mapping.SourceColumn)}""",
                $@"\[{Regex.Escape(mapping.SourceColumn)}\]"
            };

            var targetReplacement = schema.DatabaseType == DatabaseType.PostgreSQL
                ? $"\"{mapping.TargetColumn}\""
                : schema.DatabaseType == DatabaseType.SqlServer
                    ? $"[{mapping.TargetColumn}]"
                    : mapping.TargetColumn;

            foreach (var pattern in sourcePatterns)
            {
                if (Regex.IsMatch(result, pattern, RegexOptions.IgnoreCase))
                {
                    result = Regex.Replace(result, pattern, targetReplacement, RegexOptions.IgnoreCase);
                    break;
                }
            }
        }

        return result;
    }

    private string FixConcatenatedTableReferences(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql) || schema?.Tables == null)
            return sql;

        var tablesInSql = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in Regex.Matches(sql, @"(?:FROM|JOIN)\s+([a-zA-Z0-9_]+\.([a-zA-Z0-9_]+))(?:\s|$|ON)", RegexOptions.IgnoreCase))
        {
            tablesInSql.Add(m.Groups[1].Value);
        }

        var replacements = new List<(string Original, string Replacement)>();

        foreach (Match match in Regex.Matches(sql, @"\b([a-zA-Z0-9_]+)\.([a-zA-Z0-9_]+)\b", RegexOptions.IgnoreCase))
        {
            var prefix = match.Groups[1].Value;
            var columnName = match.Groups[2].Value;
            if (columnName.Equals("*", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var table in schema.Tables)
            {
                if (!table.TableName.Contains('.'))
                    continue;
                var parts = table.TableName.Split('.', 2);
                var concatenated = parts[0] + parts[1];
                if (!prefix.Equals(concatenated, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!table.Columns.Any(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (!tablesInSql.Contains(table.TableName))
                    continue;
                var correctAlias = parts[1];
                if (!replacements.Any(r => r.Original.Equals(match.Value, StringComparison.OrdinalIgnoreCase)))
                    replacements.Add((match.Value, $"{correctAlias}.{columnName}"));
                break;
            }
        }

        foreach (var (original, replacement) in replacements.OrderByDescending(r => r.Original.Length))
            sql = Regex.Replace(sql, Regex.Escape(original), replacement, RegexOptions.IgnoreCase);

        return sql;
    }

    private static bool IsCountLikeAlias(string alias)
    {
        if (string.IsNullOrEmpty(alias)) return false;
        return alias.Equals("Count", StringComparison.OrdinalIgnoreCase) ||
               alias.EndsWith("Count", StringComparison.OrdinalIgnoreCase);
    }

    private string FixColumnNameUsedAsFunction(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql) || schema?.Tables == null)
            return sql;

        var sqlFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "COUNT", "SUM", "AVG", "MIN", "MAX", "COALESCE", "NULLIF", "CAST", "CONVERT", "ISNULL", "LEN", "LEFT", "RIGHT", "SUBSTRING", "UPPER", "LOWER", "RTRIM", "LTRIM", "TRIM", "GETDATE", "DATEADD", "DATEDIFF", "YEAR", "MONTH", "DAY", "ABS", "ROUND", "CEILING", "FLOOR" };

        var schemaColumns = schema.Tables
            .SelectMany(t => t.Columns.Select(c => c.ColumnName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var searchStart = 0;
        Match match;
        while ((match = Regex.Match(sql.Substring(searchStart), @"\b([a-zA-Z0-9_]+)\s*\(\s*", RegexOptions.IgnoreCase)).Success)
        {
            var start = searchStart + match.Index;
            var identifier = match.Groups[1].Value;
            var isColumn = schemaColumns.Contains(identifier) ||
                (identifier.Length > 2 && char.IsLetter(identifier[0]) &&
                 new[] { "ID", "Quantity", "Price", "Discount", "Amount", "Count", "Date", "Name", "Number" }
                     .Any(s => identifier.EndsWith(s, StringComparison.OrdinalIgnoreCase)));
            if (sqlFunctions.Contains(identifier) || !isColumn)
            {
                searchStart = start + 1;
                continue;
            }

            var parenStart = start + match.Length;
            var depth = 1;
            var i = parenStart;
            while (i < sql.Length && depth > 0)
            {
                var c = sql[i++];
                if (c == '(') depth++;
                else if (c == ')') depth--;
            }
            if (depth != 0)
            {
                searchStart = start + 1;
                continue;
            }
            var end = i;
            var inner = sql[(parenStart + 1)..(end - 1)].Trim();
            var afterParen = sql.Length > end ? sql[end..].TrimStart() : "";
            var asMatch = Regex.Match(afterParen, @"AS\s+([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase);
            var useCount = afterParen.StartsWith("AS ", StringComparison.OrdinalIgnoreCase) &&
                asMatch.Success && IsCountLikeAlias(asMatch.Groups[1].Value);
            var replacement = useCount ? $"COUNT({inner})" : identifier;
            sql = sql.Remove(start, end - start).Insert(start, replacement);
            searchStart = start + replacement.Length;
        }

        return sql;
    }

    private string ReplaceInvalidColumnReferences(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var aliasToTable = ExtractTableAliases(sql);
        sql = ReplaceQualifiedInvalidColumnReferences(sql, schema, aliasToTable);

        if (aliasToTable.Count > 0)
            sql = ReplaceUnqualifiedInvalidColumnReferences(sql, schema, aliasToTable.Values.ToHashSet());

        return sql;
    }

    private string AddMissingSelectColumnsToGroupBy(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql)) return sql;

        var groupByMatch = Regex.Match(sql, @"\bGROUP\s+BY\s+(.*?)(?=\s+ORDER|\s+HAVING|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!groupByMatch.Success) return sql;

        var selectMatch = Regex.Match(sql, @"SELECT\s+(?:TOP\s+\d+\s+)?(.*?)\s+FROM", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!selectMatch.Success) return sql;

        var selectColumns = SplitColumnListByComma(selectMatch.Groups[1].Value);
        var groupByColumns = SplitColumnListByComma(groupByMatch.Groups[1].Value);

        var aggregatePattern = new Regex(@"\b(?:COUNT|SUM|AVG|MIN|MAX)\s*\([^)]*\)", RegexOptions.IgnoreCase);
        var groupBySet = groupByColumns
            .Select(NormalizeColumnRefForGroupBy)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = new List<string>();
        foreach (var sel in selectColumns)
        {
            var trimmed = sel.Trim();
            if (aggregatePattern.IsMatch(trimmed)) continue;
            var normalized = NormalizeColumnRefForGroupBy(trimmed);
            if (string.IsNullOrEmpty(normalized) || groupBySet.Contains(normalized)) continue;
            groupBySet.Add(normalized);
            toAdd.Add(trimmed);
        }

        if (toAdd.Count == 0) return sql;

        var newGroupBy = groupByMatch.Groups[1].Value.Trim() + ", " + string.Join(", ", toAdd);
        return sql[..groupByMatch.Groups[1].Index] + newGroupBy + sql[(groupByMatch.Groups[1].Index + groupByMatch.Groups[1].Length)..];
    }

    private static string NormalizeColumnRefForGroupBy(string colRef)
    {
        var t = colRef.Trim().Trim('"', '[', ']');
        var asIdx = t.IndexOf(" AS ", StringComparison.OrdinalIgnoreCase);
        if (asIdx >= 0) t = t[..asIdx].Trim();
        var parts = t.Split('.');
        return parts[^1];
    }

    private string RemoveInvalidColumnsFromSelectAndGroupBy(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var aliasToTable = ExtractTableAliases(sql);
        var tablesInQuery = aliasToTable.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);

        bool IsColumnValid(string columnRef)
        {
            if (string.IsNullOrWhiteSpace(columnRef))
                return true;

            var trimmed = columnRef.Trim().Trim('"', '[', ']');
            var parts = trimmed.Split('.');
            string tableName;
            string columnName;

            if (parts.Length == 3)
            {
                tableName = $"{parts[0].Trim('"', '[', ']')}.{parts[1].Trim('"', '[', ']')}";
                columnName = parts[2].Trim('"', '[', ']');
            }
            else if (parts.Length == 2 && aliasToTable.TryGetValue(parts[0].Trim('"', '[', ']'), out var tbl))
            {
                tableName = tbl;
                columnName = parts[1].Trim('"', '[', ']');
            }
            else if (parts.Length == 1)
            {
                if (tablesInQuery.Count != 1)
                    return true;
                tableName = tablesInQuery.First();
                columnName = parts[0].Trim('"', '[', ']');
            }
            else
            {
                return true;
            }

            var table = schema.Tables.FirstOrDefault(t =>
                t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                t.TableName.EndsWith($".{tableName.Split('.').Last()}", StringComparison.OrdinalIgnoreCase));

            return table != null && table.Columns.Any(c =>
                c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));
        }

        sql = RemoveInvalidColumnsFromClause(sql, @"SELECT\s+(.*?)\s+FROM", IsColumnValid);
        sql = RemoveInvalidColumnsFromClause(sql, @"GROUP\s+BY\s+(.*?)(?=\s+ORDER|\s+HAVING|$)", IsColumnValid);
        sql = CleanupSqlAfterRemoval(sql);

        return sql;
    }

    private static List<string> SplitColumnListByComma(string columnList)
    {
        var result = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < columnList.Length; i++)
        {
            var c = columnList[i];
            if (c == '(')
                depth++;
            else if (c == ')')
                depth--;
            else if (c == ',' && depth == 0)
            {
                result.Add(columnList[start..i].Trim());
                start = i + 1;
            }
        }
        if (start < columnList.Length)
            result.Add(columnList[start..].Trim());
        return result.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
    }

    private static string RemoveInvalidColumnsFromClause(string sql, string clausePattern, Func<string, bool> isColumnValid)
    {
        var match = Regex.Match(sql, clausePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
            return sql;

        var columnList = match.Groups[1].Value;
        var columns = SplitColumnListByComma(columnList);

        var validColumns = new List<string>();
        foreach (var col in columns)
        {
            var innerParts = Regex.Split(col, @"\s+(?:AS\s+)?")
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();
            var columnRef = innerParts.FirstOrDefault() ?? col;
            if (columnRef.Equals("*", StringComparison.OrdinalIgnoreCase) ||
                Regex.IsMatch(columnRef, @"\b(?:COUNT|SUM|AVG|MIN|MAX)\s*\(|^\d+$", RegexOptions.IgnoreCase))
            {
                validColumns.Add(col);
                continue;
            }
            if (isColumnValid(columnRef))
                validColumns.Add(col);
        }

        if (validColumns.Count == columns.Count)
            return sql;

        var newColumnList = string.Join(", ", validColumns);
        return sql.Substring(0, match.Groups[1].Index) +
               newColumnList +
               sql.Substring(match.Groups[1].Index + match.Groups[1].Length);
    }

    private static string ReplaceUnqualifiedInvalidColumnReferences(
        string sql,
        DatabaseSchemaInfo schema,
        HashSet<string> tableNamesInQuery)
    {
        var validColumns = schema.Tables
            .SelectMany(t => t.Columns)
            .Select(c => c.ColumnName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var tablesInQuery = schema.Tables
            .Where(t => tableNamesInQuery.Contains(t.TableName) ||
                        tableNamesInQuery.Any(tn => t.TableName.EndsWith($".{tn.Split('.').Last()}", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var standaloneMatches = Regex.Matches(sql, @"\b([a-zA-Z0-9_]+)\b", RegexOptions.IgnoreCase);

        var replacements = new List<(string Original, string Replacement)>();
        foreach (Match match in standaloneMatches)
        {
            var word = match.Groups[1].Value;
            if (word.Length < 4 || validColumns.Contains(word))
                continue;

            if (IsSqlKeyword(word))
                continue;

            var before = match.Index > 0 ? sql[match.Index - 1] : ' ';
            if (before == '.')
                continue;

            string fallback = null;
            foreach (var table in tablesInQuery)
            {
                fallback = TryResolveSimilarColumnFromSchema(table, word);
                if (!string.IsNullOrEmpty(fallback))
                    break;
            }

            if (string.IsNullOrEmpty(fallback))
                continue;

            if (!replacements.Any(r => r.Original.Equals(word, StringComparison.OrdinalIgnoreCase)))
                replacements.Add((word, fallback));
        }

        foreach (var (original, replacement) in replacements.OrderByDescending(r => r.Original.Length))
        {
            sql = Regex.Replace(sql, $@"\b{Regex.Escape(original)}\b", replacement, RegexOptions.IgnoreCase);
        }

        return sql;
    }

    private static string FixOrderByReferenceToRemovedAlias(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql) || !Regex.IsMatch(sql, @"\bORDER\s+BY\b", RegexOptions.IgnoreCase))
            return sql;

        var validColumnNames = schema.Tables
            .SelectMany(t => t.Columns)
            .Select(c => c.ColumnName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orderByMatch = Regex.Match(sql, @"ORDER\s+BY\s+([\s\S]+?)(?=\s+OFFSET\s|\s+FETCH\s|\s+ROW_NUMBER\s|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!orderByMatch.Success)
            return sql;

        var orderByClause = orderByMatch.Groups[1].Value.Trim();
        var orderByWords = Regex.Matches(orderByClause, @"\b([a-zA-Z0-9_]+)\b")
            .Select(m => m.Groups[1].Value)
            .Where(w => !IsSqlKeyword(w) && !w.Equals("1", StringComparison.OrdinalIgnoreCase) && !w.Equals("2", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var word in orderByWords)
        {
            if (validColumnNames.Contains(word))
                continue;

            sql = Regex.Replace(sql, @"(ORDER\s+BY\s+)([\s\S]+?)(?=\s+OFFSET\s|\s+FETCH\s|\s+ROW_NUMBER\s|$)", match =>
            {
                var prefix = match.Groups[1].Value;
                var clause = match.Groups[2].Value;
                var updated = Regex.Replace(clause, $@"\b{Regex.Escape(word)}\b", "1", RegexOptions.IgnoreCase);
                return prefix + updated;
            }, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            break;
        }

        return sql;
    }

    private static string ReplaceQualifiedInvalidColumnReferences(
        string sql,
        DatabaseSchemaInfo schema,
        Dictionary<string, string> aliasToTable)
    {
        var columnMatches = Regex.Matches(sql, @"\b([a-zA-Z0-9_]+)\.([a-zA-Z0-9_]+)\b", RegexOptions.IgnoreCase);
        var replacements = new List<(string Original, string Replacement)>();

        foreach (Match match in columnMatches)
        {
            var alias = match.Groups[1].Value;
            var columnName = match.Groups[2].Value;

            if (columnName.Equals("*", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!aliasToTable.TryGetValue(alias, out var tableName))
                continue;

            var table = schema.Tables.FirstOrDefault(t =>
                t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                t.TableName.EndsWith($".{tableName.Split('.').Last()}", StringComparison.OrdinalIgnoreCase));

            if (table == null)
                continue;

            var columnExists = table.Columns.Any(c =>
                c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));

            if (columnExists)
                continue;

            var fallbackColumn = TryResolveSimilarColumnFromSchema(table, columnName) ??
                                table.PrimaryKeys.FirstOrDefault() ??
                                table.Columns.FirstOrDefault()?.ColumnName;
            if (string.IsNullOrEmpty(fallbackColumn))
                continue;

            var original = match.Value;
            var replacement = $"{alias}.{fallbackColumn}";
            if (!replacements.Any(r => r.Original.Equals(original, StringComparison.OrdinalIgnoreCase)))
            {
                replacements.Add((original, replacement));
            }
        }

        foreach (var (original, replacement) in replacements)
        {
            sql = Regex.Replace(sql, Regex.Escape(original), replacement, RegexOptions.IgnoreCase);
        }

        return sql;
    }

    private static string? TryResolveSimilarColumnFromSchema(TableSchemaInfo table, string invalidColumn)
    {
        return table.Columns
            .Where(c => c.ColumnName.Length > invalidColumn.Length &&
                        c.ColumnName.EndsWith(invalidColumn, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.ColumnName.Length)
            .Select(c => c.ColumnName)
            .FirstOrDefault();
    }

    private string FixInvalidColumnsInAggregatesAndAddMissingJoins(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var aliasToTable = ExtractTableAliases(sql);
        var tablesInQuery = aliasToTable.Values.Select(NormalizeTableName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var qualifiedAggMatch = Regex.Match(sql, @"\b(COUNT|SUM|AVG|MIN|MAX)\s*\(\s*([a-zA-Z0-9_]+)\.([a-zA-Z0-9_]+)\s*\)", RegexOptions.IgnoreCase);
        if (qualifiedAggMatch.Success)
        {
            var aggFunc = qualifiedAggMatch.Groups[1].Value;
            var alias = qualifiedAggMatch.Groups[2].Value;
            var aggColumn = qualifiedAggMatch.Groups[3].Value;
            var aliasTable = aliasToTable.GetValueOrDefault(alias) ?? alias;
            var tableInfo = schema.Tables.FirstOrDefault(t =>
                t.TableName.Equals(aliasTable, StringComparison.OrdinalIgnoreCase) ||
                t.TableName.EndsWith($".{aliasTable.Split('.').Last()}", StringComparison.OrdinalIgnoreCase) ||
                t.TableName.Replace(".", "_").Equals(aliasTable.Replace(".", "_"), StringComparison.OrdinalIgnoreCase));
            if (tableInfo == null || tableInfo.Columns.Any(c => c.ColumnName.Equals(aggColumn, StringComparison.OrdinalIgnoreCase)))
            {
            }
            else
            {
                var found = FindCorrectColumnForInvalidAggregate(aggColumn, schema, tablesInQuery);
                if (found.HasValue)
                {
                    var (targetTable, targetColumn) = found.Value;
                    var joinInfo = FindJoinPathToTable(schema, aliasTable, targetTable);
                    if (joinInfo != null)
                    {
                        var newAlias = GetShortAlias(targetTable);
                        var onCond = ReplaceFromTableWithAliasInOnCondition(joinInfo.Value.OnCondition, aliasTable, alias);
                        var joinClause = schema.DatabaseType == DatabaseType.MySQL
                            ? $" INNER JOIN `{joinInfo.Value.TableRef.Replace(".", "`.`")}` {newAlias} ON {QuoteIdentifiersForMySQL(onCond, alias, newAlias)}"
                            : schema.DatabaseType == DatabaseType.PostgreSQL
                                ? $" INNER JOIN \"{joinInfo.Value.TableRef.Replace(".", "\".\"")}\" {newAlias} ON {onCond}"
                                : $" INNER JOIN [{joinInfo.Value.TableRef.Replace(".", "].[")}] {newAlias} ON {onCond}";
                        var fromJoinMatch = Regex.Match(sql, @"(FROM\s+[^\s]+(?:\s+(?:AS\s+)?[a-zA-Z0-9_]+)?)(\s+WHERE|\s+GROUP|\s+ORDER|\s+HAVING|$)", RegexOptions.IgnoreCase);
                        if (fromJoinMatch.Success)
                        {
                            var insertPos = fromJoinMatch.Groups[1].Length;
                            sql = sql.Insert(insertPos, joinClause);
                            var quotedCol = schema.DatabaseType == DatabaseType.PostgreSQL ? $"\"{targetColumn}\"" : schema.DatabaseType == DatabaseType.SqlServer ? $"[{targetColumn}]" : schema.DatabaseType == DatabaseType.MySQL ? $"`{targetColumn}`" : targetColumn;
                            var newColumnRef = $"{newAlias}.{quotedCol}";
                            sql = Regex.Replace(sql, $@"\b(COUNT|SUM|AVG|MIN|MAX)\s*\(\s*{Regex.Escape(alias)}\.{Regex.Escape(aggColumn)}\s*\)", $"$1({newColumnRef})", RegexOptions.IgnoreCase);
                            return sql;
                        }
                    }
                }
            }
        }

        var aggMatch = Regex.Match(sql, @"\b(COUNT|SUM|AVG|MIN|MAX)\s*\(\s*([a-zA-Z0-9_]+)\s*\)", RegexOptions.IgnoreCase);
        if (!aggMatch.Success)
            return sql;

        var aggColumnUnq = aggMatch.Groups[2].Value;
        var columnExists = schema.Tables
            .Where(t => tablesInQuery.Contains(t.TableName) || tablesInQuery.Contains(t.TableName.Split('.').Last()))
            .Any(t => t.Columns.Any(c => c.ColumnName.Equals(aggColumnUnq, StringComparison.OrdinalIgnoreCase)));
        if (columnExists)
            return sql;

        var foundUnq = FindCorrectColumnForInvalidAggregate(aggColumnUnq, schema, tablesInQuery);
        if (!foundUnq.HasValue)
            return sql;
        var (targetTableUnq, targetColumnUnq) = foundUnq.Value;

        var fromTable = aliasToTable.Values.FirstOrDefault();
        if (string.IsNullOrEmpty(fromTable))
            return sql;

        var joinInfoUnq = FindJoinPathToTable(schema, fromTable, targetTableUnq);
        if (joinInfoUnq == null)
            return sql;

        var newAliasUnq = GetShortAlias(targetTableUnq);
        var joinClauseUnq = $" LEFT JOIN {joinInfoUnq.Value.TableRef} {newAliasUnq} ON {joinInfoUnq.Value.OnCondition}";
        var fromJoinMatchUnq = Regex.Match(sql, @"(FROM\s+[^\s]+(?:\s+(?:AS\s+)?[a-zA-Z0-9_]+)?)(\s+WHERE|\s+GROUP|\s+ORDER|\s+HAVING|$)", RegexOptions.IgnoreCase);
        if (!fromJoinMatchUnq.Success)
            return sql;

        var insertPosUnq = fromJoinMatchUnq.Groups[1].Length;
        sql = sql.Insert(insertPosUnq, joinClauseUnq);

        var quotedColUnq = schema.DatabaseType == DatabaseType.PostgreSQL ? $"\"{targetColumnUnq}\"" : schema.DatabaseType == DatabaseType.SqlServer ? $"[{targetColumnUnq}]" : targetColumnUnq;
        var newColumnRefUnq = $"{newAliasUnq}.{quotedColUnq}";
        sql = Regex.Replace(sql, $@"\b(COUNT|SUM|AVG|MIN|MAX)\s*\(\s*{Regex.Escape(aggColumnUnq)}\s*\)", $"$1({newColumnRefUnq})", RegexOptions.IgnoreCase);

        return sql;
    }

    private static string ReplaceFromTableWithAliasInOnCondition(string onCondition, string fromTable, string alias)
    {
        var fromPart = fromTable.Split('.').Last();
        return Regex.Replace(onCondition, $@"\b{Regex.Escape(fromPart)}\.", $"{alias}.", RegexOptions.IgnoreCase);
    }

    private static string QuoteIdentifiersForMySQL(string onCondition, string alias, string newAlias)
    {
        return Regex.Replace(onCondition, @"(\w+)\.(\w+)", m =>
        {
            var left = m.Groups[1].Value;
            var col = m.Groups[2].Value;
            var part = left.Equals(alias, StringComparison.OrdinalIgnoreCase) ? $"`{alias}`.`{col}`" : $"`{newAlias}`.`{col}`";
            return part;
        });
    }

    private static (string Table, string Column)? FindCorrectColumnForInvalidAggregate(string invalidColumn, DatabaseSchemaInfo schema, HashSet<string> tablesInQuery)
    {
        var variants = new List<string> { invalidColumn };
        if (invalidColumn.EndsWith("HeaderID", StringComparison.OrdinalIgnoreCase))
            variants.Add(invalidColumn[..^7] + "ID");
        if (invalidColumn.EndsWith("ID", StringComparison.OrdinalIgnoreCase) && invalidColumn.Contains("Header", StringComparison.OrdinalIgnoreCase))
        {
            var withoutHeader = Regex.Replace(invalidColumn, "Header", "", RegexOptions.IgnoreCase);
            if (!string.IsNullOrEmpty(withoutHeader))
                variants.Add(withoutHeader);
        }

        foreach (var variant in variants)
        {
            foreach (var table in schema.Tables)
            {
                if (tablesInQuery.Contains(table.TableName) || tablesInQuery.Contains(table.TableName.Split('.').Last()))
                    continue;
                var col = table.Columns.FirstOrDefault(c => c.ColumnName.Equals(variant, StringComparison.OrdinalIgnoreCase));
                if (col != null)
                    return (table.TableName, col.ColumnName);
            }
        }
        return null;
    }

    private static (string TableRef, string OnCondition)? FindJoinPathToTable(DatabaseSchemaInfo schema, string fromTable, string targetTable)
    {
        var fromNorm = NormalizeTableName(fromTable);
        var targetNorm = NormalizeTableName(targetTable);
        var fromTableInfo = schema.Tables.FirstOrDefault(t => t.TableName.Equals(fromNorm, StringComparison.OrdinalIgnoreCase) || t.TableName.EndsWith($".{fromNorm.Split('.').Last()}", StringComparison.OrdinalIgnoreCase));
        var targetTableInfo = schema.Tables.FirstOrDefault(t => t.TableName.Equals(targetNorm, StringComparison.OrdinalIgnoreCase) || t.TableName.EndsWith($".{targetNorm.Split('.').Last()}", StringComparison.OrdinalIgnoreCase));
        if (fromTableInfo == null || targetTableInfo == null)
            return null;

        var targetFk = targetTableInfo.ForeignKeys.FirstOrDefault(fk =>
            fk.ReferencedTable.Equals(fromTableInfo.TableName, StringComparison.OrdinalIgnoreCase) ||
            fk.ReferencedTable.EndsWith($".{fromTableInfo.TableName.Split('.').Last()}", StringComparison.OrdinalIgnoreCase));
        if (targetFk == null)
            return null;

        var fromQualifier = fromTableInfo.TableName;
        var targetAlias = GetShortAlias(targetTableInfo.TableName);
        var tableRef = targetTableInfo.TableName;
        var onCondition = $"{fromQualifier}.{targetFk.ReferencedColumn} = {targetAlias}.{targetFk.ColumnName}";
        return (tableRef, onCondition);
    }

    private static string GetShortAlias(string tableName)
    {
        var last = tableName.Split('.').Last();
        if (last.Length <= 3) return last.ToLowerInvariant();
        var caps = Regex.Matches(last, @"[A-Z]").Select(m => m.Value.ToLowerInvariant()).Take(4);
        var abbrev = string.Join("", caps);
        return string.IsNullOrEmpty(abbrev) ? last[..Math.Min(4, last.Length)].ToLowerInvariant() : abbrev;
    }

    private string FixInvalidFromClauses(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var fixedSql = sql;

        var invalidPatterns = new[]
        {
            @"FROM\s+GROUP\s+BY",
            @"FROM\s+WHERE\s+",
            @"FROM\s+ORDER\s+BY",
            @"FROM\s+HAVING\s+",
            @"FROM\s+LIMIT\s+",
            @"FROM\s+\)",
            @"FROM\s*$",
            @"FROM\s+\s+GROUP"
        };

        if (!invalidPatterns.Any(pattern => Regex.IsMatch(fixedSql, pattern, RegexOptions.IgnoreCase)))
            return fixedSql;

        DatabaseLogMessages.LogDetectedInvalidFromClause(_logger, null);

        fixedSql = Regex.Replace(fixedSql, @"FROM\s+GROUP\s+BY", "WHERE 1=0 GROUP BY", RegexOptions.IgnoreCase);
        fixedSql = Regex.Replace(fixedSql, @"FROM\s+WHERE\s+", "FROM (SELECT NULL) AS InvalidTable WHERE ", RegexOptions.IgnoreCase);
        fixedSql = Regex.Replace(fixedSql, @"FROM\s+ORDER\s+BY", "FROM (SELECT NULL) AS InvalidTable ORDER BY", RegexOptions.IgnoreCase);
        fixedSql = Regex.Replace(fixedSql, @"FROM\s+HAVING\s+", "FROM (SELECT NULL) AS InvalidTable HAVING ", RegexOptions.IgnoreCase);
        fixedSql = Regex.Replace(fixedSql, @"FROM\s+LIMIT\s+", "FROM (SELECT NULL) AS InvalidTable LIMIT ", RegexOptions.IgnoreCase);

        var fromSubqueryMatch = Regex.Match(fixedSql, @"\(([^)]*FROM\s*)\)", RegexOptions.IgnoreCase);
        if (fromSubqueryMatch.Success)
        {
            fixedSql = fixedSql.Replace(fromSubqueryMatch.Value, "(SELECT NULL) AS InvalidSubquery");
        }

        return fixedSql;
    }

    /// <summary>
    /// Fixes ambiguous column names in JOIN queries by adding table aliases
    /// </summary>
    private string FixAmbiguousColumnsInJoin(string sql, DatabaseSchemaInfo schema)
    {
        var hasJoin = Regex.IsMatch(sql, @"\b(?:INNER|LEFT|RIGHT|FULL|CROSS)?\s*JOIN\b", RegexOptions.IgnoreCase);
        var hasMultipleFrom = Regex.Matches(sql, @"\bFROM\b", RegexOptions.IgnoreCase).Count > 1 ||
            Regex.IsMatch(sql, @"FROM\s+[^,\s]+\s*,\s*[^,\s]+", RegexOptions.IgnoreCase);
        if (string.IsNullOrWhiteSpace(sql) || (!hasJoin && !hasMultipleFrom))
            return sql;

        var aliasToTable = ExtractTableAliases(sql);
        if (aliasToTable.Count == 0)
        {
            aliasToTable = ExtractTableAliasesFallback(sql);
        }
        if (aliasToTable.Count == 0)
        {
            return sql;
        }

        var columnToTables = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var table in schema.Tables)
        {
            foreach (var column in table.Columns)
            {
                if (!columnToTables.ContainsKey(column.ColumnName))
                {
                    columnToTables[column.ColumnName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
                columnToTables[column.ColumnName].Add(table.TableName);
            }
        }

        var ambiguousColumns = columnToTables
            .Where(kvp => kvp.Value.Count > 1)
            .Select(kvp => kvp.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var onClauseAmbiguous = ExtractAmbiguousColumnsFromOnClause(sql);
        foreach (var col in onClauseAmbiguous)
        {
            ambiguousColumns.Add(col);
        }

        if (ambiguousColumns.Count == 0)
        {
            return sql;
        }

        var usedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var alias in aliasToTable.Keys)
        {
            usedTables.Add(aliasToTable[alias]);
        }

        var fixedSql = sql;

        const string selectPattern = @"SELECT\s+(.*?)\s+FROM";
        var selectMatch = Regex.Match(fixedSql, selectPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (selectMatch.Success)
        {
            var selectClause = selectMatch.Groups[1].Value;
            var fixedSelectClause = FixColumnReferences(selectClause, ambiguousColumns, aliasToTable, usedTables);
            if (fixedSelectClause != selectClause)
            {
                fixedSql = fixedSql[..selectMatch.Groups[1].Index] +
                           fixedSelectClause +
                           fixedSql[(selectMatch.Groups[1].Index + selectMatch.Groups[1].Length)..];
            }
        }

        const string groupByPattern = @"GROUP\s+BY\s+(.*?)(?:\s+ORDER|\s+HAVING|$)";
        var groupByMatch = Regex.Match(fixedSql, groupByPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (groupByMatch.Success)
        {
            var groupByClause = groupByMatch.Groups[1].Value;
            var fixedGroupByClause = FixColumnReferences(groupByClause, ambiguousColumns, aliasToTable, usedTables);
            if (fixedGroupByClause != groupByClause)
            {
                fixedSql = fixedSql[..groupByMatch.Groups[1].Index] +
                           fixedGroupByClause +
                           fixedSql[(groupByMatch.Groups[1].Index + groupByMatch.Groups[1].Length)..];
            }
        }

        const string orderByPattern = @"ORDER\s+BY\s+(.*?)(?:\s+(?:LIMIT|TOP)|$)";
        var orderByMatch = Regex.Match(fixedSql, orderByPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (orderByMatch.Success)
        {
            var orderByClause = orderByMatch.Groups[1].Value;
            var fixedOrderByClause = FixColumnReferences(orderByClause, ambiguousColumns, aliasToTable, usedTables);
            if (fixedOrderByClause != orderByClause)
            {
                fixedSql = fixedSql[..orderByMatch.Groups[1].Index] +
                           fixedOrderByClause +
                           fixedSql[(orderByMatch.Groups[1].Index + orderByMatch.Groups[1].Length)..];
            }
        }

        var whereMatch = Regex.Match(fixedSql, @"WHERE\s+(.*?)(?:\s+GROUP|\s+ORDER|\s+HAVING|\s+LIMIT|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (whereMatch.Success)
        {
            var whereClause = whereMatch.Groups[1].Value;
            var fixedWhereClause = FixColumnReferences(whereClause, ambiguousColumns, aliasToTable, usedTables);
            if (fixedWhereClause != whereClause)
            {
                fixedSql = fixedSql[..whereMatch.Groups[1].Index] +
                           fixedWhereClause +
                           fixedSql[(whereMatch.Groups[1].Index + whereMatch.Groups[1].Length)..];
            }
        }

        fixedSql = FixOnClauseAmbiguousColumns(fixedSql, ambiguousColumns, aliasToTable, usedTables);

        fixedSql = ReplaceAllAmbiguousBareColumns(fixedSql, ambiguousColumns, aliasToTable, usedTables);
        return fixedSql;
    }

    private string FixColumnReferences(string clause, HashSet<string> ambiguousColumns, Dictionary<string, string> aliasToTable, HashSet<string> usedTables)
    {
        var parts = new List<string>();
        var currentPart = new StringBuilder();
        var parenDepth = 0;
        var inQuotes = false;
        var quoteChar = '\0';

        foreach (var ch in clause)
        {
            switch (inQuotes)
            {
                case false when ch is '\'' or '"':
                    inQuotes = true;
                    quoteChar = ch;
                    currentPart.Append(ch);
                    break;
                case true when ch == quoteChar:
                    inQuotes = false;
                    quoteChar = '\0';
                    currentPart.Append(ch);
                    break;
                case false when ch == '(':
                    parenDepth++;
                    currentPart.Append(ch);
                    break;
                case false when ch == ')':
                    parenDepth--;
                    currentPart.Append(ch);
                    break;
                case false when ch == ',' && parenDepth == 0:
                {
                    var part = currentPart.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(part))
                    {
                        parts.Add(FixSingleColumnReference(part, ambiguousColumns, aliasToTable, usedTables));
                    }
                    currentPart.Clear();
                    break;
                }
                default:
                    currentPart.Append(ch);
                    break;
            }
        }

        var lastPart = currentPart.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(lastPart))
        {
            parts.Add(FixSingleColumnReference(lastPart, ambiguousColumns, aliasToTable, usedTables));
        }

        return string.Join(", ", parts);
    }

    private string FixSingleColumnReference(string columnRef, HashSet<string> ambiguousColumns, Dictionary<string, string> aliasToTable, HashSet<string> usedTables)
    {
        columnRef = columnRef.Trim();

        if (string.IsNullOrWhiteSpace(columnRef))
            return columnRef;

        var trimmed = columnRef.Trim();

        if (trimmed.Contains(".") && !Regex.IsMatch(trimmed, @"\b(?:COUNT|SUM|AVG|MAX|MIN)\s*\("))
        {
            return columnRef;
        }

        string fixedRef;
        if (Regex.IsMatch(trimmed, @"\b(?:COUNT|SUM|AVG|MAX|MIN|CONCAT|UPPER|LOWER|SUBSTRING|CAST|CONVERT)\s*\(.*\)", RegexOptions.IgnoreCase))
        {
            var functionMatch = Regex.Match(trimmed, @"\b(?:COUNT|SUM|AVG|MAX|MIN|CONCAT|UPPER|LOWER|SUBSTRING|CAST|CONVERT)\s*\(([^)]+)\)", RegexOptions.IgnoreCase);
            if (!functionMatch.Success)
                return ReplaceAllAmbiguousBareColumns(columnRef, ambiguousColumns, aliasToTable, usedTables);

            var innerColumn = functionMatch.Groups[1].Value.Trim();
            if (!Enumerable.Contains(ambiguousColumns, innerColumn, StringComparer.OrdinalIgnoreCase) ||
                innerColumn.Contains("."))
                return ReplaceAllAmbiguousBareColumns(columnRef, ambiguousColumns, aliasToTable, usedTables);

            var functionBestAlias = FindBestAliasForColumn(aliasToTable, usedTables);
            if (string.IsNullOrEmpty(functionBestAlias))
                return ReplaceAllAmbiguousBareColumns(columnRef, ambiguousColumns, aliasToTable, usedTables);

            var fixedInner = $"{functionBestAlias}.{innerColumn}";
            fixedRef = columnRef.Replace(innerColumn, fixedInner);
            return ReplaceAllAmbiguousBareColumns(fixedRef, ambiguousColumns, aliasToTable, usedTables);
        }

        if (trimmed.Contains(" AS ", StringComparison.OrdinalIgnoreCase))
        {
            var asMatch = Regex.Match(trimmed, @"^(.+?)\s+AS\s+", RegexOptions.IgnoreCase);
            if (asMatch.Success)
            {
                var beforeAs = asMatch.Groups[1].Value.Trim();
                return FixSingleColumnReference(beforeAs, ambiguousColumns, aliasToTable, usedTables) + " " + trimmed.Substring(asMatch.Index + asMatch.Length);
            }
        }

        return ReplaceAllAmbiguousBareColumns(columnRef, ambiguousColumns, aliasToTable, usedTables);
    }

    private string FixOnClauseAmbiguousColumns(string sql, HashSet<string> ambiguousColumns, Dictionary<string, string> aliasToTable, HashSet<string> usedTables)
    {
        var onPattern = new Regex(@"\bON\s+(.+?)(?=\b(?:LEFT|RIGHT|INNER|FULL|CROSS)?\s*JOIN\b|\bWHERE\b|\bGROUP\b|\bORDER\b|\bHAVING\b|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return onPattern.Replace(sql, m =>
        {
            var onClause = m.Groups[1].Value;
            var fixedOn = FixColumnReferences(onClause, ambiguousColumns, aliasToTable, usedTables);
            if (fixedOn == onClause)
                return m.Value;
            var nextIdx = m.Index + m.Length;
            var needsSpace = nextIdx < sql.Length && char.IsLetterOrDigit(sql[nextIdx]);
            return "ON " + fixedOn + (needsSpace ? " " : "");
        });
    }

    private static string ReplaceAllAmbiguousBareColumns(string text, HashSet<string> ambiguousColumns, Dictionary<string, string> aliasToTable, HashSet<string> usedTables)
    {
        var sqlKeywords = new[] { "SELECT", "FROM", "WHERE", "JOIN", "ON", "AND", "OR", "GROUP", "BY", "ORDER", "HAVING", "AS", "COUNT", "SUM", "AVG", "MAX", "MIN", "TOP", "LIMIT" };
        var bestAlias = FindBestAliasForColumn(aliasToTable, usedTables);
        if (string.IsNullOrEmpty(bestAlias))
            return text;

        var result = text;
        foreach (var col in ambiguousColumns)
        {
            if (sqlKeywords.Contains(col.ToUpperInvariant()))
                continue;
            result = Regex.Replace(result, $@"(?<![\]\w.])\[{Regex.Escape(col)}\](?![.\w])", $"{bestAlias}.[{col}]", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, $@"(?<!\.)\b{Regex.Escape(col)}\b", $"{bestAlias}.{col}", RegexOptions.IgnoreCase);
        }
        return result;
    }

    private static string FindBestAliasForColumn(Dictionary<string, string> aliasToTable, HashSet<string> usedTables)
    {
        foreach (var kvp in aliasToTable)
        {
            var alias = kvp.Key;
            var tableName = kvp.Value;

            if (!usedTables.Contains(tableName))
                continue;

            var tableMatch = Regex.Match(tableName, @"\.([^.]+)$");
            var shortTableName = tableMatch.Success ? tableMatch.Groups[1].Value : tableName;

            if (alias.Equals(shortTableName, StringComparison.OrdinalIgnoreCase) ||
                alias.Equals(tableName.Replace(".", "_"), StringComparison.OrdinalIgnoreCase))
            {
                return alias;
            }
        }

        return aliasToTable.Count > 0 ? aliasToTable.Keys.First() : string.Empty;
    }

    private Dictionary<string, string> ExtractTableAliases(string sql)
    {
        var aliasToTable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        const string fromPattern = @"FROM\s+(\[?[a-zA-Z0-9_]+\]?(?:\]?\.\[?[a-zA-Z0-9_]+\]?)?|""[^""]+""\.""[^""]+""|`[^`]+`\.`[^`]+`)(?:\s+(?:AS\s+)?([a-zA-Z0-9_]+))?(?:\s|$|JOIN)";
        var fromMatch = Regex.Match(sql, fromPattern, RegexOptions.IgnoreCase);
        if (fromMatch.Success)
        {
            var tableName = NormalizeTableName(fromMatch.Groups[1].Value);
            var alias = fromMatch.Groups[2].Value;
            if (string.IsNullOrWhiteSpace(alias))
            {
                var tableMatch = Regex.Match(tableName, @"\.([^.]+)$");
                alias = tableMatch.Success ? tableMatch.Groups[1].Value : tableName;
            }
            if (!string.IsNullOrWhiteSpace(alias) && !IsSqlKeyword(alias))
            {
                aliasToTable[alias] = tableName;
            }
        }

        const string joinPattern = @"(?:INNER|LEFT|RIGHT|FULL|CROSS)?\s*JOIN\s+(\[?[a-zA-Z0-9_]+\]?(?:\]?\.\[?[a-zA-Z0-9_]+\]?)?|""[^""]+""\.""[^""]+""|`[^`]+`\.`[^`]+`)(?:\s+(?:AS\s+)?([a-zA-Z0-9_]+))?(?:\s|$|ON)";
        var joinMatches = Regex.Matches(sql, joinPattern, RegexOptions.IgnoreCase);
        foreach (Match match in joinMatches)
        {
            var tableName = NormalizeTableName(match.Groups[1].Value);
            var alias = match.Groups[2].Value;
            if (string.IsNullOrWhiteSpace(alias))
            {
                var tableMatch = Regex.Match(tableName, @"\.([^.]+)$");
                alias = tableMatch.Success ? tableMatch.Groups[1].Value : tableName;
            }
            if (!string.IsNullOrWhiteSpace(alias) && !IsSqlKeyword(alias))
            {
                aliasToTable[alias] = tableName;
            }
        }

        return aliasToTable;
    }

    private static Dictionary<string, string> ExtractTableAliasesFallback(string sql)
    {
        var aliasToTable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var withExplicitAlias = new Regex(
            @"(?:FROM|JOIN)\s+([a-zA-Z0-9_\[\]""`\.]+)\s+(?:AS\s+)?([a-zA-Z0-9_]+)(?=\s*(?:ON|WHERE|GROUP|ORDER|LEFT|INNER|RIGHT|FULL|CROSS|JOIN|,|\)|$))",
            RegexOptions.IgnoreCase);
        foreach (Match m in withExplicitAlias.Matches(sql))
        {
            if (!m.Success || m.Groups.Count < 3)
                continue;
            var tableRef = m.Groups[1].Value.Trim();
            var explicitAlias = m.Groups[2].Value.Trim();
            if (string.IsNullOrWhiteSpace(tableRef) || string.IsNullOrWhiteSpace(explicitAlias) || IsSqlKeyword(explicitAlias))
                continue;
            var tableName = NormalizeTableName(tableRef);
            if (!aliasToTable.ContainsKey(explicitAlias))
                aliasToTable[explicitAlias] = tableName;
        }
        var tableOnlyPattern = @"(?:FROM|JOIN)\s+([a-zA-Z0-9_\[\]""`\.]+)(?=\s+(?:(?:AS\s+)?[a-zA-Z0-9_]+|ON|WHERE|GROUP|ORDER|LEFT|INNER|RIGHT|FULL|CROSS|$))";
        var tableMatches = Regex.Matches(sql, tableOnlyPattern, RegexOptions.IgnoreCase);
        var aliasCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in tableMatches)
        {
            if (!m.Success || m.Groups.Count < 2)
                continue;
            var tableRef = m.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(tableRef) || tableRef.Length < 2)
                continue;
            var tableName = NormalizeTableName(tableRef);
            var tableMatch = Regex.Match(tableName, @"\.([^.]+)$");
            var baseAlias = tableMatch.Success ? tableMatch.Groups[1].Value : tableName;
            if (string.IsNullOrWhiteSpace(baseAlias) || IsSqlKeyword(baseAlias))
                continue;
            if (aliasToTable.ContainsKey(baseAlias))
                continue;
            var alias = baseAlias;
            if (aliasCount.TryGetValue(baseAlias, out var count))
            {
                count++;
                aliasCount[baseAlias] = count;
                alias = $"{baseAlias}_{count}";
            }
            else
            {
                aliasCount[baseAlias] = 1;
            }
            if (!aliasToTable.ContainsKey(alias))
                aliasToTable[alias] = tableName;
        }
        return aliasToTable;
    }

    private static string NormalizeTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return tableName;

        return tableName
            .Replace("[", "")
            .Replace("]", "")
            .Replace("\"", "")
            .Replace("`", "")
            .Trim();
    }

    private static bool IsSqlKeyword(string word)
    {
        var keywords = new[] { "SELECT", "FROM", "WHERE", "JOIN", "ON", "AND", "OR", "GROUP", "BY", "ORDER", "LIMIT", "TOP", "AS", "LEFT", "RIGHT", "INNER", "OUTER", "CROSS", "HAVING" };
        return keywords.Contains(word.ToUpperInvariant());
    }

    private static HashSet<string> ExtractAmbiguousColumnsFromOnClause(string sql)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var onMatch = Regex.Match(sql, @"\bON\s+(.+?)(?=\b(?:WHERE|GROUP|ORDER|LIMIT|$))", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!onMatch.Success)
            return result;

        var onClause = onMatch.Groups[1].Value;
        var qualColPattern = @"(?:[\w\[\]""`\.]+\.)+([\w\[\]""`]+)";
        var matches = Regex.Matches(onClause, qualColPattern);
        var columnNames = new List<string>();
        foreach (Match m in matches)
        {
            if (m.Groups.Count >= 2 && !string.IsNullOrWhiteSpace(m.Groups[1].Value))
                columnNames.Add(m.Groups[1].Value.Trim('[', ']', '"', '`'));
        }

        for (var i = 0; i < columnNames.Count; i++)
        {
            for (var j = i + 1; j < columnNames.Count; j++)
            {
                if (string.Equals(columnNames[i], columnNames[j], StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(columnNames[i]);
                    break;
                }
            }
        }

        return result;
    }

    private static string FixMySQLCommonColumnMistakes(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql) || schema?.Tables == null)
            return sql;

        var hasQuantityColumn = schema.Tables.SelectMany(t => t.Columns)
            .Any(c => c.ColumnName.Equals("Quantity", StringComparison.OrdinalIgnoreCase));
        var hasStockQuantityColumn = schema.Tables.SelectMany(t => t.Columns)
            .Any(c => c.ColumnName.Equals("StockQuantity", StringComparison.OrdinalIgnoreCase));
        var hasStockLevelColumn = schema.Tables.SelectMany(t => t.Columns)
            .Any(c => c.ColumnName.Equals("StockLevel", StringComparison.OrdinalIgnoreCase));
        if (hasQuantityColumn && !hasStockQuantityColumn &&
            sql.Contains("StockQuantity", StringComparison.OrdinalIgnoreCase))
        {
            sql = Regex.Replace(sql, @"\bStockQuantity\b", "Quantity", RegexOptions.IgnoreCase);
        }
        if (hasQuantityColumn && !hasStockLevelColumn &&
            sql.Contains("StockLevel", StringComparison.OrdinalIgnoreCase))
        {
            sql = Regex.Replace(sql, @"\bStockLevel\b", "Quantity", RegexOptions.IgnoreCase);
        }

        var aliasToTable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var tableAliasRegex = new Regex(@"(?:FROM|JOIN)\s+(?:`([^`]+)`|""([^""]+)""|([a-zA-Z0-9_]+))(?:\s+(?:AS\s+)?([a-zA-Z0-9_]+))?(?=\s|$|ON|WHERE|GROUP|ORDER|JOIN|,|\))", RegexOptions.IgnoreCase);
        foreach (Match m in tableAliasRegex.Matches(sql))
        {
            var tableName = !string.IsNullOrEmpty(m.Groups[1].Value) ? m.Groups[1].Value
                : !string.IsNullOrEmpty(m.Groups[2].Value) ? m.Groups[2].Value
                : m.Groups[3].Value;
            var alias = m.Groups[4].Value;
            if (string.IsNullOrEmpty(alias))
                alias = tableName;
            var table = schema.Tables.FirstOrDefault(t =>
                t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                t.TableName.EndsWith($".{tableName}", StringComparison.OrdinalIgnoreCase));
            if (table != null && !aliasToTable.ContainsKey(alias))
                aliasToTable[alias] = table.TableName.Contains('.') ? table.TableName : tableName;
        }

        foreach (var kvp in aliasToTable)
        {
            var alias = kvp.Key;
            var tableName = kvp.Value;
            var table = schema.Tables.FirstOrDefault(t =>
                t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                t.TableName.EndsWith($".{tableName}", StringComparison.OrdinalIgnoreCase));
            if (table == null)
                continue;
            foreach (var col in table.Columns)
            {
                var longName = table.TableName.Contains('.') ? table.TableName : tableName;
                var pattern = $@"(?<![.`\w]){Regex.Escape(longName)}\.{Regex.Escape(col.ColumnName)}\b";
                var replacement = $"`{alias}`.`{col.ColumnName}`";
                sql = Regex.Replace(sql, pattern, replacement, RegexOptions.IgnoreCase);
            }
        }

        return sql;
    }

    private static string FixAliasTableColumnToAliasColumn(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql) || schema?.Tables == null)
            return sql;
        var aliasToTable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var tableAliasRegex = new Regex(@"(?:FROM|JOIN)\s+(?:`([^`]+)`|""([^""]+)""|([a-zA-Z0-9_]+))(?:\s+(?:AS\s+)?([a-zA-Z0-9_]+))?(?=\s|$|ON|WHERE|GROUP|ORDER|JOIN|,|\))", RegexOptions.IgnoreCase);
        foreach (Match m in tableAliasRegex.Matches(sql))
        {
            var tableName = (!string.IsNullOrEmpty(m.Groups[1].Value) ? m.Groups[1].Value : !string.IsNullOrEmpty(m.Groups[2].Value) ? m.Groups[2].Value : m.Groups[3].Value).Replace("`", "");
            var alias = m.Groups[4].Value;
            if (string.IsNullOrEmpty(alias)) alias = tableName.Split('.').Last().Split('_').Last();
            if (!string.IsNullOrEmpty(alias) && !aliasToTable.ContainsKey(alias))
                aliasToTable[alias] = tableName.Replace("`", "");
        }
        foreach (var kvp in aliasToTable)
        {
            var alias = kvp.Key;
            var tableName = kvp.Value;
            var tableNorm = tableName.Replace(".", "_");
            var pattern = $@"(?<![.\w]){Regex.Escape(alias)}\.{Regex.Escape(tableName)}\.([a-zA-Z_][a-zA-Z0-9_]*)(?![.\w])";
            var replacement = $"{alias}.$1";
            sql = Regex.Replace(sql, pattern, replacement, RegexOptions.IgnoreCase);
            pattern = $@"(?<![.\w]){Regex.Escape(alias)}\.{Regex.Escape(tableNorm)}\.([a-zA-Z_][a-zA-Z0-9_]*)(?![.\w])";
            sql = Regex.Replace(sql, pattern, replacement, RegexOptions.IgnoreCase);
        }
        return sql;
    }

    private string FixUnboundAliasReferences(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql) || schema?.Tables == null)
            return sql;

        var aliasToTable = ExtractTableAliases(sql);
        if (aliasToTable.Count == 0)
            aliasToTable = ExtractTableAliasesFallback(sql);
        MergeDerivedTableAliases(sql, aliasToTable);

        var qualColPattern = new Regex(@"\b([A-Za-z_][A-Za-z0-9_]*)\s*\.\s*([A-Za-z_][A-Za-z0-9_\[\]""]*)\b", RegexOptions.IgnoreCase);
        var result = qualColPattern.Replace(sql, m =>
        {
            var alias = m.Groups[1].Value;
            var column = m.Groups[2].Value.Trim('[', ']', '"');
            if (IsSqlKeyword(alias))
                return m.Value;

            string? correctAlias = null;
            if (aliasToTable.TryGetValue(alias, out var aliasTable))
            {
                var tbl = schema.Tables.FirstOrDefault(t =>
                    t.TableName.Equals(aliasTable, StringComparison.OrdinalIgnoreCase) ||
                    t.TableName.EndsWith($".{aliasTable}", StringComparison.OrdinalIgnoreCase));
                if (tbl != null && tbl.Columns.Any(c => c.ColumnName.Equals(column, StringComparison.OrdinalIgnoreCase)))
                    return m.Value;
            }

            foreach (var kvp in aliasToTable)
            {
                var tbl = schema.Tables.FirstOrDefault(t =>
                    t.TableName.Equals(kvp.Value, StringComparison.OrdinalIgnoreCase) ||
                    t.TableName.EndsWith($".{kvp.Value}", StringComparison.OrdinalIgnoreCase));
                if (tbl != null && tbl.Columns.Any(c => c.ColumnName.Equals(column, StringComparison.OrdinalIgnoreCase)))
                {
                    correctAlias = kvp.Key;
                    break;
                }
            }

            return string.IsNullOrEmpty(correctAlias) ? m.Value : $"{correctAlias}.{m.Groups[2].Value}";
        });

        return result;
    }

    private static void MergeDerivedTableAliases(string sql, Dictionary<string, string> aliasToTable)
    {
        var derivedPattern = new Regex(@"\)\s+(?:AS\s+)?([A-Za-z_][A-Za-z0-9_]*)\s*(?=\s|$|JOIN|ON|WHERE|GROUP|ORDER)", RegexOptions.IgnoreCase);
        foreach (Match m in derivedPattern.Matches(sql))
        {
            var alias = m.Groups[1].Value;
            if (string.IsNullOrEmpty(alias) || IsSqlKeyword(alias) || aliasToTable.ContainsKey(alias))
                continue;
            var subqueryText = ExtractSubqueryBeforeClosingParen(sql, m.Index);
            if (string.IsNullOrEmpty(subqueryText))
                continue;
            var fromMatch = Regex.Match(subqueryText, @"(?:FROM|JOIN)\s+(\[?[a-zA-Z0-9_]+\]?(?:\]?\.\[?[a-zA-Z0-9_]+\]?)?|""[^""]+""\.""[^""]+""|`[^`]+`\.`[^`]+`)", RegexOptions.IgnoreCase);
            if (fromMatch.Success)
            {
                var innerTable = NormalizeTableName(fromMatch.Groups[1].Value.Trim());
                if (!string.IsNullOrEmpty(innerTable))
                    aliasToTable[alias] = innerTable;
            }
        }
    }

    private static string? ExtractSubqueryBeforeClosingParen(string sql, int closingParenIndex)
    {
        if (closingParenIndex <= 0 || closingParenIndex >= sql.Length || sql[closingParenIndex] != ')')
            return null;
        var depth = 1;
        for (var i = closingParenIndex - 1; i >= 0; i--)
        {
            var ch = sql[i];
            if (ch == ')')
                depth++;
            else if (ch == '(')
            {
                depth--;
                if (depth == 0)
                    return sql.Substring(i + 1, closingParenIndex - i - 1);
            }
        }
        return null;
    }

    private string ExpandShortTableRefsToFullNames(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql) || !Regex.IsMatch(sql, @"\bJOIN\b", RegexOptions.IgnoreCase))
            return sql;

        var aliasToTable = ExtractTableAliases(sql);
        var result = sql;
        foreach (var kvp in aliasToTable)
        {
            var alias = kvp.Key;
            var fullTableName = kvp.Value;
            if (!fullTableName.Contains('.') || string.IsNullOrEmpty(alias))
                continue;

            var pattern = $@"(?<![.\w])\b{Regex.Escape(alias)}\.([a-zA-Z_][a-zA-Z0-9_\[\]""]*)\b";
            var replacement = fullTableName + ".${1}";
            result = Regex.Replace(result, pattern, replacement, RegexOptions.IgnoreCase);
        }
        return result;
    }

}

