using System.Collections.Generic;

namespace SmartRAG.API.Contracts
{
    /// <summary>
    /// Response from multi-database query
    /// </summary>
    public class MultiDatabaseQueryResponseDto : IDto
    {
        /// <summary>
        /// AI-generated answer
        /// </summary>
        public string Answer { get; set; } = string.Empty;

        /// <summary>
        /// Data sources used
        /// </summary>
        public List<string> Sources { get; set; } = new List<string>();

        /// <summary>
        /// Success status
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Total execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Number of databases queried
        /// </summary>
        public int DatabasesQueried { get; set; }

        /// <summary>
        /// Total rows retrieved
        /// </summary>
        public int TotalRowsRetrieved { get; set; }

        /// <summary>
        /// Query intent analysis (if requested)
        /// </summary>
        public QueryIntentAnalysisResponseDto? QueryAnalysis { get; set; }

        /// <summary>
        /// Any errors that occurred
        /// </summary>
        public List<string>? Errors { get; set; }
    }
}

