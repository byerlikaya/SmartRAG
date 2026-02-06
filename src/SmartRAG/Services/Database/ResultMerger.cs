
namespace SmartRAG.Services.Database;


/// <summary>
/// Merges results from multiple databases into coherent responses
/// </summary>
public class ResultMerger : IResultMerger
{
    private static readonly string[] DescriptiveColumnPatterns = { "Name", "Title", "Description", "City", "Address", "Location", "Text", "Label" };
    private static readonly string[] NonDescriptivePatterns = { "ID", "Key", "Code", "Number", "Num", "Count", "Sum", "Total", "Amount", "Value" };

    private readonly IAIService _aiService;
    private readonly IDatabaseConnectionManager _connectionManager;
    private readonly IDatabaseParserService _databaseParser;
    private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer;
    private readonly ILogger<ResultMerger> _logger;

    public ResultMerger(
        IAIService aiService,
        IDatabaseConnectionManager connectionManager,
        IDatabaseParserService databaseParser,
        IDatabaseSchemaAnalyzer schemaAnalyzer,
        ILogger<ResultMerger> logger)
    {
        _aiService = aiService;
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _databaseParser = databaseParser ?? throw new ArgumentNullException(nameof(databaseParser));
        _schemaAnalyzer = schemaAnalyzer ?? throw new ArgumentNullException(nameof(schemaAnalyzer));
        _logger = logger;
    }

    /// <summary>
    /// Merges results from multiple databases into a coherent response
    /// </summary>
    public async Task<string> MergeResultsAsync(MultiDatabaseQueryResult queryResults, string originalQuery)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Query: {originalQuery}");
        sb.AppendLine();

        var parsedResults = new Dictionary<string, ParsedQueryResult>();

        foreach (var kvp in queryResults.DatabaseResults)
        {
            var dbResult = kvp.Value;

            if (!dbResult.Success || string.IsNullOrWhiteSpace(dbResult.ResultData))
            {
                sb.AppendLine($"=== {dbResult.DatabaseName} ===");
                sb.AppendLine($"Error: {dbResult.ErrorMessage}");
                sb.AppendLine();
                continue;
            }

            var parsedData = ParseQueryResult(dbResult.ResultData);
            if (parsedData.Rows.Count <= 0)
                continue;
            parsedResults[dbResult.DatabaseId] = parsedData;
            parsedData.DatabaseName = dbResult.DatabaseName;
            parsedData.DatabaseId = dbResult.DatabaseId;
        }

        if (parsedResults.Count > 1)
        {
            var mergedData = await SmartMergeResultsAsync(parsedResults);
            if (mergedData.Rows.Count > 0)
            {
                sb.AppendLine("=== SMART MERGED RESULTS (Cross-Database JOIN) ===");
                sb.AppendLine(FormatParsedResult(mergedData));
                sb.AppendLine();
            }
            else
            {
                _logger.LogWarning("Smart merge failed, falling back to separate results");
                AppendSeparateResultsWithJoinHints(sb, parsedResults);
            }
        }
        else
        {
            AppendSeparateResults(sb, parsedResults);
        }

