using SmartRAG.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartRAG.Models
{
    /// <summary>
    /// Configuration for database parsing operations
    /// </summary>
    public class DatabaseConfig
    {
        private const int DefaultMaxRowsPerTable = 1000;
        private const int DefaultQueryTimeout = 30;
        private const int MinMaxRows = 1;
        private const int MaxMaxRows = 10000;
        private const int MinTimeout = 1;
        private const int MaxTimeout = 300;

        /// <summary>
        /// Type of database to connect to
        /// </summary>
        public DatabaseType Type { get; set; } = DatabaseType.SQLite;

        /// <summary>
        /// Database connection string
        /// </summary>
        [Required]
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// List of tables to include in extraction (empty means all tables)
        /// </summary>
        public List<string> IncludedTables { get; set; } = new List<string>();

        /// <summary>
        /// List of tables to exclude from extraction
        /// </summary>
        public List<string> ExcludedTables { get; set; } = new List<string>();

        /// <summary>
        /// Maximum number of rows to extract per table
        /// </summary>
        [Range(MinMaxRows, MaxMaxRows)]
        public int MaxRowsPerTable { get; set; } = DefaultMaxRowsPerTable;

        /// <summary>
        /// Whether to include table schema information
        /// </summary>
        public bool IncludeSchema { get; set; } = true;

        /// <summary>
        /// Whether to include foreign key relationships
        /// </summary>
        public bool IncludeForeignKeys { get; set; } = true;

        /// <summary>
        /// Query timeout in seconds
        /// </summary>
        [Range(MinTimeout, MaxTimeout)]
        public int QueryTimeoutSeconds { get; set; } = DefaultQueryTimeout;

        /// <summary>
        /// Whether to sanitize sensitive data (replace with placeholders)
        /// </summary>
        public bool SanitizeSensitiveData { get; set; } = true;

        /// <summary>
        /// List of column name patterns that contain sensitive data
        /// These are generic security patterns applicable to any domain
        /// </summary>
        public List<string> SensitiveColumns { get; set; } = new List<string>
        {
            "password", "pwd", "pass", "secret", "token", "key",
            "ssn", "social_security", "social_security_number",
            "credit_card", "creditcard", "cc_number", "card_number",
            "email", "email_address", "phone", "phone_number", "mobile"
        };
    }
}
