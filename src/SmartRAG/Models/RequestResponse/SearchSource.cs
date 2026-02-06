using System;
using System.Collections.Generic;

namespace SmartRAG.Models;



/// <summary>
/// Represents a search result source with document information and relevance score
/// </summary>
public class SearchSource
{
    /// <summary>
    /// Type of the source (Document, Audio, Database, Image, System)
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier of the source document (if applicable)
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Name of the source document file
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Relevant content excerpt from the document
    /// </summary>
    public string RelevantContent { get; set; } = string.Empty;

    /// <summary>
    /// Relevance score indicating how well this source matches the search query
    /// </summary>
    public double RelevanceScore { get; set; }

    /// <summary>
    /// Human-readable location metadata (character range, timestamps, etc.)
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Identifier of the database source (if applicable)
    /// </summary>
    public string? DatabaseId { get; set; }

    /// <summary>
    /// Name of the database source (if applicable)
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Tables referenced in the database query (if applicable)
    /// </summary>
    public List<string> Tables { get; set; } = new List<string>();

    /// <summary>
    /// Executed query for database sources (if applicable)
    /// </summary>
    public string? ExecutedQuery { get; set; }

    /// <summary>
    /// Row number reference for database sources (if applicable)
    /// </summary>
    public int? RowNumber { get; set; }

    /// <summary>
    /// Start timestamp in seconds for audio sources (if applicable)
    /// </summary>
    public double? StartTimeSeconds { get; set; }

    /// <summary>
    /// End timestamp in seconds for audio sources (if applicable)
    /// </summary>
    public double? EndTimeSeconds { get; set; }

    /// <summary>
    /// Chunk index within the document or transcript (if applicable)
    /// </summary>
    public int? ChunkIndex { get; set; }

    /// <summary>
    /// Starting character position within the document content (if applicable)
    /// </summary>
    public int? StartPosition { get; set; }

    /// <summary>
    /// Ending character position within the document content (if applicable)
    /// </summary>
    public int? EndPosition { get; set; }
}

