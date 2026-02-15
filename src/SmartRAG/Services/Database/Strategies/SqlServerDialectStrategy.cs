using System.Text;

namespace SmartRAG.Services.Database.Strategies;


public class SqlServerDialectStrategy : BaseSqlDialectStrategy
{
    public override DatabaseType DatabaseType => DatabaseType.SqlServer;
    public override bool ValidateSyntax(string sql, out string errorMessage)
    {
        if (!base.ValidateSyntax(sql, out errorMessage))
            return false;

        var topAfterOrderByPattern = new Regex(@"ORDER\s+BY\s+[^;]*?\s+TOP\s+\d+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (topAfterOrderByPattern.IsMatch(sql))
        {
            errorMessage = "TOP clause must come immediately after SELECT, not after ORDER BY";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    public override string FormatSql(string sql, DatabaseSchemaInfo? schema = null)
    {
        var formatted = base.FormatSql(sql, schema);
        formatted = ConvertBackticksToBrackets(formatted);

        formatted = ConvertLimitToTop(formatted);
        formatted = ConvertFetchFirstToTop(formatted);
        formatted = FixTopClausePlacement(formatted);
        formatted = FixGroupByOrdinal(formatted);
        formatted = FixColumnUsedAsFunction(formatted, schema);
        formatted = FixMalformedOrderBy(formatted);
        formatted = FixInvalidAliasWithDot(formatted);
        formatted = FixHallucinatedFunctionUnitPriceDiscount(formatted);

        return formatted;
    }

    private static string FixInvalidAliasWithDot(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return sql;
        return Regex.Replace(sql, @"\bAS\s+([A-Za-z_][A-Za-z0-9_]*\.[A-Za-z_][A-Za-z0-9_]*)\b", m =>
        {
            var alias = m.Groups[1].Value;
            var safeAlias = alias.Replace(".", "_");
            return $"AS {safeAlias}";
        }, RegexOptions.IgnoreCase);
    }

    private static string FixHallucinatedFunctionUnitPriceDiscount(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql) || !sql.Contains("UnitPriceDiscount", StringComparison.OrdinalIgnoreCase))
            return sql;
        return Regex.Replace(sql, @"UnitPriceDiscount\s*\([^)]*\)", "COUNT(*)", RegexOptions.IgnoreCase);
    }

    private static string FixMalformedOrderBy(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return sql;
        sql = Regex.Replace(sql, @"\bORDER\s+BY\s+(\d+)\.([a-zA-Z_][a-zA-Z0-9_]*)\b", "ORDER BY $1", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"\bORDER\s+BY\s+(\d+)\([^)]+\)", "ORDER BY $1", RegexOptions.IgnoreCase);
        return sql;
    }

    private static readonly string[] ColumnLikeSuffixes = { "ID", "Quantity", "Price", "Discount", "Amount", "Count", "Date", "Name", "Number" };

    private static string FixColumnUsedAsFunction(string sql, DatabaseSchemaInfo? schema)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var knownFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "COUNT", "SUM", "AVG", "MIN", "MAX", "COALESCE", "NULLIF", "CAST", "CONVERT",
            "ISNULL", "ISNUMERIC", "LEN", "LEFT", "RIGHT", "SUBSTRING", "UPPER", "LOWER",
            "RTRIM", "LTRIM", "TRIM", "GETDATE", "DATEADD", "DATEDIFF", "YEAR", "MONTH", "DAY",
            "ABS", "ROUND", "CEILING", "FLOOR", "ROW_NUMBER", "RANK", "DENSE_RANK",
            "TOP", "LIMIT", "SELECT", "FROM", "WHERE", "GROUP", "ORDER", "HAVING", "JOIN"
        };

