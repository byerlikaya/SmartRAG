using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SmartRAG.Services.Database;


/// <summary>
/// Executes queries across multiple databases
/// </summary>
public class DatabaseQueryExecutor : IDatabaseQueryExecutor
{
    private const int DefaultMaxRows = 100;
    private const string DebugLogFilePath = "/Users/barisyerlikaya/Projects/SmartRAG/auto-database-debug.log";

    private readonly IDatabaseConnectionManager _connectionManager;
    private readonly IDatabaseParserService _databaseParser;
    private readonly ILogger<DatabaseQueryExecutor> _logger;

    public DatabaseQueryExecutor(
        IDatabaseConnectionManager connectionManager,
        IDatabaseParserService databaseParser,
        ILogger<DatabaseQueryExecutor> logger)
    {
        _connectionManager = connectionManager;
        _databaseParser = databaseParser;
        _logger = logger;
    }

    /// <summary>
    /// [DB Query] Executes queries across multiple databases based on query intent
    /// </summary>
    public async Task<MultiDatabaseQueryResult> ExecuteMultiDatabaseQueryAsync(QueryIntent queryIntent, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MultiDatabaseQueryResult
        {
            Success = true
        };

        var maxResultsOverride = queryIntent.MaxResults > 0 ? queryIntent.MaxResults : (int?)null;

        var hasMappingDependency = await HasMappingDependencyAsync(queryIntent, cancellationToken);
        var allMappings = await GetAllMappingsAsync(cancellationToken);
        DatabaseLogMessages.LogTwoPhaseMappingCheck(_logger, hasMappingDependency, allMappings.Count, null!);

        if (hasMappingDependency && queryIntent.DatabaseQueries.Count >= 2)
        {
            var ordered = queryIntent.DatabaseQueries.OrderBy(q => q.Priority).ToList();
            var sourceValues = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < ordered.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dbQuery = ordered[i];
                var queryToExecute = dbQuery.GeneratedQuery;

                DatabaseLogMessages.LogTwoPhaseLoopStart(_logger, i, dbQuery.DatabaseName, null!);
                var willInject = i > 0 && sourceValues.Count > 0;
                DatabaseLogMessages.LogTwoPhaseInjectDecision(_logger, willInject, sourceValues.Count, null!);

                if (willInject)
                {
                    queryToExecute = InjectSourceValuesIntoTargetQuery(dbQuery, sourceValues, queryToExecute, allMappings);
                }

                var dbResult = await ExecuteSingleDatabaseQueryAsync(dbQuery, maxResultsOverride, cancellationToken, queryToExecute);
                result.DatabaseResults[dbQuery.DatabaseId] = dbResult;
                DatabaseLogMessages.LogTwoPhaseAfterExecution(_logger, dbQuery.DatabaseName, dbResult.Success, dbResult.RowCount, null!);

                if (!dbResult.Success)
                {
                    result.Success = false;
                    result.Errors.Add($"Database {dbQuery.DatabaseId}: {dbResult.ErrorMessage}");
                    break;
                }

                ExtractMappingColumnValues(_logger, dbResult.ResultData, dbQuery.DatabaseName, sourceValues, allMappings);
                DatabaseLogMessages.LogTwoPhaseAfterExtract(_logger, dbQuery.DatabaseName, sourceValues.Count, null!);
            }
        }
        else
        {
            var tasks = queryIntent.DatabaseQueries.Select(async dbQuery =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dbResult = await ExecuteSingleDatabaseQueryAsync(dbQuery, maxResultsOverride, cancellationToken, null);
                return (dbQuery.DatabaseId, dbResult);
            });

            var results = await Task.WhenAll(tasks);

