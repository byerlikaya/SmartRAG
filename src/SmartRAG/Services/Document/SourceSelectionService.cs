#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Interfaces.Document;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services.Document
{
    /// <summary>
    /// Service for determining whether document search results are sufficient to skip other sources
    /// </summary>
    public class SourceSelectionService : ISourceSelectionService
    {
        #region Constants

        private const double VectorSearchThreshold = 0.8;
        private const double TextSearchThreshold = 5.0;
        private const double ScoreTypeBoundary = 3.0;
        private const int DefaultMinResultsForEarlyExit = 1;
        private const double MinScoreRangeForEarlyExit = 0.3;
        private const int TopResultsToCheck = 5;
        private const double HighConfidenceScoreMargin = 0.2; // Reduced from 0.3 to allow early exit more readily
        private const double MinScoreAboveThreshold = 0.1; // Reduced from 0.15 to allow early exit for image results and close scores
        private const double Epsilon = 0.0001; // For floating point comparison

        #endregion

        #region Fields

        private readonly ILogger<SourceSelectionService> _logger;
        private readonly SmartRagOptions _options;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the SourceSelectionService
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="options">SmartRAG configuration options</param>
        public SourceSelectionService(
            ILogger<SourceSelectionService> logger,
            IOptions<SmartRagOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Determines if other sources should be skipped based on document search results
        /// </summary>
        public Task<bool> ShouldSkipOtherSourcesAsync(bool canAnswer, List<DocumentChunk> results, double? minRelevanceScore = null)
        {
            if (!canAnswer)
            {
                _logger.LogDebug("Cannot answer from documents, other sources needed");
                return Task.FromResult(false);
            }

            if (results == null || results.Count == 0)
            {
                _logger.LogDebug("No document results available, other sources needed");
                return Task.FromResult(false);
            }

            var config = _options.Features.SourceSelection;
            if (!config.EnableEarlyExit)
            {
                _logger.LogDebug("Early exit disabled in configuration, checking all sources");
                return Task.FromResult(false);
            }

            var scores = results
                .Where(r => r.RelevanceScore.HasValue)
                .Select(r => r.RelevanceScore!.Value)
                .OrderByDescending(s => s)
                .ToList();

            if (scores.Count == 0)
            {
                _logger.LogDebug("No relevance scores available, checking other sources");
                return Task.FromResult(false);
            }

            var topScore = scores[0];
            var threshold = minRelevanceScore ?? DetermineAdaptiveThreshold(topScore);
            var minResults = config.MinResultsForEarlyExit;

            var highQualityResults = scores.Count(s => s >= threshold);

            var topResults = scores.Take(TopResultsToCheck).ToList();
            var scoreRange = topResults.Count > 1 ? topResults[0] - topResults[topResults.Count - 1] : 0.0;

            // Check if results contain images - images with OCR content should be considered for early exit
            var hasImageResults = results.Any(r => 
                string.Equals(r.DocumentType, "Image", StringComparison.OrdinalIgnoreCase) && 
                r.RelevanceScore.HasValue && 
                r.RelevanceScore.Value >= threshold);

            var shouldSkip = highQualityResults >= minResults;

            if (shouldSkip && topScore > ScoreTypeBoundary)
            {
                var topResultsCount = topResults.Count(s => s >= threshold);
                var scoreAboveThreshold = topScore - threshold;
                
                if (topResultsCount < Math.Min(minResults, TopResultsToCheck))
                {
                    _logger.LogDebug(
                        "Top {TopCount} results don't meet threshold ({TopCountWithThreshold}/{TopCount} >= {Threshold:F4}), checking other sources",
                        TopResultsToCheck, topResultsCount, TopResultsToCheck, threshold);
                    shouldSkip = false;
                }
                else if (scoreRange < MinScoreRangeForEarlyExit && scores.Count > 1)
                {
                    // If results contain images with OCR content, be more lenient with early exit
                    if (hasImageResults && scoreAboveThreshold >= MinScoreAboveThreshold - Epsilon)
                    {
                        _logger.LogDebug(
                            "Score range narrow ({Range:F4} < {MinRange:F4}) but image results found and top score ({TopScore:F4}) is {ScoreAbove:F4} above threshold ({Threshold:F4}), allowing early exit",
                            scoreRange, MinScoreRangeForEarlyExit, topScore, scoreAboveThreshold, threshold);
                    }
                    // If top score is significantly above threshold, allow early exit despite narrow range
                    else if (scoreAboveThreshold >= MinScoreAboveThreshold - Epsilon)
                    {
                        _logger.LogDebug(
                            "Score range narrow ({Range:F4} < {MinRange:F4}) but top score ({TopScore:F4}) is {ScoreAbove:F4} above threshold ({Threshold:F4}), allowing early exit",
                            scoreRange, MinScoreRangeForEarlyExit, topScore, scoreAboveThreshold, threshold);
                    }
                    else
                    {
                        // Check high-confidence threshold as fallback
                        var highConfidenceThreshold = threshold + HighConfidenceScoreMargin;
                        if (topScore >= highConfidenceThreshold - Epsilon)
                        {
                            _logger.LogDebug(
                                "Score range narrow ({Range:F4} < {MinRange:F4}) but top score ({TopScore:F4}) exceeds high-confidence threshold ({HighConfidenceThreshold:F4}), allowing early exit",
                                scoreRange, MinScoreRangeForEarlyExit, topScore, highConfidenceThreshold);
                        }
                        else
                        {
                            _logger.LogDebug(
                                "Score range too narrow ({Range:F4} < {MinRange:F4}) and top score ({TopScore:F4}) not sufficiently above threshold ({Threshold:F4}, margin: {ScoreAbove:F4} < {MinMargin:F4}), checking other sources for better results",
                                scoreRange, MinScoreRangeForEarlyExit, topScore, threshold, scoreAboveThreshold, MinScoreAboveThreshold);
                            shouldSkip = false;
                        }
                    }
                }
                else if (scoreRange >= MinScoreRangeForEarlyExit)
                {
                    _logger.LogDebug(
                        "Good score discrimination (range: {Range:F4} >= {MinRange:F4}), allowing early exit",
                        scoreRange, MinScoreRangeForEarlyExit);
                }
            }

            if (shouldSkip)
            {
                var confidenceScore = CalculateConfidenceScore(results);
                var imageInfo = hasImageResults ? " (includes image results)" : "";
                _logger.LogInformation(
                    "High-confidence document results found{ImageInfo} (top score: {TopScore:F4}, threshold: {Threshold:F4}, high-quality results: {HighQualityCount}/{TotalCount}, confidence: {Confidence:F4}), skipping other sources for faster response",
                    imageInfo, topScore, threshold, highQualityResults, results.Count, confidenceScore);
            }
            else
            {
                _logger.LogDebug(
                    "Document results insufficient for early exit (top score: {TopScore:F4}, threshold: {Threshold:F4}, high-quality results: {HighQualityCount}/{TotalCount}, required: {RequiredCount}), checking other sources",
                    topScore, threshold, highQualityResults, results.Count, minResults);
            }

            return Task.FromResult(shouldSkip);
        }

        /// <summary>
        /// Calculates confidence score based on document search results
        /// </summary>
        public double CalculateConfidenceScore(List<DocumentChunk> results)
        {
            if (results == null || results.Count == 0)
            {
                return 0.0;
            }

            var scores = results
                .Where(r => r.RelevanceScore.HasValue)
                .Select(r => r.RelevanceScore!.Value)
                .ToList();

            if (scores.Count == 0)
            {
                return 0.0;
            }

            var maxScore = scores.Max();
            var avgScore = scores.Average();

            if (maxScore > ScoreTypeBoundary)
            {
                return Math.Min(1.0, avgScore / 10.0);
            }

            return Math.Min(1.0, avgScore);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Determines adaptive threshold based on score type (vector vs text search)
        /// </summary>
        private double DetermineAdaptiveThreshold(double maxScore)
        {
            if (maxScore > ScoreTypeBoundary)
            {
                return TextSearchThreshold;
            }

            return VectorSearchThreshold;
        }

        #endregion
    }
}
