using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Database;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartRAG.Services.Database
{
    /// <summary>
    /// Merges results from multiple databases into coherent responses
    /// </summary>
    public class ResultMerger : IResultMerger
    {
        #region Fields

        private readonly IAIService _aiService;
        private readonly ILogger<ResultMerger> _logger;

        #endregion

        #region Constructor

        public ResultMerger(
            IAIService aiService,
            ILogger<ResultMerger> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        #endregion

        #region Public Methods

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
                if (parsedData != null && parsedData.Rows.Count > 0)
                {
                    parsedResults[dbResult.DatabaseId] = parsedData;
                    parsedData.DatabaseName = dbResult.DatabaseName;
                    parsedData.DatabaseId = dbResult.DatabaseId;
                }
            }

            if (parsedResults.Count > 1)
            {
                var mergedData = await SmartMergeResultsAsync(parsedResults);
                if (mergedData != null && mergedData.Rows.Count > 0)
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
            string preferredLanguage = null)
        {
            try
            {
                var hasMultipleDatabases = queryResults.DatabaseResults.Count(r => r.Value.Success) > 1;
                var hasMergedResults = mergedData.Contains("SMART MERGED RESULTS");

                var promptBuilder = new StringBuilder();
                
                // Add language instruction if preferred language is specified
                if (!string.IsNullOrWhiteSpace(preferredLanguage))
                {
                    promptBuilder.AppendLine($"IMPORTANT: Respond in {preferredLanguage.ToUpperInvariant()} language.");
                    promptBuilder.AppendLine();
                }
                
                promptBuilder.AppendLine("Answer the user's question using ONLY the database information provided.");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("CRITICAL RULES:");
                promptBuilder.AppendLine("- Provide DIRECT, CONCISE answer to the question");
                promptBuilder.AppendLine("- Use ONLY information from the database results below");
                promptBuilder.AppendLine("- Do NOT explain data sources or methodology");
                promptBuilder.AppendLine("- Do NOT mention what information is missing or unavailable");
                promptBuilder.AppendLine("- Do NOT add unnecessary context or background");
                promptBuilder.AppendLine("- Do NOT repeat the question");
                promptBuilder.AppendLine("- Keep response SHORT and TO THE POINT");

                if (hasMultipleDatabases && !hasMergedResults)
                {
                    promptBuilder.AppendLine();
                    promptBuilder.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
                    promptBuilder.AppendLine("║  🚨 MULTIPLE DATABASES - MANUAL DATA CORRELATION REQUIRED 🚨  ║");
                    promptBuilder.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
                    promptBuilder.AppendLine();
                    promptBuilder.AppendLine("The results below come from DIFFERENT databases that could not be automatically merged.");
                    promptBuilder.AppendLine("You MUST manually correlate the data to answer the question:");
                    promptBuilder.AppendLine();
                    promptBuilder.AppendLine("1. Look for common ID columns (e.g., EntityID, ReferenceID, ForeignKeyID)");
                    promptBuilder.AppendLine("2. Match rows from different databases using these ID columns");
                    promptBuilder.AppendLine("3. Combine the information to provide a complete answer");
                    promptBuilder.AppendLine("4. If you find matching IDs, use the corresponding data from each database");
                    promptBuilder.AppendLine();
                    promptBuilder.AppendLine("EXAMPLE:");
                    promptBuilder.AppendLine("  Database A: EntityID=123, CountValue=5");
                    promptBuilder.AppendLine("  Database B: EntityID=123, NameColumn='Value1', DescriptionColumn='Value2'");
                    promptBuilder.AppendLine("  → Answer: Value1 Value2 (EntityID 123) has CountValue 5");
                    promptBuilder.AppendLine();
                }

                promptBuilder.AppendLine();
                promptBuilder.AppendLine($"User Question: {userQuery}");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Database Results:");
                promptBuilder.AppendLine(mergedData);
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Direct Answer:");

                var prompt = promptBuilder.ToString();

                var answer = await _aiService.GenerateResponseAsync(prompt, new List<string>());

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
                        Tables = ExtractTableNames(r.Value.ExecutedQuery ?? string.Empty),
                        Location = BuildDatabaseLocationDescription(r.Value.DatabaseName, r.Value.ExecutedQuery ?? string.Empty, r.Value.RowCount)
                    })
                    .ToList();

                return new RagResponse
                {
                    Query = userQuery,
                    Answer = answer ?? "Unable to generate answer",
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
                        new SearchSource
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

        #endregion

        #region Private Helper Methods

        private ParsedQueryResult ParseQueryResult(string resultData)
        {
            try
            {
                var lines = resultData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                string[] headers = null;
                int headerIndex = -1;

                for (int i = 0; i < lines.Length; i++)
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

                for (int i = headerIndex + 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.StartsWith("Rows extracted:") || line.StartsWith("==="))
                        break;

                    var values = line.Split('\t');
                    if (values.Length == headers.Length)
                    {
                        var row = new Dictionary<string, string>();
                        for (int j = 0; j < headers.Length; j++)
                        {
                            row[headers[j]] = values[j];
                        }
                        result.Rows.Add(row);
                    }
                }

                _logger.LogInformation("Parsed {RowCount} rows with {ColumnCount} columns", result.Rows.Count, result.Columns.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing query result");
                return null;
            }
        }

        private async Task<ParsedQueryResult> SmartMergeResultsAsync(
            Dictionary<string, ParsedQueryResult> parsedResults)
        {
            try
            {
                if (parsedResults.Count < 2)
                    return null;

                _logger.LogInformation("Attempting smart merge of {Count} databases", parsedResults.Count);

                var joinableResults = await FindJoinableTablesAsync(parsedResults);

                if (joinableResults == null || joinableResults.Count < 2)
                {
                    _logger.LogWarning("No joinable relationships found between databases");
                    return null;
                }

                var merged = PerformInMemoryJoin(joinableResults);

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Smart merge completed: {RowCount} merged rows", merged?.Rows.Count ?? 0);
                }
                return merged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in smart merge");
                return null;
            }
        }

        private async Task<List<(ParsedQueryResult Result, string JoinColumn)>> FindJoinableTablesAsync(
            Dictionary<string, ParsedQueryResult> parsedResults)
        {
            await Task.CompletedTask;

            var joinable = new List<(ParsedQueryResult Result, string JoinColumn)>();

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

            if (commonJoinColumns.Count == 0)
            {
                _logger.LogWarning("No common join columns found across databases");
                return null;
            }

            var bestJoinColumn = commonJoinColumns.OrderByDescending(kvp => kvp.Value.Count).First().Key;

            foreach (var kvp in parsedResults)
            {
                var result = kvp.Value;

                if (result.Columns.Any(col => col.Equals(bestJoinColumn, StringComparison.OrdinalIgnoreCase)))
                {
                    joinable.Add((result, bestJoinColumn));
                }
            }

            return joinable.Count >= 2 ? joinable : null;
        }

        private ParsedQueryResult PerformInMemoryJoin(List<(ParsedQueryResult Result, string JoinColumn)> joinableResults)
        {
            if (joinableResults.Count < 2)
                return null;

            var baseResult = joinableResults[0].Result;
            var baseJoinColumn = joinableResults[0].JoinColumn;

            var mergedColumns = new List<string>(baseResult.Columns);

            for (int i = 1; i < joinableResults.Count; i++)
            {
                var otherResult = joinableResults[i].Result;
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

            foreach (var baseRow in baseResult.Rows)
            {
                if (!baseRow.TryGetValue(baseJoinColumn, out var joinValue) || string.IsNullOrEmpty(joinValue) || joinValue == "NULL")
                    continue;

                var mergedRow = new Dictionary<string, string>(baseRow, StringComparer.OrdinalIgnoreCase);
                bool allJoinsSuccessful = true;

                for (int i = 1; i < joinableResults.Count; i++)
                {
                    var otherResult = joinableResults[i].Result;
                    var otherJoinColumn = joinableResults[i].JoinColumn;

                    var matchingRow = otherResult.Rows.FirstOrDefault(row =>
                    {
                        if (!row.TryGetValue(otherJoinColumn, out var otherJoinValue) || string.IsNullOrEmpty(otherJoinValue) || otherJoinValue == "NULL")
                            return false;

                        // Try exact string match first (case-insensitive)
                        if (joinValue.Equals(otherJoinValue, StringComparison.OrdinalIgnoreCase))
                            return true;

                        // Try numeric comparison (handles "123" vs "123.0" vs "123 ")
                        if (TryParseNumeric(joinValue, out var baseNum) && TryParseNumeric(otherJoinValue, out var otherNum))
                        {
                            return baseNum == otherNum;
                        }

                        // Try trimmed comparison (handles whitespace differences)
                        if (joinValue.Trim().Equals(otherJoinValue.Trim(), StringComparison.OrdinalIgnoreCase))
                            return true;

                        return false;
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
                        break; // No match - skip this base row (INNER JOIN behavior)
                    }
                }

                if (allJoinsSuccessful)
                {
                    merged.Rows.Add(mergedRow);
                }
            }

            return merged;
        }

        private string FormatParsedResult(ParsedQueryResult result)
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Join("\t", result.Columns));

            foreach (var row in result.Rows)
            {
                var values = result.Columns.Select(col => row.TryGetValue(col, out var val) ? val : "NULL");
                sb.AppendLine(string.Join("\t", values));
            }

            sb.AppendLine($"\nMerged rows: {result.Rows.Count}");

            return sb.ToString();
        }

        private void AppendSeparateResults(StringBuilder sb, Dictionary<string, ParsedQueryResult> parsedResults)
        {
            foreach (var kvp in parsedResults.OrderBy(x => x.Value.DatabaseName))
            {
                var result = kvp.Value;
                sb.AppendLine($"=== {result.DatabaseName} ===");
                sb.AppendLine(FormatParsedResult(result));
                sb.AppendLine();
            }
        }

        private void AppendSeparateResultsWithJoinHints(StringBuilder sb, Dictionary<string, ParsedQueryResult> parsedResults)
        {
            // Find common ID columns that could be used for joining
            var allIdColumns = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in parsedResults)
            {
                var result = kvp.Value;
                foreach (var col in result.Columns)
                {
                    if (col.EndsWith("id", StringComparison.OrdinalIgnoreCase) ||
                        col.EndsWith("ID", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!allIdColumns.ContainsKey(col))
                        {
                            allIdColumns[col] = new HashSet<string>();
                        }
                        allIdColumns[col].Add(result.DatabaseName);
                    }
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
                    if (result.Columns.Contains(idCol, StringComparer.OrdinalIgnoreCase))
                    {
                        foreach (var row in result.Rows)
                        {
                            if (row.TryGetValue(idCol, out var idValue) && !string.IsNullOrEmpty(idValue) && idValue != "NULL")
                            {
                                if (!idValueMap[idCol].ContainsKey(idValue))
                                {
                                    idValueMap[idCol][idValue] = new List<string>();
                                }
                                idValueMap[idCol][idValue].Add(result.DatabaseName);
                            }
                        }
                    }
                }
            }

            // Find matching ID values across databases
            var matchingIds = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var idCol in commonIdColumns)
            {
                foreach (var kvp in idValueMap[idCol])
                {
                    if (kvp.Value.Count >= 2) // ID appears in at least 2 databases
                    {
                        if (!matchingIds.ContainsKey(idCol))
                        {
                            matchingIds[idCol] = new List<string>();
                        }
                        matchingIds[idCol].Add(kvp.Key);
                    }
                }
            }

            foreach (var kvp in parsedResults.OrderBy(x => x.Value.DatabaseName))
            {
                var result = kvp.Value;
                sb.AppendLine($"=== {result.DatabaseName} ===");
                sb.AppendLine(FormatParsedResult(result));

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

            if (commonIdColumns.Any())
            {
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
                    sb.AppendLine("For example, if EntityID=123 appears in both databases, combine those rows.");
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
                    sb.AppendLine("If one result has an ID (e.g., EntityID=123), use that ID to filter the other database.");
                    sb.AppendLine("For example: If Database1 has EntityID=123, Database2 should query WHERE EntityID=123");
                }

                sb.AppendLine();
                sb.AppendLine("To answer the question correctly:");
                sb.AppendLine("1. Find matching ID values across different database results");
                sb.AppendLine("2. Combine information from rows that have the same ID value");
                sb.AppendLine("3. Use the combined information to provide the final answer");
                sb.AppendLine();
            }
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
            if (double.TryParse(trimmed, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out numericValue))
                return true;

            return false;
        }

        #endregion

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

        private static string BuildDatabaseLocationDescription(string databaseName, string query, int rowCount)
        {
            var builder = new StringBuilder();
            builder.Append($"Database: {databaseName}");
            builder.Append($" | Rows: {rowCount}");

            if (!string.IsNullOrWhiteSpace(query))
            {
                var sanitizedQuery = Regex.Replace(query, @"\s+", " ").Trim();
                if (sanitizedQuery.Length > 160)
                {
                    sanitizedQuery = sanitizedQuery[..160] + "...";
                }

                builder.Append(" | Query: ");
                builder.Append(sanitizedQuery);
            }

            return builder.ToString();
        }

        #region Helper Classes

        private class ParsedQueryResult
        {
            public string DatabaseName { get; set; }
            public string DatabaseId { get; set; }
            public List<string> Columns { get; set; } = new List<string>();
            public List<Dictionary<string, string>> Rows { get; set; } = new List<Dictionary<string, string>>();
        }

        #endregion
    }
}

