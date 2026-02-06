using SmartRAG.Enums;
using System;
using System.Collections.Generic;

namespace SmartRAG.API.Contracts;


/// <summary>
/// Response model for AI operations
/// </summary>
public class AIResponse
{
    /// <summary>
    /// Generated text response
    /// </summary>
    /// <example>Quantum computing is a revolutionary technology that uses quantum mechanical phenomena...</example>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// AI provider that generated the response
    /// </summary>
    /// <example>OpenAI</example>
    public AIProvider Provider { get; set; }

    /// <summary>
    /// Model used for generation
    /// </summary>
    /// <example>gpt-5.1</example>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Number of tokens used in the request
    /// </summary>
    /// <example>150</example>
    public int TokensUsed { get; set; }

    /// <summary>
    /// Time taken to generate the response
    /// </summary>
    /// <example>1.5</example>
    public double ResponseTimeSeconds { get; set; }

    /// <summary>
    /// Timestamp when the response was generated
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the request was successful
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if the request failed
    /// </summary>
    /// <example>null</example>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Response model for embedding operations
/// </summary>
public class EmbeddingResponse
{
    /// <summary>
    /// Generated embedding vector for single text
    /// </summary>
    /// <example>[0.1, -0.2, 0.3, ...]</example>
    public List<float> Embedding { get; set; } = new List<float>();

    /// <summary>
    /// Generated embedding vectors for multiple texts
    /// </summary>
    /// <example>[[0.1, -0.2, 0.3], [0.4, -0.5, 0.6]]</example>
    public List<List<float>> Embeddings { get; set; } = new List<List<float>>();

    /// <summary>
    /// AI provider that generated the embeddings
    /// </summary>
    /// <example>OpenAI</example>
    public AIProvider Provider { get; set; }

    /// <summary>
    /// Model used for embedding generation
    /// </summary>
    /// <example>text-embedding-3-small</example>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Dimension of the embedding vectors
    /// </summary>
    /// <example>1536</example>
    public int Dimensions { get; set; }

    /// <summary>
    /// Number of tokens processed
    /// </summary>
    /// <example>25</example>
    public int TokensUsed { get; set; }

    /// <summary>
    /// Time taken to generate embeddings
    /// </summary>
    /// <example>0.8</example>
    public double ResponseTimeSeconds { get; set; }

    /// <summary>
    /// Whether the request was successful
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if the request failed
    /// </summary>
    /// <example>null</example>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Response model for AI provider information
/// </summary>
public class AIProviderInfo
{
    /// <summary>
    /// AI provider type
    /// </summary>
    /// <example>OpenAI</example>
    public AIProvider Provider { get; set; }

    /// <summary>
    /// Provider display name
    /// </summary>
    /// <example>OpenAI GPT Models</example>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the provider is currently active
    /// </summary>
    /// <example>true</example>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether the provider configuration is valid
    /// </summary>
    /// <example>true</example>
    public bool IsConfigured { get; set; }

    /// <summary>
    /// Available models for this provider
    /// </summary>
    /// <example>["gpt-5.1", "gpt-5", "gpt-4o", "gpt-4o-mini", "text-embedding-3-small", "text-embedding-3-large"]</example>
    public List<string> AvailableModels { get; set; } = new List<string>();

    /// <summary>
    /// Current default model
    /// </summary>
    /// <example>gpt-5.1</example>
    public string DefaultModel { get; set; } = string.Empty;

    /// <summary>
    /// Provider capabilities
    /// </summary>
    /// <example>["TextGeneration", "Embeddings", "ChatCompletion"]</example>
    public List<string> Capabilities { get; set; } = new List<string>();

    /// <summary>
    /// Last health check status
    /// </summary>
    /// <example>true</example>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Last health check timestamp
    /// </summary>
    /// <example>2024-01-15T10:25:00Z</example>
    public DateTime LastHealthCheck { get; set; }

    /// <summary>
    /// Health check error message if unhealthy
    /// </summary>
    /// <example>null</example>
    public string HealthError { get; set; } = string.Empty;
}

