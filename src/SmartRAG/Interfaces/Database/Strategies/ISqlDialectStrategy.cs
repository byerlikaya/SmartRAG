namespace SmartRAG.Interfaces.Database.Strategies;


/// <summary>
/// Strategy interface for database-specific SQL generation and validation
/// </summary>
public interface ISqlDialectStrategy
{
    /// <summary>
    /// Gets the database type this strategy supports
    /// </summary>
    DatabaseType DatabaseType { get; }

    /// <summary>
    /// Validates the syntax of the generated SQL
    /// </summary>
    bool ValidateSyntax(string sql, out string errorMessage);

    /// <summary>
    /// Formats the SQL query according to dialect rules.
    /// Schema is optional; some dialects (e.g. PostgreSQL) use it for column quoting.
    /// </summary>
    string FormatSql(string sql, DatabaseSchemaInfo? schema = null);
}

