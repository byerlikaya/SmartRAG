using SmartRAG.Interfaces.Database.Strategies;
using SmartRAG.Models;

namespace SmartRAG.Interfaces.Database
{
    /// <summary>
    /// Interface for building SQL generation prompts
    /// </summary>
    public interface ISqlPromptBuilder
    {
        /// <summary>
        /// Builds a prompt for SQL query generation
        /// </summary>
        /// <param name="userQuery">User's natural language query</param>
        /// <param name="dbQuery">Database query intent</param>
        /// <param name="schema">Database schema information</param>
        /// <param name="strategy">SQL dialect strategy</param>
        /// <param name="fullQueryIntent">Full query intent for cross-database context</param>
        /// <returns>Formatted prompt string for AI</returns>
        string Build(string userQuery, DatabaseQueryIntent dbQuery, DatabaseSchemaInfo schema, ISqlDialectStrategy strategy, QueryIntent fullQueryIntent = null);
    }
}
