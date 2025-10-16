using Microsoft.Extensions.Logging;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartRAG.Services
{
    /// <summary>
    /// Coordinates intelligent multi-database queries using AI
    /// </summary>
    public class MultiDatabaseQueryCoordinator : IMultiDatabaseQueryCoordinator
    {
        #region Constants

        private const double MinimumConfidence = 0.0;
        private const double DefaultConfidence = 0.5;
        private const double FallbackConfidence = 0.3;
        private const int AiResponseLogLimit = 500;
        private const int MaxTablesInFallback = 5;
        private const int DefaultMaxRows = 100;
        private const int SampleDataLimit = 200;

        #endregion

        #region Fields

        private readonly IDatabaseConnectionManager _connectionManager;
        private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer;
        private readonly IDatabaseParserService _databaseParser;
        private readonly IAIService _aiService;
        private readonly ILogger<MultiDatabaseQueryCoordinator> _logger;

        #endregion

        #region Constructor

        public MultiDatabaseQueryCoordinator(
            IDatabaseConnectionManager connectionManager,
            IDatabaseSchemaAnalyzer schemaAnalyzer,
            IDatabaseParserService databaseParser,
            IAIService aiService,
            ILogger<MultiDatabaseQueryCoordinator> logger)
        {
            _connectionManager = connectionManager;
            _schemaAnalyzer = schemaAnalyzer;
            _databaseParser = databaseParser;
            _aiService = aiService;
            _logger = logger;
        }

        #endregion

        #region Public Methods

        public async Task<QueryIntent> AnalyzeQueryIntentAsync(string userQuery)
        {
            _logger.LogInformation("Analyzing query intent for: {Query}", userQuery);

            var queryIntent = new QueryIntent
            {
                OriginalQuery = userQuery
            };

            try
            {
                // Get all database schemas
                var schemas = await _schemaAnalyzer.GetAllSchemasAsync();

                if (schemas.Count == 0)
                {
                    _logger.LogWarning("No database schemas available for query analysis");
                    queryIntent.Confidence = MinimumConfidence;
                    return queryIntent;
                }

                // Build AI prompt for query analysis
                var prompt = BuildQueryAnalysisPrompt(userQuery, schemas);

                // Get AI analysis
                var aiResponse = await _aiService.GenerateResponseAsync(prompt, new List<string>());
                
                // Debug: Log AI response
                _logger.LogInformation("AI Response (first {Limit} chars): {Response}", 
                    AiResponseLogLimit,
                    aiResponse?.Substring(0, Math.Min(AiResponseLogLimit, aiResponse?.Length ?? 0)) ?? "NULL");

                // Parse AI response into QueryIntent
                queryIntent = ParseAIResponse(aiResponse, userQuery, schemas);

                _logger.LogInformation("Query analysis completed. Confidence: {Confidence}, Databases: {Count}",
                    queryIntent.Confidence, queryIntent.DatabaseQueries.Count);
                
                // Debug: Log which databases and tables were selected
                _logger.LogInformation("AI selected {Count} database(s) for this query:", queryIntent.DatabaseQueries.Count);
                foreach (var dbQuery in queryIntent.DatabaseQueries)
                {
                    _logger.LogInformation("  ✓ {DbName} → Tables: [{Tables}]", 
                        dbQuery.DatabaseName, string.Join(", ", dbQuery.RequiredTables));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing query intent");
                queryIntent.Confidence = MinimumConfidence;
            }

            return queryIntent;
        }

        public async Task<MultiDatabaseQueryResult> ExecuteMultiDatabaseQueryAsync(QueryIntent queryIntent)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new MultiDatabaseQueryResult
            {
                Success = true
            };

            _logger.LogInformation("Executing multi-database query across {Count} databases",
                queryIntent.DatabaseQueries.Count);

            // Execute queries in parallel
            var tasks = queryIntent.DatabaseQueries.Select(async dbQuery =>
            {
                var dbResult = await ExecuteSingleDatabaseQueryAsync(dbQuery);
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

            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Multi-database query completed in {Ms}ms. Success: {Success}",
                result.ExecutionTimeMs, result.Success);

            return result;
        }

        public async Task<RagResponse> QueryMultipleDatabasesAsync(string userQuery, int maxResults = 5)
        {
            _logger.LogInformation("Processing multi-database query: {Query}", userQuery);

            try
            {
                // Step 1: Analyze query intent
                var queryIntent = await AnalyzeQueryIntentAsync(userQuery);

                if (queryIntent.DatabaseQueries.Count == 0)
                {
                return new RagResponse
                {
                    Answer = "I couldn't determine which databases to query for your question. Please try rephrasing your query.",
                    Sources = new List<SearchSource> { new SearchSource { FileName = "No databases matched" } }
                };
                }
                
                // Step 1.5: Validate intent - remove invalid database/table combinations
                queryIntent = await ValidateAndCorrectQueryIntentAsync(queryIntent);

                // Step 2: Generate SQL queries
                queryIntent = await GenerateDatabaseQueriesAsync(queryIntent);

                // Step 3: Execute queries
                var queryResults = await ExecuteMultiDatabaseQueryAsync(queryIntent);

                if (!queryResults.Success)
                {
                    return new RagResponse
                    {
                        Answer = $"Some database queries failed: {string.Join(", ", queryResults.Errors)}",
                        Sources = new List<SearchSource> { new SearchSource { FileName = "Error in query execution" } }
                    };
                }

                // Step 4: Merge and generate final response
                var mergedData = await MergeResultsAsync(queryResults, userQuery);

                // Step 5: Generate AI answer from merged data
                var finalAnswer = await GenerateFinalAnswerAsync(userQuery, mergedData, queryResults);

                return finalAnswer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing multi-database query");
                return new RagResponse
                {
                    Answer = $"An error occurred while processing your query: {ex.Message}",
                    Sources = new List<SearchSource> { new SearchSource { FileName = "Error" } }
                };
            }
        }

        public async Task<QueryIntent> GenerateDatabaseQueriesAsync(QueryIntent queryIntent)
        {
            _logger.LogInformation("Generating SQL queries for {Count} databases", queryIntent.DatabaseQueries.Count);
            
            var allSchemas = await _schemaAnalyzer.GetAllSchemasAsync();
            var additionalQueries = new List<DatabaseQueryIntent>();

            foreach (var dbQuery in queryIntent.DatabaseQueries)
            {
                try
                {
                    var schema = await _schemaAnalyzer.GetSchemaAsync(dbQuery.DatabaseId);
                    if (schema == null)
                    {
                        _logger.LogWarning("Schema not found for database: {DatabaseId}", dbQuery.DatabaseId);
                        continue;
                    }

                    // Build SQL generation prompt (tables are already validated)
                    var prompt = BuildSQLGenerationPrompt(queryIntent.OriginalQuery, dbQuery, schema);
                    
                    // Generate SQL using AI
                    var sql = await _aiService.GenerateResponseAsync(prompt, new List<string>());

                    // Extract actual SQL from AI response
                    var extractedSql = ExtractSQLFromAIResponse(sql);
                    
                    // Validate SQL completeness FIRST
                    if (!IsCompleteSql(extractedSql))
                    {
                        _logger.LogWarning("Generated SQL is incomplete for {DatabaseId}, skipping", dbQuery.DatabaseId);
                        dbQuery.GeneratedQuery = null;
                        continue;
                    }
                    
                    dbQuery.GeneratedQuery = extractedSql;
                    
                    // CRITICAL: Validate SQL doesn't use tables that don't exist in this database
                    var availableTableNames = schema.Tables.Select(t => t.TableName).ToList();
                    var sqlUpper = dbQuery.GeneratedQuery.ToUpperInvariant();
                    
                    // Check for invalid patterns
                    bool hasInvalidJoin = false;
                    var validationErrors = new List<string>();
                    
                    // 1. Check for cross-database JOIN patterns (DatabaseName.TableName)
                    if (sqlUpper.Contains("JOIN") && sqlUpper.Contains("."))
                    {
                        // Check for pattern: JOIN DatabaseName.TableName or Database.Table
                        foreach (var otherSchema in await _schemaAnalyzer.GetAllSchemasAsync())
                        {
                            if (otherSchema.DatabaseId != schema.DatabaseId)
                            {
                                var crossDbPattern = $"{otherSchema.DatabaseName.ToUpperInvariant()}.";
                                if (sqlUpper.Contains(crossDbPattern))
                                {
                                    validationErrors.Add($"Cross-database reference detected: {otherSchema.DatabaseName}.TableName");
                                    hasInvalidJoin = true;
                                    break;
                                }
                            }
                        }
                    }
                    
                    // 2. Parse SQL to find all table names
                    // Include backticks (MySQL), brackets (SQL Server), and quotes as split characters
                    var words = sqlUpper.Split(new[] { ' ', '\n', '\r', '\t', ',', '(', ')', ';', '`', '[', ']', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    // Common SQL keywords and noise words to skip
                    var sqlKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "SELECT", "FROM", "WHERE", "JOIN", "LEFT", "RIGHT", "INNER", "OUTER", "CROSS",
                        "ON", "AND", "OR", "NOT", "IN", "EXISTS", "AS", "ORDER", "GROUP", "HAVING",
                        "LIMIT", "TOP", "OFFSET", "UNION", "EXCEPT", "INTERSECT", "DISTINCT", "ALL",
                        "THE", "THIS", "THAT", "THESE", "THOSE", "WITH", "WITHOUT", "BETWEEN",
                        "LIKE", "IS", "NULL", "TRUE", "FALSE", "CASE", "WHEN", "THEN", "ELSE", "END"
                    };
                    
                    for (int i = 0; i < words.Length; i++)
                    {
                        // Check words after FROM or JOIN
                        if ((words[i] == "FROM" || words[i] == "JOIN") && i + 1 < words.Length)
                        {
                            // Remove SQL identifier wrappers: backticks (MySQL), brackets (SQL Server), quotes
                            var potentialTableName = words[i + 1]
                                .TrimEnd(';', ',', ')', '(', '`', '[', ']', '"', '\'')
                                .TrimStart('`', '[', ']', '"', '\'');
                            
                            // Skip SQL keywords and noise words
                            if (potentialTableName.Length < 2 || sqlKeywords.Contains(potentialTableName))
                            {
                                continue;
                            }
                            
                            // Check if contains dot (cross-database reference or schema.table)
                            if (potentialTableName.Contains("."))
                            {
                                validationErrors.Add($"Cross-database table reference: {potentialTableName}");
                                hasInvalidJoin = true;
                                break;
                            }
                            
                            // Check if this table exists in current database
                            if (!availableTableNames.Any(t => t.ToUpperInvariant() == potentialTableName))
                            {
                                validationErrors.Add($"Table '{potentialTableName}' doesn't exist in {dbQuery.DatabaseName}");
                                hasInvalidJoin = true;
                                break;
                            }
                        }
                    }
                    
                    // 3. Check for undefined aliases (e.g., p.ColumnName when 'p' is not defined)
                    var definedAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    
                    // Add table names as valid references
                    foreach (var tableName in availableTableNames)
                    {
                        definedAliases.Add(tableName);
                    }
                    
                    // Extract table aliases from FROM/JOIN clauses
                    for (int i = 0; i < words.Length; i++)
                    {
                        if ((words[i] == "FROM" || words[i] == "JOIN") && i + 2 < words.Length)
                        {
                            var tableName = words[i + 1].TrimStart('`', '[', '"', '\'').TrimEnd('`', ']', '"', '\'');
                            var potentialAlias = words[i + 2];
                            
                            // Skip if it's a keyword
                            var keywords = new[] { "WHERE", "ON", "JOIN", "LEFT", "INNER", "RIGHT", 
                                                   "OUTER", "CROSS", "ORDER", "GROUP", "HAVING", 
                                                   "LIMIT", "TOP", "UNION", "EXCEPT" };
                            
                            if (!keywords.Contains(potentialAlias) && potentialAlias.Length <= 4)
                            {
                                definedAliases.Add(potentialAlias);
                                _logger.LogDebug("  Defined alias: {Alias} for table {Table}", potentialAlias, tableName);
                            }
                        }
                    }
                    
                    // Check for usage of undefined aliases in entire SQL
                    var sqlLines = dbQuery.GeneratedQuery.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in sqlLines)
                    {
                        // Find all alias.column references (e.g., p.ProductID, od.Quantity)
                        // Pattern: Must start with letter (not number) to avoid false positives like "0.1"
                        var matches = Regex.Matches(line, @"([a-zA-Z_]\w*)\.(\w+)");
                        foreach (Match match in matches)
                        {
                            var alias = match.Groups[1].Value.ToUpperInvariant();
                            var column = match.Groups[2].Value;
                            
                            // Skip if it's a number (decimal point)
                            if (char.IsDigit(alias[0]))
                                continue;
                            
                            // Check if alias is defined or is a table name
                            if (!definedAliases.Contains(alias))
                            {
                                validationErrors.Add($"Undefined alias '{alias}' in {alias}.{column} - this table doesn't exist in {dbQuery.DatabaseName}!");
                                hasInvalidJoin = true;
                            }
                        }
                    }
                    
                    // 4. Check for tables in subqueries (more targeted)
                    // Look for patterns like: JOIN (SELECT * FROM TableName) or WHERE EXISTS (SELECT FROM TableName)
                    var subqueryPattern = @"\(\s*SELECT\s+.+?\s+FROM\s+(\w+)";
                    var subqueryMatches = Regex.Matches(sqlUpper, subqueryPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    foreach (Match match in subqueryMatches)
                    {
                        var tableName = match.Groups[1].Value;
                        
                        // Skip if it's a valid table or alias
                        if (tableName.Length > 1 && 
                            tableName != "SELECT" && 
                            !availableTableNames.Any(t => t.ToUpperInvariant() == tableName) &&
                            !definedAliases.Contains(tableName))
                        {
                            validationErrors.Add($"Subquery uses table '{tableName}' which is not in allowed tables!");
                            hasInvalidJoin = true;
                        }
                    }
                    
                    if (hasInvalidJoin)
                    {
                        // Invalid SQL - log errors and skip this database
                        _logger.LogError("❌ CRITICAL: Generated SQL has validation errors for {Database}!", dbQuery.DatabaseName);
                        foreach (var error in validationErrors)
                        {
                            _logger.LogError("   • {Error}", error);
                        }
                        _logger.LogError("   Available tables: {Tables}", string.Join(", ", availableTableNames));
                        _logger.LogError("   Generated SQL: {SQL}", dbQuery.GeneratedQuery);
                        
                        _logger.LogWarning("⚠️  Skipping {Database} - SQL validation failed", 
                            dbQuery.DatabaseName);
                        
                        // Extract table names from failed SQL and find them in other databases
                        var missingTableNames = new List<string>();
                        for (int i = 0; i < words.Length; i++)
                        {
                            if ((words[i] == "FROM" || words[i] == "JOIN") && i + 1 < words.Length)
                            {
                                // Remove SQL identifier wrappers: backticks (MySQL), brackets (SQL Server), quotes
                                var potentialTableName = words[i + 1]
                                    .TrimEnd(';', ',', ')', '(', '`', '[', ']', '"', '\'')
                                    .TrimStart('`', '[', ']', '"', '\'');
                                if (!availableTableNames.Any(t => t.ToUpperInvariant() == potentialTableName))
                                {
                                    missingTableNames.Add(potentialTableName);
                                }
                            }
                        }
                        
                        // Try to find these tables in other databases
                        foreach (var missingTable in missingTableNames.Distinct())
                        {
                            var correctSchema = allSchemas.FirstOrDefault(s => 
                                s.Tables.Any(t => t.TableName.Equals(missingTable, StringComparison.OrdinalIgnoreCase)));
                            
                            if (correctSchema != null && 
                                !queryIntent.DatabaseQueries.Any(q => q.DatabaseId == correctSchema.DatabaseId) &&
                                !additionalQueries.Any(q => q.DatabaseId == correctSchema.DatabaseId))
                            {
                                var exactTableName = correctSchema.Tables.First(t => 
                                    t.TableName.Equals(missingTable, StringComparison.OrdinalIgnoreCase)).TableName;
                                
                                _logger.LogInformation("🔧 Found '{Table}' in {Database}, adding to query list", 
                                    exactTableName, correctSchema.DatabaseName);
                                
                                additionalQueries.Add(new DatabaseQueryIntent
                                {
                                    DatabaseId = correctSchema.DatabaseId,
                                    DatabaseName = correctSchema.DatabaseName,
                                    RequiredTables = new List<string> { exactTableName },
                                    Purpose = $"Get {exactTableName} data",
                                    Priority = 2
                                });
                            }
                        }
                        
                        dbQuery.GeneratedQuery = null;
                    }

                    if (!string.IsNullOrEmpty(dbQuery.GeneratedQuery))
                    {
                        _logger.LogInformation("✓ Generated SQL for {DatabaseId}: {SQL}", 
                            dbQuery.DatabaseId, dbQuery.GeneratedQuery);
                    }
                    else
                    {
                        _logger.LogWarning("✗ SQL generation failed validation for {DatabaseId}", dbQuery.DatabaseId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating SQL for database: {DatabaseId}", dbQuery.DatabaseId);
                }
            }
            
            // Add any additional databases discovered during SQL validation
            if (additionalQueries.Count > 0)
            {
                _logger.LogInformation("Adding {Count} additional database(s) discovered during SQL generation", additionalQueries.Count);
                
                foreach (var additionalQuery in additionalQueries)
                {
                    queryIntent.DatabaseQueries.Add(additionalQuery);
                    
                    // Generate SQL for the additional query
                    try
                    {
                        var schema = await _schemaAnalyzer.GetSchemaAsync(additionalQuery.DatabaseId);
                        if (schema != null)
                        {
                            var prompt = BuildSQLGenerationPrompt(queryIntent.OriginalQuery, additionalQuery, schema);
                            var sql = await _aiService.GenerateResponseAsync(prompt, new List<string>());
                            var extractedSql = ExtractSQLFromAIResponse(sql);
                            
                            // Validate SQL completeness
                            if (!IsCompleteSql(extractedSql))
                            {
                                _logger.LogWarning("Generated SQL is incomplete for additional database {DatabaseId}, skipping", additionalQuery.DatabaseId);
                                additionalQuery.GeneratedQuery = null;
                                continue;
                            }
                            
                            additionalQuery.GeneratedQuery = extractedSql;
                            
                            _logger.LogInformation("✓ Generated SQL for additional database {DatabaseId}: {SQL}", 
                                additionalQuery.DatabaseId, additionalQuery.GeneratedQuery);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating SQL for additional database: {DatabaseId}", additionalQuery.DatabaseId);
                    }
                }
            }

            return queryIntent;
        }

        public Task<string> MergeResultsAsync(MultiDatabaseQueryResult queryResults, string originalQuery)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Query: {originalQuery}");
            sb.AppendLine();

            foreach (var kvp in queryResults.DatabaseResults.OrderBy(x => x.Key))
            {
                var dbResult = kvp.Value;
                
                sb.AppendLine($"=== {dbResult.DatabaseName} ===");
                
                if (dbResult.Success)
                {
                    sb.AppendLine($"Rows returned: {dbResult.RowCount}");
                    sb.AppendLine(dbResult.ResultData);
                }
                else
                {
                    sb.AppendLine($"Error: {dbResult.ErrorMessage}");
                }
                
                sb.AppendLine();
            }

            return Task.FromResult(sb.ToString());
        }

        #endregion

        #region Private Helper Methods

        private async Task<QueryIntent> ValidateAndCorrectQueryIntentAsync(QueryIntent queryIntent)
        {
            _logger.LogInformation("Validating query intent for {Count} databases", queryIntent.DatabaseQueries.Count);
            
            var validQueries = new List<DatabaseQueryIntent>();
            var missingTables = new Dictionary<string, List<string>>(); // table -> databases that need it
            var allSchemas = await _schemaAnalyzer.GetAllSchemasAsync();
            
            foreach (var dbQuery in queryIntent.DatabaseQueries)
            {
                var schema = await _schemaAnalyzer.GetSchemaAsync(dbQuery.DatabaseId);
                if (schema == null)
                {
                    _logger.LogWarning("Schema not found for {DatabaseId}, removing from query list", dbQuery.DatabaseId);
                    continue;
                }
                
                // Check which tables actually exist in this database
                var validTables = new List<string>();
                var invalidTables = new List<string>();
                
                foreach (var tableName in dbQuery.RequiredTables)
                {
                    if (schema.Tables.Any(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)))
                    {
                        // Use exact casing from schema
                        var exactTableName = schema.Tables.First(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)).TableName;
                        validTables.Add(exactTableName);
                    }
                    else
                    {
                        invalidTables.Add(tableName);
                        
                        // Track this missing table
                        if (!missingTables.ContainsKey(tableName))
                        {
                            missingTables[tableName] = new List<string>();
                        }
                        missingTables[tableName].Add(dbQuery.DatabaseId);
                    }
                }
                
                if (invalidTables.Count > 0)
                {
                    _logger.LogWarning("⚠️  AI selected invalid tables for {DatabaseName}: {InvalidTables}",
                        dbQuery.DatabaseName, string.Join(", ", invalidTables));
                }
                
                if (validTables.Count > 0)
                {
                    dbQuery.RequiredTables = validTables;
                    validQueries.Add(dbQuery);
                    _logger.LogInformation("✓ {DatabaseName}: {Tables}", dbQuery.DatabaseName, string.Join(", ", validTables));
                }
                else
                {
                    _logger.LogWarning("✗ {DatabaseName}: No valid tables, removing", dbQuery.DatabaseName);
                }
            }
            
            // Auto-add missing databases for tables AI requested but put in wrong database
            _logger.LogInformation("Checking for missing tables: {Count} table(s) not found in selected databases", missingTables.Count);
            
            foreach (var kvp in missingTables)
            {
                var tableName = kvp.Key;
                _logger.LogInformation("  Looking for table '{Table}' in all databases...", tableName);
                
                // Find which database actually has this table
                var correctSchema = allSchemas.FirstOrDefault(s => 
                    s.Tables.Any(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)));
                
                if (correctSchema == null)
                {
                    _logger.LogWarning("  ✗ Table '{Table}' not found in any database!", tableName);
                    continue;
                }
                
                _logger.LogInformation("  ✓ Found '{Table}' in {Database}", tableName, correctSchema.DatabaseName);
                
                if (validQueries.Any(q => q.DatabaseId == correctSchema.DatabaseId))
                {
                    _logger.LogInformation("  → {Database} already in query list, checking if table is included...", correctSchema.DatabaseName);
                    
                    var existingQuery = validQueries.First(q => q.DatabaseId == correctSchema.DatabaseId);
                    if (!existingQuery.RequiredTables.Contains(tableName, StringComparer.OrdinalIgnoreCase))
                    {
                        var exactTableName = correctSchema.Tables.First(t => 
                            t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)).TableName;
                        
                        existingQuery.RequiredTables.Add(exactTableName);
                        _logger.LogInformation("  🔧 Added table '{Table}' to existing {Database} query", exactTableName, correctSchema.DatabaseName);
                    }
                }
                else
                {
                    var exactTableName = correctSchema.Tables.First(t => 
                        t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)).TableName;
                    
                    _logger.LogInformation("  🔧 Auto-adding {Database} database for table '{Table}'", 
                        correctSchema.DatabaseName, exactTableName);
                    
                    validQueries.Add(new DatabaseQueryIntent
                    {
                        DatabaseId = correctSchema.DatabaseId,
                        DatabaseName = correctSchema.DatabaseName,
                        RequiredTables = new List<string> { exactTableName },
                        Purpose = $"Get data from {exactTableName} table",
                        Priority = 2
                    });
                }
            }
            
            queryIntent.DatabaseQueries = validQueries;
            _logger.LogInformation("Validation complete: {Count} databases ready", validQueries.Count);
            
            return queryIntent;
        }

        private async Task<DatabaseQueryResult> ExecuteSingleDatabaseQueryAsync(DatabaseQueryIntent dbQuery)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new DatabaseQueryResult
            {
                DatabaseId = dbQuery.DatabaseId,
                DatabaseName = dbQuery.DatabaseName,
                ExecutedQuery = dbQuery.GeneratedQuery
            };

            try
            {
                var connection = await _connectionManager.GetConnectionAsync(dbQuery.DatabaseId);
                if (connection == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Database connection not found";
                    return result;
                }

                if (string.IsNullOrEmpty(dbQuery.GeneratedQuery))
                {
                    result.Success = false;
                    result.ErrorMessage = "No query generated";
                    return result;
                }

                // Execute the query
                var maxRows = connection.MaxRowsPerQuery > 0 ? connection.MaxRowsPerQuery : DefaultMaxRows;
                var queryResult = await _databaseParser.ExecuteQueryAsync(
                    connection.ConnectionString,
                    dbQuery.GeneratedQuery,
                    connection.DatabaseType,
                    maxRows);

                result.ResultData = queryResult;
                result.RowCount = CountRowsInResult(queryResult);
                result.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing query on database: {DatabaseId}", dbQuery.DatabaseId);
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

        private string BuildQueryAnalysisPrompt(string userQuery, List<DatabaseSchemaInfo> schemas)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are a database query analyzer. Analyze the user's query and determine which databases and tables are needed.");
            sb.AppendLine();
            sb.AppendLine($"User Query: {userQuery}");
            sb.AppendLine();
            sb.AppendLine("Available Databases:");
            sb.AppendLine();

            foreach (var schema in schemas)
            {
                sb.AppendLine("═══════════════════════════════════════");
                sb.AppendLine($"DATABASE #{schemas.IndexOf(schema) + 1}: {schema.DatabaseName}");
                sb.AppendLine("═══════════════════════════════════════");
                sb.AppendLine($"Database ID: {schema.DatabaseId}");
                sb.AppendLine($"Database Type: {schema.DatabaseType}");
                
                if (!string.IsNullOrEmpty(schema.Description))
                {
                    sb.AppendLine($"Description: {schema.Description}");
                }

                sb.AppendLine();
                sb.AppendLine($"TABLES AVAILABLE IN {schema.DatabaseName.ToUpperInvariant()} DATABASE:");
                
                var tableList = new List<string>();
                foreach (var table in schema.Tables.Take(20))
                {
                    tableList.Add(table.TableName);
                    sb.AppendLine($"  [{table.TableName}]");
                    sb.AppendLine($"    • {table.RowCount} rows");
                    sb.AppendLine($"    • Columns: {string.Join(", ", table.Columns.Take(8).Select(c => c.ColumnName))}");
                    
                    if (table.ForeignKeys.Any())
                    {
                        sb.AppendLine($"    • Links to: {string.Join(", ", table.ForeignKeys.Select(fk => fk.ReferencedTable).Distinct())}");
                    }
                }
                
                sb.AppendLine();
                sb.AppendLine($"⚠️  IMPORTANT: These tables ONLY exist in {schema.DatabaseName}:");
                sb.AppendLine($"    {string.Join(", ", tableList)}");
                sb.AppendLine();
            }

            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("🚨 CRITICAL RULES:");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("1. Each table exists in ONLY ONE database");
            sb.AppendLine("2. Before selecting a table, look at 'TABLES IN THIS DATABASE' section above");
            sb.AppendLine("3. Only select tables that are listed under that specific database");
            sb.AppendLine("4. For cross-database queries, identify which tables are in which database");
            sb.AppendLine("5. Use Foreign Key relationships to understand how tables connect across databases");
            sb.AppendLine("6. Use EXACT Database IDs and Table Names from the schema above");
            sb.AppendLine("7. If a query needs data from multiple databases, create separate database entries for each");
            sb.AppendLine();
            sb.AppendLine("💰 IMPORTANT: Cross-Database Data Requirements:");
            sb.AppendLine("─────────────────────────────────────────");
            sb.AppendLine("If the query requires calculations using columns from multiple tables:");
            sb.AppendLine("  ✓ Identify which tables contain the required columns");
            sb.AppendLine("  ✓ Check Foreign Key relationships to understand table connections");
            sb.AppendLine("  ✓ You MUST select ALL databases that contain required data!");
            sb.AppendLine("  ✓ Set requiresCrossDatabaseJoin: true");
            sb.AppendLine();
            sb.AppendLine("Example: Query requiring multiplication of columns from different tables");
            sb.AppendLine("  → Database1 has: TableA (ForeignKeyID, QuantityColumn)");
            sb.AppendLine("  → Database2 has: TableB (ID, PriceColumn)");
            sb.AppendLine("  → requiresCrossDatabaseJoin: true");
            sb.AppendLine();
            sb.AppendLine("Based on the user query, provide your analysis in the following JSON format:");
            sb.AppendLine("{");
            sb.AppendLine("  \"understanding\": \"Brief explanation of what the user wants\",");
            sb.AppendLine("  \"confidence\": 0.95,");
            sb.AppendLine("  \"requiresCrossDatabaseJoin\": false,");
            sb.AppendLine("  \"reasoning\": \"Why these databases and tables were selected\",");
            sb.AppendLine("  \"databases\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"databaseId\": \"EXACT_DB_ID_FROM_ABOVE\",");
            sb.AppendLine("      \"databaseName\": \"DatabaseName\",");
            sb.AppendLine("      \"requiredTables\": [\"ExactTableName1\", \"ExactTableName2\"],");
            sb.AppendLine("      \"purpose\": \"What data to get from this database\",");
            sb.AppendLine("      \"priority\": 1");
            sb.AppendLine("    }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("IMPORTANT: Respond ONLY with valid JSON, no other text, no markdown.");
            sb.AppendLine("Double-check that each table you reference EXISTS in the database you selected.");

            return sb.ToString();
        }

        private string BuildSQLGenerationPrompt(string userQuery, DatabaseQueryIntent dbQuery, DatabaseSchemaInfo schema)
        {
            var sb = new StringBuilder();
            
            // Get first table for examples
            var firstTable = schema.Tables.FirstOrDefault(t => dbQuery.RequiredTables.Contains(t.TableName, StringComparer.OrdinalIgnoreCase));
            
            sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║          SQL QUERY GENERATION FOR SINGLE DATABASE              ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("🚨 CRITICAL - READ THIS FIRST:");
            sb.AppendLine($"   YOU CAN ONLY USE THESE TABLES: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine("   ANY OTHER TABLE WILL CAUSE AN ERROR!");
            sb.AppendLine();
            sb.AppendLine($"📍 TARGET DATABASE: {schema.DatabaseName} ({schema.DatabaseType})");
            sb.AppendLine($"📋 YOUR ALLOWED TABLES: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine();
            sb.AppendLine("🎯 YOUR TASK:");
            sb.AppendLine($"   Write a simple SQL query using ONLY these tables: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine("   Return data with all foreign key IDs so application can merge with other databases");
            sb.AppendLine();
            sb.AppendLine($"❓ User Question: {userQuery}");
            sb.AppendLine($"🎨 What to retrieve from {schema.DatabaseName}: {dbQuery.Purpose}");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"TABLES AVAILABLE IN {schema.DatabaseName}:");
            sb.AppendLine("═══════════════════════════════════════");
            
            foreach (var tableName in dbQuery.RequiredTables)
            {
                var table = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                if (table != null)
                {
                    sb.AppendLine($"\n✓ Table: {schema.DatabaseName}.{table.TableName}");
                    sb.AppendLine($"  ONLY THESE COLUMNS EXIST IN THIS DATABASE:");
                    
                    foreach (var col in table.Columns)
                    {
                        sb.AppendLine($"    ✓ {col.ColumnName} ({col.DataType})");
                    }
                    
                    sb.AppendLine($"  ⚠️  Any other column will cause ERROR!");
                    
                    if (table.ForeignKeys.Any())
                    {
                        sb.AppendLine();
                        sb.AppendLine("  🔗 Foreign Keys (reference IDs to OTHER databases):");
                        foreach (var fk in table.ForeignKeys)
                        {
                            sb.AppendLine($"    • {fk.ColumnName} links to {fk.ReferencedTable} table (in ANOTHER database)");
                        }
                        sb.AppendLine();
                        sb.AppendLine("  💡 WHAT TO DO:");
                        sb.AppendLine($"     Always include {string.Join(", ", table.ForeignKeys.Select(fk => fk.ColumnName))} in your SELECT");
                        sb.AppendLine("     Application will use these IDs to fetch data from other databases");
                    }
                    
                    // Show example SQL for this table
                    sb.AppendLine();
                    sb.AppendLine($"  📝 Example SQL for {table.TableName}:");
                    
                    var fkColumns = table.ForeignKeys.Select(fk => fk.ColumnName).ToList();
                    var regularColumns = table.Columns.Where(c => !fkColumns.Contains(c.ColumnName)).Take(3).Select(c => c.ColumnName).ToList();
                    var allColumns = fkColumns.Concat(regularColumns).ToList();
                    
                    if (schema.DatabaseType == DatabaseType.SqlServer)
                    {
                        sb.AppendLine($"     SELECT TOP 100 {string.Join(", ", allColumns)} FROM {table.TableName}");
                    }
                    else
                    {
                        sb.AppendLine($"     SELECT {string.Join(", ", allColumns)} FROM {table.TableName} LIMIT 100");
                    }

                    if (!string.IsNullOrEmpty(table.SampleData))
                    {
                        sb.AppendLine();
                        sb.AppendLine($"  Sample Data (first few rows):");
                        var sampleLines = table.SampleData.Substring(0, Math.Min(SampleDataLimit, table.SampleData.Length))
                            .Split('\n')
                            .Take(3);
                        foreach (var sampleLine in sampleLines)
                        {
                            sb.AppendLine($"    {sampleLine}");
                        }
                    }
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("⚠️ TABLES NOT IN THIS DATABASE (DO NOT USE):");
            var otherTables = schema.Tables
                .Where(t => !dbQuery.RequiredTables.Contains(t.TableName, StringComparer.OrdinalIgnoreCase))
                .Select(t => t.TableName)
                .ToList();
            if (otherTables.Any())
            {
                sb.AppendLine($"  {string.Join(", ", otherTables)}");
            }
            else
            {
                sb.AppendLine("  (All tables in this database are allowed)");
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("📝 HOW TO WRITE YOUR SQL QUERY:");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("STEP 1: Choose your tables");
            sb.AppendLine($"   → You can use: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine($"   → You cannot use: Any other table in {schema.DatabaseName}");
            sb.AppendLine();
            sb.AppendLine("STEP 2: Write SELECT clause");
            sb.AppendLine("   → Include ALL foreign key columns (e.g., ProductID, CustomerID)");
            sb.AppendLine("   → Include columns needed to answer the question");
            sb.AppendLine("   → Use aggregations (SUM, COUNT, AVG) if needed");
            sb.AppendLine();
            sb.AppendLine("STEP 3: Write FROM clause");
            sb.AppendLine($"   → FROM {dbQuery.RequiredTables[0]}  ✅ (use allowed table)");
            sb.AppendLine("   → FROM OtherTable  ❌ (not in allowed list)");
            sb.AppendLine();
            sb.AppendLine("STEP 4: Write JOIN clause (if needed)");
            sb.AppendLine("   → JOIN between allowed tables only");
            sb.AppendLine($"   → Example: FROM {dbQuery.RequiredTables[0]} t1 JOIN {(dbQuery.RequiredTables.Count > 1 ? dbQuery.RequiredTables[1] : dbQuery.RequiredTables[0])} t2 ON t1.ID = t2.ID");
            sb.AppendLine("   → NEVER join with tables from other databases");
            sb.AppendLine();
            sb.AppendLine("STEP 5: Apply filters and ordering");
            sb.AppendLine("   → WHERE, GROUP BY, ORDER BY as needed");
            sb.AppendLine("   → Use columns from allowed tables only");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("💡 SIMPLE STRATEGY:");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("1. SELECT data from your allowed tables");
            sb.AppendLine("2. Always include foreign key columns (ProductID, CustomerID, etc.)");
            sb.AppendLine("3. Don't worry about data from other databases - application handles merging");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("📖 SQL TEMPLATES TO FOLLOW:");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("Template 1: Simple data retrieval with FK");
            sb.AppendLine($"  SELECT ForeignKeyColumn, Column1, Column2 FROM {dbQuery.RequiredTables[0]}");
            sb.AppendLine("  → Returns FK for merging + relevant data");
            sb.AppendLine();
            sb.AppendLine("Template 2: Aggregation with FK");
            sb.AppendLine($"  SELECT ForeignKeyColumn, SUM(NumericColumn) AS Total FROM {dbQuery.RequiredTables[0]} GROUP BY ForeignKeyColumn");
            sb.AppendLine("  → Returns FK + aggregated values");
            sb.AppendLine();
            sb.AppendLine("Template 3: JOIN between allowed tables");
            if (dbQuery.RequiredTables.Count > 1)
            {
                sb.AppendLine($"  SELECT t1.FK, t1.Column1, t2.Column2");
                sb.AppendLine($"  FROM {dbQuery.RequiredTables[0]} t1");
                sb.AppendLine($"  JOIN {dbQuery.RequiredTables[1]} t2 ON t1.ID = t2.ID");
                sb.AppendLine("  → JOIN only between allowed tables");
            }
            else
            {
                sb.AppendLine("  (Only one table available - no JOIN needed)");
            }
            sb.AppendLine();
            sb.AppendLine("🚫 ABSOLUTELY FORBIDDEN - WILL CAUSE ERRORS:");
            sb.AppendLine("  ❌ JOIN with tables not in allowed list");
            sb.AppendLine("  ❌ Use DatabaseName.TableName syntax");
            sb.AppendLine("  ❌ Reference columns from non-allowed tables");
            sb.AppendLine("  ❌ EXISTS/IN subqueries with non-allowed tables");
            sb.AppendLine("  ❌ ANY reference to tables outside your allowed list");
            sb.AppendLine();
            sb.AppendLine("🔴 CRITICAL: Even in WHERE clause, subquery, or EXISTS:");
            sb.AppendLine("   You can ONLY use tables from your allowed list!");
            sb.AppendLine("   Example of FORBIDDEN patterns:");
            sb.AppendLine("     ❌ WHERE EXISTS (SELECT 1 FROM OtherTable ...)");
            sb.AppendLine("     ❌ WHERE ColumnX IN (SELECT ID FROM OtherTable)");
            sb.AppendLine("     ❌ JOIN OtherTable ON ...");
            sb.AppendLine();
            sb.AppendLine($"   YOUR ALLOWED TABLES: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine("   Use ONLY these tables - nothing else!");
            sb.AppendLine();
            sb.AppendLine("🎯 SPECIAL CASE - Cross-Database Calculations:");
            sb.AppendLine("  User query: 'Calculate revenue from Electronics category orders'");
            sb.AppendLine($"  Your allowed tables: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine();
            sb.AppendLine("  ❌ WRONG APPROACH (referencing other DB's table):");
            sb.AppendLine("     SELECT SUM(od.Subtotal)");
            sb.AppendLine("     FROM OrderDetails od");
            sb.AppendLine("     JOIN Products p ON od.ProductID = p.ProductID  ← ERROR! Products not in your DB!");
            sb.AppendLine("     WHERE p.Category = 'Electronics'");
            sb.AppendLine();
            sb.AppendLine("  ❌ ALSO WRONG (EXISTS with other DB's table):");
            sb.AppendLine("     SELECT SUM(od.Subtotal)");
            sb.AppendLine("     FROM OrderDetails od");
            sb.AppendLine("     WHERE EXISTS (");
            sb.AppendLine("       SELECT 1 FROM Products p  ← ERROR! Products not in your DB!");
            sb.AppendLine("       WHERE od.ProductID = p.ProductID AND p.Category = 'Electronics'");
            sb.AppendLine("     )");
            sb.AppendLine();
            sb.AppendLine("  ✅ CORRECT APPROACH (return FK for merging):");
            sb.AppendLine("     SELECT ProductID, SUM(Subtotal) AS Revenue");
            sb.AppendLine("     FROM OrderDetails");
            sb.AppendLine("     GROUP BY ProductID");
            sb.AppendLine("     → Application will:");
            sb.AppendLine("        1. Get category from Products DB using ProductID");
            sb.AppendLine("        2. Filter for 'Electronics' category");
            sb.AppendLine("        3. Sum the revenues");
            sb.AppendLine();
            
            // Database-specific syntax
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"🔧 {schema.DatabaseType} SYNTAX:");
            sb.AppendLine("═══════════════════════════════════════");
            
            switch (schema.DatabaseType)
            {
                case DatabaseType.SqlServer:
                    sb.AppendLine($"Format: SELECT TOP 100 columns FROM {dbQuery.RequiredTables[0]} WHERE ... ORDER BY column");
                    sb.AppendLine("• TOP goes after SELECT");
                    sb.AppendLine("• ORDER BY at the end");
                    break;
                    
                case DatabaseType.SQLite:
                    sb.AppendLine($"Format: SELECT columns FROM {dbQuery.RequiredTables[0]} WHERE ... ORDER BY column LIMIT 100");
                    sb.AppendLine("• LIMIT at the very end");
                    sb.AppendLine("• Use EXACT table/column casing");
                    break;
                    
                case DatabaseType.MySQL:
                    sb.AppendLine($"Format: SELECT columns FROM {dbQuery.RequiredTables[0]} WHERE ... ORDER BY column LIMIT 100");
                    sb.AppendLine("• LIMIT at the very end");
                    break;
                    
                case DatabaseType.PostgreSQL:
                    sb.AppendLine($"Format: SELECT columns FROM {dbQuery.RequiredTables[0]} WHERE ... ORDER BY column LIMIT 100");
                    sb.AppendLine("• LIMIT at the very end");
                    break;
            }
            
            sb.AppendLine();
            sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║                   CRITICAL OUTPUT RULES                        ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("⛔ DO NOT WRITE:");
            sb.AppendLine("   • 'Here is the SQL query...'");
            sb.AppendLine("   • 'This query...'");
            sb.AppendLine("   • 'The key points are...'");
            sb.AppendLine("   • ANY explanations, descriptions, or comments");
            sb.AppendLine("   • Markdown code blocks (```)");
            sb.AppendLine();
            sb.AppendLine("✅ ONLY WRITE:");
            sb.AppendLine("   • Pure SQL query");
            sb.AppendLine("   • Start with SELECT");
            sb.AppendLine("   • Nothing before SELECT");
            sb.AppendLine("   • Nothing after the query ends");
            sb.AppendLine();
            sb.AppendLine("✅ Example of CORRECT output:");
            if (schema.DatabaseType == DatabaseType.SqlServer)
            {
                sb.AppendLine($"   SELECT TOP 100 Column1, Column2, ForeignKeyColumn FROM {dbQuery.RequiredTables[0]}");
            }
            else
            {
                sb.AppendLine($"   SELECT Column1, Column2, ForeignKeyColumn FROM {dbQuery.RequiredTables[0]} LIMIT 100");
            }
            sb.AppendLine();
            sb.AppendLine("❌ Example of WRONG output:");
            sb.AppendLine("   Here is the SQL query: SELECT ...");
            sb.AppendLine("   (No text before SQL!)");
            sb.AppendLine();
            sb.AppendLine("❌ Example of INCOMPLETE SQL:");
            sb.AppendLine("   SELECT Column1 FROM TableA ORDER BY");
            sb.AppendLine("   (ORDER BY must have column names!)");
            sb.AppendLine();
            sb.AppendLine("🔴 CRITICAL COMPLETENESS RULES:");
            sb.AppendLine("  - ORDER BY clause MUST include column name(s)");
            sb.AppendLine("  - GROUP BY clause MUST include column name(s)");
            sb.AppendLine("  - JOIN clause MUST include ON condition");
            sb.AppendLine("  - SQL MUST be complete and executable");
            sb.AppendLine();
            sb.AppendLine("🔴 FINAL REMINDER:");
            sb.AppendLine($"  - ALLOWED TABLES: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine("  - DO NOT use any other tables in FROM, JOIN, WHERE, or subqueries!");
            sb.AppendLine("  - Your response must START with SELECT, not with any text!");

            return sb.ToString();
        }

        private QueryIntent ParseAIResponse(string aiResponse, string originalQuery, List<DatabaseSchemaInfo> schemas)
        {
            var queryIntent = new QueryIntent
            {
                OriginalQuery = originalQuery,
                Confidence = DefaultConfidence
            };

            try
            {
                // Try to extract JSON from AI response
                var jsonStart = aiResponse.IndexOf('{');
                var jsonEnd = aiResponse.LastIndexOf('}');
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var parsedResponse = JsonSerializer.Deserialize<JsonElement>(json);

                    if (parsedResponse.TryGetProperty("understanding", out var understanding))
                    {
                        queryIntent.QueryUnderstanding = understanding.GetString() ?? string.Empty;
                    }

                    if (parsedResponse.TryGetProperty("confidence", out var confidence))
                    {
                        queryIntent.Confidence = confidence.GetDouble();
                    }

                    if (parsedResponse.TryGetProperty("requiresCrossDatabaseJoin", out var crossJoin))
                    {
                        queryIntent.RequiresCrossDatabaseJoin = crossJoin.GetBoolean();
                    }

                    if (parsedResponse.TryGetProperty("reasoning", out var reasoning))
                    {
                        queryIntent.Reasoning = reasoning.GetString();
                    }

                    if (parsedResponse.TryGetProperty("databases", out var databases) && databases.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var db in databases.EnumerateArray())
                        {
                            var dbQueryIntent = new DatabaseQueryIntent();

                            if (db.TryGetProperty("databaseId", out var dbId))
                            {
                                dbQueryIntent.DatabaseId = dbId.GetString() ?? string.Empty;
                            }

                            if (db.TryGetProperty("databaseName", out var dbName))
                            {
                                dbQueryIntent.DatabaseName = dbName.GetString() ?? string.Empty;
                            }

                            if (db.TryGetProperty("requiredTables", out var tables) && tables.ValueKind == JsonValueKind.Array)
                            {
                                dbQueryIntent.RequiredTables = tables.EnumerateArray()
                                    .Select(t => t.GetString() ?? string.Empty)
                                    .Where(t => !string.IsNullOrEmpty(t))
                                    .ToList();
                            }

                            if (db.TryGetProperty("purpose", out var purpose))
                            {
                                dbQueryIntent.Purpose = purpose.GetString();
                            }

                            if (db.TryGetProperty("priority", out var priority))
                            {
                                dbQueryIntent.Priority = priority.GetInt32();
                            }

                            queryIntent.DatabaseQueries.Add(dbQueryIntent);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse AI response, using fallback");
                // Fallback: query all databases
                queryIntent = CreateFallbackQueryIntent(originalQuery, schemas);
            }

            return queryIntent;
        }

        private QueryIntent CreateFallbackQueryIntent(string originalQuery, List<DatabaseSchemaInfo> schemas)
        {
            var queryIntent = new QueryIntent
            {
                OriginalQuery = originalQuery,
                QueryUnderstanding = "Querying all available databases",
                Confidence = FallbackConfidence,
                Reasoning = "Fallback: AI analysis failed, querying all databases"
            };

            foreach (var schema in schemas)
            {
                queryIntent.DatabaseQueries.Add(new DatabaseQueryIntent
                {
                    DatabaseId = schema.DatabaseId,
                    DatabaseName = schema.DatabaseName,
                    RequiredTables = schema.Tables.Take(MaxTablesInFallback).Select(t => t.TableName).ToList(),
                    Purpose = "Retrieve relevant data",
                    Priority = 1
                });
            }

            return queryIntent;
        }

        private string ExtractSQLFromAIResponse(string aiResponse)
        {
            if (string.IsNullOrWhiteSpace(aiResponse))
            {
                return string.Empty;
            }

            var sql = aiResponse.Trim();
            
            // Remove markdown code blocks
            if (sql.StartsWith("```"))
            {
                var codeBlockLines = sql.Split('\n');
                sql = string.Join("\n", codeBlockLines.Skip(1).Take(codeBlockLines.Length - 2)).Trim();
            }
            
            // Remove common AI explanation patterns
            var explanationPatterns = new[]
            {
                "Here is the SQL query",
                "Here's the SQL query",
                "The SQL query is",
                "This query",
                "This SQL",
                "The query",
                "Key points:",
                "This performs",
                "This will",
                "Note:",
                "Important:"
            };
            
            var responseLines = sql.Split('\n');
            var sqlLines = new List<string>();
            bool inSQL = false;
            
            foreach (var line in responseLines)
            {
                var trimmed = line.Trim();
                var trimmedUpper = trimmed.ToUpperInvariant();
                
                // Skip empty lines and explanations
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;
                
                // Skip explanation lines
                if (explanationPatterns.Any(p => trimmed.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                    continue;
                
                // Detect SQL start
                if (trimmedUpper.StartsWith("SELECT") || trimmedUpper.StartsWith("WITH"))
                {
                    inSQL = true;
                }
                
                if (inSQL)
                {
                    sqlLines.Add(line);
                    
                    // Check if SQL is complete (ends with semicolon)
                    if (trimmed.EndsWith(";"))
                    {
                        break;
                    }
                    
                    // Stop if we hit explanation text after SQL
                    if (sqlLines.Count > 3 && // At least some SQL collected
                        (trimmed.StartsWith("This ", StringComparison.OrdinalIgnoreCase) ||
                         trimmed.StartsWith("Note:", StringComparison.OrdinalIgnoreCase) ||
                         trimmed.StartsWith("The ", StringComparison.OrdinalIgnoreCase) ||
                         trimmed.StartsWith("Here ", StringComparison.OrdinalIgnoreCase)))
                    {
                        // Remove this explanation line
                        sqlLines.RemoveAt(sqlLines.Count - 1);
                        break;
                    }
                }
            }
            
            if (sqlLines.Count > 0)
            {
                sql = string.Join("\n", sqlLines).Trim().TrimEnd(';');
            }

            return sql.Trim();
        }

        private bool IsCompleteSql(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return false;
            }
            
            var sqlUpper = sql.ToUpperInvariant();
            var lines = sql.Split('\n').Select(l => l.Trim()).ToArray();
            
            // Check for incomplete clauses
            foreach (var line in lines)
            {
                var lineUpper = line.ToUpperInvariant();
                
                // ORDER BY must have column name(s) after it
                if (lineUpper.Trim() == "ORDER BY")
                {
                    _logger.LogWarning("Incomplete SQL: ORDER BY clause has no columns");
                    return false;
                }
                
                // GROUP BY must have column name(s) after it
                if (lineUpper.Trim() == "GROUP BY")
                {
                    _logger.LogWarning("Incomplete SQL: GROUP BY clause has no columns");
                    return false;
                }
                
                // JOIN must have ON condition
                if ((lineUpper.Contains(" JOIN ") || lineUpper.EndsWith(" JOIN")) && 
                    !lineUpper.Contains(" ON "))
                {
                    // Check if ON is on next line
                    var currentIndex = Array.IndexOf(lines, line);
                    if (currentIndex == lines.Length - 1 || 
                        !lines[currentIndex + 1].ToUpperInvariant().TrimStart().StartsWith("ON "))
                    {
                        _logger.LogWarning("Incomplete SQL: JOIN clause has no ON condition");
                        return false;
                    }
                }
            }
            
            // Must have basic SQL structure
            if (!sqlUpper.Contains("SELECT") || !sqlUpper.Contains("FROM"))
            {
                _logger.LogWarning("Incomplete SQL: Missing SELECT or FROM clause");
                return false;
            }
            
            return true;
        }

        private int CountRowsInResult(string resultData)
        {
            if (string.IsNullOrEmpty(resultData))
            {
                return 0;
            }

            return resultData.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Count(line => !line.StartsWith("---") && !line.StartsWith("Table:"));
        }

        private async Task<RagResponse> GenerateFinalAnswerAsync(
            string userQuery, 
            string mergedData, 
            MultiDatabaseQueryResult queryResults)
        {
            try
            {
                var prompt = $@"Based on the following data from multiple databases, answer the user's question.

User Question: {userQuery}

Database Results:
{mergedData}

Provide a clear, concise answer synthesizing information from all relevant databases.
If data is missing or incomplete, mention it.";

                var answer = await _aiService.GenerateResponseAsync(prompt, new List<string>());

                var sources = queryResults.DatabaseResults
                    .Where(r => r.Value.Success)
                    .Select(r => new SearchSource 
                    { 
                        FileName = $"{r.Value.DatabaseName} ({r.Value.RowCount} rows)",
                        RelevantContent = r.Value.ResultData
                    })
                    .ToList();

                return new RagResponse
                {
                    Answer = answer ?? "Unable to generate answer",
                    Sources = sources
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating final answer");
                return new RagResponse
                {
                    Answer = mergedData,
                    Sources = new List<SearchSource> { new SearchSource { FileName = "Raw data (AI generation failed)" } }
                };
            }
        }

        #endregion
    }
}


