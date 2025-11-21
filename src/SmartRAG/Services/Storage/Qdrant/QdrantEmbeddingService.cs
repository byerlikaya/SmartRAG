using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Interfaces.Storage.Qdrant;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Services.Storage.Qdrant
{
    /// <summary>
    /// Service for generating embeddings for text content using hash-based approach
    /// </summary>
    public class QdrantEmbeddingService : IQdrantEmbeddingService
    {
        #region Constants

        private const int DefaultVectorDimension = 768;

        #endregion

        #region Fields

        private readonly ILogger<QdrantEmbeddingService> _logger;
        private readonly QdrantConfig _config;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the QdrantEmbeddingService
        /// </summary>
        /// <param name="config">Qdrant configuration options</param>
        /// <param name="logger">Logger instance for this service</param>
        public QdrantEmbeddingService(IOptions<QdrantConfig> config, ILogger<QdrantEmbeddingService> logger)
        {
            _config = config.Value;
            _logger = logger;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates an embedding vector for the given text using hash-based approach
        /// </summary>
        /// <param name="text">Text to generate embedding for</param>
        /// <returns>List of float values representing the embedding vector</returns>
        public async Task<List<float>> GenerateEmbeddingAsync(string text)
        {
            try
            {
                // Using hash-based approach for fast embedding generation
                // This provides deterministic embeddings suitable for testing and development

                // Get vector dimension from config or use default
                var vectorDimension = await GetVectorDimensionAsync();

                var hash = text.GetHashCode();
                var random = new Random(hash);
                var embedding = new List<float>(vectorDimension);

                // Generate embedding with correct dimension
                for (int i = 0; i < vectorDimension; i++)
                {
                    embedding.Add((float)(random.NextDouble() * 2 - 1)); // -1 to 1
                }

                // Fast normalization using SIMD-friendly approach
                var sumSquares = 0.0f;
                for (int i = 0; i < embedding.Count; i++)
                {
                    sumSquares += embedding[i] * embedding[i];
                }

                var magnitude = (float)Math.Sqrt(sumSquares);
                if (magnitude > 0.001f) // Avoid division by very small numbers
                {
                    var invMagnitude = 1.0f / magnitude;
                    for (int i = 0; i < embedding.Count; i++)
                    {
                        embedding[i] *= invMagnitude;
                    }
                }

                _logger.LogDebug("Generated embedding with dimension {Dimension} for text length {TextLength}",
                    vectorDimension, text?.Length ?? 0);

                return embedding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate embedding for text");
                return null;
            }
        }

        /// <summary>
        /// Gets the vector dimension for embeddings
        /// </summary>
        public Task<int> GetVectorDimensionAsync()
        {
            try
            {
                // First try to get from config
                if (_config.VectorSize > 0)
                {
                    return Task.FromResult(_config.VectorSize);
                }

                // Default fallback dimensions based on common models
                _logger.LogDebug("Using default vector dimension: {Dimension}", DefaultVectorDimension);
                return Task.FromResult(DefaultVectorDimension);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get vector dimension, using default");
                return Task.FromResult(DefaultVectorDimension);
            }
        }

        #endregion
    }
}
