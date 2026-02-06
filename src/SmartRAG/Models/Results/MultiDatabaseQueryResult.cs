namespace SmartRAG.Models.Results;


/// <summary>
/// Results from multi-database query execution
/// </summary>
public class MultiDatabaseQueryResult
{
    /// <summary>
    /// Results per database
    /// </summary>
    public Dictionary<string, DatabaseQueryResult> DatabaseResults { get; set; } = new Dictionary<string, DatabaseQueryResult>();

    /// <summary>
    /// Overall success status
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Any errors encountered
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();

    /// <summary>
    /// Total execution time in milliseconds
    /// </summary>
    public long ExecutionTimeMs { get; set; }
}


