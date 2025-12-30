using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartRAG.Demo.Models;
using SmartRAG.Demo.Services.Console;
using SmartRAG.Demo.Services.TestQuery;
using SmartRAG.Interfaces.Mcp;
using SmartRAG.Models;
using System.Text;

namespace SmartRAG.Demo.Handlers.QueryHandlers;

/// <summary>
/// Handler for query-related operations
/// </summary>
public class QueryHandler(
    ILogger<QueryHandler> logger,
    IConsoleService console,
    IMultiDatabaseQueryCoordinator? multiDbCoordinator,
    IAIService aiService,
    IDocumentService documentService,
    IDocumentSearchService documentSearchService,
    IDatabaseSchemaAnalyzer? schemaAnalyzer,
    ITestQueryGenerator testQueryGenerator,
    IConversationManagerService conversationManager,
    IServiceProvider serviceProvider) : IQueryHandler
{
    #region Fields

    private readonly ILogger<QueryHandler> _logger = logger;
    private readonly IConsoleService _console = console;
    private readonly IMultiDatabaseQueryCoordinator? _multiDbCoordinator = multiDbCoordinator;
    private readonly IAIService _aiService = aiService;
    private readonly IDocumentService _documentService = documentService;
    private readonly IDocumentSearchService _documentSearchService = documentSearchService;
    private readonly IDatabaseSchemaAnalyzer? _schemaAnalyzer = schemaAnalyzer;
    private readonly ITestQueryGenerator _testQueryGenerator = testQueryGenerator;
    private readonly IConversationManagerService _conversationManager = conversationManager;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    #endregion

    #region Public Methods

    public async Task RunMultiDatabaseQueryAsync(string language, CancellationToken cancellationToken = default)
    {
        _console.WriteSectionHeader("🤖 Multi-Database Smart Query");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Language: {language}");
        Console.ResetColor();
        Console.WriteLine();
        
        Console.WriteLine("Sample Questions:");
        Console.WriteLine("  • Show me the items with highest values");
        Console.WriteLine("  • Find records where foreign key ID is 1 and show related data");
        Console.WriteLine("  • How many records match the criteria from both databases?");
        Console.WriteLine("  • What is the total of all numeric values?");
        Console.WriteLine();

        var query = _console.ReadLine($"Your question ({language}): ");
        if (string.IsNullOrWhiteSpace(query))
        {
            _console.WriteError("Empty query entered!");
            return;
        }

        try
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("⏳ Analyzing databases and preparing query...");
            Console.ResetColor();

            var languageInstructedQuery = $"{query}\n\n[IMPORTANT: Respond in {language} language]";
            var response = await _multiDbCoordinator!.QueryMultipleDatabasesAsync(languageInstructedQuery, maxResults: 10, cancellationToken);

            Console.WriteLine();
            _console.WriteSuccess("ANSWER:");
            Console.WriteLine("─────────────────────────────────────────────────────────────────");
            Console.WriteLine(response.Answer);
            Console.WriteLine();

            if (response.Sources.Any())
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("📚 Sources:");
                foreach (var source in response.Sources)
                {
                    Console.WriteLine($"   • {source.FileName}");
                }
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during multi-database query");
            _console.WriteError($"Error: {ex.Message}");
        }
    }

    public async Task AnalyzeQueryIntentAsync(string language, CancellationToken cancellationToken = default)
    {
        _console.WriteSectionHeader("🔬 Query Analysis (SQL Generation - Without Execution)");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Language: {language}");
        Console.ResetColor();
        Console.WriteLine();

        var query = _console.ReadLine($"Question to analyze ({language}): ");
        if (string.IsNullOrWhiteSpace(query))
        {
            _console.WriteError("Empty query entered!");
            return;
        }

        try
        {
            Console.WriteLine();
            Console.WriteLine("⏳ AI analyzing...");

            var languageInstructedQuery = $"{query}\n\n[IMPORTANT: Analyze and respond in {language} language]";

            // Use IQueryIntentAnalyzer instead of deprecated method
            var queryIntentAnalyzer = _serviceProvider.GetRequiredService<IQueryIntentAnalyzer>();
            var intent = await queryIntentAnalyzer.AnalyzeQueryIntentAsync(languageInstructedQuery, cancellationToken);
            intent = await _multiDbCoordinator!.GenerateDatabaseQueriesAsync(intent, cancellationToken);

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"AI Understanding: {intent.QueryUnderstanding}");
            Console.WriteLine($"Confidence Level: {intent.Confidence:P0}");
            Console.ResetColor();

            if (!string.IsNullOrEmpty(intent.Reasoning))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"Reasoning: {intent.Reasoning}");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.WriteLine($"Databases to Query: {intent.DatabaseQueries.Count}");
            Console.WriteLine();

            foreach (var dbQuery in intent.DatabaseQueries)
            {
                _logger.LogDebug("Displaying SQL for {DatabaseName}: {SQL}", dbQuery.DatabaseName, dbQuery.GeneratedQuery);
                
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"📊 {dbQuery.DatabaseName}");
                Console.ResetColor();
                Console.WriteLine($"   Tables: {string.Join(", ", dbQuery.RequiredTables)}");
                Console.WriteLine($"   Purpose: {dbQuery.Purpose}");
                Console.WriteLine($"   Priority: {dbQuery.Priority}");

                if (!string.IsNullOrEmpty(dbQuery.GeneratedQuery))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n   Generated SQL:");
                    Console.WriteLine($"   {dbQuery.GeneratedQuery.Replace("\n", "\n   ")}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n   ⚠️ SQL generation failed or was skipped");
                    Console.ResetColor();
                }

                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during query analysis");
            _console.WriteError($"Error: {ex.Message}");
        }
    }

    public async Task RunTestQueriesAsync(string language, CancellationToken cancellationToken = default)
    {
        _console.WriteSectionHeader("🧪 Automatic Test Queries");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"🌍 Query Language: {language}");
        Console.ResetColor();
        Console.WriteLine();

        Console.WriteLine("📊 Analyzing database schemas to generate test queries...");
        var testQueries = await _testQueryGenerator.GenerateTestQueriesAsync(language);

        if (testQueries.Count == 0)
        {
            _console.WriteWarning("No test queries could be generated. Please ensure databases are connected.");
            return;
        }

        _console.WriteSuccess($"Generated {testQueries.Count} cross-database test queries");

        var categoryBreakdown = testQueries.GroupBy(q => q.Category.Split(' ')[0])
            .Select(g => $"{g.Key} ({g.Count()})");
        
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  Categories: {string.Join(", ", categoryBreakdown)}");
        Console.ResetColor();
        Console.WriteLine();

        var testCount = GetTestCount(testQueries.Count);
        Console.WriteLine();

        var failedQueries = new List<(TestQuery Query, string Error, string GeneratedSQL)>();
        var successCount = 0;

        for (int i = 0; i < testCount; i++)
        {
            var testQuery = testQueries[i];

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{i + 1}/{testCount}] {testQuery.Category}");
            Console.WriteLine($"  Query: {testQuery.Query}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"  Databases: {testQuery.DatabaseName}");
            if (!string.IsNullOrEmpty(testQuery.DatabaseTypes))
            {
                Console.WriteLine($"  Types: {testQuery.DatabaseTypes}");
            }
            Console.ResetColor();

            try
            {
                var languageInstructedQuery = $"{testQuery.Query}\n\n[IMPORTANT: Respond in {language} language]";
                var response = await _multiDbCoordinator!.QueryMultipleDatabasesAsync(languageInstructedQuery, maxResults: 5, cancellationToken);

                if (IsErrorResponse(response.Answer))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⚠️  Query Failed: {response.Answer}");
                    Console.ResetColor();

                    var schemas = _schemaAnalyzer != null ? await _schemaAnalyzer.GetAllSchemasAsync(cancellationToken) : new List<DatabaseSchemaInfo>();
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
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"  Source: {string.Join(", ", response.Sources.Select(s => s.FileName))}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                _console.WriteError($"Exception: {ex.Message}");
                failedQueries.Add((testQuery, ex.Message, string.Empty));
            }

            Console.WriteLine();
            if (i < testCount - 1)
            {
                await Task.Delay(500, cancellationToken);
            }
        }

        DisplayTestSummary(successCount, testCount, failedQueries);
    }


    public async Task RunConversationalChatAsync(string language, bool useLocalEnvironment, string aiProvider, CancellationToken cancellationToken = default)
    {
        _console.WriteSectionHeader("💬 Conversational Assistant");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Language: {language}");
        Console.WriteLine($"Environment: {(useLocalEnvironment ? "LOCAL (Ollama)" : $"CLOUD ({aiProvider})")}");
        Console.ResetColor();
        Console.WriteLine();

        _console.WriteInfo("SmartRAG commands: /new, /reset, /clear (start new session).");
        _console.WriteInfo("Demo commands: /exit, /quit, /q, /back (return to menu), /help (show help).");

        Console.WriteLine();

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
                var response = await _documentSearchService.QueryIntelligenceAsync(trimmedInput, maxResults: 8, startNewConversation: false, CancellationToken.None);

                Console.WriteLine();
                _console.WriteSuccess("Assistant:");
                Console.WriteLine("─────────────────────────────────────────────────────────────────");
                Console.WriteLine(response.Answer);
                Console.WriteLine();

                if (response.Sources != null && response.Sources.Any())
                {
                    PrintSources(response.Sources, maxCount: 5);
                    Console.WriteLine();
                }

                if (response.SearchMetadata != null)
                {
                    PrintSearchMetadata(response.SearchMetadata);
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during conversational chat");
                _console.WriteError($"Error: {ex.Message}", ex);
            }
        }
    }

    public async Task RunMcpQueryAsync(string language, CancellationToken cancellationToken = default)
    {
        _console.WriteSectionHeader("🔌 MCP Integration Test");

        var mcpIntegration = _serviceProvider.GetService<IMcpIntegrationService>();

        if (mcpIntegration == null)
        {
            _console.WriteWarning("MCP integration service is not available. Ensure Features.EnableMcpSearch is true in configuration.");
            return;
        }

        try
        {
            Console.WriteLine("Listing available MCP tools from connected servers...");
            Console.WriteLine();

            var tools = await mcpIntegration.GetAvailableToolsAsync();

            if (tools == null || tools.Count == 0)
            {
                _console.WriteWarning("No MCP tools found. Check MCP server configuration and connectivity.");
                return;
            }

            var grouped = tools
                .GroupBy(t => string.IsNullOrWhiteSpace(t.ServerId) ? "Unknown" : t.ServerId)
                .ToList();

            foreach (var group in grouped)
            {
                _console.WriteInfo($"Server: {group.Key}");
                foreach (var tool in group)
                {
                    Console.WriteLine($"  • {tool.Name} - {tool.Description}");
                }
                Console.WriteLine();
            }

            var serverId = _console.ReadLine("ServerId (exact as listed above): ");
            var toolName = _console.ReadLine("Tool name: ");

            if (string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(toolName))
            {
                _console.WriteError("ServerId and tool name are required.");
                return;
            }

            var query = _console.ReadLine($"Natural language query for MCP tool ({language}): ");
            if (string.IsNullOrWhiteSpace(query))
            {
                _console.WriteError("Empty query entered.");
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                { "query", query },
                { "language", language }
            };

            _console.WriteInfo("Calling MCP tool...");

            var result = await mcpIntegration.CallToolAsync(serverId, toolName, parameters);

            if (!result.IsSuccess)
            {
                _console.WriteError($"MCP tool call failed: {result.ErrorMessage}");
                return;
            }

            _console.WriteSuccess("MCP Tool Result:");
            Console.WriteLine("─────────────────────────────────────────────────────────────────");
            Console.WriteLine(result.Content);
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MCP query");
            _console.WriteError($"Error: {ex.Message}", ex);
        }
    }


    #endregion

    #region Private Methods

    /// <summary>
    /// Gets the number of tests to run from user input
    /// </summary>
    /// <param name="totalQueries">Total number of available test queries</param>
    /// <returns>Number of tests to run</returns>
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

    /// <summary>
    /// Checks if the response indicates an error
    /// </summary>
    /// <param name="answer">Response answer to check</param>
    /// <returns>True if error detected, false otherwise</returns>
    private static bool IsErrorResponse(string answer)
    {
        return answer.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
               answer.Contains("error", StringComparison.OrdinalIgnoreCase) ||
               answer.Contains("SQLite Error", StringComparison.OrdinalIgnoreCase) ||
               answer.Contains("does not exist", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts SQL-related error information from error message
    /// </summary>
    /// <param name="errorMessage">Error message to parse</param>
    /// <param name="schemas">Database schemas for context</param>
    /// <returns>Formatted error information</returns>
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

    /// <summary>
    /// Displays test execution summary with success/failure statistics
    /// </summary>
    /// <param name="successCount">Number of successful tests</param>
    /// <param name="testCount">Total number of tests</param>
    /// <param name="failedQueries">List of failed queries with error details</param>
    private void DisplayTestSummary(int successCount, int testCount, List<(TestQuery Query, string Error, string GeneratedSQL)> failedQueries)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine("📊 TEST SUMMARY");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        
        double successRate = (double)successCount / testCount * 100;
        
        _console.WriteSuccess($"Successful: {successCount}/{testCount}");

        if (failedQueries.Any())
        {
            _console.WriteError($"Failed: {failedQueries.Count}/{testCount}");
        }
        
        Console.WriteLine();
        Console.ForegroundColor = successRate >= 70 ? ConsoleColor.Green : 
                                         successRate >= 50 ? ConsoleColor.Yellow : 
                                         ConsoleColor.Red;
        Console.WriteLine($"📈 Success Rate: {successRate:F1}%");
        Console.ResetColor();
        
        if (failedQueries.Any())
        {
            Console.WriteLine();

            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("🔴 FAILED QUERIES (for analysis):");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");

            for (int i = 0; i < failedQueries.Count; i++)
            {
                var failed = failedQueries[i];
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[FAIL #{i + 1}]");
                Console.ResetColor();
                Console.WriteLine($"Category: {failed.Query.Category}");
                Console.WriteLine($"Databases: {failed.Query.DatabaseName}");
                
                if (!string.IsNullOrEmpty(failed.Query.DatabaseTypes))
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"Database Types: {failed.Query.DatabaseTypes}");
                    Console.ResetColor();
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"User Query:");
                Console.WriteLine($"  {failed.Query.Query}");
                Console.ResetColor();
                
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error Details:");
                Console.WriteLine($"  {failed.Error}");
                Console.ResetColor();

                if (!string.IsNullOrEmpty(failed.GeneratedSQL))
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"SQL Analysis:");
                    Console.WriteLine($"  {failed.GeneratedSQL}");
                    Console.ResetColor();
                }

                Console.WriteLine("─────────────────────────────────────────────────────────────────");
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("💡 Copy the failed queries above to share for troubleshooting.");
            Console.ResetColor();
        }
        else
        {
            _console.WriteSuccess("🎉 All tests passed successfully!");
        }
    }

    /// <summary>
    /// Checks if input is an exit command
    /// </summary>
    /// <param name="input">Input string to check</param>
    /// <returns>True if exit command, false otherwise</returns>
    private static bool IsExitCommand(string input)
    {
        var normalized = input.Trim().ToLowerInvariant();
        return normalized is "/exit" or "/quit" or "/q" or "/back";
    }

    /// <summary>
    /// Checks if input is a help command
    /// </summary>
    /// <param name="input">Input string to check</param>
    /// <returns>True if help command, false otherwise</returns>
    private static bool IsHelpCommand(string input)
    {
        var normalized = input.Trim().ToLowerInvariant();
        return normalized is "/help" or "/h" or "/?";
    }

    /// <summary>
    /// Prints available chat commands help message
    /// </summary>
    private void PrintChatHelp()
    {
        Console.WriteLine();
        Console.WriteLine("Available commands:");
        Console.WriteLine();
        Console.WriteLine("SmartRAG Library Commands (handled by SmartRAG):");
        Console.WriteLine("  • /new, /reset, /clear — start a new conversation session");
        Console.WriteLine("  • /chat, /talk, /conversation — force conversation mode");
        Console.WriteLine();
        Console.WriteLine("Demo Application Commands (handled by this demo):");
        Console.WriteLine("  • /help, /h, /? — show this help message");
        Console.WriteLine("  • /exit, /quit, /q, /back — return to the main menu");
        Console.WriteLine();
        Console.WriteLine("Any other text will be processed using SmartRAG's conversational intelligence.");
        Console.WriteLine();
    }

    /// <summary>
    /// Prints search result sources with formatting
    /// </summary>
    /// <param name="sources">Collection of search sources</param>
    /// <param name="maxCount">Maximum number of sources to display (unused - all sources are displayed)</param>
    private void PrintSources(IReadOnlyCollection<SearchSource> sources, int maxCount)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("📚 Sources:");

        foreach (var source in sources)
        {
            Console.WriteLine($"   • {BuildSourceLine(source)}");
        }

        Console.ResetColor();
    }

    /// <summary>
    /// Prints search metadata information about which searches were performed
    /// </summary>
    /// <param name="metadata">Search metadata to display</param>
    private void PrintSearchMetadata(SearchMetadata metadata)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("🔍 Search Operations:");

        var searches = new List<string>();

        if (metadata.DocumentSearchPerformed)
        {
            var docInfo = metadata.DocumentChunksFound > 0
                ? $"Document Search ({metadata.DocumentChunksFound} chunks found)"
                : "Document Search";
            searches.Add($"   ✓ {docInfo}");
        }

        if (metadata.DatabaseSearchPerformed)
        {
            var dbInfo = metadata.DatabaseResultsFound > 0
                ? $"Database Search ({metadata.DatabaseResultsFound} results found)"
                : "Database Search";
            searches.Add($"   ✓ {dbInfo}");
        }

        if (metadata.McpSearchPerformed)
        {
            var mcpInfo = metadata.McpResultsFound > 0
                ? $"MCP Search ({metadata.McpResultsFound} results found)"
                : "MCP Search";
            searches.Add($"   ✓ {mcpInfo}");
        }

        if (searches.Count == 0)
        {
            searches.Add("   (No searches performed)");
        }

        foreach (var search in searches)
        {
            Console.WriteLine(search);
        }

        Console.ResetColor();
    }

    /// <summary>
    /// Builds a formatted source line string from search source
    /// </summary>
    /// <param name="source">Search source to format</param>
    /// <returns>Formatted source line</returns>
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

    /// <summary>
    /// Formats time range from start and end seconds
    /// </summary>
    /// <param name="startSeconds">Start time in seconds</param>
    /// <param name="endSeconds">End time in seconds</param>
    /// <returns>Formatted time range string</returns>
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

    /// <summary>
    /// Formats seconds into timestamp string (HH:MM:SS or MM:SS)
    /// </summary>
    /// <param name="seconds">Time in seconds</param>
    /// <returns>Formatted timestamp</returns>
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

    /// <summary>
    /// Generates combined answer from document and database contexts
    /// </summary>
    /// <param name="query">User query</param>
    /// <param name="documentContext">Document context information</param>
    /// <param name="databaseAnswer">Database query answer</param>
    /// <param name="language">Response language</param>
    /// <returns>Combined answer from AI</returns>
    private async Task<string> GenerateCombinedAnswer(string query, List<string> documentContext, string? databaseAnswer, string language, CancellationToken cancellationToken = default)
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

        return await _aiService.GenerateResponseAsync(finalPrompt, new List<string>(), CancellationToken.None);
    }

    public async Task ClearConversationHistoryAsync(CancellationToken cancellationToken = default)
    {
        _console.WriteSectionHeader("🧹 Clear Conversation History");

        _console.WriteWarning("WARNING: This will permanently delete ALL conversation history!");
        Console.WriteLine();

        var confirmation = _console.ReadLine("Are you sure? Type 'yes' to confirm: ");
        if (confirmation?.ToLower() != "yes")
        {
            _console.WriteInfo("Operation cancelled");
            return;
        }

        try
        {
            Console.WriteLine();
            Console.WriteLine("🧹 Clearing conversation history...");

            await _conversationManager.ClearAllConversationsAsync(cancellationToken);

            _console.WriteSuccess("Conversation history cleared successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing conversation history");
            _console.WriteError($"Error: {ex.Message}");
        }
    }

    #endregion
}

