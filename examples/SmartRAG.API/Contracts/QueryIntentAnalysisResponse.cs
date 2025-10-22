using System.Collections.Generic;

namespace SmartRAG.API.Contracts
{
    /// <summary>
    /// Response containing AI analysis of query intent
    /// </summary>
    public class QueryIntentAnalysisResponseDto : IDto
    {
        /// <summary>
        /// Original user query
        /// </summary>
        public string OriginalQuery { get; set; } = string.Empty;

        /// <summary>
        /// AI's understanding of the query
        /// </summary>
        public string QueryUnderstanding { get; set; } = string.Empty;

        /// <summary>
        /// Confidence level (0-1)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Whether cross-database join is needed
        /// </summary>
        public bool RequiresCrossDatabaseJoin { get; set; }

        /// <summary>
        /// AI reasoning for the query plan
        /// </summary>
        public string? Reasoning { get; set; }

        /// <summary>
        /// Databases that will be queried
        /// </summary>
        public List<DatabaseQueryPlanDto> DatabaseQueries { get; set; } = new List<DatabaseQueryPlanDto>();
    }

    /// <summary>
    /// Query plan for a specific database
    /// </summary>
    public class DatabaseQueryPlanDto
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
        /// Tables that will be queried
        /// </summary>
        public List<string> RequiredTables { get; set; } = new List<string>();

        /// <summary>
        /// Generated SQL query
        /// </summary>
        public string? GeneratedQuery { get; set; }

        /// <summary>
        /// Purpose of this query
        /// </summary>
        public string? Purpose { get; set; }

        /// <summary>
        /// Priority (higher = more important)
        /// </summary>
        public int Priority { get; set; }
    }
}

