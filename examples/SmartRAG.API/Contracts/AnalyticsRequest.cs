using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SmartRAG.API.Contracts;


/// <summary>
/// Request model for analytics data filtering and querying
/// </summary>
public class AnalyticsRequest
{
    /// <summary>
    /// Start date for analytics data (optional, defaults to 30 days ago)
    /// </summary>
    /// <example>2024-01-01T00:00:00Z</example>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for analytics data (optional, defaults to now)
    /// </summary>
    /// <example>2024-01-31T23:59:59Z</example>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Grouping interval for time-based analytics
    /// </summary>
    /// <example>daily</example>
    [DefaultValue("daily")]
    public string GroupBy { get; set; } = "daily";

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    /// <example>100</example>
    [Range(1, 1000)]
    [DefaultValue(100)]
    public int Limit { get; set; } = 100;

    /// <summary>
    /// Filter by specific user or session (optional)
    /// </summary>
    /// <example>user123</example>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Filter by AI provider (optional)
    /// </summary>
    /// <example>OpenAI</example>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Filter by operation type (optional)
    /// </summary>
    /// <example>search</example>
    public string OperationType { get; set; } = string.Empty;
}

/// <summary>
/// Request model for exporting analytics data
/// </summary>
public class AnalyticsExportRequest : AnalyticsRequest
{
    /// <summary>
    /// Export format (json, csv, xlsx)
    /// </summary>
    /// <example>csv</example>
    [Required]
    [DefaultValue("csv")]
    public string Format { get; set; } = "csv";

    /// <summary>
    /// Whether to include detailed metrics
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool IncludeDetails { get; set; } = true;

    /// <summary>
    /// Whether to include performance metrics
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool IncludePerformance { get; set; } = true;

    /// <summary>
    /// Whether to include error logs
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool IncludeErrors { get; set; } = false;
}

