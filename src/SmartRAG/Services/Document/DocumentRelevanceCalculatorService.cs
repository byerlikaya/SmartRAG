using Microsoft.Extensions.Logging;
using SmartRAG.Entities;
using SmartRAG.Interfaces.Document;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartRAG.Services.Document
{
    /// <summary>
    /// Service for calculating document-level relevance scores
    /// </summary>
    public class DocumentRelevanceCalculatorService : IDocumentRelevanceCalculatorService
    {
        #region Constants

        private const double QueryCoverageBonusMultiplier = 5000.0;
        private const double UniqueKeywordBonusMultiplier = 2500.0;
        private const double FrequencyBonusMultiplier = 75.0;      

        #endregion

        #region Fields

        private readonly ILogger<DocumentRelevanceCalculatorService> _logger;
        private readonly IQueryWordMatcherService _queryWordMatcher;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the DocumentRelevanceCalculatorService
        /// </summary>
        /// <param name="queryWordMatcher">Service for query word matching operations</param>
        /// <param name="logger">Logger instance for this service</param>
        public DocumentRelevanceCalculatorService(
            IQueryWordMatcherService queryWordMatcher,
            ILogger<DocumentRelevanceCalculatorService> logger)
        {
            _queryWordMatcher = queryWordMatcher ?? throw new ArgumentNullException(nameof(queryWordMatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Calculates relevance scores for documents based on query words and chunk scores
        /// </summary>
        public List<DocumentScoreResult> CalculateDocumentScores(
            List<Entities.Document> documents,
            List<DocumentChunk> scoredChunks,
            List<string> queryWords,
            Dictionary<string, HashSet<Guid>> wordDocumentMap,
            int topChunksPerDocument)
        {
            if (documents == null || scoredChunks == null || queryWords == null || wordDocumentMap == null)
            {
                throw new ArgumentNullException();
            }

            return documents.Select(doc =>
            {
                var docChunks = scoredChunks.Where(c => c.DocumentId == doc.Id).ToList();
                if (docChunks.Count == 0)
                {
                    return new DocumentScoreResult
                    {
                        Document = doc,
                        Score = 0.0,
                        QueryWordMatches = 0,
                        UniqueKeywords = 0
                    };
                }

                var topChunks = docChunks
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .Take(topChunksPerDocument)
                    .ToList();

                var avgScore = topChunks.Average(c => c.RelevanceScore ?? 0.0);
                var docContent = string.Join(" ", topChunks.Select(c => c.Content)).ToLowerInvariant();

                var (queryWordMatches, totalQueryWordOccurrences) = _queryWordMatcher.CountQueryWordMatches(docContent, queryWords);

                var queryCoverageRatio = queryWords.Count > 0 ? (double)queryWordMatches / queryWords.Count : 0.0;
                var queryCoverageBonus = queryCoverageRatio * queryCoverageRatio * QueryCoverageBonusMultiplier;

                var uniqueKeywordCount = _queryWordMatcher.FindUniqueKeywords(wordDocumentMap, doc.Id);
                var uniqueKeywordBonus = uniqueKeywordCount * UniqueKeywordBonusMultiplier;
                var frequencyBonus = totalQueryWordOccurrences * FrequencyBonusMultiplier;

                var queryWordMatchBonus = uniqueKeywordBonus + queryCoverageBonus + frequencyBonus;
                var finalScore = avgScore + queryWordMatchBonus;

                return new DocumentScoreResult
                {
                    Document = doc,
                    Score = finalScore,
                    QueryWordMatches = queryWordMatches,
                    UniqueKeywords = uniqueKeywordCount
                };
            })
            .OrderByDescending(x => x.Score)
            .ToList();
        }

        /// <summary>
        /// Identifies relevant documents based on calculated scores
        /// </summary>
        public List<Entities.Document> IdentifyRelevantDocuments(
            List<DocumentScoreResult> documentScores,
            double scoreThreshold)
        {
            if (documentScores == null || documentScores.Count == 0)
            {
                return new List<Entities.Document>();
            }

            var topDocument = documentScores.FirstOrDefault();
            var secondDocument = documentScores.Skip(1).FirstOrDefault();

            var relevantDocuments = new List<Entities.Document>();
            if (topDocument != null && topDocument.Score > 0)
            {
                relevantDocuments.Add(topDocument.Document);

                if (secondDocument != null && secondDocument.Score > 0 &&
                    secondDocument.Score >= topDocument.Score * scoreThreshold)
                {
                    relevantDocuments.Add(secondDocument.Document);
                }
            }

            return relevantDocuments;
        }

        /// <summary>
        /// Applies document-level boost to chunks from relevant documents
        /// </summary>
        /// <param name="chunks">List of chunks to boost</param>
        /// <param name="relevantDocumentIds">Set of relevant document IDs</param>
        /// <param name="boostAmount">Amount to boost relevance score</param>
        public void ApplyDocumentBoost(List<DocumentChunk> chunks, HashSet<Guid> relevantDocumentIds, double boostAmount)
        {
            if (chunks == null || relevantDocumentIds == null)
            {
                return;
            }

            foreach (var chunk in chunks)
            {
                if (relevantDocumentIds.Contains(chunk.DocumentId))
                {
                    chunk.RelevanceScore = (chunk.RelevanceScore ?? 0.0) + boostAmount;
                }
            }
        }

        #endregion
    }
}

