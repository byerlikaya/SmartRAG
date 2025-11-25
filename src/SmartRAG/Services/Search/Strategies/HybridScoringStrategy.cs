using SmartRAG.Entities;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Search;
using SmartRAG.Interfaces.Search.Strategies;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Services.Search.Strategies
{
    public class HybridScoringStrategy : IScoringStrategy
    {
        private readonly ISemanticSearchService _semanticSearchService;
        private readonly IDocumentScoringService _documentScoringService;

        private const double HybridSemanticWeight = 0.8;
        private const double HybridKeywordWeight = 0.2;

        public HybridScoringStrategy(
            ISemanticSearchService semanticSearchService,
            IDocumentScoringService documentScoringService)
        {
            _semanticSearchService = semanticSearchService;
            _documentScoringService = documentScoringService;
        }

        public async Task<double> CalculateScoreAsync(string query, DocumentChunk chunk, List<float> queryEmbedding)
        {
            var enhancedSemanticScore = await _semanticSearchService.CalculateEnhancedSemanticSimilarityAsync(query, chunk.Content);

            var keywordScore = _documentScoringService.CalculateKeywordRelevanceScore(query, chunk.Content);

            var hybridScore = (enhancedSemanticScore * HybridSemanticWeight) + (keywordScore * HybridKeywordWeight);

            return hybridScore;
        }
    }
}
