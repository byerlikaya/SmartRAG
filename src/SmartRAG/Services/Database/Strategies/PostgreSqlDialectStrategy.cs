namespace SmartRAG.Services.Database.Strategies;


public class PostgreSqlDialectStrategy : BaseSqlDialectStrategy
{
    public override DatabaseType DatabaseType => DatabaseType.PostgreSQL;
    public override string FormatSql(string sql, DatabaseSchemaInfo? schema = null)
    {
        if (string.IsNullOrWhiteSpace(sql)) return sql;

        var formatted = base.FormatSql(sql, schema);
        formatted = QuotePostgreSqlTableNames(formatted);
        if (schema != null)
            formatted = QuoteColumnsForPostgreSql(formatted, schema);
        return formatted;
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

