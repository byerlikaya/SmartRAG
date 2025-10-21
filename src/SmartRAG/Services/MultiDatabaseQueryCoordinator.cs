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
                    
                    // CRITICAL: Multi-Retry validation with progressive strategies
                    const int maxRetries = 3;
                    bool validationPassed = false;
                    List<string> allErrors = new List<string>();
                    
                    for (int retryAttempt = 0; retryAttempt <= maxRetries; retryAttempt++)
                    {
                        var currentErrors = new List<string>();
                        
                        // Validate #1: SQL columns exist in schema (generic validation)
                        if (!ValidateSQLColumnExistence(dbQuery.GeneratedQuery, schema, dbQuery.RequiredTables, out var columnErrors))
                        {
                            currentErrors.AddRange(columnErrors);
                        }
                        
                        // Validate #2: SQL tables exist in schema (generic validation)
                        var tableErrors = await ValidateSQLTableExistenceAsync(dbQuery.GeneratedQuery, schema, dbQuery.DatabaseName);
                        if (tableErrors.Any())
                        {
                            currentErrors.AddRange(tableErrors);
                        }
                        
                        // Validate #3: SQL syntax correctness (generic validation)
                        var syntaxErrors = ValidateSQLSyntax(dbQuery.GeneratedQuery, schema.DatabaseType);
                        if (syntaxErrors.Any())
                        {
                            currentErrors.AddRange(syntaxErrors);
                        }
                        
                        // Check if all validations passed
                        if (currentErrors.Count == 0)
                        {
                            validationPassed = true;
                            if (retryAttempt > 0)
                            {
                                _logger.LogInformation("SQL validation passed after {Attempts} retry attempt(s) for {DatabaseName}", retryAttempt, schema.DatabaseName);
                            }
                                break;
                        }
                        
                        // Validation failed - accumulate errors
                        allErrors.AddRange(currentErrors);
                        
                        if (retryAttempt == maxRetries)
                        {
                            // Max retries reached
                            _logger.LogWarning("SQL validation failed after {MaxRetries} attempts for {DatabaseName}", maxRetries, schema.DatabaseName);
                            foreach (var error in allErrors.Distinct())
                            {
                                _logger.LogWarning("  - {Error}", error);
                            }
                            break;
                        }
                        
                        // Retry with progressively stricter prompts
                        _logger.LogInformation("Retry attempt {Attempt}/{MaxRetries} for {DatabaseName}", retryAttempt + 1, maxRetries, schema.DatabaseName);
                        
                        string retryPrompt;
                        if (retryAttempt == 0)
                        {
                            // First retry: Emphasize validation errors
                            retryPrompt = BuildStricterSQLPrompt(queryIntent.OriginalQuery, dbQuery, schema, currentErrors);
                        }
                        else if (retryAttempt == 1)
                        {
                            // Second retry: Ultra-strict with ALL previous errors
                            retryPrompt = BuildUltraStrictSQLPrompt(queryIntent.OriginalQuery, dbQuery, schema, allErrors.Distinct().ToList(), retryAttempt + 1);
                        }
                        else
                        {
                            // Third retry: Simplest possible query
                            retryPrompt = BuildSimplifiedSQLPrompt(queryIntent.OriginalQuery, dbQuery, schema, allErrors.Distinct().ToList());
                        }
                        
                        var retriedSql = await _aiService.GenerateResponseAsync(retryPrompt, new List<string>());
                        var retriedExtracted = ExtractSQLFromAIResponse(retriedSql);
                        
                        if (!IsCompleteSql(retriedExtracted))
                        {
                            _logger.LogWarning("Retry {Attempt} generated incomplete SQL for {DatabaseName}", retryAttempt + 1, schema.DatabaseName);
                            continue;
                        }
                        
                        dbQuery.GeneratedQuery = retriedExtracted;
                    }
                    
                    if (!validationPassed)
                    {
                        _logger.LogWarning("Could not generate valid SQL for {DatabaseName} after {MaxRetries} attempts, skipping", schema.DatabaseName, maxRetries);
                        dbQuery.GeneratedQuery = null;
                        continue;
                    }

                    
                    // Log the generated SQL result
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

        public async Task<string> MergeResultsAsync(MultiDatabaseQueryResult queryResults, string originalQuery)
        {
            _logger.LogInformation("Merging results from {Count} databases with smart JOIN", queryResults.DatabaseResults.Count);
            
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
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("🚨 HOW TO WRITE 'PURPOSE' FIELD:");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("The 'purpose' field MUST specify WHAT DATA TYPES to retrieve:");
            sb.AppendLine();
            sb.AppendLine("✗ BAD Purpose Examples (too vague):");
            sb.AppendLine("  'Get data from table'");
            sb.AppendLine("  'Retrieve information'");
            sb.AppendLine("  'Query records'");
            sb.AppendLine();
            sb.AppendLine("✓ GOOD Purpose Examples (describes data types and patterns):");
            sb.AppendLine("  'Get TEXT columns (look for columns with NAME pattern in column name) and foreign keys'");
            sb.AppendLine("  'Get NUMERIC columns (INT/DECIMAL types for aggregation) and foreign keys'");
            sb.AppendLine("  'Get DATETIME columns (temporal data) and foreign keys'");
            sb.AppendLine("  'Get TEXT columns (classification data) and foreign keys'");
            sb.AppendLine();
            sb.AppendLine("PATTERN: 'Get [DATA_TYPE] columns ([what pattern to match in schema]) and foreign keys'");
            sb.AppendLine();
            sb.AppendLine("HOW TO MAP USER KEYWORDS TO DATA TYPES:");
            sb.AppendLine();
            sb.AppendLine("  If user asks WHO/NAME questions:");
            sb.AppendLine("  → Purpose: 'Get TEXT columns (find columns containing word NAME in their column name) and foreign keys'");
            sb.AppendLine();
            sb.AppendLine("  If user asks WHERE/LOCATION questions:");
            sb.AppendLine("  → Purpose: 'Get TEXT columns (find location-related column names) and foreign keys'");
            sb.AppendLine();
            sb.AppendLine("  If user asks NUMERIC VALUE questions:");
            sb.AppendLine("  → Purpose: 'Get NUMERIC columns (INT/DECIMAL types for calculations) and foreign keys'");
            sb.AppendLine();
            sb.AppendLine("  If user asks COUNT/QUANTITY questions:");
            sb.AppendLine("  → Purpose: 'Get NUMERIC columns (INT types for counting) and foreign keys'");
            sb.AppendLine();
            sb.AppendLine("  If user asks TIME/DATE questions:");
            sb.AppendLine("  → Purpose: 'Get DATETIME columns (DATE/TIMESTAMP types) and foreign keys'");
            sb.AppendLine();
            sb.AppendLine("  If user asks STATUS/STATE questions:");
            sb.AppendLine("  → Purpose: 'Get TEXT columns (classification/state information) and foreign keys'");
            sb.AppendLine();
            sb.AppendLine("PATTERN:");
            sb.AppendLine("  Purpose = 'Get [DATA_TYPE] columns ([description of what to look for]) and foreign keys'");
            sb.AppendLine();
            sb.AppendLine("🚨 PURPOSE MUST DESCRIBE DATA TYPES TO FIND, NOT SPECIFIC COLUMN NAMES!");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("CROSS-DATABASE DATA REQUIREMENTS:");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("If the query requires calculations using columns from multiple tables:");
            sb.AppendLine("  ✓ Identify which tables contain the required columns");
            sb.AppendLine("  ✓ Check Foreign Key relationships to understand table connections");
            sb.AppendLine("  ✓ You MUST select ALL databases that contain required data!");
            sb.AppendLine("  ✓ Set requiresCrossDatabaseJoin: true");
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
            sb.AppendLine("🚨 ABSOLUTE RULES - FOLLOW EXACTLY:");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("1. Use ONLY columns listed in schema below (case-sensitive)");
            sb.AppendLine("2. Use ONLY tables listed in your allowed tables");
            sb.AppendLine("3. DO NOT assume any column exists - check schema first");
            sb.AppendLine("4. DO NOT use table aliases (p, c, o) - use full table names");
            sb.AppendLine("5. DO NOT use parameters (@param, :param)");
            sb.AppendLine("6. DO NOT write JOIN - return foreign key IDs only");
            sb.AppendLine("7. Match database type syntax (SQL Server=TOP, Others=LIMIT)");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"TARGET DATABASE: {schema.DatabaseName} ({schema.DatabaseType})");
            sb.AppendLine($"YOUR ALLOWED TABLES: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine();
            sb.AppendLine("USER QUESTION:");
            sb.AppendLine($"  {userQuery}");
            sb.AppendLine();
            sb.AppendLine("WHAT TO RETRIEVE FROM THIS DATABASE:");
            sb.AppendLine($"  {dbQuery.Purpose}");
            sb.AppendLine();
            sb.AppendLine("🚨 CRITICAL COLUMN SELECTION:");
            sb.AppendLine($"Task: {dbQuery.Purpose}");
            sb.AppendLine();
            sb.AppendLine("MANDATORY COLUMN SELECTION RULES:");
            sb.AppendLine("1. Read your task carefully and identify keywords");
            sb.AppendLine("2. For EACH keyword, find ALL matching columns in schema");
            sb.AppendLine("3. SELECT ALL columns that match the keywords");
            sb.AppendLine("4. ALWAYS include ALL foreign key columns (columns ending with 'ID')");
            sb.AppendLine();
            sb.AppendLine("COLUMN SELECTION STRATEGY:");
            sb.AppendLine();
            sb.AppendLine("  Step 1: Analyze your task - what CONCEPTS are mentioned?");
            sb.AppendLine("  Step 2: Look at schema below and find columns matching those concepts");
            sb.AppendLine("  Step 3: SELECT ALL matching columns + ALL foreign keys");
            sb.AppendLine();
            sb.AppendLine("CONCEPT MATCHING GUIDE:");
            sb.AppendLine();
            sb.AppendLine("  Task mentions IDENTITY (who, name, person):");
            sb.AppendLine("  → Find TEXT columns: Search for column names containing pattern 'NAME'");
            sb.AppendLine();
            sb.AppendLine("  Task mentions LOCATION (where, place):");
            sb.AppendLine("  → Find TEXT columns: Search for geographic/location patterns in column names");
            sb.AppendLine();
            sb.AppendLine("  Task mentions MONETARY (how much, value):");
            sb.AppendLine("  → Find NUMERIC columns: Search for monetary/value patterns in column names");
            sb.AppendLine();
            sb.AppendLine("  Task mentions QUANTITY (how many, count):");
            sb.AppendLine("  → Find INT columns: Search for count/quantity patterns in column names");
            sb.AppendLine();
            sb.AppendLine("  Task mentions TEMPORAL (when, date, time):");
            sb.AppendLine("  → Find DATETIME columns: Search for time-related patterns in column names");
            sb.AppendLine();
            sb.AppendLine("  Task mentions CLASSIFICATION (type, category, kind):");
            sb.AppendLine("  → Find TEXT columns: Search for classification patterns in column names");
            sb.AppendLine();
            sb.AppendLine("🚨 MATCH CONCEPTS TO COLUMN NAME PATTERNS IN SCHEMA BELOW!");
            sb.AppendLine("🚨 INCLUDE: All matching columns + ALL foreign keys (ending with 'ID')!");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"TABLES AVAILABLE IN {schema.DatabaseName}:");
            sb.AppendLine("═══════════════════════════════════════");
            
            foreach (var tableName in dbQuery.RequiredTables)
            {
                var table = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                if (table != null)
                {
                    sb.AppendLine($"\n🚨 Table: {table.TableName}");
                    sb.AppendLine("═══════════════════════════════════════");
                    sb.AppendLine($"AVAILABLE COLUMNS (use EXACT names, case-sensitive):");
                    
                    var columnList = string.Join(", ", table.Columns.Select(c => c.ColumnName));
                    sb.AppendLine($"  {columnList}");
                    
                    sb.AppendLine();
                    sb.AppendLine($"🚨 YOU CAN ONLY USE THESE {table.Columns.Count} COLUMNS FROM {table.TableName}");
                    sb.AppendLine("ANY OTHER COLUMN NAME WILL CAUSE ERROR!");
                    
                    if (table.ForeignKeys.Any())
                    {
                        sb.AppendLine();
                        sb.AppendLine("Foreign Keys (must include in SELECT for data merging):");
                        foreach (var fk in table.ForeignKeys)
                        {
                            sb.AppendLine($"  {fk.ColumnName}");
                        }
                    }
                    
                    // Show example SQL for this table
                    sb.AppendLine();
                    sb.AppendLine($"  Example SQL for {table.TableName}:");
                    
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
            sb.AppendLine("TABLES NOT IN THIS DATABASE (DO NOT USE):");
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
            sb.AppendLine("HOW TO WRITE YOUR SQL QUERY:");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("STEP 1: Choose your tables");
            sb.AppendLine($"   → You can use: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine($"   → You cannot use: Any other table in {schema.DatabaseName}");
            sb.AppendLine();
            sb.AppendLine("STEP 2: Write SELECT clause");
            sb.AppendLine("   → Verify EACH column exists in the table's column list above");
            sb.AppendLine("   → Include ALL foreign key columns that exist in the table");
            sb.AppendLine("   → Include columns needed to answer the question");
            sb.AppendLine("   → Use aggregations (SUM, COUNT, AVG) if needed");
            sb.AppendLine("   → DO NOT assume a column exists - CHECK THE SCHEMA FIRST!");
            sb.AppendLine();
            sb.AppendLine("STEP 3: Write FROM clause");
            sb.AppendLine($"   ✓ FROM {dbQuery.RequiredTables[0]} (use allowed table)");
            sb.AppendLine("   ✗ FROM OtherTable (not in allowed list)");
            sb.AppendLine();
            sb.AppendLine("STEP 4: Write JOIN clause (if needed)");
            sb.AppendLine("   → JOIN between allowed tables only");
            sb.AppendLine("   → BEFORE writing ON clause, verify columns exist in BOTH tables!");
            sb.AppendLine($"   → Example: FROM {dbQuery.RequiredTables[0]} t1 JOIN {(dbQuery.RequiredTables.Count > 1 ? dbQuery.RequiredTables[1] : dbQuery.RequiredTables[0])} t2 ON t1.ID = t2.ID");
            sb.AppendLine("   → NEVER join with tables from other databases");
            sb.AppendLine("   → Using t1.NonExistentColumn or t2.NonExistentColumn will cause ERROR!");
            sb.AppendLine();
            sb.AppendLine("STEP 5: Apply filters and ordering");
            sb.AppendLine("   → WHERE, GROUP BY, ORDER BY as needed");
            sb.AppendLine("   → Use columns from allowed tables only");
            sb.AppendLine("   ✗ NEVER use aggregates (SUM, AVG, COUNT) in WHERE clause!");
            sb.AppendLine("   ✓ Use HAVING for filtering aggregated values");
            sb.AppendLine("   ✓ If using GROUP BY: ALL non-aggregate SELECT columns must be in GROUP BY");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("SIMPLE STRATEGY:");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("1. SELECT data from your allowed tables");
            sb.AppendLine("2. Always include foreign key ID columns (if they exist)");
            sb.AppendLine("3. Don't worry about data from other databases - application handles merging");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("SQL TEMPLATES TO FOLLOW:");
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
            sb.AppendLine("🚨 ABSOLUTELY FORBIDDEN - WILL CAUSE ERRORS:");
            sb.AppendLine("  ✗ JOIN with tables not in allowed list");
            sb.AppendLine("  ✗ Use DatabaseName.TableName syntax");
            sb.AppendLine("  ✗ Reference columns from non-allowed tables");
            sb.AppendLine("  ✗ EXISTS/IN subqueries with non-allowed tables");
            sb.AppendLine("  ✗ ANY reference to tables outside your allowed list");
            sb.AppendLine();
            sb.AppendLine("CRITICAL: Even in WHERE clause, subquery, or EXISTS:");
            sb.AppendLine("   You can ONLY use tables from your allowed list!");
            sb.AppendLine("   Example of FORBIDDEN patterns:");
            sb.AppendLine("     ✗ WHERE EXISTS (SELECT 1 FROM OtherTable ...)");
            sb.AppendLine("     ✗ WHERE ColumnX IN (SELECT ID FROM OtherTable)");
            sb.AppendLine("     ✗ JOIN OtherTable ON ...");
            sb.AppendLine();
            sb.AppendLine($"   YOUR ALLOWED TABLES: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine("   Use ONLY these tables - nothing else!");
            sb.AppendLine();
            sb.AppendLine("SPECIAL CASE - Cross-Database Calculations:");
            sb.AppendLine("  User query: 'Calculate total numeric value from filtered records'");
            sb.AppendLine($"  Your allowed tables: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine();
            sb.AppendLine("  ✗ WRONG APPROACH (referencing other DB's table):");
            sb.AppendLine("     SELECT SUM(t1.NumericColumn)");
            sb.AppendLine("     FROM TableA t1");
            sb.AppendLine("     JOIN TableB t2 ON t1.ForeignKeyID = t2.ID  ← ERROR! TableB not in your DB!");
            sb.AppendLine("     WHERE t2.FilterColumn = 'SpecificValue'");
            sb.AppendLine();
            sb.AppendLine("  ✗ ALSO WRONG (EXISTS with other DB's table):");
            sb.AppendLine("     SELECT SUM(t1.NumericColumn)");
            sb.AppendLine("     FROM TableA t1");
            sb.AppendLine("     WHERE EXISTS (");
            sb.AppendLine("       SELECT 1 FROM TableB t2  ← ERROR! TableB not in your DB!");
            sb.AppendLine("       WHERE t1.ForeignKeyID = t2.ID AND t2.FilterColumn = 'SpecificValue'");
            sb.AppendLine("     )");
            sb.AppendLine();
            sb.AppendLine("  ✓ CORRECT APPROACH (return FK for merging):");
            sb.AppendLine("     SELECT ForeignKeyID, SUM(NumericColumn) AS Total");
            sb.AppendLine("     FROM TableA");
            sb.AppendLine("     GROUP BY ForeignKeyID");
            sb.AppendLine("     → Application will:");
            sb.AppendLine("        1. Get filter values from TableB database using ForeignKeyID");
            sb.AppendLine("        2. Apply filtering based on values");
            sb.AppendLine("        3. Sum the totals");
            sb.AppendLine();
            
            // Database-specific syntax
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"{schema.DatabaseType} SYNTAX:");
            sb.AppendLine("═══════════════════════════════════════");
            
            switch (schema.DatabaseType)
            {
                case DatabaseType.SqlServer:
                    sb.AppendLine("🚨 SQL SERVER DATABASE - CRITICAL SYNTAX RULES");
                    sb.AppendLine();
                    sb.AppendLine("ABSOLUTELY FORBIDDEN:");
                    sb.AppendLine("✗ LIMIT keyword (does not exist in SQL Server)");
                    sb.AppendLine("✗ FETCH NEXT");
                    sb.AppendLine("✗ Table aliases (c, p, o)");
                    sb.AppendLine("✗ Parameters: @ParamName, :ParamName, ?");
                    sb.AppendLine("✗ Template syntax: <placeholder>, {variable}");
                    sb.AppendLine("✗ JOIN statements (return FK IDs only)");
                    sb.AppendLine();
                    sb.AppendLine("REQUIRED FORMAT:");
                    sb.AppendLine($"SELECT TOP 100 columns FROM {dbQuery.RequiredTables[0]} WHERE conditions ORDER BY column");
                    sb.AppendLine();
                    sb.AppendLine("CORRECT EXAMPLES:");
                    sb.AppendLine("✓ SELECT TOP 100 Column1, Column2 FROM TableName");
                    sb.AppendLine("✓ WHERE DateColumn >= DATEADD(month, -3, GETDATE())");
                    sb.AppendLine("✓ WHERE NumericColumn > 100 (use literal values, NOT @param)");
                    sb.AppendLine("✓ SELECT Column1, SUM(Column2) FROM TableName GROUP BY Column1");
                    break;
                    
                case DatabaseType.SQLite:
                    sb.AppendLine("🚨 SQLITE DATABASE - CRITICAL SYNTAX RULES");
                    sb.AppendLine();
                    sb.AppendLine("ABSOLUTELY FORBIDDEN:");
                    sb.AppendLine("✗ TOP keyword (does not exist in SQLite)");
                    sb.AppendLine("✗ Columns not in schema");
                    sb.AppendLine("✗ Parameters: @ParamName, :ParamName, ?");
                    sb.AppendLine("✗ Template syntax: <placeholder>, {variable}");
                    sb.AppendLine();
                    sb.AppendLine("REQUIRED FORMAT:");
                    sb.AppendLine($"SELECT columns FROM {dbQuery.RequiredTables[0]} WHERE conditions ORDER BY column LIMIT 100");
                    sb.AppendLine();
                    sb.AppendLine("CORRECT EXAMPLES:");
                    sb.AppendLine("✓ SELECT Column1, Column2 FROM TableName LIMIT 100");
                    sb.AppendLine("✓ WHERE DateColumn >= date('now', '-3 month')");
                    sb.AppendLine("✓ WHERE NumericColumn > 100 (use literal values, NOT ?)");
                    sb.AppendLine("✓ Use EXACT table/column casing from schema");
                    break;
                    
                case DatabaseType.MySQL:
                    sb.AppendLine("🚨 MYSQL DATABASE - CRITICAL SYNTAX RULES");
                    sb.AppendLine();
                    sb.AppendLine("ABSOLUTELY FORBIDDEN:");
                    sb.AppendLine("✗ TOP keyword (does not exist in MySQL)");
                    sb.AppendLine("✗ Parameters: @ParamName, :ParamName, ?");
                    sb.AppendLine("✗ Template syntax: <placeholder>, {variable}");
                    sb.AppendLine();
                    sb.AppendLine("CRITICAL GROUP BY RULE (MySQL strict mode):");
                    sb.AppendLine("If using GROUP BY, EVERY non-aggregate column in SELECT MUST be in GROUP BY");
                    sb.AppendLine("✗ WRONG: SELECT Col1, Col2, SUM(Col3) FROM Table GROUP BY Col1");
                    sb.AppendLine("✓ CORRECT: SELECT Col1, Col2, SUM(Col3) FROM Table GROUP BY Col1, Col2");
                    sb.AppendLine("✓ OR use only aggregates: SELECT SUM(Col1), AVG(Col2) FROM Table");
                    sb.AppendLine();
                    sb.AppendLine("REQUIRED FORMAT:");
                    sb.AppendLine($"SELECT columns FROM {dbQuery.RequiredTables[0]} WHERE conditions ORDER BY column LIMIT 100");
                    sb.AppendLine();
                    sb.AppendLine("CORRECT EXAMPLES:");
                    sb.AppendLine("✓ SELECT Column1, Column2 FROM TableName LIMIT 100");
                    sb.AppendLine("✓ WHERE DateColumn >= DATE_SUB(NOW(), INTERVAL 3 MONTH)");
                    sb.AppendLine("✓ WHERE NumericColumn > 100 (use literal values, NOT ?)");
                    break;
                    
                case DatabaseType.PostgreSQL:
                    sb.AppendLine("🚨 POSTGRESQL DATABASE - CRITICAL SYNTAX RULES");
                    sb.AppendLine();
                    sb.AppendLine("ABSOLUTELY FORBIDDEN:");
                    sb.AppendLine("✗ TOP keyword (does not exist in PostgreSQL)");
                    sb.AppendLine("✗ INTERVAL without quotes");
                    sb.AppendLine("✗ Parameters: @ParamName, :ParamName, ?");
                    sb.AppendLine("✗ Template syntax: <placeholder>, {variable}");
                    sb.AppendLine();
                    sb.AppendLine("REQUIRED FORMAT:");
                    sb.AppendLine($"SELECT columns FROM {dbQuery.RequiredTables[0]} WHERE conditions ORDER BY column LIMIT 100");
                    sb.AppendLine();
                    sb.AppendLine("CORRECT EXAMPLES:");
                    sb.AppendLine("✓ SELECT Column1, Column2 FROM TableName LIMIT 100");
                    sb.AppendLine("✓ WHERE DateColumn >= CURRENT_DATE - INTERVAL '3 months'");
                    sb.AppendLine("✓ WHERE DateColumn >= NOW() - INTERVAL '30 days'");
                    sb.AppendLine("✓ WHERE NumericColumn > 100 (use literal values, NOT $1)");
                    sb.AppendLine();
                    sb.AppendLine("DATE/TIME ARITHMETIC:");
                    sb.AppendLine("✗ WRONG: INTERVAL 30 DAYS (no quotes)");
                    sb.AppendLine("✓ CORRECT: INTERVAL '30 days' (with quotes!)");
                    break;
            }
            
            sb.AppendLine();
            sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║                   CRITICAL OUTPUT RULES                        ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("🚨 LANGUAGE-CRITICAL RULE:");
            sb.AppendLine("   SQL is a COMPUTER LANGUAGE - it ONLY understands SQL keywords!");
            sb.AppendLine("   ✗ NEVER write Turkish, German, Russian, or any human language in SQL");
            sb.AppendLine("   ✗ NEVER write comments or explanations in SQL");
            sb.AppendLine("   ✗ NEVER translate SQL keywords to other languages");
            sb.AppendLine("   ✓ ONLY use English SQL keywords: SELECT, FROM, WHERE, JOIN, etc.");
            sb.AppendLine();
            sb.AppendLine("   BAD EXAMPLES (will cause syntax errors):");
            sb.AppendLine("   ✗ 'Bu sorgu, ürünleri seçer' (Turkish text in SQL)");
            sb.AppendLine("   ✗ 'Diese Abfrage wählt Produkte' (German text in SQL)");
            sb.AppendLine("   ✗ 'Этот запрос выбирает продукты' (Russian text in SQL)");
            sb.AppendLine("   ✗ SELECT * FROM TableA -- This selects data");
            sb.AppendLine();
            sb.AppendLine("   GOOD EXAMPLE:");
            sb.AppendLine("   ✓ SELECT Column1, Column2 FROM TableA");
            sb.AppendLine("   (Pure SQL only, no comments, no human language text!)");
            sb.AppendLine();
            sb.AppendLine("DO NOT WRITE:");
            sb.AppendLine("   • 'Here is the SQL query...'");
            sb.AppendLine("   • 'This query...'");
            sb.AppendLine("   • 'The key points are...'");
            sb.AppendLine("   • ANY explanations, descriptions, or comments");
            sb.AppendLine("   • Markdown code blocks (```)");
            sb.AppendLine("   • ANY non-English text");
            sb.AppendLine("   • ANY SQL comments (-- or /* */)");
            sb.AppendLine();
            sb.AppendLine("Example of CORRECT output:");
            if (schema.DatabaseType == DatabaseType.SqlServer)
            {
                sb.AppendLine($"   ✓ SELECT TOP 100 Column1, Column2, ForeignKeyColumn FROM {dbQuery.RequiredTables[0]}");
            }
            else
            {
                sb.AppendLine($"   ✓ SELECT Column1, Column2, ForeignKeyColumn FROM {dbQuery.RequiredTables[0]} LIMIT 100");
            }
            sb.AppendLine();
            sb.AppendLine("Example of WRONG output:");
            sb.AppendLine("   ✗ Here is the SQL query: SELECT ...");
            sb.AppendLine("   (No text before SQL!)");
            sb.AppendLine();
            sb.AppendLine("Example of INCOMPLETE SQL:");
            sb.AppendLine("   ✗ SELECT Column1 FROM TableA ORDER BY");
            sb.AppendLine("   (ORDER BY must have column names!)");
            sb.AppendLine();
            sb.AppendLine("CRITICAL COMPLETENESS RULES:");
            sb.AppendLine("  - ORDER BY clause MUST include column name(s)");
            sb.AppendLine("  - GROUP BY clause MUST include column name(s)");
            sb.AppendLine("  - JOIN clause MUST include ON condition");
            sb.AppendLine("  - SQL MUST be complete and executable");
            sb.AppendLine();
            sb.AppendLine("FINAL REMINDER:");
            sb.AppendLine($"  - ALLOWED TABLES: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine("  - DO NOT use any other tables in FROM, JOIN, WHERE, or subqueries!");
            sb.AppendLine("  - Your response must START with SELECT, not with any text!");
            sb.AppendLine();
            sb.AppendLine("LANGUAGE ENFORCEMENT:");
            sb.AppendLine("  - SQL is ENGLISH-ONLY computer language");
            sb.AppendLine("  - Even if user question is in Turkish/German/Russian:");
            sb.AppendLine("    ✓ SQL must still be pure English SQL");
            sb.AppendLine("    ✗ NO Turkish/German/Russian text in SQL output");
            sb.AppendLine("  - Example: User asks 'Müşterileri göster'");
            sb.AppendLine("    ✓ Correct: SELECT * FROM TableA");
            sb.AppendLine("    ✗ Wrong: Bu sorgu verileri seçer: SELECT * FROM TableA");
            sb.AppendLine();
            sb.AppendLine("YOUR RESPONSE = SQL QUERY ONLY (starts with SELECT, pure English SQL, no text!)");

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

        /// <summary>
        /// Builds a stricter SQL generation prompt after validation failures (generic approach)
        /// </summary>
        private string BuildStricterSQLPrompt(string userQuery, DatabaseQueryIntent dbQuery, DatabaseSchemaInfo schema, List<string> previousErrors)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║       SQL REGENERATION - PREVIOUS ATTEMPT HAD ERRORS           ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("🚨 CRITICAL: Your previous SQL had these errors:");
            
            // Categorize errors for better understanding
            var columnErrors = previousErrors.Where(e => e.Contains("Column") || e.Contains("column")).ToList();
            var tableErrors = previousErrors.Where(e => e.Contains("Table") || e.Contains("table") && !e.Contains("Column")).ToList();
            var syntaxErrors = previousErrors.Where(e => e.Contains("syntax") || e.Contains("aggregate") || e.Contains("GROUP BY") || e.Contains("WHERE")).ToList();
            var otherErrors = previousErrors.Except(columnErrors).Except(tableErrors).Except(syntaxErrors).ToList();
            
            if (columnErrors.Any())
            {
                sb.AppendLine();
                sb.AppendLine("  COLUMN ERRORS:");
                foreach (var error in columnErrors)
                {
                    sb.AppendLine($"     ✗ {error}");
                }
            }
            
            if (tableErrors.Any())
            {
                sb.AppendLine();
                sb.AppendLine("  TABLE ERRORS:");
                foreach (var error in tableErrors)
                {
                    sb.AppendLine($"     ✗ {error}");
                }
            }
            
            if (syntaxErrors.Any())
            {
                sb.AppendLine();
                sb.AppendLine("  SQL SYNTAX ERRORS:");
                foreach (var error in syntaxErrors)
                {
                    sb.AppendLine($"     ✗ {error}");
                }
            }
            
            if (otherErrors.Any())
            {
                sb.AppendLine();
                sb.AppendLine("  OTHER ERRORS:");
                foreach (var error in otherErrors)
                {
                    sb.AppendLine($"     ✗ {error}");
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("AVAILABLE COLUMNS (USE ONLY THESE):");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            
            foreach (var tableName in dbQuery.RequiredTables)
            {
                var table = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                if (table != null)
                {
                    sb.AppendLine();
                    sb.AppendLine($"Table: {table.TableName}");
                    sb.AppendLine($"Available Columns:");
                    foreach (var col in table.Columns)
                    {
                        sb.AppendLine($"  {col.ColumnName} ({col.DataType})");
                    }
                    sb.AppendLine($"🚨 ANY OTHER COLUMN IN {table.TableName} WILL CAUSE ERROR!");
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("RULES - READ CAREFULLY:");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("COLUMN RULES:");
            sb.AppendLine("1. ONLY use columns listed above");
            sb.AppendLine("2. If a column doesn't exist, DO NOT use it");
            sb.AppendLine("3. Check column names character-by-character");
            sb.AppendLine("4. Use exact casing from the list above");
            sb.AppendLine("5. Before writing JOIN, verify both tables have the referenced columns");
            sb.AppendLine();
            sb.AppendLine("SQL SYNTAX RULES:");
            sb.AppendLine("6. ✗ NEVER use aggregates (SUM, AVG, COUNT) in WHERE clause");
            sb.AppendLine("7. ✓ Use HAVING clause for filtering aggregates");
            sb.AppendLine("8. ✓ In GROUP BY queries: SELECT only grouped columns or aggregates");
            sb.AppendLine("9. ✓ Every non-aggregate column in SELECT must be in GROUP BY");
            sb.AppendLine();
            sb.AppendLine("EXAMPLES:");
            sb.AppendLine("  ✗ WHERE SUM(Amount) > 100  (WRONG)");
            sb.AppendLine("  ✓ HAVING SUM(Amount) > 100 (CORRECT)");
            sb.AppendLine();
            sb.AppendLine("  ✗ SELECT Col1, Col2, SUM(Col3) GROUP BY Col1  (WRONG - Col2 not in GROUP BY)");
            sb.AppendLine("  ✓ SELECT Col1, SUM(Col3) GROUP BY Col1        (CORRECT)");
            sb.AppendLine();
            sb.AppendLine($"User Query: {userQuery}");
            sb.AppendLine($"Purpose: {dbQuery.Purpose}");
            sb.AppendLine();
            sb.AppendLine("LANGUAGE RULE:");
            sb.AppendLine("   SQL must be PURE ENGLISH - NO Turkish/German/Russian text!");
            sb.AppendLine("   ✗ Do NOT write: 'Bu sorgu', 'Diese Abfrage', 'Этот запрос'");
            sb.AppendLine("   ✓ Only write: SELECT, FROM, WHERE, etc. (English SQL keywords)");
            sb.AppendLine();
            sb.AppendLine("Generate a valid SQL query using ONLY the columns listed above.");
            sb.AppendLine("Follow SQL syntax rules strictly.");
            sb.AppendLine("Output ONLY pure English SQL query, no explanations, no comments.");
            
            return sb.ToString();
        }

        /// <summary>
        /// Builds an ultra-strict prompt for second retry attempt (even more emphasis on validation)
        /// </summary>
        private string BuildUltraStrictSQLPrompt(string userQuery, DatabaseQueryIntent dbQuery, DatabaseSchemaInfo schema, List<string> allPreviousErrors, int attemptNumber)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
            sb.AppendLine($"║     RETRY ATTEMPT #{attemptNumber} - ULTRA STRICT MODE        ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("🚨 CRITICAL: ALL previous attempts FAILED with these errors:");
            foreach (var error in allPreviousErrors)
            {
                sb.AppendLine($"   ✗ {error}");
            }
            sb.AppendLine();
            sb.AppendLine("YOU MUST FIX THESE ERRORS NOW!");
            sb.AppendLine();
            
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("EXACT COLUMN LIST - NO OTHER COLUMNS EXIST:");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            
            foreach (var tableName in dbQuery.RequiredTables)
            {
                var table = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                if (table != null)
                {
                    sb.AppendLine();
                    sb.AppendLine($"━━━ TABLE: {table.TableName} ━━━");
                    sb.AppendLine($"COMPLETE COLUMN LIST ({table.Columns.Count} columns):");
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        var col = table.Columns[i];
                        sb.AppendLine($"  {i + 1}. {col.ColumnName} ({col.DataType})");
                    }
                    sb.AppendLine();
                    sb.AppendLine($"🚨 THESE ARE THE ONLY {table.Columns.Count} COLUMNS IN {table.TableName}!");
                    sb.AppendLine($"ANY OTHER COLUMN NAME = INSTANT ERROR!");
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("MANDATORY CHECKLIST BEFORE WRITING SQL:");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("COLUMN CHECKS:");
            sb.AppendLine("□ Did I verify EVERY column exists in the exact list above?");
            sb.AppendLine("□ Did I check spelling character-by-character?");
            sb.AppendLine("□ Did I avoid assuming column names?");
            sb.AppendLine("□ Did I use ONLY columns from the numbered list?");
            sb.AppendLine();
            sb.AppendLine("SQL SYNTAX CHECKS:");
            sb.AppendLine("□ Did I avoid aggregates (SUM, AVG, COUNT) in WHERE clause?");
            sb.AppendLine("□ Did I use HAVING for aggregate filtering?");
            sb.AppendLine("□ If using GROUP BY: Are ALL non-aggregate SELECT columns in GROUP BY?");
            sb.AppendLine("□ Are parentheses balanced?");
            sb.AppendLine();
            sb.AppendLine("COMMON MISTAKES TO AVOID:");
            sb.AppendLine("  ✗ WHERE SUM(...) > value     → Use HAVING");
            sb.AppendLine("  ✗ WHERE AVG(...) > value     → Use HAVING");
            sb.AppendLine("  ✗ SELECT A, B, SUM(C) GROUP BY A  → Add B to GROUP BY");
            sb.AppendLine();
            sb.AppendLine($"Query: {userQuery}");
            sb.AppendLine($"Task: {dbQuery.Purpose}");
            sb.AppendLine();
            sb.AppendLine("CRITICAL: SQL must be PURE ENGLISH - NO Turkish/German/Russian text!");
            sb.AppendLine("Write the SQL query. Triple-check EVERY column name AND syntax before outputting.");
            sb.AppendLine("Output format: Pure English SQL only, no text, no comments.");
            
            return sb.ToString();
        }

        /// <summary>
        /// Builds a simplified prompt for final retry (simplest possible query)
        /// </summary>
        private string BuildSimplifiedSQLPrompt(string userQuery, DatabaseQueryIntent dbQuery, DatabaseSchemaInfo schema, List<string> allPreviousErrors)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║    FINAL ATTEMPT - SIMPLIFIED QUERY STRATEGY                   ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("Previous complex queries failed. Let's simplify.");
            sb.AppendLine();
            sb.AppendLine("Previous errors:");
            foreach (var error in allPreviousErrors.Take(3))
            {
                sb.AppendLine($"  - {error}");
            }
            sb.AppendLine();
            
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("SIMPLIFIED STRATEGY:");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("Write the SIMPLEST possible query that:");
            sb.AppendLine("1. Uses ONLY the first table in the list");
            sb.AppendLine("2. SELECTs ALL columns using: SELECT * FROM TableName");
            sb.AppendLine("3. NO JOINs, NO complex WHERE clauses");
            sb.AppendLine("4. NO aggregates in WHERE (use HAVING if needed)");
            sb.AppendLine("5. NO GROUP BY issues - keep it simple");
            sb.AppendLine("6. Just return basic data for merging");
            sb.AppendLine();
            
            sb.AppendLine("Available tables:");
            foreach (var tableName in dbQuery.RequiredTables)
            {
                var table = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                if (table != null)
                {
                    sb.AppendLine($"  • {table.TableName} ({table.Columns.Count} columns)");
                }
            }
            
            sb.AppendLine();
            sb.AppendLine($"SIMPLEST QUERY TEMPLATE:");
            if (schema.DatabaseType == DatabaseType.SqlServer)
            {
                sb.AppendLine($"  SELECT TOP 100 * FROM {dbQuery.RequiredTables[0]}");
            }
            else
            {
                sb.AppendLine($"  SELECT * FROM {dbQuery.RequiredTables[0]} LIMIT 100");
            }
            
            sb.AppendLine();
            sb.AppendLine("🚨 CRITICAL: Write pure ENGLISH SQL - NO Turkish/German/Russian words!");
            sb.AppendLine("Write a simple query like above. No complexity.");
            sb.AppendLine("Output: Pure English SQL only, no text, no comments.");
            
            return sb.ToString();
        }

        /// <summary>
        /// Validates SQL syntax for common errors (generic validation)
        /// </summary>
        private List<string> ValidateSQLSyntax(string sql, DatabaseType databaseType)
        {
            var errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(sql))
            {
                return errors;
            }

            try
            {
                var sqlUpper = sql.ToUpperInvariant();
                
                // 0. Check for non-English text in SQL (Turkish, German, Russian, etc.)
                var nonEnglishPatterns = new[]
                {
                    // Turkish characters
                    "ç", "ğ", "ı", "ö", "ş", "ü", "Ç", "Ğ", "İ", "Ö", "Ş", "Ü",
                    // German umlauts
                    "ä", "ö", "ü", "ß", "Ä", "Ö", "Ü",
                    // Russian Cyrillic
                    "а", "б", "в", "г", "д", "е", "ж", "з", "и", "к", "л", "м", "н", "о", "п", "р", "с", "т", "у", "ф", "х", "ц", "ч", "ш", "щ", "ъ", "ы", "ь", "э", "ю", "я",
                    "А", "Б", "В", "Г", "Д", "Е", "Ж", "З", "И", "К", "Л", "М", "Н", "О", "П", "Р", "С", "Т", "У", "Ф", "Х", "Ц", "Ч", "Ш", "Щ", "Ъ", "Ы", "Ь", "Э", "Ю", "Я"
                };
                
                foreach (var pattern in nonEnglishPatterns)
                {
                    if (sql.Contains(pattern))
                    {
                        errors.Add($"Non-English character detected in SQL: '{pattern}'. SQL must use only English characters and SQL keywords.");
                        break;
                    }
                }
                
                // Also check for common Turkish/German/Russian SQL keywords
                var nonEnglishKeywords = new[]
                {
                    "sorgu", "seçer", "tablo", "kolon", // Turkish
                    "abfrage", "wählt", "tabelle", "spalte", // German
                    "запрос", "выбирает", "таблица", "столбец" // Russian
                };
                
                foreach (var keyword in nonEnglishKeywords)
                {
                    if (sqlUpper.Contains(keyword.ToUpperInvariant()))
                    {
                        errors.Add($"Non-English keyword detected in SQL: '{keyword}'. SQL must use only English SQL keywords (SELECT, FROM, WHERE, etc.).");
                        break;
                    }
                }
                
                // 1. Check for aggregate functions in WHERE clause (common error)
                if (sqlUpper.Contains("WHERE"))
                {
                    var whereClausePattern = @"WHERE\s+(.+?)(?:GROUP\s+BY|ORDER\s+BY|LIMIT|OFFSET|$)";
                    var whereMatch = Regex.Match(sqlUpper, whereClausePattern, RegexOptions.Singleline);
                    
                    if (whereMatch.Success)
                    {
                        var whereClause = whereMatch.Groups[1].Value;
                        
                        // Check for aggregate functions: SUM, AVG, COUNT, MAX, MIN
                        var aggregates = new[] { "SUM(", "AVG(", "COUNT(", "MAX(", "MIN(" };
                        foreach (var agg in aggregates)
                        {
                            if (whereClause.Contains(agg))
                            {
                                errors.Add($"Aggregate function in WHERE clause detected. Use HAVING instead of WHERE for aggregates.");
                                break;
                            }
                        }
                    }
                }
                
                // 2. Check for HAVING without GROUP BY or aggregate (SQLite error)
                if (sqlUpper.Contains("HAVING"))
                {
                    // HAVING requires either GROUP BY or aggregate in SELECT
                    var hasGroupBy = sqlUpper.Contains("GROUP BY");
                    var hasAggregate = sqlUpper.Contains("SUM(") || sqlUpper.Contains("AVG(") || 
                                      sqlUpper.Contains("COUNT(") || sqlUpper.Contains("MAX(") || 
                                      sqlUpper.Contains("MIN(");
                    
                    if (!hasGroupBy && !hasAggregate)
                    {
                        errors.Add("HAVING clause without GROUP BY or aggregate function. Remove HAVING or add GROUP BY.");
                    }
                }
                
                // 3. Check for GROUP BY without aggregate (might cause HAVING errors)
                if (sqlUpper.Contains("GROUP BY") && !sqlUpper.Contains("SUM(") && 
                    !sqlUpper.Contains("AVG(") && !sqlUpper.Contains("COUNT(") && 
                    !sqlUpper.Contains("MAX(") && !sqlUpper.Contains("MIN("))
                {
                    // If there's HAVING too, this is definitely wrong
                    if (sqlUpper.Contains("HAVING"))
                    {
                        errors.Add("GROUP BY with HAVING but no aggregate function in SELECT. Add aggregate or remove HAVING.");
                    }
                }
                
                // 4. Database-specific forbidden keywords
                if (databaseType == DatabaseType.SqlServer)
                {
                    if (sqlUpper.Contains("LIMIT"))
                    {
                        errors.Add("LIMIT keyword is not valid in SQL Server. Use TOP instead: SELECT TOP 100 ...");
                    }
                    if (sqlUpper.Contains("FETCH NEXT"))
                    {
                        errors.Add("FETCH NEXT is not allowed. Use TOP instead: SELECT TOP 100 ...");
                    }
                }
                else if (databaseType == DatabaseType.SQLite || databaseType == DatabaseType.MySQL || databaseType == DatabaseType.PostgreSQL)
                {
                    if (sqlUpper.Contains(" TOP ") || sqlUpper.Contains("TOP\t"))
                    {
                        errors.Add($"TOP keyword is not valid in {databaseType}. Use LIMIT instead: ...LIMIT 100");
                    }
                }
                
                // 5. Check for table aliases (c.Column, p.Column, o.Column patterns)
                var aliasPattern = @"\b[a-z]\.[A-Za-z_][A-Za-z0-9_]*";
                if (Regex.IsMatch(sql, aliasPattern))
                {
                    errors.Add("Table aliases detected (like c.Column, p.Column). Use full table names instead: TableName.Column");
                }
                
                // 6. Check for basic syntax issues
                var openParens = sql.Count(c => c == '(');
                var closeParens = sql.Count(c => c == ')');
                if (openParens != closeParens)
                {
                    errors.Add($"Mismatched parentheses: {openParens} opening, {closeParens} closing");
                }

                return errors;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating SQL syntax (continuing anyway)");
                return errors;
            }
        }

        /// <summary>
        /// Validates that all tables referenced in SQL exist in the schema (generic validation)
        /// </summary>
        private async Task<List<string>> ValidateSQLTableExistenceAsync(string sql, DatabaseSchemaInfo schema, string databaseName)
        {
            var errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(sql) || schema == null)
            {
                return errors;
            }

            try
            {
                var availableTableNames = schema.Tables.Select(t => t.TableName).ToList();
                var sqlUpper = sql.ToUpperInvariant();
                
                // Common SQL keywords to skip
                var sqlKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "SELECT", "FROM", "WHERE", "JOIN", "LEFT", "RIGHT", "INNER", "OUTER", "CROSS",
                    "ON", "AND", "OR", "NOT", "IN", "EXISTS", "AS", "ORDER", "GROUP", "HAVING",
                    "LIMIT", "TOP", "OFFSET", "UNION", "EXCEPT", "INTERSECT", "DISTINCT", "ALL",
                    "THE", "THIS", "THAT", "THESE", "THOSE", "WITH", "WITHOUT", "BETWEEN",
                    "LIKE", "IS", "NULL", "TRUE", "FALSE", "CASE", "WHEN", "THEN", "ELSE", "END"
                };
                
                // 1. Check for cross-database references (DatabaseName.TableName)
                if (sqlUpper.Contains("."))
                {
                    var allSchemas = await _schemaAnalyzer.GetAllSchemasAsync();
                    foreach (var otherSchema in allSchemas)
                    {
                        if (otherSchema.DatabaseId != schema.DatabaseId)
                        {
                            var crossDbPattern = $"{otherSchema.DatabaseName.ToUpperInvariant()}.";
                            if (sqlUpper.Contains(crossDbPattern))
                            {
                                errors.Add($"Cross-database reference: {otherSchema.DatabaseName}.TableName not allowed");
                                break;
                            }
                        }
                    }
                }
                
                // 2. Parse SQL to find table references
                var words = sqlUpper.Split(new[] { ' ', '\n', '\r', '\t', ',', '(', ')', ';', '`', '[', ']', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries);
                
                for (int i = 0; i < words.Length; i++)
                {
                    // Check words after FROM or JOIN
                    if ((words[i] == "FROM" || words[i] == "JOIN") && i + 1 < words.Length)
                    {
                        var potentialTableName = words[i + 1]
                            .TrimEnd(';', ',', ')', '(', '`', '[', ']', '"', '\'')
                            .TrimStart('`', '[', ']', '"', '\'');
                        
                        // Skip SQL keywords
                        if (potentialTableName.Length < 2 || sqlKeywords.Contains(potentialTableName))
                        {
                            continue;
                        }
                        
                        // Check if contains dot (schema.table or database.table)
                        if (potentialTableName.Contains("."))
                        {
                            errors.Add($"Qualified table reference '{potentialTableName}' not allowed");
                            continue;
                        }
                        
                        // Check if this table exists in current database
                        if (!availableTableNames.Any(t => t.Equals(potentialTableName, StringComparison.OrdinalIgnoreCase)))
                        {
                            errors.Add($"Table '{potentialTableName}' doesn't exist in {databaseName}. Available: {string.Join(", ", availableTableNames)}");
                        }
                    }
                }
                
                // 3. Check for tables in subqueries
                var subqueryPattern = @"\(\s*SELECT\s+.+?\s+FROM\s+(\w+)";
                var subqueryMatches = Regex.Matches(sqlUpper, subqueryPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                
                foreach (Match match in subqueryMatches)
                {
                    var tableName = match.Groups[1].Value;
                    
                    if (tableName.Length > 1 && 
                        tableName != "SELECT" && 
                        !availableTableNames.Any(t => t.Equals(tableName, StringComparison.OrdinalIgnoreCase)))
                    {
                        errors.Add($"Subquery references table '{tableName}' which doesn't exist in {databaseName}");
                    }
                }

                return errors;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating SQL table existence (continuing anyway)");
                return errors;
            }
        }

        /// <summary>
        /// Validates that all columns referenced in SQL exist in the schema (generic validation with alias support)
        /// </summary>
        private bool ValidateSQLColumnExistence(string sql, DatabaseSchemaInfo schema, List<string> allowedTables, out List<string> errors)
        {
            errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(sql) || schema == null || allowedTables == null || !allowedTables.Any())
            {
                return true; // Skip validation if data is invalid
            }

            try
            {
                // Get all available columns from allowed tables
                var availableColumns = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                
                foreach (var tableName in allowedTables)
                {
                    var table = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                    if (table != null)
                    {
                        var columnSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var col in table.Columns)
                        {
                            columnSet.Add(col.ColumnName);
                        }
                        availableColumns[table.TableName] = columnSet;
                    }
                }

                // Parse SQL to extract table aliases (FROM/JOIN TableName alias)
                var aliasToTable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                
                // Pattern: FROM/JOIN TableName alias
                var aliasPattern = @"(?:FROM|JOIN)\s+([a-zA-Z_][a-zA-Z0-9_]*)\s+(?:AS\s+)?([a-zA-Z_][a-zA-Z0-9_]*)\b";
                var aliasMatches = Regex.Matches(sql, aliasPattern, RegexOptions.IgnoreCase);
                
                foreach (Match aliasMatch in aliasMatches)
                {
                    if (aliasMatch.Groups.Count >= 3)
                    {
                        var tableName = aliasMatch.Groups[1].Value;
                        var alias = aliasMatch.Groups[2].Value;
                        
                        // Check if this is one of our allowed tables
                        var matchingTable = allowedTables.FirstOrDefault(t => 
                            t.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                        
                        if (matchingTable != null && !string.IsNullOrEmpty(alias))
                        {
                            // Skip SQL keywords that might be mistaken as aliases
                            var sqlKeywords = new[] { "ON", "WHERE", "AND", "OR", "GROUP", "ORDER", "HAVING", "LIMIT", "OFFSET", "SET" };
                            if (!sqlKeywords.Contains(alias.ToUpperInvariant()))
                            {
                                aliasToTable[alias] = matchingTable;
                            }
                        }
                    }
                }

                // Parse SQL to find column references (alias.Column or Table.Column)
                var columnPattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\.\s*([a-zA-Z_][a-zA-Z0-9_]*)\b";
                var matches = Regex.Matches(sql, columnPattern, RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    if (match.Groups.Count >= 3)
                    {
                        var tableOrAlias = match.Groups[1].Value;
                        var columnName = match.Groups[2].Value;

                        // First, check if it's an alias
                        string actualTableName = null;
                        if (aliasToTable.ContainsKey(tableOrAlias))
                        {
                            actualTableName = aliasToTable[tableOrAlias];
                        }
                        else
                        {
                            // Check if this is a direct table reference
                            actualTableName = availableColumns.Keys.FirstOrDefault(t => 
                                t.Equals(tableOrAlias, StringComparison.OrdinalIgnoreCase));
                        }

                        if (actualTableName != null && availableColumns.ContainsKey(actualTableName))
                        {
                            // Validate column exists in table
                            if (!availableColumns[actualTableName].Contains(columnName))
                            {
                                var availableCols = string.Join(", ", availableColumns[actualTableName].Take(10));
                                if (availableColumns[actualTableName].Count > 10)
                                {
                                    availableCols += ", ...";
                                }
                                errors.Add($"Column '{columnName}' does not exist in table '{actualTableName}' (alias: '{tableOrAlias}'). Available columns: {availableCols}");
                            }
                        }
                    }
                }

                return errors.Count == 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating SQL column existence (continuing anyway)");
                return true; // Don't block SQL execution on validation errors
            }
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

        #region Smart Merging Helper Methods

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
                
                _logger.LogInformation("Smart merge completed: {RowCount} merged rows", merged?.Rows.Count ?? 0);
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
            
            _logger.LogInformation("Selected join column: {JoinColumn} (found in {Count} databases)", 
                bestJoinColumn, commonJoinColumns.First().Value.Count);
            
            // Build joinable list with the selected column
            foreach (var kvp in parsedResults)
            {
                var result = kvp.Value;
                
                if (result.Columns.Any(col => col.Equals(bestJoinColumn, StringComparison.OrdinalIgnoreCase)))
                {
                    joinable.Add((result, bestJoinColumn));
                    _logger.LogInformation("{DatabaseName} can join on column: {JoinColumn}", result.DatabaseName, bestJoinColumn);
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
            
            _logger.LogInformation("INNER JOIN completed: {BaseRows} base rows → {MergedRows} merged rows",
                baseResult.Rows.Count, merged.Rows.Count);
            
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


