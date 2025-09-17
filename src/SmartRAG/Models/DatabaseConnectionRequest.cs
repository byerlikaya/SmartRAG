using SmartRAG.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartRAG.Models
{
    /// <summary>
    /// Request model for database connection operations
    /// </summary>
    public class DatabaseConnectionRequest
    {
        #region Properties

        /// <summary>
        /// Database connection string
        /// </summary>
        [Required]
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Type of database
        /// </summary>
        public DatabaseType DatabaseType { get; set; } = DatabaseType.SQLite;

        /// <summary>
        /// List of specific tables to include (optional)
        /// </summary>
        public List<string> IncludedTables { get; set; }

        /// <summary>
        /// List of tables to exclude (optional)
        /// </summary>
        public List<string> ExcludedTables { get; set; }

        /// <summary>
        /// Maximum number of rows to extract per table
        /// </summary>
        [Range(1, 10000)]
        public int MaxRows { get; set; } = 1000;

        /// <summary>
        /// Whether to include schema information
        /// </summary>
        public bool IncludeSchema { get; set; } = true;

        /// <summary>
        /// Whether to include foreign key relationships
        /// </summary>
        public bool IncludeForeignKeys { get; set; } = true;

        /// <summary>
        /// Whether to sanitize sensitive data
        /// </summary>
        public bool SanitizeSensitiveData { get; set; } = true;

        #endregion
    }
}
