using Microsoft.Extensions.Logging;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmartRAG.Services.Database.Validation
{
    public class SqlValidator
    {
        private readonly ILogger _logger;

        public SqlValidator(ILogger logger)
        {
            _logger = logger;
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

            var tableMatches = Regex.Matches(sql, @"(?:FROM|JOIN)\s+([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase);

            foreach (Match match in tableMatches)
            {
                var tableName = match.Groups[1].Value;

                if (IsSqlKeyword(tableName)) continue;

                // CRITICAL: Check if table exists in schema (ERROR if not)
                var tableExists = schema.Tables.Any(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                if (!tableExists)
                {
                    errors.Add($"Table '{tableName}' does not exist in database '{schema.DatabaseName}'");
                    continue;
                }

                if (!requiredTables.Contains(tableName, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Table '{Table}' exists in database '{Database}' but was not in the required tables list. " +
                        "Allowing query to proceed. This may indicate QueryIntentAnalyzer needs improvement.",
                        tableName, schema.DatabaseName);

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

        /// <summary>
        /// Extracts table aliases from SQL query (e.g., "FROM TableName t2" -> {"t2": "TableName"})
        /// </summary>
        private Dictionary<string, string> ExtractTableAliases(string sql)
        {
            var aliasToTable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var aliasMatches = Regex.Matches(sql,
                @"(?:FROM|JOIN)\s+([a-zA-Z0-9_]+)(?:\s+(?:AS\s+)?([a-zA-Z0-9_]+))?",
                RegexOptions.IgnoreCase);

            foreach (Match match in aliasMatches)
            {
                var tableName = match.Groups[1].Value;
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
