using System;
using System.Collections.Generic;

namespace SmartRAG.Entities;



/// <summary>
/// Represents a document with its content, metadata, and associated chunks
/// </summary>
public class Document
{
    /// <summary>
    /// Unique identifier for the document
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Original file name of the document
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type of the document
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Full text content of the document
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Username or identifier of who uploaded the document
    /// </summary>
    public string UploadedBy { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the document was uploaded
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Collection of text chunks derived from this document
    /// </summary>
    public List<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();

    /// <summary>
    /// Optional metadata associated with the document
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; }

    /// <summary>
    /// Size of the original file in bytes
    /// </summary>
    public long FileSize { get; set; }
}

