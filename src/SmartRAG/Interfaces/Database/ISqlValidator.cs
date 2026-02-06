
namespace SmartRAG.Interfaces.Database;


/// <summary>
/// Interface for SQL query validation
/// </summary>
public interface ISqlValidator
{
    /// <summary>
    /// Validates a SQL query against the database schema
    /// </summary>
    /// <param name="sql">SQL query to validate</param>
    /// <param name="schema">Database schema information</param>
    /// <param name="requiredTables">List of required table names</param>
    /// <param name="allDatabaseNames">List of all database names in the system (for cross-database reference detection). Can be null.</param>
    /// <returns>List of validation errors, empty if valid</returns>
    List<string> ValidateQuery(string sql, DatabaseSchemaInfo schema, List<string> requiredTables, List<string>? allDatabaseNames = null);
}

