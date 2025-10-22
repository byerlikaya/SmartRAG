using SmartRAG.Enums;
using System;
using System.Collections.Generic;

namespace SmartRAG.API.Contracts
{
    /// <summary>
    /// Response model for database operations
    /// </summary>
    public class DatabaseResponse
    {
        /// <summary>
        /// Success status of the operation
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }

        /// <summary>
        /// Response message
        /// </summary>
        /// <example>Database connected successfully</example>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Extracted database content
        /// </summary>
        /// <example>=== SQL Server Database Content ===\nDatabase: Northwind\nServer: localhost\n\n--- Table: Customers ---\n...</example>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Database type that was processed
        /// </summary>
        /// <example>SqlServer</example>
        public DatabaseType DatabaseType { get; set; }

        /// <summary>
        /// Number of tables processed
        /// </summary>
        /// <example>5</example>
        public int TablesProcessed { get; set; }

        /// <summary>
        /// Total number of rows extracted
        /// </summary>
        /// <example>2500</example>
        public int TotalRowsExtracted { get; set; }

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        /// <example>1250</example>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Timestamp when the operation was completed
        /// </summary>
        /// <example>2025-01-16T14:30:00Z</example>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// List of any errors or warnings encountered during processing
        /// </summary>
        /// <example>["Warning: Table 'LargeTable' truncated to 1000 rows"]</example>
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
