
namespace SmartRAG.Services.Database.Strategies;

public class MySqlDialectStrategy : BaseSqlDialectStrategy
{
    public override DatabaseType DatabaseType => DatabaseType.MySQL;

    public override string FormatSql(string sql, DatabaseSchemaInfo? schema = null)
    {
        var formatted = base.FormatSql(sql, schema);
        formatted = FixDerivedTableMissingAlias(formatted);
        return formatted;
    }

    private static string FixDerivedTableMissingAlias(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;
        var fixedSql = Regex.Replace(sql, @"\)\s+(?=ORDER\s+BY|GROUP\s+BY|WHERE|HAVING|LIMIT|$)",
            ") _dt ", RegexOptions.IgnoreCase);
        return fixedSql;
    }
}

