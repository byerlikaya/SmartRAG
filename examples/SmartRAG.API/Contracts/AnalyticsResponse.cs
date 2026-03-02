
namespace SmartRAG.API.Contracts;


/// <summary>
/// Response model for usage analytics data
/// </summary>
public class UsageAnalyticsResponse
{
    /// <summary>
    /// Total number of requests in the time period
    /// </summary>
    /// <example>1250</example>
    public int TotalRequests { get; set; }

    /// <summary>
    /// Total number of successful requests
    /// </summary>
    /// <example>1200</example>
    public int SuccessfulRequests { get; set; }

    /// <summary>
    /// Total number of failed requests
    /// </summary>
    /// <example>50</example>
    public int FailedRequests { get; set; }

    /// <summary>
    /// Success rate as percentage
    /// </summary>
    /// <example>96.0</example>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Total number of unique users/sessions
    /// </summary>
    /// <example>45</example>
    public int UniqueUsers { get; set; }

    /// <summary>
    /// Total number of documents processed
    /// </summary>
    /// <example>180</example>
    public int DocumentsProcessed { get; set; }

    /// <summary>
    /// Total tokens consumed across all requests
    /// </summary>
    /// <example>125000</example>
    public long TotalTokens { get; set; }

    /// <summary>
    /// Average tokens per request
    /// </summary>
    /// <example>100.0</example>
    public double AverageTokensPerRequest { get; set; }

    /// <summary>
    /// Requests grouped by time period
    /// </summary>
    public List<TimeSeriesData> RequestsByTime { get; set; } = new List<TimeSeriesData>();

    /// <summary>
    /// Requests grouped by AI provider
    /// </summary>
    public List<ProviderUsageData> RequestsByProvider { get; set; } = new List<ProviderUsageData>();

    /// <summary>
    /// Requests grouped by operation type
    /// </summary>
    public List<OperationUsageData> RequestsByOperation { get; set; } = new List<OperationUsageData>();

    /// <summary>
    /// Top queries by frequency
    /// </summary>
    public List<QueryFrequencyData> TopQueries { get; set; } = new List<QueryFrequencyData>();

    /// <summary>
    /// Analytics data collection period
    /// </summary>
    public AnalyticsPeriod Period { get; set; } = new AnalyticsPeriod();
}

/// <summary>
/// Response model for performance analytics data
/// </summary>
public class PerformanceAnalyticsResponse
{
    /// <summary>
    /// Average response time in seconds
    /// </summary>
    /// <example>1.25</example>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Median response time in seconds
    /// </summary>
    /// <example>0.95</example>
    public double MedianResponseTime { get; set; }

    /// <summary>
    /// 95th percentile response time in seconds
    /// </summary>
    /// <example>2.8</example>
    public double P95ResponseTime { get; set; }

    /// <summary>
    /// 99th percentile response time in seconds
    /// </summary>
    /// <example>4.2</example>
    public double P99ResponseTime { get; set; }

    /// <summary>
    /// Fastest response time in seconds
    /// </summary>
    /// <example>0.15</example>
    public double FastestResponseTime { get; set; }

    /// <summary>
    /// Slowest response time in seconds
    /// </summary>
    /// <example>8.5</example>
    public double SlowestResponseTime { get; set; }

    /// <summary>
    /// Average memory usage in MB
    /// </summary>
    /// <example>245.5</example>
    public double AverageMemoryUsage { get; set; }

    /// <summary>
    /// Peak memory usage in MB
    /// </summary>
    /// <example>512.8</example>
    public double PeakMemoryUsage { get; set; }

    /// <summary>
    /// Average CPU usage percentage
    /// </summary>
    /// <example>25.4</example>
    public double AverageCpuUsage { get; set; }

    /// <summary>
    /// Performance data grouped by time period
    /// </summary>
    public List<PerformanceTimeSeriesData> PerformanceByTime { get; set; } = new List<PerformanceTimeSeriesData>();

    /// <summary>
    /// Performance data grouped by AI provider
    /// </summary>
    public List<ProviderPerformanceData> PerformanceByProvider { get; set; } = new List<ProviderPerformanceData>();

    /// <summary>
    /// Performance data grouped by operation type
    /// </summary>
    public List<OperationPerformanceData> PerformanceByOperation { get; set; } = new List<OperationPerformanceData>();

    /// <summary>
    /// Analytics data collection period
    /// </summary>
    public AnalyticsPeriod Period { get; set; } = new AnalyticsPeriod();
}

/// <summary>
/// Response model for error analytics data
/// </summary>
public class ErrorAnalyticsResponse
{
    /// <summary>
    /// Total number of errors
    /// </summary>
    /// <example>25</example>
    public int TotalErrors { get; set; }

    /// <summary>
    /// Error rate as percentage
    /// </summary>
    /// <example>2.1</example>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Most common error types
    /// </summary>
    public List<ErrorTypeData> ErrorsByType { get; set; } = new List<ErrorTypeData>();

    /// <summary>
    /// Errors grouped by time period
    /// </summary>
    public List<TimeSeriesData> ErrorsByTime { get; set; } = new List<TimeSeriesData>();

    /// <summary>
    /// Errors grouped by AI provider
    /// </summary>
    public List<ProviderErrorData> ErrorsByProvider { get; set; } = new List<ProviderErrorData>();

    /// <summary>
    /// Recent error details
    /// </summary>
    public List<ErrorDetailData> RecentErrors { get; set; } = new List<ErrorDetailData>();

    /// <summary>
    /// Analytics data collection period
    /// </summary>
    public AnalyticsPeriod Period { get; set; } = new AnalyticsPeriod();
}

