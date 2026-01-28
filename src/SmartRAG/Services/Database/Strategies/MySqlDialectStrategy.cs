using SmartRAG.Enums;

namespace SmartRAG.Services.Database.Strategies
{
    public class MySqlDialectStrategy : BaseSqlDialectStrategy
    {
        public override DatabaseType DatabaseType => DatabaseType.MySQL;
        protected override string GetDialectName() => "MySQL";

        public override string EscapeIdentifier(string identifier)
        {
            // MySQL uses backticks for identifiers with spaces/special chars
            if (string.IsNullOrWhiteSpace(identifier)) return identifier;
            
            // If identifier contains space or special chars, wrap in backticks
            if (identifier.Contains(" ") || identifier.Contains("-") || identifier.Contains("."))
            {
                return $"`{identifier}`";
            }
            
            return identifier;
        }
    }
}
