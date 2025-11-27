using SmartRAG.Enums;
using SmartRAG.Models;
using System.Text;

namespace SmartRAG.Services.Database.Strategies
{
    public class MySqlDialectStrategy : BaseSqlDialectStrategy
    {
        public override DatabaseType DatabaseType => DatabaseType.MySQL;
        protected override string GetDialectName() => "MySQL";

        public override string BuildSystemPrompt(DatabaseSchemaInfo schema, string queryIntent)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are an expert MySQL query generator.");
            sb.AppendLine("Generate a valid MySQL SQL query based on the user's intent and the provided schema.");
            sb.AppendLine("Rules:");
            sb.AppendLine("1. Use standard MySQL syntax.");
            sb.AppendLine("2. Use backticks ` for identifiers.");
            sb.AppendLine("3. Return ONLY the SQL query, no markdown, no explanations.");
            sb.AppendLine();
            sb.AppendLine("Schema:");
            sb.AppendLine(FormatSchemaDescription(schema));
            sb.AppendLine();
            sb.AppendLine($"Query Intent: {queryIntent}");
            
            return sb.ToString();
        }

        public override string GetLimitClause(int limit)
        {
            return $"LIMIT {limit}";
        }
    }
}
