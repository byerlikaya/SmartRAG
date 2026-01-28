using SmartRAG.Enums;

namespace SmartRAG.Services.Database.Strategies
{
    public class PostgreSqlDialectStrategy : BaseSqlDialectStrategy
    {
        public override DatabaseType DatabaseType => DatabaseType.PostgreSQL;
        public override string FormatSql(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return sql;

            var formatted = base.FormatSql(sql);
            
            formatted = QuotePostgreSqlTableNames(formatted);
            
            return formatted;
        }

        private static string QuotePostgreSqlTableNames(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return sql;

            var pattern = @"\b(?:FROM|JOIN)\s+([a-zA-Z_][a-zA-Z0-9_]*\.[a-zA-Z_][a-zA-Z0-9_]*)";
            var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            return regex.Replace(sql, match =>
            {
                var tableName = match.Groups[1].Value;
                var quoted = EscapeTableNameForPostgreSql(tableName);
                return match.Value.Replace(tableName, quoted);
            });
        }

        private static string EscapeTableNameForPostgreSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) return tableName;

            if (tableName.Contains('.'))
            {
                var parts = tableName.Split('.', 2);
                var schemaPart = parts[0];
                var tablePart = parts[1];
                
                var quotedSchema = HasUpperCase(schemaPart) ? $"\"{schemaPart}\"" : schemaPart;
                var quotedTable = HasUpperCase(tablePart) ? $"\"{tablePart}\"" : tablePart;
                
                return $"{quotedSchema}.{quotedTable}";
            }
            
            return HasUpperCase(tableName) ? $"\"{tableName}\"" : tableName;
        }

        private static bool HasUpperCase(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return false;
            
            foreach (var c in str)
            {
                if (char.IsUpper(c))
                    return true;
            }
            
            return false;
        }
    }
}
