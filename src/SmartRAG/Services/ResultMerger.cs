using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartRAG.Services
{
    /// <summary>
    /// Merges results from multiple databases into coherent responses
    /// </summary>
    public class ResultMerger : IResultMerger
    {
        #region Fields

        private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer;
        private readonly IAIService _aiService;
        private readonly ILogger<ResultMerger> _logger;

        #endregion

        #region Constructor

        public ResultMerger(
            IDatabaseSchemaAnalyzer schemaAnalyzer,
            IAIService aiService,
            ILogger<ResultMerger> logger)
        {
            _schemaAnalyzer = schemaAnalyzer;
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

            // Parse all database results into structured data
            var parsedResults = new Dictionary<string, ParsedQueryResult>();
            var allSchemas = await _schemaAnalyzer.GetAllSchemasAsync();
            
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

            // If we have multiple successful databases, try to smart merge them
            if (parsedResults.Count > 1)
            {
                var mergedData = await SmartMergeResultsAsync(parsedResults, allSchemas);
                if (mergedData != null && mergedData.Rows.Count > 0)
                {
                    sb.AppendLine("=== SMART MERGED RESULTS (Cross-Database JOIN) ===");
                    sb.AppendLine(FormatParsedResult(mergedData));
                    sb.AppendLine();
                }
                else
                {
                    _logger.LogWarning("Smart merge failed, falling back to separate results");
                    AppendSeparateResults(sb, parsedResults);
                }
            }
            else
            {
                // Only one database or no successful results - show separately
                AppendSeparateResults(sb, parsedResults);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates final AI answer from merged database results
        /// </summary>
        public async Task<RagResponse> GenerateFinalAnswerAsync(
            string userQuery, 
            string mergedData, 
            MultiDatabaseQueryResult queryResults)
        {
            try
            {
                var prompt = $@"Answer the user's question using ONLY the database information provided.

CRITICAL RULES:
- Provide DIRECT, CONCISE answer to the question
- Use ONLY information from the database results below
- Do NOT explain data sources or methodology
- Do NOT mention what information is missing or unavailable
- Do NOT add unnecessary context or background
- Do NOT repeat the question
- Keep response SHORT and TO THE POINT

User Question: {userQuery}

Database Results:
{mergedData}

Direct Answer:";

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
                
                // Find header line (tab-separated column names)
                string[] headers = null;
                int headerIndex = -1;
                
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains('\t') && !lines[i].StartsWith("===") && !lines[i].StartsWith("Query:") && !lines[i].StartsWith("Rows"))
                    {
                        headers = lines[i].Split('\t');
                        headerIndex = i;
                        break;
                    }
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
                
                // Parse data rows
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
            Dictionary<string, ParsedQueryResult> parsedResults,
            List<DatabaseSchemaInfo> allSchemas)
        {
            try
            {
                if (parsedResults.Count < 2)
                    return null;
                
                _logger.LogInformation("Attempting smart merge of {Count} databases", parsedResults.Count);
                
                // Find foreign key relationships between databases
                var joinableResults = await FindJoinableTablesAsync(parsedResults, allSchemas);
                
                if (joinableResults == null || joinableResults.Count < 2)
                {
                    _logger.LogWarning("No joinable relationships found between databases");
                    return null;
                }
                
                // Perform inner join based on foreign keys
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
            Dictionary<string, ParsedQueryResult> parsedResults,
            List<DatabaseSchemaInfo> allSchemas)
        {
            await Task.CompletedTask;
            
            var joinable = new List<(ParsedQueryResult Result, string JoinColumn)>();
            
            // Collect all potential join columns from all results
            var allJoinCandidates = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var kvp in parsedResults)
            {
                var result = kvp.Value;
                
                // Find columns that end with "id" (any case: ID, Id, id) - generic foreign key pattern
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
            
            // Find join columns that appear in at least 2 databases
            var commonJoinColumns = allJoinCandidates.Where(kvp => kvp.Value.Count >= 2).ToList();
            
            if (commonJoinColumns.Count == 0)
            {
                _logger.LogWarning("No common join columns found across databases");
                return null;
            }
            
            // Use the most common join column (appears in most databases)
            var bestJoinColumn = commonJoinColumns.OrderByDescending(kvp => kvp.Value.Count).First().Key;
            
            // Build joinable list with the selected column
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
            
            // Start with the first table
            var baseResult = joinableResults[0].Result;
            var baseJoinColumn = joinableResults[0].JoinColumn;
            
            // Build merged columns (avoid duplicates)
            var mergedColumns = new List<string>(baseResult.Columns);
            
            for (int i = 1; i < joinableResults.Count; i++)
            {
                var otherResult = joinableResults[i].Result;
                foreach (var col in otherResult.Columns)
                {
                    // Don't duplicate join column or already existing columns
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
            
            // Perform INNER JOIN: iterate base table and find matching rows in other tables
            foreach (var baseRow in baseResult.Rows)
            {
                if (!baseRow.TryGetValue(baseJoinColumn, out var joinValue) || string.IsNullOrEmpty(joinValue) || joinValue == "NULL")
                    continue;
                
                // Start with base row data
                var mergedRow = new Dictionary<string, string>(baseRow, StringComparer.OrdinalIgnoreCase);
                bool allJoinsSuccessful = true;
                
                // Try to join with each other table
                for (int i = 1; i < joinableResults.Count; i++)
                {
                    var otherResult = joinableResults[i].Result;
                    var otherJoinColumn = joinableResults[i].JoinColumn;
                    
                    // Find matching row in other table
                    var matchingRow = otherResult.Rows.FirstOrDefault(row =>
                        row.TryGetValue(otherJoinColumn, out var otherJoinValue) &&
                        joinValue.Equals(otherJoinValue, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingRow != null)
                    {
                        // Add columns from matching row (skip duplicates)
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
                
                // Add merged row only if all joins were successful (INNER JOIN)
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
            
            // Header
            sb.AppendLine(string.Join("\t", result.Columns));
            
            // Rows
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

