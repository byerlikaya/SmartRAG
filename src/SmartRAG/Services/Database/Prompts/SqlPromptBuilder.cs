using SmartRAG.Interfaces.Database;
using SmartRAG.Interfaces.Database.Strategies;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartRAG.Services.Database.Prompts
{
    /// <summary>
    /// Builds prompts for SQL query generation
    /// </summary>
    public class SqlPromptBuilder : ISqlPromptBuilder
    {
        private const int SampleDataLimit = 200;

        private static readonly HashSet<string> FilterStopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Common articles and prepositions
            "the", "a", "an", "and", "or", "but", "for", "with", "from", "into", "onto", "about", "over", "under",
            "between", "within", "without", "through", "during", "before", "after", "above", "below",
            
            // Common auxiliary verbs and modals
            "will", "would", "could", "should", "have", "has", "had", "been", "being", "is", "are", "was", "were",
            
            // Common pronouns and determiners
            "than", "then", "them", "they", "their", "there", "those", "these", "this", "that", "each", "every",
            "when", "where", "which", "while", "whose", "what", "ever", "many", "much", "more", "most", "some", "such",
            "only", "also", "just", "like", "make", "take", "give", "need", "want",
            
            // Common query verbs
            "time", "date", "question", "asked", "asking", "show", "list", "tell", "provide", "please"
        };

        public string Build(string userQuery, DatabaseQueryIntent dbQuery, DatabaseSchemaInfo schema, ISqlDialectStrategy strategy, QueryIntent fullQueryIntent = null)
        {
            var sb = new StringBuilder();
            var filterKeywords = ExtractFilterKeywords(userQuery);
            var allowedTableSchemas = schema.Tables
                .Where(t => dbQuery.RequiredTables.Contains(t.TableName, StringComparer.OrdinalIgnoreCase))
                .ToList();

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

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("SQL QUERY GENERATOR");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"Database: {schema.DatabaseName} ({schema.DatabaseType})");
            sb.AppendLine($"User Query: \"{userQuery}\"");
            sb.AppendLine($"Purpose: {dbQuery.Purpose}");
            
            var dialectInfo = strategy.DatabaseType == SmartRAG.Enums.DatabaseType.SqlServer
                ? "SQL Server - Use [brackets] for identifiers with spaces"
                : strategy.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL
                    ? "PostgreSQL - Use \"quotes\" for case-sensitive identifiers, case-sensitive!"
                    : strategy.DatabaseType == SmartRAG.Enums.DatabaseType.MySQL
                        ? "MySQL - Use backticks for identifiers with spaces"
                        : "SQLite - Use double quotes for identifiers with spaces";
            
            sb.AppendLine($"SQL Dialect: {dialectInfo}");
            sb.AppendLine();
            sb.AppendLine("🚨 IMPORTANT: The schema information below is the ONLY source of truth!");
            sb.AppendLine("   Use ONLY the tables and columns listed in the schema section below.");
            sb.AppendLine("   DO NOT use any table or column that is NOT listed below!");
            sb.AppendLine();

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("RULE 1: SECURITY - ONLY SELECT ALLOWED");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("✗ FORBIDDEN: CREATE, DROP, DELETE, UPDATE, INSERT, EXEC, GRANT, REVOKE");
            sb.AppendLine("✓ ALLOWED: SELECT (and only SELECT)");
            sb.AppendLine();

            // Cross-database context (if applicable)
            if (fullQueryIntent != null && fullQueryIntent.DatabaseQueries.Count > 1)
            {
                var otherDbCount = fullQueryIntent.DatabaseQueries.Count(q => q.DatabaseId != dbQuery.DatabaseId);
                if (otherDbCount > 0)
                {
                    sb.AppendLine("═══════════════════════════════════════════════════════════════");
                    sb.AppendLine("🚨 MULTI-DATABASE QUERY - CRITICAL WARNINGS!");
                    sb.AppendLine("═══════════════════════════════════════════════════════════════");
                    sb.AppendLine($"  → This query is part of a {fullQueryIntent.DatabaseQueries.Count}-database query");
                    sb.AppendLine();
                    sb.AppendLine("🚨 FORBIDDEN:");
                    sb.AppendLine("  ✗ DO NOT use tables from other databases in your SQL!");
                    sb.AppendLine("  ✗ DO NOT try to JOIN tables from different databases!");
                    sb.AppendLine("  ✗ DO NOT reference columns from tables in other databases!");
                    sb.AppendLine();
                    sb.AppendLine("✓ ALLOWED:");
                    sb.AppendLine("  ✓ Use ONLY tables listed in 'TABLES AVAILABLE' section below");
                    sb.AppendLine("  ✓ Use ONLY columns from tables in THIS database");
                    sb.AppendLine("  ✓ ALWAYS include ID columns in SELECT (for joining results later)");
                    sb.AppendLine();
                    sb.AppendLine($"Other databases in this query: {string.Join(", ", fullQueryIntent.DatabaseQueries.Where(q => q.DatabaseId != dbQuery.DatabaseId).Select(q => q.DatabaseName))}");
                    sb.AppendLine($"Your current database: {schema.DatabaseName}");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("RULE 2: GROUP BY - MANDATORY FOR AGGREGATES");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("IF you use SUM/COUNT/AVG/MAX/MIN:");
            sb.AppendLine("  → ALL non-aggregate columns in SELECT MUST be in GROUP BY");
            sb.AppendLine();
            sb.AppendLine("Examples:");
            sb.AppendLine("  ✓ SELECT EntityID, SUM(Value) FROM T GROUP BY EntityID");
            sb.AppendLine("  ✓ SELECT Col1, Col2, SUM(Value) FROM T GROUP BY Col1, Col2");
            sb.AppendLine("  ✗ SELECT EntityID, SUM(Value) FROM T  -- Missing GROUP BY!");
            sb.AppendLine();

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("RULE 2.4: JOIN RULES - CRITICAL!");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("🚨 CRITICAL: CROSS JOIN is FORBIDDEN!");
            sb.AppendLine();
            sb.AppendLine("FORBIDDEN:");
            sb.AppendLine("  ✗ CROSS JOIN  -- ABSOLUTELY FORBIDDEN!");
            sb.AppendLine();
            sb.AppendLine("ALLOWED JOIN TYPES:");
            sb.AppendLine("  ✓ INNER JOIN ... ON condition");
            sb.AppendLine("  ✓ LEFT JOIN ... ON condition");
            sb.AppendLine("  ✓ RIGHT JOIN ... ON condition (if needed)");
            sb.AppendLine();
            sb.AppendLine("If you need to combine data from multiple tables:");
            sb.AppendLine("  ✓ Use INNER JOIN with proper ON clause based on foreign keys");
            sb.AppendLine("  ✓ Use LEFT JOIN if one table may not have matching rows");
            sb.AppendLine("  ✗ NEVER use CROSS JOIN - it will cause query rejection!");
            sb.AppendLine();

            // Table Name Escaping Rule
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("RULE 2.5: TABLE/COLUMN NAME ESCAPING");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            
            if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.SqlServer)
            {
                sb.AppendLine("SQL Server uses SQUARE BRACKETS for identifiers with spaces:");
                sb.AppendLine("  ✓ SELECT * FROM [Multi Word Table]");
                sb.AppendLine("  ✗ SELECT * FROM Multi Word Table  -- SYNTAX ERROR!");
            }
            else if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.SQLite || strategy.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
            {
                sb.AppendLine("Use DOUBLE QUOTES for identifiers with spaces:");
                sb.AppendLine("  ✓ SELECT * FROM \"Multi Word Table\"");
                sb.AppendLine("  ✗ SELECT * FROM Multi Word Table  -- SYNTAX ERROR!");
            }
            else if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.MySQL)
            {
                sb.AppendLine("MySQL uses BACKTICKS for identifiers with spaces:");
                sb.AppendLine("  ✓ SELECT * FROM `Multi Word Table`");
                sb.AppendLine("  ✗ SELECT * FROM Multi Word Table  -- SYNTAX ERROR!");
            }
            sb.AppendLine();

            // Schema.Table Format Rule
            if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.SqlServer || strategy.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("RULE 2.6: SCHEMA.TABLE FORMAT - CRITICAL!");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("🚨 CRITICAL: Use schema.table format (NOT database.schema.table)");
                sb.AppendLine();
                sb.AppendLine("CORRECT FORMAT:");
                if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.SqlServer)
                {
                    sb.AppendLine("  ✓ SELECT * FROM [SchemaName].[TableName]");
                    sb.AppendLine("  ✓ SELECT * FROM SchemaName.TableName");
                }
                else if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
                {
                    sb.AppendLine("  ✓ SELECT * FROM \"SchemaName\".\"TableName\"");
                    sb.AppendLine("  ✓ SELECT * FROM schemaname.tablename (if identifiers are lowercase)");
                    sb.AppendLine();
                    sb.AppendLine("🚨🚨🚨 CRITICAL FOR POSTGRESQL: Table names are case-sensitive! 🚨🚨🚨");
                    sb.AppendLine("  → Use EXACT case as shown in schema section below - NO EXCEPTIONS!");
                    sb.AppendLine("  → If schema shows lowercase: schemaname.tablename → Use lowercase: schemaname.tablename");
                    sb.AppendLine("  → If schema shows mixed case: \"SchemaName\".\"TableName\" → Use double quotes: \"SchemaName\".\"TableName\"");
                    sb.AppendLine("  → WRONG: SchemaName.TableName (if schema shows schemaname.tablename)");
                    sb.AppendLine("  → WRONG: schemaname.TableName (mixed case without quotes)");
                    sb.AppendLine("  → Check schema section below FIRST, then use EXACT format!");
                }
                sb.AppendLine();
                sb.AppendLine("WRONG FORMAT (WILL CAUSE ERROR):");
                sb.AppendLine("  ✗ SELECT * FROM DatabaseName.SchemaName.TableName  -- Database prefix FORBIDDEN!");
                sb.AppendLine("  ✗ SELECT * FROM SchemaName.TableName  -- Wrong case if schema shows schemaname.tablename");
                sb.AppendLine("  ✗ SELECT * FROM SchemaA.TableB  -- This table exists in DIFFERENT database!");
                sb.AppendLine();
                sb.AppendLine("Rule: Table names in schema are already in 'schema.table' format.");
                sb.AppendLine("Use them EXACTLY as shown in the schema section below.");
                sb.AppendLine("NEVER add database name prefix!");
                sb.AppendLine("NEVER use tables from other databases!");
                sb.AppendLine();
            }

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("RULE 3: QUERY PATTERNS");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("Choose the correct pattern based on the purpose:");
            sb.AppendLine();
            sb.AppendLine("PATTERN 1: Simple SELECT (no aggregation)");
            sb.AppendLine("  Purpose: Get descriptive data (names, descriptions)");
            sb.AppendLine("  🚨 CRITICAL: ALWAYS include BOTH ID column AND descriptive columns (Name, Description, etc.)");
            sb.AppendLine("  SQL: SELECT ID_Column, Name_Column, Other_Descriptive_Columns FROM Table");
            sb.AppendLine("  ✓ Example: SELECT EntityID, EntityName, EntityDescription, Location FROM EntityTable");
            sb.AppendLine("  ✗ WRONG: SELECT EntityID FROM EntityTable  -- Missing Name!");
            sb.AppendLine("  ✗ WRONG: SELECT EntityName FROM EntityTable  -- Missing ID!");
            sb.AppendLine();
            sb.AppendLine("PATTERN 2: Aggregation Query (with GROUP BY)");
            sb.AppendLine("  Purpose: Get numeric data for calculations");
            sb.AppendLine("  🚨 CRITICAL: Include ID column in SELECT and GROUP BY");
            sb.AppendLine("  ⚠️ If descriptive columns exist (Name, Description), include them too!");
            sb.AppendLine("  SQL: SELECT ID_Column, Name_Column, SUM(Calc) AS Total FROM Table T");
            sb.AppendLine("       LEFT JOIN ReferenceTable R ON T.ID = R.ID");
            sb.AppendLine("       GROUP BY ID_Column, Name_Column");
            sb.AppendLine("  Example: SELECT T.EntityID, SUM(D.Price * D.Quantity * (1-D.DiscountRate)) AS TotalAmount");
            sb.AppendLine("           FROM TransactionTable T JOIN DetailTable D ON T.TransactionID = D.TransactionID");
            sb.AppendLine("           GROUP BY T.EntityID");
            sb.AppendLine("           ORDER BY TotalAmount DESC");
            if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.SqlServer)
            {
                sb.AppendLine("           TOP 5");
            }
            else
            {
                sb.AppendLine("           LIMIT 5");
            }
            sb.AppendLine();
            sb.AppendLine("PATTERN 3: Cross-Database Join (return IDs)");
            sb.AppendLine("  Purpose: Return data that will be joined with another database");
            sb.AppendLine("  SQL: ALWAYS include ID columns for joining");
            sb.AppendLine("  Example: SELECT EntityID, NameColumn FROM TableA WHERE EntityID IN (...)");
            sb.AppendLine();

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("RULE 4: ANTI-HALLUCINATION - CRITICAL!");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("🚨🚨🚨 FORBIDDEN - NEVER DO THESE:");
            sb.AppendLine();
            sb.AppendLine("⛔ NEVER invent table names that don't exist in schema!");
            sb.AppendLine("   ✗ WRONG: FROM TableNameHistory  -- If TableNameHistory is NOT listed in schema!");
            sb.AppendLine("   ✗ WRONG: FROM SchemaA.TableBHistory  -- If TableBHistory is NOT listed in schema!");
            sb.AppendLine("   ✗ WRONG: FROM AnyTableName  -- If AnyTableName is NOT listed in schema!");
            sb.AppendLine("   ✓ CORRECT: Use ONLY tables listed in schema section below");
            sb.AppendLine("   ✓ If table is NOT in schema section, it does NOT exist - DO NOT use it!");
            sb.AppendLine();
            sb.AppendLine("⛔ NEVER invent column names that don't exist in schema!");
            sb.AppendLine("   ✗ WRONG: SELECT ColumnX FROM TableName  -- If ColumnX doesn't exist in schema!");
            sb.AppendLine("   ✗ WRONG: SELECT NameColumn FROM TableName  -- If NameColumn doesn't exist in schema!");
            sb.AppendLine("   ✗ WRONG: SELECT NumericColumn FROM TableName  -- If NumericColumn doesn't exist!");
            sb.AppendLine("   ✓ CORRECT: Check schema section below, use ONLY listed columns");
            sb.AppendLine("   ✓ If column doesn't exist in schema, DO NOT use it - query will fail!");
            sb.AppendLine();
            sb.AppendLine("⛔ NEVER invent values in WHERE clause!");
            sb.AppendLine("   ✗ WHERE NameColumn = 'Invented Value'  -- Value doesn't exist in data!");
            sb.AppendLine("   ✗ WHERE LocationColumn = 'Specific Place'  -- Unless verified in schema!");
            sb.AppendLine("   ✓ Use GROUP BY + ORDER BY instead for TOP N queries");
            sb.AppendLine();
            sb.AppendLine("⛔ NEVER use tables from other databases!");
            sb.AppendLine("   ✗ WRONG: JOIN SchemaA.TableB  -- If TableB is in DIFFERENT database!");
            sb.AppendLine("   ✗ WRONG: FROM SchemaX.TableY  -- If TableY is in DIFFERENT database!");
            sb.AppendLine("   ✓ CORRECT: Use ONLY tables listed in schema section below");
            sb.AppendLine();
            sb.AppendLine("⛔ For TOP N / HIGHEST / MOST queries:");
            sb.AppendLine("   ✓ CORRECT: SELECT EntityID, SUM(Value) AS Total FROM T");
            sb.AppendLine("              GROUP BY EntityID ORDER BY Total DESC");
            sb.AppendLine("   ✗ WRONG: SELECT * FROM T WHERE EntityName = 'TopEntity'");
            sb.AppendLine();
            sb.AppendLine("⛔ Return ONLY IDs in result if descriptive columns don't exist in this table");
            sb.AppendLine("   The application will merge descriptive data from other databases");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("GENERATE SQL NOW");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"User Query: \"{userQuery}\"");
            sb.AppendLine($"Your Task: {dbQuery.Purpose}");
            sb.AppendLine();

            if (filterKeywords.Count > 0)
            {
                sb.AppendLine("TEXT FILTER KEYWORDS FROM QUESTION (use case-insensitive LIKE):");
                sb.AppendLine($"  {string.Join(", ", filterKeywords)}");
                sb.AppendLine("Use patterns like LOWER(ColumnName) LIKE '%keyword%' unless sample data shows the exact stored value.");
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
                }
                sb.AppendLine();
                sb.AppendLine("WHAT YOU MUST DO INSTEAD:");
                sb.AppendLine("  ✓ IGNORE these filter keywords completely");
                sb.AppendLine("  ✓ RETURN ALL ROWS with foreign key columns");
                sb.AppendLine();
                sb.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
                sb.AppendLine();
            }

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"🚨🚨🚨 TABLES AVAILABLE IN {schema.DatabaseName} - READ CAREFULLY! 🚨🚨🚨");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("⛔ FORBIDDEN:");
            sb.AppendLine("  ✗ DO NOT invent table names that are NOT listed below!");
            sb.AppendLine("  ✗ DO NOT use tables from other databases!");
            sb.AppendLine("  ✗ DO NOT add prefixes/suffixes to table names!");
            sb.AppendLine();
            sb.AppendLine("✓ REQUIRED:");
            sb.AppendLine($"  ✓ Use ONLY these {dbQuery.RequiredTables.Count} table(s): {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine("  ✓ Copy table names EXACTLY as shown below (character by character)");
            sb.AppendLine("  ✓ If a table is NOT listed below, it does NOT exist - DO NOT use it!");
            sb.AppendLine();

            foreach (var tableName in dbQuery.RequiredTables)
            {
                var table = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                if (table != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("═══════════════════════════════════════════════════════════════");
                    sb.AppendLine($"📋 TABLE #{Array.IndexOf(dbQuery.RequiredTables.ToArray(), tableName) + 1}: {table.TableName}");
                    sb.AppendLine("═══════════════════════════════════════════════════════════════");
                    if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
                    {
                        sb.AppendLine("🚨🚨🚨 POSTGRESQL CASE-SENSITIVE - COPY EXACTLY! 🚨🚨🚨");
                        sb.AppendLine($"   ✓ CORRECT FORMAT: {table.TableName}");
                        sb.AppendLine($"   ✓ USE THIS EXACT STRING: \"{table.TableName}\"");
                        sb.AppendLine("   ✗ WRONG: Change ANY letter case");
                        sb.AppendLine("   ✗ WRONG: Add/remove quotes if not shown");
                        sb.AppendLine("   ✗ WRONG: Use mixed case if schema shows lowercase");
                        sb.AppendLine();
                        sb.AppendLine("🚨 CRITICAL: Columns below are also case-sensitive - copy EXACTLY!");
                    }
                    else
                    {
                        sb.AppendLine("🚨 CRITICAL: Use EXACT table name and column names as shown below");
                    }
                    sb.AppendLine();
                    sb.AppendLine("AVAILABLE COLUMNS:");
                    foreach (var column in table.Columns)
                    {
                        var columnInfo = $"  • {column.ColumnName} ({column.DataType})";
                        if (column.IsPrimaryKey)
                            columnInfo += " [PRIMARY KEY]";
                        if (column.IsForeignKey)
                            columnInfo += " [FOREIGN KEY]";
                        sb.AppendLine(columnInfo);
                    }
                    sb.AppendLine();
                    sb.AppendLine("⚠️ WARNING: Do NOT use columns from other tables!");
                    sb.AppendLine($"   These columns exist ONLY in {table.TableName} table.");
                    sb.AppendLine($"   If you need data from another table, it's in a DIFFERENT database!");
                    sb.AppendLine();

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
                    }

                    if (!string.IsNullOrEmpty(table.SampleData))
                    {
                        sb.AppendLine();
                        sb.AppendLine($"  Sample Data (first few rows):");
                        var sampleLines = table.SampleData[..Math.Min(SampleDataLimit, table.SampleData.Length)]
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
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("🚨🚨🚨 FINAL CHECKLIST - VERIFY BEFORE WRITING SQL! 🚨🚨🚨");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("⛔ ABSOLUTELY FORBIDDEN:");
            sb.AppendLine($"  ✗ DO NOT use any table NOT in this list: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine("  ✗ DO NOT invent table names (e.g., TableNameHistory, SchemaA.TableB)");
            sb.AppendLine("  ✗ DO NOT use tables from other databases");
            sb.AppendLine("  ✗ DO NOT use columns that are NOT listed above");
            sb.AppendLine();
            sb.AppendLine("✓ REQUIRED:");
            sb.AppendLine($"  ✓ Use ONLY these {dbQuery.RequiredTables.Count} table(s): {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine("  ✓ Copy table names EXACTLY as shown above (character by character)");
            sb.AppendLine("  ✓ Use ONLY columns listed above for each table");
            sb.AppendLine("  ✓ If column doesn't exist in this table, it's in a DIFFERENT database - DO NOT use it!");
            sb.AppendLine("  ✓ Start with SELECT (no CREATE, DROP, DELETE)");
            sb.AppendLine("  ✓ If using SUM/COUNT/AVG, include GROUP BY");
            sb.AppendLine("  ✓ Never invent WHERE clause values");
            sb.AppendLine("  ✓ NEVER use CROSS JOIN - use INNER JOIN or LEFT JOIN with ON clause");
            if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
            {
                sb.AppendLine("  ✓ Use EXACT table names from schema above - COPY EXACTLY (case-sensitive!)");
                sb.AppendLine("  ✓ Use EXACT column names from schema above - COPY EXACTLY (case-sensitive!)");
                sb.AppendLine("  ✓ If schema shows 'schemaname.tablename' → Use 'schemaname.tablename' (NOT 'SchemaName.TableName')");
                sb.AppendLine("  ✓ If schema shows '\"SchemaName\".\"TableName\"' → Use double quotes");
            }
            else
            {
                sb.AppendLine("  ✓ Use EXACT table names from schema above");
                sb.AppendLine("  ✓ Use EXACT column names from schema above");
            }
            sb.AppendLine("  ✓ NEVER use tables from other databases - only tables listed above");
            sb.AppendLine("  ✓ If you see table names from other databases in the query, DO NOT use them here!");
            sb.AppendLine();
            sb.AppendLine("Write your SQL now:");

            return sb.ToString();
        }

        /// <summary>
        /// Extracts meaningful keywords from user query for SQL WHERE clause filtering.
        /// Filters out common stop words and short words that are unlikely to be column names.
        /// Language-agnostic: Works with any language by filtering based on length and common patterns.
        /// </summary>
        private List<string> ExtractFilterKeywords(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<string>();

            var words = query.Split(new[] { ' ', ',', '.', '?', '!', ';', ':', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

            return words
                .Where(w =>
                    w.Length > 2 &&
                    !FilterStopWords.Contains(w) &&
                    !IsNumeric(w))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Checks if a word is purely numeric (e.g., "123", "45.67")
        /// </summary>
        private static bool IsNumeric(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return false;

            var cleaned = word.Replace(".", "").Replace(",", "").Replace("-", "").Replace("+", "");

            return cleaned.Length > 0 && cleaned.All(char.IsDigit);
        }
    }
}
