using System;
using System.Collections.Generic;

namespace SmartRAG.Dashboard.Models;

/// <summary>
/// Represents a single document in dashboard listings.
/// </summary>
public sealed class DocumentSummaryResponse
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string UploadedBy { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }

    public long FileSize { get; set; }

    /// <summary>
    /// Database type for schema documents (e.g. SQLite, SqlServer, PostgreSQL, MySQL).
    /// </summary>
    public string DatabaseType { get; set; } = string.Empty;

    /// <summary>
    /// Storage collection name (e.g. Qdrant document collection).
    /// </summary>
    public string CollectionName { get; set; } = string.Empty;
}

/// <summary>
/// Paged document list response for the dashboard.
/// </summary>
public sealed class PagedDocumentsResponse
{
    public List<DocumentSummaryResponse> Items { get; set; } = new();

    public int TotalCount { get; set; }
}

