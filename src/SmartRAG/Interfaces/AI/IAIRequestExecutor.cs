using SmartRAG.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.AI
{
    /// <summary>
    /// Interface for executing AI requests with a specific provider
    /// </summary>
    public interface IAIRequestExecutor
    {
        /// <summary>
        /// Generates a response using the specified provider
        /// </summary>
        Task<string> GenerateResponseAsync(AIProvider provider, string query, IEnumerable<string> context);

        /// <summary>
        /// Generates embeddings using the specified provider
        /// </summary>
        Task<List<float>> GenerateEmbeddingsAsync(AIProvider provider, string text);

        /// <summary>
        /// Generates batch embeddings using the specified provider
        /// </summary>
        Task<List<List<float>>> GenerateEmbeddingsBatchAsync(AIProvider provider, IEnumerable<string> texts);
    }
}
