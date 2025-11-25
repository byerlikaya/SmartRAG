using SmartRAG.Interfaces.Database.Strategies;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartRAG.Services.Database.Prompts
{
    public class SqlPromptBuilder
    {
        private const int SampleDataLimit = 200;
        
        private static readonly HashSet<string> FilterStopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "and", "the", "for", "with", "from", "into", "onto", "about", "over", "under", 
            "between", "within", "without", "will", "would", "could", "should", "have", "has", 
            "had", "been", "being", "than", "then", "them", "they", "their", "there", "those", 
            "these", "when", "where", "which", "while", "whose", "what", "that", "this", "each", 
            "ever", "every", "many", "much", "more", "most", "some", "such", "only", "also", 
            "just", "like", "make", "take", "give", "need", "want", "time", "date", "question", 
            "asked", "asking", "show", "list", "tell", "provide", "please"
        };

        public string Build(string userQuery, DatabaseQueryIntent dbQuery, DatabaseSchemaInfo schema, ISqlDialectStrategy strategy)
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
            sb.AppendLine($"  6. {strategy.GetLimitClause(100)} at the end (or equivalent)");
            sb.AppendLine();
            sb.AppendLine("CROSS-DATABASE LOGIC (CRITICAL):");
            sb.AppendLine("  - If the user asks for a metric or attribute that is NOT in this database:");
            sb.AppendLine("    1. DO NOT try to calculate it or guess it.");
            sb.AppendLine("    2. DO NOT aggregate if it hides the Entity ID needed for joining.");
            sb.AppendLine("    3. INSTEAD, SELECT the Entity ID (Foreign Key) AND the Descriptive Attribute.");
            sb.AppendLine("    4. EXAMPLE: SELECT EntityID, DescriptiveColumn FROM ... (allows joining with other databases)");
            sb.AppendLine("    5. This allows the system to merge results with the database that has the missing metric.");
            sb.AppendLine();
            sb.AppendLine("AMBIGUITY PREVENTION (CRITICAL):");
            sb.AppendLine("  - ALWAYS use meaningful Table Aliases (e.g., use 't1', 't2' or derived from table name).");
            sb.AppendLine("  - ALWAYS qualify columns with these aliases (e.g., 't1.ColumnName', 't2.OtherColumn').");
            sb.AppendLine("  - Example: SELECT t1.Id, t2.Name FROM Table1 t1 JOIN Table2 t2 ON t1.ForeignKey = t2.Id");
            sb.AppendLine("  - NEVER use column names without table alias when joining tables.");
            sb.AppendLine();
            sb.AppendLine("ABSOLUTELY FORBIDDEN:");
            sb.AppendLine("  ✗ NO nested subqueries (no SELECT inside WHERE)");
            sb.AppendLine("  ✗ NO complex logic (no multiple levels of nesting)");
            sb.AppendLine("  ✗ NO aggregate functions in WHERE clause");
            sb.AppendLine("  ✗ NO using aggregate functions (COUNT, SUM, etc.) without a GROUP BY clause");
            sb.AppendLine("  ✗ NO more than 2 JOINs");
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
            sb.AppendLine($"   If you need 'products' table but it's NOT in the allowed list above,");
            sb.AppendLine($"   DO NOT write: JOIN products");
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
            
            return sb.ToString();
        }

        private List<string> ExtractFilterKeywords(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<string>();

            var words = query.Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
            
            return words
                .Where(w => w.Length > 2 && !FilterStopWords.Contains(w))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
