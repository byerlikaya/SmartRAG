#nullable enable

using SmartRAG.Interfaces.Document;
using System;

namespace SmartRAG.Services.Document;


/// <summary>
/// Service for analyzing queries and determining search parameters
/// </summary>
public class QueryAnalysisService : IQueryAnalysisService
{
    private const int ComprehensiveSearchMultiplier = 3;

    private readonly IQueryPatternAnalyzerService _queryPatternAnalyzer;

    /// <summary>
    /// Initializes a new instance of the QueryAnalysisService
    /// </summary>
    /// <param name="queryPatternAnalyzer">Service for analyzing query patterns</param>
    public QueryAnalysisService(IQueryPatternAnalyzerService queryPatternAnalyzer)
    {
        _queryPatternAnalyzer = queryPatternAnalyzer ?? throw new ArgumentNullException(nameof(queryPatternAnalyzer));
    }

    /// <summary>
    /// Determines initial search count based on query characteristics
    /// </summary>
    public int DetermineInitialSearchCount(string query, int defaultMaxResults)
    {
        if (_queryPatternAnalyzer.RequiresComprehensiveSearch(query))
        {
            return defaultMaxResults * ComprehensiveSearchMultiplier;
        }

        return defaultMaxResults;
    }
}