        return sb.ToString();
    }

    /// <summary>
    /// [AI Query] Generates final AI answer from merged database results
    /// </summary>
    public async Task<RagResponse> GenerateFinalAnswerAsync(
        string userQuery,
        string mergedData,
        MultiDatabaseQueryResult queryResults,
        string? preferredLanguage = null)
    {
        try
        {
            var promptBuilder = new StringBuilder();

            // Add language instruction if preferred language is specified
            if (!string.IsNullOrWhiteSpace(preferredLanguage))
            {
                promptBuilder.AppendLine($"IMPORTANT: Respond in {preferredLanguage.ToUpperInvariant()} language.");
                promptBuilder.AppendLine();
            }

            promptBuilder.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
            promptBuilder.AppendLine("║  🚨🚨🚨 CRITICAL - READ THIS FIRST! 🚨🚨🚨                         ║");
            promptBuilder.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("⛔⛔⛔ ABSOLUTELY FORBIDDEN - NEVER DO THESE:");
            promptBuilder.AppendLine("  ✗✗✗ NEVER invent names, numbers, or example data!");
            promptBuilder.AppendLine("  ✗✗✗ NEVER provide sample data, hypothetical values, or fictional examples!");
            promptBuilder.AppendLine("  ✗✗✗ NEVER create fictional examples like 'EntityName - NumericValue' or 'DescriptiveColumn - AggregationValue'!");
            promptBuilder.AppendLine("  ✗✗✗ NEVER provide suggestions or instructions to the user!");
            promptBuilder.AppendLine("  ✗✗✗ NEVER write 'database connection failed', 'no data available', 'missing data'");
            promptBuilder.AppendLine("  ✗✗✗ NEVER provide SQL examples, code snippets, or query explanations");
            promptBuilder.AppendLine("  ✗✗✗ NEVER suggest SQL queries to the user");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("🚨🚨🚨 IF NO DATA AVAILABLE:");
            promptBuilder.AppendLine("  → Say ONLY: 'I could not find the answer to your question'");
            promptBuilder.AppendLine("  → DO NOT invent any data!");
            promptBuilder.AppendLine("  → DO NOT provide examples!");
            promptBuilder.AppendLine("  → DO NOT suggest anything!");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("✓ REQUIRED:");
            promptBuilder.AppendLine("  ✓ If you see '📊 Total rows: X' where X > 0 → Data EXISTS, use it!");
            promptBuilder.AppendLine("  ✓ Use EXACT values from database results - copy character by character");
            promptBuilder.AppendLine("  ✓ If descriptive data missing → Say 'PrimaryKeyColumn: X (descriptive data not available in results)'");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════");
            promptBuilder.AppendLine("HOW TO READ DATABASE RESULTS:");
            promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════");
            promptBuilder.AppendLine("Format: '📊 Total rows: X | Columns: Col1, Col2, Col3'");
            promptBuilder.AppendLine("Then: Column names (tab-separated)");
            promptBuilder.AppendLine("Then: Data rows (tab-separated values matching columns)");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("ANSWER STRATEGY:");
            promptBuilder.AppendLine("  1. Check: Do you see ANY rows? (📊 Total rows: X where X > 0)");
            promptBuilder.AppendLine("     → If YES: Data EXISTS, answer using this data!");
            promptBuilder.AppendLine("     → If NO (Total rows: 0) OR you see Error messages:");
            promptBuilder.AppendLine("       → Say ONLY: 'I could not find the answer to your question'");
            promptBuilder.AppendLine("       → DO NOT provide example data, sample names, or hypothetical values");
                promptBuilder.AppendLine("       → DO NOT create fictional examples like 'DescriptiveColumn - AggregationAlias'");
            promptBuilder.AppendLine("       → Simply state that you could not find the requested information");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("  2. Question type:");
            promptBuilder.AppendLine("     → COUNT/Number: Return count value or row count");
            promptBuilder.AppendLine("     → TOTAL/SUM: Return aggregated numeric value from results");
            promptBuilder.AppendLine("     → LIST: Return all rows (format as list)");
            promptBuilder.AppendLine("     → TOP N: Sort by numeric column (descending), take first N");
            promptBuilder.AppendLine("     → GROUPING queries (e.g., 'which grouping has most'): Show ALL groupings from results, not just one!");
            promptBuilder.AppendLine("       → If query asks about grouping level and results contain multiple groupings, show ALL of them");
            promptBuilder.AppendLine("       → Format: List each grouping with its count/amount");
            promptBuilder.AppendLine("       → Example: If results show 4 groupings, show all 4, not just the top one");
            promptBuilder.AppendLine("       → Order by count/amount DESC (highest first) but show ALL results");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("  3. Multiple databases:");
            promptBuilder.AppendLine("     → Find ID columns (ending with 'ID' or 'Id') in ALL results");
            promptBuilder.AppendLine("     → Match rows by ID values (numeric comparison, then string)");
            promptBuilder.AppendLine("     → IMPORTANT: Even if column names are different (e.g., ColumnA vs ColumnB),");
            promptBuilder.AppendLine("       if the VALUES match (e.g., ValueX = ValueX), combine those rows!");
            promptBuilder.AppendLine("     → Use semantic understanding: Different ID column names may represent the same entity");
            promptBuilder.AppendLine("     → Combine matched rows (ID + all columns)");
            promptBuilder.AppendLine("     → If no match: Use each database separately");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"User Question: {userQuery}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════");
            promptBuilder.AppendLine("📊 DATABASE RESULTS - USE ONLY THESE VALUES:");
            promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(mergedData);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════");
            promptBuilder.AppendLine("🚨🚨🚨 NOW GENERATE YOUR ANSWER:");
            promptBuilder.AppendLine("  ✓ Use ONLY data from results above");
            promptBuilder.AppendLine("  ✓ Use EXACT values (no modifications)");
            promptBuilder.AppendLine("  ✓ If query asks about grouping level and results contain multiple groupings, show ALL of them");
            promptBuilder.AppendLine("  ✓ If descriptive data missing, say 'PrimaryKeyColumn: X (descriptive data not available)'");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("  🚨 GROUPING QUERIES - CRITICAL:");
            promptBuilder.AppendLine("    → If query asks 'which grouping has most' and results show multiple groupings:");
            promptBuilder.AppendLine("      → Show ALL groupings from results (not just the first one)");
            promptBuilder.AppendLine("      → Format: List each grouping with its count/amount");
            promptBuilder.AppendLine("      → Order by count/amount DESC but include ALL results");
            promptBuilder.AppendLine("    → Example: If results show 4 groupings, show all 4 with their values");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("  ✗✗✗ If NO data is available (Total rows: 0 or Error messages):");
            promptBuilder.AppendLine("    → Say ONLY: 'I could not find the answer to your question'");
            promptBuilder.AppendLine("    → DO NOT invent names or descriptive data!");
            promptBuilder.AppendLine("    → DO NOT invent numbers, counts, or aggregation values!");
            promptBuilder.AppendLine("    → DO NOT provide example data, sample names, or hypothetical values!");
            promptBuilder.AppendLine("    → DO NOT create fictional examples!");
            promptBuilder.AppendLine("    → DO NOT write SQL code examples or suggest SQL queries!");
            promptBuilder.AppendLine("    → DO NOT write code blocks with ```sql or ```!");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("🚨 REMEMBER: If you don't see data in results above, you MUST say:");
            promptBuilder.AppendLine("    'I could not find the answer to your question'");
            promptBuilder.AppendLine("    NOTHING ELSE! NO EXAMPLES! NO SUGGESTIONS!");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Your answer (start directly, no preamble):");

            var prompt = promptBuilder.ToString();

            var answer = await _aiService.GenerateResponseAsync(prompt, new List<string>());

            answer = RemoveSQLCodeBlocksFromAnswer(answer);

            var sources = queryResults.DatabaseResults
                .Where(r => r.Value.Success)
                .Select(r => new SearchSource
                {
                    SourceType = "Database",
                    DatabaseId = r.Value.DatabaseId,
                    DatabaseName = r.Value.DatabaseName,
                    FileName = $"{r.Value.DatabaseName} ({r.Value.RowCount} rows)",
                    RelevantContent = r.Value.ResultData,
                    ExecutedQuery = r.Value.ExecutedQuery,
                    Tables = ExtractTableNames(r.Value.ExecutedQuery),
                    Location = BuildDatabaseLocationDescription(r.Value.DatabaseName, r.Value.ExecutedQuery, r.Value.RowCount)
                })
                .ToList();

            return new RagResponse
            {
                Query = userQuery,
                Answer = answer,
                Sources = sources,
                SearchedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating final answer");
            return new RagResponse
            {
                Answer = mergedData,
                Sources = new List<SearchSource>
                {
                    new()
                    {
                        SourceType = "System",
                        FileName = "Raw data (AI generation failed)",
                        RelevantContent = ex.Message,
                        Location = "System notification"
                    }
                },
                Query = userQuery,
                SearchedAt = DateTime.UtcNow
            };
        }
    }

    private ParsedQueryResult? ParseQueryResult(string resultData)
    {
        try
        {
            var lines = resultData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string[] headers = null;
            var headerIndex = -1;

            for (var i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]) ||
                    lines[i].StartsWith("===") ||
                    lines[i].StartsWith("Query:") ||
                    lines[i].StartsWith("Rows"))
                {
                    continue;
                }

                headers = lines[i].Split('\t');
                headerIndex = i;
                break;
            }

            if (headers == null || headerIndex == -1)
            {
                _logger.LogWarning("Could not parse query result - no header found");
                return null;
            }

            var result = new ParsedQueryResult
            {
                Columns = headers.ToList()
            };

            for (var i = headerIndex + 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.StartsWith("Rows extracted:") || line.StartsWith("==="))
                    break;

                var values = line.Split('\t');
                if (values.Length != headers.Length)
                    continue;

                var row = new Dictionary<string, string>();

                for (var j = 0; j < headers.Length; j++)
                {
                    row[headers[j]] = values[j];
                }
                result.Rows.Add(row);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing query result");
            return null;
        }
    }

    private async Task<ParsedQueryResult?> SmartMergeResultsAsync(
        Dictionary<string, ParsedQueryResult> parsedResults)
    {
        try
        {
            ParsedQueryResult mappingBasedRetry;
            if (parsedResults.Count < 2)
            {
                mappingBasedRetry = await TryMergeWithMappingWhenTargetMissingAsync(parsedResults);

                if (mappingBasedRetry!.Rows.Count <= 0)
                    return null;

                _logger.LogInformation("Merge successful using mapping when target database result was missing");
                return mappingBasedRetry;
            }

            var joinableResults = await FindJoinableTablesAsync(parsedResults);

            if (joinableResults is { Count: < 2 })
            {
                _logger.LogWarning("No joinable relationships found between databases");

                mappingBasedRetry = await TryMergeWithMappingWhenTargetMissingAsync(parsedResults);

                if (mappingBasedRetry!.Rows.Count <= 0)
                    return null;

                _logger.LogInformation("Merge successful using mapping when target database result was missing");
                return mappingBasedRetry;

            }

            var merged = PerformInMemoryJoin(joinableResults);
            if (merged.Rows.Count != 0)
                return merged;

            _logger.LogWarning("Smart merge failed: No matching rows found after join attempt");

            var retryMerged = await RetryMergeWithFilteredQueryAsync(joinableResults);

            if (retryMerged.Rows.Count > 0)
            {
                _logger.LogInformation("Retry merge successful with filtered query");
                return retryMerged;
            }

            mappingBasedRetry = await TryMergeWithMappingWhenTargetMissingAsync(parsedResults);

            if (mappingBasedRetry.Rows.Count <= 0)
                return null;

            _logger.LogInformation("Merge successful using mapping when target database result was missing");
            return mappingBasedRetry;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in smart merge");
            return null;
        }
    }

    private async Task<List<(ParsedQueryResult Result, string JoinColumn)>?> FindJoinableTablesAsync(
        Dictionary<string, ParsedQueryResult> parsedResults)
    {
        await Task.CompletedTask;

        var joinable = new List<(ParsedQueryResult Result, string JoinColumn)>();

        var mappingBasedMatches = await FindMappingBasedMatchesAsync(parsedResults);
        if (mappingBasedMatches.Count >= 2)
        {
            _logger.LogInformation("Found mapping-based matches across {Count} databases", mappingBasedMatches.Count);
            return mappingBasedMatches;
        }

        var allJoinCandidates = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in parsedResults)
        {
            var result = kvp.Value;

            var fkColumns = result.Columns.Where(col =>
                col.EndsWith("id", StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var fkCol in fkColumns)
            {
                if (!allJoinCandidates.ContainsKey(fkCol))
                {
                    allJoinCandidates[fkCol] = new List<string>();
                }
                allJoinCandidates[fkCol].Add(result.DatabaseId);
            }
        }

        var commonJoinColumns = allJoinCandidates.Where(kvp => kvp.Value.Count >= 2).ToList();

        if (commonJoinColumns.Count > 0)
        {
            var bestJoinColumn = commonJoinColumns.OrderByDescending(kvp => kvp.Value.Count).First().Key;

            foreach (var kvp in parsedResults)
            {
                var result = kvp.Value;

                if (result.Columns.Any(col => col.Equals(bestJoinColumn, StringComparison.OrdinalIgnoreCase)))
                {
                    joinable.Add((result, bestJoinColumn));
                }
            }

            if (joinable.Count >= 2)
            {
                _logger.LogInformation("Found common join column: {JoinColumn} across {Count} databases", bestJoinColumn, joinable.Count);
                return joinable;
            }
        }

        var valueBasedMatches = FindValueBasedMatches(parsedResults);
        if (valueBasedMatches.Count >= 2)
        {
            _logger.LogInformation("Found value-based matches across {Count} databases", valueBasedMatches.Count);
            return valueBasedMatches;
        }

        _logger.LogWarning("No joinable relationships found between databases");
        return null;
    }

    private async Task<List<(ParsedQueryResult Result, string JoinColumn)>?> FindMappingBasedMatchesAsync(
        Dictionary<string, ParsedQueryResult> parsedResults)
    {
        try
        {
            var connections = await _connectionManager.GetAllConnectionsAsync();
            var databaseNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in parsedResults)
            {
                var result = kvp.Value;
                var connection = connections.FirstOrDefault(c =>
                    c.Name.Equals(result.DatabaseName, StringComparison.OrdinalIgnoreCase));
                if (connection != null)
                {
                    databaseNameMap[result.DatabaseId] = connection.Name;
                }
            }

            var mappings = new List<(string SourceDatabaseId, string SourceColumn, string TargetDatabaseId, string TargetColumn)>();

            foreach (var sourceKvp in parsedResults)
            {
                var sourceResult = sourceKvp.Value;
                if (!databaseNameMap.TryGetValue(sourceResult.DatabaseId, out var sourceDbName))
                    continue;

                var sourceConnection = connections.FirstOrDefault(c =>
                    (c.Name).Equals(sourceDbName, StringComparison.OrdinalIgnoreCase));
                if (sourceConnection?.CrossDatabaseMappings == null)
                    continue;

                foreach (var mapping in sourceConnection.CrossDatabaseMappings)
                {
                    if (!mapping.SourceDatabase.Equals(sourceDbName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!sourceResult.Columns.Contains(mapping.SourceColumn, StringComparer.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("FindMappingBasedMatchesAsync: Source column '{SourceColumn}' not found in result columns: {AvailableColumns}",
                            mapping.SourceColumn, string.Join(", ", sourceResult.Columns));
                        continue;
                    }

                    var targetResultMatch = parsedResults.Values.FirstOrDefault(r =>
                        (databaseNameMap.TryGetValue(r.DatabaseId, out var targetDbName) &&
                         targetDbName.Equals(mapping.TargetDatabase, StringComparison.OrdinalIgnoreCase)));

                    if (targetResultMatch != null &&
                        targetResultMatch.Columns.Contains(mapping.TargetColumn, StringComparer.OrdinalIgnoreCase))
                    {
                        mappings.Add((sourceResult.DatabaseId, mapping.SourceColumn,
                                     targetResultMatch.DatabaseId, mapping.TargetColumn));
                    }
                }
            }

            if (mappings.Count == 0)
            {
                _logger.LogWarning("FindMappingBasedMatchesAsync: No mappings found. Source columns available: {SourceColumns}, Target columns available: {TargetColumns}",
                    string.Join(", ", parsedResults.Values.SelectMany(r => r.Columns).Distinct()),
                    string.Join(", ", parsedResults.Values.SelectMany(r => r.Columns).Distinct()));
                return null;
            }

            _logger.LogDebug("FindMappingBasedMatchesAsync: Found {Count} potential mappings", mappings.Count);

            var bestMapping = mappings.GroupBy(m => new { m.SourceDatabaseId, m.TargetDatabaseId })
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (bestMapping == null)
                return null;

            _logger.LogInformation("FindMappingBasedMatchesAsync: Using mapping {SourceDb}.[{SourceCol}] → {TargetDb}.[{TargetCol}]",
                parsedResults[bestMapping.First().SourceDatabaseId].DatabaseName,
                bestMapping.First().SourceColumn,
                parsedResults[bestMapping.First().TargetDatabaseId].DatabaseName,
                bestMapping.First().TargetColumn);

            var firstMapping = bestMapping.First();
            var joinable = new List<(ParsedQueryResult Result, string JoinColumn)>();

            if (parsedResults.TryGetValue(firstMapping.SourceDatabaseId, out var sourceResultForJoin))
            {
                joinable.Add((sourceResultForJoin, firstMapping.SourceColumn));
            }

            if (parsedResults.TryGetValue(firstMapping.TargetDatabaseId, out var targetResultForJoin))
            {
                joinable.Add((targetResultForJoin, firstMapping.TargetColumn));
            }

            return joinable.Count >= 2 ? joinable : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error finding mapping-based matches");
            return null;
        }
    }

    private List<(ParsedQueryResult Result, string JoinColumn)>? FindValueBasedMatches(
        Dictionary<string, ParsedQueryResult> parsedResults)
    {
        var allIdColumns = new List<(string DatabaseId, string DatabaseName, string ColumnName, HashSet<string> Values)>();

        foreach (var kvp in parsedResults)
        {
            var result = kvp.Value;
            var idColumns = result.Columns.Where(col =>
                col.EndsWith("id", StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var idCol in idColumns)
            {
                var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in result.Rows)
                {
                    if (!row.TryGetValue(idCol, out var value) || string.IsNullOrEmpty(value) ||
                        value == "NULL") continue;

                    var normalizedValue = value.Trim();
                    if (TryParseNumeric(normalizedValue, out _))
                    {
                        values.Add(normalizedValue);
                    }
                }

                if (values.Count > 0)
                {
                    allIdColumns.Add((result.DatabaseId, result.DatabaseName, idCol, values));
                }
            }
        }

        if (allIdColumns.Count < 2)
            return null;

        var bestMatch = FindBestValueBasedMatch(allIdColumns);

        var joinable = new List<(ParsedQueryResult Result, string JoinColumn)>();

        foreach (var match in bestMatch)
        {
            var result = parsedResults[match.DatabaseId];
            joinable.Add((result, match.ColumnName));
        }

        return joinable.Count >= 2 ? joinable : null;
    }

    private List<(string DatabaseId, string ColumnName)>? FindBestValueBasedMatch(
        List<(string DatabaseId, string DatabaseName, string ColumnName, HashSet<string> Values)> allIdColumns)
    {
        var bestMatch = new List<(string DatabaseId, string ColumnName)>();
        var maxMatches = 0;

        for (var i = 0; i < allIdColumns.Count; i++)
        {
            for (var j = i + 1; j < allIdColumns.Count; j++)
            {
                var col1 = allIdColumns[i];
                var col2 = allIdColumns[j];

                if (col1.DatabaseId == col2.DatabaseId)
                    continue;

                var intersection = col1.Values.Intersect(col2.Values, StringComparer.OrdinalIgnoreCase).ToList();
                var matchCount = intersection.Count;

                if (matchCount <= maxMatches || !(matchCount >= Math.Min(2, Math.Min(col1.Values.Count, col2.Values.Count) * 0.1)))
                    continue;
                maxMatches = matchCount;
                bestMatch = new List<(string DatabaseId, string ColumnName)>
                {
                    (col1.DatabaseId, col1.ColumnName),
                    (col2.DatabaseId, col2.ColumnName)
                };
            }
        }

        if (maxMatches == 0)
            return null;

        _logger.LogInformation("Found value-based match: {MatchCount} matching values between {Col1} and {Col2}",
            maxMatches, bestMatch[0].ColumnName, bestMatch[1].ColumnName);

        return bestMatch;
    }

    private ParsedQueryResult? PerformInMemoryJoin(List<(ParsedQueryResult Result, string JoinColumn)> joinableResults)
    {
        if (joinableResults.Count < 2)
            return null;

        var baseResult = joinableResults[0].Result;
        var baseJoinColumn = joinableResults[0].JoinColumn;

        var mergedColumns = new List<string>(baseResult.Columns);

        for (var i = 1; i < joinableResults.Count; i++)
        {
            var otherResult = joinableResults[i].Result;
            var otherJoinColumn = joinableResults[i].JoinColumn;
            foreach (var col in otherResult.Columns)
            {
                if (!mergedColumns.Contains(col, StringComparer.OrdinalIgnoreCase))
                {
                    mergedColumns.Add(col);
                }
            }
        }

        var merged = new ParsedQueryResult
        {
            Columns = mergedColumns,
            DatabaseName = "Merged (" + string.Join(" + ", joinableResults.Select(j => j.Result.DatabaseName)) + ")"
        };

        var processedRows = 0;
        var matchedRows = 0;
        var skippedRows = 0;

        foreach (var baseRow in baseResult.Rows)
        {
            processedRows++;
            if (!baseRow.TryGetValue(baseJoinColumn, out var joinValue) || string.IsNullOrEmpty(joinValue) || joinValue == "NULL")
            {
                skippedRows++;
                continue;
            }

            var mergedRow = new Dictionary<string, string>(baseRow, StringComparer.OrdinalIgnoreCase);
            var allJoinsSuccessful = true;

            for (var i = 1; i < joinableResults.Count; i++)
            {
                var otherResult = joinableResults[i].Result;
                var otherJoinColumn = joinableResults[i].JoinColumn;

                var matchingRow = otherResult.Rows.FirstOrDefault(row =>
                {
                    if (!row.TryGetValue(otherJoinColumn, out var otherJoinValue) || string.IsNullOrEmpty(otherJoinValue) || otherJoinValue == "NULL")
                        return false;

                    return AreValuesEqual(joinValue, otherJoinValue);
                });

                if (matchingRow != null)
                {
                    foreach (var kvp in matchingRow)
                    {
                        if (!mergedRow.ContainsKey(kvp.Key))
                        {
                            mergedRow[kvp.Key] = kvp.Value;
                        }
                    }
                }
                else
                {
                    allJoinsSuccessful = false;
                    break;
                }
            }

            if (!allJoinsSuccessful)
                continue;
            merged.Rows.Add(mergedRow);
            matchedRows++;
        }

        _logger.LogInformation("PerformInMemoryJoin completed: Processed={Processed}, Matched={Matched}, Skipped={Skipped}, Final rows={FinalRows}",
            processedRows, matchedRows, skippedRows, merged.Rows.Count);

        if (merged.Rows.Count == 0)
        {
            _logger.LogWarning("PerformInMemoryJoin: No matching rows found. Base values sample: {SampleValues}",
                string.Join(", ", baseResult.Rows.Take(5).Select(r => r.TryGetValue(baseJoinColumn, out var v) ? v : "NULL")));
        }

        return merged.Rows.Count > 0 ? merged : null;
    }

    private async Task<ParsedQueryResult?> RetryMergeWithFilteredQueryAsync(
        List<(ParsedQueryResult Result, string JoinColumn)> joinableResults)
    {
        try
        {
            if (joinableResults.Count < 2)
            {
                return null;
            }

            var aggregationResult = joinableResults.FirstOrDefault(j =>
                j.Result.Columns.Any(c => c.IndexOf("Count", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          c.IndexOf("Sum", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          c.IndexOf("Total", StringComparison.OrdinalIgnoreCase) >= 0));
            ParsedQueryResult descriptiveResult = null;
            foreach (var j in joinableResults)
            {
                foreach (var col in j.Result.Columns)
                {
                    if (!await IsDescriptiveColumnAsync(col, j.Result, j.Result.DatabaseId))
                        continue;

                    descriptiveResult = j.Result;
                    break;
                }
                if (descriptiveResult != null)
                    break;
            }

            var descriptiveResultTuple = joinableResults.FirstOrDefault(j => j.Result == descriptiveResult);

            if (aggregationResult.Result == null || descriptiveResultTuple.Result == null)
            {
                return null;
            }

            var aggregationJoinColumn = aggregationResult.JoinColumn;
            var descriptiveJoinColumn = descriptiveResultTuple.JoinColumn;

            var idValues = new HashSet<string>();
            foreach (var row in aggregationResult.Result.Rows)
            {
                if (row.TryGetValue(aggregationJoinColumn, out var value) &&
                    !string.IsNullOrEmpty(value) && value != "NULL")
                {
                    idValues.Add(value.Trim());
                }
            }

            if (idValues.Count == 0)
            {
                return null;
            }

            _logger.LogInformation("RetryMergeWithFilteredQueryAsync: Found {Count} ID values: {Ids}",
                idValues.Count, string.Join(", ", idValues.Take(10)));

            var connections = await _connectionManager.GetAllConnectionsAsync();
            var descriptiveConnection = connections.FirstOrDefault(c =>
                (c.Name ?? string.Empty).Equals(descriptiveResultTuple.Result.DatabaseName, StringComparison.OrdinalIgnoreCase));

            if (descriptiveConnection == null)
            {
                _logger.LogWarning("RetryMergeWithFilteredQueryAsync: Connection not found for {Database}",
                    descriptiveResultTuple.Result.DatabaseName);
                return null;
            }

            CrossDatabaseMapping mapping = null;
            foreach (var connection in connections)
            {
                if (connection?.CrossDatabaseMappings == null)
                    continue;

                var foundMapping = connection.CrossDatabaseMappings.FirstOrDefault(m =>
                    m.TargetDatabase.Equals(descriptiveResultTuple.Result.DatabaseName, StringComparison.OrdinalIgnoreCase) &&
                    m.TargetColumn.Equals(descriptiveJoinColumn, StringComparison.OrdinalIgnoreCase));

                if (foundMapping == null)
                    continue;

                mapping = foundMapping;
                break;
            }

            if (mapping == null)
            {
                _logger.LogWarning("RetryMergeWithFilteredQueryAsync: Mapping not found for {Database}.{Column} in any connection",
                    descriptiveResultTuple.Result.DatabaseName, descriptiveJoinColumn);
                return null;
            }

            _logger.LogInformation("RetryMergeWithFilteredQueryAsync: Found mapping {SourceDb}.{SourceTable}.{SourceCol} → {TargetDb}.{TargetTable}.{TargetCol}",
                mapping.SourceDatabase, mapping.SourceTable, mapping.SourceColumn,
                mapping.TargetDatabase, mapping.TargetTable, mapping.TargetColumn);

            var tableName = mapping.TargetTable;
            var numericIds = idValues.Where(v => TryParseNumeric(v, out _)).ToList();

            if (numericIds.Count == 0)
                return null;

            var idList = string.Join(", ", numericIds);

            var descriptiveColumns = new List<string>();
            foreach (var col in descriptiveResultTuple.Result.Columns)
            {
                if (col.Equals(descriptiveJoinColumn, StringComparison.OrdinalIgnoreCase) ||
                    !await IsDescriptiveColumnAsync(col, descriptiveResultTuple.Result,
                        descriptiveResultTuple.Result.DatabaseId))
                    continue;

                descriptiveColumns.Add(col);
                if (descriptiveColumns.Count >= 5)
                    break;
            }

            if (descriptiveColumns.Count == 0)
            {
                descriptiveColumns = descriptiveResultTuple.Result.Columns
                    .Where(c => !c.Equals(descriptiveJoinColumn, StringComparison.OrdinalIgnoreCase))
                    .Take(5)
                    .ToList();
            }

            var selectColumns = new List<string> { descriptiveJoinColumn };
            selectColumns.AddRange(descriptiveColumns);

            string filterQuery;

            switch (descriptiveConnection.DatabaseType)
            {
                case DatabaseType.PostgreSQL:
                {
                    var quotedTableName = string.Join(".", tableName.Split('.').Select(p => $"\"{p}\""));
                    var quotedColumns = selectColumns.Select(c => $"\"{c}\"");
                    filterQuery = $"SELECT {string.Join(", ", quotedColumns)} FROM {quotedTableName} WHERE \"{descriptiveJoinColumn}\" IN ({idList})";
                    break;
                }
                case DatabaseType.SqlServer:
                default:
                    filterQuery = $"SELECT {string.Join(", ", selectColumns)} FROM {tableName} WHERE {descriptiveJoinColumn} IN ({idList})";
                    break;
            }

            _logger.LogInformation("Retrying merge with filtered query");

            var maxRows = descriptiveConnection.MaxRowsPerQuery > 0 ? descriptiveConnection.MaxRowsPerQuery : 100;
            var filteredResult = await _databaseParser.ExecuteQueryAsync(
                descriptiveConnection.ConnectionString,
                filterQuery,
                descriptiveConnection.DatabaseType,
                maxRows);

            var filteredParsed = ParseQueryResult(filteredResult);
            if (filteredParsed == null || filteredParsed.Rows.Count == 0)
                return null;

            filteredParsed.DatabaseName = descriptiveResultTuple.Result.DatabaseName;
            filteredParsed.DatabaseId = descriptiveResultTuple.Result.DatabaseId;

            var retryJoinable = new List<(ParsedQueryResult Result, string JoinColumn)>
            {
                (aggregationResult.Result, aggregationJoinColumn),
                (filteredParsed, descriptiveJoinColumn)
            };

            return PerformInMemoryJoin(retryJoinable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in retry merge with filtered query");
            return null;
        }
    }

    private async Task<ParsedQueryResult?> TryMergeWithMappingWhenTargetMissingAsync(
        Dictionary<string, ParsedQueryResult> parsedResults)
    {
        try
        {
            if (parsedResults.Count == 0)
                return null;

            var connections = await _connectionManager.GetAllConnectionsAsync();
            var databaseNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in parsedResults)
            {
                var result = kvp.Value;
                var connection = connections.FirstOrDefault(c =>
                    (c.Name).Equals(result.DatabaseName, StringComparison.OrdinalIgnoreCase));
                if (connection != null)
                {
                    databaseNameMap[result.DatabaseId] = connection.Name;
                }
            }

            foreach (var sourceKvp in parsedResults)
            {
                var sourceResult = sourceKvp.Value;
                if (!databaseNameMap.TryGetValue(sourceResult.DatabaseId, out var sourceDbName))
                    continue;

                var sourceConnection = connections.FirstOrDefault(c =>
                    (c.Name).Equals(sourceDbName, StringComparison.OrdinalIgnoreCase));
                if (sourceConnection?.CrossDatabaseMappings == null)
                    continue;

                foreach (var mapping in sourceConnection.CrossDatabaseMappings)
                {
                    if (!mapping.SourceDatabase.Equals(sourceDbName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!sourceResult.Columns.Any(c => c.Equals(mapping.SourceColumn, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    var targetConnection = connections.FirstOrDefault(c =>
                        (c.Name ?? string.Empty).Equals(mapping.TargetDatabase, StringComparison.OrdinalIgnoreCase));
                    if (targetConnection == null)
                        continue;

                    var hasTargetResult = parsedResults.Values.Any(r =>
                        databaseNameMap.TryGetValue(r.DatabaseId, out var targetDbName) &&
                        targetDbName.Equals(mapping.TargetDatabase, StringComparison.OrdinalIgnoreCase));

                    if (hasTargetResult)
                        continue;

                    _logger.LogInformation("Found mapping {SourceDb}.{SourceTable}.{SourceCol} → {TargetDb}.{TargetTable}.{TargetCol}, but target database result is missing. Generating filtered query...",
                        mapping.SourceDatabase, mapping.SourceTable, mapping.SourceColumn,
                        mapping.TargetDatabase, mapping.TargetTable, mapping.TargetColumn);

                    var idValues = new HashSet<string>();
                    foreach (var row in sourceResult.Rows)
                    {
                        if (row.TryGetValue(mapping.SourceColumn, out var value) &&
                            !string.IsNullOrEmpty(value) && value != "NULL")
                        {
                            idValues.Add(value.Trim());
                        }
                    }

                    if (idValues.Count == 0)
                        continue;

                    var numericIds = idValues.Where(v => TryParseNumeric(v, out _)).ToList();
                    if (numericIds.Count == 0)
                        continue;

                    var idList = string.Join(", ", numericIds);

                    var targetJoinColumn = mapping.TargetColumn;
                    var tableName = mapping.TargetTable;

                    var selectColumns = new List<string> { targetJoinColumn };

                    var testQuery = targetConnection.DatabaseType == DatabaseType.PostgreSQL
                        ? $"SELECT * FROM {tableName} LIMIT 1"
                        : targetConnection.DatabaseType == DatabaseType.SqlServer
                        ? $"SELECT TOP 1 * FROM {tableName}"
                        : $"SELECT * FROM {tableName} LIMIT 1";

                    try
                    {
                        var testResult = await _databaseParser.ExecuteQueryAsync(
                            targetConnection.ConnectionString,
                            testQuery,
                            targetConnection.DatabaseType,
                            1);

                        var testParsed = ParseQueryResult(testResult);
                        if (testParsed != null && testParsed.Columns.Any())
                        {
                            testParsed.DatabaseName = mapping.TargetDatabase;

                            var descriptiveColumns = new List<string>();
                            var targetDatabaseId = await _connectionManager.GetDatabaseIdAsync(targetConnection);

                            foreach (var col in testParsed.Columns)
                            {
                                if (!col.Equals(targetJoinColumn, StringComparison.OrdinalIgnoreCase) &&
                                    await IsDescriptiveColumnAsync(col, testParsed, targetDatabaseId))
                                {
                                    descriptiveColumns.Add(col);
                                    if (descriptiveColumns.Count >= 5)
                                        break;
                                }
                            }

                            if (descriptiveColumns.Any())
                            {
                                selectColumns.AddRange(descriptiveColumns);
                            }
                            else
                            {
                                var fallbackColumns = testParsed.Columns
                                    .Where(c => !c.Equals(targetJoinColumn, StringComparison.OrdinalIgnoreCase))
                                    .Take(5)
                                    .ToList();
                                selectColumns.AddRange(fallbackColumns);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not determine descriptive columns for table {Table}, using join column only", tableName);
                    }

                    selectColumns = selectColumns.Distinct().ToList();

                    string filterQuery;

                    switch (targetConnection.DatabaseType)
                    {
                        case DatabaseType.PostgreSQL:
                        {
                            var quotedColumns = selectColumns.Select(c => $"\"{c}\"");
                            filterQuery = $"SELECT {string.Join(", ", quotedColumns)} FROM {tableName} WHERE \"{targetJoinColumn}\" IN ({idList}) LIMIT 100";
                            break;
                        }
                        case SmartRAG.Enums.DatabaseType.SqlServer:
                            filterQuery = $"SELECT TOP 100 {string.Join(", ", selectColumns)} FROM {tableName} WHERE {targetJoinColumn} IN ({idList})";
                            break;
                        default:
                            filterQuery = $"SELECT {string.Join(", ", selectColumns)} FROM {tableName} WHERE {targetJoinColumn} IN ({idList}) LIMIT 100";
                            break;
                    }

                    _logger.LogInformation("Executing filtered query for missing target database");

                    var maxRows = targetConnection.MaxRowsPerQuery > 0 ? targetConnection.MaxRowsPerQuery : 100;
                    var filteredResult = await _databaseParser.ExecuteQueryAsync(
                        targetConnection.ConnectionString,
                        filterQuery,
                        targetConnection.DatabaseType,
                        maxRows);

                    var filteredParsed = ParseQueryResult(filteredResult);
                    if (filteredParsed == null || filteredParsed.Rows.Count == 0)
                        continue;

                    filteredParsed.DatabaseName = mapping.TargetDatabase;
                    filteredParsed.DatabaseId = targetConnection.Name ?? mapping.TargetDatabase;

                    var joinableResults = new List<(ParsedQueryResult Result, string JoinColumn)>
                    {
                        (sourceResult, mapping.SourceColumn),
                        (filteredParsed, targetJoinColumn)
                    };

                    var merged = PerformInMemoryJoin(joinableResults);

                    if (merged == null || merged.Rows.Count <= 0)
                        continue;
                    _logger.LogInformation("Successfully merged results using mapping when target database was missing. Merged {RowCount} rows.", merged.Rows.Count);

                    return merged;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error trying merge with mapping when target missing");
            return null;
        }
    }

    private static bool AreValuesEqual(string value1, string value2)
    {
        if (string.IsNullOrWhiteSpace(value1) || string.IsNullOrWhiteSpace(value2))
            return false;

        var v1 = value1.Trim();
        var v2 = value2.Trim();

        if (v1.Equals(v2, StringComparison.OrdinalIgnoreCase))
            return true;

        if (TryParseNumeric(v1, out var num1) && TryParseNumeric(v2, out var num2))
        {
            return Math.Abs(num1 - num2) < 0.0001;
        }

        return false;
    }

    private static string FormatParsedResult(ParsedQueryResult result)
    {
        var sb = new StringBuilder();

        if (result.Rows.Count == 0)
        {
            sb.AppendLine("No rows found");
            return sb.ToString();
        }

        sb.AppendLine($"📊 Total rows: {result.Rows.Count} | Columns: {string.Join(", ", result.Columns)}");
        sb.AppendLine();
        sb.AppendLine(string.Join("\t", result.Columns));

        foreach (var row in result.Rows)
        {
            var values = result.Columns.Select(col => row.TryGetValue(col, out var val) ? val : "NULL");
            sb.AppendLine(string.Join("\t", values));
        }

        return sb.ToString();
    }

    private static void AppendSeparateResults(StringBuilder sb, Dictionary<string, ParsedQueryResult> parsedResults)
    {
        foreach (var kvp in parsedResults.OrderBy(x => x.Value.DatabaseName))
        {
            var result = kvp.Value;
            sb.AppendLine($"=== {result.DatabaseName} ===");
            sb.AppendLine("🚨 CRITICAL: Use ONLY the data shown below. DO NOT invent any names or values!");
            sb.AppendLine(FormatParsedResult(result));
            sb.AppendLine("🚨 REMINDER: If names are not shown above, use ID only - NEVER invent names!");
            sb.AppendLine();
        }
    }

    private static void AppendSeparateResultsWithJoinHints(StringBuilder sb, Dictionary<string, ParsedQueryResult> parsedResults)
    {
        // Find common ID columns that could be used for joining
        var allIdColumns = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in parsedResults)
        {
            var result = kvp.Value;
            foreach (var col in result.Columns)
            {
                if (!col.EndsWith("id", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!allIdColumns.ContainsKey(col))
                {
                    allIdColumns[col] = new HashSet<string>();
                }
                allIdColumns[col].Add(result.DatabaseName);
            }
        }

        var commonIdColumns = allIdColumns
            .Where(kvp => kvp.Value.Count >= 2)
            .Select(kvp => kvp.Key)
            .ToList();

        // Collect all ID values from all results for matching
        var idValueMap = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.OrdinalIgnoreCase);

        foreach (var idCol in commonIdColumns)
        {
            idValueMap[idCol] = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in parsedResults)
            {
                var result = kvp.Value;
                if (!result.Columns.Contains(idCol, StringComparer.OrdinalIgnoreCase))
                    continue;

                foreach (var row in result.Rows)
                {
                    if (!row.TryGetValue(idCol, out var idValue) || string.IsNullOrEmpty(idValue) ||
                        idValue == "NULL") continue;

                    if (!idValueMap[idCol].ContainsKey(idValue))
                    {
                        idValueMap[idCol][idValue] = new List<string>();
                    }
                    idValueMap[idCol][idValue].Add(result.DatabaseName);
                }
            }
        }

        // Find matching ID values across databases
        var matchingIds = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var idCol in commonIdColumns)
        {
            foreach (var kvp in idValueMap[idCol])
            {
                if (kvp.Value.Count < 2)
                    continue; // ID appears in at least 2 databases
                if (!matchingIds.ContainsKey(idCol))
                {
                    matchingIds[idCol] = new List<string>();
                }
                matchingIds[idCol].Add(kvp.Key);
            }
        }

        foreach (var kvp in parsedResults.OrderBy(x => x.Value.DatabaseName))
        {
            var result = kvp.Value;
            sb.AppendLine($"=== {result.DatabaseName} ===");
            sb.AppendLine("🚨 CRITICAL: Use ONLY the data shown below. DO NOT invent any names or values!");
            sb.AppendLine(FormatParsedResult(result));
            sb.AppendLine("🚨 REMINDER: If names are not shown above, use ID only - NEVER invent names!");

            // Add join hints if common ID columns exist
            if (commonIdColumns.Any())
            {
                var resultIdColumns = result.Columns
                    .Where(col => commonIdColumns.Contains(col, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (resultIdColumns.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine($"⚠️ JOIN HINT: This result contains ID column(s): {string.Join(", ", resultIdColumns)}");

                    // Show actual ID values from this result
                    if (result.Rows.Any())
                    {
                        var firstRow = result.Rows.First();
                        var idValues = resultIdColumns
                            .Where(idCol => firstRow.ContainsKey(idCol))
                            .Select(idCol => $"{idCol}={firstRow[idCol]}")
                            .ToList();

                        if (idValues.Any())
                        {
                            sb.AppendLine($"   ID value(s) in this result: {string.Join(", ", idValues)}");
                        }
                    }
                }
            }

            sb.AppendLine();
        }

        if (!commonIdColumns.Any())
            return;

        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("🔗 CROSS-DATABASE JOIN INSTRUCTIONS:");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"Common ID columns found: {string.Join(", ", commonIdColumns)}");
        sb.AppendLine();

        // Show matching ID values if found
        if (matchingIds.Any())
        {
            sb.AppendLine("✅ MATCHING ID VALUES FOUND (use these for joining):");
            foreach (var idCol in commonIdColumns)
            {
                if (matchingIds.ContainsKey(idCol) && matchingIds[idCol].Any())
                {
                    sb.AppendLine($"   {idCol}: {string.Join(", ", matchingIds[idCol])}");
                }
            }
            sb.AppendLine();
            sb.AppendLine("CRITICAL: Use the ID values listed above to match rows across databases.");
            sb.AppendLine("For example, if PrimaryKeyColumn=ValueX appears in both databases, combine those rows.");
        }
        else
        {
            sb.AppendLine("⚠️ WARNING: No matching ID values found across databases.");
            sb.AppendLine("This means the results cannot be automatically joined.");
            sb.AppendLine();
            sb.AppendLine("POSSIBLE REASONS:");
            sb.AppendLine("1. The queries returned different ID values (e.g., different entities)");
            sb.AppendLine("2. One query needs to be filtered using the ID from the other query");
            sb.AppendLine("3. The queries are not related (wrong database selection)");
            sb.AppendLine();
            sb.AppendLine("SOLUTION:");
            sb.AppendLine("Look at the ID values in each result above.");
            sb.AppendLine("If one result has an ID (e.g., PrimaryKeyColumn=ValueX), use that ID to filter the other database.");
            sb.AppendLine("For example: If Database1 has PrimaryKeyColumn=ValueX, Database2 should query WHERE PrimaryKeyColumn=ValueX");
        }

        sb.AppendLine();
        sb.AppendLine("To answer the question correctly:");
        sb.AppendLine("1. Find matching ID values across different database results");
        sb.AppendLine("2. Combine information from rows that have the same ID value");
        sb.AppendLine("3. Use the combined information to provide the final answer");
        sb.AppendLine();
    }

    /// <summary>
    /// Determines if a column is descriptive (contains text data) using schema-based detection and fallback patterns
    /// </summary>
    private async Task<bool> IsDescriptiveColumnAsync(string columnName, ParsedQueryResult result, string databaseId)
    {
        var columnNameLower = columnName.ToLowerInvariant();

        if (NonDescriptivePatterns.Any(pattern => columnNameLower.Contains(pattern.ToLowerInvariant())))
        {
            return false;
        }

        try
        {
            var schema = await _schemaAnalyzer.GetSchemaAsync(databaseId);

            foreach (var table in schema.Tables)
            {
                var column = table.Columns.FirstOrDefault(c =>
                    c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));

                if (column == null)
                    continue;

                var dataTypeLower = column.DataType.ToLowerInvariant();

                if (column.IsPrimaryKey || column.IsForeignKey)
                {
                    return false;
                }

                var textTypes = new[] { "varchar", "nvarchar", "char", "nchar", "text", "ntext", "string" };
                if (!textTypes.Any(type => dataTypeLower.Contains(type)))
                    continue;

                switch (column.MaxLength)
                {
                    case > 10:
                    case null:
                        return true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking schema for column {ColumnName} in database {DatabaseId}", columnName, databaseId);
        }

        if (DescriptiveColumnPatterns.Any(pattern =>
            columnNameLower.Contains(pattern.ToLowerInvariant())))
        {
            return true;
        }

        if (result.Rows.Count <= 0)
            return false;

        var sampleValues = result.Rows.Take(10)
            .Where(row => row.TryGetValue(columnName, out var val) && !string.IsNullOrEmpty(val) && val != "NULL")
            .Select(row => row[columnName])
            .ToList();

        if (sampleValues.Count <= 0)
            return false;

        var nonNumericCount = sampleValues.Count(v => !TryParseNumeric(v, out _));
        return nonNumericCount > sampleValues.Count * 0.7;
    }


    /// <summary>
    /// Attempts to parse a string value as a numeric value for comparison
    /// </summary>
    private static bool TryParseNumeric(string value, out double numericValue)
    {
        numericValue = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Remove whitespace and try parsing
        var trimmed = value.Trim();
        return double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out numericValue);
    }

    private static List<string> ExtractTableNames(string query)
    {
        var tables = new List<string>();

        if (string.IsNullOrWhiteSpace(query))
        {
            return tables;
        }

        var matches = Regex.Matches(query, @"\bFROM\s+([^\s,;]+)|\bJOIN\s+([^\s,;]+)", RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var tableName = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            tableName = tableName.Trim();

            if (!string.IsNullOrEmpty(tableName) && !tables.Contains(tableName, StringComparer.OrdinalIgnoreCase))
            {
                tables.Add(tableName);
            }
        }

        return tables;
    }

    private static string RemoveSQLCodeBlocksFromAnswer(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return answer;

        var result = answer;

        result = Regex.Replace(
            result,
            @"```sql\s*[\s\S]*?```",
            string.Empty,
            RegexOptions.IgnoreCase);

        result = Regex.Replace(
            result,
            @"```\s*[\s\S]*?```",
            string.Empty,
            RegexOptions.IgnoreCase);

        result = Regex.Replace(
            result,
            @"SELECT\s+[\w\s,\.\(\)\*]+\s+FROM\s+[\w\s,\.\(\)]+(?:\s+WHERE\s+[\w\s,\.\(\)=<>'""]+)?(?:\s+GROUP\s+BY\s+[\w\s,\.\(\)]+)?(?:\s+ORDER\s+BY\s+[\w\s,\.\(\)]+)?(?:\s+LIMIT\s+\d+)?;?",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        result = Regex.Replace(
            result,
            @"Please\s+.*?database\s+.*?run:?",
            string.Empty,
            RegexOptions.IgnoreCase);

        result = Regex.Replace(
            result,
            @"This\s+query\s+.*?will\s+show\.?",
            string.Empty,
            RegexOptions.IgnoreCase);

        return result.Trim();
    }

    private static string BuildDatabaseLocationDescription(string databaseName, string query, int rowCount)
    {
        var builder = new StringBuilder();
        builder.Append($"Database: {databaseName}");
        builder.Append($" | Rows: {rowCount}");

        if (string.IsNullOrWhiteSpace(query))
            return builder.ToString();

        var sanitizedQuery = Regex.Replace(query, @"\s+", " ").Trim();
        if (sanitizedQuery.Length > 160)
        {
            sanitizedQuery = sanitizedQuery[..160] + "...";
        }

        builder.Append(" | Query: ");
        builder.Append(sanitizedQuery);

        return builder.ToString();
    }

    private class ParsedQueryResult
    {
        public string DatabaseName { get; set; }
        public string DatabaseId { get; set; }
        public List<string> Columns { get; set; } = new();
        public List<Dictionary<string, string>> Rows { get; set; } = new();
    }
}


