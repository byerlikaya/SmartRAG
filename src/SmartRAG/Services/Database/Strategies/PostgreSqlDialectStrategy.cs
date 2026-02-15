using System.Text;
using System.Text.RegularExpressions;

namespace SmartRAG.Services.Database.Strategies;


public class PostgreSqlDialectStrategy : BaseSqlDialectStrategy
{
    public override DatabaseType DatabaseType => DatabaseType.PostgreSQL;
    public override string FormatSql(string sql, DatabaseSchemaInfo? schema = null)
    {
        if (string.IsNullOrWhiteSpace(sql)) return sql;

        var formatted = base.FormatSql(sql, schema);
        formatted = FixOrderByPositionalQuoted(formatted);
        formatted = QuotePostgreSqlTableNames(formatted);
        formatted = NormalizeQuotedAliases(formatted);
        if (schema != null)
        {
            formatted = QuoteColumnsForPostgreSql(formatted, schema);
            formatted = QualifyTableRefsWithSchema(formatted, schema);
        }
        formatted = NormalizeDoubleQuotedIdentifiers(formatted);
        formatted = FixOrderByPositionalQuoted(formatted);
        return formatted;
    }

    private static string FixOrderByPositionalQuoted(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return sql;
        return Regex.Replace(sql, @"\bORDER\s+BY\s+""(\d+)""\b", "ORDER BY $1", RegexOptions.IgnoreCase);
    }

    private static string QualifyTableRefsWithSchema(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql) || schema?.Tables == null) return sql;
        foreach (var table in schema.Tables)
        {
            if (!table.TableName.Contains('.')) continue;
            var parts = table.TableName.Split('.', 2);
            var schemaPart = parts[0];
            var tablePart = parts[1];
            var fullRef = $"\"{schemaPart}\".\"{tablePart}\"";
            if (!Regex.IsMatch(sql, $@"\b(?:FROM|JOIN)\s+{Regex.Escape(fullRef)}\b", RegexOptions.IgnoreCase))
                continue;
            var unqualifiedPattern = $@"\b{Regex.Escape(tablePart)}\.";
            if (Regex.IsMatch(sql, unqualifiedPattern))
                sql = Regex.Replace(sql, $@"\b{Regex.Escape(tablePart)}\.", $"{fullRef}.", RegexOptions.IgnoreCase);
            var quotedUnqualifiedPattern = $@"""{Regex.Escape(tablePart)}""\.";
            if (Regex.IsMatch(sql, quotedUnqualifiedPattern))
                sql = Regex.Replace(sql, quotedUnqualifiedPattern, $"{fullRef}.", RegexOptions.IgnoreCase);
        }
        return sql;
    }

    private static string NormalizeDoubleQuotedIdentifiers(string sql)
    {
        return Regex.Replace(sql, "\"\"([a-zA-Z0-9_]+)\"\"", "\"$1\"");
    }

    private static string QuoteColumnsForPostgreSql(string sql, DatabaseSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(sql) || schema?.Tables == null)
            return sql;

        var columnsToQuote = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in schema.Tables)
        {
            foreach (var col in table.Columns)
            {
                if (string.IsNullOrEmpty(col.ColumnName)) continue;
                if (!HasUpperCase(col.ColumnName)) continue;
                var key = col.ColumnName;
                if (!columnsToQuote.ContainsKey(key))
                    columnsToQuote[key] = $"\"{col.ColumnName}\"";
            }
        }

        if (columnsToQuote.Count == 0)
            return sql;

        var result = ExcludeStringsAndReplaceIdentifiers(sql, columnsToQuote);
        return result;
    }

    private static string ExcludeStringsAndReplaceIdentifiers(string sql, Dictionary<string, string> replacements)
    {
        var sb = new StringBuilder();
        var i = 0;
        while (i < sql.Length)
        {
            if (sql[i] == '\'' || sql[i] == '"')
            {
                var quote = sql[i];
                sb.Append(sql[i++]);
                while (i < sql.Length)
                {
                    if (sql[i] == quote)
                    {
                        sb.Append(sql[i++]);
                        if (i < sql.Length && sql[i] == quote)
                        {
                            sb.Append(sql[i++]);
                            continue;
                        }
                        break;
                    }
                    sb.Append(sql[i++]);
                }
                continue;
            }

            if (char.IsLetter(sql[i]) || sql[i] == '_')
            {
                var start = i;
                while (i < sql.Length && (char.IsLetterOrDigit(sql[i]) || sql[i] == '_'))
                    i++;
                var word = sql[start..i];
                if (replacements.TryGetValue(word, out var quoted))
                    sb.Append(quoted);
                else
                    sb.Append(word);
                continue;
            }

            sb.Append(sql[i++]);
        }
        return sb.ToString();
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

    /// <summary>
    /// Normalizes PostgreSQL table aliases so that aliases are never quoted.
    /// This avoids case-sensitivity issues where an alias is declared with quotes
    /// (e.g. "Address") but later referenced without quotes (Address), which PostgreSQL
    /// treats as different identifiers and can cause "missing FROM-clause entry" errors.
    /// </summary>
    private static string NormalizeQuotedAliases(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        const string pattern = @"\b(FROM|JOIN)\s+([^\s]+)\s+(AS\s+)?""([A-Za-z_][A-Za-z0-9_]*)""";

        return Regex.Replace(
            sql,
            pattern,
            match =>
            {
                var keyword = match.Groups[1].Value;
                var tableExpression = match.Groups[2].Value;
                var asPart = match.Groups[3].Value;
                var aliasName = match.Groups[4].Value;

                return $"{keyword} {tableExpression} {asPart}{aliasName}";
            },
            RegexOptions.IgnoreCase);
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

