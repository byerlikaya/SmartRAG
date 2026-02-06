using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SmartRAG.API.Contracts;


/// <summary>
/// Request model for generating text embeddings
/// </summary>
public class EmbeddingRequest
{
    /// <summary>
    /// Single text to generate embedding for
    /// </summary>
    /// <example>This is a sample text for embedding generation</example>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Multiple texts to generate embeddings for (batch processing)
    /// </summary>
    /// <example>["First text", "Second text", "Third text"]</example>
    public List<string> Texts { get; set; } = new List<string>();

    /// <summary>
    /// Whether to normalize the embedding vectors
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool Normalize { get; set; } = true;

    /// <summary>
    /// Embedding model to use (optional, uses provider default)
    /// </summary>
    /// <example>text-embedding-3-small</example>
    public string Model { get; set; } = string.Empty;
}

