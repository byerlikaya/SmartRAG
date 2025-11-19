using Microsoft.Extensions.Logging;
using SmartRAG.Enums;
using SmartRAG.Interfaces.Database;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

            // 1. Validate Tables
            var tableErrors = ValidateTables(sql, schema, requiredTables);
            errors.AddRange(tableErrors);

            // 2. Validate Columns
            var columnErrors = ValidateColumns(sql, schema, requiredTables);
            errors.AddRange(columnErrors);

            // 3. Validate Forbidden Keywords/Patterns
            var syntaxErrors = ValidateSyntax(sql);
            errors.AddRange(syntaxErrors);

            return errors;
        }

        private List<string> ValidateTables(string sql, DatabaseSchemaInfo schema, List<string> requiredTables)
        {
            var errors = new List<string>();
            
            // Extract table names from SQL (simple regex approach)
            // Matches FROM TableName, JOIN TableName
            var tableMatches = Regex.Matches(sql, @"(?:FROM|JOIN)\s+([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase);
            
            foreach (Match match in tableMatches)
            {
                var tableName = match.Groups[1].Value;
                
                // Skip if it's a keyword (false positive)
                if (IsSqlKeyword(tableName)) continue;

                // Check if table exists in schema
                var tableExists = schema.Tables.Any(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                if (!tableExists)
                {
                    errors.Add($"Table '{tableName}' does not exist in database '{schema.DatabaseName}'");
                    continue;
                }

                // Check if table is allowed for this query
                if (!requiredTables.Contains(tableName, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"Table '{tableName}' is not allowed for this query (not in required tables list)");
                }
            }

            return errors;
        }

        private List<string> ValidateColumns(string sql, DatabaseSchemaInfo schema, List<string> requiredTables)
        {
            var errors = new List<string>();
            
            // This is a simplified column validation. 
            // A full SQL parser would be needed for 100% accuracy, but this catches common hallucinations.
            
            foreach (var table in schema.Tables)
            {
                // Only check tables that are actually used/allowed
                if (!requiredTables.Contains(table.TableName, StringComparer.OrdinalIgnoreCase)) continue;

                // We can't easily extract columns per table without a full parser.
                // But we can check if the SQL contains "TableName.ColumnName" patterns that are invalid.
                
                // Regex to find TableName.ColumnName
                var columnMatches = Regex.Matches(sql, $@"{table.TableName}\.([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase);
                
                foreach (Match match in columnMatches)
                {
                    var columnName = match.Groups[1].Value;
                    if (columnName.Equals("*")) continue; // Allow SELECT * (though discouraged)

                    var columnExists = table.Columns.Any(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));
                    if (!columnExists)
                    {
                        errors.Add($"Column '{columnName}' does not exist in table '{table.TableName}'");
                    }
                }
                
                // Also check for aliases if possible, but that's harder without parsing.
                // For now, we rely on the AI being instructed to use Table.Column format or unique column names.
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

            // Check for nested subqueries (heuristic: multiple SELECTs)
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
    }
}
