namespace SmartRAG.Models.Configuration;


/// <summary>
/// SQLite storage configuration
/// </summary>
public class SqliteConfig
{
    /// <summary>
    /// Path to the SQLite database file
    /// </summary>
    public string DatabasePath { get; set; } = "SmartRag.db";
}

