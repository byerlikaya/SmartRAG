using SmartRAG.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.AI
{

    /// <summary>
    /// Interface for AI providers
    /// </summary>
    public interface IAIProvider
    {
        /// <summary>
        /// Generates text response using the AI provider
        /// </summary>
        /// <param name="prompt">Text prompt to send to the AI provider</param>
        /// <param name="config">AI provider configuration settings</param>
        /// <returns>AI-generated text response</returns>
        Task<string> GenerateTextAsync(string prompt, AIProviderConfig config);

        /// <summary>
        /// Generates embedding vector for the given text
        /// </summary>
        /// <param name="text">Text to generate embedding for</param>
        /// <param name="config">AI provider configuration settings</param>
        /// <returns>List of float values representing the embedding vector</returns>
        Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config);

        /// <summary>
        /// Generates embeddings for multiple texts in a single request (if supported)
        /// </summary>
        /// <param name="texts">Collection of texts to generate embeddings for</param>
        /// <param name="config">AI provider configuration settings</param>
        /// <returns>List of embedding vectors, one for each input text</returns>
        Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, AIProviderConfig config);

        /// <summary>
        /// Chunks text into smaller segments for processing
        /// </summary>
        /// <param name="text">Text to chunk</param>
        /// <param name="maxChunkSize">Maximum size of each chunk in characters</param>
        /// <returns>List of text chunks</returns>
        Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000);
    }
}