#region Supporting Data Models

/// <summary>
/// Time series data point
/// </summary>
public class TimeSeriesData
{
    /// <summary>
    /// Timestamp for the data point
    /// </summary>
    /// <example>2024-01-15T00:00:00Z</example>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Value for the data point
    /// </summary>
    /// <example>125</example>
    public long Value { get; set; }

    /// <summary>
    /// Label for the data point
    /// </summary>
    /// <example>2024-01-15</example>
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// Performance time series data point
/// </summary>
public class PerformanceTimeSeriesData : TimeSeriesData
{
    /// <summary>
    /// Average response time for this time period
    /// </summary>
    /// <example>1.25</example>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Memory usage for this time period
    /// </summary>
    /// <example>245.5</example>
    public double MemoryUsage { get; set; }

    /// <summary>
    /// CPU usage for this time period
    /// </summary>
    /// <example>25.4</example>
    public double CpuUsage { get; set; }
}

/// <summary>
/// Provider usage data
/// </summary>
public class ProviderUsageData
{
    /// <summary>
    /// AI provider name
    /// </summary>
    /// <example>OpenAI</example>
    public AIProvider Provider { get; set; }

    /// <summary>
    /// Number of requests for this provider
    /// </summary>
    /// <example>850</example>
    public int RequestCount { get; set; }

    /// <summary>
    /// Success rate for this provider
    /// </summary>
    /// <example>98.2</example>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Total tokens consumed by this provider
    /// </summary>
    /// <example>85000</example>
    public long TotalTokens { get; set; }

    /// <summary>
    /// Percentage of total requests
    /// </summary>
    /// <example>68.0</example>
    public double Percentage { get; set; }
}

/// <summary>
/// Provider performance data
/// </summary>
public class ProviderPerformanceData : ProviderUsageData
{
    /// <summary>
    /// Average response time for this provider
    /// </summary>
    /// <example>1.15</example>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// 95th percentile response time
    /// </summary>
    /// <example>2.5</example>
    public double P95ResponseTime { get; set; }
}

/// <summary>
/// Provider error data
/// </summary>
public class ProviderErrorData : ProviderUsageData
{
    /// <summary>
    /// Number of errors for this provider
    /// </summary>
    /// <example>15</example>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Error rate for this provider
    /// </summary>
    /// <example>1.8</example>
    public double ErrorRate { get; set; }
}

/// <summary>
/// Operation usage data
/// </summary>
public class OperationUsageData
{
    /// <summary>
    /// Operation type (search, upload, generate, etc.)
    /// </summary>
    /// <example>search</example>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Number of requests for this operation
    /// </summary>
    /// <example>650</example>
    public int RequestCount { get; set; }

    /// <summary>
    /// Success rate for this operation
    /// </summary>
    /// <example>97.5</example>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Percentage of total requests
    /// </summary>
    /// <example>52.0</example>
    public double Percentage { get; set; }
}

/// <summary>
/// Operation performance data
/// </summary>
public class OperationPerformanceData : OperationUsageData
{
    /// <summary>
    /// Average response time for this operation
    /// </summary>
    /// <example>0.95</example>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Average tokens per request for this operation
    /// </summary>
    /// <example>85.5</example>
    public double AverageTokensPerRequest { get; set; }
}

/// <summary>
/// Query frequency data
/// </summary>
public class QueryFrequencyData
{
    /// <summary>
    /// Query text (truncated for privacy)
    /// </summary>
    /// <example>What is machine learning...</example>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Number of times this query was asked
    /// </summary>
    /// <example>25</example>
    public int Frequency { get; set; }

    /// <summary>
    /// Percentage of total queries
    /// </summary>
    /// <example>2.0</example>
    public double Percentage { get; set; }

    /// <summary>
    /// Average response time for this query
    /// </summary>
    /// <example>1.2</example>
    public double AverageResponseTime { get; set; }
}

/// <summary>
/// Error type data
/// </summary>
public class ErrorTypeData
{
    /// <summary>
    /// Error type or category
    /// </summary>
    /// <example>TimeoutException</example>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// Number of occurrences
    /// </summary>
    /// <example>12</example>
    public int Count { get; set; }

    /// <summary>
    /// Percentage of total errors
    /// </summary>
    /// <example>48.0</example>
    public double Percentage { get; set; }

    /// <summary>
    /// Most recent occurrence
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime LastOccurrence { get; set; }
}

/// <summary>
/// Error detail data
/// </summary>
public class ErrorDetailData
{
    /// <summary>
    /// Error timestamp
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Error type
    /// </summary>
    /// <example>TimeoutException</example>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// Error message
    /// </summary>
    /// <example>Request timeout after 30 seconds</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// AI provider that caused the error
    /// </summary>
    /// <example>OpenAI</example>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Operation that caused the error
    /// </summary>
    /// <example>search</example>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// User/session ID (anonymized)
    /// </summary>
    /// <example>user_***123</example>
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Analytics period information
/// </summary>
public class AnalyticsPeriod
{
    /// <summary>
    /// Start date of the analytics period
    /// </summary>
    /// <example>2024-01-01T00:00:00Z</example>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the analytics period
    /// </summary>
    /// <example>2024-01-31T23:59:59Z</example>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Duration of the period in days
    /// </summary>
    /// <example>30</example>
    public int DurationDays { get; set; }

    /// <summary>
    /// Grouping interval used
    /// </summary>
    /// <example>daily</example>
    public string GroupBy { get; set; } = string.Empty;
}

#endregion

