using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Database;
using SmartRAG.Interfaces.Database.Strategies;
using SmartRAG.Models;
using SmartRAG.Services.Database.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Services.Database
{
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
            
            _logger.LogInformation("Executing {Count} database queries in priority order: {Order}",
                queryIntent.DatabaseQueries.Count,
                string.Join(" → ", queryIntent.DatabaseQueries.Select(q => $"{q.DatabaseName}(priority:{q.Priority})")));
            
            var schemas = new Dictionary<string, DatabaseSchemaInfo>();

            var strategies = new Dictionary<string, ISqlDialectStrategy>();
            var requiredMappingColumns = new Dictionary<string, List<string>>();

            foreach (var dbQuery in queryIntent.DatabaseQueries)
            {
                var schema = await _schemaAnalyzer.GetSchemaAsync(dbQuery.DatabaseId, cancellationToken);
                if (schema == null)
                {
                    _logger.LogWarning("Schema not found for database {DatabaseId}", dbQuery.DatabaseId);
                    dbQuery.GeneratedQuery = null;
                    continue;
                }

                schemas[dbQuery.DatabaseId] = schema;
                strategies[dbQuery.DatabaseId] = _strategyFactory.GetStrategy(schema.DatabaseType);
                requiredMappingColumns[dbQuery.DatabaseId] = await GetRequiredMappingColumnsAsync(schema.DatabaseName, dbQuery.RequiredTables);

                if (requiredMappingColumns[dbQuery.DatabaseId].Any())
                {
                    _logger.LogInformation("Database {DatabaseName} requires mapping columns: {Columns}",
                        schema.DatabaseName, string.Join(", ", requiredMappingColumns[dbQuery.DatabaseId]));
                }
            }

            if (schemas.Count == 0)
            {
                _logger.LogError("No valid schemas found for any database");
                return queryIntent;
            }

            var promptParts = _promptBuilder.BuildMultiDatabaseSeparated(queryIntent.OriginalQuery, queryIntent, schemas, strategies);
            
            var additionalInstructions = new StringBuilder();
            foreach (var kvp in requiredMappingColumns.Where(k => k.Value.Any()))
            {
                additionalInstructions.AppendLine($"\n🚨 CRITICAL FOR {schemas[kvp.Key].DatabaseName}:");
                additionalInstructions.AppendLine("MAPPING COLUMNS REQUIRED - MUST include in SELECT:");
                foreach (var col in kvp.Value)
                {
                    additionalInstructions.AppendLine($"  • {col}");
                }
            }
            
            if (additionalInstructions.Length > 0)
            {
                promptParts.UserMessage += "\n" + additionalInstructions.ToString();
            }

            _logger.LogDebug("Sending separated multi-database prompt to AI for {DatabaseCount} databases (schema as context)", queryIntent.DatabaseQueries.Count);
            _logger.LogTrace("Context (schema) length: {Length} characters", promptParts.SystemMessage?.Length ?? 0);
            _logger.LogTrace("Query (rules) length: {Length} characters", promptParts.UserMessage?.Length ?? 0);
            
            var context = new List<string> { promptParts.SystemMessage };
            var aiResponse = await _aiService.GenerateResponseAsync(promptParts.UserMessage, context, cancellationToken);
            _logger.LogDebug("AI response length: {Length} characters", aiResponse?.Length ?? 0);
            
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Full AI response: {Response}", aiResponse);
            }
            
            var databaseSqls = ExtractMultiDatabaseSQL(aiResponse, queryIntent.DatabaseQueries, schemas);
            _logger.LogDebug("Extracted SQL for {ExtractedCount} out of {TotalCount} databases", databaseSqls.Count, queryIntent.DatabaseQueries.Count);

            foreach (var dbQuery in queryIntent.DatabaseQueries)
            {
                if (!databaseSqls.TryGetValue(dbQuery.DatabaseId, out var extractedSql) || string.IsNullOrEmpty(extractedSql))
                {
                    _logger.LogError("Failed to extract SQL for database {DatabaseId}", dbQuery.DatabaseId);
                    dbQuery.GeneratedQuery = null;
                    continue;
                }

                var schema = schemas[dbQuery.DatabaseId];
                var strategy = strategies[dbQuery.DatabaseId];
                
                extractedSql = strategy.FormatSql(extractedSql);
                
                _logger.LogDebug("AI generated SQL for database {DatabaseName}: {Sql}", schema.DatabaseName, extractedSql);

                var allDatabaseNames = schemas.Values.Select(s => s.DatabaseName).Distinct().ToList();
                if (!ValidateSql(extractedSql, schema, dbQuery.RequiredTables, strategy, allDatabaseNames, out var validationErrors))
                {
                    _logger.LogError("AI generated invalid SQL for database {DatabaseName}. SQL: {Sql}. Errors: {Errors}",
                        schema.DatabaseName, extractedSql, string.Join("; ", validationErrors));
                    dbQuery.GeneratedQuery = null;
                    continue;
                }

                var mappingErrors = await ValidateCrossDatabaseMappingColumnsAsync(extractedSql, schema.DatabaseName, dbQuery.RequiredTables);
                if (mappingErrors.Any())
                {
                    _logger.LogWarning("Generated SQL missing required mapping columns for database {DatabaseName}. SQL: {Sql}. Missing: {Missing}",
                        schema.DatabaseName, extractedSql, string.Join(", ", mappingErrors));
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
                _logger.LogWarning("AI response is empty, cannot extract SQL");
                return result;
            }

            response = Regex.Replace(response, @"```sql\s*", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            response = Regex.Replace(response, @"```\s*", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string currentDatabase = null;
            var currentSql = new List<string>();
            bool inSql = false;
            bool waitingForConfirmed = true;

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
                                _logger.LogDebug("Extracted SQL for database {DatabaseId}: {SqlLength} chars", currentDatabase, sql.Length);
                            }
                        }
                        
                        var dbIndex = int.Parse(dbHeaderMatch.Groups[1].Value) - 1;
                        if (dbIndex >= 0 && dbIndex < databaseQueries.Count)
                        {
                            currentDatabase = databaseQueries[dbIndex].DatabaseId;
                            currentSql.Clear();
                            inSql = false;
                            waitingForConfirmed = true;
                            _logger.LogDebug("Found database header: {DatabaseId}", currentDatabase);
                        }
                        else
                        {
                            currentDatabase = null;
                            currentSql.Clear();
                            inSql = false;
                            waitingForConfirmed = true;
                            _logger.LogDebug("Skipping database header at index {DbIndex} (out of range, expected databases: {ExpectedCount})", dbIndex, databaseQueries.Count);
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
                            _logger.LogDebug("Extracted SQL for database {DatabaseId}: {SqlLength} chars", currentDatabase, sql.Length);
                        }
                    }
                    
                    var dbIndex = int.Parse(dbMatch.Groups[1].Value) - 1;
                    if (dbIndex >= 0 && dbIndex < databaseQueries.Count)
                    {
                        currentDatabase = databaseQueries[dbIndex].DatabaseId;
                        currentSql.Clear();
                        inSql = false;
                        waitingForConfirmed = true;
                        _logger.LogDebug("Found database marker: {DatabaseId}", currentDatabase);
                    }
                    else
                    {
                        currentDatabase = null;
                        currentSql.Clear();
                        inSql = false;
                        waitingForConfirmed = true;
                        _logger.LogDebug("Skipping database marker at index {DbIndex} (out of range, expected databases: {ExpectedCount})", dbIndex, databaseQueries.Count);
                    }
                    continue;
                }

                if (trimmedLine.Equals("CONFIRMED", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentDatabase != null)
                    {
                        inSql = true;
                        waitingForConfirmed = false;
                        _logger.LogDebug("Found CONFIRMED marker for database {DatabaseId}", currentDatabase);
                    }
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

                if (currentDatabase != null && (inSql || !waitingForConfirmed))
                {
                    if (trimmedLine.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
                    {
                        inSql = true;
                        waitingForConfirmed = false;
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
                            char.IsDigit(trimmedLine[0]))
                        {
                            currentSql.Add(trimmedLine);
                        }
                        else if (trimmedLine.Length > 50 && !lowerLine.Contains("database") && !lowerLine.Contains("explanation"))
                        {
                            currentSql.Add(trimmedLine);
                        }
                        else
                        {
                            var sql = ExtractCompleteSQL(string.Join(" ", currentSql));
                            if (!string.IsNullOrWhiteSpace(sql) && sql.Length > 20)
                            {
                                result[currentDatabase] = sql;
                                _logger.LogDebug("Completed SQL extraction for database {DatabaseId}", currentDatabase);
                            }
                            inSql = false;
                        }
                    }
                }
            }

            if (currentDatabase != null && currentSql.Any())
            {
                var sql = ExtractCompleteSQL(string.Join(" ", currentSql));
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    result[currentDatabase] = sql;
                    _logger.LogDebug("Final SQL extraction for database {DatabaseId}: {SqlLength} chars", currentDatabase, sql.Length);
                }
            }

            if (result.Count == 0)
            {
                _logger.LogWarning("Failed to extract any SQL from AI response. Response preview: {Preview}", 
                    response?.Substring(0, Math.Min(500, response?.Length ?? 0)) ?? "null");
            }

            return result;
        }

        private async Task<List<string>> GetRequiredMappingColumnsAsync(string databaseName, List<string> requiredTables)
        {
            var requiredColumns = new List<string>();
            
            try
            {
                var connections = await _connectionManager.GetAllConnectionsAsync();
                var connection = connections.FirstOrDefault(c =>
                    (c.Name ?? string.Empty).Equals(databaseName, StringComparison.OrdinalIgnoreCase));
                
                if (connection?.CrossDatabaseMappings == null)
                    return requiredColumns;

                foreach (var mapping in connection.CrossDatabaseMappings)
                {
                    if (!mapping.SourceDatabase.Equals(databaseName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (requiredTables != null && requiredTables.Count > 0)
                    {
                        var tableName = mapping.SourceTable;
                        if (tableName.Contains('.'))
                        {
                            var parts = tableName.Split('.');
                            tableName = parts.Length > 1 ? parts[1] : parts[0];
                        }
                        
                        if (!requiredTables.Any(t => t.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                                                     t.Equals(mapping.SourceTable, StringComparison.OrdinalIgnoreCase) ||
                                                     mapping.SourceTable.Contains(t, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }
                    }

                    if (!requiredColumns.Contains($"{mapping.SourceTable}.{mapping.SourceColumn}", StringComparer.OrdinalIgnoreCase))
                    {
                        requiredColumns.Add($"{mapping.SourceTable}.{mapping.SourceColumn}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting required mapping columns for database {DatabaseName}", databaseName);
            }

            return requiredColumns;
        }


        private async Task<List<string>> ValidateCrossDatabaseMappingColumnsAsync(string sql, string databaseName, List<string> requiredTables)
        {
            var missingColumns = new List<string>();
            
            try
            {
                var connections = await _connectionManager.GetAllConnectionsAsync();
                var connection = connections.FirstOrDefault(c =>
                    (c.Name ?? string.Empty).Equals(databaseName, StringComparison.OrdinalIgnoreCase));
                
                if (connection?.CrossDatabaseMappings == null)
                    return missingColumns;

                var sqlUpper = sql.ToUpperInvariant();
                
                foreach (var mapping in connection.CrossDatabaseMappings)
                {
                    if (!mapping.SourceDatabase.Equals(databaseName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (requiredTables != null && requiredTables.Count > 0)
                    {
                        var tableName = mapping.SourceTable;
                        if (tableName.Contains('.'))
                        {
                            var parts = tableName.Split('.');
                            tableName = parts.Length > 1 ? parts[1] : parts[0];
                        }
                        
                        if (!requiredTables.Any(t => t.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                                                     t.Equals(mapping.SourceTable, StringComparison.OrdinalIgnoreCase) ||
                                                     mapping.SourceTable.Contains(t, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }
                    }

                    var columnName = mapping.SourceColumn;
                    var columnPattern = $@"\b{Regex.Escape(columnName)}\b";
                    if (!Regex.IsMatch(sql, columnPattern, RegexOptions.IgnoreCase))
                    {
                        missingColumns.Add($"{mapping.SourceTable}.{mapping.SourceColumn}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating cross-database mapping columns for database {DatabaseName}", databaseName);
            }

            return missingColumns;
        }

        private bool ValidateSql(string sql, DatabaseSchemaInfo schema, List<string> requiredTables, ISqlDialectStrategy strategy, List<string> allDatabaseNames, out List<string> errors)
        {
            errors = new List<string>();

            if (!strategy.ValidateSyntax(sql, out var syntaxError))
            {
                errors.Add(syntaxError);
            }

            var schemaErrors = _validator.ValidateQuery(sql, schema, requiredTables, allDatabaseNames);
            errors.AddRange(schemaErrors);

            return errors.Count == 0;
        }

        private string ExtractSQLFromAIResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return null;

            var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var sqlLines = new List<string>();
            bool foundSql = false;
            bool foundConfirmed = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (trimmedLine.Equals("CONFIRMED", StringComparison.OrdinalIgnoreCase))
                {
                    foundConfirmed = true;
                    continue;
                }
                
                if (trimmedLine.StartsWith("CORRECTION:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                if (trimmedLine.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
                {
                    foundSql = true;
                }
                
                if (foundSql && !trimmedLine.Equals("CONFIRMED", StringComparison.OrdinalIgnoreCase))
                {
                    sqlLines.Add(trimmedLine);
                }
            }

            if (!foundSql || sqlLines.Count == 0)
            {
                _logger.LogWarning("No SQL found in AI response. Response: {Response}", response);
                return null;
            }

            if (!foundConfirmed)
            {
                _logger.LogWarning("AI did not confirm with 'CONFIRMED'. Response: {Response}", response);
            }

            var sql = string.Join(" ", sqlLines);
            
            if (string.IsNullOrWhiteSpace(sql))
            {
                var match = Regex.Match(response, @"```sql\s*(.*?)\s*```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }

                match = Regex.Match(response, @"```\s*(.*?)\s*```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }

                var trimmed = response.Trim();
                if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    return ExtractCompleteSQL(trimmed);
                }

                var selectMatch = Regex.Match(response, @"(SELECT\s+.*?)(?:\n\n|\r\n\r\n|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (selectMatch.Success)
                {
                    return ExtractCompleteSQL(selectMatch.Groups[1].Value.Trim());
                }

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                    {
                        var sqlStartIndex = response.IndexOf(trimmedLine, StringComparison.OrdinalIgnoreCase);
                        if (sqlStartIndex >= 0)
                        {
                            var remainingText = response.Substring(sqlStartIndex);
                            return ExtractCompleteSQL(remainingText);
                        }
                    }
                }
            }

            return string.IsNullOrWhiteSpace(sql) ? string.Empty : ExtractCompleteSQL(sql);
        }

        private string ExtractCompleteSQL(string sqlText)
        {
            if (string.IsNullOrWhiteSpace(sqlText)) return string.Empty;

            sqlText = Regex.Replace(sqlText, @"```sql\s*", "", RegexOptions.IgnoreCase);
            sqlText = Regex.Replace(sqlText, @"```\s*", "", RegexOptions.IgnoreCase);
            sqlText = Regex.Replace(sqlText, @"###\s+.*", "", RegexOptions.IgnoreCase);
            sqlText = Regex.Replace(sqlText, @"Explanation:.*", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var lines = sqlText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var sqlLines = new List<string>();
            bool foundSelect = false;

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
                    var isSQLKeyword = Array.Exists(sqlKeywords, kw => trimmed.StartsWith(kw, StringComparison.OrdinalIgnoreCase));
                    
                    if (!isSQLKeyword && trimmed.Length > 0 && char.IsLetter(trimmed[0]) && !trimmed.Contains("(") && !trimmed.Contains(")") && !trimmed.Contains(",") && !trimmed.Contains("=") && !trimmed.Contains("'") && !trimmed.Contains("\""))
                    {
                        break;
                    }

                    sqlLines.Add(trimmed);
                }
            }

            return string.Join(" ", sqlLines).Trim();
        }

        private bool ContainsCrossDatabaseTableReference(string sql, DatabaseSchemaInfo schema)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return false;

            var validTableNames = schema.Tables.Select(t => t.TableName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            
            var databasePrefixPattern = @"\b\w+\.\w+\.\w+\b";
            var matches = Regex.Matches(sql, databasePrefixPattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                var threePartName = match.Value;
                var parts = threePartName.Split('.');
                if (parts.Length == 3)
                {
                    var possibleTableName = $"{parts[1]}.{parts[2]}";
                    if (!validTableNames.Contains(parts[2]) && !validTableNames.Contains(possibleTableName))
                    {
                        _logger.LogWarning("Detected potential cross-database reference: {Reference}", threePartName);
                        return true;
                    }
                }
            }

            return false;
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
            
            var replacements = new List<(string Original, string Replacement)>();
            
            foreach (var pattern in databasePrefixPatterns)
            {
                var matches = Regex.Matches(fixedSql, pattern, RegexOptions.IgnoreCase);
                
                foreach (Match match in matches)
            {
                var fullMatch = match.Value;
                var databaseName = match.Groups[1].Value.Trim('"', '[', ']');
                var schemaName = match.Groups[2].Value.Trim('"', '[', ']');
                var tableName = match.Groups[3].Value.Trim('"', '[', ']');
                var twoPartName = $"{schemaName}.{tableName}";
                
                if (!schema.DatabaseName.Equals(databaseName, StringComparison.OrdinalIgnoreCase))
                {
                    string replacement = null;
                    
                    var foundValidTable = validTableNames.FirstOrDefault(vt => 
                        vt.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                        vt.Equals(twoPartName, StringComparison.OrdinalIgnoreCase) ||
                        (vt.Contains('.') && vt.Split('.').Last().Equals(tableName, StringComparison.OrdinalIgnoreCase)) ||
                        (vt.Contains('.') && vt.Equals(twoPartName, StringComparison.OrdinalIgnoreCase)));
                    
                    if (foundValidTable != null)
                    {
                        _logger.LogWarning("Removing database prefix from table reference: {Full} -> {Table}", fullMatch, foundValidTable);
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
                            _logger.LogWarning("Removing database prefix from table reference: {Full} -> {Table}", fullMatch, requiredTableMatch);
                            replacement = requiredTableMatch;
                        }
                        else
                        {
                            _logger.LogWarning("Removing entire cross-database table reference: {Full} (table not found in {Database})", fullMatch, schema.DatabaseName);
                            replacement = string.Empty;
                        }
                    }
                    
                    if (replacement != null && !replacements.Any(r => r.Original.Equals(fullMatch, StringComparison.OrdinalIgnoreCase)))
                    {
                        replacements.Add((fullMatch, replacement));
                    }
                }
            }
            }
            
            foreach (var (original, replacement) in replacements.OrderByDescending(r => r.Original.Length))
            {
                if (string.IsNullOrEmpty(replacement))
                {
                    fixedSql = Regex.Replace(fixedSql, Regex.Escape(original), string.Empty, RegexOptions.IgnoreCase);
                }
                else
                {
                    fixedSql = Regex.Replace(fixedSql, Regex.Escape(original), replacement, RegexOptions.IgnoreCase);
                }
            }

            fixedSql = FixInvalidFromClauses(fixedSql);

            return fixedSql;
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

            foreach (var pattern in invalidPatterns)
            {
                if (Regex.IsMatch(fixedSql, pattern, RegexOptions.IgnoreCase))
                {
                    _logger.LogWarning("Detected invalid FROM clause after removing cross-database reference. Attempting to fix SQL.");
                    
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
                    
                    break;
                }
            }

            return fixedSql;
        }

        private string FixCrossDatabaseJoinInSQL(string sql, DatabaseSchemaInfo schema, List<string> requiredTables, ISqlDialectStrategy strategy)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return sql;

            var fixedSql = sql;
            var removedAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var joinMatches = Regex.Matches(sql, @"(?:INNER|LEFT|RIGHT|FULL)\s+JOIN\s+(\[?[a-zA-Z0-9_]+\]?(?:\]?\.\[?[a-zA-Z0-9_]+\]?)?)(?:\s+(?:AS\s+)?([a-zA-Z0-9_]+))?\s+ON", RegexOptions.IgnoreCase);
            
            foreach (Match match in joinMatches)
            {
                var tableNameRaw = match.Groups[1].Value.Trim('[', ']');
                var alias = match.Groups[2].Value;
                var tableName = tableNameRaw;
                
                if (tableName.Contains('.'))
                {
                    var parts = tableName.Split('.');
                    if (parts.Length >= 2)
                    {
                        var schemaPart = parts[0];
                        var tablePart = parts[1];
                        tableName = $"{schemaPart}.{tablePart}";
                    }
                    else
                    {
                        tableName = parts[0];
                    }
                }

                var tableExists = schema.Tables.Any(t => 
                    t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                    t.TableName.Equals(tableNameRaw, StringComparison.OrdinalIgnoreCase));

                if (!tableExists && requiredTables != null)
                {
                    var matchesRequiredTable = requiredTables.Any(t => 
                        t.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                        t.Equals(tableNameRaw, StringComparison.OrdinalIgnoreCase));

                    if (!matchesRequiredTable)
                    {
                        _logger.LogWarning("Removing cross-database table reference: {Table} (alias: {Alias})", tableNameRaw, alias);
                        if (!string.IsNullOrWhiteSpace(alias))
                        {
                            removedAliases.Add(alias);
                        }
                        fixedSql = RemoveTableJoinFromSQL(fixedSql, tableNameRaw, alias);
                    }
                }
            }

            foreach (var alias in removedAliases)
            {
                fixedSql = RemoveAliasReferencesFromSQL(fixedSql, alias);
            }

            return fixedSql;
        }

        private string RemoveTableJoinFromSQL(string sql, string tableNameToRemove, string alias)
        {
            var escapedTable = Regex.Escape(tableNameToRemove);
            var escapedAlias = string.IsNullOrWhiteSpace(alias) ? null : Regex.Escape(alias);
            
            var joinPattern = $@"(?:INNER|LEFT|RIGHT|FULL)\s+JOIN\s+{escapedTable}(?:\s+(?:AS\s+)?{escapedAlias})?\s+ON\s+[^\s]+\s*=\s*[^\s]+";
            var result = Regex.Replace(sql, joinPattern, string.Empty, RegexOptions.IgnoreCase);
            
            result = Regex.Replace(result, @"\s+", " ", RegexOptions.IgnoreCase);
            
            return result;
        }

        private string RemoveAliasReferencesFromSQL(string sql, string aliasToRemove)
        {
            if (string.IsNullOrWhiteSpace(aliasToRemove))
                return sql;

            var result = sql;
            var escapedAlias = Regex.Escape(aliasToRemove);
            var aliasRefPattern = $@"\b{escapedAlias}\.";
            
            var selectMatch = Regex.Match(result, @"SELECT\s+(.*?)\s+FROM", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (selectMatch.Success)
            {
                var selectClause = selectMatch.Groups[1].Value;
                var selectItems = new List<string>();
                var currentItem = new StringBuilder();
                var parenDepth = 0;
                
                foreach (var ch in selectClause)
                {
                    if (ch == '(') parenDepth++;
                    else if (ch == ')') parenDepth--;
                    
                    if (ch == ',' && parenDepth == 0)
                    {
                        var item = currentItem.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(item) && !Regex.IsMatch(item, aliasRefPattern, RegexOptions.IgnoreCase))
                        {
                            selectItems.Add(item);
                        }
                        currentItem.Clear();
                    }
                    else
                    {
                        currentItem.Append(ch);
                    }
                }
                
                var lastItem = currentItem.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(lastItem) && !Regex.IsMatch(lastItem, aliasRefPattern, RegexOptions.IgnoreCase))
                {
                    selectItems.Add(lastItem);
                }
                
                if (selectItems.Count > 0)
                {
                    var cleanedSelect = string.Join(", ", selectItems);
                    result = Regex.Replace(result, @"SELECT\s+.*?\s+FROM", $"SELECT {cleanedSelect} FROM", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                }
            }
            
            var groupByMatch = Regex.Match(result, @"GROUP\s+BY\s+(.*?)(?:\s+ORDER|\s+HAVING|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (groupByMatch.Success)
            {
                var groupByClause = groupByMatch.Groups[1].Value;
                var groupByItems = new List<string>();
                var currentItem = new StringBuilder();
                var parenDepth = 0;
                
                foreach (var ch in groupByClause)
                {
                    if (ch == '(') parenDepth++;
                    else if (ch == ')') parenDepth--;
                    
                    if (ch == ',' && parenDepth == 0)
                    {
                        var item = currentItem.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(item) && !Regex.IsMatch(item, aliasRefPattern, RegexOptions.IgnoreCase))
                        {
                            groupByItems.Add(item);
                        }
                        currentItem.Clear();
                    }
                    else
                    {
                        currentItem.Append(ch);
                    }
                }
                
                var lastItem = currentItem.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(lastItem) && !Regex.IsMatch(lastItem, aliasRefPattern, RegexOptions.IgnoreCase))
                {
                    groupByItems.Add(lastItem);
                }
                
                if (groupByItems.Count > 0)
                {
                    var cleanedGroupBy = string.Join(", ", groupByItems);
                    result = Regex.Replace(result, @"GROUP\s+BY\s+.*?(?=\s+ORDER|\s+HAVING|$)", $"GROUP BY {cleanedGroupBy}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                }
                else
                {
                    result = Regex.Replace(result, @"\s+GROUP\s+BY\s+.*?(?=\s+ORDER|\s+HAVING|$)", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                }
            }
            
            result = Regex.Replace(result, @"\s+", " ", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\s+,\s*", ", ", RegexOptions.IgnoreCase);
            
            return result;
        }

        private string FixInvalidOrderByAliases(string sql, SmartRAG.Enums.DatabaseType databaseType)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return sql;

            if (databaseType != SmartRAG.Enums.DatabaseType.PostgreSQL)
                return sql;

            var orderByPattern = @"ORDER\s+BY\s+([^\s,]+(?:\s*,\s*[^\s,]+)*)";
            var match = Regex.Match(sql, orderByPattern, RegexOptions.IgnoreCase);
            
            if (!match.Success)
                return sql;

            var orderByClause = match.Groups[1].Value;
            var columns = orderByClause.Split(',').Select(c => c.Trim()).ToList();
            
            var invalidAliases = new[] { "OrderCount", "TotalAmount", "SumValue", "CountValue", "AvgValue" };
            var hasInvalidAlias = columns.Any(col => invalidAliases.Any(alias => 
                col.Equals(alias, StringComparison.OrdinalIgnoreCase) ||
                col.Equals($"\"{alias}\"", StringComparison.OrdinalIgnoreCase)));

            if (hasInvalidAlias)
            {
                _logger.LogWarning("Removing invalid ORDER BY aliases from SQL: {OrderBy}", orderByClause);
                var validColumns = columns.Where(col => !invalidAliases.Any(alias => 
                    col.Equals(alias, StringComparison.OrdinalIgnoreCase) ||
                    col.Equals($"\"{alias}\"", StringComparison.OrdinalIgnoreCase))).ToList();
                
                if (validColumns.Count > 0)
                {
                    var newOrderBy = string.Join(", ", validColumns);
                    return Regex.Replace(sql, orderByPattern, $"ORDER BY {newOrderBy}", RegexOptions.IgnoreCase);
                }
                else
                {
                    return Regex.Replace(sql, @"\s+ORDER\s+BY\s+[^\s]+(?:\s*,\s*[^\s]+)*", string.Empty, RegexOptions.IgnoreCase);
                }
            }

            return sql;
        }
    }
}
