
namespace SmartRAG.Services.Database.Strategies;


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
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

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

        if (!tableName.Contains('.'))
            return HasUpperCase(tableName) ? $"\"{tableName}\"" : tableName;

        var parts = tableName.Split('.', 2);
        var schemaPart = parts[0];
        var tablePart = parts[1];

        var quotedSchema = HasUpperCase(schemaPart) ? $"\"{schemaPart}\"" : schemaPart;
        var quotedTable = HasUpperCase(tablePart) ? $"\"{tablePart}\"" : tablePart;

        return $"{quotedSchema}.{quotedTable}";

    }

    private static bool HasUpperCase(string str)
    {
        return !string.IsNullOrWhiteSpace(str) && str.Any(char.IsUpper);
    }
}

