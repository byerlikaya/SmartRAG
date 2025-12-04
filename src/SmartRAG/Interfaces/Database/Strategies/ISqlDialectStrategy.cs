using SmartRAG.Enums;
using SmartRAG.Models;

namespace SmartRAG.Interfaces.Database.Strategies
{
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
        /// Builds the system prompt for SQL generation specific to this dialect
        /// </summary>
        string BuildSystemPrompt(DatabaseSchemaInfo schema, string userQuery);

        /// <summary>
        /// Validates the syntax of the generated SQL
        /// </summary>
        bool ValidateSyntax(string sql, out string errorMessage);

        /// <summary>
        /// Formats the SQL query according to dialect rules
        /// </summary>
        string FormatSql(string sql);

        /// <summary>
        /// Gets the limit clause format for this dialect
        /// </summary>
        string GetLimitClause(int limit);
    }
}
