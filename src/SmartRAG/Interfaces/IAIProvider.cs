using SmartRAG.Entities;
using SmartRAG.Models;

namespace SmartRAG.Interfaces;

/// <summary>
/// Interface for AI providers
/// </summary>
public interface IAIProvider
{
    /// <summary>
    /// Generates text response using the AI provider
    /// </summary>
    Task<string> GenerateTextAsync(string prompt, AIProviderConfig config);
    
    /// <summary>
    /// Generates embedding vector for the given text
    /// </summary>
    Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config);
    
    /// <summary>
    /// Generates embeddings for multiple texts in batch
    /// </summary>
    Task<List<List<float>>?> GenerateEmbeddingsBatchAsync(List<string> texts, AIProviderConfig config);
    
    /// <summary>
    /// Chunks text into smaller segments for processing
    /// </summary>
    Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000);
    
    /// <summary>
    /// Clears embeddings for the given document chunks
    /// </summary>
    Task ClearEmbeddingsAsync(List<DocumentChunk> chunks);
    
    /// <summary>
    /// Clears all cached embeddings
    /// </summary>
    Task ClearAllEmbeddingsAsync();
    
    /// <summary>
    /// Regenerates embeddings for all documents
    /// </summary>
    Task<bool> RegenerateAllEmbeddingsAsync(List<Document> documents);
}
