namespace SmartRAG.Models
{
    /// <summary>
    /// Results from a single database query
    /// </summary>
    public class DatabaseQueryResult
    {
        /// <summary>
        /// Database ID
        /// </summary>
        public string DatabaseId { get; set; } = string.Empty;

        /// <summary>
        /// Database name
        /// </summary>
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
        /// Query that was executed
        /// </summary>
        public string ExecutedQuery { get; set; } = string.Empty;

        /// <summary>
        /// Query results as formatted text
        /// </summary>
        public string ResultData { get; set; } = string.Empty;

        /// <summary>
        /// Number of rows returned
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// Success status
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if any
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }
    }
}

