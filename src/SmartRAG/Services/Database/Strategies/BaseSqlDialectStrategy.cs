namespace SmartRAG.Services.Database.Strategies;


/// <summary>
/// Base class for SQL dialect strategies containing common logic
/// </summary>
public abstract class BaseSqlDialectStrategy : ISqlDialectStrategy
{
    public abstract DatabaseType DatabaseType { get; }

    public virtual bool ValidateSyntax(string sql, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            errorMessage = "SQL query is empty";
            return false;
        }

        var forbiddenKeywords = new[] { "DROP", "DELETE", "TRUNCATE", "ALTER", "CREATE", "GRANT", "REVOKE", "EXEC", "EXECUTE" };
        foreach (var keyword in forbiddenKeywords)
        {
            // Use word boundary regex to avoid false positives (e.g., "CreatedDate" should not match "CREATE")
            var pattern = $@"\b{Regex.Escape(keyword)}\b";
            if (Regex.IsMatch(sql, pattern, RegexOptions.IgnoreCase))
            {
                if (!IsInsideStringLiteral(sql, keyword))
                {
                    errorMessage = $"SQL contains forbidden keyword: {keyword}";
                    return false;
                }
            }
        }

        errorMessage = string.Empty;
        return true;
    }

    public virtual string FormatSql(string sql, DatabaseSchemaInfo? schema = null)
    {
        if (string.IsNullOrWhiteSpace(sql)) return sql;

        var formatted = sql.Trim();
        if (formatted.EndsWith(";")) formatted = formatted.TrimEnd(';');

        formatted = FixGroupOrderByTypo(formatted);
        formatted = FixMissingCommaInGroupBy(formatted);
        formatted = FixLeadingCommaInGroupBy(formatted);
        formatted = FixDuplicateBy(formatted);
        formatted = FixTrailingComma(formatted);

        return formatted;
    }

    private static string FixGroupOrderByTypo(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return sql;
        var g = Regex.Replace(sql, @"\bGROUP\s+(?!BY\b)([A-Za-z0-9_.\[\],\s]+?)(?=\s+ORDER|\s+HAVING|\s*$|;)", "GROUP BY $1", RegexOptions.IgnoreCase);
        return Regex.Replace(g, @"\bORDER\s+(?!BY\b)([A-Za-z0-9_.\[\],\s]+?)(?=\s*$|;)", "ORDER BY $1", RegexOptions.IgnoreCase);
    }

    private static string FixDuplicateBy(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return sql;
        var r = Regex.Replace(sql, @"\b(GROUP|ORDER)\s+BY\s+BY\b", "$1 BY", RegexOptions.IgnoreCase);
        return Regex.Replace(r, @"\b(GROUP|ORDER)\s+BY\s+([^,\s]+)\s+BY\b", "$1 BY $2", RegexOptions.IgnoreCase);
    }

    private static string FixTrailingComma(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return sql;
        sql = Regex.Replace(sql, @",\s*(?=FROM|WHERE|GROUP|ORDER|HAVING|LIMIT|TOP|\)|$)", " ", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @",\s*(?=\s*(?:ASC|DESC)\b)", " ", RegexOptions.IgnoreCase);
        return sql;
    }

    private static string FixLeadingCommaInGroupBy(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return sql;
        var r = Regex.Replace(sql, @"\bGROUP\s+BY\s+,", "GROUP BY ", RegexOptions.IgnoreCase);
        return Regex.Replace(r, @"\bORDER\s+BY\s+,", "ORDER BY ", RegexOptions.IgnoreCase);
    }

    private static string FixMissingCommaInGroupBy(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return sql;
        string prev;
        do
        {
            prev = sql;
            sql = Regex.Replace(sql, @"\bGROUP\s+BY\s+([A-Za-z0-9_.\[\]]+)\s+([A-Za-z0-9_.\[\]]+)(?=\s+ORDER|\s+HAVING|\s*$|;|\s*,)", "GROUP BY $1, $2", RegexOptions.IgnoreCase);
        }
        while (sql != prev);
        return sql;
    }

    protected bool IsInsideStringLiteral(string sql, string keyword)
    {
        var index = sql.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return false;

        var quoteCount = 0;
        for (var i = 0; i < index; i++)
        {
            if (sql[i] == '\'') quoteCount++;
        }

        return quoteCount % 2 != 0;
    }
}

