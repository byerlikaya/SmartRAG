using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.Database;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmartRAG.Services.Database.Validation
{
    /// <summary>
    /// Validates SQL queries against database schemas
    /// </summary>
    public class SqlValidator : ISqlValidator
    {
        private readonly ILogger<SqlValidator> _logger;

        /// <summary>
        /// Initializes a new instance of the SqlValidator
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public SqlValidator(ILogger<SqlValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<string> ValidateQuery(string sql, DatabaseSchemaInfo schema, List<string> requiredTables)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(sql))
            {
                errors.Add("SQL query is empty");
                return errors;
            }

            var tableErrors = ValidateTables(sql, schema, requiredTables);
            errors.AddRange(tableErrors);

            var columnErrors = ValidateColumns(sql, schema, requiredTables);
            errors.AddRange(columnErrors);

            var syntaxErrors = ValidateSyntax(sql);
            errors.AddRange(syntaxErrors);

            return errors;
        }

        private List<string> ValidateTables(string sql, DatabaseSchemaInfo schema, List<string> requiredTables)
        {
            var errors = new List<string>();

            var tableMatches = Regex.Matches(sql, @"(?:FROM|JOIN)\s+(\[?[a-zA-Z0-9_]+\]?(?:\]?\.\[?[a-zA-Z0-9_]+\]?)?)", RegexOptions.IgnoreCase);

            foreach (Match match in tableMatches)
            {
                var tableNameRaw = match.Groups[1].Value;
                var tableName = NormalizeTableName(tableNameRaw);

                if (IsSqlKeyword(tableName)) continue;

                // For PostgreSQL, check case-sensitive matching first
                if (schema.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
                {
                    var exactMatch = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableNameRaw, StringComparison.Ordinal));
                    if (exactMatch != null)
                    {
                        var exactTableName = exactMatch.TableName;
                        if (!requiredTables.Any(t => t.Equals(exactTableName, StringComparison.OrdinalIgnoreCase)))
                        {
                            _logger.LogWarning(
                                "Table '{Table}' exists in database '{Database}' but was not in the required tables list.",
                                exactTableName, schema.DatabaseName);
                        }
                        continue;
                    }

                    // Try case-insensitive match for better error message
                    var caseInsensitiveMatch = schema.Tables.FirstOrDefault(t => 
                        t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                        t.TableName.Equals(tableNameRaw, StringComparison.OrdinalIgnoreCase));
                    
                    if (caseInsensitiveMatch != null)
                    {
                        errors.Add($"Table '{tableNameRaw}' case mismatch in PostgreSQL. Use exact case: '{caseInsensitiveMatch.TableName}'");
                        continue;
                    }
                }

                var tableExists = schema.Tables.Any(t => 
                    t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                    t.TableName.Equals(tableNameRaw, StringComparison.OrdinalIgnoreCase));
                if (!tableExists)
                {
                    errors.Add($"Table '{tableNameRaw}' does not exist in database '{schema.DatabaseName}'");
                    continue;
                }

                var matchedTable = schema.Tables.FirstOrDefault(t => 
                    t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                    t.TableName.Equals(tableNameRaw, StringComparison.OrdinalIgnoreCase));
                var tableNameForRequiredCheck = matchedTable?.TableName ?? tableName;

                if (!requiredTables.Contains(tableNameForRequiredCheck, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Table '{Table}' exists in database '{Database}' but was not in the required tables list. " +
                        "Allowing query to proceed. This may indicate QueryIntentAnalyzer needs improvement.",
                        tableNameForRequiredCheck, schema.DatabaseName);

                }
            }

            return errors;
        }

        private List<string> ValidateColumns(string sql, DatabaseSchemaInfo schema, List<string> requiredTables)
        {
            var errors = new List<string>();

            var aliasToTable = ExtractTableAliases(sql);

            var columnMatches = Regex.Matches(sql, @"\b([a-zA-Z0-9_]+)\.([a-zA-Z0-9_]+)\b", RegexOptions.IgnoreCase);

            foreach (Match match in columnMatches)
            {
                var prefix = match.Groups[1].Value;
                var columnName = match.Groups[2].Value;

                if (columnName.Equals("*", StringComparison.OrdinalIgnoreCase)) continue;

                string tableName = prefix;

                if (aliasToTable.ContainsKey(prefix))
                {
                    tableName = aliasToTable[prefix];
                }

                var table = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));

                if (table == null) continue;

                if (!requiredTables.Contains(table.TableName, StringComparer.OrdinalIgnoreCase)) continue;

                // For PostgreSQL, check case-sensitive matching first
                if (schema.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
                {
                    var exactMatch = table.Columns.FirstOrDefault(c => c.ColumnName.Equals(columnName, StringComparison.Ordinal));
                    if (exactMatch != null)
                        continue;

                    var caseInsensitiveMatch = table.Columns.FirstOrDefault(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));
                    if (caseInsensitiveMatch != null)
                    {
                        var aliasInfo = aliasToTable.ContainsKey(prefix) ? $" (via alias '{prefix}')" : "";
                        errors.Add($"Column '{columnName}' case mismatch in PostgreSQL. Use exact case: '{caseInsensitiveMatch.ColumnName}' in table '{tableName}'{aliasInfo}");
                        continue;
                    }
                }

                var columnExists = table.Columns.Any(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));

                if (!columnExists)
                {
                    var aliasInfo = aliasToTable.ContainsKey(prefix) ? $" (via alias '{prefix}')" : "";
                    errors.Add($"Column '{columnName}' does not exist in table '{tableName}'{aliasInfo}");
                }
            }

            return errors;
        }

        private List<string> ValidateSyntax(string sql)
        {
            var errors = new List<string>();

            if (sql.Contains("CROSS JOIN", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("CROSS JOIN is not allowed. Use explicit INNER JOIN or LEFT JOIN.");
            }

            var selectCount = Regex.Matches(sql, @"SELECT\s", RegexOptions.IgnoreCase).Count;

            if (selectCount > 2)
            {
                errors.Add("Too many nested subqueries (max 2 levels allowed).");
            }

            return errors;
        }

        private bool IsSqlKeyword(string word)
        {
            var keywords = new[] { "SELECT", "FROM", "WHERE", "JOIN", "ON", "AND", "OR", "GROUP", "BY", "ORDER", "LIMIT", "TOP", "AS", "LEFT", "RIGHT", "INNER", "OUTER" };
            return keywords.Contains(word.ToUpperInvariant());
        }

        private string NormalizeTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return tableName;

            var normalized = tableName
                .Replace("[", "")
                .Replace("]", "")
                .Replace("\"", "")
                .Replace("`", "")
                .Trim();

            return normalized;
        }

        private Dictionary<string, string> ExtractTableAliases(string sql)
        {
            var aliasToTable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var aliasMatches = Regex.Matches(sql,
                @"(?:FROM|JOIN)\s+(\[?[a-zA-Z0-9_]+\]?(?:\]?\.\[?[a-zA-Z0-9_]+\]?)?)(?:\s+(?:AS\s+)?([a-zA-Z0-9_]+))?",
                RegexOptions.IgnoreCase);

            foreach (Match match in aliasMatches)
            {
                var tableNameRaw = match.Groups[1].Value;
                var tableName = NormalizeTableName(tableNameRaw);
                var alias = match.Groups[2].Value;

                if (!string.IsNullOrWhiteSpace(alias) && !IsSqlKeyword(alias))
                {
                    aliasToTable[alias] = tableName;
                }
            }

            return aliasToTable;
        }
    }
}
