using SmartRAG.Enums;
using SmartRAG.Interfaces.Database.Strategies;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartRAG.Services.Database.Strategies
{
    /// <summary>
    /// Base class for SQL dialect strategies containing common logic
    /// </summary>
    public abstract class BaseSqlDialectStrategy : ISqlDialectStrategy
    {
        public abstract DatabaseType DatabaseType { get; }

        public abstract string BuildSystemPrompt(DatabaseSchemaInfo schema, string queryIntent);

        public virtual bool ValidateSyntax(string sql, out string errorMessage)
        {
            // Basic validation common to most SQL dialects
            if (string.IsNullOrWhiteSpace(sql))
            {
                errorMessage = "SQL query is empty";
                return false;
            }

            // Check for basic SQL injection keywords that shouldn't be in a generated query
            // This is a safety net, though the AI should be instructed not to use them
            var forbiddenKeywords = new[] { "DROP", "DELETE", "TRUNCATE", "ALTER", "GRANT", "REVOKE" };
            foreach (var keyword in forbiddenKeywords)
            {
                if (sql.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Allow these words if they are part of a string literal (simple check)
                    // A more robust check would parse the SQL, but this is a basic safety net
                    if (!IsInsideStringLiteral(sql, keyword))
                    {
                        errorMessage = $"SQL contains forbidden keyword: {keyword}";
                        return false;
                    }
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        public virtual string FormatSql(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return sql;
            
            // Basic cleanup
            var formatted = sql.Trim();
            if (formatted.EndsWith(";")) formatted = formatted.TrimEnd(';');
            
            return formatted;
        }

        public abstract string GetLimitClause(int limit);

        protected bool IsInsideStringLiteral(string sql, string keyword)
        {
            // Simplified check - assumes single quotes for strings
            // This is not a full SQL parser but covers common cases
            int index = sql.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return false;

            int quoteCount = 0;
            for (int i = 0; i < index; i++)
            {
                if (sql[i] == '\'') quoteCount++;
            }

            return quoteCount % 2 != 0;
        }
        
        protected string FormatSchemaDescription(DatabaseSchemaInfo schema)
        {
            var sb = new StringBuilder();
            
            foreach (var table in schema.Tables)
            {
                sb.AppendLine($"Table: {table.TableName}");
                sb.AppendLine("Columns:");
                foreach (var col in table.Columns)
                {
                    sb.AppendLine($"  - {col.ColumnName} ({col.DataType})");
                }
                
                if (table.ForeignKeys != null && table.ForeignKeys.Count > 0)
                {
                    sb.AppendLine("Foreign Keys:");
                    foreach (var fk in table.ForeignKeys)
                    {
                        sb.AppendLine($"  - {fk.ColumnName} -> {fk.ReferencedTable}.{fk.ReferencedColumn}");
                    }
                }
                sb.AppendLine();
            }
            
            return sb.ToString();
        }

        // Abstract method to be implemented by concrete dialect strategies
        protected abstract string GetDialectName();
    }
}
