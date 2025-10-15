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
using System.Threading.Tasks;

namespace SmartRAG.Services
{
    /// <summary>
    /// Coordinates intelligent multi-database queries using AI
    /// </summary>
    public class MultiDatabaseQueryCoordinator : IMultiDatabaseQueryCoordinator
    {
        private readonly IDatabaseConnectionManager _connectionManager;
        private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer;
        private readonly IDatabaseParserService _databaseParser;
        private readonly IAIService _aiService;
        private readonly ILogger<MultiDatabaseQueryCoordinator> _logger;

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
                    queryIntent.Confidence = 0.0;
                    return queryIntent;
                }

                // Build AI prompt for query analysis
                var prompt = BuildQueryAnalysisPrompt(userQuery, schemas);

                // Get AI analysis
                var aiResponse = await _aiService.GenerateResponseAsync(prompt, new List<string>());
                
                // Debug: Log AI response
                _logger.LogInformation("AI Response (first 500 chars): {Response}", 
                    aiResponse?.Substring(0, Math.Min(500, aiResponse?.Length ?? 0)) ?? "NULL");

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
                queryIntent.Confidence = 0.0;
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
                    dbQuery.GeneratedQuery = ExtractSQLFromAIResponse(sql);
                    
                    // CRITICAL: Validate SQL doesn't use tables that don't exist in this database
                    var availableTableNames = schema.Tables.Select(t => t.TableName).ToList();
                    var sqlUpper = dbQuery.GeneratedQuery.ToUpper();
                    
                    // Check for JOIN keyword followed by table names not in this database
                    bool hasInvalidJoin = false;
                    
                    foreach (var table in availableTableNames)
                    {
                        var tableUpper = table.ToUpper();
                        // This table is OK, it exists in this database
                    }
                    
