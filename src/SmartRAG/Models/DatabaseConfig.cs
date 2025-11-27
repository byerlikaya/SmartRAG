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
        #region Constants

        private const int DefaultMaxRowsPerTable = 1000;
        private const int DefaultQueryTimeout = 30;
        private const int MinMaxRows = 1;
        private const int MaxMaxRows = 10000;
        private const int MinTimeout = 1;
        private const int MaxTimeout = 300;

        #endregion

        #region Properties

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
        /// Whether to include index information
        /// </summary>
        public bool IncludeIndexes { get; set; } = false;

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

        /// <summary>
        /// Enable connection pooling for better performance
        /// </summary>
        public bool EnableConnectionPooling { get; set; } = true;

        /// <summary>
        /// Maximum number of connections in the pool
        /// </summary>
        [Range(1, 100)]
        public int MaxPoolSize { get; set; } = 10;

        /// <summary>
        /// Minimum number of connections in the pool
        /// </summary>
        [Range(1, 50)]
        public int MinPoolSize { get; set; } = 2;

        /// <summary>
        /// Enable query result caching
        /// </summary>
        public bool EnableQueryCaching { get; set; } = true;

        /// <summary>
        /// Cache duration in minutes
        /// </summary>
        [Range(1, 1440)]
        public int CacheDurationMinutes { get; set; } = 30;

        /// <summary>
        /// Enable parallel table processing
        /// </summary>
        public bool EnableParallelProcessing { get; set; } = true;

        /// <summary>
        /// Maximum degree of parallelism for table processing
        /// </summary>
        [Range(1, 10)]
        public int MaxDegreeOfParallelism { get; set; } = 3;

        /// <summary>
        /// Enable streaming for large datasets
        /// </summary>
        public bool EnableStreaming { get; set; } = true;

        /// <summary>
        /// Batch size for streaming operations
        /// </summary>
        [Range(100, 10000)]
        public int StreamingBatchSize { get; set; } = 1000;

        /// <summary>
        /// Maximum memory usage threshold in MB before triggering streaming mode
        /// </summary>
        [Range(50, 2048)]
        public int MaxMemoryThresholdMB { get; set; } = 500;

        /// <summary>
        /// Enable automatic garbage collection for large operations
        /// </summary>
        public bool EnableAutoGarbageCollection { get; set; } = true;

        /// <summary>
        /// Force streaming mode regardless of data size
        /// </summary>
        public bool ForceStreamingMode { get; set; } = false;

        /// <summary>
        /// Maximum string builder capacity to prevent excessive memory allocation
        /// </summary>
        [Range(1024, 1048576)]
        public int MaxStringBuilderCapacity { get; set; } = 65536; // 64KB

        #endregion
    }
}