        var schemaColumns = schema?.Tables != null
            ? schema.Tables.SelectMany(t => t.Columns.Select(c => c.ColumnName)).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var pattern = new Regex(@"((?:\[?[A-Za-z_][A-Za-z0-9_]*\]?\.)?\[?[A-Za-z_][A-Za-z0-9_]*\]?)\s*\(\s*([A-Za-z0-9_.\[\]]+)\s*\)", RegexOptions.IgnoreCase);
        return pattern.Replace(sql, m =>
        {
            var raw = m.Groups[1].Value;
            var identifier = raw.Trim('[', ']');
            var lastPart = identifier.Contains('.') ? identifier.Split('.').Last() : identifier;
            if (knownFunctions.Contains(lastPart))
                return m.Value;
            if (schemaColumns.Contains(lastPart) || schemaColumns.Contains(identifier))
                return raw;
            if (ColumnLikeSuffixes.Any(s => lastPart.EndsWith(s, StringComparison.OrdinalIgnoreCase)) &&
                lastPart.Length > 2 && char.IsLetter(lastPart[0]))
                return raw;
            return m.Value;
        });
    }

    private static string ConvertBackticksToBrackets(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql) || !sql.Contains('`'))
            return sql;
        return Regex.Replace(sql, @"`([^`]+)`", "[$1]", RegexOptions.None);
    }

    private static string ConvertLimitToTop(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql) || !sql.ToUpperInvariant().Contains("LIMIT "))
            return sql;

        var limitMatch = Regex.Match(sql, @"\bLIMIT\s+(\d+)(?:\s+OFFSET\s+\d+)?\s*;?\s*$", RegexOptions.IgnoreCase);
        if (!limitMatch.Success)
            limitMatch = Regex.Match(sql, @"\bLIMIT\s+(\d+)\s*;?\s*$", RegexOptions.IgnoreCase);
        if (!limitMatch.Success)
            return sql;

        var n = limitMatch.Groups[1].Value;
        var withoutLimit = sql[..limitMatch.Index].TrimEnd();

        if (Regex.IsMatch(withoutLimit, @"\bSELECT\s+TOP\s+\d+\b", RegexOptions.IgnoreCase))
            return withoutLimit + ";";

        var selectMatch = Regex.Match(withoutLimit, @"\bSELECT\b", RegexOptions.IgnoreCase);
        if (!selectMatch.Success)
            return sql;

        var insertPos = selectMatch.Index + 6;
        while (insertPos < withoutLimit.Length && char.IsWhiteSpace(withoutLimit[insertPos]))
            insertPos++;

        return withoutLimit[..insertPos] + $"TOP {n} " + withoutLimit[insertPos..] + ";";
    }

    private static string ConvertFetchFirstToTop(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var fetchMatch = Regex.Match(sql, @"\b(ORDER\s+BY\s+[\s\S]+?)\s+FETCH\s+FIRST\s+(\d+)\s+ROWS?\s+ONLY\s*;?\s*$", RegexOptions.IgnoreCase);
        if (!fetchMatch.Success)
            return sql;

        var orderByClause = fetchMatch.Groups[1].Value.Trim();
        var n = fetchMatch.Groups[2].Value;
        var withoutFetch = sql[..fetchMatch.Index].TrimEnd();

        if (Regex.IsMatch(withoutFetch, @"\bSELECT\s+TOP\s+\d+\b", RegexOptions.IgnoreCase))
            return withoutFetch + " " + orderByClause + ";";

        var selectMatch = Regex.Match(withoutFetch, @"\bSELECT\b", RegexOptions.IgnoreCase);
        if (!selectMatch.Success)
            return sql;

        var insertPos = selectMatch.Index + 6;
        while (insertPos < withoutFetch.Length && char.IsWhiteSpace(withoutFetch[insertPos]))
            insertPos++;

        return withoutFetch[..insertPos] + $"TOP {n} " + withoutFetch[insertPos..] + " " + orderByClause + ";";
    }

    private static string FixTopClausePlacement(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var topAfterOrderByPattern = new Regex(
            @"(ORDER\s+BY\s+[^;]*?)\s+TOP\s+(\d+)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        var match = topAfterOrderByPattern.Match(sql);
        if (!match.Success)
            return sql;

        var orderByClause = match.Groups[1].Value.Trim();
        var topValue = match.Groups[2].Value;

        var selectPattern = new Regex(@"SELECT\s+(TOP\s+\d+\s+)?", RegexOptions.IgnoreCase);
        var selectMatch = selectPattern.Match(sql);
        if (selectMatch.Success && selectMatch.Groups[1].Success)
            return sql.Replace(match.Value, orderByClause);

        var selectIndex = sql.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
        if (selectIndex < 0)
            return sql;

        var afterSelectIndex = selectIndex + 6;
        while (afterSelectIndex < sql.Length && char.IsWhiteSpace(sql[afterSelectIndex]))
            afterSelectIndex++;

        var fixedSql = sql[..afterSelectIndex] + $"TOP {topValue} " + sql[afterSelectIndex..];
        fixedSql = fixedSql.Replace(match.Value, orderByClause);

        return fixedSql;
    }

    private static string FixGroupByOrdinal(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql) || !Regex.IsMatch(sql, @"GROUP\s+BY\s+[\d\s,]+", RegexOptions.IgnoreCase))
            return sql;

        var groupByMatch = Regex.Match(sql, @"GROUP\s+BY\s+([\d\s,]+)(?:\s+ORDER|\s+HAVING|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!groupByMatch.Success)
            return sql;

        var ordinals = Regex.Matches(groupByMatch.Groups[1].Value, @"\d+")
            .Select(m => int.TryParse(m.Value, out var n) ? n : 0)
            .Where(n => n > 0)
            .ToList();
        if (ordinals.Count == 0)
            return sql;

        var selectMatch = Regex.Match(sql, @"SELECT\s+(?:TOP\s+\d+\s+)?(.*?)\s+FROM", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!selectMatch.Success)
            return sql;

        var selectClause = selectMatch.Groups[1].Value;
        var columns = new List<string>();
        var depth = 0;
        var current = new StringBuilder();
        foreach (var ch in selectClause)
        {
            if (ch == '(') depth++;
            else if (ch == ')') depth--;
            else if ((ch == ',' || ch == '\t') && depth == 0)
            {
                var part = current.ToString().Trim();
                var asMatch = Regex.Match(part, @"^(.+?)\s+AS\s+[\w\[\]""]+$", RegexOptions.IgnoreCase);
                columns.Add(asMatch.Success ? asMatch.Groups[1].Value.Trim() : part);
                current.Clear();
                continue;
            }
            if (depth >= 0) current.Append(ch);
        }
        var last = current.ToString().Trim();
        if (!string.IsNullOrEmpty(last))
        {
            var asMatch = Regex.Match(last, @"^(.+?)\s+AS\s+[\w\[\]""]+$", RegexOptions.IgnoreCase);
            columns.Add(asMatch.Success ? asMatch.Groups[1].Value.Trim() : last);
        }

        if (columns.Count == 0 || ordinals.Max() > columns.Count)
            return sql;

        var replacementColumns = string.Join(", ", ordinals.Select(i => columns[i - 1]));
        return Regex.Replace(sql, @"GROUP\s+BY\s+[\d\s,]+", $"GROUP BY {replacementColumns}", RegexOptions.IgnoreCase);
    }

}

