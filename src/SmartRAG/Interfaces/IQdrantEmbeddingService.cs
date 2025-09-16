using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces
{
    /// <summary>
    /// Interface for generating embeddings for text content
    /// </summary>
    public interface IQdrantEmbeddingService
    {
        /// <summary>
        /// Generates an embedding vector for the given text
        /// </summary>
        /// <param name="text">Text to generate embedding for</param>
        /// <returns>List of float values representing the embedding vector</returns>
        Task<List<float>> GenerateEmbeddingAsync(string text);

        /// <summary>
        /// Gets the vector dimension for embeddings
        /// </summary>
        Task<int> GetVectorDimensionAsync();
    }
}
