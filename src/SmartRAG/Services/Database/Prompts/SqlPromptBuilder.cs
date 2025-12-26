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

            sb.AppendLine("═══════════════════════════════════════════════════════════════════");
            sb.AppendLine("          SQL QUERY GENERATION - ANSWER THE USER'S QUESTION              ");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║  🚨🚨🚨 CRITICAL SECURITY RULES - READ FIRST! 🚨🚨🚨          ║");
            sb.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("ABSOLUTELY FORBIDDEN - YOUR QUERY WILL BE REJECTED IF YOU USE:");
            sb.AppendLine("  ✗ CREATE, DROP, ALTER, TRUNCATE (DDL statements)");
            sb.AppendLine("  ✗ DELETE, UPDATE, INSERT (DML statements - ONLY SELECT allowed)");
            sb.AppendLine("  ✗ EXEC, EXECUTE, SP_, XP_ (stored procedures)");
            sb.AppendLine("  ✗ GRANT, REVOKE (security statements)");
            sb.AppendLine();
            sb.AppendLine("YOU MUST ONLY GENERATE SELECT STATEMENTS!");
            sb.AppendLine();
            sb.AppendLine("WRONG EXAMPLES (WILL FAIL):");
            sb.AppendLine("  ✗ CREATE TABLE TableA ...");
            sb.AppendLine("  ✗ DROP TABLE TableA ...");
            sb.AppendLine("  ✗ DELETE FROM TableA ...");
            sb.AppendLine("  ✗ EXEC sp_ProcedureName ...");
            sb.AppendLine();
            sb.AppendLine("CORRECT EXAMPLE:");
            sb.AppendLine("  ✓ SELECT EntityID, NameColumn FROM TableA ORDER BY EntityID");
            sb.AppendLine();
            sb.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
            sb.AppendLine($"║  🎯 TARGET DATABASE: {schema.DatabaseName} ({schema.DatabaseType}) 🎯    ║");
            sb.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine($"🚨 CRITICAL: You are generating SQL for {schema.DatabaseType} database!");
            sb.AppendLine($"   Database Name: {schema.DatabaseName}");
            sb.AppendLine($"   Database Type: {schema.DatabaseType}");
            sb.AppendLine();

            sb.AppendLine(strategy.BuildSystemPrompt(schema, userQuery));

            sb.AppendLine();
            sb.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║  🚨 MANDATORY: WRITE SIMPLE SQL - NO COMPLEX QUERIES! 🚨    ║");
            sb.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("REQUIRED QUERY PATTERN (DO NOT DEVIATE):");
            sb.AppendLine("  1. Simple SELECT with descriptive columns");
            sb.AppendLine("  2. ALWAYS include ID columns (e.g. EntityID, ForeignKeyID) in SELECT to allow joining");
            sb.AppendLine("  3. Maximum 2 JOINs (table1 JOIN table2 JOIN table3)");
            sb.AppendLine("  4. Simple WHERE clause (1-2 conditions maximum)");
            sb.AppendLine("  5. Simple ORDER BY (1 column)");
            if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.SqlServer)
            {
                sb.AppendLine($"  6. {strategy.GetLimitClause(100)} immediately after SELECT (SQL Server syntax)");
            }
            else
            {
                sb.AppendLine($"  6. {strategy.GetLimitClause(100)} at the end (or equivalent)");
            }
            sb.AppendLine();
            sb.AppendLine("CROSS-DATABASE LOGIC (CRITICAL):");
            sb.AppendLine("  - If the user asks for a metric or attribute that is NOT in this database:");
            sb.AppendLine("    1. DO NOT try to calculate it or guess it.");
            sb.AppendLine("    2. DO NOT aggregate if it hides the Entity ID needed for joining.");
            sb.AppendLine("    3. INSTEAD, SELECT the Entity ID (Foreign Key) AND the Descriptive Attribute.");
            sb.AppendLine("    4. EXAMPLE: SELECT EntityID, DescriptiveColumn FROM ... (allows joining with other databases)");
            sb.AppendLine("    5. This allows the system to merge results with the database that has the missing metric.");

            if (fullQueryIntent != null && fullQueryIntent.DatabaseQueries.Count > 1 && fullQueryIntent.RequiresCrossDatabaseJoin)
            {
                var otherDbQueries = fullQueryIntent.DatabaseQueries
                    .Where(q => q.DatabaseId != dbQuery.DatabaseId)
                    .ToList();

                if (otherDbQueries.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
                    sb.AppendLine("║  🔗 CROSS-DATABASE CONTEXT - OTHER DATABASES IN THIS QUERY 🔗  ║");
                    sb.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
                    sb.AppendLine();
                    sb.AppendLine("This query is part of a MULTI-DATABASE query. Other databases will also be queried:");

                    foreach (var otherDb in otherDbQueries)
                    {
                        sb.AppendLine($"  • {otherDb.DatabaseName}: {otherDb.Purpose}");
                        if (otherDb.RequiredTables.Any())
                        {
                            sb.AppendLine($"    Tables: {string.Join(", ", otherDb.RequiredTables)}");
                        }
                    }

                    sb.AppendLine();
                    sb.AppendLine("CRITICAL INSTRUCTIONS FOR CROSS-DATABASE QUERIES:");
                    sb.AppendLine("  1. If another database will return an EntityID (e.g., from aggregation/calculation):");
                    sb.AppendLine("     → Your query should return ALL rows with EntityID and descriptive columns");
                    sb.AppendLine("     → DO NOT filter or limit - let the system match EntityIDs after both queries run");
                    sb.AppendLine("  2. If your database has the EntityID source (e.g., aggregation query):");
                    sb.AppendLine("     → Return EntityID and the calculated metric");
                    sb.AppendLine("     → Use ORDER BY and LIMIT/TOP to get the top result");
                    sb.AppendLine("  3. If your database has descriptive data (e.g., names, details):");
                    sb.AppendLine("     → Return EntityID and all descriptive columns");
                    sb.AppendLine("     → DO NOT use ORDER BY CreatedDate or similar - return all matching rows");
                    sb.AppendLine("     → The system will match your EntityID with the EntityID from the other database");
                    sb.AppendLine();
                    sb.AppendLine("EXAMPLE SCENARIO:");
                    sb.AppendLine("  User asks: 'Who has the most items?'");
                    sb.AppendLine("  Database A (aggregation): Returns EntityID=123, CountValue=15");
                    sb.AppendLine("  Database B (lookup): Should return EntityID=123, NameColumn='Value', DescriptionColumn='Value'");
                    sb.AppendLine("  → Database B should NOT filter by CreatedDate - it should return EntityID=123's details");
                    sb.AppendLine();
                }
            }

            sb.AppendLine();
            sb.AppendLine("AMBIGUITY PREVENTION (CRITICAL):");
            sb.AppendLine("  - ALWAYS use meaningful Table Aliases (e.g., use 't1', 't2' or derived from table name).");
            sb.AppendLine("  - ALWAYS qualify columns with these aliases (e.g., 't1.ColumnName', 't2.OtherColumn').");
            sb.AppendLine("  - Example: SELECT t1.Id, t2.Name FROM Table1 t1 JOIN Table2 t2 ON t1.ForeignKey = t2.Id");
            sb.AppendLine("  - NEVER use column names without table alias when joining tables.");
            sb.AppendLine();
            sb.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║  🚨 ADDITIONAL FORBIDDEN PATTERNS 🚨                          ║");
            sb.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("QUERY COMPLEXITY RESTRICTIONS:");
            sb.AppendLine("  ✗ NO nested subqueries (no SELECT inside WHERE)");
            sb.AppendLine("  ✗ NO complex logic (no multiple levels of nesting)");
            sb.AppendLine("  ✗ NO aggregate functions in WHERE clause");
            sb.AppendLine("  ✗ NO using aggregate functions (COUNT, SUM, etc.) without a GROUP BY clause");
            sb.AppendLine("  ✗ NO more than 2 JOINs");
            sb.AppendLine();
            sb.AppendLine("REMEMBER: ONLY SELECT statements are allowed. NO CREATE, DROP, DELETE, EXEC!");
            sb.AppendLine();


            sb.AppendLine("═══════════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("USER'S QUESTION:");
            sb.AppendLine($"   \"{userQuery}\"");
            sb.AppendLine();
            sb.AppendLine("YOUR TASK FOR THIS DATABASE:");
            sb.AppendLine($"   {dbQuery.Purpose}");
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

            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"TABLES AVAILABLE IN {schema.DatabaseName} (ONLY IN THIS DATABASE):");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("🚨 CRITICAL: You MUST ONLY use tables listed below. DO NOT invent or use tables from other databases. 🚨");

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
            sb.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
            sb.AppendLine($"║  🚨 TABLE VALIDATION CHECKLIST - VERIFY BEFORE WRITING SQL 🚨  ║");
            sb.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine($"BEFORE you write ANY table name in your SQL, CHECK:");
            sb.AppendLine();
            sb.AppendLine("1️⃣  Is the table name in the list above?");
            sb.AppendLine($"    ✓ ALLOWED TABLES: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine($"    ✗ FORBIDDEN: Any table NOT in this list");
            sb.AppendLine();
            sb.AppendLine("2️⃣  Are you using the EXACT table name from the list?");
            sb.AppendLine("    ✓ Use exact spelling and case");
            sb.AppendLine("    ✗ Do NOT guess or invent similar table names");
            sb.AppendLine();
            sb.AppendLine("3️⃣  Does each column exist in that table's column list above?");
            sb.AppendLine("    ✓ Cross-reference every column with the AVAILABLE COLUMNS list");
            sb.AppendLine("    ✗ Do NOT use columns from other tables");
            sb.AppendLine();
            sb.AppendLine($"🚨 COMMON MISTAKE TO AVOID:");
            sb.AppendLine($"   If you need a table but it's NOT in the allowed list above,");
            sb.AppendLine($"   DO NOT write: JOIN [TableName]");
            sb.AppendLine($"   INSTEAD: DO NOT use that table at all - it doesn't exist in {schema.DatabaseName}!");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("HOW TO WRITE YOUR SQL QUERY:");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("STEP 1: Choose your tables");
            sb.AppendLine($"   → You can ONLY use: {string.Join(", ", dbQuery.RequiredTables)}");
            sb.AppendLine($"   → These tables ONLY exist in {schema.DatabaseName}");
            sb.AppendLine();
            sb.AppendLine("STEP 2: Write SELECT clause");
            sb.AppendLine("   → Verify EACH column exists in the table's column list above");
            sb.AppendLine("   → Include ALL foreign key columns that exist in the table");
            sb.AppendLine();
            sb.AppendLine("STEP 3: Write FROM clause");
            sb.AppendLine($"   ✓ FROM {dbQuery.RequiredTables[0]} (use allowed table)");
            sb.AppendLine();
            sb.AppendLine("STEP 4: Write JOIN clause (if needed)");
            sb.AppendLine("   → JOIN between allowed tables only");
            sb.AppendLine("   → Verify the referenced table is in the allowed list");
            sb.AppendLine();
            sb.AppendLine("STEP 5: Apply filters and ordering");
            sb.AppendLine("   → WHERE, GROUP BY, ORDER BY as needed");
            sb.AppendLine();
            sb.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║  🚨 FINAL CHECK BEFORE WRITING SQL 🚨                        ║");
            sb.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("BEFORE you write your SQL, verify:");
            sb.AppendLine("  ✓ Does it start with SELECT? (NOT CREATE, DROP, DELETE, EXEC)");
            sb.AppendLine("  ✓ Are all table names in the allowed list above?");
            sb.AppendLine("  ✓ Are all column names in the table's column list?");
            sb.AppendLine("  ✓ Does it follow the simple query pattern?");
            sb.AppendLine();
            sb.AppendLine("IF YOUR SQL CONTAINS CREATE, DROP, DELETE, EXEC, OR ANY OTHER");
            sb.AppendLine("NON-SELECT STATEMENT, IT WILL BE REJECTED AND THE QUERY WILL FAIL!");
            sb.AppendLine();
            sb.AppendLine("NOW WRITE YOUR SQL QUERY (SELECT statement only):");

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
