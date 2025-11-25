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
            return ""; 
        }
        
        public override string FormatSql(string sql)
        {
            var formatted = base.FormatSql(sql);
            if (formatted.ToUpper().Contains("LIMIT "))
            {
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
