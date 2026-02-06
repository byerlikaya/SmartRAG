
namespace SmartRAG.Services.Database.Validation;


/// <summary>
/// Validates SQL queries against database schemas
/// </summary>
public class SqlValidator : ISqlValidator
{
    private readonly ILogger<SqlValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the SqlValidator
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public SqlValidator(ILogger<SqlValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public List<string> ValidateQuery(string sql, DatabaseSchemaInfo schema, List<string> requiredTables, List<string> allDatabaseNames = null)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(sql))
        {
            errors.Add("SQL query is empty");
            return errors;
        }

        var tableErrors = ValidateTables(sql, schema, requiredTables, allDatabaseNames);
        errors.AddRange(tableErrors);

        var columnErrors = ValidateColumns(sql, schema, requiredTables);
        errors.AddRange(columnErrors);

        var syntaxErrors = ValidateSyntax(sql, schema);
        errors.AddRange(syntaxErrors);

        return errors;
    }

    private List<string> ValidateTables(string sql, DatabaseSchemaInfo schema, List<string> requiredTables, List<string> allDatabaseNames = null)
    {
        var errors = new List<string>();

        var threePartPatterns = new[]
        {
            @"(?:FROM|JOIN)\s+(\[?[a-zA-Z0-9_]+\]?\.\[?[a-zA-Z0-9_]+\]?\.\[?[a-zA-Z0-9_]+\]?)",
            @"(?:FROM|JOIN)\s+(""?[a-zA-Z0-9_]+""?\.""?[a-zA-Z0-9_]+""?\.""?[a-zA-Z0-9_]+""?)",
            @"(?:WHERE|IN|EXISTS|NOT\s+EXISTS)[\s\S]*?FROM\s+(\[?[a-zA-Z0-9_]+\]?\.\[?[a-zA-Z0-9_]+\]?\.\[?[a-zA-Z0-9_]+\]?)",
            @"(?:WHERE|IN|EXISTS|NOT\s+EXISTS)[\s\S]*?FROM\s+(""?[a-zA-Z0-9_]+""?\.""?[a-zA-Z0-9_]+""?\.""?[a-zA-Z0-9_]+""?)"
        };

        foreach (var pattern in threePartPatterns)
        {
            var matches = Regex.Matches(sql, pattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                var fullTableName = match.Groups[1].Value;
                var normalized = fullTableName
                    .Replace("[", string.Empty)
                    .Replace("]", string.Empty)
                    .Replace("\"", string.Empty);
                var parts = normalized.Split('.');
                
                if (parts.Length == 3)
                {
                    var databaseName = parts[0];
                    if (!schema.DatabaseName.Equals(databaseName, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"Cross-database reference not allowed: '{fullTableName}'. Use only tables from '{schema.DatabaseName}' database.");
                        _logger.LogWarning(
                            "Detected cross-database table reference: {Reference} in database {Database}",
                            fullTableName, schema.DatabaseName);
                    }
                }
            }
        }

        var allTablePatterns = new[]
        {
            @"(?:FROM|JOIN)\s+(\[?[a-zA-Z0-9_]+\]?(?:\]?\.\[?[a-zA-Z0-9_]+\]?)?)",
            @"(?:WHERE|IN|EXISTS|NOT\s+EXISTS)[\s\S]*?FROM\s+(\[?[a-zA-Z0-9_]+\]?(?:\]?\.\[?[a-zA-Z0-9_]+\]?)?)",
            @"SELECT[\s\S]*?FROM\s+(\[?[a-zA-Z0-9_]+\]?(?:\]?\.\[?[a-zA-Z0-9_]+\]?)?)",
            @"(?:FROM|JOIN)\s+(""?[a-zA-Z0-9_]+""?(?:\.""?[a-zA-Z0-9_]+""?)?)",
            @"(?:WHERE|IN|EXISTS|NOT\s+EXISTS)[\s\S]*?FROM\s+(""?[a-zA-Z0-9_]+""?(?:\.""?[a-zA-Z0-9_]+""?)?)",
            @"SELECT[\s\S]*?FROM\s+(""?[a-zA-Z0-9_]+""?(?:\.""?[a-zA-Z0-9_]+""?)?)"
        };

        var allTableMatches = new HashSet<string>();

        foreach (var pattern in allTablePatterns)
        {
            var matches = Regex.Matches(sql, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1 && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                {
                    allTableMatches.Add(match.Groups[1].Value);
                }
            }
        }

        foreach (var tableNameRaw in allTableMatches)
        {
            if (tableNameRaw.Split('.').Length == 3)
                continue;
            
            var tableName = NormalizeTableName(tableNameRaw);

            if (IsSqlKeyword(tableName)) continue;

            var normalizedParts = tableName.Split('.');
            if (normalizedParts.Length == 2 && allDatabaseNames != null && allDatabaseNames.Count > 1)
            {
                var schemaPart = normalizedParts[0];
                foreach (var otherDbName in allDatabaseNames)
                {
                    if (otherDbName.Equals(schema.DatabaseName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (otherDbName.Contains(schemaPart, StringComparison.OrdinalIgnoreCase) ||
                        schemaPart.Equals(otherDbName, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"Cross-database reference detected: '{tableNameRaw}' appears to reference schema or database '{otherDbName}', but you are querying '{schema.DatabaseName}' database. Use ONLY tables from '{schema.DatabaseName}' database.");
                        _logger.LogWarning(
                            "Detected potential cross-database reference: {Reference} in database {Database}, possibly referencing {OtherDatabase}",
                            tableNameRaw, schema.DatabaseName, otherDbName);
                        break;
                    }
                }
            }

            if (schema.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
            {
                var exactMatch = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.Ordinal));
                if (exactMatch != null)
                {
                    var exactTableName = exactMatch.TableName;
                    if (!requiredTables.Any(t => t.Equals(exactTableName, StringComparison.OrdinalIgnoreCase)))
                    {
                        _logger.LogWarning(
                            "Table '{Table}' exists in database '{Database}' but was not in the required tables list.",
                            exactTableName, schema.DatabaseName);
                    }
                    continue;
                }

                var caseInsensitiveMatch = schema.Tables.FirstOrDefault(t => 
                    t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                
                if (caseInsensitiveMatch != null)
                {
                    errors.Add($"Table '{tableNameRaw}' case mismatch in PostgreSQL. Use exact case: '{caseInsensitiveMatch.TableName}'");
                    continue;
                }
            }

            var tableExists = schema.Tables.Any(t => 
                t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                t.TableName.Equals(tableNameRaw, StringComparison.OrdinalIgnoreCase));
            if (!tableExists)
            {
                errors.Add($"Table '{tableNameRaw}' does not exist in database '{schema.DatabaseName}'");
                continue;
            }

            var matchedTable = schema.Tables.FirstOrDefault(t => 
                t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                t.TableName.Equals(tableNameRaw, StringComparison.OrdinalIgnoreCase));
            var tableNameForRequiredCheck = matchedTable?.TableName ?? tableName;

            if (!requiredTables.Contains(tableNameForRequiredCheck, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Table '{Table}' exists in database '{Database}' but was not in the required tables list. " +
                    "Allowing query to proceed. This may indicate QueryIntentAnalyzer needs improvement.",
                    tableNameForRequiredCheck, schema.DatabaseName);

            }
        }

        return errors;
    }

    private List<string> ValidateColumns(string sql, DatabaseSchemaInfo schema, List<string> requiredTables)
    {
        var errors = new List<string>();

        var aliasToTable = ExtractTableAliases(sql);

        var columnMatches = Regex.Matches(sql, @"\b([a-zA-Z0-9_]+)\.([a-zA-Z0-9_]+)\b", RegexOptions.IgnoreCase);

        foreach (Match match in columnMatches)
        {
            var prefix = match.Groups[1].Value;
            var columnName = match.Groups[2].Value;

            if (columnName.Equals("*", StringComparison.OrdinalIgnoreCase)) continue;

            string tableName = prefix;

            if (aliasToTable.ContainsKey(prefix))
            {
                tableName = aliasToTable[prefix];
            }

            var table = schema.Tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));

            if (table == null) continue;

            if (!requiredTables.Contains(table.TableName, StringComparer.OrdinalIgnoreCase)) continue;

            // For PostgreSQL, check case-sensitive matching first
            if (schema.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
            {
                var exactMatch = table.Columns.FirstOrDefault(c => c.ColumnName.Equals(columnName, StringComparison.Ordinal));
                if (exactMatch != null)
                    continue;

                var caseInsensitiveMatch = table.Columns.FirstOrDefault(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));
                if (caseInsensitiveMatch != null)
                {
                    var aliasInfo = aliasToTable.ContainsKey(prefix) ? $" (via alias '{prefix}')" : "";
                    errors.Add($"Column '{columnName}' case mismatch in PostgreSQL. Use exact case: '{caseInsensitiveMatch.ColumnName}' in table '{tableName}'{aliasInfo}");
                    continue;
                }
            }

            var columnExists = table.Columns.Any(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));

            if (!columnExists)
            {
                var aliasInfo = aliasToTable.ContainsKey(prefix) ? $" (via alias '{prefix}')" : "";
                errors.Add($"Column '{columnName}' does not exist in table '{tableName}'{aliasInfo}");
            }
        }

        return errors;
    }

    private List<string> ValidateSyntax(string sql, DatabaseSchemaInfo schema)
    {
        var errors = new List<string>();

        if (Regex.IsMatch(sql, @"\[values from previous database results\]", RegexOptions.IgnoreCase))
        {
            errors.Add("SQL contains placeholder text '[values from previous database results]'. SQL must be executable with literal values only.");
        }

        var placeholderMatch = Regex.Match(sql, @"\[([a-zA-Z\s]+)\]");
        if (placeholderMatch.Success && placeholderMatch.Groups[1].Value.Contains(" "))
        {
            errors.Add($"SQL contains placeholder text '[{placeholderMatch.Groups[1].Value}]'. Use only literal values and actual table/column names.");
        }

        if (sql.Contains("CROSS JOIN", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("CROSS JOIN is not allowed. Use explicit INNER JOIN or LEFT JOIN.");
        }

        var selectCount = Regex.Matches(sql, @"SELECT\s", RegexOptions.IgnoreCase).Count;

        if (selectCount > 2)
        {
            errors.Add("Too many nested subqueries (max 2 levels allowed).");
        }

        if (schema.DatabaseType == SmartRAG.Enums.DatabaseType.PostgreSQL)
        {
            if (Regex.IsMatch(sql, @"\bSELECT\s+TOP\s+\d+\b", RegexOptions.IgnoreCase))
            {
                errors.Add("PostgreSQL does not support TOP. Use LIMIT at the end of the query instead (e.g., ORDER BY ... LIMIT 5).");
            }
        }

        if (schema.DatabaseType == SmartRAG.Enums.DatabaseType.SqlServer)
        {
            if (Regex.IsMatch(sql, @"\bLIMIT\s+\d+", RegexOptions.IgnoreCase) && 
                !sql.Contains("FETCH", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("SQL Server does not support LIMIT. Use TOP N immediately after SELECT instead (e.g., SELECT TOP 5 ...).");
            }
        }

        return errors;
    }

    private bool IsSqlKeyword(string word)
    {
        var keywords = new[] { "SELECT", "FROM", "WHERE", "JOIN", "ON", "AND", "OR", "GROUP", "BY", "ORDER", "LIMIT", "TOP", "AS", "LEFT", "RIGHT", "INNER", "OUTER" };
        return keywords.Contains(word.ToUpperInvariant());
    }

    private string NormalizeTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return tableName;

        var normalized = tableName
            .Replace("[", "")
            .Replace("]", "")
            .Replace("\"", "")
            .Replace("`", "")
            .Trim();

        return normalized;
    }

    private Dictionary<string, string> ExtractTableAliases(string sql)
    {
        var aliasToTable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var aliasMatches = Regex.Matches(sql,
            @"(?:FROM|JOIN)\s+(\[?[a-zA-Z0-9_]+\]?(?:\]?\.\[?[a-zA-Z0-9_]+\]?)?)(?:\s+(?:AS\s+)?([a-zA-Z0-9_]+))?",
            RegexOptions.IgnoreCase);

        foreach (Match match in aliasMatches)
        {
            var tableNameRaw = match.Groups[1].Value;
            var tableName = NormalizeTableName(tableNameRaw);
            var alias = match.Groups[2].Value;

            if (!string.IsNullOrWhiteSpace(alias) && !IsSqlKeyword(alias))
            {
                aliasToTable[alias] = tableName;
            }
        }

        return aliasToTable;
    }
}

