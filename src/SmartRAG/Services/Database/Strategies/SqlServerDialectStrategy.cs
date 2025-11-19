using SmartRAG.Enums;
using SmartRAG.Models;
using System.Text;

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
            sb.AppendLine("Rules:");
            sb.AppendLine("1. Use T-SQL syntax.");
            sb.AppendLine("2. Use brackets [] for identifiers.");
            sb.AppendLine("3. Use TOP for limiting results (e.g., SELECT TOP 10 * ...).");
            sb.AppendLine("4. Return ONLY the SQL query, no markdown, no explanations.");
            sb.AppendLine();
            sb.AppendLine("Schema:");
            sb.AppendLine(FormatSchemaDescription(schema));
            sb.AppendLine();
            sb.AppendLine($"Query Intent: {queryIntent}");
            
            return sb.ToString();
        }

        public override string GetLimitClause(int limit)
        {
            // SQL Server uses TOP in SELECT clause, not LIMIT at end
            // This method might need to be handled differently or return empty string if handled in prompt
            return ""; 
        }
        
        public override string FormatSql(string sql)
        {
            var formatted = base.FormatSql(sql);
            // Ensure it doesn't end with LIMIT if the AI mistakenly added it
            if (formatted.ToUpper().Contains("LIMIT "))
            {
                // Simple heuristic to remove LIMIT clause if present at end
                var index = formatted.LastIndexOf("LIMIT ", System.StringComparison.OrdinalIgnoreCase);
                if (index > formatted.Length - 20) // Only if near end
                {
                    formatted = formatted.Substring(0, index).Trim();
                }
            }
            return formatted;
        }
    }
}
