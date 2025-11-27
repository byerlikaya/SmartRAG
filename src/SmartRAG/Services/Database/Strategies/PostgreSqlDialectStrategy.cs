using SmartRAG.Enums;
using SmartRAG.Models;
using System.Text;

namespace SmartRAG.Services.Database.Strategies
{
    public class PostgreSqlDialectStrategy : BaseSqlDialectStrategy
    {
        public override DatabaseType DatabaseType => DatabaseType.PostgreSQL;
        protected override string GetDialectName() => "PostgreSQL";

        public override string BuildSystemPrompt(DatabaseSchemaInfo schema, string queryIntent)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are an expert PostgreSQL query generator.");
            sb.AppendLine("Generate a valid PostgreSQL SQL query based on the user's intent and the provided schema.");
            sb.AppendLine("Rules:");
            sb.AppendLine("1. Use standard PostgreSQL syntax.");
            sb.AppendLine("2. Use double quotes for identifiers if they are case-sensitive.");
            sb.AppendLine("3. Use ILIKE for case-insensitive pattern matching.");
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
            return $"LIMIT {limit}";
        }
    }
}
