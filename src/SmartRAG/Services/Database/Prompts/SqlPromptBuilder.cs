using SmartRAG.Interfaces.Database;
using SmartRAG.Interfaces.Database.Strategies;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartRAG.Services.Database.Prompts
{
    /// <summary>
    /// Builds prompts for SQL query generation
    /// </summary>
    public class SqlPromptBuilder : ISqlPromptBuilder
    {
        private const int SampleDataLimit = 200;
        private readonly IDatabaseConnectionManager _connectionManager;

        public SqlPromptBuilder(IDatabaseConnectionManager connectionManager = null)
        {
            _connectionManager = connectionManager;
        }

        private static readonly HashSet<string> FilterStopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "for", "with", "from", "into", "onto", "about", "over", "under",
            "between", "within", "without", "through", "during", "before", "after", "above", "below",
            "will", "would", "could", "should", "have", "has", "had", "been", "being", "is", "are", "was", "were",
            "than", "then", "them", "they", "their", "there", "those", "these", "this", "that", "each", "every",
            "when", "where", "which", "while", "whose", "what", "ever", "many", "much", "more", "most", "some", "such",
            "only", "also", "just", "like", "make", "take", "give", "need", "want",
            "time", "date", "question", "asked", "asking", "show", "list", "tell", "provide", "please"
        };

        /// <summary>
        /// Quotes PostgreSQL identifier (schema.table) properly for case-sensitive names
        /// </summary>
        private static string QuotePostgreSqlIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return identifier;

            if (identifier.Contains('.'))
            {
                var parts = identifier.Split('.', 2);
                var schemaPart = parts[0];
                var tablePart = parts[1];
                
                var quotedSchema = HasUpperCase(schemaPart) ? $"\"{schemaPart}\"" : schemaPart;
                var quotedTable = HasUpperCase(tablePart) ? $"\"{tablePart}\"" : tablePart;
                
                return $"{quotedSchema}.{quotedTable}";
            }
            
            return HasUpperCase(identifier) ? $"\"{identifier}\"" : identifier;
        }

        /// <summary>
        /// Checks if string contains uppercase letters
        /// </summary>
        private static bool HasUpperCase(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return false;
            
            foreach (var c in str)
            {
                if (char.IsUpper(c))
                    return true;
            }
            
            return false;
        }

        private static bool IsNumericColumn(string dataType)
        {
            if (string.IsNullOrWhiteSpace(dataType))
                return false;

            var typeLower = dataType.ToLowerInvariant();
            var numericTypes = new[] { "int", "bigint", "smallint", "tinyint", "decimal", "numeric", "money", "float", "real", "double", "number" };
            return numericTypes.Any(nt => typeLower.Contains(nt));
        }

        private static bool IsTextColumn(string dataType)
        {
            if (string.IsNullOrWhiteSpace(dataType))
                return false;

            var typeLower = dataType.ToLowerInvariant();
            var textTypes = new[] { "varchar", "nvarchar", "text", "char", "nchar", "string", "ntext" };
            return textTypes.Any(tt => typeLower.Contains(tt));
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
            catch
            {
            }

            return mappings;
        }

        private List<CrossDatabaseMapping> GetRelevantCrossDatabaseMappings(string databaseName, List<string> requiredTables)
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
                    
                    foreach (var mapping in connection.CrossDatabaseMappings)
                    {
                        bool isRelevant = false;
                        
                        if (mapping.SourceDatabase.Equals(databaseName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (requiredTables == null || requiredTables.Count == 0 ||
                                requiredTables.Any(t => t.Equals(mapping.SourceTable, StringComparison.OrdinalIgnoreCase) ||
                                                       t.Contains(mapping.SourceTable.Split('.').Last(), StringComparison.OrdinalIgnoreCase)))
                            {
                                isRelevant = true;
                            }
                        }
                        
                        if (mapping.TargetDatabase.Equals(databaseName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (requiredTables == null || requiredTables.Count == 0 ||
                                requiredTables.Any(t => t.Equals(mapping.TargetTable, StringComparison.OrdinalIgnoreCase) ||
                                                       t.Contains(mapping.TargetTable.Split('.').Last(), StringComparison.OrdinalIgnoreCase)))
                            {
                                var reverseMapping = new CrossDatabaseMapping
                                {
                                    SourceDatabase = mapping.TargetDatabase,
                                    SourceTable = mapping.TargetTable,
                                    SourceColumn = mapping.TargetColumn,
                                    TargetDatabase = mapping.SourceDatabase,
                                    TargetTable = mapping.SourceTable,
                                    TargetColumn = mapping.SourceColumn,
                                    RelationshipType = mapping.RelationshipType
                                };
                                if (!mappings.Any(m => m.SourceDatabase == reverseMapping.SourceDatabase &&
                                                      m.SourceTable == reverseMapping.SourceTable &&
                                                      m.SourceColumn == reverseMapping.SourceColumn &&
                                                      m.TargetDatabase == reverseMapping.TargetDatabase &&
                                                      m.TargetTable == reverseMapping.TargetTable &&
                                                      m.TargetColumn == reverseMapping.TargetColumn))
                                {
                                    mappings.Add(reverseMapping);
                                }
                                isRelevant = true;
                            }
                        }
                        
                        if (isRelevant && mapping.SourceDatabase.Equals(databaseName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!mappings.Any(m => m.SourceDatabase == mapping.SourceDatabase &&
                                                  m.SourceTable == mapping.SourceTable &&
                                                  m.SourceColumn == mapping.SourceColumn &&
                                                  m.TargetDatabase == mapping.TargetDatabase &&
                                                  m.TargetTable == mapping.TargetTable &&
                                                  m.TargetColumn == mapping.TargetColumn))
                            {
                                mappings.Add(mapping);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return mappings;
        }

        public string BuildMultiDatabase(string userQuery, QueryIntent queryIntent, Dictionary<string, DatabaseSchemaInfo> schemas, Dictionary<string, ISqlDialectStrategy> strategies)
        {
            if (queryIntent == null || queryIntent.DatabaseQueries == null || queryIntent.DatabaseQueries.Count == 0)
                throw new ArgumentException("QueryIntent must contain at least one database query", nameof(queryIntent));


            var sb = new StringBuilder();
            
            sb.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║  🚨🚨🚨 MULTI-DATABASE QUERY - GENERATE SQL FOR ALL! 🚨🚨🚨  ║");
            sb.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("🚨🚨🚨🚨🚨 CRITICAL RULE #1 - READ THIS FIRST! 🚨🚨🚨🚨🚨");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("EACH DATABASE IS COMPLETELY SEPARATE AND ISOLATED:");
            sb.AppendLine($"  → You will write SQL queries for {queryIntent.DatabaseQueries.Count} different database(s)");
            sb.AppendLine("  ✗✗✗ NEVER use tables from one database in another database's query!");
            sb.AppendLine("  ✗✗✗ NEVER write: OtherDatabaseName.SchemaName.TableName");
            sb.AppendLine("  ✗✗✗ NEVER write: [OtherDatabaseName].[SchemaName].[TableName]");
            sb.AppendLine("  ✗✗✗ NEVER write: \"OtherDatabaseName\".SchemaName.TableName");
            sb.AppendLine("  ✗✗✗ NEVER use subqueries that reference other databases!");
            sb.AppendLine();
            sb.AppendLine("✓✓✓ CORRECT APPROACH:");
            sb.AppendLine("  → Each database query uses ONLY tables from that specific database");
            sb.AppendLine("  → Look at the table list for each database below - use ONLY those tables");
            sb.AppendLine("  → If you need data from another database, use literal values or parameters");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"📊 USER QUERY: \"{userQuery}\"");
            sb.AppendLine($"🎯 TOTAL DATABASES: {queryIntent.DatabaseQueries.Count}");
            sb.AppendLine();
            sb.AppendLine("🚨 CRITICAL: You will generate SQL queries for MULTIPLE databases.");
            sb.AppendLine("Each database has different tables and columns.");
            sb.AppendLine("You MUST understand the relationships between databases using CROSS-DATABASE MAPPINGS below.");
            sb.AppendLine();
            
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"🚨🚨🚨 CRITICAL: YOU ARE WRITING SQL FOR {queryIntent.DatabaseQueries.Count} DATABASE(S) 🚨🚨🚨");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();

            if (queryIntent.DatabaseQueries.Count == 1)
            {
                sb.AppendLine("⚠️⚠️⚠️ SINGLE DATABASE MODE ⚠️⚠️⚠️");
                sb.AppendLine();
                sb.AppendLine("CRITICAL: You are writing SQL for ONLY ONE database!");
                sb.AppendLine($"  → Generate EXACTLY 1 SQL query for: {queryIntent.DatabaseQueries[0].DatabaseId}");
                sb.AppendLine("  → Use JOINs to combine multiple tables in the SAME database");
                sb.AppendLine("  → Do NOT create multiple SQL blocks for the same database!");
                sb.AppendLine("  → Do NOT generate SQL for other databases!");
                sb.AppendLine();
                sb.AppendLine("✓ CORRECT: One SQL query with JOINs");
                sb.AppendLine("✗ WRONG: Multiple SQL queries for same database");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("⚠️⚠️⚠️ MULTI-DATABASE MODE ⚠️⚠️⚠️");
                sb.AppendLine();
                sb.AppendLine($"CRITICAL: You are writing SQL for {queryIntent.DatabaseQueries.Count} DIFFERENT databases!");
                sb.AppendLine($"  → Generate EXACTLY {queryIntent.DatabaseQueries.Count} SQL queries (one per database)");
                sb.AppendLine("  → Each database gets ONE SQL block");
                sb.AppendLine("  → Queries execute sequentially (first query results feed into second)");
                sb.AppendLine();
            }
            
            var allMappings = GetAllCrossDatabaseMappings();
            if (allMappings.Any() && queryIntent.DatabaseQueries.Count > 1)
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("🔗 CROSS-DATABASE MAPPINGS - UNDERSTAND RELATIONSHIPS!");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine();
                sb.AppendLine("These mappings show how columns in different databases relate:");
                foreach (var mapping in allMappings)
                {
                    sb.AppendLine($"  • {mapping.SourceDatabase}.{mapping.SourceTable}.{mapping.SourceColumn}");
                    sb.AppendLine($"    → {mapping.TargetDatabase}.{mapping.TargetTable}.{mapping.TargetColumn}");
                }
                sb.AppendLine();
                sb.AppendLine("🚨 CRITICAL RULE:");
                sb.AppendLine("  → If you SELECT from SourceTable, you MUST include SourceColumn in SELECT!");
                sb.AppendLine("  → If you SELECT from TargetTable, you MUST include TargetColumn in SELECT!");
                sb.AppendLine("  → These columns are needed to JOIN results from different databases!");
                sb.AppendLine();
            }

            for (int i = 0; i < queryIntent.DatabaseQueries.Count; i++)
            {
                var dbQuery = queryIntent.DatabaseQueries[i];
                var schema = schemas[dbQuery.DatabaseId];
                var strategy = strategies[dbQuery.DatabaseId];

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine($"📊 DATABASE #{i + 1}: {schema.DatabaseName} ({strategy.DatabaseType})");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine($"🚨🚨🚨🚨🚨 DATABASE #{i + 1}: {schema.DatabaseName} - CRITICAL RULES 🚨🚨🚨🚨🚨");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
                sb.AppendLine("⚠️⚠️⚠️ BEFORE WRITING SQL FOR THIS DATABASE, READ THIS! ⚠️⚠️⚠️");
                sb.AppendLine();
                sb.AppendLine($"This is DATABASE #{i + 1} named: {schema.DatabaseName}");
                sb.AppendLine($"You are writing SQL query FOR THIS DATABASE ONLY!");
                sb.AppendLine();
                sb.AppendLine("✗✗✗✗✗ FORBIDDEN - NEVER DO THESE IN SQL FOR THIS DATABASE: ✗✗✗✗✗");
                sb.AppendLine($"  1. ✗ NEVER write: OtherDatabaseName.SchemaName.TableName");
                sb.AppendLine($"  2. ✗ NEVER write: [OtherDatabaseName].[SchemaName].[TableName]");
                sb.AppendLine($"  3. ✗ NEVER write: SELECT * FROM OtherDatabase.Schema.Table");
                sb.AppendLine($"  4. ✗ NEVER write: WHERE Column IN (SELECT Column FROM OtherDatabase.Table)");
                sb.AppendLine($"  5. ✗ NEVER reference ANY table that belongs to ANOTHER database");
                sb.AppendLine($"  6. ✗ If you see multiple databases listed → They are ALL COMPLETELY SEPARATE!");
                sb.AppendLine($"  7. ✗ For {schema.DatabaseName} query → ONLY use {schema.DatabaseName} tables!");
                sb.AppendLine();
                sb.AppendLine($"✓✓✓✓✓ ALLOWED - ONLY THESE TABLES EXIST IN {schema.DatabaseName}: ✓✓✓✓✓");
            foreach (var tableName in dbQuery.RequiredTables)
            {
                    sb.AppendLine($"  ✓ {tableName}");
                }
                    sb.AppendLine();
                sb.AppendLine("🚨🚨🚨 REMEMBER: 🚨🚨🚨");
                sb.AppendLine($"  → When writing SQL for DATABASE #{i + 1} ({schema.DatabaseName})");
                sb.AppendLine($"  → Look at the table list ABOVE");
                sb.AppendLine($"  → Use ONLY those tables in your SQL");
                sb.AppendLine($"  → If a table name is NOT in the list above, it DOES NOT EXIST in {schema.DatabaseName}!");
                sb.AppendLine($"  → DO NOT invent tables from other databases!");
                sb.AppendLine();
                sb.AppendLine($"Purpose: {dbQuery.Purpose}");
                sb.AppendLine();
                
                var dialectInfo = strategy.DatabaseType == SmartRAG.Enums.DatabaseType.SqlServer
                    ? "SQL Server - Use [brackets] for identifiers with spaces"
                    : strategy.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL
                        ? "PostgreSQL - Use \"quotes\" for case-sensitive identifiers, case-sensitive!"
                        : strategy.DatabaseType == SmartRAG.Enums.DatabaseType.MySQL
                            ? "MySQL - Use backticks for identifiers with spaces"
                            : "SQLite - Use double quotes for identifiers with spaces";
                
                sb.AppendLine($"💾 SQL DIALECT: {dialectInfo}");
                
                    if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
                    {
                        sb.AppendLine();
                    sb.AppendLine("🚨🚨🚨 POSTGRESQL RULES - CRITICAL! 🚨🚨🚨");
                    sb.AppendLine("  → PostgreSQL is CASE-SENSITIVE for identifiers!");
                    sb.AppendLine("  → You MUST use double quotes around ALL identifiers (table names, column names)!");
                    sb.AppendLine("  ✓ CORRECT: SELECT \"ColumnName1\", \"ColumnName2\" FROM \"SchemaName\".\"TableName\"");
                    sb.AppendLine("  ✗ WRONG: SELECT ColumnName1, ColumnName2 FROM SchemaName.TableName  -- Will fail with 'column does not exist'!");
                        sb.AppendLine();
                    sb.AppendLine("  → PostgreSQL uses LIMIT, NOT TOP!");
                    sb.AppendLine("  ✓ CORRECT: SELECT \"ColumnName\" FROM \"SchemaName\".\"TableName\" ORDER BY \"ColumnName\" DESC LIMIT 5");
                    sb.AppendLine("  ✗ WRONG: SELECT TOP 5 \"ColumnName\" FROM \"SchemaName\".\"TableName\"  -- SYNTAX ERROR! PostgreSQL does not support TOP!");
                    sb.AppendLine("  → LIMIT MUST be at the END, after ORDER BY clause!");
                        sb.AppendLine();
                }
                
                if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.SqlServer)
                {
                    sb.AppendLine();
                    sb.AppendLine("🚨🚨🚨 SQL SERVER TOP N RULE - CRITICAL! 🚨🚨🚨");
                    sb.AppendLine("  ✗✗✗ LIMIT is FORBIDDEN in SQL Server! Use TOP N instead!");
                    sb.AppendLine("  ✓ CORRECT: SELECT TOP 5 ... FROM ... ORDER BY ...");
                    sb.AppendLine("  ✗ WRONG: SELECT ... FROM ... ORDER BY ... LIMIT 5  -- SYNTAX ERROR!");
                    sb.AppendLine("  → TOP N MUST be immediately after SELECT keyword");
                    sb.AppendLine("  → Example: SELECT TOP 5 ColumnName FROM TableName ORDER BY ColumnName DESC");
                }
                else if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.MySQL)
                {
                    sb.AppendLine();
                    sb.AppendLine("🚨🚨🚨 MySQL SYNTAX RULES - CRITICAL! 🚨🚨🚨");
                    sb.AppendLine("  1. Use BACKTICKS for identifiers: `TableName`, `ColumnName`");
                    sb.AppendLine("     ✓ CORRECT: SELECT `ColumnA`, `ColumnB` FROM `SchemaName`.`TableName`");
                    sb.AppendLine("     ✗ WRONG: SELECT \"ColumnA\", \"ColumnB\" FROM \"SchemaName\".\"TableName\"  -- SYNTAX ERROR!");
                    sb.AppendLine("  2. Use LIMIT, NOT TOP!");
                    sb.AppendLine("     ✓ CORRECT: SELECT `ColumnA` FROM `TableName` ORDER BY `ColumnA` LIMIT 5");
                    sb.AppendLine("     ✗ WRONG: SELECT TOP 5 `ColumnA` FROM `TableName`  -- SYNTAX ERROR!");
                    sb.AppendLine("  → LIMIT N MUST be at the END, after ORDER BY clause");
                }
                else if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL || 
                         strategy.DatabaseType == SmartRAG.Enums.DatabaseType.SQLite)
                {
                    sb.AppendLine();
                    sb.AppendLine("🚨🚨🚨 LIMIT RULE - CRITICAL! 🚨🚨🚨");
                    sb.AppendLine("  → Use LIMIT, NOT TOP!");
                    sb.AppendLine("  ✓ CORRECT: SELECT ... FROM ... ORDER BY ... LIMIT 5");
                    sb.AppendLine("  ✗ WRONG: SELECT TOP 5 ... FROM ...  -- SYNTAX ERROR! TOP is not supported!");
                    sb.AppendLine("  → LIMIT N MUST be at the END, after ORDER BY clause");
                }
                
                    sb.AppendLine();
                
                foreach (var tableName in dbQuery.RequiredTables)
                {
                    var table = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                    if (table != null)
                    {
                        var tableType = table.RowCount > 10000 ? "TRANSACTIONAL" : (table.RowCount > 1000 ? "LOOKUP" : "MASTER");
                        sb.AppendLine($"📋 TABLE: {table.TableName} (Rows: {table.RowCount:N0}, Type: {tableType})");
                        
                        var pkColumns = table.PrimaryKeys.Any() ? table.PrimaryKeys : table.Columns.Where(c => c.IsPrimaryKey).Select(c => c.ColumnName).ToList();
                        if (pkColumns.Any())
                        {
                            sb.AppendLine($"   PK: {string.Join(", ", pkColumns)}");
                        }

                    if (table.ForeignKeys.Any())
                    {
                            sb.AppendLine("   Foreign Keys:");
                            foreach (var fk in table.ForeignKeys.Take(5))
                            {
                                sb.AppendLine($"     {fk.ColumnName} → {fk.ReferencedTable}.{fk.ReferencedColumn}");
                            }
                        }
                        
                        if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
                        {
                            var quotedTableName = QuotePostgreSqlIdentifier(table.TableName);
                            sb.AppendLine($"   Use EXACT format: {quotedTableName}");
                            sb.AppendLine($"   PostgreSQL columns (with quotes): {string.Join(", ", table.Columns.Select(c => QuotePostgreSqlIdentifier(c.ColumnName)))}");
                            sb.AppendLine($"   🚨 REMEMBER: All PostgreSQL identifiers MUST be quoted!");
                        }
                        else
                        {
                            var importantColumns = table.Columns
                                .Where(c => c.IsPrimaryKey || c.IsForeignKey || 
                                           IsNumericColumn(c.DataType) || IsTextColumn(c.DataType))
                                .Take(12)
                                .Select(c => {
                                    var markers = new List<string>();
                                    if (c.IsPrimaryKey) markers.Add("PK");
                                    if (c.IsForeignKey) markers.Add("FK");
                                    var markerStr = markers.Any() ? $"[{string.Join(",", markers)}]" : "";
                                    return $"{c.ColumnName}({c.DataType}){markerStr}";
                                });
                            sb.AppendLine($"   Columns: {string.Join(", ", importantColumns)}");
                        }
                        
                        var relevantMappings = allMappings.Where(m =>
                            (m.SourceDatabase.Equals(schema.DatabaseName, StringComparison.OrdinalIgnoreCase) &&
                             m.SourceTable.Equals(table.TableName, StringComparison.OrdinalIgnoreCase)) ||
                            (m.TargetDatabase.Equals(schema.DatabaseName, StringComparison.OrdinalIgnoreCase) &&
                             m.TargetTable.Equals(table.TableName, StringComparison.OrdinalIgnoreCase))).ToList();
                        
                        if (relevantMappings.Any())
                        {
                            sb.AppendLine("   🚨 REQUIRED MAPPING COLUMNS (MUST include in SELECT):");
                            foreach (var mapping in relevantMappings)
                            {
                                if (mapping.SourceDatabase.Equals(schema.DatabaseName, StringComparison.OrdinalIgnoreCase))
                                {
                                    sb.AppendLine($"     • {mapping.SourceColumn} (maps to {mapping.TargetDatabase}.{mapping.TargetColumn})");
                                }
                                else
                                {
                                    sb.AppendLine($"     • {mapping.TargetColumn} (maps from {mapping.SourceDatabase}.{mapping.SourceColumn})");
                                }
                            }
                        }
            sb.AppendLine();
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("🚨🚨🚨 OUTPUT FORMAT - MANDATORY! NO MARKDOWN! 🚨🚨🚨");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("❌❌❌ FORBIDDEN: Do NOT use markdown code blocks (```sql or ```)!");
            sb.AppendLine("❌❌❌ FORBIDDEN: Do NOT use markdown headers (### or ##)!");
            sb.AppendLine("❌❌❌ FORBIDDEN: Do NOT add explanations or notes after SQL!");
            sb.AppendLine("❌❌❌ FORBIDDEN: Do NOT use TEXT placeholders like VALUE1, VALUE2, [values], or (something)!");
            sb.AppendLine("❌❌❌ FORBIDDEN: Do NOT use bracket/parenthesis placeholders like [values from previous database results]!");
            sb.AppendLine();
            sb.AppendLine("⚠️⚠️⚠️ CRITICAL: All SQL queries must be EXECUTABLE AS-IS! ⚠️⚠️⚠️");
            sb.AppendLine("  → For sequential queries: Use NUMERIC placeholder values (system will replace them)");
            sb.AppendLine("  → These are EXAMPLE numbers - system replaces them with real values from first query");
            sb.AppendLine("  → ✓ CORRECT: WHERE column IN (101, 205, 387, 412, 598)  -- Example IDs, system replaces");
            sb.AppendLine("  → ✗ WRONG: WHERE column IN (VALUE1, VALUE2, VALUE3)  -- Text causes SQL error!");
            sb.AppendLine("  → ✗ WRONG: WHERE column IN ([values from previous database results])  -- Bracket causes SQL error!");
            sb.AppendLine("  → Use actual column/table names from the schema above");
            sb.AppendLine();
            sb.AppendLine("✓✓✓ REQUIRED: Use EXACTLY this format (copy-paste this structure):");
            sb.AppendLine();
            for (int i = 0; i < queryIntent.DatabaseQueries.Count; i++)
            {
                var dbQuery = queryIntent.DatabaseQueries[i];
                var schema = schemas[dbQuery.DatabaseId];
                sb.AppendLine($"DATABASE {i + 1}: {schema.DatabaseName}");
                sb.AppendLine("CONFIRMED");
                sb.AppendLine("SELECT [columns] FROM [tables] WHERE [conditions] ORDER BY [columns] [TOP N or LIMIT N];");
                sb.AppendLine();
            }
            sb.AppendLine("EXAMPLE FORMAT (STRUCTURE ONLY - REPLACE WITH ACTUAL DATABASE/TABLE/COLUMN NAMES):");
            sb.AppendLine();
            for (int i = 0; i < queryIntent.DatabaseQueries.Count && i < 3; i++)
            {
                var dbQuery = queryIntent.DatabaseQueries[i];
                var schema = schemas[dbQuery.DatabaseId];
                var strategy = strategies[dbQuery.DatabaseId];
                
                sb.AppendLine($"DATABASE {i + 1}: {schema.DatabaseName}");
                sb.AppendLine("CONFIRMED");
                
                if (i == 0)
                {
                    if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.SqlServer)
                    {
                        sb.AppendLine("SELECT TOP 5 T1.JoinColumnID, COUNT(T2.RelatedID) AS CountValue FROM SchemaName.TableName1 T1 INNER JOIN SchemaName.TableName2 T2 ON T1.PrimaryKeyID = T2.ForeignKeyID GROUP BY T1.JoinColumnID ORDER BY CountValue DESC;");
                    }
                    else
                    {
                        sb.AppendLine("SELECT T1.JoinColumnID, COUNT(T2.RelatedID) AS CountValue FROM SchemaName.TableName1 T1 INNER JOIN SchemaName.TableName2 T2 ON T1.PrimaryKeyID = T2.ForeignKeyID GROUP BY T1.JoinColumnID ORDER BY CountValue DESC LIMIT 5;");
                    }
                }
                else
                {
            if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
            {
                        sb.AppendLine("SELECT \"JoinColumnID\", \"ColumnName1\", \"ColumnName2\" FROM \"SchemaName\".\"TableName1\" WHERE \"JoinColumnID\" IN (1, 5, 10, 15, 20);");
            }
            else
            {
                        sb.AppendLine("SELECT JoinColumnID, ColumnName1, ColumnName2 FROM SchemaName.TableName1 WHERE JoinColumnID IN (1, 5, 10, 15, 20);");
            }
                }
            sb.AppendLine();
            }
            if (queryIntent.DatabaseQueries.Count > 3)
            {
                sb.AppendLine($"... (and {queryIntent.DatabaseQueries.Count - 3} more database(s)) ...");
                sb.AppendLine();
            }
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("⚠️⚠️⚠️ CRITICAL EXAMPLES - STUDY THESE CAREFULLY! ⚠️⚠️⚠️");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"SCENARIO: You need data from multiple databases ({queryIntent.DatabaseQueries.Count} database(s) listed above)");
            sb.AppendLine();
            sb.AppendLine("✗✗✗✗✗ WRONG - Using another database's tables (NEVER DO THIS!): ✗✗✗✗✗");
            sb.AppendLine("DATABASE X: CurrentDatabaseName");
            sb.AppendLine("CONFIRMED");
            sb.AppendLine("SELECT ColumnName FROM OtherDatabaseName.SchemaName.TableName1 WHERE ...  -- ✗✗✗ FORBIDDEN!");
            sb.AppendLine("-- REASON: OtherDatabaseName tables do NOT exist in CurrentDatabaseName!");
            sb.AppendLine();
            sb.AppendLine("✗✗✗✗✗ WRONG - Subquery with cross-database reference (NEVER DO THIS!): ✗✗✗✗✗");
            sb.AppendLine("DATABASE X: CurrentDatabaseName");
            sb.AppendLine("CONFIRMED");
            sb.AppendLine("SELECT * FROM SchemaName.TableName1 WHERE JoinColumnID IN (SELECT JoinColumnID FROM OtherDatabaseName.SchemaName.TableName1);  -- ✗✗✗ FORBIDDEN!");
            sb.AppendLine("-- REASON: Subquery references OtherDatabaseName, but we're in CurrentDatabaseName!");
            sb.AppendLine();
            sb.AppendLine("✗✗✗✗✗ WRONG - 3-part name with different database (NEVER DO THIS!): ✗✗✗✗✗");
            sb.AppendLine("DATABASE X: CurrentDatabaseName");
            sb.AppendLine("CONFIRMED");
            sb.AppendLine("SELECT ColumnName FROM [OtherDatabaseName].[SchemaName].[TableName1] WHERE ...  -- ✗✗✗ FORBIDDEN!");
            sb.AppendLine("-- REASON: [OtherDatabaseName] is a DIFFERENT database!");
            sb.AppendLine();
            sb.AppendLine("✓✓✓✓✓ CORRECT - Each database uses ONLY its own tables: ✓✓✓✓✓");
            sb.AppendLine("DATABASE X: CurrentDatabaseName");
            sb.AppendLine("CONFIRMED");
            sb.AppendLine("SELECT JoinColumnID, ColumnName1, ColumnName2 FROM SchemaName.TableName1 WHERE JoinColumnID IN (1, 5, 10);");
            sb.AppendLine("-- REASON: Uses ONLY tables that exist in CurrentDatabaseName (listed above)");
            sb.AppendLine("-- NOTE: The values (1, 5, 10) come from previous database result - but we don't reference other databases!");
            sb.AppendLine();
            sb.AppendLine("🚨 KEY POINT: Each database query is INDEPENDENT!");
            sb.AppendLine("  → Previous database query returns: JoinColumnID values (e.g., 1, 5, 10)");
            sb.AppendLine("  → Current database query uses: WHERE JoinColumnID IN (1, 5, 10) - but ONLY uses current database tables!");
            sb.AppendLine("  → NEVER reference tables from other databases in your SQL!");
            sb.AppendLine();
            sb.AppendLine("🚨🚨🚨 CRITICAL RULES - READ EVERY WORD! 🚨🚨🚨");
            sb.AppendLine();
            sb.AppendLine("RULE 1: Generate SQL for ALL databases listed above");
            sb.AppendLine("  → You must generate SQL for EACH database separately");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("🚨🚨🚨🚨🚨 RULE 2: CROSS-DATABASE REFERENCE = FORBIDDEN! 🚨🚨🚨🚨🚨");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("⚠️⚠️⚠️ THIS IS THE MOST CRITICAL RULE - VIOLATION = QUERY FAILURE! ⚠️⚠️⚠️");
            sb.AppendLine();
            sb.AppendLine("FUNDAMENTAL PRINCIPLE:");
            sb.AppendLine($"  → You have {queryIntent.DatabaseQueries.Count} separate, isolated database(s)");
            sb.AppendLine("  → Each database has its OWN tables - they do NOT share tables!");
            sb.AppendLine("  → Tables in one database do NOT exist in another database");
            sb.AppendLine("  → You CANNOT query tables from one database while writing SQL for another!");
            sb.AppendLine();
            sb.AppendLine("✗✗✗✗✗ FORBIDDEN PATTERNS (NEVER USE THESE!): ✗✗✗✗✗");
            sb.AppendLine();
            sb.AppendLine("  1. ✗ OtherDatabaseName.SchemaName.TableName");
            sb.AppendLine("     Example: SELECT * FROM DatabaseX.SchemaName.TableName1;  -- ✗ WRONG if writing for DatabaseY!");
            sb.AppendLine();
            sb.AppendLine("  2. ✗ [OtherDatabaseName].[SchemaName].[TableName]");
            sb.AppendLine("     Example: SELECT * FROM [DatabaseX].[Schema].[Table];  -- ✗ WRONG if writing for DatabaseY!");
            sb.AppendLine();
            sb.AppendLine("  3. ✗ Subquery referencing other database");
            sb.AppendLine("     Example: WHERE Column IN (SELECT Column FROM OtherDatabase.Table);  -- ✗ WRONG!");
            sb.AppendLine();
            sb.AppendLine("  4. ✗ JOIN with other database table");
            sb.AppendLine("     Example: FROM Table1 T1 JOIN OtherDatabase.Schema.Table2 T2 ON ...  -- ✗ WRONG!");
            sb.AppendLine();
            sb.AppendLine("✓✓✓✓✓ CORRECT APPROACH: ✓✓✓✓✓");
            sb.AppendLine();
            sb.AppendLine("  For EACH database listed above:");
            sb.AppendLine("    → Write SQL using ONLY tables from that specific database");
            sb.AppendLine("    → Look at the table list for that database - use ONLY those tables");
            sb.AppendLine("    → If you need values from a previous database's results, use literal values");
            sb.AppendLine();
            sb.AppendLine("  CORRECT EXAMPLE (2 databases):");
            sb.AppendLine("    Database 1: Returns JoinColumnID values (e.g., 1, 5, 10)");
            sb.AppendLine("    Database 2: SELECT * FROM SchemaName.TableName1 WHERE JoinColumnID IN (1, 5, 10);");
            sb.AppendLine("    → Note: Values come from Database 1 result, but we don't reference Database 1 in Database 2 SQL!");
            sb.AppendLine();
            sb.AppendLine($"  CORRECT EXAMPLE ({queryIntent.DatabaseQueries.Count} databases):");
            sb.AppendLine("    → Write {queryIntent.DatabaseQueries.Count} separate SQL queries");
            sb.AppendLine("    → Each query uses ONLY its own database's tables");
            sb.AppendLine("    → Use literal values from previous results, NOT subqueries to other databases!");
            sb.AppendLine();
            sb.AppendLine("RULE 3: SQL Server TOP N Rule (CRITICAL!)");
            sb.AppendLine("  → For SQL Server: Use SELECT TOP N (immediately after SELECT)");
            sb.AppendLine("  → Example: SELECT TOP 5 ColumnName FROM TableName ORDER BY ColumnName DESC");
            sb.AppendLine("  ✗✗✗ NEVER use LIMIT in SQL Server - it will cause SYNTAX ERROR!");
            sb.AppendLine();
            sb.AppendLine("RULE 4: LIMIT Rule (for PostgreSQL/MySQL/SQLite)");
            sb.AppendLine("  → For PostgreSQL/MySQL/SQLite: Use LIMIT N at the END (after ORDER BY)");
            sb.AppendLine("  → Example: SELECT ColumnName FROM TableName ORDER BY ColumnName DESC LIMIT 5");
            sb.AppendLine();
            sb.AppendLine("RULE 5: Include ALL mapping columns in SELECT");
            sb.AppendLine("  → If mapping columns are listed for a table, you MUST include them in SELECT");
            sb.AppendLine("  → These columns are needed to JOIN results from different databases");
            sb.AppendLine();
            sb.AppendLine("RULE 6: Use table aliases in JOINs");
            sb.AppendLine("  → Always use: FROM Table1 T1 JOIN Table2 T2 ON T1.Column = T2.Column");
            sb.AppendLine("  → Always prefix columns: T1.ColumnName, T2.ColumnName");
            sb.AppendLine();

            return sb.ToString();
        }

        public SqlPromptParts BuildMultiDatabaseSeparated(string userQuery, QueryIntent queryIntent, Dictionary<string, DatabaseSchemaInfo> schemas, Dictionary<string, ISqlDialectStrategy> strategies)
        {
            if (queryIntent == null || queryIntent.DatabaseQueries == null || queryIntent.DatabaseQueries.Count == 0)
                throw new ArgumentException("QueryIntent must contain at least one database query", nameof(queryIntent));

            var systemMessage = BuildSystemMessage(queryIntent, schemas, strategies);
            var userMessage = BuildUserMessage(userQuery, queryIntent, schemas, strategies);

            return new SqlPromptParts
            {
                SystemMessage = systemMessage,
                UserMessage = userMessage
            };
        }

        private string BuildSystemMessage(QueryIntent queryIntent, Dictionary<string, DatabaseSchemaInfo> schemas, Dictionary<string, ISqlDialectStrategy> strategies)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("DATABASE SCHEMA INFORMATION");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"Total Databases: {queryIntent.DatabaseQueries.Count}");
            sb.AppendLine();

            var allMappings = GetAllCrossDatabaseMappings();
            if (allMappings.Any())
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("🔗 CROSS-DATABASE MAPPINGS - SEQUENTIAL EXECUTION PATTERN");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine();
                sb.AppendLine("⚠️ CRITICAL: These mappings show that queries will execute SEQUENTIALLY:");
                sb.AppendLine();
                foreach (var mapping in allMappings)
                {
                    sb.AppendLine($"  {mapping.SourceDatabase}.{mapping.SourceColumn} → {mapping.TargetDatabase}.{mapping.TargetColumn}");
                    sb.AppendLine($"    Execution: Query {mapping.SourceDatabase} FIRST, then use {mapping.SourceColumn} values to query {mapping.TargetDatabase} using {mapping.TargetColumn}");
                }
                sb.AppendLine();
                sb.AppendLine("🎯 HOW SEQUENTIAL EXECUTION WORKS:");
                sb.AppendLine("  1. First query executes → returns values");
                sb.AppendLine("  2. System extracts mapping column values from results");
                sb.AppendLine("  3. System injects these values into second query");
                sb.AppendLine("  4. Second query executes with real values");
                sb.AppendLine();
                sb.AppendLine("💡 YOUR TASK:");
                sb.AppendLine("  → First query: MUST include mapping column in SELECT");
                sb.AppendLine("  → Second query: Use NUMERIC placeholder values (system will replace them)");
                sb.AppendLine("  → Example: WHERE ColumnName IN (101, 205, 387, 412, 598)");
                sb.AppendLine("  → ⚠️ CRITICAL: Use NUMERIC values (101, 205), NOT text (VALUE1, VALUE2)!");
                sb.AppendLine("  → Use at least 5-10 realistic numeric placeholder values to show the pattern");
                sb.AppendLine();
            }

            for (int i = 0; i < queryIntent.DatabaseQueries.Count; i++)
            {
                var dbQuery = queryIntent.DatabaseQueries[i];
                var schema = schemas[dbQuery.DatabaseId];
                var strategy = strategies[dbQuery.DatabaseId];
                
                sb.AppendLine($"DATABASE {i + 1}: {schema.DatabaseName}");
                sb.AppendLine($"  Type: {strategy.DatabaseType}");
                sb.AppendLine($"  Purpose: {dbQuery.Purpose}");
                sb.AppendLine($"  Tables:");
                
                foreach (var tableName in dbQuery.RequiredTables)
                {
                    var table = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                    if (table != null)
                    {
                        sb.AppendLine($"    • {table.TableName}");
                        if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
                        {
                            sb.AppendLine($"      Columns: {string.Join(", ", table.Columns.Select(c => QuotePostgreSqlIdentifier(c.ColumnName)))}");
                        }
                        else
                        {
                            sb.AppendLine($"      Columns: {string.Join(", ", table.Columns.Select(c => c.ColumnName))}");
                        }
                        
                        var relevantMappings = allMappings.Where(m =>
                            (m.SourceDatabase.Equals(schema.DatabaseName, StringComparison.OrdinalIgnoreCase) &&
                             m.SourceTable.Equals(table.TableName, StringComparison.OrdinalIgnoreCase)) ||
                            (m.TargetDatabase.Equals(schema.DatabaseName, StringComparison.OrdinalIgnoreCase) &&
                             m.TargetTable.Equals(table.TableName, StringComparison.OrdinalIgnoreCase))).ToList();
                        
                        if (relevantMappings.Any())
                        {
                            sb.AppendLine("      REQUIRED MAPPING COLUMNS (must include in SELECT):");
                            foreach (var mapping in relevantMappings)
                            {
                                if (mapping.SourceDatabase.Equals(schema.DatabaseName, StringComparison.OrdinalIgnoreCase))
                                {
                                    sb.AppendLine($"        • {mapping.SourceColumn}");
                                }
                                else
                                {
                                    sb.AppendLine($"        • {mapping.TargetColumn}");
                                }
                            }
                        }
                        
                        if (table.ForeignKeys != null && table.ForeignKeys.Any())
                        {
                            sb.AppendLine("      FOREIGN KEY RELATIONSHIPS:");
                            foreach (var fk in table.ForeignKeys)
                            {
                                sb.AppendLine($"        • {fk.ColumnName} → {fk.ReferencedTable}.{fk.ReferencedColumn}");
                            }
                        }
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string BuildUserMessage(string userQuery, QueryIntent queryIntent, Dictionary<string, DatabaseSchemaInfo> schemas, Dictionary<string, ISqlDialectStrategy> strategies)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("🚨 MULTI-DATABASE SQL GENERATION TASK 🚨");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"USER QUERY: \"{userQuery}\"");
            sb.AppendLine();
            sb.AppendLine($"TASK: Generate {queryIntent.DatabaseQueries.Count} SEQUENTIAL SQL queries");
            sb.AppendLine();
            
            var allMappings = GetAllCrossDatabaseMappings();
            if (allMappings.Any())
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("🔗 CROSS-DATABASE EXECUTION FLOW (READ CAREFULLY!)");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine();
                sb.AppendLine("⚠️ These mappings define SEQUENTIAL execution:");
                foreach (var mapping in allMappings)
                {
                    sb.AppendLine($"  {mapping.SourceDatabase}.{mapping.SourceColumn} → {mapping.TargetDatabase}.{mapping.TargetColumn}");
                }
                sb.AppendLine();
                sb.AppendLine("🎯 EXECUTION PATTERN:");
                sb.AppendLine("  1. Query source database → Get values from source column");
                sb.AppendLine("  2. System injects values into target database query");
                sb.AppendLine("  3. Target query uses: WHERE target_column IN (value1, value2, ...)");
                sb.AppendLine();
            }
            
            sb.AppendLine("⚠️ EXECUTION ORDER MATTERS:");
            sb.AppendLine("  → Queries will execute in the order you write them");
            sb.AppendLine("  → Later queries can use values from earlier queries");
            sb.AppendLine("  → Check FOREIGN KEY RELATIONSHIPS in SYSTEM message for JOIN logic");
            sb.AppendLine();
            sb.AppendLine("⚠️⚠️ CRITICAL: JOIN PATH RULES ⚠️⚠️");
            sb.AppendLine("  → When joining tables, you MUST follow the FOREIGN KEY RELATIONSHIPS chain");
            sb.AppendLine("  → Example: If TableA → TableB → TableC (via FKs), you CANNOT skip TableB!");
            sb.AppendLine("  → You MUST join: TableA JOIN TableB ON ... JOIN TableC ON ...");
            sb.AppendLine("  → NEVER skip intermediate tables in the FK chain!");
            sb.AppendLine();
            sb.AppendLine("  → For queries asking for quantity/stock/amount/total:");
            sb.AppendLine("    1. Find the classification/master table (MASTER or LOOKUP type)");
            sb.AppendLine("    2. JOIN to the related/detail table (via foreign key)");
            sb.AppendLine("    3. JOIN to the transaction/quantity table (via foreign key from detail)");
            sb.AppendLine("    4. Use SUM(NumericColumn) to calculate total");
            sb.AppendLine("  → Example structure: TableA (MASTER) → TableB (LOOKUP) → TableC (TRANSACTIONAL)");
            sb.AppendLine("    SELECT T1.ColumnX, COUNT(T2.ColumnY), SUM(T3.ColumnZ) FROM TableA T1");
            sb.AppendLine("    JOIN TableB T2 ON T1.ForeignKeyA = T2.PrimaryKeyB JOIN TableC T3 ON T2.ForeignKeyB = T3.PrimaryKeyC");
            sb.AppendLine();
            sb.AppendLine("⚠️⚠️ CRITICAL: SINGLE DATABASE vs MULTI-DATABASE RULES ⚠️⚠️");
            sb.AppendLine();
            sb.AppendLine("RULE A: If ALL required tables are in the SAME database:");
            sb.AppendLine("  → Create ONE query using JOINs (NOT multiple queries!)");
            sb.AppendLine("  → Include ALL necessary tables via foreign key relationships");
            sb.AppendLine("  → Example: TableA + TableB + TableC (all in Database1)");
            sb.AppendLine("    ✓ CORRECT: SELECT T1.ColumnX, COUNT(T2.ColumnY), SUM(T3.ColumnZ) FROM TableA T1 JOIN TableB T2 ... JOIN TableC T3 ...");
            sb.AppendLine("    ✗ WRONG: Create separate queries for same database");
            sb.AppendLine();
            sb.AppendLine("RULE B: If tables are in DIFFERENT databases (cross-database mapping exists):");
            sb.AppendLine("  → Create MULTIPLE queries (one per database)");
            sb.AppendLine("  → FIRST query (priority: 1): Perform aggregation (COUNT, SUM, GROUP BY, ORDER BY)");
            sb.AppendLine("  → SECOND+ queries (priority: 2+): NO aggregation! Just SELECT descriptive columns");
            sb.AppendLine("  → Example:");
            sb.AppendLine("    Query 1: SELECT TOP 5 KeyColumn, COUNT(*) AS Total ... GROUP BY KeyColumn ORDER BY Total DESC");
            sb.AppendLine("    Query 2: SELECT KeyColumn, DescriptiveColumn FROM ... WHERE KeyColumn IN (...)");
            sb.AppendLine("  → ✗ NEVER do: SELECT DescriptiveColumn, COUNT(*) ... GROUP BY in second query!");
            sb.AppendLine("  → ✓ ALWAYS: SELECT KeyColumn, DescriptiveColumn ... WHERE KeyColumn IN (...)");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("🚨 CRITICAL RULE #1: DATABASE ISOLATION");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("Each database is COMPLETELY ISOLATED:");
            sb.AppendLine("  ✗ NEVER reference tables from one database in another database's query");
            sb.AppendLine("  ✗ NEVER write: OtherDatabaseName.SchemaName.TableName");
            sb.AppendLine("  ✗ NEVER write: [OtherDatabaseName].[SchemaName].[TableName]");
            sb.AppendLine("  ✗ NEVER use subqueries that reference other databases");
            sb.AppendLine();
            sb.AppendLine("  ✓ Each query uses ONLY tables from its own database (see SYSTEM message)");
            sb.AppendLine("  ✓ Use literal values (1, 5, 10) instead of cross-database subqueries");
            sb.AppendLine();
            
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("🚨 CRITICAL RULE #2: SQL DIALECT RULES");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            for (int i = 0; i < queryIntent.DatabaseQueries.Count; i++)
            {
                var dbQuery = queryIntent.DatabaseQueries[i];
                var schema = schemas[dbQuery.DatabaseId];
                var strategy = strategies[dbQuery.DatabaseId];
                
                sb.AppendLine($"DATABASE {i + 1} ({schema.DatabaseName}) - {strategy.DatabaseType}:");
                
                if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.SqlServer)
                {
                    sb.AppendLine("  → Use TOP N: SELECT TOP 5 ... (immediately after SELECT)");
                    sb.AppendLine("  ✗ NEVER use LIMIT - causes SYNTAX ERROR!");
                }
                else if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
                {
                    sb.AppendLine("  → Use LIMIT N: ... ORDER BY ... LIMIT 5 (at the end)");
                    sb.AppendLine("  → Use double quotes: \"SchemaName\".\"TableName\", \"ColumnName\"");
                    sb.AppendLine("  ✗ NEVER use TOP - causes SYNTAX ERROR!");
                }
                else
                {
                    sb.AppendLine("  → Use LIMIT N: ... ORDER BY ... LIMIT 5 (at the end)");
                    sb.AppendLine("  ✗ NEVER use TOP - causes SYNTAX ERROR!");
                }
                sb.AppendLine();
            }
            
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("🚨 CRITICAL RULE #3: OUTPUT FORMAT");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"⚠️⚠️ CRITICAL: You MUST generate SQL for EXACTLY {queryIntent.DatabaseQueries.Count} database(s) - NO MORE, NO LESS!");
            sb.AppendLine();
            sb.AppendLine("❌ FORBIDDEN:");
            sb.AppendLine("  • No markdown code blocks (```sql or ```)");
            sb.AppendLine("  • No markdown headers (### or ##)");
            sb.AppendLine("  • No explanations or notes after SQL");
            sb.AppendLine("  • No TEXT placeholders (VALUE1, VALUE2, [values from previous database results])");
            sb.AppendLine("  • For sequential queries: Use NUMERIC placeholder values (101, 205, 387)");
            sb.AppendLine("  • SQL must be EXECUTABLE AS-IS");
            sb.AppendLine($"  • Do NOT create SQL for databases not listed below (ONLY {queryIntent.DatabaseQueries.Count} database(s) required)");
            sb.AppendLine();
            sb.AppendLine($"✓ REQUIRED FORMAT (EXACTLY {queryIntent.DatabaseQueries.Count} database(s)):");
            sb.AppendLine();
            for (int i = 0; i < queryIntent.DatabaseQueries.Count; i++)
            {
                var dbQuery = queryIntent.DatabaseQueries[i];
                var schema = schemas[dbQuery.DatabaseId];
                sb.AppendLine($"DATABASE {i + 1}: {schema.DatabaseName}");
                sb.AppendLine("CONFIRMED");
                sb.AppendLine("SELECT [actual columns] FROM [actual tables] WHERE [actual conditions];");
                sb.AppendLine();
            }
            
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("EXAMPLES");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("✓ CORRECT SEQUENTIAL EXECUTION EXAMPLE:");
            sb.AppendLine();
            sb.AppendLine("Scenario: Get top 5 items from Database1, then get details from Database2");
            sb.AppendLine("Mapping: Database1.JoinColumnID → Database2.TargetColumnID");
            sb.AppendLine();
            sb.AppendLine("DATABASE 1: FirstDatabase (EXECUTES FIRST)");
            sb.AppendLine("CONFIRMED");
            sb.AppendLine("SELECT TOP 5 T1.JoinColumnID, COUNT(*) AS CountValue FROM SchemaName.TableA T1 GROUP BY T1.JoinColumnID ORDER BY CountValue DESC;");
            sb.AppendLine("→ This returns JoinColumnID values: 101, 205, 387, 412, 598");
            sb.AppendLine();
            sb.AppendLine("DATABASE 2: SecondDatabase (EXECUTES SECOND, uses values from DATABASE 1)");
            sb.AppendLine("CONFIRMED");
            sb.AppendLine("SELECT \"TargetColumnID\", \"ColumnX\", \"ColumnY\" FROM \"SchemaName\".\"TableB\" WHERE \"TargetColumnID\" IN (101, 205, 387, 412, 598) LIMIT 5;");
            sb.AppendLine();
            sb.AppendLine("💡 IMPORTANT: The numbers (101, 205, 387) are EXAMPLE placeholders!");
            sb.AppendLine("  → System will AUTOMATICALLY replace them with real values from DATABASE 1");
            sb.AppendLine("  → After DATABASE 1 executes, system extracts JoinColumnID values");
            sb.AppendLine("  → System rewrites DATABASE 2 query with those real values");
            sb.AppendLine("  → Example: (101, 205, 387) becomes (4515, 15994, 12569) from actual results");
            sb.AppendLine();
            sb.AppendLine("🎯 KEY POINTS:");
            sb.AppendLine("  • DATABASE 1: Must SELECT the mapping column (JoinColumnID)");
            sb.AppendLine("  • DATABASE 2: Use IN clause with NUMERIC placeholder values");
            sb.AppendLine("  • Numbers are EXAMPLES ONLY - system replaces them automatically");
            sb.AppendLine("  • Use realistic-looking IDs (101, 205, 387) not sequential (1, 2, 3)");
            sb.AppendLine();
            sb.AppendLine("✗ WRONG (cross-database reference):");
            sb.AppendLine("DATABASE 2: SecondDatabase");
            sb.AppendLine("CONFIRMED");
            sb.AppendLine("SELECT * FROM FirstDatabase.SchemaName.TableA;  -- ✗ FORBIDDEN!");
            sb.AppendLine();
            sb.AppendLine("✗ WRONG (text placeholder):");
            sb.AppendLine("WHERE \"JoinColumnID\" IN (VALUE1, VALUE2, VALUE3);  -- ✗ FORBIDDEN! Use numeric: (101, 205, 387)");
            sb.AppendLine();
            sb.AppendLine("✗ WRONG (bracket placeholder):");
            sb.AppendLine("WHERE \"JoinColumnID\" IN ([values from previous database results]);  -- ✗ FORBIDDEN!");
            sb.AppendLine();
            
            sb.AppendLine("NOW GENERATE SQL FOR ALL DATABASES LISTED IN SYSTEM MESSAGE.");

            return sb.ToString();
        }
    }
}
