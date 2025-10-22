using System.ComponentModel.DataAnnotations;

namespace SmartRAG.API.Contracts
{
    /// <summary>
    /// Request for multi-database intelligent querying
    /// </summary>
    public class MultiDatabaseQueryRequest
    {
        /// <summary>
        /// Natural language query
        /// </summary>
        /// <example>Show me the top selling products and who bought them</example>
        [Required(ErrorMessage = "Query is required")]
        [MinLength(3, ErrorMessage = "Query must be at least 3 characters")]
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Maximum number of results to return
        /// </summary>
        /// <example>10</example>
        [Range(1, 100, ErrorMessage = "Max results must be between 1 and 100")]
        public int MaxResults { get; set; } = 10;

        /// <summary>
        /// Optional: Specific database IDs to query (empty = all databases)
        /// </summary>
        public string[]? DatabaseIds { get; set; }

        /// <summary>
        /// Whether to include query analysis details in response
        /// </summary>
        public bool IncludeQueryAnalysis { get; set; } = false;
    }
}

