using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartRAG.Services
{
    /// <summary>
    /// Analyzes user queries and determines which databases/tables to query
    /// </summary>
    public class QueryIntentAnalyzer : IQueryIntentAnalyzer
    {
        #region Constants

        private const double MinimumConfidence = 0.0;

        #endregion

        #region Fields

        private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer;
        private readonly IAIService _aiService;
        private readonly ILogger<QueryIntentAnalyzer> _logger;

        #endregion

        #region Constructor

        public QueryIntentAnalyzer(
            IDatabaseSchemaAnalyzer schemaAnalyzer,
            IAIService aiService,
            ILogger<QueryIntentAnalyzer> logger)
        {
            _schemaAnalyzer = schemaAnalyzer;
            _aiService = aiService;
            _logger = logger;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sanitizes user input for safe logging by removing control characters and limiting length.
        /// Prevents log injection attacks by removing newlines, carriage returns, and other control characters.
        /// </summary>
        private static string SanitizeForLog(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            
            const int maxLogLength = 500;
            
            // Remove control characters (including newlines, carriage returns, tabs, etc.)
            var sanitized = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                // Allow printable characters and common whitespace (space only)
                if (!char.IsControl(c) || c == ' ')
                {
                    sanitized.Append(c);
                }
            }
            
            var result = sanitized.ToString();
            
            // Limit length to prevent log flooding
            if (result.Length > maxLogLength)
            {
                result = result.Substring(0, maxLogLength) + "... (truncated)";
            }
            
            return result;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Analyzes user query and determines which databases/tables to query
        /// </summary>
        /// <param name="userQuery">Natural language user query</param>
        /// <returns>Query intent with database routing information</returns>
        public async Task<QueryIntent> AnalyzeQueryIntentAsync(string userQuery)
        {
            _logger.LogInformation("Analyzing query intent for: {Query}", SanitizeForLog(userQuery));

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

                // Parse AI response into QueryIntent
                queryIntent = ParseAIResponse(aiResponse, userQuery, schemas);

                if (queryIntent.DatabaseQueries.Count == 0)
                {
                    _logger.LogWarning("AI analysis returned no databases. Falling back to schema-driven defaults.");
                    queryIntent = CreateFallbackQueryIntent(userQuery, schemas);
                }

                _logger.LogInformation("Query analysis completed. Confidence: {Confidence}, Databases: {Count}",
                    queryIntent.Confidence, queryIntent.DatabaseQueries.Count);
                
                // Debug: Log which databases and tables were selected (only in Debug mode)
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("AI selected {Count} database(s) for this query:", queryIntent.DatabaseQueries.Count);
                    foreach (var dbQuery in queryIntent.DatabaseQueries)
                    {
                        _logger.LogDebug("  ✓ {DbName} → Tables: [{Tables}]", 
                            dbQuery.DatabaseName, string.Join(", ", dbQuery.RequiredTables));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing query intent");
                queryIntent.Confidence = MinimumConfidence;
            }

            return queryIntent;
        }

        #endregion

        #region Private Helper Methods

        private string BuildQueryAnalysisPrompt(string userQuery, List<DatabaseSchemaInfo> schemas)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are a database query analyzer. Analyze the user's query and determine which databases and tables are needed.");
            sb.AppendLine();
            sb.AppendLine($"User Query: {userQuery}");
            sb.AppendLine();
            
            // CRITICAL: Show database-table mapping upfront
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("DATABASE-TABLE MAPPING (READ THIS FIRST!)");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            
            // Build comprehensive table-to-database index
            var tableToDatabase = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var schema in schemas)
            {
                foreach (var table in schema.Tables.Take(20))
                {
                    tableToDatabase[table.TableName] = schema.DatabaseName;
                }
            }
            
            // Show by database WITH COLUMNS (so AI knows which table has which columns)
            foreach (var schema in schemas)
            {
                sb.AppendLine($"DATABASE: {schema.DatabaseName}");
                foreach (var table in schema.Tables.Take(10)) // Limit to prevent token overflow
                {
                    var keyColumns = table.Columns.Take(5).Select(c => c.ColumnName).ToList();
                    sb.AppendLine($"  • {table.TableName}: {string.Join(", ", keyColumns)}{(table.Columns.Count > 5 ? ", ..." : "")}");
                    sb.AppendLine($"    (exists ONLY in {schema.DatabaseName})");
                }
                sb.AppendLine();
            }
            
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("VERIFICATION CHECKLIST - USE THIS BEFORE WRITING JSON:");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("For EACH table you want to add to \"requiredTables\":");
            sb.AppendLine();
            sb.AppendLine("Step 1: Find the table name in the list above");
            sb.AppendLine("Step 2: Note which database it belongs to");
            sb.AppendLine("Step 3: Add it ONLY to that database's requiredTables array");
            sb.AppendLine();
            sb.AppendLine("EXAMPLE:");
            if (tableToDatabase.Count > 0)
            {
                var exampleTable = tableToDatabase.First();
                sb.AppendLine($"  - Want to query '{exampleTable.Key}'?");
                sb.AppendLine($"  - Look above → '{exampleTable.Key}' is in '{exampleTable.Value}'");
                sb.AppendLine($"  - Add to JSON → \"databaseName\": \"{exampleTable.Value}\", \"requiredTables\": [\"{exampleTable.Key}\"]");
            }
            sb.AppendLine();
            sb.AppendLine("NEVER add a table to the wrong database!");
            sb.AppendLine("Each table exists in EXACTLY ONE database!");
            sb.AppendLine("If unsure, leave it out – system will handle missing tables safely.");
            sb.AppendLine();
            sb.AppendLine("BEFORE YOU RESPOND:");
            sb.AppendLine("✓ Re-check the mapping above for every table you list");
            sb.AppendLine("✓ Confirm the database actually contains that table");
            sb.AppendLine("✓ Remove any table you cannot verify");
            sb.AppendLine("✓ If the user question references multiple concepts (descriptive context + transactional metrics), you must use multiple databases");
            sb.AppendLine("✗ NEVER return only one database if the question clearly needs multiple sources");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
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
                sb.AppendLine($"IMPORTANT: These tables ONLY exist in {schema.DatabaseName}:");
                sb.AppendLine($"    {string.Join(", ", tableList)}");
                sb.AppendLine();
            }

            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("CRITICAL RULES - READ CAREFULLY:");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("RULE #1: VERIFY TABLE LOCATION BEFORE ADDING TO JSON!");
            sb.AppendLine("   Before adding a table to 'requiredTables', SCROLL UP and verify:");
            sb.AppendLine("   - Is this table listed under THIS database's 'TABLES AVAILABLE' section?");
            sb.AppendLine("   - If NO, find which database ACTUALLY contains this table");
            sb.AppendLine("   - Only add tables that are ACTUALLY in that database!");
            sb.AppendLine();
            sb.AppendLine("Example: If you want to query a table named 'TableA':");
            sb.AppendLine("   1. Search above for 'TableA' in each database's table list");
            sb.AppendLine("   2. Found in Database1? → Add to Database1 requiredTables");
            sb.AppendLine("   3. NOT listed under Database2? → DO NOT add to Database2 requiredTables!");
            sb.AppendLine();
            sb.AppendLine("RULE #2: Identify ALL data sources the question needs!");
            sb.AppendLine("   - Does the question mention descriptive attributes (names, classifications)?");
            sb.AppendLine("     → Include the database containing those descriptive tables.");
            sb.AppendLine("   - Does the question mention quantitative events (transactions, measurements, timelines)?");
            sb.AppendLine("     → Include the database containing those transactional tables.");
            sb.AppendLine("   - When the question spans multiple concepts, you MUST return multiple databases.");
            sb.AppendLine("   - Returning only one database is allowed ONLY when every required concept lives in that single database.");
            sb.AppendLine();
            sb.AppendLine("   QUICK CHECK FOR MULTI-DB NEEDS:");
            sb.AppendLine("   • Descriptive attributes + Aggregated metrics → requires multiple databases");
            sb.AppendLine("   • Reference data + Activity logs → requires multiple databases");
            sb.AppendLine("   • Classification names + Recent events → requires multiple databases");
            sb.AppendLine();
            sb.AppendLine("2. Each table exists in ONLY ONE database");
            sb.AppendLine("3. Before selecting a table, look at 'TABLES IN THIS DATABASE' section above");
            sb.AppendLine("4. Only select tables that are listed under that specific database");
            sb.AppendLine("5. For cross-database queries, identify which tables are in which database");
            sb.AppendLine("6. Use Foreign Key relationships to understand how tables connect across databases");
            sb.AppendLine("7. Use EXACT Database IDs and Table Names from the schema above");
            sb.AppendLine("8. If a query needs data from multiple databases, create separate database entries for each");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("HOW TO WRITE 'PURPOSE' FIELD:");
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
            sb.AppendLine("PURPOSE MUST DESCRIBE DATA TYPES TO FIND, NOT SPECIFIC COLUMN NAMES!");
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

        private QueryIntent ParseAIResponse(string aiResponse, string originalQuery, List<DatabaseSchemaInfo> schemas)
        {
            var queryIntent = new QueryIntent
            {
                OriginalQuery = originalQuery,
                Confidence = MinimumConfidence
            };

            if (string.IsNullOrWhiteSpace(aiResponse))
            {
                _logger.LogWarning("AI response is empty");
                return queryIntent;
            }

            try
            {
                // Try to extract JSON from response (might have markdown code blocks)
                var jsonStart = aiResponse.IndexOf('{');
                var jsonEnd = aiResponse.LastIndexOf('}');
                
                if (jsonStart < 0 || jsonEnd < jsonStart)
                {
                    _logger.LogWarning("Could not find JSON in AI response");
                    return queryIntent;
                }

                var jsonText = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                
                // Parse JSON
                var jsonDoc = JsonDocument.Parse(jsonText);
                var root = jsonDoc.RootElement;

                // Extract confidence
                if (root.TryGetProperty("confidence", out var confidenceElement))
                {
                    queryIntent.Confidence = confidenceElement.GetDouble();
                }

                // Extract understanding and reasoning
                if (root.TryGetProperty("understanding", out var understandingElement))
                {
                    queryIntent.QueryUnderstanding = understandingElement.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("reasoning", out var reasoningElement))
                {
                    queryIntent.Reasoning = reasoningElement.GetString();
                }

                if (root.TryGetProperty("requiresCrossDatabaseJoin", out var crossDbElement))
                {
                    queryIntent.RequiresCrossDatabaseJoin = crossDbElement.GetBoolean();
                }

                // Extract databases
                if (root.TryGetProperty("databases", out var databasesElement) && databasesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var dbElement in databasesElement.EnumerateArray())
                    {
                        var dbQuery = new DatabaseQueryIntent();

                        if (dbElement.TryGetProperty("databaseId", out var dbIdElement))
                        {
                            dbQuery.DatabaseId = dbIdElement.GetString() ?? string.Empty;
                        }

                        if (dbElement.TryGetProperty("databaseName", out var dbNameElement))
                        {
                            dbQuery.DatabaseName = dbNameElement.GetString() ?? string.Empty;
                        }

                        if (dbElement.TryGetProperty("requiredTables", out var tablesElement) && tablesElement.ValueKind == JsonValueKind.Array)
                        {
                            // CRITICAL: Validate each table exists in THIS database's schema
                            var targetSchema = schemas.FirstOrDefault(s => 
                                s.DatabaseId.Equals(dbQuery.DatabaseId, StringComparison.OrdinalIgnoreCase) ||
                                s.DatabaseName.Equals(dbQuery.DatabaseName, StringComparison.OrdinalIgnoreCase));
                            
                            if (targetSchema == null)
                            {
                                _logger.LogWarning("Schema not found for database: {DatabaseName}", dbQuery.DatabaseName);
                                continue;
                            }
                            
                            var validTables = targetSchema.Tables.Select(t => t.TableName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                            
                            foreach (var tableElement in tablesElement.EnumerateArray())
                            {
                                var tableName = tableElement.GetString() ?? string.Empty;
                                
                                // VALIDATE: Table must exist in this database's schema
                                if (validTables.Contains(tableName))
                                {
                                    dbQuery.RequiredTables.Add(tableName);
                                }
                                else
                                {
                                    _logger.LogWarning("AI attempted to add table '{Table}' to '{Database}', but it doesn't exist there. Skipping.", 
                                        tableName, dbQuery.DatabaseName);
                                }
                            }

                            // Automatically include foreign-key dependencies so joins have required tables
                            ExpandTablesWithForeignKeyDependencies(dbQuery, targetSchema);
                        }

                        if (dbElement.TryGetProperty("purpose", out var purposeElement))
                        {
                            dbQuery.Purpose = purposeElement.GetString() ?? string.Empty;
                        }

                        if (dbElement.TryGetProperty("priority", out var priorityElement))
                        {
                            dbQuery.Priority = priorityElement.GetInt32();
                        }

                        // Validate database exists
                        var schema = schemas.FirstOrDefault(s => s.DatabaseId.Equals(dbQuery.DatabaseId, StringComparison.OrdinalIgnoreCase));
                        if (schema != null)
                        {
                            queryIntent.DatabaseQueries.Add(dbQuery);
                        }
                        else
                        {
                            _logger.LogWarning("AI selected non-existent database: {DatabaseId}", dbQuery.DatabaseId);
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing AI response JSON");
                // Fallback: query all databases
                queryIntent = CreateFallbackQueryIntent(originalQuery, schemas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error parsing AI response");
                // Fallback: query all databases
                queryIntent = CreateFallbackQueryIntent(originalQuery, schemas);
            }

            return queryIntent;
        }

        private QueryIntent CreateFallbackQueryIntent(string originalQuery, List<DatabaseSchemaInfo> schemas)
        {
            const int MaxTablesInFallback = 5;
            const double FallbackConfidence = 0.3;

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

        private void ExpandTablesWithForeignKeyDependencies(DatabaseQueryIntent dbQuery, DatabaseSchemaInfo schema)
        {
            if (schema == null || dbQuery.RequiredTables.Count == 0)
            {
                return;
            }

            var existingTables = new HashSet<string>(dbQuery.RequiredTables, StringComparer.OrdinalIgnoreCase);
            var processingQueue = new Queue<string>(existingTables);

            while (processingQueue.Count > 0)
            {
                var currentTableName = processingQueue.Dequeue();
                var tableSchema = schema.Tables.FirstOrDefault(t => 
                    t.TableName.Equals(currentTableName, StringComparison.OrdinalIgnoreCase));

                if (tableSchema == null || tableSchema.ForeignKeys.Count == 0)
                {
                    continue;
                }

                foreach (var foreignKey in tableSchema.ForeignKeys)
                {
                    if (string.IsNullOrWhiteSpace(foreignKey.ReferencedTable))
                    {
                        continue;
                    }

                    var referencedTable = schema.Tables.FirstOrDefault(t => 
                        t.TableName.Equals(foreignKey.ReferencedTable, StringComparison.OrdinalIgnoreCase));

                    if (referencedTable == null)
                    {
                        continue;
                    }

                    if (existingTables.Add(referencedTable.TableName))
                    {
                        dbQuery.RequiredTables.Add(referencedTable.TableName);
                        processingQueue.Enqueue(referencedTable.TableName);

                        if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            _logger.LogDebug("Auto-added table '{ReferencedTable}' because '{SourceTable}' has foreign key column '{ColumnName}'",
                                referencedTable.TableName,
                                currentTableName,
                                foreignKey.ColumnName);
                        }
                    }
                }
            }
        }

        #endregion
    }
}

