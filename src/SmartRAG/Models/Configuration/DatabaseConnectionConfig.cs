using SmartRAG.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartRAG.Models
{
    /// <summary>
    /// Configuration for a database connection
    /// </summary>
    public class DatabaseConnectionConfig
    {
        /// <summary>
        /// Optional name for the database connection.
        /// If not provided, system will auto-generate from database name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Database connection string
        /// </summary>
        [Required(ErrorMessage = "Connection string is required")]
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Type of database (SQLite, SqlServer, MySQL, PostgreSQL)
        /// </summary>
        [Required(ErrorMessage = "Database type is required")]
        public DatabaseType DatabaseType { get; set; }

        /// <summary>
        /// Whether this connection is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Maximum rows to retrieve per query (default from DatabaseConfig)
        /// </summary>
        public int MaxRowsPerQuery { get; set; }

        /// <summary>
        /// Include/Exclude specific tables (empty = all tables)
        /// </summary>
        public string[] IncludedTables { get; set; }

        /// <summary>
        /// Exclude specific tables from analysis
        /// </summary>
        public string[] ExcludedTables { get; set; }

        /// <summary>
        /// Cross-database column mappings for joining data across different databases
        /// </summary>
        public List<CrossDatabaseMapping> CrossDatabaseMappings { get; set; } = new List<CrossDatabaseMapping>();
    }
}

