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
            // High confidence: AI is very sure about query intent
            if (confidence >= HighConfidenceThreshold)
            {
                if (hasDatabaseQueries && hasDocumentMatches)
                    return QueryStrategy.Hybrid;

                if (hasDatabaseQueries)
                    return QueryStrategy.DatabaseOnly;

                return QueryStrategy.DocumentOnly;
            }

            // Medium confidence: Check both sources when possible
            if (confidence >= MediumConfidenceMin && confidence <= MediumConfidenceMax)
            {
                if (hasDatabaseQueries && hasDocumentMatches)
                    return QueryStrategy.Hybrid;

                if (hasDatabaseQueries)
                    return QueryStrategy.DatabaseOnly;

                return hasDocumentMatches ? QueryStrategy.DocumentOnly : QueryStrategy.DatabaseOnly;
            }

            // Low confidence: Still check database if queries are available
            if (hasDatabaseQueries && hasDocumentMatches)
                return QueryStrategy.Hybrid;

            if (hasDatabaseQueries)
                return QueryStrategy.DatabaseOnly;

            return QueryStrategy.DocumentOnly;
        }
    }
}

