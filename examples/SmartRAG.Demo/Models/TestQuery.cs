namespace SmartRAG.Demo.Models;

/// <summary>
/// Represents a test query for multi-database testing
/// </summary>
public class TestQuery
{
    /// <summary>
    /// Category of the test query (with emoji)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// The actual query text
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Database names involved in the query
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Database types involved in the query
    /// </summary>
    public string DatabaseTypes { get; set; } = string.Empty;
}