                    // Parse SQL to find all table names (basic parsing)
                    var words = sqlUpper.Split(new[] { ' ', '\n', '\r', '\t', ',', '(', ')', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    for (int i = 0; i < words.Length; i++)
                    {
                        // Check words after FROM or JOIN
                        if ((words[i] == "FROM" || words[i] == "JOIN") && i + 1 < words.Length)
                        {
                            var potentialTableName = words[i + 1].TrimEnd(';', ',', ')', '(');
                            
                            // Skip table aliases and keywords
                            if (potentialTableName.Length < 2 || 
                                potentialTableName == "SELECT" || 
                                potentialTableName == "WHERE" ||
                                potentialTableName == "ON")
                            {
                                continue;
                            }
                            
                            // Check if this table exists in current database
                            if (!availableTableNames.Any(t => t.ToUpper() == potentialTableName))
                            {
                                _logger.LogError("❌ CRITICAL: Generated SQL uses table '{Table}' but it doesn't exist in {Database}!", 
                                    potentialTableName, dbQuery.DatabaseName);
                                _logger.LogError("   Available tables: {Tables}", string.Join(", ", availableTableNames));
                                _logger.LogError("   Generated SQL: {SQL}", dbQuery.GeneratedQuery);
                                
                                hasInvalidJoin = true;
                                break;
                            }
                        }
                    }
                    
                    if (hasInvalidJoin)
                    {
                        // Invalid SQL - skip this database but try to find tables in other databases
                        _logger.LogWarning("⚠️  Skipping {Database} - SQL uses tables that don't exist in this database", 
                            dbQuery.DatabaseName);
                        
                        // Extract table names from failed SQL and find them in other databases
                        var missingTableNames = new List<string>();
                        for (int i = 0; i < words.Length; i++)
                        {
                            if ((words[i] == "FROM" || words[i] == "JOIN") && i + 1 < words.Length)
                            {
                                var potentialTableName = words[i + 1].TrimEnd(';', ',', ')', '(');
                                if (!availableTableNames.Any(t => t.ToUpper() == potentialTableName))
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
                            additionalQuery.GeneratedQuery = ExtractSQLFromAIResponse(sql);
                            
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
                var maxRows = connection.MaxRowsPerQuery > 0 ? connection.MaxRowsPerQuery : 100;
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
                sb.AppendLine($"TABLES AVAILABLE IN {schema.DatabaseName.ToUpper()} DATABASE:");
                
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
            sb.AppendLine("Generate a SQL query to answer the user's question.");
            sb.AppendLine();
            sb.AppendLine($"User Query: {userQuery}");
            sb.AppendLine($"Database Type: {schema.DatabaseType}");
            sb.AppendLine($"Purpose: {dbQuery.Purpose}");
            sb.AppendLine();
            sb.AppendLine("Required Tables:");
            
            foreach (var tableName in dbQuery.RequiredTables)
            {
                var table = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                if (table != null)
                {
                    sb.AppendLine($"\nTable: {table.TableName}");
                    sb.AppendLine($"Columns: {string.Join(", ", table.Columns.Select(c => $"{c.ColumnName} ({c.DataType})"))}");
                    
                    if (table.ForeignKeys.Any())
                    {
                        sb.AppendLine("Foreign Keys:");
                        foreach (var fk in table.ForeignKeys)
                        {
                            sb.AppendLine($"  {fk.ColumnName} -> {fk.ReferencedTable}.{fk.ReferencedColumn}");
                        }
                    }

                    if (!string.IsNullOrEmpty(table.SampleData))
                    {
                        sb.AppendLine($"Sample Data:\n{table.SampleData.Substring(0, Math.Min(200, table.SampleData.Length))}");
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("🚨 CRITICAL SQL GENERATION RULES:");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"You are generating SQL for ONLY the {schema.DatabaseName} database.");
            sb.AppendLine($"Available tables in THIS database: {string.Join(", ", schema.Tables.Select(t => t.TableName))}");
            sb.AppendLine();
            sb.AppendLine("CRITICAL RESTRICTIONS:");
            sb.AppendLine($"1. ALLOWED TABLES: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine("2. You can ONLY use tables from the list above");
            sb.AppendLine("3. You can ONLY JOIN between tables in the list above");
            sb.AppendLine("4. If you need data from a table not in the list, DO NOT include it in SQL");
            sb.AppendLine("5. For foreign key columns, SELECT the ID values - application will resolve them");
            sb.AppendLine();
            sb.AppendLine("⚠️ NEVER JOIN WITH TABLES NOT IN ALLOWED LIST - THIS WILL FAIL!");
            sb.AppendLine();
            sb.AppendLine("Cross-database data strategy:");
            sb.AppendLine("- Each database query returns partial data with reference IDs");
            sb.AppendLine("- Application merges results from all databases automatically");
            sb.AppendLine("- Your job: Return relevant IDs and data from THIS database only");
            sb.AppendLine();
            sb.AppendLine("Cross-database Example:");
            sb.AppendLine("❌ WRONG: SELECT p.ProductName FROM OrderDetails od JOIN Products p ON od.ProductID = p.ProductID");
            sb.AppendLine("   (Products table doesn't exist in this database!)");
            sb.AppendLine("✅ CORRECT: SELECT ProductID, SUM(Quantity) AS Total FROM OrderDetails GROUP BY ProductID ORDER BY Total DESC");
            sb.AppendLine("   (Application will fetch ProductName from another database using ProductID)");
            sb.AppendLine();
            
            // Database-specific syntax instructions
            switch (schema.DatabaseType)
            {
                case DatabaseType.SqlServer:
                    sb.AppendLine("SQL Server Syntax Rules (CRITICAL):");
                    sb.AppendLine("- Correct syntax: SELECT TOP 100 column1, column2 FROM TableName WHERE ...");
                    sb.AppendLine("- WRONG syntax: SELECT column1 FROM TableName TOP 100");
                    sb.AppendLine("- WRONG syntax: SELECT column1 FROM TableName LIMIT 100");
                    sb.AppendLine("- TOP keyword MUST come immediately after SELECT");
                    sb.AppendLine("- Table and column names are case-insensitive");
                    sb.AppendLine("- Use single quotes for string literals");
                    sb.AppendLine("- Maximum 100 rows using TOP 100");
                    break;
                    
                case DatabaseType.SQLite:
                    sb.AppendLine("SQLite Syntax Rules (CRITICAL):");
                    sb.AppendLine("- Correct syntax: SELECT column1, column2 FROM TableName WHERE ... LIMIT 100");
                    sb.AppendLine("- WRONG syntax: SELECT TOP 100 FROM TableName");
                    sb.AppendLine("- Table and column names are case-sensitive - use EXACT casing from schema above");
                    sb.AppendLine("- Use single quotes for string literals");
                    sb.AppendLine("- Maximum 100 rows using LIMIT 100 at the end of query");
                    break;
                    
                case DatabaseType.MySQL:
                    sb.AppendLine("MySQL Syntax Rules:");
                    sb.AppendLine("- Use LIMIT 100 at the end of query for row limiting");
                    sb.AppendLine("- Use backticks (`) for table/column names if they contain special characters");
                    break;
                    
                case DatabaseType.PostgreSQL:
                    sb.AppendLine("PostgreSQL Syntax Rules:");
                    sb.AppendLine("- Use LIMIT 100 at the end of query for row limiting");
                    sb.AppendLine("- Table and column names are lowercase unless quoted");
                    break;
            }
            
            sb.AppendLine();
            sb.AppendLine("Return ONLY the SQL query, no explanations, no markdown, no code blocks.");
            
            // Add database-specific example
            if (schema.DatabaseType == DatabaseType.SqlServer)
            {
                sb.AppendLine("Example format: SELECT TOP 100 Column1, Column2 FROM TableName WHERE ...");
            }
            else
            {
                sb.AppendLine("Example format: SELECT Column1, Column2 FROM TableName WHERE ... LIMIT 100");
            }

            return sb.ToString();
        }

        private QueryIntent ParseAIResponse(string aiResponse, string originalQuery, List<DatabaseSchemaInfo> schemas)
        {
            var queryIntent = new QueryIntent
            {
                OriginalQuery = originalQuery,
                Confidence = 0.5
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
                Confidence = 0.3,
                Reasoning = "Fallback: AI analysis failed, querying all databases"
            };

            foreach (var schema in schemas)
            {
                queryIntent.DatabaseQueries.Add(new DatabaseQueryIntent
                {
                    DatabaseId = schema.DatabaseId,
                    DatabaseName = schema.DatabaseName,
                    RequiredTables = schema.Tables.Take(5).Select(t => t.TableName).ToList(),
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

            // Remove markdown code blocks
            var sql = aiResponse.Trim();
            
            if (sql.StartsWith("```"))
            {
                var lines = sql.Split('\n');
                sql = string.Join("\n", lines.Skip(1).Take(lines.Length - 2));
            }

            return sql.Trim();
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

