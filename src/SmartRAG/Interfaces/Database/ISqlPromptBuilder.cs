
namespace SmartRAG.Interfaces.Database;


/// <summary>
/// Container for separated system and user prompts
/// </summary>
public class SqlPromptParts
{
    /// <summary>
    /// System message containing schema information (tables, columns, mappings)
    /// </summary>
    public string SystemMessage { get; set; }

    /// <summary>
    /// User message containing rules, examples, and user query
    /// </summary>
    public string UserMessage { get; set; }
}

/// <summary>
/// Interface for building SQL generation prompts
/// </summary>
public interface ISqlPromptBuilder
{
    /// <summary>
    /// Builds separated system and user prompts for better AI context understanding
    /// </summary>
    /// <param name="userQuery">User's natural language query</param>
    /// <param name="queryIntent">Full query intent with all database queries</param>
    /// <param name="schemas">Dictionary of database schemas by database ID</param>
    /// <param name="strategies">Dictionary of SQL dialect strategies by database ID</param>
    /// <returns>SqlPromptParts containing system message (schema) and user message (rules)</returns>
    SqlPromptParts BuildMultiDatabaseSeparated(string userQuery, QueryIntent queryIntent, Dictionary<string, DatabaseSchemaInfo> schemas, Dictionary<string, ISqlDialectStrategy> strategies);
}

