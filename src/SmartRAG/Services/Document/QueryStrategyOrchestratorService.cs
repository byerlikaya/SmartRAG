#nullable enable

using SmartRAG.Enums;
using SmartRAG.Interfaces.Document;

namespace SmartRAG.Services.Document
{
    /// <summary>
    /// Service for determining query execution strategy
    /// </summary>
    public class QueryStrategyOrchestratorService : IQueryStrategyOrchestratorService
    {
        private const double HighConfidenceThreshold = 0.7;
        private const double MediumConfidenceMin = 0.3;
        private const double MediumConfidenceMax = 0.7;

        /// <summary>
        /// Determines the appropriate query strategy based on confidence and available data sources
        /// </summary>
        public QueryStrategy DetermineQueryStrategy(double confidence, bool hasDatabaseQueries, bool hasDocumentMatches)
        {
            if (confidence >= HighConfidenceThreshold)
            {
                if (hasDatabaseQueries && hasDocumentMatches)
                    return QueryStrategy.Hybrid;

                if (hasDatabaseQueries)
                    return QueryStrategy.DatabaseOnly;

                return QueryStrategy.DocumentOnly;
            }

            if (confidence >= MediumConfidenceMin && confidence <= MediumConfidenceMax)
                return hasDocumentMatches ? QueryStrategy.Hybrid : QueryStrategy.DatabaseOnly;

            if (hasDocumentMatches)
                return QueryStrategy.DocumentOnly;

            return QueryStrategy.DocumentOnly;
        }
    }
}

