using SmartRAG.Enums;

namespace SmartRAG.Services.Database.Strategies
{
    public class SqliteDialectStrategy : BaseSqlDialectStrategy
    {
        public override DatabaseType DatabaseType => DatabaseType.SQLite;
        protected override string GetDialectName() => "SQLite";

        public override string EscapeIdentifier(string identifier)
        {
            // SQLite uses double quotes for identifiers with spaces/special chars
            if (string.IsNullOrWhiteSpace(identifier)) return identifier;
            
            // If identifier contains space or special chars, wrap in double quotes
            if (identifier.Contains(" ") || identifier.Contains("-") || identifier.Contains("."))
            {
                return $"\"{identifier}\"";
            }
            
            return identifier;
        }
    }
}
