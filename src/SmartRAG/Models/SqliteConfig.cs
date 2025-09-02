namespace SmartRAG.Models
{

    /// <summary>
    /// SQLite storage configuration
    /// </summary>
    public class SqliteConfig
    {
        public string DatabasePath { get; set; } = "SmartRag.db";

        public bool EnableForeignKeys { get; set; } = true;
    }
}
