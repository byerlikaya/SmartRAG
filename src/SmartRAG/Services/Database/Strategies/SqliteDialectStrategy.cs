using SmartRAG.Enums;
using SmartRAG.Models;
using System.Text;

namespace SmartRAG.Services.Database.Strategies
{
    public class SqliteDialectStrategy : BaseSqlDialectStrategy
    {
        public override DatabaseType DatabaseType => DatabaseType.SQLite;
        protected override string GetDialectName() => "SQLite";

        public override string BuildSystemPrompt(DatabaseSchemaInfo schema, string queryIntent)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are an expert SQLite query generator.");
            sb.AppendLine("Generate a valid SQLite SQL query based on the user's intent and the provided schema.");
            sb.AppendLine("Rules:");
            sb.AppendLine("1. Use standard SQLite syntax.");
            sb.AppendLine("2. Do NOT use functions not supported by SQLite (e.g., CONCAT, use || instead).");
            sb.AppendLine("3. Return ONLY the SQL query, no markdown, no explanations.");
            sb.AppendLine("4. Use parameterized queries where possible (though for this task generate full SQL).");
            sb.AppendLine("5. Handle case-insensitivity appropriately using COLLATE NOCASE if needed.");
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
