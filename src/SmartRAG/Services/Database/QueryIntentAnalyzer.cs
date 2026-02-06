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

namespace SmartRAG.Services.Database;


/// <summary>
/// Analyzes user queries and determines which databases/tables to query
/// </summary>
public class QueryIntentAnalyzer : IQueryIntentAnalyzer
{
    private const double MinimumConfidence = 0.0;

    private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer;
    private readonly IAIService _aiService;
    private readonly IDatabaseConnectionManager _connectionManager;
    private readonly ILogger<QueryIntentAnalyzer> _logger;

    public QueryIntentAnalyzer(
        IDatabaseSchemaAnalyzer schemaAnalyzer,
        IAIService aiService,
        IDatabaseConnectionManager connectionManager,
        ILogger<QueryIntentAnalyzer> logger)
    {
        _schemaAnalyzer = schemaAnalyzer;
        _aiService = aiService;
        _connectionManager = connectionManager;
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
            var schemas = await _schemaAnalyzer.GetAllSchemasAsync(cancellationToken);

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

    private bool QueryRequiresCrossDatabaseMapping(string userQuery, List<CrossDatabaseMapping> mappings)
    {
        if (mappings == null || !mappings.Any())
            return false;

        var schemas = _schemaAnalyzer.GetAllSchemasAsync().GetAwaiter().GetResult();
        if (schemas == null || !schemas.Any())
            return false;

        var hasDescriptiveKeywords = ContainsDescriptiveKeywords(userQuery, schemas);
        var hasAggregationKeywords = ContainsAggregationKeywords(userQuery, schemas);

        return hasDescriptiveKeywords && hasAggregationKeywords;
    }

    private bool ContainsDescriptiveKeywords(string query, List<DatabaseSchemaInfo> schemas)
    {
        if (string.IsNullOrWhiteSpace(query) || schemas == null || !schemas.Any())
            return false;

        var lowerQuery = query.ToLowerInvariant();
        
        var descriptivePatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var schema in schemas)
        {
            foreach (var table in schema.Tables)
            {
                foreach (var column in table.Columns)
                {
                    var columnLower = column.ColumnName.ToLowerInvariant();
                    if (IsDescriptiveColumnPattern(columnLower))
                    {
                        var words = ExtractWords(columnLower);
                        foreach (var word in words)
                        {
                            if (word.Length > 2)
                            {
                                descriptivePatterns.Add(word);
                            }
                        }
                    }
                }
            }
        }

        return descriptivePatterns.Any(pattern => lowerQuery.Contains(pattern)) ||
               ContainsGenericDescriptivePatterns(lowerQuery);
    }

    private bool ContainsAggregationKeywords(string query, List<DatabaseSchemaInfo> schemas)
    {
        if (string.IsNullOrWhiteSpace(query) || schemas == null || !schemas.Any())
            return false;

        var lowerQuery = query.ToLowerInvariant();
        
        var hasNumericColumns = schemas.Any(schema => 
            schema.Tables.Any(table => 
                table.Columns.Any(col => IsNumericType(col.DataType))));

        var aggregationPatterns = new[] { "count", "sum", "avg", "total", "most", "top", "max", "min", "highest", "lowest", "first", "order", "sort" };
        var hasAggregationPattern = aggregationPatterns.Any(pattern => lowerQuery.Contains(pattern));

        return hasNumericColumns && hasAggregationPattern;
    }

