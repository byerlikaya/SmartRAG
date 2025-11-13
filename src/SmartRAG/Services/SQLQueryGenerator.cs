using Microsoft.Extensions.Logging;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartRAG.Services
{
    /// <summary>
    /// Generates optimized SQL queries for databases based on query intent
    /// </summary>
    public class SQLQueryGenerator : ISQLQueryGenerator
    {
        #region Constants

        private const int MaxRetries = 3;
        private const int SampleDataLimit = 200;
        private const int MaxFilterKeywords = 12;
        private static readonly string[] ClauseTerminators = new[]
        {
            " ORDER BY",
            " GROUP BY",
            " HAVING",
            " LIMIT",
            " OFFSET",
            " FETCH",
            " UNION",
            " EXCEPT",
            " INTERSECT",
            " FOR",
            ";"
        };

        private static readonly HashSet<string> FilterStopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "and",
            "the",
            "for",
            "with",
            "from",
            "into",
            "onto",
            "about",
            "over",
            "under",
            "between",
            "within",
            "without",
            "will",
            "would",
            "could",
            "should",
            "have",
            "has",
            "had",
            "been",
            "being",
            "than",
            "then",
            "them",
            "they",
            "their",
            "there",
            "those",
            "these",
            "when",
            "where",
            "which",
            "while",
            "whose",
            "what",
            "that",
            "this",
            "each",
            "ever",
            "every",
            "many",
            "much",
            "more",
            "most",
            "some",
            "such",
            "only",
            "also",
            "just",
            "like",
            "make",
            "take",
            "give",
            "need",
            "want",
            "time",
            "date",
            "question",
            "asked",
            "asking",
            "show",
            "list",
            "tell",
            "provide",
            "please"
        };

        #endregion

        #region Fields

        private readonly IDatabaseSchemaAnalyzer _schemaAnalyzer;
        private readonly IAIService _aiService;
        private readonly ILogger<SQLQueryGenerator> _logger;

        #endregion

        #region Constructor

        public SQLQueryGenerator(
            IDatabaseSchemaAnalyzer schemaAnalyzer,
            IAIService aiService,
            ILogger<SQLQueryGenerator> logger)
        {
            _schemaAnalyzer = schemaAnalyzer;
            _aiService = aiService;
            _logger = logger;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates optimized SQL queries for each database based on intent
        /// </summary>
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
                    var basePrompt = BuildSQLGenerationPrompt(queryIntent.OriginalQuery, dbQuery, schema);
                    
                    // CRITICAL: Use few-shot learning - show AI EXACTLY what to do
                    var prompt = new StringBuilder();
                    prompt.AppendLine("You are a SQL TRANSLATOR. Your ONLY job:");
                    prompt.AppendLine("1. Read the schema (tables + columns + foreign keys)");
                    prompt.AppendLine("2. Write ONE simple SELECT with max 2 JOINs");
                    prompt.AppendLine("3. NO nested subqueries (FORBIDDEN!)");
                    prompt.AppendLine("4. ALWAYS join using: TableA.ForeignKeyColumn = TableB.PrimaryKeyColumn");
                    prompt.AppendLine();
                    prompt.AppendLine("EXAMPLE 1: Find items with classification X");
                    prompt.AppendLine("Schema:");
                    prompt.AppendLine("  Table1(ID [PK], Name, ClassificationID [FK→Table2.ID])");
                    prompt.AppendLine("  Table2(ID [PK], ClassName)");
                    prompt.AppendLine("JOIN PATTERN: Table1.ClassificationID = Table2.ID");
                    prompt.AppendLine();
                    prompt.AppendLine("CORRECT:");
                    prompt.AppendLine("SELECT t1.ID, t1.Name, t2.ClassName");
                    prompt.AppendLine("FROM Table1 t1");
                    prompt.AppendLine("JOIN Table2 t2 ON t1.ClassificationID = t2.ID  ← FK to PK!");
                    prompt.AppendLine("WHERE t2.ClassName = 'X'");
                    prompt.AppendLine("LIMIT 100");
                    prompt.AppendLine();
                    prompt.AppendLine("EXAMPLE 2: Count by classification");
                    prompt.AppendLine("Schema: Table1(ID [PK], ClassificationID, Value)");
                    prompt.AppendLine("CORRECT:");
                    prompt.AppendLine("SELECT ClassificationID, SUM(Value) AS Total");
                    prompt.AppendLine("FROM Table1");
                    prompt.AppendLine("GROUP BY ClassificationID");
                    prompt.AppendLine("ORDER BY Total DESC");
                    prompt.AppendLine("LIMIT 100");
                    prompt.AppendLine();
                    prompt.AppendLine("WRONG (NEVER DO THIS):");
                    prompt.AppendLine("  ✗ JOIN Table1 t1 ON t1.ID = t1.ID  ← Self-join!");
                    prompt.AppendLine("  ✗ WHERE ID IN (SELECT ...) ← Nested subquery!");
                    prompt.AppendLine();
                    prompt.AppendLine("═══════════════════════════════════════════════════════════════");
                    prompt.AppendLine("NOW GENERATE SQL FOR THIS USER QUESTION:");
                    prompt.AppendLine("═══════════════════════════════════════════════════════════════");
                    prompt.AppendLine();
                    prompt.Append(basePrompt);
                    
                    // Generate SQL using AI
                    var sql = await _aiService.GenerateResponseAsync(prompt.ToString(), new List<string>());
                    _logger.LogDebug("AI raw response for {DatabaseId}: {RawSQL}", dbQuery.DatabaseId, sql);

                    // Extract actual SQL from AI response
                    var extractedSql = ExtractSQLFromAIResponse(sql);
                    _logger.LogDebug("Extracted SQL for {DatabaseId}: {ExtractedSQL}", dbQuery.DatabaseId, extractedSql);
                    
                    // Validate SQL completeness FIRST
                    if (!IsCompleteSql(extractedSql))
                    {
                        _logger.LogWarning("Generated SQL is incomplete for {DatabaseId}, skipping", dbQuery.DatabaseId);
                        dbQuery.GeneratedQuery = null;
                        continue;
                    }
                    
                    dbQuery.GeneratedQuery = extractedSql;
                    
                    // CRITICAL: Multi-Retry validation with progressive strategies
                    bool validationPassed = false;
                    List<string> allErrors = new List<string>();
                    
                    for (int retryAttempt = 0; retryAttempt <= MaxRetries; retryAttempt++)
                    {
                        var currentErrors = new List<string>();
                        
                        // Validate #0: Check for forbidden filter keywords (CRITICAL)
                        // Only reject if:
                        // 1. Keyword doesn't match any column name pattern (e.g., "classification" → ClassificationName)
                        // 2. AND there are no TEXT columns that could contain this value
                        var filterKeywords = ExtractFilterKeywords(queryIntent.OriginalQuery);
                        var allowedTables = schema.Tables
                            .Where(t => dbQuery.RequiredTables.Contains(t.TableName, StringComparer.OrdinalIgnoreCase))
                            .ToList();
                        
                        // Check if any TEXT/VARCHAR columns exist that could store the filter value
                        var hasTextColumns = allowedTables
                            .SelectMany(t => t.Columns)
                            .Any(c => c.DataType?.Contains("varchar", StringComparison.OrdinalIgnoreCase) == true ||
                                     c.DataType?.Contains("text", StringComparison.OrdinalIgnoreCase) == true ||
                                     c.DataType?.Contains("nvarchar", StringComparison.OrdinalIgnoreCase) == true ||
                                     c.DataType?.Contains("char", StringComparison.OrdinalIgnoreCase) == true);
                        
                        var forbiddenKeywords = filterKeywords
                            .Where(keyword => !string.IsNullOrWhiteSpace(keyword) && keyword.Length >= 3)
                            .Where(keyword => !ColumnMatchesAnySchemaElement(keyword, allowedTables))
                            .ToList();
                        
                        if (forbiddenKeywords.Any() && dbQuery.GeneratedQuery.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
                        {
                            // Check if SQL contains forbidden keywords in WHERE clause
                            var whereMatch = Regex.Match(dbQuery.GeneratedQuery, @"WHERE\s+(.+?)(?:GROUP\s+BY|ORDER\s+BY|LIMIT|OFFSET|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                            if (whereMatch.Success)
                            {
                                var whereClause = whereMatch.Groups[1].Value;
                                foreach (var forbidden in forbiddenKeywords)
                                {
                                    if (whereClause.IndexOf(forbidden, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        currentErrors.Add($"FORBIDDEN: SQL uses filter keyword '{forbidden}' but no TEXT columns exist to filter. Remove WHERE clause and return foreign keys only.");
                                    }
                                }
                            }
                        }

                        var columnKeywordMap = BuildColumnKeywordMap(filterKeywords, allowedTables);
                        var matchedFilterKeywords = new HashSet<string>(columnKeywordMap.Values.SelectMany(v => v), StringComparer.OrdinalIgnoreCase);

                        if (matchedFilterKeywords.Count > 0 && !ContainsTextFilterForAllowedColumns(dbQuery.GeneratedQuery, allowedTables, columnKeywordMap))
                        {
                            currentErrors.Add("MANDATORY: Add a WHERE clause filtering text columns (e.g., ColumnName LIKE '%keyword%') that matches the user keywords.");
                        }

                        if (dbQuery.GeneratedQuery.Contains("CROSS JOIN", StringComparison.OrdinalIgnoreCase))
                        {
                            currentErrors.Add("FORBIDDEN: CROSS JOIN is not allowed. Use explicit INNER JOIN or LEFT JOIN with matching foreign keys.");
                        }
                        
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
                        
                        if (retryAttempt == MaxRetries)
                        {
                            // Max retries reached
                            _logger.LogWarning("SQL validation failed after {MaxRetries} attempts for {DatabaseName}", MaxRetries, schema.DatabaseName);
                            foreach (var error in allErrors.Distinct())
                            {
                                _logger.LogWarning("  - {Error}", error);
                            }
                            break;
                        }
                        
                        // Retry with progressively stricter prompts
                        _logger.LogDebug("Retry attempt {Attempt}/{MaxRetries} for {DatabaseName}", retryAttempt + 1, MaxRetries, schema.DatabaseName);
                        
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
                        _logger.LogWarning("Could not generate valid SQL for {DatabaseName} after {MaxRetries} attempts, skipping", schema.DatabaseName, MaxRetries);
                        dbQuery.GeneratedQuery = null;
                        continue;
                    }

                    
                    // Log the generated SQL result (only if failed)
                    if (string.IsNullOrEmpty(dbQuery.GeneratedQuery))
                    {
                        _logger.LogWarning("SQL generation failed validation for {DatabaseId}", dbQuery.DatabaseId);
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

        #endregion

        #region Private Helper Methods

        private string BuildSQLGenerationPrompt(string userQuery, DatabaseQueryIntent dbQuery, DatabaseSchemaInfo schema)
        {
            var sb = new StringBuilder();
            var filterKeywords = ExtractFilterKeywords(userQuery);
            var allowedTableSchemas = schema.Tables
                .Where(t => dbQuery.RequiredTables.Contains(t.TableName, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var firstTable = allowedTableSchemas.FirstOrDefault();
            var allAllowedColumns = allowedTableSchemas
                .SelectMany(t => t.Columns)
                .Select(c => c.ColumnName)
                .ToList();

            bool ContainsColumnFragment(string fragment) =>
                allAllowedColumns.Any(name => name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);

            var unmatchedFilterKeywords = filterKeywords
                .Where(keyword => !ContainsColumnFragment(keyword))
                .Take(5)
                .ToList();
            
            sb.AppendLine("═══════════════════════════════════════════════════════════════════");
            sb.AppendLine("          SQL QUERY GENERATION - ANSWER THE USER'S QUESTION              ");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║  🚨 MANDATORY: WRITE SIMPLE SQL - NO COMPLEX QUERIES! 🚨    ║");
            sb.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("REQUIRED QUERY PATTERN (DO NOT DEVIATE):");
            sb.AppendLine("  1. Simple SELECT with descriptive columns");
            sb.AppendLine("  2. Maximum 2 JOINs (table1 JOIN table2 JOIN table3)");
            sb.AppendLine("  3. Simple WHERE clause (1-2 conditions maximum)");
            sb.AppendLine("  4. Simple ORDER BY (1 column)");
            sb.AppendLine("  5. LIMIT 100 at the end");
            sb.AppendLine();
            sb.AppendLine("ABSOLUTELY FORBIDDEN:");
            sb.AppendLine("  ✗ NO nested subqueries (no SELECT inside WHERE)");
            sb.AppendLine("  ✗ NO complex logic (no multiple levels of nesting)");
            sb.AppendLine("  ✗ NO aggregate functions in WHERE clause");
            sb.AppendLine("  ✗ NO more than 2 JOINs");
            sb.AppendLine();
            sb.AppendLine("EXAMPLE VALID QUERY:");
            sb.AppendLine("  SELECT t1.NameColumn, t2.DescColumn, t1.ID");
            sb.AppendLine("  FROM Table1 t1");
            sb.AppendLine("  JOIN Table2 t2 ON t1.ForeignKeyID = t2.ID");
            sb.AppendLine("  WHERE t2.TextColumn = 'value'");
            sb.AppendLine("  ORDER BY t1.DateColumn DESC");
            sb.AppendLine("  LIMIT 100");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("USER'S QUESTION:");
            sb.AppendLine($"   \"{userQuery}\"");
            sb.AppendLine();
            sb.AppendLine("YOUR TASK FOR THIS DATABASE:");
            sb.AppendLine($"   {dbQuery.Purpose}");
            sb.AppendLine();
            sb.AppendLine("YOUR GOAL:");
            sb.AppendLine("   Write SQL that retrieves the EXACT data needed to answer the question!");
            sb.AppendLine("   - If question asks 'which classification', you must JOIN to get the descriptive text");
            sb.AppendLine("   - If question asks for top/most/highest, you must use GROUP BY + SUM + ORDER BY");
            sb.AppendLine("   - If question asks for latest/most recent, you must use ORDER BY date DESC");
            sb.AppendLine("   - If question has a filter (e.g. 'specific classification'), you must use WHERE");
            sb.AppendLine();
            sb.AppendLine("CRITICAL RULES:");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("1. Use ONLY columns listed in schema below (case-sensitive)");
            sb.AppendLine("2. Use ONLY tables listed in your allowed tables");
            sb.AppendLine("3. ✓ YOU CAN JOIN tables within THIS database");
            sb.AppendLine("4. ✗ DO NOT JOIN with tables from OTHER databases");
            sb.AppendLine("5. ✓ USE WHERE clauses to filter data based on user's question");
            sb.AppendLine("6. ✓ USE GROUP BY + aggregates (SUM, COUNT, MAX) when needed");
            sb.AppendLine("7. ✓ USE ORDER BY to sort results (especially for 'top', 'most', 'best')");
            sb.AppendLine("8. DO NOT use parameters (@param, :param) - use actual values");
            sb.AppendLine("9. Match database type syntax (SQL Server=TOP, Others=LIMIT)");
            sb.AppendLine("10. Return SPECIFIC data that answers the question, not just 'SELECT *'");
            sb.AppendLine("11. MAXIMUM 2 levels of subqueries - NO MORE!");
            sb.AppendLine("12. Keep SQL SIMPLE and EFFICIENT - avoid nested loops!");
            sb.AppendLine("13. NEVER put aggregates (SUM/COUNT/MAX/MIN) inside WHERE - use HAVING");
            sb.AppendLine("14. Confirm literal values using sample data before using '='; prefer case-insensitive LIKE filters when unsure.");
            sb.AppendLine("15. Always balance parentheses – every '(' must have a matching ')'.");
            sb.AppendLine("16. Prefer CTEs or single-level subqueries instead of deeply nested SELECT chains.");
            sb.AppendLine("17. If a column or table is not listed below, DO NOT invent it – return the foreign key (ID column) so other databases can enrich the data.");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"TARGET DATABASE: {schema.DatabaseName} ({schema.DatabaseType})");
            sb.AppendLine();
            sb.AppendLine("TABLES AVAILABLE IN THIS DATABASE (ONLY THESE EXIST):");
            sb.AppendLine($"   {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine();
            sb.AppendLine("TABLES NOT AVAILABLE IN THIS DATABASE (DO NOT USE, THEY DON'T EXIST HERE):");
            var otherTablesFromSchema = schema.Tables
                .Where(t => !dbQuery.RequiredTables.Contains(t.TableName, StringComparer.OrdinalIgnoreCase))
                .Select(t => t.TableName)
                .ToList();
            if (otherTablesFromSchema.Any())
            {
                sb.AppendLine($"   {string.Join(", ", otherTablesFromSchema)}");
                sb.AppendLine("   🚨 THESE TABLES ARE IN OTHER DATABASES - DO NOT REFERENCE THEM!");
            }
            else
            {
                sb.AppendLine("   (All tables in schema are available)");
            }
            sb.AppendLine();
            sb.AppendLine("USER QUESTION:");
            sb.AppendLine($"  {userQuery}");
            sb.AppendLine();
            sb.AppendLine("WHAT TO RETRIEVE FROM THIS DATABASE:");
            sb.AppendLine($"  {dbQuery.Purpose}");
            sb.AppendLine();
            sb.AppendLine("CRITICAL COLUMN SELECTION:");
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
            sb.AppendLine("  Step 4: Identify which table holds the descriptive text (e.g., a text column containing classification/name values)");
            sb.AppendLine("  Step 5: JOIN that table and filter using its actual column values");
            sb.AppendLine();

            if (filterKeywords.Count > 0)
            {
                sb.AppendLine("TEXT FILTER KEYWORDS FROM QUESTION (use case-insensitive LIKE):");
                sb.AppendLine($"  {string.Join(", ", filterKeywords)}");
                sb.AppendLine("Use patterns like LOWER(ColumnName) LIKE '%keyword%' unless sample data shows the exact stored value.");
                sb.AppendLine("Check the sample data shown below to learn the REAL stored values (language, plural form).");
                sb.AppendLine("If sample data and user wording differ, combine both: e.g. LOWER(ColumnName) LIKE '%keyword%' OR LOWER(ColumnName) LIKE '%storedvalue%'.");
                sb.AppendLine("Never assume the user's language matches the stored data; rely on sample rows to decide the correct literal or patterns.");
                sb.AppendLine();
            }

            if (unmatchedFilterKeywords.Any())
            {
                sb.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
                sb.AppendLine("║  🚨 ABSOLUTELY FORBIDDEN - WILL CAUSE IMMEDIATE FAILURE 🚨   ║");
                sb.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
                sb.AppendLine();
                sb.AppendLine("The following filter keywords from user question do NOT exist as columns in this database:");
                sb.AppendLine($"    {string.Join(", ", unmatchedFilterKeywords)}");
                sb.AppendLine();
                sb.AppendLine("THESE KEYWORDS ARE FORBIDDEN IN YOUR SQL:");
                foreach (var keyword in unmatchedFilterKeywords)
                {
                    sb.AppendLine($"  ✗ FORBIDDEN: WHERE ... LIKE '%{keyword}%'");
                    sb.AppendLine($"  ✗ FORBIDDEN: WHERE ColumnName = '{keyword}'");
                    sb.AppendLine($"  ✗ FORBIDDEN: Any reference to '{keyword}' in WHERE clause");
                }
                sb.AppendLine();
                sb.AppendLine("WHY? Because you don't have columns containing these values!");
                sb.AppendLine();
                sb.AppendLine("WHAT YOU MUST DO INSTEAD:");
                sb.AppendLine("  ✓ IGNORE these filter keywords completely");
                sb.AppendLine("  ✓ DO NOT try to filter using similar column names");
                sb.AppendLine("  ✓ DO NOT try to guess related columns");
                sb.AppendLine("  ✓ RETURN ALL ROWS with foreign key columns");
                sb.AppendLine("  ✓ Application will handle filtering using other databases");
                sb.AppendLine();
                sb.AppendLine("MANDATORY EXAMPLE:");
                sb.AppendLine("  ✓ CORRECT: SELECT ForeignKeyID, NumericColumn FROM TableA");
                sb.AppendLine("           (No WHERE clause, returns ALL rows)");
                sb.AppendLine();
                sb.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
                sb.AppendLine();
            }

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
            sb.AppendLine("  Task mentions CLASSIFICATION (type, group, label):");
            sb.AppendLine("  → Find TEXT columns: Search for classification patterns in column names");
            sb.AppendLine();
            sb.AppendLine("MATCH CONCEPTS TO COLUMN NAME PATTERNS IN SCHEMA BELOW!");
            sb.AppendLine("INCLUDE: All matching columns + ALL foreign keys (ending with 'ID')!");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"TABLES AVAILABLE IN {schema.DatabaseName}:");
            sb.AppendLine("═══════════════════════════════════════");
            
            foreach (var tableName in dbQuery.RequiredTables)
            {
                var table = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                if (table != null)
                {
                    sb.AppendLine($"\nTable: {table.TableName}");
                    sb.AppendLine("═══════════════════════════════════════");
                    sb.AppendLine($"AVAILABLE COLUMNS (use EXACT names, case-sensitive):");
                    
                    var columnList = string.Join(", ", table.Columns.Select(c => c.ColumnName));
                    sb.AppendLine($"  {columnList}");
                    
                    sb.AppendLine();
                    sb.AppendLine($"YOU CAN ONLY USE THESE {table.Columns.Count} COLUMNS FROM {table.TableName}");
                    sb.AppendLine("ANY OTHER COLUMN NAME WILL CAUSE ERROR!");
                    
                    if (table.ForeignKeys.Any())
                    {
                        sb.AppendLine();
                        sb.AppendLine("Foreign Keys (use these for JOINs):");
                        foreach (var fk in table.ForeignKeys)
                        {
                            var referencedTarget = string.IsNullOrWhiteSpace(fk.ReferencedTable)
                                ? "UNKNOWN TABLE"
                                : $"{fk.ReferencedTable}.{(string.IsNullOrWhiteSpace(fk.ReferencedColumn) ? "ID" : fk.ReferencedColumn)}";

                            sb.AppendLine($"  {fk.ColumnName} → {referencedTarget}");
                        }
                        sb.AppendLine("Always JOIN using the exact foreign key column shown above.");
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
                sb.AppendLine("  Referencing any of the tables above will fail. Return their foreign key IDs instead.");
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
            sb.AppendLine("CRITICAL CROSS-DATABASE RULE:");
            sb.AppendLine($"   This database ({schema.DatabaseName}) does NOT contain all tables!");
            sb.AppendLine($"   If user query mentions tables that are NOT in your allowed list:");
            sb.AppendLine($"   → DO NOT try to JOIN with tables not in your allowed list!");
            sb.AppendLine($"   → DO NOT use subqueries referencing tables not in your allowed list!");
            sb.AppendLine($"   → Instead: Return foreign key columns (ID columns ending with 'ID') for merging!");
            sb.AppendLine($"   → Application will merge data from other databases automatically!");
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
            sb.AppendLine("   ✓ Join the table that contains the descriptive text you filter on");
            sb.AppendLine("   ✗ NEVER use aggregates (SUM, AVG, COUNT) in WHERE clause!");
            sb.AppendLine("   ✓ Use HAVING for filtering aggregated values");
            sb.AppendLine("   ✓ If using GROUP BY: ALL non-aggregate SELECT columns must be in GROUP BY");
            sb.AppendLine("   ✗ If using GROUP BY + ORDER BY: ORDER BY columns must ALSO be in GROUP BY (or use aggregates)");
            sb.AppendLine("   CRITICAL: NEVER use more than 3 levels of nested subqueries!");
            sb.AppendLine("   ✗ WRONG: SELECT ... WHERE X IN (SELECT ... WHERE Y IN (SELECT ... WHERE Z IN (SELECT ...))))");
            sb.AppendLine("   ✓ CORRECT: Use JOINs and GROUP BY instead of nested subqueries!");
            sb.AppendLine("   ✓ Example: SELECT Col1, SUM(Col2) FROM T GROUP BY Col1 ORDER BY Col1  (CORRECT)");
            sb.AppendLine("   ✓ Example: SELECT Col1, SUM(Col2) AS Total FROM T GROUP BY Col1 ORDER BY Total DESC  (CORRECT - aggregate alias)");
            sb.AppendLine("   ✗ Example: SELECT Col1, SUM(Col2) FROM T GROUP BY Col1 ORDER BY Col3  (ERROR - Col3 not in GROUP BY)");
            sb.AppendLine("   ✗ Example: WHERE SUM(Col2) > 100  (ERROR - use HAVING SUM(Col2) > 100)");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("HOW TO ANSWER USER'S QUESTION:");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("STEP 1: Understand what user is asking");
            sb.AppendLine($"   Question: \"{userQuery}\"");
            sb.AppendLine("   → What data do they want?");
            sb.AppendLine("   → What filters/conditions apply?");
            sb.AppendLine("   → Do they want aggregation (count, sum, max)?");
            sb.AppendLine("   → Do they want sorting (top, best, most)?");
            sb.AppendLine();
            sb.AppendLine("STEP 2: Build SQL to answer the question");
            sb.AppendLine("   → SELECT the columns that contain the answer");
            sb.AppendLine("   → FROM the relevant table(s)");
            sb.AppendLine("   → JOIN if data is spread across multiple tables");
            sb.AppendLine("   → WHERE to filter based on conditions");
            sb.AppendLine("   → GROUP BY if you need aggregation");
            sb.AppendLine("   → ORDER BY if user wants 'top', 'best', 'most', 'least'");
            sb.AppendLine("   → LIMIT/TOP to return relevant rows");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("SQL PATTERN EXAMPLES:");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("Pattern 1: 'Which classification does item X belong to?'");
            sb.AppendLine("  → Need: Item identifier + descriptive classification value");
            sb.AppendLine("  → Solution: JOIN with the table that stores classification text");
            if (dbQuery.RequiredTables.Count > 1)
            {
                var table1 = dbQuery.RequiredTables[0];
                var table2 = dbQuery.RequiredTables[1];
                sb.AppendLine($"  SELECT t1.NameColumn, t2.ClassificationColumn");
                sb.AppendLine($"  FROM {table1} t1");
                sb.AppendLine($"  JOIN {table2} t2 ON t1.ForeignKeyID = t2.ID");
                sb.AppendLine($"  WHERE t1.NameColumn = 'ValueFromUserQuery'");
            }
            sb.AppendLine();
            sb.AppendLine("Pattern 2: 'What is the top/most/best X?' (AGGREGATION PATTERN)");
            sb.AppendLine("  → Need: Aggregation (SUM, COUNT, MAX) + GROUP BY + ORDER BY + LIMIT/TOP");
            sb.AppendLine("  CRITICAL: If user asks MULTIPLE questions combined:");
            sb.AppendLine("      → Example: 'highest value' + 'most recent' + 'with specific classification'");
            sb.AppendLine("      → SPLIT THE PROBLEM: Handle ONE concept per database!");
            sb.AppendLine("      → Database1 (with descriptive text columns) handles text-based filters");
            sb.AppendLine("      → Database2 (with numeric/metric columns) handles aggregation");
            sb.AppendLine("      → Application merges results using foreign keys");
            sb.AppendLine("  ✓ CORRECT STRATEGY: Return foreign keys + aggregates, let app handle the rest");
            sb.AppendLine("  ✗ WRONG STRATEGY: Try to answer everything in one complex nested query");
            sb.AppendLine();
            if (schema.DatabaseType == DatabaseType.SqlServer)
            {
                sb.AppendLine($"  SELECT TOP 1 ReferenceID, SUM(NumericColumn) AS Total");
                sb.AppendLine($"  FROM {dbQuery.RequiredTables[0]}");
                sb.AppendLine($"  GROUP BY ReferenceID");
                sb.AppendLine($"  ORDER BY Total DESC");
            }
            else
            {
                sb.AppendLine($"  SELECT ReferenceID, SUM(NumericColumn) AS Total");
                sb.AppendLine($"  FROM {dbQuery.RequiredTables[0]}");
                sb.AppendLine($"  GROUP BY ReferenceID");
                sb.AppendLine($"  ORDER BY Total DESC");
                sb.AppendLine($"  LIMIT 1");
            }
            sb.AppendLine();
            sb.AppendLine("Pattern 2b: 'What is the top item within a specific classification?'");
            sb.AppendLine("  → Need: JOIN + WHERE + Aggregation + GROUP BY + ORDER BY + LIMIT/TOP");
            if (dbQuery.RequiredTables.Count > 1 && schema.DatabaseType == DatabaseType.SqlServer)
            {
                var table1 = dbQuery.RequiredTables[0];
                var table2 = dbQuery.RequiredTables[1];
                sb.AppendLine($"  SELECT TOP 1 t1.ReferenceID, SUM(t1.NumericColumn) AS Total");
                sb.AppendLine($"  FROM {table1} t1");
                sb.AppendLine($"  JOIN {table2} t2 ON t1.ForeignKeyID = t2.ID");
                sb.AppendLine($"  WHERE t2.ClassificationColumn = 'ValueFromUserQuery'");
                sb.AppendLine($"  GROUP BY t1.ReferenceID");
                sb.AppendLine($"  ORDER BY Total DESC");
            }
            else if (dbQuery.RequiredTables.Count > 1)
            {
                var table1 = dbQuery.RequiredTables[0];
                var table2 = dbQuery.RequiredTables[1];
                sb.AppendLine($"  SELECT t1.ReferenceID, SUM(t1.NumericColumn) AS Total");
                sb.AppendLine($"  FROM {table1} t1");
                sb.AppendLine($"  JOIN {table2} t2 ON t1.ForeignKeyID = t2.ID");
                sb.AppendLine($"  WHERE t2.ClassificationColumn = 'ValueFromUserQuery'");
                sb.AppendLine($"  GROUP BY t1.ReferenceID");
                sb.AppendLine($"  ORDER BY Total DESC");
                sb.AppendLine($"  LIMIT 1");
            }
            sb.AppendLine();
            sb.AppendLine("Pattern 3: 'Show all items restricted to a specific classification'");
            sb.AppendLine("  → Need: Filter with WHERE clause + JOIN");
            if (dbQuery.RequiredTables.Count > 1)
            {
                var table1 = dbQuery.RequiredTables[0];
                var table2 = dbQuery.RequiredTables[1];
                sb.AppendLine($"  SELECT t1.*");
                sb.AppendLine($"  FROM {table1} t1");
                sb.AppendLine($"  JOIN {table2} t2 ON t1.ForeignKeyID = t2.ID");
                sb.AppendLine($"  WHERE t2.ClassificationColumn = 'ValueFromUserQuery'");
            }
            sb.AppendLine();
            sb.AppendLine("🚨 ABSOLUTELY FORBIDDEN - WILL CAUSE ERRORS:");
            sb.AppendLine("  ✗ JOIN with tables not in allowed list");
            sb.AppendLine("  ✗ Use DatabaseName.TableName syntax");
            sb.AppendLine("  ✗ Reference columns from non-allowed tables");
            sb.AppendLine("  ✗ EXISTS/IN subqueries with non-allowed tables");
            sb.AppendLine("  ✗ ANY reference to tables outside your allowed list");
            sb.AppendLine("  More than 3 levels of nested subqueries (SIMPLIFY IMMEDIATELY!)");
            sb.AppendLine("  If tables mentioned in user query are NOT in your allowed list, DO NOT reference them!");
            sb.AppendLine("  ✗ Aggregate functions (SUM, AVG, COUNT) in WHERE clause (use HAVING instead)");
            sb.AppendLine();
            sb.AppendLine("CROSS-DATABASE QUERY STRATEGY:");
            sb.AppendLine($"   Your allowed tables: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine("   If user query needs data from tables that are NOT in your allowed list:");
            sb.AppendLine("   → DO NOT try to JOIN with tables not in your allowed list (they don't exist here!)");
            sb.AppendLine("   → DO NOT use subqueries like: WHERE ForeignKeyColumn IN (SELECT ... FROM OtherTable ...)");
            sb.AppendLine("   → Instead: Return foreign key columns (columns ending with 'ID')");
            sb.AppendLine("   → Example: SELECT ForeignKeyColumn, SUM(NumericColumn) FROM TableA GROUP BY ForeignKeyColumn");
            sb.AppendLine("   → Application will merge this with data from other database!");
            sb.AppendLine();
            sb.AppendLine("CRITICAL: Even in WHERE clause, subquery, or EXISTS:");
            sb.AppendLine("   You can ONLY use tables from your allowed list!");
            sb.AppendLine("   Example of FORBIDDEN patterns:");
            sb.AppendLine("     ✗ WHERE EXISTS (SELECT 1 FROM OtherTable ...)");
            sb.AppendLine("     ✗ WHERE ColumnX IN (SELECT ID FROM OtherTable)");
            sb.AppendLine("     ✗ JOIN OtherTable ON ...");
            sb.AppendLine("     ✗ WHERE SUM(Column) > 100  (use HAVING SUM(Column) > 100)");
            sb.AppendLine("     ✗ 7 levels of nested SELECT (maximum 3 levels allowed)");
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
                    sb.AppendLine("SQL SERVER DATABASE - CRITICAL SYNTAX RULES");
                    sb.AppendLine();
                    sb.AppendLine("ABSOLUTELY FORBIDDEN:");
                    sb.AppendLine("✗ LIMIT keyword (does not exist in SQL Server - use TOP instead)");
                    sb.AppendLine("✗ FETCH NEXT (use TOP instead)");
                    sb.AppendLine("✗ FETCH FIRST (use TOP instead)");
                    sb.AppendLine("✗ Parameters: @ParamName, :ParamName, ?");
                    sb.AppendLine("✗ Template syntax: <placeholder>, {variable}");
                    sb.AppendLine("✗ Placeholder values (ENTER_ID_HERE, etc.)");
                    sb.AppendLine("✗ Aggregate functions (SUM, AVG, COUNT) in WHERE clause (use HAVING instead)");
                    sb.AppendLine("✗ More than 3 levels of nested subqueries (simplify your query)");
                    sb.AppendLine();
                    sb.AppendLine("REQUIRED FORMAT:");
                    sb.AppendLine($"SELECT TOP 100 columns FROM {dbQuery.RequiredTables[0]} WHERE conditions ORDER BY column");
                    sb.AppendLine();
                    sb.AppendLine("CORRECT EXAMPLES:");
                    sb.AppendLine("✓ SELECT TOP 100 Column1, Column2 FROM TableName");
                    sb.AppendLine("✓ SELECT TOP 1 Column1, SUM(Column2) FROM TableName GROUP BY Column1 ORDER BY SUM(Column2) DESC");
                    sb.AppendLine("✓ SELECT Column1 FROM TableName ORDER BY Column1 OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY");
                    sb.AppendLine("✗ SELECT Column1 FROM TableName FETCH FIRST 10 ROWS ONLY (WRONG - use TOP instead)");
                    sb.AppendLine("✓ WHERE DateColumn >= DATEADD(month, -3, GETDATE())");
                    sb.AppendLine("✓ WHERE NumericColumn > 100 (use literal values, NOT @param)");
                    sb.AppendLine("✓ SELECT Column1, SUM(Column2) FROM TableName GROUP BY Column1 HAVING SUM(Column2) > 100");
                    sb.AppendLine("✗ SELECT Column1, SUM(Column2) FROM TableName WHERE SUM(Column2) > 100 (WRONG - use HAVING)");
                    sb.AppendLine("✓ WHERE ID = 1 (use actual values, not placeholders)");
                    sb.AppendLine("✓ JOIN is ALLOWED within same database: FROM TableA t1 JOIN TableB t2 ON t1.ID = t2.ID");
                    break;
                    
                case DatabaseType.SQLite:
                    sb.AppendLine("SQLITE DATABASE - CRITICAL SYNTAX RULES");
                    sb.AppendLine();
                    sb.AppendLine("ABSOLUTELY FORBIDDEN:");
                    sb.AppendLine("✗ TOP keyword (does not exist in SQLite)");
                    sb.AppendLine("✗ Columns not in schema");
                    sb.AppendLine("✗ Parameters: @ParamName, :ParamName, ?");
                    sb.AppendLine("✗ Template syntax: <placeholder>, {variable}");
                    sb.AppendLine("✗ Placeholder values (ENTER_ID_HERE, etc.)");
                    sb.AppendLine();
                    sb.AppendLine("REQUIRED FORMAT:");
                    sb.AppendLine($"SELECT columns FROM {dbQuery.RequiredTables[0]} WHERE conditions ORDER BY column LIMIT 100");
                    sb.AppendLine();
                    sb.AppendLine("CORRECT EXAMPLES:");
                    sb.AppendLine("✓ SELECT Column1, Column2 FROM TableName LIMIT 100");
                    sb.AppendLine("✓ WHERE DateColumn >= date('now', '-3 month')");
                    sb.AppendLine("✓ WHERE NumericColumn > 100 (use literal values, NOT ?)");
                    sb.AppendLine("✓ Use EXACT table/column casing from schema");
                    sb.AppendLine("✓ WHERE ID = 1 (use actual values, not placeholders)");
                    break;
                    
                case DatabaseType.MySQL:
                    sb.AppendLine("MYSQL DATABASE - CRITICAL SYNTAX RULES");
                    sb.AppendLine();
                    sb.AppendLine("ABSOLUTELY FORBIDDEN:");
                    sb.AppendLine("✗ TOP keyword (does not exist in MySQL)");
                    sb.AppendLine("✗ Parameters: @ParamName, :ParamName, ?");
                    sb.AppendLine("✗ Template syntax: <placeholder>, {variable}");
                    sb.AppendLine("✗ Placeholder values (ENTER_ID_HERE, etc.)");
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
                    sb.AppendLine("✓ WHERE ID = 1 (use actual values, not placeholders)");
                    break;
                    
                case DatabaseType.PostgreSQL:
                    sb.AppendLine("POSTGRESQL DATABASE - CRITICAL SYNTAX RULES");
                    sb.AppendLine();
                    sb.AppendLine("ABSOLUTELY FORBIDDEN:");
                    sb.AppendLine("✗ TOP keyword (does not exist in PostgreSQL)");
                    sb.AppendLine("✗ INTERVAL without quotes");
                    sb.AppendLine("✗ Parameters: @ParamName, :ParamName, ?");
                    sb.AppendLine("✗ Template syntax: <placeholder>, {variable}");
                    sb.AppendLine("✗ AVG() function on string/text columns (use numeric columns only)");
                    sb.AppendLine("✗ Placeholder values (ENTER_ID_HERE, etc.)");
                    sb.AppendLine();
                    sb.AppendLine("REQUIRED FORMAT:");
                    sb.AppendLine($"SELECT columns FROM {dbQuery.RequiredTables[0]} WHERE conditions ORDER BY column LIMIT 100");
                    sb.AppendLine();
                    sb.AppendLine("CORRECT EXAMPLES:");
                    sb.AppendLine("✓ SELECT Column1, Column2 FROM TableName LIMIT 100");
                    sb.AppendLine("✓ WHERE DateColumn >= CURRENT_DATE - INTERVAL '3 months'");
                    sb.AppendLine("✓ WHERE DateColumn >= NOW() - INTERVAL '30 days'");
                    sb.AppendLine("✓ WHERE NumericColumn > 100 (use literal values, NOT $1)");
                    sb.AppendLine("✓ SELECT AVG(NumericColumn) FROM TableName (numeric columns only)");
                    sb.AppendLine("✓ WHERE ID = 1 (use actual values, not placeholders)");
                    sb.AppendLine();
                    sb.AppendLine("DATE/TIME ARITHMETIC:");
                    sb.AppendLine("✗ WRONG: INTERVAL 30 DAYS (no quotes)");
                    sb.AppendLine("✓ CORRECT: INTERVAL '30 days' (with quotes!)");
                    sb.AppendLine();
                    sb.AppendLine("AGGREGATE FUNCTIONS:");
                    sb.AppendLine("✗ WRONG: AVG(TextColumn) (string column)");
                    sb.AppendLine("✓ CORRECT: AVG(NumericColumn) (numeric column)");
                    sb.AppendLine("✓ CORRECT: COUNT(*) (count all rows)");
                    sb.AppendLine("✓ CORRECT: SUM(numeric_column) (sum numeric values)");
                    break;
            }
            
            sb.AppendLine();
            sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║                   CRITICAL OUTPUT RULES                        ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("LANGUAGE-CRITICAL RULE:");
            sb.AppendLine("   SQL is a COMPUTER LANGUAGE - it ONLY understands SQL keywords!");
            sb.AppendLine("   ✗ NEVER write any human language text in SQL (regardless of user's language)");
            sb.AppendLine("   ✗ NEVER write comments or explanations in SQL");
            sb.AppendLine("   ✗ NEVER translate SQL keywords to other languages");
            sb.AppendLine("   ✓ ONLY use English SQL keywords: SELECT, FROM, WHERE, JOIN, etc.");
            sb.AppendLine();
            sb.AppendLine("   BAD EXAMPLES (will cause syntax errors):");
            sb.AppendLine("   ✗ Any human language text mixed with SQL");
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
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("FINAL ANSWER CHECKLIST:");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"User asked: \"{userQuery}\"");
            sb.AppendLine();
            sb.AppendLine("Before submitting your SQL, verify:");
            sb.AppendLine("✓ Does my SQL directly answer the user's question?");
            sb.AppendLine("✓ Did I use JOIN if data is in multiple tables?");
            sb.AppendLine("✓ Did I use WHERE to filter based on conditions?");
            sb.AppendLine("✓ Did I use GROUP BY for aggregation (sum, count, avg)?");
            sb.AppendLine("✓ Did I use HAVING (not WHERE) for filtering aggregates?");
            sb.AppendLine("✓ Did I use ORDER BY for 'top', 'most', 'best' queries?");
            sb.AppendLine("✓ Did I use LIMIT/TOP to restrict results?");
            sb.AppendLine("✓ Are all columns verified in the schema above?");
            sb.AppendLine("CRITICAL: Did I use JOINs instead of nested subqueries?");
            sb.AppendLine("CRITICAL: NO nested SELECT inside WHERE clause!");
            sb.AppendLine("Did I check if all tables I reference are in my allowed list?");
            sb.AppendLine("If tables mentioned in user query are NOT in my list, did I avoid referencing them?");
            sb.AppendLine("✓ Is it pure SQL with NO comments or explanations?");
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
            sb.AppendLine("GENERIC SQL PATTERN TEMPLATES:");
            sb.AppendLine("  ✓ Use a CTE to calculate aggregates, then select the ranked result:");
            sb.AppendLine("     WITH RankedData AS (");
            sb.AppendLine("         SELECT ColumnA, SUM(NumericColumn) AS TotalValue");
            sb.AppendLine("         FROM TableA");
            sb.AppendLine("         GROUP BY ColumnA");
            sb.AppendLine("     )");
            if (schema.DatabaseType == DatabaseType.SqlServer)
            {
                sb.AppendLine("     SELECT TOP 1 *");
                sb.AppendLine("     FROM RankedData");
                sb.AppendLine("     ORDER BY TotalValue DESC;");
            }
            else
            {
                sb.AppendLine("     SELECT *");
                sb.AppendLine("     FROM RankedData");
                sb.AppendLine("     ORDER BY TotalValue DESC");
                sb.AppendLine("     LIMIT 1;");
            }
            sb.AppendLine("  ✓ To get the most recent record, ORDER BY the datetime column DESC and use TOP/LIMIT 1.");
            sb.AppendLine("  ✗ Avoid nesting SELECT statements more than two levels deep – refactor into CTEs instead.");
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
            sb.AppendLine("  - DO NOT use placeholder values (ENTER_ID_HERE, etc.)");
            sb.AppendLine("  - DO NOT use AVG() on string/text columns - use numeric columns only");
            sb.AppendLine("  - Use actual values (1, 2, 3) instead of placeholders");
            sb.AppendLine();
            sb.AppendLine("LANGUAGE ENFORCEMENT:");
            sb.AppendLine("  - SQL is ENGLISH-ONLY computer language");
            sb.AppendLine("  - Even if user question is in any non-English language:");
            sb.AppendLine("    ✓ SQL must still be pure English SQL");
            sb.AppendLine("    ✗ NO human language text in SQL output (regardless of user's language)");
            sb.AppendLine("  - Example: User asks in any language");
            sb.AppendLine("    ✓ Correct: SELECT * FROM TableA");
            sb.AppendLine("    ✗ Wrong: Any human language text followed by SQL");
            sb.AppendLine();
            sb.AppendLine("YOUR RESPONSE = SQL QUERY ONLY (starts with SELECT, pure English SQL, no text!)");

            return sb.ToString();
        }

        private static List<string> ExtractFilterKeywords(string userQuery)
        {
            if (string.IsNullOrWhiteSpace(userQuery))
            {
                return new List<string>();
            }

            var normalizedQuery = RemoveDiacritics(userQuery).ToLowerInvariant();
            var separators = new[] { ' ', '\t', '\r', '\n', ',', ';', '.', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '{', '}', '/', '\\', '-', '_', '’' };
            var rawTokens = normalizedQuery.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            var keywords = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var token in rawTokens)
            {
                var cleanedToken = new string(token.Where(char.IsLetterOrDigit).ToArray());

                if (string.IsNullOrEmpty(cleanedToken))
                {
                    continue;
                }

                if (cleanedToken.Length < 3 || cleanedToken.Length > 32)
                {
                    continue;
                }

                if (FilterStopWords.Contains(cleanedToken))
                {
                    continue;
                }

                if (seen.Add(cleanedToken))
                {
                    keywords.Add(cleanedToken);

                    if (keywords.Count >= MaxFilterKeywords)
                    {
                        break;
                    }
                }
            }

            return keywords;
        }

        private static Dictionary<string, List<string>> BuildColumnKeywordMap(IEnumerable<string> keywords, List<TableSchemaInfo> tables)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var table in tables)
            {
                foreach (var column in table.Columns)
                {
                    if (!IsTextDataType(column.DataType))
                    {
                        continue;
                    }

                    var columnKey = $"{table.TableName}.{column.ColumnName}";

                    foreach (var keyword in keywords)
                    {
                        if (KeywordMatchesColumn(keyword, column, table))
                        {
                            if (!result.TryGetValue(columnKey, out var columnKeywords))
                            {
                                columnKeywords = new List<string>();
                                result[columnKey] = columnKeywords;
                            }

                            if (!columnKeywords.Contains(keyword, StringComparer.OrdinalIgnoreCase))
                            {
                                columnKeywords.Add(keyword);
                            }
                        }
                    }
                }
            }

            return result;
        }

        private static bool KeywordMatchesColumn(string keyword, ColumnSchemaInfo column, TableSchemaInfo table)
        {
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 3)
            {
                return false;
            }

            var normalizedKeyword = NormalizeText(keyword);

            if (column.ColumnName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            var normalizedColumnName = NormalizeText(column.ColumnName);
            if (!string.IsNullOrEmpty(normalizedColumnName) && normalizedColumnName.Contains(normalizedKeyword))
            {
                return true;
            }

            var normalizedSample = NormalizeText(table.SampleData ?? string.Empty);
            if (!string.IsNullOrEmpty(normalizedSample) && normalizedSample.Contains(normalizedKeyword))
            {
                return true;
            }

            return false;
        }

        private static bool ColumnMatchesAnySchemaElement(string keyword, List<TableSchemaInfo> tables)
        {
            foreach (var table in tables)
            {
                foreach (var column in table.Columns)
                {
                    if (!IsTextDataType(column.DataType))
                    {
                        continue;
                    }

                    if (KeywordMatchesColumn(keyword, column, table))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return RemoveDiacritics(text).ToLowerInvariant();
        }

        private static string ConvertNonAsciiToAscii(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return line;
            }

            var sanitized = RemoveDiacritics(line);
            sanitized = sanitized.Replace('ı', 'i').Replace('İ', 'I');
            sanitized = sanitized.Replace('ß', 's');
            return sanitized;
        }

        private static string NormalizeSqlFormatting(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return sql;
            }

            var upper = sql.ToUpperInvariant();
            if (!upper.StartsWith("SELECT", StringComparison.Ordinal))
            {
                return sql;
            }

            if (upper.StartsWith("SELECT TOP", StringComparison.Ordinal))
            {
                return sql;
            }

            var topMatch = Regex.Match(sql, @"\bTOP\s+(\d+)\b", RegexOptions.IgnoreCase);
            if (!topMatch.Success)
            {
                return sql;
            }

            var fromIndex = upper.IndexOf("FROM", StringComparison.Ordinal);
            if (fromIndex > -1 && topMatch.Index < fromIndex)
            {
                // TOP already before FROM; nothing to change
                return sql;
            }

            var selectIndex = upper.IndexOf("SELECT", StringComparison.Ordinal);
            if (selectIndex < 0)
            {
                return sql;
            }

            var topStart = topMatch.Index;
            var topLength = topMatch.Length;
            var removalStart = topStart;
            while (removalStart > 0 && char.IsWhiteSpace(sql[removalStart - 1]))
            {
                removalStart--;
            }

            var topValue = sql.Substring(topStart, topLength);
            sql = sql.Remove(removalStart, topStart + topLength - removalStart);
            sql = sql.Insert(selectIndex + "SELECT".Length, " " + topValue);

            return sql;
        }

        private static string ExtractTopLevelClause(string sql, string clauseKeyword)
        {
            if (string.IsNullOrWhiteSpace(sql) || string.IsNullOrWhiteSpace(clauseKeyword))
            {
                return string.Empty;
            }

            var upper = sql.ToUpperInvariant();
            clauseKeyword = clauseKeyword.ToUpperInvariant();

            int depth = 0;
            for (int i = 0; i <= upper.Length - clauseKeyword.Length; i++)
            {
                var currentChar = upper[i];

                if (currentChar == '(')
                {
                    depth++;
                    continue;
                }

                if (currentChar == ')')
                {
                    depth = Math.Max(0, depth - 1);
                    continue;
                }

                if (depth == 0 && upper.IndexOf(clauseKeyword, i, StringComparison.Ordinal) == i)
                {
                    var builder = new StringBuilder();
                    int clauseDepth = 0;
                    int j = i + clauseKeyword.Length;

                    while (j < upper.Length)
                    {
                        var ch = upper[j];

                        if (ch == '(')
                        {
                            clauseDepth++;
                        }
                        else if (ch == ')')
                        {
                            if (clauseDepth == 0)
                            {
                                break;
                            }

                            clauseDepth--;
                        }

                        if (clauseDepth == 0 && IsClauseTerminator(upper, j))
                        {
                            break;
                        }

                        builder.Append(sql[j]);
                        j++;
                    }

                    return builder.ToString().Trim();
                }
            }

            return string.Empty;
        }

        private static bool IsClauseTerminator(string upperSql, int position)
        {
            var index = position;

            while (index < upperSql.Length && char.IsWhiteSpace(upperSql[index]))
            {
                index++;
            }

            foreach (var terminator in ClauseTerminators)
            {
                if (index <= upperSql.Length - terminator.Length &&
                    upperSql.IndexOf(terminator, index, StringComparison.Ordinal) == index)
                {
                    var endIndex = index + terminator.Length;
                    if (endIndex >= upperSql.Length)
                    {
                        return true;
                    }

                    var nextChar = upperSql[endIndex];
                    if (!char.IsLetterOrDigit(nextChar))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ContainsTextFilterForAllowedColumns(
            string sql,
            IEnumerable<TableSchemaInfo> allowedTables,
            IReadOnlyDictionary<string, List<string>> columnKeywordMap)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return false;
            }

            if (columnKeywordMap == null || columnKeywordMap.Count == 0)
            {
                return false;
            }

            var whereClause = ExtractTopLevelClause(sql, "WHERE");
            if (string.IsNullOrEmpty(whereClause))
            {
                return false;
            }

            foreach (var table in allowedTables)
            {
                foreach (var column in table.Columns)
                {
                    if (!IsTextDataType(column.DataType))
                    {
                        continue;
                    }

                    var columnKey = $"{table.TableName}.{column.ColumnName}";
                    if (!columnKeywordMap.TryGetValue(columnKey, out var keywordsForColumn) || keywordsForColumn.Count == 0)
                    {
                        continue;
                    }

                    var columnPattern = Regex.Escape(column.ColumnName);

                    foreach (var keyword in keywordsForColumn)
                    {
                        var escapedKeyword = Regex.Escape(keyword);
                        var escapedKeywordUpper = Regex.Escape(keyword.ToUpperInvariant());

                        var patterns = new[]
                        {
                            $@"\b{columnPattern}\b\s*(?:=|LIKE|ILIKE)\s*(?:N)?'[^']*{escapedKeyword}[^']*'",
                            $@"LOWER\s*\(\s*{columnPattern}\s*\)\s*(?:=|LIKE|ILIKE)\s*'[^']*{escapedKeyword}[^']*'",
                            $@"UPPER\s*\(\s*{columnPattern}\s*\)\s*(?:=|LIKE|ILIKE)\s*'[^']*{escapedKeywordUpper}[^']*'"
                        };

                        foreach (var pattern in patterns)
                        {
                            if (Regex.IsMatch(whereClause, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsTextDataType(string dataType)
        {
            if (string.IsNullOrWhiteSpace(dataType))
            {
                return false;
            }

            var normalized = dataType.ToLowerInvariant();
            return normalized.Contains("char") ||
                   normalized.Contains("text") ||
                   normalized.Contains("string") ||
                   normalized.Contains("nclob") ||
                   normalized.Contains("clob");
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalizedText = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalizedText.Length);

            foreach (var character in normalizedText)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
                if (unicodeCategory == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                builder.Append(character);
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private string ExtractSQLFromAIResponse(string aiResponse)
        {
            if (string.IsNullOrWhiteSpace(aiResponse))
            {
                return string.Empty;
            }

            // Step 1: Extract from markdown code blocks if present (```sql ... ```)
            if (aiResponse.Contains("```"))
            {
                var firstBacktick = aiResponse.IndexOf("```");
                var lastBacktick = aiResponse.LastIndexOf("```");
                
                if (firstBacktick < lastBacktick && lastBacktick > firstBacktick + 3)
                {
                    // Extract content between code fences
                    var codeContent = aiResponse.Substring(firstBacktick + 3, lastBacktick - firstBacktick - 3);
                    
                    // Remove language identifier if present (e.g., "sql" after opening ```)
                    var lines = codeContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 0 && lines[0].Trim().Length <= 10 && lines[0].Trim().All(c => char.IsLetter(c) || c == ' '))
                    {
                        // First line looks like language identifier (e.g., "sql"), skip it
                        aiResponse = string.Join("\n", lines.Skip(1));
                    }
                    else
                    {
                        aiResponse = codeContent;
                    }
                }
            }
            
            // Step 2: Remove ALL non-English lines (human language explanations)
            var allLines = aiResponse.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var sqlLines = new List<string>();
            
            foreach (var line in allLines)
            {
                var sanitizedLine = ConvertNonAsciiToAscii(line);
                sqlLines.Add(sanitizedLine);
            }
            
            var sql = string.Join("\n", sqlLines).Trim();
            
            // Step 3: Auto-complete missing closing parentheses
            int openCount = sql.Count(c => c == '(');
            int closeCount = sql.Count(c => c == ')');
            if (openCount > closeCount)
            {
                sql += new string(')', openCount - closeCount);
            }
            
            // Step 4: Clean up and return
            sql = sql.Trim().TrimEnd(';').Trim();
            sql = NormalizeSqlFormatting(sql);
            return sql;
        }

        private bool IsCompleteSql(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return false;
            }
            
            var sqlUpper = sql.ToUpperInvariant();
            var lines = sql.Split('\n').Select(l => l.Trim()).ToArray();
            
            // Check for incomplete clauses (check full SQL, not line-by-line)
            // Multi-line SQL may have ORDER BY on one line and column on next line
            if (sqlUpper.Contains("ORDER BY"))
            {
                // Extract everything after ORDER BY
                var orderByIndex = sqlUpper.IndexOf("ORDER BY");
                var afterOrderBy = sqlUpper.Substring(orderByIndex + 8).Trim(); // "ORDER BY".Length = 8
                
                // Check if there's anything meaningful after ORDER BY (not just whitespace/newlines)
                var nextToken = afterOrderBy.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                
                if (string.IsNullOrWhiteSpace(nextToken))
                {
                    _logger.LogWarning("Incomplete SQL: ORDER BY clause has no columns");
                    return false;
                }
            }
            
            if (sqlUpper.Contains("GROUP BY"))
            {
                // Extract everything after GROUP BY
                var groupByIndex = sqlUpper.IndexOf("GROUP BY");
                var afterGroupBy = sqlUpper.Substring(groupByIndex + 8).Trim(); // "GROUP BY".Length = 8
                
                // Check if there's anything meaningful after GROUP BY
                var nextToken = afterGroupBy.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                
                if (string.IsNullOrWhiteSpace(nextToken))
                {
                    _logger.LogWarning("Incomplete SQL: GROUP BY clause has no columns");
                        return false;
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

        private string BuildStricterSQLPrompt(string userQuery, DatabaseQueryIntent dbQuery, DatabaseSchemaInfo schema, List<string> previousErrors)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║       SQL REGENERATION - PREVIOUS ATTEMPT HAD ERRORS           ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("CRITICAL: Your previous SQL had these errors:");
            
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
            
            // Build a comprehensive map of what columns belong to which tables
            var allColumnsInSchema = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var allColumnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var table in schema.Tables)
            {
                allColumnsInSchema[table.TableName] = new HashSet<string>(table.Columns.Select(c => c.ColumnName), StringComparer.OrdinalIgnoreCase);
                foreach (var col in table.Columns)
                {
                    allColumnNames.Add(col.ColumnName);
                }
            }
            
            foreach (var tableName in dbQuery.RequiredTables)
            {
                var table = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                if (table != null)
                {
                    sb.AppendLine();
                    sb.AppendLine($"═══ {table.TableName} ═══");
                    sb.AppendLine($"HAS these columns:");
                    foreach (var col in table.Columns)
                    {
                        sb.AppendLine($"   {table.TableName}.{col.ColumnName} ({col.DataType})");
                    }
                    
                    // GENERIC: Show columns from OTHER tables in schema that DON'T exist in THIS table
                    var otherTables = schema.Tables.Where(t => !t.TableName.Equals(table.TableName, StringComparison.OrdinalIgnoreCase)).ToList();
                    var columnsInOtherTables = otherTables
                        .SelectMany(t => t.Columns.Select(c => new { Table = t.TableName, Column = c.ColumnName }))
                        .Where(x => !allColumnsInSchema[table.TableName].Contains(x.Column))
                        .GroupBy(x => x.Column)
                        .Select(g => new { Column = g.Key, Tables = string.Join(", ", g.Select(x => x.Table)) })
                        .Take(5) // Limit to 5 examples
                        .ToList();
                    
                    if (columnsInOtherTables.Any())
                    {
                        sb.AppendLine($"DOES NOT HAVE (but other tables do):");
                        foreach (var item in columnsInOtherTables)
                        {
                            sb.AppendLine($"   {table.TableName}.{item.Column} - ERROR! (exists in: {item.Tables})");
                        }
                    }
                    
                    sb.AppendLine();
                    sb.AppendLine($"CRITICAL: Verify column exists in {table.TableName} before using alias!");
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
            sb.AppendLine("10. ✗ If using GROUP BY + ORDER BY: ORDER BY columns must ALSO be in GROUP BY (or use aggregate alias)");
            sb.AppendLine("11. ✗ DO NOT use placeholder values (ENTER_ID_HERE, etc.)");
            sb.AppendLine("12. ✗ DO NOT use AVG() on string/text columns - use numeric columns only");
            sb.AppendLine("13. ✓ Use actual values (1, 2, 3) instead of placeholders");
            sb.AppendLine("14. ✗ DO NOT reference tables from other databases in JOIN conditions");
            sb.AppendLine("15. NEVER use more than 3 levels of nested subqueries - simplify your query!");
            sb.AppendLine("16. If tables mentioned in user query are NOT in your allowed list - DO NOT reference them!");
            sb.AppendLine("17. ✓ For SQL Server: Use TOP instead of LIMIT");
            sb.AppendLine("18. ✓ For SQL Server: JOIN is ALLOWED within same database");
            sb.AppendLine();
            sb.AppendLine($"CROSS-DATABASE RULE - YOUR ALLOWED TABLES: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine("   If tables mentioned in user query are NOT in this list:");
            sb.AppendLine("   → DO NOT try to JOIN with tables not in your allowed list!");
            sb.AppendLine("   → DO NOT use: WHERE ForeignKeyColumn IN (SELECT ... FROM OtherTable ...)");
            sb.AppendLine("   → Instead: Return foreign key columns (columns ending with 'ID')");
            sb.AppendLine("   → Example: SELECT ForeignKeyColumn, SUM(NumericColumn) FROM TableA GROUP BY ForeignKeyColumn");
            sb.AppendLine();
            sb.AppendLine("EXAMPLES:");
            sb.AppendLine("  ✗ WHERE SUM(Amount) > 100  (WRONG)");
            sb.AppendLine("  ✓ HAVING SUM(Amount) > 100 (CORRECT)");
            sb.AppendLine();
            sb.AppendLine("  ✗ SELECT Col1, Col2, SUM(Col3) GROUP BY Col1  (WRONG - Col2 not in GROUP BY)");
            sb.AppendLine("  ✓ SELECT Col1, SUM(Col3) GROUP BY Col1        (CORRECT)");
            sb.AppendLine();
            sb.AppendLine("  ✗ SELECT Col1, SUM(Col2) FROM T GROUP BY Col1 ORDER BY Col3  (WRONG - Col3 not in GROUP BY)");
            sb.AppendLine("  ✓ SELECT Col1, SUM(Col2) FROM T GROUP BY Col1 ORDER BY Col1  (CORRECT)");
            sb.AppendLine("  ✓ SELECT Col1, SUM(Col2) AS Total FROM T GROUP BY Col1 ORDER BY Total DESC  (CORRECT - aggregate alias)");
            sb.AppendLine();
            sb.AppendLine("  ✗ WHERE ID = ENTER_ID_HERE  (WRONG - placeholder)");
            sb.AppendLine("  ✓ WHERE ID = 1                       (CORRECT - actual value)");
            sb.AppendLine();
            sb.AppendLine("  ✗ SELECT AVG(TextColumn) FROM TableA  (WRONG - string column)");
            sb.AppendLine("  ✓ SELECT AVG(NumericColumn) FROM TableA           (CORRECT - numeric column)");
            sb.AppendLine();
            sb.AppendLine("  ✗ SELECT TableA.Column1 FROM TableA JOIN TableB (WRONG - cross-database JOIN)");
            sb.AppendLine("  ✓ SELECT ForeignKeyID, TextColumn FROM TableA WHERE ForeignKeyID = 1 (CORRECT - single database)");
            sb.AppendLine();
            sb.AppendLine($"User Query: {userQuery}");
            sb.AppendLine($"Purpose: {dbQuery.Purpose}");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("OUTPUT FORMAT - CRITICAL:");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("1. Write ONLY the SQL query");
            sb.AppendLine("2. NO human language text (regardless of language)");
            sb.AppendLine("3. NO explanations in any language");
            sb.AppendLine("4. NO descriptions in any language");
            sb.AppendLine("5. NO numbered lists (\"1.\", \"2.\", \"3.\")");
            sb.AppendLine("6. NO comments (-- or /* */)");
            sb.AppendLine("7. ONLY English SQL keywords: SELECT, FROM, WHERE, JOIN, GROUP BY, ORDER BY");
            sb.AppendLine();
            sb.AppendLine("CORRECT OUTPUT:");
            sb.AppendLine("  SELECT col1, col2");
            sb.AppendLine("  FROM table1");
            sb.AppendLine("  WHERE condition");
            sb.AppendLine();
            sb.AppendLine("WRONG OUTPUT:");
            sb.AppendLine("  SELECT col1, col2");
            sb.AppendLine("  FROM table1");
            sb.AppendLine("  ");
            sb.AppendLine("  Açıklama: Bu sorgu...  ← STOP! DON'T WRITE THIS!");
            sb.AppendLine();
            sb.AppendLine("WRITE ONLY SQL - NOTHING ELSE!");
            
            return sb.ToString();
        }

        private string BuildUltraStrictSQLPrompt(string userQuery, DatabaseQueryIntent dbQuery, DatabaseSchemaInfo schema, List<string> allPreviousErrors, int attemptNumber)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
            sb.AppendLine($"║     RETRY ATTEMPT #{attemptNumber} - ULTRA STRICT MODE        ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("CRITICAL: ALL previous attempts FAILED with these errors:");
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
                    sb.AppendLine($"THESE ARE THE ONLY {table.Columns.Count} COLUMNS IN {table.TableName}!");
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
            sb.AppendLine("□ If using GROUP BY + ORDER BY: Are ALL ORDER BY columns in GROUP BY (or aggregates/aliases)?");
            sb.AppendLine("□ Are parentheses balanced?");
            sb.AppendLine("□ Did I avoid placeholder values (ENTER_ID_HERE, etc.)?");
            sb.AppendLine("□ Did I avoid AVG() on string/text columns?");
            sb.AppendLine("□ Did I use actual values (1, 2, 3) instead of placeholders?");
            sb.AppendLine("□ Did I avoid cross-database JOIN references?");
            sb.AppendLine("□ Do I have maximum 3 levels of nested subqueries?");
            sb.AppendLine("□ For SQL Server: Did I use TOP instead of LIMIT?");
            sb.AppendLine("□ For SQL Server: Did I remember JOIN is ALLOWED within same database?");
            sb.AppendLine();
            sb.AppendLine("COMMON MISTAKES TO AVOID:");
            sb.AppendLine("  ✗ WHERE SUM(...) > value     → Use HAVING SUM(...) > value");
            sb.AppendLine("  ✗ WHERE AVG(...) > value     → Use HAVING AVG(...) > value");
            sb.AppendLine("  ✗ SELECT A, B, SUM(C) GROUP BY A  → Add B to GROUP BY");
            sb.AppendLine("  ✗ SELECT A, SUM(B) FROM T GROUP BY A ORDER BY C  → C not in GROUP BY (use A or SUM(B) alias)");
            sb.AppendLine("  ✗ WHERE ID = ENTER_ID_HERE  → Use actual value");
            sb.AppendLine("  ✗ SELECT AVG(TextColumn)  → Use numeric column");
            sb.AppendLine("  ✗ TableA JOIN OtherDB.TableB  → Cross-database JOIN not allowed");
            sb.AppendLine("  ✓ TableA JOIN TableB (same database)  → ALLOWED and CORRECT");
            sb.AppendLine("  50 levels of nested SELECT  → MAXIMUM 3 levels allowed! Use JOINs instead!");
            sb.AppendLine("  JOIN TableX (if TableX NOT in allowed list)  → TableX doesn't exist here!");
            sb.AppendLine("  WHERE ForeignKeyColumn IN (SELECT ... FROM OtherTable ...)  → OtherTable doesn't exist here!");
            sb.AppendLine("  ✓ SELECT ForeignKeyColumn, SUM(NumericColumn) FROM TableA GROUP BY ForeignKeyColumn  → Return FK for merging");
            sb.AppendLine("  ✗ SELECT ... LIMIT 1 (SQL Server)  → Use SELECT TOP 1");
            sb.AppendLine();
            sb.AppendLine("CROSS-DATABASE STRATEGY:");
            sb.AppendLine($"   Your allowed tables: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine("   If tables mentioned in user query are NOT in your list:");
            sb.AppendLine("   → DO NOT reference tables not in your allowed list!");
            sb.AppendLine("   → Return foreign key columns (columns ending with 'ID') instead!");
            sb.AppendLine("   → Application will merge with data from other database!");
            sb.AppendLine();
            sb.AppendLine($"Query: {userQuery}");
            sb.AppendLine($"Task: {dbQuery.Purpose}");
            sb.AppendLine();
            sb.AppendLine("CRITICAL: SQL must be PURE ENGLISH - NO human language text!");
            sb.AppendLine("Write the SQL query. Triple-check EVERY column name AND syntax before outputting.");
            sb.AppendLine("Output format: Pure English SQL only, no text, no comments.");
            
            return sb.ToString();
        }

        private string BuildSimplifiedSQLPrompt(string userQuery, DatabaseQueryIntent dbQuery, DatabaseSchemaInfo schema, List<string> allPreviousErrors)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║    FINAL ATTEMPT - FIX ERRORS & KEEP SQL MEANINGFUL            ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("USER'S QUESTION (DON'T FORGET THIS!):");
            sb.AppendLine($"   \"{userQuery}\"");
            sb.AppendLine();
            sb.AppendLine("YOUR TASK:");
            sb.AppendLine($"   {dbQuery.Purpose}");
            sb.AppendLine();
            sb.AppendLine("Previous attempts had these SPECIFIC errors:");
            foreach (var error in allPreviousErrors.Take(5))
            {
                sb.AppendLine($"  ✗ {error}");
            }
            sb.AppendLine();
            
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("FIX THE ERRORS - DON'T GIVE UP ON THE QUERY!");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("CRITICAL: Don't write 'SELECT * LIMIT 100' - that doesn't answer the question!");
            sb.AppendLine();
            sb.AppendLine("STRATEGY:");
            sb.AppendLine("1. Read the errors above carefully");
            sb.AppendLine("2. Fix ONLY those specific errors");
            sb.AppendLine("3. Keep the SQL meaningful - it must answer the user's question");
            sb.AppendLine("4. If you need JOINs to answer the question, use them");
            sb.AppendLine("5. If you need GROUP BY/aggregates, use them correctly");
            sb.AppendLine("6. Just make sure all column/table names are correct");
            sb.AppendLine();
            sb.AppendLine("RULES (fix these if they caused errors):");
            sb.AppendLine("✗ NO aggregates in WHERE - use HAVING");
            sb.AppendLine("✗ NO columns in SELECT that aren't in GROUP BY (unless aggregated)");
            sb.AppendLine("✗ NO columns in ORDER BY that aren't in GROUP BY (unless aggregated)");
            sb.AppendLine("✗ NO placeholder values (ENTER_ID_HERE, etc.)");
            sb.AppendLine("✗ NO AVG() on string columns");
            sb.AppendLine("✗ NO cross-database JOINs");
            sb.AppendLine("MAXIMUM 3 levels of nested subqueries - use JOINs instead!");
            sb.AppendLine("If tables mentioned in user query are NOT in allowed list - DO NOT reference them!");
            sb.AppendLine("✓ Use ONLY columns that exist in schema");
            sb.AppendLine("✓ Use ONLY tables you're allowed to use");
            sb.AppendLine("✓ Return foreign key columns (columns ending with 'ID') for cross-database merging");
            sb.AppendLine();
            sb.AppendLine($"YOUR ALLOWED TABLES: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine("   If tables mentioned in user query are NOT in this list:");
            sb.AppendLine("   → DO NOT JOIN with tables not in your allowed list!");
            sb.AppendLine("   → DO NOT use subqueries referencing tables not in your allowed list!");
            sb.AppendLine("   → Return foreign key columns (columns ending with 'ID') for merging!");
            sb.AppendLine();
            
            sb.AppendLine("Available tables and columns:");
            foreach (var tableName in dbQuery.RequiredTables)
            {
                var table = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                if (table != null)
                {
                    sb.AppendLine($"\n{table.TableName}:");
                    sb.AppendLine($"  Columns: {string.Join(", ", table.Columns.Take(10).Select(c => c.ColumnName))}");
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("YOUR GOAL: Write SQL that ANSWERS THE QUESTION and FIXES THE ERRORS!");
            sb.AppendLine("Don't give up - fix the specific errors and keep the query meaningful!");
            sb.AppendLine();
            sb.AppendLine("Output: Pure English SQL only, no explanations.");
            
            return sb.ToString();
        }

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
                
                // 0. Check for non-ASCII characters in SQL (outside string literals)
                // SQL keywords and identifiers must use ASCII characters only
                // String literals (quoted values) are excluded from this check
                // This generic check covers all languages
                var nonAsciiChar = CheckForNonAsciiCharacters(sql);
                if (nonAsciiChar.HasValue)
                {
                    errors.Add($"Non-ASCII character detected in SQL: '{nonAsciiChar.Value}' (U+{(int)nonAsciiChar.Value:X4}). SQL keywords and identifiers must use only ASCII characters (a-z, A-Z, 0-9, and standard SQL operators).");
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
                    if (sqlUpper.Contains("FETCH NEXT") || sqlUpper.Contains("FETCH FIRST"))
                    {
                        errors.Add("FETCH syntax is not allowed. Use TOP instead: SELECT TOP 100 ...");
                    }
                }
                else if (databaseType == DatabaseType.SQLite || databaseType == DatabaseType.MySQL || databaseType == DatabaseType.PostgreSQL)
                {
                    if (sqlUpper.Contains(" TOP ") || sqlUpper.Contains("TOP\t"))
                    {
                        errors.Add($"TOP keyword is not valid in {databaseType}. Use LIMIT instead: ...LIMIT 100");
                    }
                }
                
                // 5. Check for GROUP BY / ORDER BY mismatch
                var groupByClause = ExtractTopLevelClause(sql, "GROUP BY");
                var orderByClause = ExtractTopLevelClause(sql, "ORDER BY");
                if (!string.IsNullOrEmpty(groupByClause) && !string.IsNullOrEmpty(orderByClause))
                {
                    var groupByColumns = groupByClause
                        .Split(',')
                        .Select(c => c.Trim())
                        .Where(c => !string.IsNullOrEmpty(c))
                        .Select(c => c.Split('.').Last().Trim())
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var orderByColumns = orderByClause
                        .Split(',')
                        .Select(c => c.Trim())
                        .Where(c => !string.IsNullOrEmpty(c))
                        .Select(c => Regex.Replace(c, @"\s+(ASC|DESC).*$", "", RegexOptions.IgnoreCase))
                        .Select(c => c.Split('.').Last().Trim())
                        .ToList();

                    foreach (var orderCol in orderByColumns)
                    {
                        var isAggregate = orderCol.Contains("SUM(") ||
                                          orderCol.Contains("AVG(") ||
                                          orderCol.Contains("COUNT(") ||
                                          orderCol.Contains("MAX(") ||
                                          orderCol.Contains("MIN(");

                        if (!isAggregate && !groupByColumns.Contains(orderCol))
                        {
                            errors.Add($"ORDER BY column '{orderCol}' must be in GROUP BY clause or use an aggregate function. Current GROUP BY: {string.Join(", ", groupByColumns)}");
                        }
                    }
                }
                
                // 6. Table aliases are ALLOWED in SQL (e.g., "FROM TableA t1" then "t1.Column1")
                // This is standard SQL syntax, not an error!
                
                // 6b. Check for ANY subquery nesting (FORBIDDEN - use JOINs instead)
                var maxNestingLevel = GetMaxSubqueryNesting(sql);
                if (maxNestingLevel > 1)
                {
                    errors.Add($"Nested subqueries detected ({maxNestingLevel} levels). FORBIDDEN: Use JOINs instead of subqueries. Maximum nesting: 1 level (simple subquery in WHERE IN is OK, no more).");
                }
                
                // 7. Check for basic syntax issues
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
        /// Checks for non-ASCII characters in SQL, excluding string literals (quoted values).
        /// SQL keywords and identifiers must use ASCII characters only.
        /// </summary>
        /// <param name="sql">SQL query to check</param>
        /// <returns>First non-ASCII character found, or null if all characters are ASCII</returns>
        private static char? CheckForNonAsciiCharacters(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return null;
            }

            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            bool escaped = false;

            for (int i = 0; i < sql.Length; i++)
            {
                char c = sql[i];

                // Handle escape sequences
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                // Track string literal boundaries
                if (c == '\'' && !inDoubleQuote)
                {
                    inSingleQuote = !inSingleQuote;
                    continue;
                }

                if (c == '"' && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                    continue;
                }

                // Skip characters inside string literals
                if (inSingleQuote || inDoubleQuote)
                {
                    continue;
                }

                // Check if character is non-ASCII (outside 0-127 range)
                // Allow ASCII control characters (0-31) for formatting (newlines, tabs, etc.)
                // Allow ASCII printable characters (32-126) for SQL syntax
                // Reject extended ASCII (128-255) and Unicode characters
                if (c > 127)
                {
                    return c;
                }
            }

            return null;
        }

        private static int GetMaxSubqueryNesting(string sql)
        {
            // CRITICAL: CTE (WITH clause) SELECT statements don't count as nested subqueries
            // Remove CTE definitions before counting nesting
            var sqlToAnalyze = sql;
            
            if (sql.TrimStart().StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            {
                // Find the end of CTE definitions (right before the main SELECT)
                // Pattern: WITH ... AS (...) SELECT → we want to skip the CTE part
                var mainSelectPattern = @"WITH\s+.+?\)\s+(SELECT\s+.+)";
                var mainSelectMatch = Regex.Match(sql, mainSelectPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                
                if (mainSelectMatch.Success && mainSelectMatch.Groups.Count >= 2)
                {
                    // Only analyze the main SELECT query, not the CTE definitions
                    sqlToAnalyze = mainSelectMatch.Groups[1].Value;
                }
            }
            
            // Count maximum nesting level of SELECT statements (excluding CTE)
            int maxLevel = 0;
            int currentLevel = 0;
            
            var tokens = sqlToAnalyze.ToUpperInvariant().Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i] == "SELECT")
                {
                    currentLevel++;
                    maxLevel = Math.Max(maxLevel, currentLevel);
                }
                else if (tokens[i].Contains(")"))
                {
                    // Closing a subquery - count closing parens
                    currentLevel -= tokens[i].Count(c => c == ')');
                    if (currentLevel < 0) currentLevel = 0;
                }
            }
            
            return Math.Max(0, maxLevel - 1);
        }

        private async Task<List<string>> ValidateSQLTableExistenceAsync(string sql, DatabaseSchemaInfo schema, string databaseName)
        {
            var errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(sql) || schema == null)
            {
                return errors;
            }

            try
            {
                // CRITICAL: Extract CTE (WITH clause) names - they're temporary tables
                var cteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (sql.TrimStart().StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
                {
                    // Pattern: WITH CteName AS (...)
                    var ctePattern = @"WITH\s+([a-zA-Z_][a-zA-Z0-9_]*)\s+AS\s*\(";
                    var cteMatches = Regex.Matches(sql, ctePattern, RegexOptions.IgnoreCase);
                    foreach (Match cteMatch in cteMatches)
                    {
                        if (cteMatch.Groups.Count >= 2)
                        {
                            cteNames.Add(cteMatch.Groups[1].Value);
                        }
                    }
                }
                
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
                        
                        // Skip SQL keywords and CTE names
                        if (potentialTableName.Length < 2 || 
                            sqlKeywords.Contains(potentialTableName) || 
                            cteNames.Contains(potentialTableName))
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
                        !cteNames.Contains(tableName) &&  // Skip CTE names
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

        private bool ValidateSQLColumnExistence(string sql, DatabaseSchemaInfo schema, List<string> allowedTables, out List<string> errors)
        {
            errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(sql) || schema == null || allowedTables == null || !allowedTables.Any())
            {
                return true; // Skip validation if data is invalid
            }

            try
            {
                // CRITICAL: Extract CTE (WITH clause) names - they're temporary tables, not real columns/tables
                var cteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (sql.TrimStart().StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
                {
                    // Pattern: WITH CteName AS (...)
                    var ctePattern = @"WITH\s+([a-zA-Z_][a-zA-Z0-9_]*)\s+AS\s*\(";
                    var cteMatches = Regex.Matches(sql, ctePattern, RegexOptions.IgnoreCase);
                    foreach (Match cteMatch in cteMatches)
                    {
                        if (cteMatch.Groups.Count >= 2)
                        {
                            cteNames.Add(cteMatch.Groups[1].Value);
                        }
                    }
                }
                
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

                // Create a unified set of all available columns
                var allColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var colSet in availableColumns.Values)
                {
                    foreach (var col in colSet)
                    {
                        allColumns.Add(col);
                    }
                }

                // Parse SQL to extract table aliases (FROM/JOIN TableName alias)
                var aliasToTable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                
                // CRITICAL: First, map CTE aliases (CTEs can be used like tables)
                // Pattern: FROM/JOIN CteName alias
                foreach (var cteName in cteNames)
                {
                    var cteAliasPattern = $@"(?:FROM|JOIN)\s+{cteName}\s+(?:AS\s+)?([a-zA-Z_][a-zA-Z0-9_]*)\b";
                    var cteAliasMatches = Regex.Matches(sql, cteAliasPattern, RegexOptions.IgnoreCase);
                    
                    foreach (Match cteAliasMatch in cteAliasMatches)
                    {
                        if (cteAliasMatch.Groups.Count >= 2)
                        {
                            var alias = cteAliasMatch.Groups[1].Value;
                            if (!string.IsNullOrEmpty(alias) && alias != "ON" && alias != "WHERE")
                            {
                                aliasToTable[alias] = cteName; // Map alias → CTE name
                            }
                        }
                    }
                }
                
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

                // 1. Validate qualified columns (Table.Column or Alias.Column)
                var qualifiedColumnPattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\.\s*([a-zA-Z_][a-zA-Z0-9_]*)\b";
                var qualifiedMatches = Regex.Matches(sql, qualifiedColumnPattern, RegexOptions.IgnoreCase);

                foreach (Match match in qualifiedMatches)
                {
                    if (match.Groups.Count >= 3)
                    {
                        var tableOrAlias = match.Groups[1].Value;
                        var columnName = match.Groups[2].Value;
                        
                        // Skip string literal matches like 'Spor'.sql
                        if (tableOrAlias.StartsWith("'", StringComparison.Ordinal) &&
                            tableOrAlias.EndsWith("'", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        // SKIP if this is a CTE reference (temporary table from WITH clause)
                        if (cteNames.Contains(tableOrAlias))
                        {
                            continue; // CTE columns are not validated against schema
                        }

                        // First, check if it's an alias
                        string actualTableName = null;
                        if (aliasToTable.ContainsKey(tableOrAlias))
                        {
                            actualTableName = aliasToTable[tableOrAlias];
                            
                            // CRITICAL: If alias resolves to a CTE, skip validation
                            if (cteNames.Contains(actualTableName))
                            {
                                continue; // CTE columns (via alias) are not validated against schema
                            }
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
                                errors.Add($"Column '{columnName}' does not exist in table '{actualTableName}'. Available columns: {availableCols}");
                            }
                        }
                    }
                }
                
                // 2. Validate unqualified columns in all SQL clauses
                // SQL keywords and functions to skip
                var sqlKeywordsToSkip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "SELECT", "FROM", "WHERE", "JOIN", "LEFT", "RIGHT", "INNER", "OUTER", "CROSS",
                    "ON", "AND", "OR", "NOT", "IN", "EXISTS", "AS", "ORDER", "GROUP", "HAVING",
                    "LIMIT", "TOP", "OFFSET", "UNION", "EXCEPT", "INTERSECT", "DISTINCT", "ALL",
                    "BY", "ASC", "DESC", "BETWEEN", "LIKE", "IS", "NULL", "TRUE", "FALSE", 
                    "CASE", "WHEN", "THEN", "ELSE", "END", "CAST", "CONVERT", "WITH",  // Added WITH for CTE
                    "SUM", "AVG", "COUNT", "MAX", "MIN", "OVER", "PARTITION", "ROW_NUMBER",
                    "RANK", "DENSE_RANK", "FIRST_VALUE", "LAST_VALUE", "LAG", "LEAD"
                };
                
                // Add all CTE names to skip list (they're temporary tables, not columns)
                foreach (var cteName in cteNames)
                {
                    sqlKeywordsToSkip.Add(cteName);
                }
                
                // Include all function names (words directly followed by '(') so they are not treated as columns
                var functionPattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\(";
                var functionMatches = Regex.Matches(sql, functionPattern, RegexOptions.IgnoreCase);
                foreach (Match functionMatch in functionMatches)
                {
                    if (functionMatch.Groups.Count > 1)
                    {
                        var functionName = functionMatch.Groups[1].Value;
                        if (!string.IsNullOrWhiteSpace(functionName))
                        {
                            sqlKeywordsToSkip.Add(functionName);
                        }
                    }
                }
                
                // Remove string literals to avoid treating them as column names
                var sqlWithoutStrings = Regex.Replace(sql, @"'[^']*'|""[^""]*""", " ", RegexOptions.IgnoreCase);
                
                // Extract all potential column names from SQL
                var wordPattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b";
                var wordMatches = Regex.Matches(sqlWithoutStrings, wordPattern, RegexOptions.IgnoreCase);
                
                var potentialColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (Match match in wordMatches)
                {
                    var word = match.Groups[1].Value;
                    
                    // Skip SQL keywords, table names, and already validated qualified columns
                    if (!sqlKeywordsToSkip.Contains(word) &&
                        !allowedTables.Any(t => t.Equals(word, StringComparison.OrdinalIgnoreCase)) &&
                        !aliasToTable.ContainsKey(word))
                    {
                        potentialColumns.Add(word);
                    }
                }
                
                // Validate each potential column
                foreach (var potentialColumn in potentialColumns)
                {
                    // Check if it's a valid column name
                    if (!allColumns.Contains(potentialColumn))
                    {
                        // Additional check: might be an alias defined in SELECT (e.g., "Total AS TotalAmount")
                        var selectAliasPattern = $@"\bAS\s+{potentialColumn}\b";
                        if (Regex.IsMatch(sql, selectAliasPattern, RegexOptions.IgnoreCase))
                        {
                            continue; // It's a SELECT alias, skip validation
                        }
                        
                        // Generate helpful error message with available columns
                        var availableColsList = string.Join(", ", allColumns.Take(20));
                        if (allColumns.Count > 20)
                        {
                            availableColsList += $", ... ({allColumns.Count - 20} more)";
                        }
                        
                        // Include table-specific column lists for clarity
                        var tableSpecificInfo = new StringBuilder();
                        foreach (var kvp in availableColumns)
                        {
                            tableSpecificInfo.AppendLine();
                            tableSpecificInfo.Append($"  • {kvp.Key}: {string.Join(", ", kvp.Value.Take(5))}");
                            if (kvp.Value.Count > 5)
                            {
                                tableSpecificInfo.Append($", ... ({kvp.Value.Count - 5} more)");
                            }
                        }
                        
                        errors.Add($"Column '{potentialColumn}' does not exist in schema. Available columns:{tableSpecificInfo}");
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


        #endregion
    }
}

