
namespace SmartRAG.Services.Document;


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
        switch (confidence)
        {
            // High confidence: AI is very sure about query intent
            case >= HighConfidenceThreshold when hasDatabaseQueries && hasDocumentMatches:
                return QueryStrategy.Hybrid;
            case >= HighConfidenceThreshold:
                return hasDatabaseQueries ? QueryStrategy.DatabaseOnly : QueryStrategy.DocumentOnly;
            // Medium confidence: Check both sources when possible
            case >= MediumConfidenceMin and <= MediumConfidenceMax when hasDatabaseQueries && hasDocumentMatches:
                return QueryStrategy.Hybrid;
            case >= MediumConfidenceMin and <= MediumConfidenceMax when hasDatabaseQueries:
                return QueryStrategy.DatabaseOnly;
            case >= MediumConfidenceMin and <= MediumConfidenceMax:
                return hasDocumentMatches ? QueryStrategy.DocumentOnly : QueryStrategy.DatabaseOnly;
        }

        return hasDatabaseQueries switch
        {
            // Low confidence: Still check database if queries are available
            true when hasDocumentMatches => QueryStrategy.Hybrid,
            true => QueryStrategy.DatabaseOnly,
            _ => QueryStrategy.DocumentOnly
        };
    }
}