    private bool IsDescriptiveColumnPattern(string columnName)
    {
        var descriptiveTerms = new[] { "name", "title", "description", "label", "text", "value" };
        return descriptiveTerms.Any(term => columnName.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsNumericType(string dataType)
    {
        if (string.IsNullOrWhiteSpace(dataType))
            return false;

        var typeLower = dataType.ToLowerInvariant();
        var numericTypes = new[] { "int", "bigint", "smallint", "tinyint", "decimal", "numeric", "money", "float", "real", "double", "number" };
        return numericTypes.Any(nt => typeLower.Contains(nt));
    }

    private bool IsTextType(string dataType)
    {
        if (string.IsNullOrWhiteSpace(dataType))
            return false;

        var typeLower = dataType.ToLowerInvariant();
        var textTypes = new[] { "varchar", "nvarchar", "text", "char", "nchar", "string", "ntext" };
        return textTypes.Any(tt => typeLower.Contains(tt));
    }

    private bool ContainsGenericDescriptivePatterns(string lowerQuery)
    {
        var genericPatterns = new[] { "who", "which", "what", "name", "title", "description", "entity", "entities" };
        return genericPatterns.Any(pattern => lowerQuery.Contains(pattern));
    }

    private List<string> ExtractWords(string text)
    {
        var words = new List<string>();
        var currentWord = new StringBuilder();

        foreach (var ch in text)
        {
            if (char.IsLetter(ch) || ch == '_')
            {
                currentWord.Append(ch);
            }
            else
            {
                if (currentWord.Length > 0)
                {
                    words.Add(currentWord.ToString());
                    currentWord.Clear();
                }
            }
        }

        if (currentWord.Length > 0)
        {
            words.Add(currentWord.ToString());
        }

        return words;
    }

    private string BuildQueryAnalysisPrompt(string userQuery, List<DatabaseSchemaInfo> schemas)
    {
        var sb = new StringBuilder();
        var allMappings = GetAllCrossDatabaseMappings();
        
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("DATABASE QUERY ANALYZER");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"User Query: \"{userQuery}\"");
        sb.AppendLine();

        if (allMappings.Any())
        {
            var requiresMapping = QueryRequiresCrossDatabaseMapping(userQuery, allMappings);
            
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("🚨 CRITICAL: CROSS-DATABASE MAPPINGS");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            
            if (requiresMapping)
            {
                sb.AppendLine("⚠️ THIS QUERY REQUIRES MULTI-DATABASE ACCESS!");
                sb.AppendLine();
            }
            
            sb.AppendLine("Mapping Format: SourceDatabase.SourceTable.SourceColumn → TargetDatabase.TargetTable.TargetColumn");
            sb.AppendLine();
            foreach (var mapping in allMappings)
            {
                sb.AppendLine($"  • {mapping.SourceDatabase}.{mapping.SourceTable}.{mapping.SourceColumn}");
                sb.AppendLine($"    → {mapping.TargetDatabase}.{mapping.TargetTable}.{mapping.TargetColumn}");
            }
            sb.AppendLine();
            sb.AppendLine("⚠️⚠️ CRITICAL DECISION RULE: SINGLE vs MULTI-DATABASE ⚠️⚠️");
            sb.AppendLine();
            sb.AppendLine("BEFORE creating multiple queries, check:");
            sb.AppendLine("  → Are ALL required tables in the SAME database?");
            sb.AppendLine("    • YES → Create ONE query with JOINs (NOT multiple queries!)");
            sb.AppendLine("    • NO → Create multiple queries (only if tables are in DIFFERENT databases)");
            sb.AppendLine();
            sb.AppendLine("Example: Query asks for 'classification with most items and total quantity'");
            sb.AppendLine("  Scenario A: All tables in Database1");
            sb.AppendLine("    ✓ CORRECT: ONE query → JOIN TableA + TableB + TableC");
            sb.AppendLine("    ✗ WRONG: Multiple queries for same database");
            sb.AppendLine("  Scenario B: TableA in Database1, TableC in Database2");
            sb.AppendLine("    ✓ CORRECT: TWO queries → Database1(aggregation) + Database2(descriptive)");
            sb.AppendLine();
            sb.AppendLine("RULE: If query asks for descriptive info + aggregation ACROSS DIFFERENT databases:");
            sb.AppendLine("      Descriptive: queries asking for names, titles, descriptions, or entity identification");
            sb.AppendLine("      Aggregation: queries containing count, sum, top, most, highest, total, etc.");
            sb.AppendLine("      → Mapping: Database1.SchemaName.TableA.ColumnX → Database2.SchemaName.TableB.ColumnY");
            sb.AppendLine("      → Solution: Include BOTH Database1 AND Database2!");
            sb.AppendLine("        • Database1 (priority: 1): Get aggregation (COUNT, SUM, etc.) and foreign key IDs");
            sb.AppendLine("        • Database2 (priority: 2): Get descriptive data (Name, Title columns) using the IDs");
            sb.AppendLine();
            sb.AppendLine("🎯 MULTI-DATABASE EXECUTION ORDER (ONLY when tables are in different databases):");
            sb.AppendLine("  1. AGGREGATION FIRST (priority: 1)");
            sb.AppendLine("     → Database with COUNT/SUM/numeric operations");
            sb.AppendLine("     → Select TOP N + foreign key column for mapping");
            sb.AppendLine("  2. DESCRIPTIVE SECOND (priority: 2)");
            sb.AppendLine("     → Database with Name/Title/text columns");
            sb.AppendLine("     → Filter using foreign key values from step 1");
            sb.AppendLine();
            sb.AppendLine("  Example (MULTI-DATABASE):");
            sb.AppendLine("    Query: 'Which entities have the most records? Show top 5 names.'");
            sb.AppendLine("    Tables: EntityTable in Database1, NameTable in Database2");
            sb.AppendLine("    Step 1 (priority: 1): Database1 → Aggregate data, get TOP 5 ForeignKeyColumnIDs");
            sb.AppendLine("    Step 2 (priority: 2): Database2 → Get descriptive data WHERE KeyColumn IN (...)");
            sb.AppendLine();
            sb.AppendLine("CORRECT Response:");
            sb.AppendLine("  {");
            sb.AppendLine("    \"databases\": [");
            sb.AppendLine("      {");
            sb.AppendLine("        \"databaseId\": \"database1-id\",");
            sb.AppendLine("        \"databaseName\": \"Database1\",");
            sb.AppendLine("        \"requiredTables\": [\"SchemaName.TableA\"],");
            sb.AppendLine("        \"purpose\": \"Get numeric data for calculations and foreign key IDs\"");
            sb.AppendLine("      },");
            sb.AppendLine("      {");
            sb.AppendLine("        \"databaseId\": \"database2-id\",");
            sb.AppendLine("        \"databaseName\": \"Database2\",");
            sb.AppendLine("        \"requiredTables\": [\"SchemaName.TableB\"],");
            sb.AppendLine("        \"purpose\": \"Get descriptive text data and foreign key IDs\"");
            sb.AppendLine("      }");
            sb.AppendLine("    ]");
            sb.AppendLine("  }");
            sb.AppendLine();
        }

        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("AVAILABLE SCHEMAS");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        foreach (var schema in schemas)
        {
            sb.AppendLine($"DATABASE: {schema.DatabaseName} (ID: {schema.DatabaseId})");
            sb.AppendLine($"  Type: {schema.DatabaseType}, Total Rows: {schema.TotalRowCount:N0}");
            sb.AppendLine("  Tables:");
            
            foreach (var table in schema.Tables.Take(20))
            {
                var tableType = table.RowCount > 10000 ? "TRANSACTIONAL" : (table.RowCount > 1000 ? "LOOKUP" : "MASTER");
                
                sb.AppendLine($"    • {table.TableName} (Rows: {table.RowCount:N0}, Type: {tableType})");
                
                var pkColumns = table.PrimaryKeys.Any() ? table.PrimaryKeys : table.Columns.Where(c => c.IsPrimaryKey).Select(c => c.ColumnName).ToList();
                if (pkColumns.Any())
                {
                    sb.AppendLine($"      PK: {string.Join(", ", pkColumns)}");
                }
                
                if (table.ForeignKeys.Any())
                {
                    sb.AppendLine("      Foreign Keys:");
                    foreach (var fk in table.ForeignKeys.Take(5))
                    {
                        sb.AppendLine($"        {fk.ColumnName} → {fk.ReferencedTable}.{fk.ReferencedColumn}");
                    }
                }
                
                var importantColumns = table.Columns
                    .Where(c => c.IsPrimaryKey || c.IsForeignKey || 
                               IsNumericType(c.DataType) || IsTextType(c.DataType))
                    .Take(10)
                    .Select(c => {
                        var markers = new List<string>();
                        if (c.IsPrimaryKey) markers.Add("PK");
                        if (c.IsForeignKey) markers.Add("FK");
                        var markerStr = markers.Any() ? $" [{string.Join(",", markers)}]" : "";
                        return $"{c.ColumnName}({c.DataType}){markerStr}";
                    });
                
                sb.AppendLine($"      Columns: {string.Join(", ", importantColumns)}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("🎯 TABLE SELECTION GUIDE (LANGUAGE & DOMAIN AGNOSTIC)");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("Match query intent to table structure patterns (NOT specific names!):");
        sb.AppendLine();
        sb.AppendLine("PATTERN 1: Hierarchical Classification");
        sb.AppendLine("  Query intent: Grouping, categorization, classification questions");
        sb.AppendLine("  Table structure: Tables that represent classification hierarchies");
        sb.AppendLine("  Column patterns: Foreign keys pointing to parent/classification tables");
        sb.AppendLine("  JOIN path: TableA (MASTER) → TableB (LOOKUP) → TableC (DETAIL) via foreign key relationships");
        sb.AppendLine("  Example structure: TableA → TableB → TableC (where TableA classifies TableB, TableB references TableC)");
        sb.AppendLine();
        sb.AppendLine("PATTERN 2: Numeric Aggregation");
        sb.AppendLine("  Query intent: Quantity, amount, count, sum, total calculations");
        sb.AppendLine("  Table structure: Tables containing numeric accumulation data");
        sb.AppendLine("  Column patterns: Integer or decimal columns representing quantities/amounts");
        sb.AppendLine("  Data types: int, bigint, decimal, numeric, money, float (numeric types)");
        sb.AppendLine("  JOIN requirement: Must join to master entity tables via foreign keys");
        sb.AppendLine();
        sb.AppendLine("PATTERN 3: Descriptive Entity Lookup");
        sb.AppendLine("  Query intent: Who, which, name, identifier questions");
        sb.AppendLine("  Table structure: Tables containing entity master data");
        sb.AppendLine("  Column patterns: Text/varchar columns containing human-readable identifiers");
        sb.AppendLine("  Data types: varchar, nvarchar, text, char (text types)");
        sb.AppendLine("  Purpose: Provides descriptive information (not numeric calculations)");
        sb.AppendLine();
        sb.AppendLine("PATTERN 4: Spatial/Location Reference");
        sb.AppendLine("  Query intent: Where, location, geography, address questions");
        sb.AppendLine("  Table structure: Tables representing geographic or location data");
        sb.AppendLine("  Column patterns: Text columns representing locations, coordinates, or geographic hierarchies");
        sb.AppendLine("  JOIN path: Location → Entity (entities reference locations via foreign keys)");
        sb.AppendLine();
        sb.AppendLine("PATTERN 5: Transactional Activity (CRITICAL FOR COUNT QUERIES!)");
        sb.AppendLine("  Query intent: Count events, activities, transactions, historical records over time");
        sb.AppendLine("  Query keywords: 'how many', 'count', 'most/least transactions', 'top N by activity'");
        sb.AppendLine("  ⚠️ CRITICAL: For counting repeated events, use the TRANSACTIONAL table (high row count), NOT the master entity table!");
        sb.AppendLine("  Table identification:");
        sb.AppendLine("    • TRANSACTIONAL tables: Rows > 10,000 (each row = one event/transaction)");
        sb.AppendLine("    • MASTER tables: Rows < 10,000 (each row = one entity, referenced by transactional tables)");
        sb.AppendLine("  Column patterns: Date/time columns, status columns, foreign key references to master entities");
        sb.AppendLine("  JOIN requirement: JOIN transactional table TO master table (using foreign keys from schema)");
        sb.AppendLine("  Flow: COUNT(transactional rows) JOIN master table GROUP BY master entity");
        sb.AppendLine();
        sb.AppendLine("⚠️ CRITICAL: Match by STRUCTURE and DATA TYPE, NOT by specific column/table names!");
        sb.AppendLine("  → Use schema's FOREIGN KEY RELATIONSHIPS to identify table relationships");
        sb.AppendLine("  → Use DATA TYPES to identify numeric vs text columns");
        sb.AppendLine("  → Use TABLE RELATIONSHIPS to identify master-detail or classification hierarchies");
        sb.AppendLine();

        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("🚨 CRITICAL RULES");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("RULE 1: Use EXACT table names from schema (case-sensitive for PostgreSQL)");
        sb.AppendLine("RULE 2: Each table exists in ONLY ONE database - check schema before adding");
        sb.AppendLine("RULE 3: Aggregation queries (COUNT, SUM, TOP, MOST) need tables with numeric columns (int, decimal, etc.)");
        sb.AppendLine("RULE 4: Descriptive queries (identifiers, names, entities) need tables with text columns (varchar, text) for human-readable data");
        if (allMappings.Any())
        {
            sb.AppendLine("RULE 5: If mappings exist AND query needs both aggregation + descriptive → Include BOTH databases!");
            sb.AppendLine("RULE 6: SET PRIORITY CORRECTLY:");
            sb.AppendLine("        • Aggregation database = priority: 1 (executes first)");
            sb.AppendLine("        • Descriptive database = priority: 2 (executes second, uses values from first)");
        }
        sb.AppendLine("RULE 7: ALWAYS follow JOIN paths via foreign keys");
        sb.AppendLine("        Example: If TableA → TableB → TableC (via FKs), include ALL three tables!");
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("❌ ANTI-PATTERNS (DO NOT DO THIS!)");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("WRONG #1: Ignoring foreign key chains");
        sb.AppendLine("  Query: 'Which classification has most items and what is the total quantity?'");
        sb.AppendLine("  ❌ WRONG: Select only TableA + TableC (skips intermediate TableB)");
        sb.AppendLine("  ✓ CORRECT: Select TableA + TableB + TableC (follows FK chain)");
        sb.AppendLine();
        sb.AppendLine("WRONG #2: Selecting irrelevant tables by name pattern only");
        sb.AppendLine("  Query: 'Which group has most items?'");
        sb.AppendLine("  ❌ WRONG: Select TableX (has text column matching pattern but represents different entity domain)");
        sb.AppendLine("  ✓ CORRECT: Select TableA + TableB (tables that actually represent the queried entity via foreign key relationships)");
        sb.AppendLine();
        sb.AppendLine("WRONG #3: Missing database when cross-join needed");
        sb.AppendLine("  Query: 'Top 5 entities with most records + their names'");
        sb.AppendLine("  ❌ WRONG: Only select one database");
        sb.AppendLine("  ✓ CORRECT: Select Database1(aggregation) + Database2(names) with mapping");
        sb.AppendLine();
        sb.AppendLine("WRONG #4: Table names that don't exist");
        sb.AppendLine("  ❌ WRONG: 'RequiredTables': ['TableX'] (not in schema!)");
        sb.AppendLine("  ✓ CORRECT: Use ONLY tables listed in AVAILABLE SCHEMAS section above");
        sb.AppendLine();
        sb.AppendLine("WRONG #5: Using master entity table for counting repeated events");
        sb.AppendLine("  Query: 'Who did the most [action]?' or 'Count [events] per [entity]'");
        sb.AppendLine("  ❌ WRONG: Use master entity table (low row count) → COUNT always returns 1 per entity!");
        sb.AppendLine("  ✓ CORRECT: Use TRANSACTIONAL table (high row count, Type: TRANSACTIONAL) JOIN master table");
        sb.AppendLine("  ✓ Identify by: Check 'Rows' field in schema - transactional tables have Rows > 10,000");
        sb.AppendLine("  ✓ JOIN path: Use foreign keys shown in schema (ColumnName → ReferencedTable.ReferencedColumn)");
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("🎯 OUTPUT FORMAT (STRICT!)");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("⚠️⚠️ CRITICAL: You MUST return ONLY valid JSON - NO markdown, NO ```json blocks, NO explanation text!");
        sb.AppendLine();
        sb.AppendLine("REQUIRED JSON STRUCTURE:");
        sb.AppendLine("{");
        sb.AppendLine("  \"understanding\": \"Brief explanation\",");
        sb.AppendLine("  \"confidence\": 0.95,");
        sb.AppendLine("  \"requiresCrossDatabaseJoin\": " + (allMappings.Any() && QueryRequiresCrossDatabaseMapping(userQuery, allMappings) ? "true" : "false") + ",");
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
        sb.AppendLine("VALIDATION CHECKLIST:");
        sb.AppendLine("  ✓ Output starts with '{' and ends with '}'");
        sb.AppendLine("  ✓ NO markdown formatting (no ```json)");
        sb.AppendLine("  ✓ NO explanatory text before/after JSON");
        sb.AppendLine("  ✓ Each table in requiredTables EXISTS in that database's schema");
        sb.AppendLine("  ✓ databaseId and databaseName are EXACT matches from AVAILABLE SCHEMAS");
        sb.AppendLine("  ✓ priority field is set correctly (1 for aggregation, 2+ for descriptive)");
        sb.AppendLine();
        if (allMappings.Any() && QueryRequiresCrossDatabaseMapping(userQuery, allMappings))
        {
            sb.AppendLine("⚠️⚠️ FINAL CHECK: Query requires multi-database access - did you include BOTH databases with correct priorities?");
            sb.AppendLine();
        }

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

    private List<CrossDatabaseMapping> GetAllCrossDatabaseMappings()
    {
        var mappings = new List<CrossDatabaseMapping>();
        
        if (_connectionManager == null)
            return mappings;

        try
        {
            var connectionsTask = _connectionManager.GetAllConnectionsAsync();
            var connections = connectionsTask.GetAwaiter().GetResult();
            
            foreach (var connection in connections)
            {
                if (connection?.CrossDatabaseMappings == null)
                    continue;
                
                mappings.AddRange(connection.CrossDatabaseMappings);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve cross-database mappings");
        }

        return mappings;
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


