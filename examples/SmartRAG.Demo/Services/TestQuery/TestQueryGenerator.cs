using Microsoft.Extensions.Logging;
using SmartRAG.Demo.Models;
using SmartRAG.Demo.Services.Translation;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models.Schema;
using System.Text;
using System.Text.Json;

namespace SmartRAG.Demo.Services.TestQuery;

/// <summary>
/// Generates test queries for multi-database testing
/// </summary>
public class TestQueryGenerator(
    ILogger<TestQueryGenerator> logger,
    IDatabaseSchemaAnalyzer? schemaAnalyzer,
    IAIService aiService,
    ITranslationService translationService) : ITestQueryGenerator
{
    #region Public Methods

    public async Task<List<Models.TestQuery>> GenerateTestQueriesAsync(string language)
    {
        var testQueries = new List<Models.TestQuery>();

        try
        {
            if (schemaAnalyzer == null)
            {
                logger.LogWarning("Database schema analyzer is not available. Database search feature is disabled.");
                return testQueries;
            }

            var schemas = await schemaAnalyzer.GetAllSchemasAsync(CancellationToken.None);

            if (schemas.Count < 2)
            {
                logger.LogWarning("Need at least 2 databases for cross-database tests. Currently have: {Count}", schemas.Count);
                return testQueries;
            }

            var aiGeneratedQueries = await GenerateAITestQueriesAsync(schemas, language);
            if (aiGeneratedQueries.Count > 0)
            {
                testQueries.AddRange(aiGeneratedQueries);
            }

            testQueries.AddRange(await GenerateSchemaBasedQueriesAsync(schemas, language));

            var random = new Random();
            testQueries = testQueries.OrderBy(x => random.Next()).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating test queries");
        }

        return testQueries;
    }

    public async Task<List<Models.TestQuery>> GenerateAITestQueriesAsync(List<DatabaseSchemaInfo> schemas, string language)
    {
        var queries = new List<Models.TestQuery>();

        try
        {
            var schemaPrompt = BuildSchemaPromptForAI(schemas);
            var random = new Random();
            var queryCountVariation = random.Next(8, 15);
            
            var focusAreas = new[]
            {
                "aggregations and calculations",
                "data correlations and relationships",
                "temporal comparisons and trends",
                "filtering and grouping across databases",
                "comprehensive data analysis"
            };
            var selectedFocus = focusAreas[random.Next(focusAreas.Length)];

            var aiPrompt = BuildAIPrompt(schemaPrompt, language, queryCountVariation, selectedFocus);
            var response = await aiService.GenerateResponseAsync(aiPrompt, new List<string>(), CancellationToken.None);

            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var aiQueries = JsonSerializer.Deserialize<List<JsonElement>>(json);

                if (aiQueries != null)
                {
                    foreach (var item in aiQueries)
                    {
                        if (item.TryGetProperty("category", out var cat) &&
                            item.TryGetProperty("query", out var q) &&
                            item.TryGetProperty("databases", out var dbs))
                        {
                            var dbList = dbs.GetString() ?? "";
                            if (dbList.Contains("+") || dbList.Contains(","))
                            {
                                var dbTypes = ExtractDatabaseTypes(dbList, schemas);

                                queries.Add(new Models.TestQuery
                                {
                                    Category = cat.GetString() ?? "🧪 Test",
                                    Query = q.GetString() ?? "",
                                    DatabaseName = dbList,
                                    DatabaseTypes = dbTypes
                                });
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to generate AI test queries");
        }

        return queries;
    }

    public async Task<List<Models.TestQuery>> GenerateSchemaBasedQueriesAsync(List<DatabaseSchemaInfo> schemas, string language)
    {
        await Task.CompletedTask;
        
        var queries = new List<Models.TestQuery>();

        var databasePairs = new List<(DatabaseSchemaInfo Db1, DatabaseSchemaInfo Db2)>();
        for (int i = 0; i < schemas.Count; i++)
        {
            for (int j = i + 1; j < schemas.Count; j++)
            {
                databasePairs.Add((schemas[i], schemas[j]));
            }
        }

        GenerateCrossJoinQueries(schemas, queries, language);
        GenerateCrossCalculationQueries(databasePairs, queries, language);
        GenerateMultiDatabaseCoverageQuery(schemas, queries, language);
        GenerateTemporalAnalysisQueries(schemas, queries, language);
        GenerateCoverageTestQueries(schemas, queries, language);

        return queries;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Builds schema prompt for AI query generation
    /// </summary>
    /// <param name="schemas">List of database schemas</param>
    /// <returns>Formatted schema prompt string</returns>
    private string BuildSchemaPromptForAI(List<DatabaseSchemaInfo> schemas)
    {
        var sb = new StringBuilder();
        sb.AppendLine("AVAILABLE DATABASES:");
        sb.AppendLine();

        foreach (var schema in schemas)
        {
            sb.AppendLine($"DATABASE: {schema.DatabaseName} ({schema.DatabaseType})");
            sb.AppendLine("TABLES:");

            foreach (var table in schema.Tables.Take(5))
            {
                sb.AppendLine($"  - {schema.DatabaseName}.{table.TableName} ({table.RowCount} rows)");
                sb.AppendLine($"    Columns: {string.Join(", ", table.Columns.Select(c => $"{c.ColumnName} ({c.DataType})"))}");

                if (table.ForeignKeys.Any())
                {
                    foreach (var fk in table.ForeignKeys.Take(3))
                    {
                        sb.AppendLine($"    FK: {fk.ColumnName} → {fk.ReferencedTable}.{fk.ReferencedColumn}");
                    }
                }
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds AI prompt for generating test queries
    /// </summary>
    /// <param name="schemaPrompt">Schema information prompt</param>
    /// <param name="language">Target language for queries</param>
    /// <param name="queryCount">Number of queries to generate</param>
    /// <param name="focus">Focus area for query generation</param>
    /// <returns>Complete AI prompt string</returns>
    private static string BuildAIPrompt(string schemaPrompt, string language, int queryCount, string focus)
    {
        return $@"{schemaPrompt}

Based on the database schemas above, generate {queryCount} intelligent, MEANINGFUL cross-database test queries.

CRITICAL LANGUAGE REQUIREMENT:
YOU MUST write EVERY SINGLE query in {language} language!
ONLY category field stays in English (with emoji).
Query field MUST be in {language}.

YOUR TASK:
Analyze the schemas and generate REALISTIC questions that require data from MULTIPLE databases.

CRITICAL REQUIREMENTS:
1. EVERY query MUST use at least 2 databases
2. Queries should be SPECIFIC and MEANINGFUL (not generic calculations)
3. Focus on foreign key relationships between databases
4. Generate questions based on actual table/column names
5. Think about data relationships and correlations
6. Each database should be used in at least one query

🚨 CRITICAL SQL GENERATION RULES:
1. NEVER use phrases like ""specific record"", ""certain ID"", ""particular value""
2. NEVER assume column names that aren't in the schema
3. ALWAYS use aggregation queries: ""total"", ""average"", ""count"", ""sum""
4. ALWAYS use phrases: ""all records"", ""total value"", ""highest"", ""lowest""
5. Focus on GROUP BY, SUM, AVG, COUNT queries that don't require WHERE with specific values
6. Use foreign keys visible in schema for joins

CRITICAL INSTRUCTION - HOW TO WRITE QUERIES:
Step 1: Analyze the schema above and identify:
  - Numeric columns (DECIMAL, INT types)
  - Foreign key columns (columns referencing other tables)
  - Date/time columns (DATETIME, TIMESTAMP types)
  - Text columns (VARCHAR, TEXT types for status/category)

Step 2: Use ACTUAL column/table names from the schema
  - Look at schema and use the real names you see there
  - DO NOT use placeholder syntax
  - DO NOT assume column names not in schema

EXAMPLE QUERY PATTERNS:
✓ ""Show all records from [table in schema] with their related [foreign key table] data""
✓ ""Calculate total [numeric column from schema] grouped by [text column from schema]""
✓ ""What is the average [numeric column from schema] across all records?""
✓ ""Compare totals between [database 1] and [database 2]""

BAD PATTERNS - DO NOT USE:
✗ ""Show specific record with ID 5"" (specific value)
✗ ""Calculate numericColumn"" (placeholder not replaced)
✗ ""Get TableName.ColumnName"" (database prefix in query text)

Category options (use emoji prefix - category in English, query in {language}):
- Cross-DB Join
- Cross-DB Calculation
- Cross-DB Filter
- Cross-DB Temporal
- Cross-DB Search
- Coverage Test
- Multi-DB Coverage

REQUIRED JSON FORMAT:
[
  {{
    ""category"": ""💰 Cross-DB Calculation"",
    ""query"": ""[Query in {language}]"",
    ""databases"": ""Database1 + Database2""
  }}
]

Respond ONLY with the JSON array, no other text.";
    }

    /// <summary>
    /// Extracts database types from database names using schema information
    /// </summary>
    /// <param name="databaseNames">Comma or plus-separated database names</param>
    /// <param name="schemas">List of database schemas</param>
    /// <returns>Formatted database types string</returns>
    private static string ExtractDatabaseTypes(string databaseNames, List<DatabaseSchemaInfo> schemas)
    {
        var dbNames = databaseNames.Split(new[] { " + ", ", " }, StringSplitOptions.RemoveEmptyEntries);
        var dbTypes = new List<string>();

        foreach (var dbName in dbNames)
        {
            var schema = schemas.FirstOrDefault(s => s.DatabaseName.Equals(dbName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (schema != null)
            {
                dbTypes.Add(schema.DatabaseType.ToString());
            }
        }

        return string.Join(" + ", dbTypes);
    }

    /// <summary>
    /// Generates cross-database join queries based on foreign key relationships
    /// </summary>
    /// <param name="schemas">List of database schemas</param>
    /// <param name="queries">List to add generated queries to</param>
    /// <param name="language">Target language for queries</param>
    private void GenerateCrossJoinQueries(List<DatabaseSchemaInfo> schemas, List<Models.TestQuery> queries, string language)
    {
        var tablesWithForeignKeys = schemas
            .SelectMany(s => s.Tables.Where(t => t.ForeignKeys.Any()).Select(t => new { Schema = s, Table = t }))
            .ToList();

        foreach (var item in tablesWithForeignKeys)
        {
            foreach (var fk in item.Table.ForeignKeys.Take(2))
            {
                var referencedDb = schemas.FirstOrDefault(s =>
                    s.Tables.Any(t => t.TableName.Equals(fk.ReferencedTable, StringComparison.OrdinalIgnoreCase)));

                if (referencedDb != null && referencedDb.DatabaseId != item.Schema.DatabaseId)
                {
                    var genericQuery = translationService.TranslateQuery(
                        "show_records",
                        language,
                        item.Table.TableName,
                        fk.ReferencedTable);

                    queries.Add(new Models.TestQuery
                    {
                        Category = "🔗 Cross-DB Join",
                        Query = genericQuery,
                        DatabaseName = $"{item.Schema.DatabaseName} + {referencedDb.DatabaseName}",
                        DatabaseTypes = $"{item.Schema.DatabaseType} + {referencedDb.DatabaseType}"
                    });
                }
            }
        }
    }

    /// <summary>
    /// Generates cross-database calculation queries for numeric columns
    /// </summary>
    /// <param name="databasePairs">Pairs of databases to query</param>
    /// <param name="queries">List to add generated queries to</param>
    /// <param name="language">Target language for queries</param>
    private void GenerateCrossCalculationQueries(List<(DatabaseSchemaInfo Db1, DatabaseSchemaInfo Db2)> databasePairs, List<Models.TestQuery> queries, string language)
    {
        foreach (var pair in databasePairs)
        {
            var table1WithNumeric = pair.Db1.Tables.FirstOrDefault(t =>
                t.Columns.Any(c => IsNumericType(c.DataType) && !c.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase)));

            var table2WithNumeric = pair.Db2.Tables.FirstOrDefault(t =>
                t.Columns.Any(c => IsNumericType(c.DataType) && !c.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase)));

            if (table1WithNumeric != null && table2WithNumeric != null)
            {
                var hasFkRelation = table1WithNumeric.ForeignKeys.Any(fk =>
                    fk.ReferencedTable.Equals(table2WithNumeric.TableName, StringComparison.OrdinalIgnoreCase));

                if (hasFkRelation)
                {
                    var numericCol1 = table1WithNumeric.Columns.FirstOrDefault(c => 
                        IsNumericType(c.DataType) && !c.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase))?.ColumnName;
                    
                    var numericCol2 = table2WithNumeric.Columns.FirstOrDefault(c => 
                        IsNumericType(c.DataType) && !c.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase))?.ColumnName;

                    if (!string.IsNullOrEmpty(numericCol1) && !string.IsNullOrEmpty(numericCol2))
                    {
                        var calculationQuery = translationService.TranslateQuery(
                            "calculate_value",
                            language,
                            numericCol1,
                            table1WithNumeric.TableName,
                            numericCol2,
                            table2WithNumeric.TableName);

                        queries.Add(new Models.TestQuery
                        {
                            Category = "💰 Cross-DB Calculation",
                            Query = calculationQuery,
                            DatabaseName = $"{pair.Db1.DatabaseName} + {pair.Db2.DatabaseName}",
                            DatabaseTypes = $"{pair.Db1.DatabaseType} + {pair.Db2.DatabaseType}"
                        });
                    }
                }
            }
        }
    }

    /// <summary>
    /// Generates multi-database coverage query for all available databases
    /// </summary>
    /// <param name="schemas">List of database schemas</param>
    /// <param name="queries">List to add generated queries to</param>
    /// <param name="language">Target language for queries</param>
    private void GenerateMultiDatabaseCoverageQuery(List<DatabaseSchemaInfo> schemas, List<Models.TestQuery> queries, string language)
    {
        if (schemas.Count >= 2)
        {
            var allDbNames = string.Join(" + ", schemas.Select(s => s.DatabaseName));
            var allDbTypes = string.Join(" + ", schemas.Select(s => s.DatabaseType));

            var coverageQuery = translationService.TranslateQuery("analyze_correlation", language);

            queries.Add(new Models.TestQuery
            {
                Category = "🌐 Multi-DB Coverage",
                Query = coverageQuery,
                DatabaseName = allDbNames,
                DatabaseTypes = allDbTypes
            });
        }
    }

    /// <summary>
    /// Generates temporal analysis queries for tables with date/time columns
    /// </summary>
    /// <param name="schemas">List of database schemas</param>
    /// <param name="queries">List to add generated queries to</param>
    /// <param name="language">Target language for queries</param>
    private void GenerateTemporalAnalysisQueries(List<DatabaseSchemaInfo> schemas, List<Models.TestQuery> queries, string language)
    {
        var tablesWithDates = schemas
            .SelectMany(s => s.Tables.Where(t =>
                t.Columns.Any(c => c.DataType.Contains("date", StringComparison.OrdinalIgnoreCase) ||
                                  c.DataType.Contains("time", StringComparison.OrdinalIgnoreCase)))
                .Select(t => new { Schema = s, Table = t }))
            .ToList();

        if (tablesWithDates.Count >= 2)
        {
            var dateTable1 = tablesWithDates[0];
            var dateTable2 = tablesWithDates.FirstOrDefault(t => t.Schema.DatabaseId != dateTable1.Schema.DatabaseId);

            if (dateTable2 != null)
            {
                var temporalQuery = translationService.TranslateQuery(
                    "timeline_correlation",
                    language,
                    dateTable1.Table.TableName,
                    dateTable2.Table.TableName);

                queries.Add(new Models.TestQuery
                {
                    Category = "📅 Cross-DB Temporal",
                    Query = temporalQuery,
                    DatabaseName = $"{dateTable1.Schema.DatabaseName} + {dateTable2.Schema.DatabaseName}",
                    DatabaseTypes = $"{dateTable1.Schema.DatabaseType} + {dateTable2.Schema.DatabaseType}"
                });
            }
        }
    }

    /// <summary>
    /// Generates coverage test queries to ensure all databases are tested
    /// </summary>
    /// <param name="schemas">List of database schemas</param>
    /// <param name="queries">List to add generated queries to</param>
    /// <param name="language">Target language for queries</param>
    private void GenerateCoverageTestQueries(List<DatabaseSchemaInfo> schemas, List<Models.TestQuery> queries, string language)
    {
        var databasesUsed = new HashSet<string>();
        foreach (var query in queries)
        {
            var dbNames = query.DatabaseName.Split(new[] { " + " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var db in dbNames)
            {
                databasesUsed.Add(db);
            }
        }

        foreach (var schema in schemas)
        {
            if (!databasesUsed.Contains(schema.DatabaseName))
            {
                var otherDb = schemas.FirstOrDefault(s => s.DatabaseId != schema.DatabaseId);
                if (otherDb != null)
                {
                    var table1 = schema.Tables.FirstOrDefault();
                    var table2 = otherDb.Tables.FirstOrDefault();

                    if (table1 != null && table2 != null)
                    {
                        var relationshipQuery = translationService.TranslateQuery(
                            "analyze_relationship",
                            language,
                            table1.TableName,
                            table2.TableName);

                        queries.Add(new Models.TestQuery
                        {
                            Category = "✅ Coverage Test",
                            Query = relationshipQuery,
                            DatabaseName = $"{schema.DatabaseName} + {otherDb.DatabaseName}",
                            DatabaseTypes = $"{schema.DatabaseType} + {otherDb.DatabaseType}"
                        });
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if data type is numeric
    /// </summary>
    /// <param name="dataType">Database column data type</param>
    /// <returns>True if numeric type, false otherwise</returns>
    private static bool IsNumericType(string dataType)
    {
        return dataType.Contains("int", StringComparison.OrdinalIgnoreCase) ||
               dataType.Contains("decimal", StringComparison.OrdinalIgnoreCase) ||
               dataType.Contains("numeric", StringComparison.OrdinalIgnoreCase) ||
               dataType.Contains("float", StringComparison.OrdinalIgnoreCase) ||
               dataType.Contains("double", StringComparison.OrdinalIgnoreCase) ||
               dataType.Contains("money", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}

