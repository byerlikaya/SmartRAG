namespace SmartRAG.Enums;


/// <summary>
/// Strategy for query execution
/// </summary>
public enum QueryStrategy
{
    /// <summary>
    /// Execute database query only
    /// </summary>
    DatabaseOnly,

    /// <summary>
    /// Execute document query only
    /// </summary>
    DocumentOnly,

    /// <summary>
    /// Execute both database and document queries
    /// </summary>
    Hybrid
}


