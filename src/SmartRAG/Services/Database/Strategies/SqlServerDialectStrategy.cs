using SmartRAG.Enums;
using SmartRAG.Models;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartRAG.Services.Database.Strategies
{
    public class SqlServerDialectStrategy : BaseSqlDialectStrategy
    {
        public override DatabaseType DatabaseType => DatabaseType.SqlServer;
        protected override string GetDialectName() => "SQL Server";

        public override string BuildSystemPrompt(DatabaseSchemaInfo schema, string queryIntent)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are an expert T-SQL (SQL Server) query generator.");
            sb.AppendLine("Generate a valid T-SQL query based on the user's intent and the provided schema.");
            sb.AppendLine();
            sb.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║  🚨 SQL SERVER CRITICAL SYNTAX - TOP CLAUSE PLACEMENT 🚨     ║");
            sb.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("CORRECT SQL SERVER SYNTAX:");
            sb.AppendLine("  ✓ SELECT TOP 100 columns FROM table ORDER BY column DESC");
            sb.AppendLine("  ✓ SELECT TOP 1 * FROM table WHERE condition");
            sb.AppendLine();
            sb.AppendLine("WRONG SYNTAX (WILL CAUSE ERROR):");
            sb.AppendLine("  ✗ SELECT columns FROM table ORDER BY column DESC TOP 1");
            sb.AppendLine("  ✗ SELECT columns FROM table ORDER BY column LIMIT 100");
            sb.AppendLine();
            sb.AppendLine("Rules:");
            sb.AppendLine("1. Use T-SQL syntax.");
            sb.AppendLine("2. Use brackets [] for identifiers if needed.");
            sb.AppendLine("3. TOP MUST come immediately after SELECT, before column names.");
            sb.AppendLine("4. NEVER use LIMIT - SQL Server does not support it.");
            sb.AppendLine("5. NEVER put TOP at the end of query.");
            sb.AppendLine("6. Return ONLY the SQL query, no markdown, no explanations.");
            sb.AppendLine();
            sb.AppendLine("Schema:");
            sb.AppendLine(FormatSchemaDescription(schema));
            sb.AppendLine();
            sb.AppendLine($"Query Intent: {queryIntent}");

            return sb.ToString();
        }

        public override string GetLimitClause(int limit)
        {
            return $"TOP {limit}";
        }

        public override bool ValidateSyntax(string sql, out string errorMessage)
        {
            // First check base validation
            if (!base.ValidateSyntax(sql, out errorMessage))
            {
                return false;
            }

            // Check for TOP keyword in wrong position (after ORDER BY)
            var topAfterOrderByPattern = new Regex(@"ORDER\s+BY\s+[^;]*?\s+TOP\s+\d+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (topAfterOrderByPattern.IsMatch(sql))
            {
                errorMessage = "TOP clause must come immediately after SELECT, not after ORDER BY";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string FormatSql(string sql)
        {
            var formatted = base.FormatSql(sql);

            // Remove LIMIT clause (SQL Server doesn't support it)
            if (formatted.ToUpper().Contains("LIMIT "))
            {
                var index = formatted.LastIndexOf("LIMIT ", System.StringComparison.OrdinalIgnoreCase);
                if (index > formatted.Length - 20) // Only if near end
                {
                    formatted = formatted[..index].Trim();
                }
            }

            // Fix TOP clause placement: Move TOP from after ORDER BY to after SELECT
            formatted = FixTopClausePlacement(formatted);

            return formatted;
        }

        /// <summary>
        /// Fixes TOP clause placement in SQL Server queries.
        /// Moves TOP from after ORDER BY to immediately after SELECT.
        /// </summary>
        private string FixTopClausePlacement(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return sql;
            }

            // Pattern to match: ORDER BY ... TOP N (case insensitive)
            var topAfterOrderByPattern = new Regex(
                @"(ORDER\s+BY\s+[^;]*?)\s+TOP\s+(\d+)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            );

            var match = topAfterOrderByPattern.Match(sql);
            if (!match.Success)
            {
                return sql; // No fix needed
            }

            var orderByClause = match.Groups[1].Value.Trim();
            var topValue = match.Groups[2].Value;

            // Check if SELECT already has TOP
            var selectPattern = new Regex(@"SELECT\s+(TOP\s+\d+\s+)?", RegexOptions.IgnoreCase);
            var selectMatch = selectPattern.Match(sql);

            if (selectMatch.Success && selectMatch.Groups[1].Success)
            {
                // SELECT already has TOP, just remove the incorrect TOP after ORDER BY
                return sql.Replace(match.Value, orderByClause);
            }

            // Insert TOP after SELECT
            var selectIndex = sql.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
            if (selectIndex < 0)
            {
                return sql; // No SELECT found, can't fix
            }

            var afterSelectIndex = selectIndex + 6; // Length of "SELECT"

            // Find where column list starts (skip whitespace)
            while (afterSelectIndex < sql.Length && char.IsWhiteSpace(sql[afterSelectIndex]))
            {
                afterSelectIndex++;
            }

            // Insert "TOP N " after SELECT
            var fixedSql = sql[..afterSelectIndex]
                         + $"TOP {topValue} "
                         + sql[afterSelectIndex..];

            // Remove the incorrect TOP after ORDER BY
            fixedSql = fixedSql.Replace(match.Value, orderByClause);

            return fixedSql;
        }
    }
}
