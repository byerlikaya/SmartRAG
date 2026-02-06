using System.Collections.Generic;

namespace SmartRAG.Models.RequestResponse;


/// <summary>
/// Represents AI-analyzed query intent for multi-database querying
/// </summary>
public class QueryIntent
{
    /// <summary>
    /// Original user query
    /// </summary>
    public string OriginalQuery { get; set; } = string.Empty;

    /// <summary>
    /// AI's understanding of the query
    /// </summary>
    public string QueryUnderstanding { get; set; } = string.Empty;

    /// <summary>
    /// Required database queries
    /// </summary>
    public List<DatabaseQueryIntent> DatabaseQueries { get; set; } = new List<DatabaseQueryIntent>();

    /// <summary>
    /// Confidence level (0-1)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Whether cross-database join is needed
    /// </summary>
    public bool RequiresCrossDatabaseJoin { get; set; }

    /// <summary>
    /// AI reasoning for the query plan
    /// </summary>
    public string Reasoning { get; set; }
}

/// <summary>
/// Query intent for a specific database
/// </summary>
public class DatabaseQueryIntent
{
    /// <summary>
    /// Database ID to query
    /// </summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>
    /// Database name
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Tables to query
    /// </summary>
    public List<string> RequiredTables { get; set; } = new List<string>();

    /// <summary>
    /// Generated SQL query
    /// </summary>
    public string GeneratedQuery { get; set; }

    /// <summary>
    /// Purpose of this query
    /// </summary>
    public string Purpose { get; set; }

    /// <summary>
    /// Priority (higher = more important)
    /// </summary>
    public int Priority { get; set; } = 1;
}


