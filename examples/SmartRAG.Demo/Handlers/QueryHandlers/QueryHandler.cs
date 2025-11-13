using Microsoft.Extensions.Logging;
using SmartRAG.Demo.Models;
using SmartRAG.Demo.Services.Console;
using SmartRAG.Demo.Services.TestQuery;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SmartRAG.Demo.Handlers.QueryHandlers;

/// <summary>
/// Handler for query-related operations
/// </summary>
public class QueryHandler(
    ILogger<QueryHandler> logger,
    IConsoleService console,
    IMultiDatabaseQueryCoordinator multiDbCoordinator,
    IAIService aiService,
    IDocumentService documentService,
    IDocumentSearchService documentSearchService,
    IDatabaseSchemaAnalyzer schemaAnalyzer,
    ITestQueryGenerator testQueryGenerator) : IQueryHandler
{
    #region Fields

    private readonly ILogger<QueryHandler> _logger = logger;
    private readonly IConsoleService _console = console;
    private readonly IMultiDatabaseQueryCoordinator _multiDbCoordinator = multiDbCoordinator;
    private readonly IAIService _aiService = aiService;
    private readonly IDocumentService _documentService = documentService;
    private readonly IDocumentSearchService _documentSearchService = documentSearchService;
    private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer = schemaAnalyzer;
    private readonly ITestQueryGenerator _testQueryGenerator = testQueryGenerator;

    #endregion

    #region Public Methods

    public async Task RunMultiDatabaseQueryAsync(string language)
    {
        _console.WriteSectionHeader("🤖 Multi-Database Smart Query");

        System.Console.ForegroundColor = ConsoleColor.DarkGray;
        System.Console.WriteLine($"Language: {language}");
        System.Console.ResetColor();
        System.Console.WriteLine();
        
        System.Console.WriteLine("Sample Questions:");
        System.Console.WriteLine("  • Show me the items with highest values");
        System.Console.WriteLine("  • Find records where foreign key ID is 1 and show related data");
        System.Console.WriteLine("  • How many records match the criteria from both databases?");
        System.Console.WriteLine("  • What is the total of all numeric values?");
        System.Console.WriteLine();

        var query = _console.ReadLine($"Your question ({language}): ");
        if (string.IsNullOrWhiteSpace(query))
        {
            _console.WriteError("Empty query entered!");
            return;
        }

        try
        {
            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine("⏳ Analyzing databases and preparing query...");
            System.Console.ResetColor();

            var languageInstructedQuery = $"{query}\n\n[IMPORTANT: Respond in {language} language]";
            var response = await _multiDbCoordinator.QueryMultipleDatabasesAsync(languageInstructedQuery, maxResults: 10);

            System.Console.WriteLine();
            _console.WriteSuccess("ANSWER:");
            System.Console.WriteLine("─────────────────────────────────────────────────────────────────");
            System.Console.WriteLine(response.Answer);
            System.Console.WriteLine();

            if (response.Sources.Any())
            {
                System.Console.ForegroundColor = ConsoleColor.DarkGray;
                System.Console.WriteLine("📚 Sources:");
                foreach (var source in response.Sources)
                {
                    System.Console.WriteLine($"   • {source.FileName}");
                }
                System.Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during multi-database query");
            _console.WriteError($"Error: {ex.Message}");
        }
    }

    public async Task AnalyzeQueryIntentAsync(string language)
    {
        _console.WriteSectionHeader("🔬 Query Analysis (SQL Generation - Without Execution)");

        System.Console.ForegroundColor = ConsoleColor.DarkGray;
        System.Console.WriteLine($"Language: {language}");
        System.Console.ResetColor();
        System.Console.WriteLine();

        var query = _console.ReadLine($"Question to analyze ({language}): ");
        if (string.IsNullOrWhiteSpace(query))
        {
            _console.WriteError("Empty query entered!");
            return;
        }

        try
        {
            System.Console.WriteLine();
            System.Console.WriteLine("⏳ AI analyzing...");

            var languageInstructedQuery = $"{query}\n\n[IMPORTANT: Analyze and respond in {language} language]";

            var intent = await _multiDbCoordinator.AnalyzeQueryIntentAsync(languageInstructedQuery);
            intent = await _multiDbCoordinator.GenerateDatabaseQueriesAsync(intent);

            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine($"AI Understanding: {intent.QueryUnderstanding}");
            System.Console.WriteLine($"Confidence Level: {intent.Confidence:P0}");
            System.Console.ResetColor();

            if (!string.IsNullOrEmpty(intent.Reasoning))
            {
                System.Console.ForegroundColor = ConsoleColor.DarkGray;
                System.Console.WriteLine($"Reasoning: {intent.Reasoning}");
                System.Console.ResetColor();
            }

            System.Console.WriteLine();
            System.Console.WriteLine($"Databases to Query: {intent.DatabaseQueries.Count}");
            System.Console.WriteLine();

            foreach (var dbQuery in intent.DatabaseQueries)
            {
                _logger.LogDebug("Displaying SQL for {DatabaseName}: {SQL}", dbQuery.DatabaseName, dbQuery.GeneratedQuery);
                
                System.Console.ForegroundColor = ConsoleColor.Cyan;
                System.Console.WriteLine($"📊 {dbQuery.DatabaseName}");
                System.Console.ResetColor();
                System.Console.WriteLine($"   Tables: {string.Join(", ", dbQuery.RequiredTables)}");
                System.Console.WriteLine($"   Purpose: {dbQuery.Purpose}");
                System.Console.WriteLine($"   Priority: {dbQuery.Priority}");

                if (!string.IsNullOrEmpty(dbQuery.GeneratedQuery))
                {
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine($"\n   Generated SQL:");
                    System.Console.WriteLine($"   {dbQuery.GeneratedQuery.Replace("\n", "\n   ")}");
                    System.Console.ResetColor();
                }
                else
                {
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    System.Console.WriteLine($"\n   ⚠️ SQL generation failed or was skipped");
                    System.Console.ResetColor();
                }

                System.Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during query analysis");
            _console.WriteError($"Error: {ex.Message}");
        }
    }

    public async Task RunTestQueriesAsync(string language)
    {
        _console.WriteSectionHeader("🧪 Automatic Test Queries");

        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine($"🌍 Query Language: {language}");
        System.Console.ResetColor();
        System.Console.WriteLine();

        System.Console.WriteLine("📊 Analyzing database schemas to generate test queries...");
        var testQueries = await _testQueryGenerator.GenerateTestQueriesAsync(language);

        if (testQueries.Count == 0)
        {
            _console.WriteWarning("No test queries could be generated. Please ensure databases are connected.");
            return;
        }

        _console.WriteSuccess($"Generated {testQueries.Count} cross-database test queries");

        var categoryBreakdown = testQueries.GroupBy(q => q.Category.Split(' ')[0])
            .Select(g => $"{g.Key} ({g.Count()})");
        
        System.Console.ForegroundColor = ConsoleColor.DarkGray;
        System.Console.WriteLine($"  Categories: {string.Join(", ", categoryBreakdown)}");
        System.Console.ResetColor();
        System.Console.WriteLine();

        var testCount = GetTestCount(testQueries.Count);
        System.Console.WriteLine();

        var failedQueries = new List<(TestQuery Query, string Error, string GeneratedSQL)>();
        var successCount = 0;

        for (int i = 0; i < testCount; i++)
        {
            var testQuery = testQueries[i];

            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine($"[{i + 1}/{testCount}] {testQuery.Category}");
            System.Console.WriteLine($"  Query: {testQuery.Query}");
            System.Console.ForegroundColor = ConsoleColor.DarkCyan;
            System.Console.WriteLine($"  Databases: {testQuery.DatabaseName}");
            if (!string.IsNullOrEmpty(testQuery.DatabaseTypes))
            {
                System.Console.WriteLine($"  Types: {testQuery.DatabaseTypes}");
            }
            System.Console.ResetColor();

            try
            {
                var languageInstructedQuery = $"{testQuery.Query}\n\n[IMPORTANT: Respond in {language} language]";
                var response = await _multiDbCoordinator.QueryMultipleDatabasesAsync(languageInstructedQuery, maxResults: 5);

                if (IsErrorResponse(response.Answer))
                {
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    System.Console.WriteLine($"⚠️  Query Failed: {response.Answer}");
                    System.Console.ResetColor();

                    var schemas = await _schemaAnalyzer.GetAllSchemasAsync();
                    var sqlInfo = ExtractSQLFromError(response.Answer, schemas);
                    failedQueries.Add((testQuery, response.Answer, sqlInfo));
                }
                else
                {
                    _console.WriteSuccess($"Answer: {response.Answer}");
                    successCount++;
                }

                if (response.Sources.Any())
                {
                    System.Console.ForegroundColor = ConsoleColor.DarkGray;
                    System.Console.WriteLine($"  Source: {string.Join(", ", response.Sources.Select(s => s.FileName))}");
                    System.Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                _console.WriteError($"Exception: {ex.Message}");
                failedQueries.Add((testQuery, ex.Message, string.Empty));
            }

            System.Console.WriteLine();
            if (i < testCount - 1)
            {
                await Task.Delay(500);
            }
        }

        DisplayTestSummary(successCount, testCount, failedQueries);
    }


    public async Task RunConversationalChatAsync(string language, bool useLocalEnvironment, string aiProvider)
    {
        _console.WriteSectionHeader("💬 Conversational Assistant");

        System.Console.ForegroundColor = ConsoleColor.DarkGray;
        System.Console.WriteLine($"Language: {language}");
        System.Console.WriteLine($"Environment: {(useLocalEnvironment ? "LOCAL (Ollama)" : $"CLOUD ({aiProvider})")}");
        System.Console.ResetColor();
        System.Console.WriteLine();

        _console.WriteInfo("Type /new to reset the session, /exit to return to the main menu, /help to see available commands.");
        System.Console.WriteLine();

        while (true)
        {
            var userInput = _console.ReadLine("You: ");

            if (userInput == null)
            {
                _console.WriteWarning("Input stream closed. Ending conversation.");
                break;
            }

            var trimmedInput = userInput.Trim();

            if (string.IsNullOrWhiteSpace(trimmedInput))
            {
                continue;
            }

            if (IsExitCommand(trimmedInput))
            {
                _console.WriteInfo("👋 Conversation ended. Returning to main menu.");
                break;
            }

            if (IsHelpCommand(trimmedInput))
            {
                PrintChatHelp();
                continue;
            }

            try
            {
                var query = IsCommand(trimmedInput)
                    ? trimmedInput
                    : string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}\n\n[IMPORTANT: Respond in {1} language]",
                        userInput,
                        language);

                var response = await _documentSearchService.QueryIntelligenceAsync(query, maxResults: 8);

                System.Console.WriteLine();
                _console.WriteSuccess("Assistant:");
                System.Console.WriteLine("─────────────────────────────────────────────────────────────────");
                System.Console.WriteLine(response.Answer);
                System.Console.WriteLine();

                if (response.Sources != null && response.Sources.Any())
                {
                    PrintSources(response.Sources, maxCount: 5);
                    System.Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during conversational chat");
                _console.WriteError($"Error: {ex.Message}", ex);
            }
        }
    }

    #endregion

    #region Private Methods

    private int GetTestCount(int totalQueries)
    {
        var input = _console.ReadLine($"How many tests to run? (1-{totalQueries}, Enter for all): ");
        
        if (string.IsNullOrWhiteSpace(input))
        {
            return totalQueries;
        }

        if (int.TryParse(input, out var parsed) && parsed > 0 && parsed <= totalQueries)
        {
            return parsed;
        }

        return totalQueries;
    }

    private static bool IsErrorResponse(string answer)
    {
        return answer.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
               answer.Contains("error", StringComparison.OrdinalIgnoreCase) ||
               answer.Contains("SQLite Error", StringComparison.OrdinalIgnoreCase) ||
               answer.Contains("does not exist", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractSQLFromError(string errorMessage, List<SmartRAG.Models.DatabaseSchemaInfo> schemas)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return string.Empty;

        var sqlInfo = new StringBuilder();
        var issues = new List<string>();

        if (errorMessage.Contains("no such column", StringComparison.OrdinalIgnoreCase))
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                errorMessage,
                @"no such column:\s*'?([^'.\s]+(?:\.[^'.\s]+)?)'?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
            {
                issues.Add($"Missing column: {match.Groups[1].Value}");
            }
        }

        if (errorMessage.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                errorMessage,
                @"(Column|Table)\s+'?([^']+)'?\s+does not exist",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
            {
                issues.Add($"{match.Groups[1].Value} '{match.Groups[2].Value}' not found in schema");
            }
        }

        if (errorMessage.Contains("HAVING clause", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add("SQL Syntax: HAVING clause used incorrectly");
        }

        if (errorMessage.Contains("No query generated", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add("Validation failed after 3 retry attempts");
        }

        if (issues.Any())
        {
            foreach (var issue in issues)
            {
                sqlInfo.AppendLine($"• {issue}");
            }
        }
        else
        {
            sqlInfo.AppendLine("Error details not parsed. Check application logs.");
        }

        return sqlInfo.ToString().TrimEnd();
    }

    private void DisplayTestSummary(int successCount, int testCount, List<(TestQuery Query, string Error, string GeneratedSQL)> failedQueries)
    {
        System.Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        System.Console.WriteLine("📊 TEST SUMMARY");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        
        double successRate = (double)successCount / testCount * 100;
        
        _console.WriteSuccess($"Successful: {successCount}/{testCount}");

        if (failedQueries.Any())
        {
            _console.WriteError($"Failed: {failedQueries.Count}/{testCount}");
        }
        
        System.Console.WriteLine();
        System.Console.ForegroundColor = successRate >= 70 ? ConsoleColor.Green : 
                                         successRate >= 50 ? ConsoleColor.Yellow : 
                                         ConsoleColor.Red;
        System.Console.WriteLine($"📈 Success Rate: {successRate:F1}%");
        System.Console.ResetColor();
        
        if (failedQueries.Any())
        {
            System.Console.WriteLine();

            System.Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            System.Console.WriteLine("🔴 FAILED QUERIES (for analysis):");
            System.Console.WriteLine("═══════════════════════════════════════════════════════════════════");

            for (int i = 0; i < failedQueries.Count; i++)
            {
                var failed = failedQueries[i];
                System.Console.WriteLine();
                System.Console.ForegroundColor = ConsoleColor.Yellow;
                System.Console.WriteLine($"[FAIL #{i + 1}]");
                System.Console.ResetColor();
                System.Console.WriteLine($"Category: {failed.Query.Category}");
                System.Console.WriteLine($"Databases: {failed.Query.DatabaseName}");
                
                if (!string.IsNullOrEmpty(failed.Query.DatabaseTypes))
                {
                    System.Console.ForegroundColor = ConsoleColor.Magenta;
                    System.Console.WriteLine($"Database Types: {failed.Query.DatabaseTypes}");
                    System.Console.ResetColor();
                }

                System.Console.WriteLine();
                System.Console.ForegroundColor = ConsoleColor.Cyan;
                System.Console.WriteLine($"User Query:");
                System.Console.WriteLine($"  {failed.Query.Query}");
                System.Console.ResetColor();
                
                System.Console.WriteLine();
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine($"Error Details:");
                System.Console.WriteLine($"  {failed.Error}");
                System.Console.ResetColor();

                if (!string.IsNullOrEmpty(failed.GeneratedSQL))
                {
                    System.Console.WriteLine();
                    System.Console.ForegroundColor = ConsoleColor.DarkGray;
                    System.Console.WriteLine($"SQL Analysis:");
                    System.Console.WriteLine($"  {failed.GeneratedSQL}");
                    System.Console.ResetColor();
                }

                System.Console.WriteLine("─────────────────────────────────────────────────────────────────");
            }

            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine("💡 Copy the failed queries above to share for troubleshooting.");
            System.Console.ResetColor();
        }
        else
        {
            _console.WriteSuccess("🎉 All tests passed successfully!");
        }
    }

    private static bool IsCommand(string input)
    {
        return input.StartsWith("/", StringComparison.Ordinal);
    }

    private static bool IsExitCommand(string input)
    {
        var normalized = input.Trim().ToLowerInvariant();
        return normalized is "/exit" or "/quit" or "/q" or "/back";
    }

    private static bool IsHelpCommand(string input)
    {
        var normalized = input.Trim().ToLowerInvariant();
        return normalized is "/help" or "/h" or "/?";
    }

    private void PrintChatHelp()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("Available commands:");
        System.Console.WriteLine("  • /new, /reset, /clear — start a new conversation session");
        System.Console.WriteLine("  • /help — show this help message");
        System.Console.WriteLine("  • /exit, /quit, /back — return to the main menu");
        System.Console.WriteLine();
        System.Console.WriteLine("Any other text will be processed using SmartRAG's conversational intelligence.");
        System.Console.WriteLine();
    }

    private void PrintSources(IReadOnlyCollection<SearchSource> sources, int maxCount)
    {
        System.Console.ForegroundColor = ConsoleColor.DarkGray;
        System.Console.WriteLine("📚 Sources:");

        var displayed = 0;

        foreach (var source in sources)
        {
            if (displayed >= maxCount)
            {
                break;
            }

            System.Console.WriteLine($"   • {BuildSourceLine(source)}");
            displayed++;
        }

        if (sources.Count > maxCount)
        {
            System.Console.WriteLine($"   • … {sources.Count - maxCount} more");
        }

        System.Console.ResetColor();
    }

    private static string BuildSourceLine(SearchSource source)
    {
        var label = string.IsNullOrWhiteSpace(source.SourceType) ? "Document" : source.SourceType;
        var name = !string.IsNullOrWhiteSpace(source.FileName)
            ? source.FileName
            : !string.IsNullOrWhiteSpace(source.DatabaseName)
                ? source.DatabaseName
                : label;

        var details = new List<string>();

        if (source.ChunkIndex.HasValue)
        {
            details.Add($"chunk {source.ChunkIndex.Value + 1}");
        }

        if (source.RowNumber.HasValue)
        {
            details.Add($"row {source.RowNumber.Value}");
        }

        if (source.StartPosition.HasValue && source.EndPosition.HasValue)
        {
            details.Add($"chars {source.StartPosition}-{source.EndPosition}");
        }

        var timeRange = FormatTimeRange(source.StartTimeSeconds, source.EndTimeSeconds);
        if (!string.IsNullOrEmpty(timeRange))
        {
            details.Add($"audio {timeRange}");
        }

        if (!string.IsNullOrWhiteSpace(source.Location))
        {
            details.Add(source.Location);
        }

        if (source.Tables != null && source.Tables.Any())
        {
            var tablePreview = source.Tables.Take(3).ToList();
            var tableSuffix = source.Tables.Count > tablePreview.Count ? ", …" : string.Empty;
            details.Add($"tables: {string.Join(", ", tablePreview)}{tableSuffix}");
        }

        var detailText = details.Count > 0 ? $" ({string.Join(" | ", details)})" : string.Empty;

        return $"[{label}] {name}{detailText}";
    }

    private static string FormatTimeRange(double? startSeconds, double? endSeconds)
    {
        if (!startSeconds.HasValue && !endSeconds.HasValue)
        {
            return string.Empty;
        }

        if (startSeconds.HasValue && endSeconds.HasValue)
        {
            return $"{FormatTimestamp(startSeconds.Value)}-{FormatTimestamp(endSeconds.Value)}";
        }

        if (startSeconds.HasValue)
        {
            return $"{FormatTimestamp(startSeconds.Value)}-";
        }

        return $"-{FormatTimestamp(endSeconds!.Value)}";
    }

    private static string FormatTimestamp(double seconds)
    {
        if (seconds < 0)
        {
            seconds = 0;
        }

        var time = TimeSpan.FromSeconds(seconds);

        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}"
            : $"{time.Minutes:D2}:{time.Seconds:D2}";
    }

    private async Task<string> GenerateCombinedAnswer(string query, List<string> documentContext, string? databaseAnswer, string language)
    {
        var combinedContext = new List<string>();

        if (documentContext.Any())
        {
            combinedContext.Add("=== DOCUMENT INFORMATION ===");
            combinedContext.AddRange(documentContext);
        }

        if (!string.IsNullOrEmpty(databaseAnswer))
        {
            combinedContext.Add("=== DATABASE INFORMATION ===");
            combinedContext.Add(databaseAnswer);
        }

        var finalPrompt = $@"User Question: {query}

Available Information:
{string.Join("\n\n", combinedContext)}

Instructions:
- Analyze both document and database information
- Provide a comprehensive answer combining insights from both sources
- If information conflicts, mention both perspectives
- Respond in {language} language
- Be clear about which source provided which information";

        return await _aiService.GenerateResponseAsync(finalPrompt, new List<string>());
    }

    #endregion
}

