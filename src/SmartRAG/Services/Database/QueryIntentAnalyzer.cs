using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Database;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Services.Database
{
    /// <summary>
    /// Analyzes user queries and determines which databases/tables to query
    /// </summary>
    public class QueryIntentAnalyzer : IQueryIntentAnalyzer
    {
        private const double MinimumConfidence = 0.0;

        private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer;
        private readonly IAIService _aiService;
        private readonly ILogger<QueryIntentAnalyzer> _logger;

        public QueryIntentAnalyzer(
            IDatabaseSchemaAnalyzer schemaAnalyzer,
            IAIService aiService,
            ILogger<QueryIntentAnalyzer> logger)
        {
            _schemaAnalyzer = schemaAnalyzer;
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>
        /// [AI Query] Analyzes user query and determines which databases/tables to query
        /// </summary>
        /// <param name="userQuery">Natural language user query</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Query intent with database routing information</returns>
        public async Task<QueryIntent> AnalyzeQueryIntentAsync(string userQuery, CancellationToken cancellationToken = default)
        {
            var queryIntent = new QueryIntent
            {
                OriginalQuery = userQuery
            };

            try
            {
                var schemas = await _schemaAnalyzer.GetAllSchemasAsync();

                if (schemas.Count == 0)
                {
                    _logger.LogWarning("No database schemas available for query analysis");
                    queryIntent.Confidence = MinimumConfidence;
                    return queryIntent;
                }

                var prompt = BuildQueryAnalysisPrompt(userQuery, schemas);

                var aiResponse = await _aiService.GenerateResponseAsync(prompt, new List<string>());

                queryIntent = ParseAIResponse(aiResponse, userQuery, schemas);

                if (queryIntent.DatabaseQueries.Count == 0)
                {
                    queryIntent = CreateFallbackQueryIntent(userQuery, schemas);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing query intent");
                queryIntent.Confidence = MinimumConfidence;
            }

            return queryIntent;
        }

        private string BuildQueryAnalysisPrompt(string userQuery, List<DatabaseSchemaInfo> schemas)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("DATABASE QUERY ANALYZER");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"User Query: \"{userQuery}\"");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("STEP 1: AVAILABLE SCHEMAS");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();

            // Show schema in compact format
            foreach (var schema in schemas)
            {
                sb.AppendLine($"DATABASE: {schema.DatabaseName} (ID: {schema.DatabaseId})");
                sb.AppendLine($"  Type: {schema.DatabaseType}, Total Rows: {schema.TotalRowCount}");
                sb.AppendLine("  Tables:");
                
                foreach (var table in schema.Tables.Take(15)) // Limit for token efficiency
                {
                    var keyColumns = string.Join(", ", table.Columns.Take(6).Select(c => $"{c.ColumnName}({c.DataType})"));
                    var fkInfo = table.ForeignKeys.Any() ? $" [FK: {string.Join(", ", table.ForeignKeys.Select(fk => fk.ReferencedTable).Distinct())}]" : "";
                    sb.AppendLine($"    - {table.TableName}: {keyColumns}{fkInfo}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("STEP 2: SIMPLE RULES");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("RULE 1: EACH TABLE EXISTS IN ONLY ONE DATABASE - CRITICAL!");
            sb.AppendLine("  → Check schema above BEFORE adding a table to requiredTables");
            sb.AppendLine("  → NEVER add a table to a database that doesn't contain it");
            sb.AppendLine("  → Use EXACT table names as shown in schema (case-sensitive for PostgreSQL)");
            sb.AppendLine();
            sb.AppendLine("COMMON MISTAKES TO AVOID:");
            sb.AppendLine("  ✗ Adding 'SchemaName.TableName' to Database1 if schema shows 'schemaname.tablename'");
            sb.AppendLine("  ✗ Adding 'SchemaA.TableB' to Database1 (it's in Database2!)");
            sb.AppendLine("  ✗ Adding 'SchemaX.TableY' if table doesn't exist in that database");
            sb.AppendLine("  ✓ Use EXACT table name from schema section above");
            sb.AppendLine();
            sb.AppendLine("RULE 2: NUMERIC/AGGREGATION QUESTIONS → TRANSACTION TABLES");
            sb.AppendLine("  → Look for tables with:");
            sb.AppendLine("    • Numeric columns (int, decimal, float, money data types)");
            sb.AppendLine("    • Foreign key columns (ending with ID suffix)");
            sb.AppendLine("    • Transaction/detail data patterns");
            sb.AppendLine("  → Purpose: 'Get numeric data for calculations and foreign key IDs'");
            sb.AppendLine();
            sb.AppendLine("RULE 3: DESCRIPTIVE QUESTIONS → MULTI-TABLE SELECTION");
            sb.AppendLine("  🚨 When query requests entity identification or descriptive info → SELECT BOTH:");
            sb.AppendLine();
            sb.AppendLine("  A) TRANSACTION TABLE (for calculations/aggregations):");
            sb.AppendLine("     → Contains: Numeric columns + Foreign Key IDs");
            sb.AppendLine("     → Purpose: 'Get numeric data and foreign key IDs'");
            sb.AppendLine("     → Pattern: Tables with Amount/Quantity/Price columns");
            sb.AppendLine();
            sb.AppendLine("  B) REFERENCE TABLE (for names/descriptions):");
            sb.AppendLine("     → Contains: Name/Title columns + Primary Key IDs");
            sb.AppendLine("     → Purpose: 'Get descriptive text data and foreign key IDs'");
            sb.AppendLine("     → Pattern: Tables with Name/Title/Description columns");
            sb.AppendLine();
            sb.AppendLine("  ✓ CORRECT Multi-Table Pattern:");
            sb.AppendLine("    • 'Top entities by value?' → TransactionTable (numeric data) + ReferenceTable (entity names)");
            sb.AppendLine("    • 'Best item by quantity?' → DetailTable (quantities) + ItemTable (item names)");
            sb.AppendLine("    • 'Highest performer?' → PerformanceTable (metrics) + ActorTable (actor names)");
            sb.AppendLine("    • 'Totals by category?' → TransactionTable (amounts) + CategoryTable (category names)");
            sb.AppendLine();
            sb.AppendLine("  ✗ WRONG Single-Table Pattern:");
            sb.AppendLine("    • 'Top entities?' → ReferenceTable only  -- Missing numeric data!");
            sb.AppendLine("    • 'Best item?' → ItemTable only  -- Missing transaction data!");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("STEP 3: OUTPUT FORMAT");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("Return ONLY valid JSON (no markdown, no extra text):");
            sb.AppendLine("{");
            sb.AppendLine("  \"understanding\": \"Brief explanation\",");
            sb.AppendLine("  \"confidence\": 0.95,");
            sb.AppendLine("  \"requiresCrossDatabaseJoin\": false,");
            sb.AppendLine("  \"reasoning\": \"Why these were selected\",");
            sb.AppendLine("  \"databases\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"databaseId\": \"EXACT_ID_FROM_SCHEMA\",");
            sb.AppendLine("      \"databaseName\": \"EXACT_NAME_FROM_SCHEMA\",");
            sb.AppendLine("      \"requiredTables\": [\"Table1\", \"Table2\"],");
            sb.AppendLine("      \"purpose\": \"Get [numeric/text] data and foreign keys\",");
            sb.AppendLine("      \"priority\": 1");
            sb.AppendLine("    }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("CRITICAL: Use EXACT table names from schema above!");
            sb.AppendLine();
            sb.AppendLine("VALIDATION RULES:");
            sb.AppendLine("  → Each table in requiredTables MUST exist in that database's schema");
            sb.AppendLine("  → Table names are case-sensitive for PostgreSQL (use lowercase if schema shows lowercase)");
            sb.AppendLine("  → Table names must match EXACTLY (including schema prefix: schema.table)");
            sb.AppendLine("  → If table doesn't exist in database, it will be removed and query may fail");
            sb.AppendLine();

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
                var jsonStart = aiResponse.IndexOf('{');
                var jsonEnd = aiResponse.LastIndexOf('}');

                if (jsonStart < 0 || jsonEnd < jsonStart)
                {
                    _logger.LogWarning("Could not find JSON in AI response");
                    return queryIntent;
                }

                var jsonText = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);

                var jsonDoc = JsonDocument.Parse(jsonText);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("confidence", out var confidenceElement))
                {
                    queryIntent.Confidence = confidenceElement.GetDouble();
                }

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
                                _logger.LogWarning("Schema not found for database");
                                continue;
                            }

                            var validTables = targetSchema.Tables.Select(t => t.TableName).ToHashSet(StringComparer.OrdinalIgnoreCase);

                            foreach (var tableElement in tablesElement.EnumerateArray())
                            {
                                var tableName = tableElement.GetString() ?? string.Empty;

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

                        var schema = schemas.FirstOrDefault(s => s.DatabaseId.Equals(dbQuery.DatabaseId, StringComparison.OrdinalIgnoreCase));
                        if (schema != null)
                        {
                            queryIntent.DatabaseQueries.Add(dbQuery);
                        }
                        else
                        {
                            _logger.LogWarning("AI selected non-existent database");
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing AI response JSON");
                queryIntent = CreateFallbackQueryIntent(originalQuery, schemas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error parsing AI response");
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

                    }
                }
            }
        }
    }
}

