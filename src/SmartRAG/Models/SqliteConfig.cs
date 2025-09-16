namespace SmartRAG.Models
{
    /// <summary>
    /// SQLite storage configuration
    /// </summary>
    public class SqliteConfig
    {
        /// <summary>
        /// Path to the SQLite database file
        /// </summary>
        public string DatabasePath { get; set; } = "SmartRag.db";

        /// <summary>
        /// Whether to enable foreign key constraints for data integrity
        /// </summary>
        public bool EnableForeignKeys { get; set; } = true;
    }
}
