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
        /// Language-agnostic: Works with any language by filtering based on length and numeric patterns.
        /// </summary>
        private List<string> ExtractFilterKeywords(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<string>();

            var words = query.Split(new[] { ' ', ',', '.', '?', '!', ';', ':', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

            return words
                .Where(w =>
                    w.Length > 2 &&
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
            sb.AppendLine("  🚨🚨🚨 CRITICAL: If you see 'SchemaName.TableName' in SYSTEM message for Database X,");
            sb.AppendLine("     → That table EXISTS ONLY in Database X");
            sb.AppendLine("     → If you're writing SQL for Database Y, that table DOES NOT EXIST there!");
            sb.AppendLine("     → Example: If SYSTEM shows 'SchemaA.TableA' for Database1,");
            sb.AppendLine("       → You CANNOT use 'SchemaA.TableA' when writing SQL for Database2!");
            sb.AppendLine("       → ✗ WRONG: SELECT ... FROM SchemaA.TableA (if writing for Database2)");
            sb.AppendLine("       → ✓ CORRECT: Use ONLY tables listed for Database2");
            sb.AppendLine();
            sb.AppendLine("✓✓✓ CORRECT APPROACH:");
            sb.AppendLine("  → Each database query uses ONLY tables from that specific database");
            sb.AppendLine("  → Look at the table list for each database below - use ONLY those tables");
            sb.AppendLine("  → If you need data from another database, use literal values or parameters");
            sb.AppendLine("  → FIRST query: SELECT mapping columns + aggregation columns");
            sb.AppendLine("  → SECOND+ queries: Use WHERE column IN (1, 2, 3) with numeric placeholders");
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
                sb.AppendLine("  → 🚨🚨🚨 CRITICAL: If you see only 1 database listed, generate ONLY 1 SQL query!");
                sb.AppendLine("  → 🚨🚨🚨 CRITICAL: Do NOT create DATABASE 1 and DATABASE 2 blocks if only 1 database exists!");
                sb.AppendLine("  → 🚨🚨🚨 CRITICAL: Single database = Single SQL query with JOINs, NOT multiple queries!");
                sb.AppendLine();
                sb.AppendLine("✓ CORRECT: One SQL query with JOINs");
                sb.AppendLine("✗ WRONG: Multiple SQL queries for same database");
                sb.AppendLine("✗ WRONG: DATABASE 1: DatabaseName ... DATABASE 2: SameDatabaseName (if only 1 database!)");
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
                sb.AppendLine($"This is DATABASE #{i + 1}");
                sb.AppendLine($"You are writing SQL query FOR THIS DATABASE ONLY!");
                sb.AppendLine();
                sb.AppendLine("✗✗✗✗✗ FORBIDDEN - NEVER DO THESE IN SQL FOR THIS DATABASE: ✗✗✗✗✗");
                sb.AppendLine($"  1. ✗ NEVER write: OtherDatabaseName.SchemaName.TableName");
                sb.AppendLine($"  2. ✗ NEVER write: [OtherDatabaseName].[SchemaName].[TableName]");
                sb.AppendLine($"  3. ✗ NEVER write: SELECT * FROM OtherDatabase.Schema.Table");
                sb.AppendLine($"  4. ✗ NEVER write: WHERE Column IN (SELECT Column FROM OtherDatabase.Table)");
                sb.AppendLine($"  5. ✗ NEVER reference ANY table that belongs to ANOTHER database");
                sb.AppendLine($"  6. ✗ If you see multiple databases listed → They are ALL COMPLETELY SEPARATE!");
                sb.AppendLine($"  7. ✗ For this database query → ONLY use this database's tables!");
                sb.AppendLine();
                sb.AppendLine($"✓✓✓✓✓ ALLOWED - ONLY THESE TABLES EXIST IN THIS DATABASE: ✓✓✓✓✓");
            foreach (var tableName in dbQuery.RequiredTables)
            {
                    sb.AppendLine($"  ✓ {tableName}");
                }
                    sb.AppendLine();
                sb.AppendLine("🚨🚨🚨 REMEMBER: 🚨🚨🚨");
                sb.AppendLine($"  → When writing SQL for DATABASE #{i + 1}");
                sb.AppendLine($"  → Look at the table list ABOVE");
                sb.AppendLine($"  → Use ONLY those tables in your SQL");
                sb.AppendLine($"  → If a table name is NOT in the list above, it DOES NOT EXIST in this database!");
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
                    sb.AppendLine("  ✗✗✗ NEVER use LIMIT in SQL Server queries - it will cause SYNTAX ERROR!");
                    sb.AppendLine("  ✓ CORRECT: SELECT TOP 5 ... FROM ... ORDER BY ...");
                    sb.AppendLine("  ✗ WRONG: SELECT ... FROM ... ORDER BY ... LIMIT 5  -- SYNTAX ERROR!");
                    sb.AppendLine("  ✗ WRONG: SELECT ... FROM ... GROUP BY ... ORDER BY ... LIMIT 5  -- SYNTAX ERROR!");
                    sb.AppendLine("  → TOP N MUST be immediately after SELECT keyword");
                    sb.AppendLine("  → Example: SELECT TOP 5 ColumnName FROM TableName ORDER BY ColumnName DESC");
                    sb.AppendLine("  → Example: SELECT TOP 5 GroupingColumn, COUNT(...) FROM ... GROUP BY GroupingColumn ORDER BY COUNT(...) DESC");
                    sb.AppendLine("  → 🚨🚨🚨 REMEMBER: SQL Server = TOP N (after SELECT), NOT LIMIT N (after ORDER BY)!");
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
                            (m.SourceTable.Equals(table.TableName, StringComparison.OrdinalIgnoreCase) ||
                             m.TargetTable.Equals(table.TableName, StringComparison.OrdinalIgnoreCase))).ToList();
                        
                        if (relevantMappings.Any())
                        {
                            sb.AppendLine("   🚨 REQUIRED MAPPING COLUMNS (MUST include in SELECT):");
                            foreach (var mapping in relevantMappings)
                            {
                                if (mapping.SourceTable.Equals(table.TableName, StringComparison.OrdinalIgnoreCase))
                                {
                                    sb.AppendLine($"     • {mapping.SourceColumn} (maps to TargetDatabase.{mapping.TargetColumn})");
                                }
                                else
                                {
                                    sb.AppendLine($"     • {mapping.TargetColumn} (maps from SourceDatabase.{mapping.SourceColumn})");
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
            sb.AppendLine("  → ✓ CORRECT: WHERE column IN (1, 2, 3)  -- Example numeric values, system replaces with real IDs");
            sb.AppendLine("  → ✗ WRONG: WHERE column IN (VALUE1, VALUE2, VALUE3)  -- Text causes SQL error!");
            sb.AppendLine("  → ✗ WRONG: WHERE column IN ([values from previous database results])  -- Bracket causes SQL error!");
            sb.AppendLine("  → Use actual column/table names from the schema above");
            sb.AppendLine();
            sb.AppendLine("✓✓✓ REQUIRED: Use EXACTLY this format (copy-paste this structure):");
            sb.AppendLine("MANDATORY: Each block MUST have: 1) 'DATABASE N: <Name>' (exact name from DATABASE # above), 2) 'CONFIRMED', 3) SQL. Parser cannot extract without these lines.");
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

        public SqlPromptParts BuildMultiDatabaseSeparated(string userQuery, QueryIntent queryIntent, Dictionary<string, DatabaseSchemaInfo> schemas, Dictionary<string, ISqlDialectStrategy> strategies, Dictionary<string, List<Entities.DocumentChunk>> schemaChunksMap = null, Dictionary<string, List<string>> requiredMappingColumns = null)
        {
            if (queryIntent == null || queryIntent.DatabaseQueries == null || queryIntent.DatabaseQueries.Count == 0)
                throw new ArgumentException("QueryIntent must contain at least one database query", nameof(queryIntent));

            var systemMessage = BuildSystemMessage(queryIntent, schemas, strategies, schemaChunksMap, requiredMappingColumns);
            var userMessage = BuildUserMessage(userQuery, queryIntent, schemas, strategies);

            return new SqlPromptParts
            {
                SystemMessage = systemMessage,
                UserMessage = userMessage
            };
        }

        private string BuildSystemMessage(QueryIntent queryIntent, Dictionary<string, DatabaseSchemaInfo> schemas, Dictionary<string, ISqlDialectStrategy> strategies, Dictionary<string, List<Entities.DocumentChunk>> schemaChunksMap = null, Dictionary<string, List<string>> requiredMappingColumns = null)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"DATABASE SCHEMA INFORMATION ({queryIntent.DatabaseQueries.Count} database(s))");
            sb.AppendLine();
            sb.AppendLine("⚠️⚠️⚠️ SEMANTIC KEYWORDS - LANGUAGE BRIDGE ⚠️⚠️⚠️");
            sb.AppendLine("  → Each table has 'Semantic Keywords' extracted from table/column names");
            sb.AppendLine("  → These keywords help match user queries in ANY language to schema elements");
            sb.AppendLine("  → Example: Table 'TableNameA' has keywords: keyword1, keyword2, keyword3");
            sb.AppendLine("  → User query in any language can match keywords → corresponding table");
            sb.AppendLine("  → Use semantic keywords to understand which tables/columns match the user's intent");
            sb.AppendLine();

            var allMappings = GetAllCrossDatabaseMappings();
            if (allMappings.Any())
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("🔗 CROSS-DATABASE MAPPINGS (SEQUENTIAL EXECUTION)");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                foreach (var mapping in allMappings)
                {
                    sb.AppendLine($"  {mapping.SourceDatabase}.{mapping.SourceColumn} → {mapping.TargetDatabase}.{mapping.TargetColumn}");
                }
                sb.AppendLine();
                sb.AppendLine("🚨 CRITICAL RULES:");
                sb.AppendLine("  → FIRST query (priority 1): MUST include mapping column in SELECT (e.g., SELECT ColumnName, COUNT(*) ...)");
                sb.AppendLine("  → SECOND+ queries (priority 2+): Use numeric placeholders WHERE column IN (1, 2, 3)");
                sb.AppendLine("  → SYSTEM will automatically replace placeholders (1, 2, 3) with real values from first query results");
                sb.AppendLine("  → Do NOT use actual values - use placeholders, system replaces them");
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
                
                if (requiredMappingColumns != null && requiredMappingColumns.TryGetValue(dbQuery.DatabaseId, out var mappingCols) && mappingCols != null && mappingCols.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("  🚨🚨🚨🚨🚨 MAPPING COLUMNS REQUIRED - MUST INCLUDE IN SELECT 🚨🚨🚨🚨🚨");
                    sb.AppendLine("  🚨🚨🚨🚨🚨 IF YOU DON'T INCLUDE THESE, QUERY WILL FAIL! 🚨🚨🚨🚨🚨");
                    foreach (var col in mappingCols)
                    {
                        sb.AppendLine($"    • {col}");
                    }
                    sb.AppendLine("  → These columns are CRITICAL to link results between databases");
                    sb.AppendLine("  → You MUST include ALL of them in your SELECT clause");
                    sb.AppendLine("  → If you skip even one, the cross-database query will fail");
                    sb.AppendLine();
                }
                
                if (schemaChunksMap != null && schemaChunksMap.TryGetValue(dbQuery.DatabaseId, out var relevantChunks) && relevantChunks != null && relevantChunks.Count > 0)
                {
                    sb.AppendLine($"  Tables ({relevantChunks.Count} table(s) from schema chunks):");
                    sb.AppendLine();
                    
                    foreach (var chunk in relevantChunks.OrderBy(c => c.ChunkIndex))
                    {
                        sb.AppendLine(chunk.Content);
                        sb.AppendLine();
                    }
                    
                    var chunkTableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var chunk in relevantChunks)
                    {
                        var tableName = ExtractTableNameFromChunkContent(chunk.Content);
                        if (!string.IsNullOrWhiteSpace(tableName))
                        {
                            chunkTableNames.Add(tableName);
                        }
                    }
                    
                    foreach (var tableName in dbQuery.RequiredTables)
                    {
                        if (!chunkTableNames.Contains(tableName))
                        {
                            var table = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                            if (table != null)
                            {
                                sb.AppendLine($"    • {table.TableName} (required but not in search results)");
                                if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
                                {
                                    sb.AppendLine($"      Columns: {string.Join(", ", table.Columns.Select(c => QuotePostgreSqlIdentifier(c.ColumnName)))}");
                                }
                                else
                                {
                                    sb.AppendLine($"      Columns: {string.Join(", ", table.Columns.Select(c => c.ColumnName))}");
                                }
                                
                                if (table.ForeignKeys != null && table.ForeignKeys.Any())
                                {
                                    sb.AppendLine("      FOREIGN KEY RELATIONSHIPS:");
                                    foreach (var fk in table.ForeignKeys)
                                    {
                                        sb.AppendLine($"        • {fk.ColumnName} → {fk.ReferencedTable}.{fk.ReferencedColumn}");
                                    }
                                }
                                sb.AppendLine();
                            }
                        }
                    }
                }
                else
                {
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
                                (m.SourceTable.Equals(table.TableName, StringComparison.OrdinalIgnoreCase) ||
                                 m.TargetTable.Equals(table.TableName, StringComparison.OrdinalIgnoreCase))).ToList();
                            
                            if (relevantMappings.Any())
                            {
                                sb.AppendLine("      REQUIRED MAPPING COLUMNS (must include in SELECT):");
                                foreach (var mapping in relevantMappings)
                                {
                                    if (mapping.SourceTable.Equals(table.TableName, StringComparison.OrdinalIgnoreCase))
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
            }

            return sb.ToString();
        }

        private static string ExtractTableNameFromChunkContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith("Table:", StringComparison.OrdinalIgnoreCase))
                {
                    var tableName = line.Substring("Table:".Length).Trim();
                    return tableName;
                }
            }

            return string.Empty;
        }

        private string BuildUserMessage(string userQuery, QueryIntent queryIntent, Dictionary<string, DatabaseSchemaInfo> schemas, Dictionary<string, ISqlDialectStrategy> strategies)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"USER QUERY: \"{userQuery}\"");
            sb.AppendLine($"TASK: Generate {queryIntent.DatabaseQueries.Count} SQL query/queries");
            sb.AppendLine();
            sb.AppendLine("CRITICAL RULES:");
            if (queryIntent.DatabaseQueries.Count == 1)
            {
                sb.AppendLine("  1. DATABASE ISOLATION: Single database - use ONLY tables from SYSTEM message, use JOINs within same database");
            }
            else
            {
                sb.AppendLine("  1. DATABASE ISOLATION: NEVER use DatabaseName.SchemaName.TableName format!");
                sb.AppendLine("     ✓ CORRECT: FROM SchemaName.TableName (same database)");
                sb.AppendLine("     ✗ WRONG: FROM OtherDatabaseName.SchemaName.TableName (cross-database reference)");
            }
            sb.AppendLine("  2. JOIN PATH: Follow FK chain (TableA → TableB → TableC), never skip tables");
            sb.AppendLine("     → 🚨🚨🚨 CRITICAL: JOIN order matters! Follow the foreign key relationships!");
            sb.AppendLine("     → 🚨🚨🚨 CRITICAL: If you need ColumnX from TableC, you MUST JOIN through TableB first!");
            sb.AppendLine("     → Example: To get ColumnX from TableC, you need: TableA T1 JOIN TableB T2 ON T1.FK = T2.PK JOIN TableC T3 ON T2.FK = T3.PK");
            sb.AppendLine("     → ✗ WRONG: FROM TableA T1 JOIN TableC T3 ON ... (skipping TableB breaks FK chain!)");
            sb.AppendLine("     → ✗ WRONG: FROM TableB T1 JOIN TableC T2 ON T1.ColumnX = T2.ColumnY (if ColumnX is not FK in TableB!)");
            sb.AppendLine("     → ✓ CORRECT: Check SYSTEM message for FK relationships, follow the chain step by step");
            sb.AppendLine("  3. TABLE ALIASES & COLUMN VERIFICATION:");
            sb.AppendLine("     → Use T1, T2, T3 consistently");
            sb.AppendLine("     → 🚨🚨🚨 CRITICAL: Before using T1.ColumnName, verify ColumnName exists in T1's table!");
            sb.AppendLine("     → 🚨🚨🚨 CRITICAL: Track alias-to-table mapping as you write JOINs!");
            sb.AppendLine("       Example: FROM TableA T1 JOIN TableB T2 ON T1.FK = T2.PK");
            sb.AppendLine("       → T1 = TableA → Check SYSTEM message: TableA has columns: Col1, Col2, Col3");
            sb.AppendLine("       → T2 = TableB → Check SYSTEM message: TableB has columns: Col4, Col5, Col6");
            sb.AppendLine("       → Use T1.Col1, T1.Col2, T2.Col4, T2.Col5");
            sb.AppendLine("       → ✗ WRONG: T1.Col4 (Col4 is in TableB, not TableA)");
            sb.AppendLine("       → ✗ WRONG: T2.Col1 (Col1 is in TableA, not TableB)");
            sb.AppendLine("     → 🚨🚨🚨 CRITICAL: Check SYSTEM message: Each table shows its columns - use ONLY columns from that table!");
            sb.AppendLine("     → 🚨🚨🚨 CRITICAL: If T1 = TableA and ColumnX is in TableB, you CANNOT use T1.ColumnX!");
            sb.AppendLine("     → Example: If T2 = TableB, T2.ColumnX is WRONG if ColumnX exists only in T3 = TableC");
            sb.AppendLine("     → 🚨🚨🚨 CRITICAL: If you need ColumnX from TableC, you MUST use T3.ColumnX (where T3 = TableC)!");
            sb.AppendLine("     → PostgreSQL: Alias WITHOUT quotes (T1), column WITH quotes (T1.\"ColumnName\")");
            sb.AppendLine("     → ✗ WRONG PostgreSQL: \"T1\".\"ColumnName\" FROM \"Schema\".\"Table\" \"T1\" (alias in quotes fails!)");
            sb.AppendLine("     → ✓ CORRECT PostgreSQL: T1.\"ColumnName\" FROM \"Schema\".\"Table\" T1 WHERE T1.\"ID\" IN (1, 2, 3)");
            sb.AppendLine("     → PostgreSQL CRITICAL: Alias NEVER in quotes - T1, T2, T3 (NOT \"T1\", \"T2\", \"T3\")");
            sb.AppendLine("     → PostgreSQL CRITICAL: Column ALWAYS in quotes when using alias - T1.\"ColumnName\" (NOT T1.ColumnName)");
            sb.AppendLine("  4. COUNT() & GROUP BY - CRITICAL RULES:");
            sb.AppendLine("     → COUNT(): Use COUNT(DISTINCT ColumnName) to count unique records");
            sb.AppendLine("     → COUNT(): Use column from the table you're counting (COUNT(T3.PrimaryKeyColumn) where T3=DetailTable)");
            sb.AppendLine("     → GROUP BY: Group by the AGGREGATION LEVEL, NOT individual detail records!");
            sb.AppendLine("       🚨🚨🚨 CRITICAL: If SELECT includes COUNT(DISTINCT DetailKeyColumn), GROUP BY must be ONLY GroupingColumn!");
            sb.AppendLine("       🚨🚨🚨 CRITICAL: If SELECT includes DetailKeyColumn, DO NOT include it in GROUP BY!");
            sb.AppendLine("       🚨🚨🚨 CRITICAL: If query asks 'which grouping has most detail records', DO NOT SELECT DetailKeyColumn!");
            sb.AppendLine("       🚨🚨🚨 CRITICAL: SELECT should ONLY have: GroupingColumn, COUNT(DISTINCT DetailKeyColumn), SUM/AVG/MAX/MIN(...)");
            sb.AppendLine("       🚨🚨🚨 CRITICAL: NEVER SELECT DetailKeyColumn when grouping by GroupingColumn!");
            sb.AppendLine("       🚨🚨🚨 CRITICAL: MAPPING COLUMNS in GROUP BY:");
            sb.AppendLine("         → Mapping columns (for cross-database joins) MUST be in SELECT");
            sb.AppendLine("         → 🚨🚨🚨 CRITICAL: Mapping columns should ONLY be in GROUP BY if they ARE the grouping level!");
            sb.AppendLine("         → 🚨🚨🚨 CRITICAL: If mapping column is NOT the grouping level, it MUST NOT be in GROUP BY!");
            sb.AppendLine("         → 🚨 RULE: GROUP BY should contain ONLY columns that define the aggregation level");
            sb.AppendLine("         → 🚨 RULE: If query asks 'which GroupingLevel has most X', GROUP BY should be ONLY GroupingLevel column");
            sb.AppendLine("         → 🚨 RULE: Do NOT add mapping columns to GROUP BY just because they're in SELECT!");
            sb.AppendLine("         → Example 1: Query 'which grouping level has most detail records'");
            sb.AppendLine("           → Grouping level: GroupingColumn");
            sb.AppendLine("           → ✓ CORRECT: SELECT GroupingColumn, COUNT(...) GROUP BY GroupingColumn");
            sb.AppendLine("           → ✗ WRONG: SELECT GroupingColumn, MappingColumn, COUNT(...) GROUP BY GroupingColumn, MappingColumn (MappingColumn not grouping level!)");
            sb.AppendLine("         → Example 2: Query 'which grouping level has most detail records' (with cross-database join)");
            sb.AppendLine("           → Grouping level: GroupingColumn");
            sb.AppendLine("           → Mapping column: MappingColumn (needed for join to other database, but NOT grouping level)");
            sb.AppendLine("           → ✓ CORRECT: SELECT GroupingColumn, MappingColumn, COUNT(...) GROUP BY GroupingColumn (MappingColumn in SELECT for join, NOT in GROUP BY)");
            sb.AppendLine("           → ✗ WRONG: SELECT GroupingColumn, MappingColumn, COUNT(...) GROUP BY GroupingColumn, MappingColumn (MappingColumn creates extra grouping!)");
            sb.AppendLine("           → ✗ WRONG: SELECT GroupingColumn, MappingColumn, OtherColumn, COUNT(...) GROUP BY GroupingColumn, MappingColumn, OtherColumn (if OtherColumn not grouping level!)");
            sb.AppendLine("         → 🚨 EXCEPTION: If MappingColumn IS the GroupingColumn, then GROUP BY MappingColumn");
            sb.AppendLine("         → 🚨🚨🚨 REMEMBER: GROUP BY should have ONLY the columns that define what you're grouping by!");
            sb.AppendLine("         → 🚨🚨🚨 REMEMBER: If query asks 'which X has most Y', GROUP BY should be ONLY X (not X, Z, W)!");
            sb.AppendLine("       Example: If query asks about grouping level (e.g., 'which grouping has most detail records'):");
            sb.AppendLine("         → SELECT GroupingColumn, COUNT(DISTINCT DetailKeyColumn), SUM(AmountColumn)");
            sb.AppendLine("         → GROUP BY GroupingColumn (NOT GroupingColumn, DetailKeyColumn)");
            sb.AppendLine("         → COUNT(DISTINCT DetailKeyColumn) counts detail records per grouping");
            sb.AppendLine("         → ✗ WRONG: SELECT GroupingColumn, DetailKeyColumn, COUNT(...) GROUP BY GroupingColumn, DetailKeyColumn");
            sb.AppendLine("         → ✗ WRONG: SELECT GroupingColumn, DetailKeyColumn, COUNT(DISTINCT DetailKeyColumn) GROUP BY GroupingColumn, DetailKeyColumn");
            sb.AppendLine("         → ✗ WRONG: SELECT GroupingColumn, COUNT(DISTINCT DetailKeyColumn), DetailKeyColumn GROUP BY GroupingColumn, DetailKeyColumn");
            sb.AppendLine("         → ✓ CORRECT: SELECT GroupingColumn, COUNT(DISTINCT DetailKeyColumn), SUM(...) GROUP BY GroupingColumn");
            sb.AppendLine("         → ✓ CORRECT: SELECT GroupingColumn, COUNT(DISTINCT DetailKeyColumn), SUM(...) GROUP BY GroupingColumn (DetailKeyColumn NOT in SELECT, NOT in GROUP BY)");
            sb.AppendLine("       🚨🚨🚨 SPECIFIC EXAMPLE - Grouping level aggregation:");
            sb.AppendLine("         → Query: 'which grouping level has most detail records and total amount'");
            sb.AppendLine("         → ✓ CORRECT: SELECT GroupingColumn, COUNT(DISTINCT DetailKeyColumn), SUM(AmountColumn) GROUP BY GroupingColumn");
            sb.AppendLine("         → ✗ WRONG: SELECT GroupingColumn, DetailKeyColumn, COUNT(DISTINCT DetailKeyColumn), SUM(AmountColumn) GROUP BY GroupingColumn, DetailKeyColumn");
            sb.AppendLine("         → ✗ WRONG: SELECT GroupingColumn, COUNT(DISTINCT DetailKeyColumn), SUM(AmountColumn), DetailKeyColumn GROUP BY GroupingColumn, DetailKeyColumn");
            sb.AppendLine("         → Reason: DetailKeyColumn is detail-level, GroupingColumn is grouping-level. Grouping by DetailKeyColumn creates one row per detail record, not per grouping!");
            sb.AppendLine("     → GROUP BY: If query asks about grouping level, group ONLY by grouping column");
            sb.AppendLine("     → GROUP BY: If query asks about individual detail records, then group by detail identifier");
            sb.AppendLine("     → GROUP BY: Do NOT include detail-level columns (like DetailKeyColumn) when grouping by grouping level!");
            sb.AppendLine("     → GROUP BY: If SELECT has COUNT(DISTINCT DetailKeyColumn), GROUP BY must be ONLY GroupingColumn!");
            sb.AppendLine("     → SELECT: If grouping by GroupingColumn, DO NOT SELECT DetailKeyColumn (only use it in COUNT(DISTINCT DetailKeyColumn))!");
            sb.AppendLine("  5. GROUP BY: Use column from correct level (TopLevel for grouping, SubLevel for sub-grouping)");
            sb.AppendLine("  6. LIMIT/TOP Usage - CRITICAL RULE:");
            sb.AppendLine("     → Use LIMIT/TOP ONLY when user explicitly asks for specific number (e.g., 'first N', 'top N')");
            sb.AppendLine("     → If user asks 'which grouping has most' or 'most X' WITHOUT specifying a number:");
            sb.AppendLine("       → DO NOT use LIMIT 1 or TOP 1!");
            sb.AppendLine("       → Return ALL groupings ordered by count/amount DESC");
            sb.AppendLine("       → Let user see all results, not just the top one");
            sb.AppendLine("     → Example: 'Which grouping has most detail records?' → GROUP BY GroupingColumn ORDER BY COUNT DESC (NO LIMIT)");
            sb.AppendLine("     → Example: 'First 5 groupings' → GROUP BY GroupingColumn ORDER BY COUNT DESC LIMIT 5 (WITH LIMIT)");
            sb.AppendLine("     → ✗ WRONG: 'most X' query with LIMIT 1 (user wants to see all, not just top one)");
            sb.AppendLine("     → ✓ CORRECT: 'most X' query without LIMIT (return all ordered by count DESC)");
            sb.AppendLine("  7. Check SYSTEM message for exact table/column names");
            sb.AppendLine("  8. MAPPING COLUMNS: If SYSTEM message shows 🚨🚨🚨 MAPPING COLUMNS REQUIRED, you MUST include ALL of them in SELECT!");
            sb.AppendLine();
            if (queryIntent.DatabaseQueries.Count == 1)
            {
            sb.AppendLine("⚠️⚠️ SINGLE DATABASE QUERY RULES ⚠️⚠️");
            sb.AppendLine();
            sb.AppendLine("ALL tables are in the same database:");
            sb.AppendLine("  → Create EXACTLY ONE SQL query using JOINs");
            sb.AppendLine("  → Include ALL necessary tables via foreign key relationships from SYSTEM message");
            sb.AppendLine("  → Follow FK chain: TableA → TableB → TableC (all in same database)");
            sb.AppendLine();
            sb.AppendLine("🚨🚨🚨 CRITICAL: JOIN CHAIN & ALIAS TRACKING 🚨🚨🚨");
            sb.AppendLine("  Step 1: Write your JOINs: FROM TableA T1 JOIN TableB T2 ON T1.FK = T2.PK JOIN TableC T3 ON T2.FK = T3.PK");
            sb.AppendLine("  Step 2: Map aliases to tables:");
            sb.AppendLine("    → T1 = TableA → Check SYSTEM message: TableA has columns: ColA1, ColA2, ColA3");
            sb.AppendLine("    → T2 = TableB → Check SYSTEM message: TableB has columns: ColB1, ColB2, ColB3");
            sb.AppendLine("    → T3 = TableC → Check SYSTEM message: TableC has columns: ColC1, ColC2, ColC3");
            sb.AppendLine("  Step 3: Use columns from correct table:");
            sb.AppendLine("    → ✓ CORRECT: T1.ColA1, T1.ColA2, T2.ColB1, T2.ColB2, T3.ColC1, T3.ColC2");
            sb.AppendLine("    → ✗ WRONG: T1.ColB1 (ColB1 is in TableB, not TableA)");
            sb.AppendLine("    → ✗ WRONG: T2.ColC1 (ColC1 is in TableC, not TableB)");
            sb.AppendLine("    → ✗ WRONG: T2.ColA1 (ColA1 is in TableA, not TableB)");
            sb.AppendLine("  Step 4: GROUP BY at correct aggregation level:");
            sb.AppendLine("    → If query asks about grouping level (e.g., 'which grouping has most detail records'): GROUP BY GroupingColumn ONLY");
            sb.AppendLine("    → 🚨🚨🚨 CRITICAL: If SELECT includes COUNT(DISTINCT DetailKeyColumn), GROUP BY must be ONLY GroupingColumn!");
            sb.AppendLine("    → 🚨🚨🚨 CRITICAL: If SELECT includes DetailKeyColumn, DO NOT include it in GROUP BY!");
            sb.AppendLine("    → 🚨🚨🚨 CRITICAL: If query asks 'which grouping has most detail records', DO NOT SELECT DetailKeyColumn!");
            sb.AppendLine("    → 🚨🚨🚨 CRITICAL: SELECT should ONLY have: GroupingColumn, COUNT(DISTINCT DetailKeyColumn), SUM/AVG/MAX/MIN(...)");
            sb.AppendLine("    → 🚨🚨🚨 CRITICAL: NEVER SELECT DetailKeyColumn when grouping by GroupingColumn!");
            sb.AppendLine("    → 🚨🚨🚨 CRITICAL: GROUP BY should contain ONLY columns that define the aggregation level");
            sb.AppendLine("    → 🚨🚨🚨 CRITICAL: Do NOT add unrelated columns to GROUP BY (even if they're in SELECT for JOINs)!");
            sb.AppendLine("    → ✗ WRONG: GROUP BY GroupingColumn, DetailKeyColumn (creates one row per detail record, not per grouping)");
            sb.AppendLine("    → ✗ WRONG: SELECT GroupingColumn, DetailKeyColumn, COUNT(...) GROUP BY GroupingColumn, DetailKeyColumn");
            sb.AppendLine("    → ✗ WRONG: SELECT GroupingColumn, COUNT(DISTINCT DetailKeyColumn), DetailKeyColumn GROUP BY GroupingColumn, DetailKeyColumn");
            sb.AppendLine("    → ✗ WRONG: SELECT GroupingColumn, JoinColumn, COUNT(...) GROUP BY GroupingColumn, JoinColumn (if JoinColumn not grouping level)");
            sb.AppendLine("    → ✓ CORRECT: SELECT GroupingColumn, COUNT(DISTINCT DetailKeyColumn), SUM(...) GROUP BY GroupingColumn");
            sb.AppendLine("    → ✓ CORRECT: SELECT GroupingColumn, COUNT(DISTINCT DetailKeyColumn), SUM(...) GROUP BY GroupingColumn (DetailKeyColumn NOT in SELECT, NOT in GROUP BY)");
            sb.AppendLine("    → COUNT(DISTINCT DetailKeyColumn) counts detail records per grouping");
            sb.AppendLine("    → 🚨🚨🚨 SPECIFIC EXAMPLE - Grouping level aggregation:");
            sb.AppendLine("      → Query: 'which grouping level has most detail records and total amount'");
            sb.AppendLine("      → ✓ CORRECT: SELECT GroupingColumn, COUNT(DISTINCT DetailKeyColumn), SUM(AmountColumn) GROUP BY GroupingColumn");
            sb.AppendLine("      → ✗ WRONG: SELECT GroupingColumn, DetailKeyColumn, COUNT(DISTINCT DetailKeyColumn), SUM(AmountColumn) GROUP BY GroupingColumn, DetailKeyColumn");
            sb.AppendLine("      → ✗ WRONG: SELECT GroupingColumn, COUNT(DISTINCT DetailKeyColumn), SUM(AmountColumn), DetailKeyColumn GROUP BY GroupingColumn, DetailKeyColumn");
            sb.AppendLine("      → Reason: DetailKeyColumn is detail-level, GroupingColumn is grouping-level. Grouping by DetailKeyColumn creates one row per detail record, not per grouping!");
            sb.AppendLine("  → Example: SELECT T1.ColumnX, COUNT(DISTINCT T2.ColumnY), SUM(T3.ColumnZ)");
            sb.AppendLine("            FROM SchemaName.TableA T1");
            sb.AppendLine("            JOIN SchemaName.TableB T2 ON T1.FK = T2.PK");
            sb.AppendLine("            JOIN SchemaName.TableC T3 ON T2.FK = T3.PK");
            sb.AppendLine("            GROUP BY T1.ColumnX");
            sb.AppendLine("  ✗ WRONG: Creating multiple queries for same database");
            sb.AppendLine("  ✗ WRONG: Referencing other databases");
            sb.AppendLine("  ✗ WRONG: Using T2.ColumnName when ColumnName is in T3's table");
            sb.AppendLine("  ✗ WRONG: GROUP BY GroupingColumn, DetailKeyColumn when query asks about grouping level");
            sb.AppendLine();
            }
            else
            {
                sb.AppendLine("⚠️⚠️ MULTI-DATABASE QUERY RULES ⚠️⚠️");
                sb.AppendLine();
                sb.AppendLine("Tables are in DIFFERENT databases:");
                sb.AppendLine("  → Create MULTIPLE queries (one per database)");
                sb.AppendLine("  → FIRST query (priority: 1): Perform aggregation (COUNT, SUM, GROUP BY, ORDER BY)");
                sb.AppendLine("  → SECOND+ queries (priority: 2+): NO aggregation! Just SELECT descriptive columns");
                sb.AppendLine("  → Example:");
                sb.AppendLine("    Query 1: SELECT TOP 5 KeyColumn, COUNT(*) AS Total ... GROUP BY KeyColumn ORDER BY Total DESC");
                sb.AppendLine("    Query 2: SELECT KeyColumn, DescriptiveColumn FROM ... WHERE KeyColumn IN (1, 2, 3)");
                sb.AppendLine();
            }
            
            sb.AppendLine("SQL DIALECT RULES:");
            sb.AppendLine("  → See SYSTEM message for detailed dialect-specific rules (PostgreSQL quotes, SQL Server TOP, etc.)");
            for (int i = 0; i < queryIntent.DatabaseQueries.Count; i++)
            {
                var strategy = strategies[queryIntent.DatabaseQueries[i].DatabaseId];
                if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.SqlServer)
                {
                    sb.AppendLine($"  DB{i + 1}: SQL Server → Use TOP N (not LIMIT)");
                }
                else if (strategy.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
                {
                    sb.AppendLine($"  DB{i + 1}: PostgreSQL → Use LIMIT N, DOUBLE QUOTES for ALL identifiers");
                    sb.AppendLine($"    🚨 ALIAS RULE: Alias WITHOUT quotes, column WITH quotes!");
                    sb.AppendLine($"    🚨🚨🚨 POSTGRESQL ALIAS RULE - CRITICAL! 🚨🚨🚨");
                    sb.AppendLine($"    ✓ CORRECT: SELECT T1.\"ColumnName\" FROM \"SchemaName\".\"TableName\" T1 WHERE T1.\"ID\" IN (1, 2, 3)");
                    sb.AppendLine($"    ✗ WRONG: SELECT \"T1\".\"ColumnName\" FROM \"SchemaName\".\"TableName\" \"T1\"  -- SYNTAX ERROR!");
                    sb.AppendLine($"    ✗ WRONG: SELECT T1.ColumnName FROM \"SchemaName\".\"TableName\" T1  -- Column must be quoted!");
                    sb.AppendLine($"    Rule: Alias WITHOUT quotes (T1), Column WITH quotes (T1.\"ColumnName\")");
                    sb.AppendLine($"    Rule: FROM \"Schema\".\"Table\" T1 (NOT \"T1\")");
                    sb.AppendLine($"    Rule: WHERE T1.\"ID\" IN (...) (NOT \"T1\".\"ID\")");
                    sb.AppendLine($"    Rule: SELECT T1.\"Col1\", T2.\"Col2\" (NOT \"T1\".\"Col1\")");
                }
                else
                {
                    sb.AppendLine($"  DB{i + 1}: {strategy.DatabaseType} → Use LIMIT N (not TOP)");
                }
            }
            sb.AppendLine();
            
            sb.AppendLine("OUTPUT FORMAT:");
            sb.AppendLine("  ❌ No markdown (```sql), no explanations, no text placeholders");
            sb.AppendLine("  ❌ NEVER use DatabaseName.SchemaName.TableName format (cross-database reference)");
            var allMappings = GetAllCrossDatabaseMappings();
            if (allMappings.Any() && queryIntent.DatabaseQueries.Count > 1)
            {
                sb.AppendLine("  ✓ For sequential queries: Use numeric placeholders WHERE column IN (1, 2, 3)");
                sb.AppendLine("  ✓ SYSTEM will replace placeholders with real values from first query - you don't need to know actual values");
            }
            sb.AppendLine($"  ✓ Generate EXACTLY {queryIntent.DatabaseQueries.Count} database(s)");
            sb.AppendLine("  ✓ Use EXACT database names from SYSTEM message (not 'DatabaseName')");
            sb.AppendLine("  ✓ MANDATORY: Each block = 'DATABASE N: <Name>' (from SYSTEM), 'CONFIRMED', then SQL; parser cannot extract without these lines.");
            sb.AppendLine();
            for (int i = 0; i < queryIntent.DatabaseQueries.Count; i++)
            {
                var dbQuery = queryIntent.DatabaseQueries[i];
                var schema = schemas[dbQuery.DatabaseId];
                sb.AppendLine($"DATABASE {i + 1}: {schema.DatabaseName}");
                sb.AppendLine("CONFIRMED");
                sb.AppendLine("SELECT [columns] FROM [SchemaName].[TableName] WHERE [conditions];");
                sb.AppendLine("  → Use SchemaName.TableName (NOT DatabaseName.SchemaName.TableName)");
                sb.AppendLine();
            }
            
            sb.AppendLine("FINAL CHECKLIST:");
            sb.AppendLine("  ✓ Column exists in table (check SYSTEM message - verify T1.ColumnName means ColumnName is in T1's table)");
            sb.AppendLine("  ✓ JOIN chain follows FK relationships (TableA → TableB → TableC, never skip tables)");
            sb.AppendLine("  ✓ JOIN uses correct columns (if ColumnX is in TableC, use T3.ColumnX, not T1.ColumnX or T2.ColumnX)");
            sb.AppendLine("  ✓ Alias-to-table mapping tracked (T1=TableA, T2=TableB, T3=TableC - verify each alias's columns)");
            sb.AppendLine("  ✓ Table alias matches JOIN chain (T1, T2, T3)");
            sb.AppendLine("  ✓ PostgreSQL: Alias WITHOUT quotes (T1), column WITH quotes (T1.\"ColumnName\")");
            sb.AppendLine("  ✓ PostgreSQL: FROM \"Schema\".\"Table\" T1 (NOT \"T1\")");
            sb.AppendLine("  ✓ PostgreSQL: SELECT T1.\"Col\", WHERE T1.\"ID\" IN (...), ORDER BY T1.\"Col\" (alias NEVER in quotes)");
            sb.AppendLine("  ✓ PostgreSQL: ✗ WRONG \"T1\".\"Col\" - ✓ CORRECT T1.\"Col\"");
            sb.AppendLine("  ✓ SQL Server: Use TOP N (after SELECT), NOT LIMIT N (after ORDER BY)");
            sb.AppendLine("  ✓ SQL Server: ✗ WRONG SELECT ... ORDER BY ... LIMIT 5 - ✓ CORRECT SELECT TOP 5 ... ORDER BY ...");
            sb.AppendLine("  ✓ COUNT() uses column from correct table");
            sb.AppendLine("  ✓ GROUP BY uses correct aggregation level (grouping queries → GROUP BY GroupingColumn ONLY, NOT GroupingColumn, DetailKeyColumn)");
            sb.AppendLine("  ✓ GROUP BY does NOT include detail-level columns when grouping by grouping level");
            sb.AppendLine("  ✓ GROUP BY does NOT include unrelated columns (even if in SELECT for JOINs) - ONLY grouping level columns");
            sb.AppendLine("  ✓ GROUP BY does NOT include mapping columns unless they ARE the grouping level");
            sb.AppendLine("  ✓ SELECT does NOT include DetailKeyColumn when grouping by GroupingColumn (only use DetailKeyColumn in COUNT(DISTINCT DetailKeyColumn))");
            sb.AppendLine("  ✓ If query asks 'which grouping has most detail records', SELECT has ONLY: GroupingColumn, COUNT(DISTINCT DetailKeyColumn), SUM/AVG/MAX/MIN(...)");
            sb.AppendLine("  ✓ If query asks 'which grouping has most detail records', DetailKeyColumn is NOT in SELECT and NOT in GROUP BY");
            sb.AppendLine("  ✓ Single database queries: Generate ONLY 1 SQL query (not multiple blocks for same database)");
            sb.AppendLine("  ✓ Cross-database isolation: Each database query uses ONLY tables from that database (check SYSTEM message table list)");
            sb.AppendLine("  ✓ Cross-database isolation: NEVER use SchemaName.TableName from Database X when writing SQL for Database Y");
            sb.AppendLine("  ✓ Cross-database isolation: If SYSTEM shows 'SchemaName.TableName' for Database X, it EXISTS ONLY in Database X");
            sb.AppendLine("  ✓ Mapping columns: In SELECT for cross-database joins, but in GROUP BY ONLY if they are grouping level");
            sb.AppendLine("  ✓ LIMIT/TOP: Only used when user explicitly asks for specific number (e.g., 'first N'), NOT for 'most X' queries without number");
            sb.AppendLine("  ✓ 'most X' queries: Return ALL results ordered DESC (NO LIMIT 1), unless user specifies a number");
            sb.AppendLine("  ✓ Table/column names match SYSTEM message exactly");
            sb.AppendLine("  ✓ Semantic keywords used to match user query intent to schema elements");
            if (queryIntent.DatabaseQueries.Count > 1)
            {
                sb.AppendLine("  ✓✓✓ MAPPING COLUMNS: Check SYSTEM message for 🚨🚨🚨 MAPPING COLUMNS REQUIRED - ALL must be in SELECT! ✓✓✓");
            }
            sb.AppendLine();
            sb.AppendLine("NOW GENERATE SQL.");

            return sb.ToString();
        }
    }
}
