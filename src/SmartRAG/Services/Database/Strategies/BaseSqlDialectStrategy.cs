
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

    public virtual string FormatSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return sql;

        var formatted = sql.Trim();
        if (formatted.EndsWith(";")) formatted = formatted.TrimEnd(';');

        return formatted;
    }

    protected bool IsInsideStringLiteral(string sql, string keyword)
    {
        int index = sql.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return false;

        int quoteCount = 0;
        for (int i = 0; i < index; i++)
        {
            if (sql[i] == '\'') quoteCount++;
        }

        return quoteCount % 2 != 0;
    }
}