            foreach (var (databaseId, dbResult) in results)
            {
                result.DatabaseResults[databaseId] = dbResult;

                if (!dbResult.Success)
                {
                    result.Success = false;
                    result.Errors.Add($"Database {databaseId}: {dbResult.ErrorMessage}");
                }
            }
        }

        stopwatch.Stop();
        result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

        return result;
    }

    private async Task<DatabaseQueryResult> ExecuteSingleDatabaseQueryAsync(DatabaseQueryIntent dbQuery, int? maxResultsOverride, CancellationToken cancellationToken = default, string? queryOverride = null)
    {
        var queryToExecute = queryOverride ?? dbQuery.GeneratedQuery;
        var stopwatch = Stopwatch.StartNew();
        var result = new DatabaseQueryResult
        {
            DatabaseId = dbQuery.DatabaseId,
            DatabaseName = dbQuery.DatabaseName,
            ExecutedQuery = queryToExecute
        };

        try
        {
            var connection = await _connectionManager.GetConnectionAsync(dbQuery.DatabaseId, cancellationToken);
            if (connection == null)
            {
                result.Success = false;
                result.ErrorMessage = "Database connection not found";
                return result;
            }

            if (string.IsNullOrEmpty(queryToExecute))
            {
                result.Success = false;
                result.ErrorMessage = "No query generated";
                return result;
            }

            DatabaseLogMessages.LogExecutingSqlForDatabase(_logger, dbQuery.DatabaseName, null!);
            DatabaseLogMessages.LogExecutingSqlWithQuery(_logger, dbQuery.DatabaseName, queryToExecute, null!);

            var configMaxRows = connection.MaxRowsPerQuery > 0 ? connection.MaxRowsPerQuery : DefaultMaxRows;
            var maxRows = maxResultsOverride.HasValue ? Math.Min(maxResultsOverride.Value, configMaxRows) : configMaxRows;
            var queryResult = await _databaseParser.ExecuteQueryAsync(
                connection.ConnectionString,
                queryToExecute,
                connection.DatabaseType,
                maxRows,
                cancellationToken);

            result.ResultData = queryResult;
            result.RowCount = CountRowsInResult(queryResult);
            result.Success = true;
        }
        catch (Exception ex)
        {
            DatabaseLogMessages.LogQueryExecutionFailed(_logger, dbQuery.DatabaseName, ex);
            AppendDebugLog(dbQuery.DatabaseName, queryToExecute ?? string.Empty, "Error executing SQL query.", ex);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    private static void AppendDebugLog(string databaseName, string sql, string message, Exception? ex = null)
    {
        if (string.IsNullOrWhiteSpace(sql) && string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            var builder = new StringBuilder();
            builder.AppendLine($"[{DateTime.UtcNow:O}] Database={databaseName}");

            if (!string.IsNullOrWhiteSpace(message))
            {
                builder.AppendLine(message);
            }

            if (!string.IsNullOrWhiteSpace(sql))
            {
                builder.AppendLine("SQL:");
                builder.AppendLine(sql);
            }

            if (ex != null)
            {
                builder.AppendLine("Error:");
                builder.AppendLine(ex.Message);
            }

            builder.AppendLine(new string('-', 80));
            File.AppendAllText(DebugLogFilePath, builder.ToString());
        }
        catch
        {
            // Debug logging must never affect query execution
        }
    }

    private async Task<bool> HasMappingDependencyAsync(QueryIntent queryIntent, CancellationToken cancellationToken)
    {
        var connections = await _connectionManager.GetAllConnectionsAsync(cancellationToken);
        var dbNames = queryIntent.DatabaseQueries.Select(q => q.DatabaseName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return connections.Any(c => c.CrossDatabaseMappings?.Any(m =>
            dbNames.Contains(m.SourceDatabase) &&
            !string.IsNullOrEmpty(m.TargetDatabase) &&
            dbNames.Contains(m.TargetDatabase)) ?? false);
    }

    private async Task<List<CrossDatabaseMapping>> GetAllMappingsAsync(CancellationToken cancellationToken)
    {
        var connections = await _connectionManager.GetAllConnectionsAsync(cancellationToken);
        return connections.SelectMany(c => c.CrossDatabaseMappings ?? new List<CrossDatabaseMapping>()).ToList();
    }

    private static void ExtractMappingColumnValues(ILogger logger, string resultData, string databaseName,
        Dictionary<string, HashSet<string>> sourceValues,
        List<CrossDatabaseMapping> mappings)
    {
        DatabaseLogMessages.LogExtractMappingEntry(logger, databaseName, resultData?.Length ?? 0, mappings.Count, null!);

        if (string.IsNullOrWhiteSpace(resultData))
            return;

        var lines = resultData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string[]? headers = null;
        var headerIndex = -1;

        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("===") || lines[i].StartsWith("Query:") || lines[i].StartsWith("Rows"))
                continue;
            headers = lines[i].Split('\t');
            headerIndex = i;
            break;
        }

        if (headers == null || headerIndex < 0)
        {
            DatabaseLogMessages.LogExtractMappingNoHeaders(logger, databaseName, null!);
            return;
        }

        DatabaseLogMessages.LogExtractMappingHeaders(logger, string.Join(", ", headers), headerIndex, null!);

        var sourceMappings = mappings.Where(m =>
            m.SourceDatabase.Equals(databaseName, StringComparison.OrdinalIgnoreCase)).ToList();
        var columnsToExtract = sourceMappings
            .Select(m => m.SourceColumn)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        DatabaseLogMessages.LogExtractMappingColumnsToExtract(logger, databaseName, string.Join(", ", columnsToExtract), columnsToExtract.Count, null!);

        foreach (var colName in columnsToExtract)
        {
            var colIndex = Array.FindIndex(headers, h => h.Equals(colName, StringComparison.OrdinalIgnoreCase));
            DatabaseLogMessages.LogExtractMappingColumnLookup(logger, colName, colIndex, colIndex >= 0, null!);
            if (colIndex < 0)
                continue;

            var key = $"{databaseName}.{colName}";
            if (!sourceValues.ContainsKey(key))
                sourceValues[key] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = headerIndex + 1; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("Rows extracted:") || lines[i].StartsWith("==="))
                    break;
                var values = lines[i].Split('\t');
                if (colIndex < values.Length)
                {
                    var val = values[colIndex]?.Trim();
                    if (!string.IsNullOrEmpty(val) && val != "NULL")
                        sourceValues[key].Add(val);
                }
            }

            DatabaseLogMessages.LogExtractMappingValuesExtracted(logger, key, sourceValues[key].Count, null!);
        }
    }

    private string InjectSourceValuesIntoTargetQuery(DatabaseQueryIntent targetQuery,
        Dictionary<string, HashSet<string>> sourceValues,
        string targetSql,
        List<CrossDatabaseMapping> allMappings)
    {
        var targetMappings = allMappings
            .Where(m => m.TargetDatabase.Equals(targetQuery.DatabaseName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        DatabaseLogMessages.LogInjectEntry(_logger, targetQuery.DatabaseName, targetMappings.Count, null!);
        DatabaseLogMessages.LogInjectSourceValuesKeys(_logger, string.Join(", ", sourceValues.Keys), null!);

        if (targetMappings.Count == 0)
            return targetSql;

        foreach (var mapping in targetMappings)
        {
            if (!mapping.TargetDatabase.Equals(targetQuery.DatabaseName, StringComparison.OrdinalIgnoreCase))
                continue;

            var sourceDb = mapping.SourceDatabase;
            var sourceCol = mapping.SourceColumn;
            var targetCol = mapping.TargetColumn;

            var key = $"{sourceDb}.{sourceCol}";
            var found = sourceValues.TryGetValue(key, out var values);
            var valuesCount = values?.Count ?? 0;
            string? altKey = null;
            if (!found || valuesCount == 0)
            {
                altKey = sourceValues.Keys.FirstOrDefault(k => k.StartsWith(sourceDb, StringComparison.OrdinalIgnoreCase));
                if (altKey != null)
                {
                    values = sourceValues[altKey];
                    valuesCount = values?.Count ?? 0;
                }
                else
                    continue;
            }

            DatabaseLogMessages.LogInjectKeyLookup(_logger, key, found, altKey ?? "(none)", valuesCount, null!);

            if (values == null || values.Count == 0)
            {
                var colPattern = $@"([""\[\]?\s]){Regex.Escape(targetCol)}([""\[\]?\s]*\s+IN\s*\(\s*)[^)]*(\s*\))";
                var match = Regex.Match(targetSql, colPattern, RegexOptions.IgnoreCase);
                DatabaseLogMessages.LogInjectRegexMatch(_logger, match.Success, null!);
                if (match.Success)
                {
                    var prefix = match.Groups[1].Value.Equals("\"", StringComparison.Ordinal) ? " " : match.Groups[1].Value;
                    targetSql = targetSql[..match.Index] + prefix + "1=0" + targetSql[(match.Index + match.Length)..];
                    DatabaseLogMessages.LogInjectedSourceValuesIntoTargetQuery(_logger,
                        mapping.SourceDatabase, mapping.SourceColumn, mapping.TargetDatabase, 0, null!);
                    break;
                }
                DatabaseLogMessages.LogInjectNoMatchSkipped(_logger, null!);
                continue;
            }

            var valueList = string.Join(", ", values);
            var colPatternWithValues = $@"([""\[\]?\s]){Regex.Escape(targetCol)}([""\[\]?\s]*\s+IN\s*\(\s*)[^)]*(\s*\))";
            var matchWithValues = Regex.Match(targetSql, colPatternWithValues, RegexOptions.IgnoreCase);
            DatabaseLogMessages.LogInjectRegexMatch(_logger, matchWithValues.Success, null!);
            if (matchWithValues.Success)
            {
                targetSql = targetSql[..matchWithValues.Index] + matchWithValues.Groups[1].Value + targetCol + matchWithValues.Groups[2].Value + valueList + matchWithValues.Groups[3].Value + targetSql[(matchWithValues.Index + matchWithValues.Length)..];
                DatabaseLogMessages.LogInjectedSourceValuesIntoTargetQuery(_logger,
                    mapping.SourceDatabase, mapping.SourceColumn, mapping.TargetDatabase, values.Count, null!);
                break;
            }
            DatabaseLogMessages.LogInjectNoMatchSkipped(_logger, null!);
        }

        return targetSql;
    }

    private int CountRowsInResult(string resultData)
    {
        if (string.IsNullOrEmpty(resultData))
        {
            return 0;
        }

        var lines = resultData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (!line.StartsWith("Rows extracted:"))
                continue;
            if (int.TryParse(line["Rows extracted:".Length..].Trim(), out var count))
            {
                return count;
            }
        }

        var dataRows = 0;
        var headerFound = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("===") || line.StartsWith("Query:") || line.StartsWith("Rows"))
                continue;

            if (!headerFound)
            {
                headerFound = true;
                continue;
            }

            dataRows++;
        }

        return dataRows;
    }
}


