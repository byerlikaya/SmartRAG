using System;
using System.Collections.Generic;

namespace SmartRAG.Entities;

/// <summary>
/// Represents a chunk of text from a document with its embedding and metadata
/// </summary>
public class DocumentChunk
{
    #region Properties

    /// <summary>
    /// Unique identifier for the chunk
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of the parent document this chunk belongs to
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Text content of this chunk
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Sequential index of this chunk within the document
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Vector embedding representation of the chunk content
    /// </summary>
    public List<float>? Embedding { get; set; }

    /// <summary>
    /// Relevance score for search ranking purposes
    /// </summary>
    public double? RelevanceScore { get; set; }

    /// <summary>
    /// Timestamp when this chunk was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Starting character position of this chunk in the original document
    /// </summary>
    public int StartPosition { get; set; }

    /// <summary>
    /// Ending character position of this chunk in the original document
    /// </summary>
    public int EndPosition { get; set; }

    #endregion
}
