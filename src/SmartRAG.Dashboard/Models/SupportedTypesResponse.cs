using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartRAG.Dashboard.Models;

/// <summary>
/// Response for the supported document types endpoint (for upload UI).
/// </summary>
public sealed class SupportedDocumentTypesResponse
{
    [JsonPropertyName("extensions")]
    public List<string> Extensions { get; set; } = new();

    [JsonPropertyName("mimeTypes")]
    public List<string> MimeTypes { get; set; } = new();
}
