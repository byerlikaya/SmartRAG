using System.Collections.Generic;
using System.IO;

namespace SmartRAG.Models.RequestResponse;


/// <summary>
/// Request DTO for document upload operation
/// </summary>
public class UploadDocumentRequest
{
    /// <summary>
    /// File stream containing the document content
    /// </summary>
    public Stream FileStream { get; set; } = null!;

    /// <summary>
    /// Name of the file
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type of the file
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the user uploading the document
    /// </summary>
    public string UploadedBy { get; set; } = string.Empty;

    /// <summary>
    /// Language code for document processing (ISO 639-1 format, e.g., "tr", "en", "de")
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Size of the file in bytes
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Additional metadata to attach to the document
    /// </summary>
    public Dictionary<string, object>? AdditionalMetadata { get; set; }
}

