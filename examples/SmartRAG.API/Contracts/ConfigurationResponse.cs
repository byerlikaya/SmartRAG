using SmartRAG.Enums;
using System;
using System.Collections.Generic;

namespace SmartRAG.API.Contracts
{
    /// <summary>
    /// Response model for configuration operations
    /// </summary>
    public class ConfigurationResponse
    {
        /// <summary>
        /// Configuration section name
        /// </summary>
        /// <example>AIProvider</example>
        public string Section { get; set; } = string.Empty;

        /// <summary>
        /// Configuration data (sensitive values may be masked)
        /// </summary>
        /// <example>{"provider": "OpenAI", "apiKey": "sk-***", "model": "gpt-5.1"}</example>
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// When the configuration was last updated
        /// </summary>
        /// <example>2024-01-15T10:30:00Z</example>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Who last updated the configuration
        /// </summary>
        /// <example>admin</example>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Configuration version
        /// </summary>
        /// <example>1.2.3</example>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Whether the configuration is valid
        /// </summary>
        /// <example>true</example>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Configuration validation errors (if any)
        /// </summary>
        public List<string> ValidationErrors { get; set; } = new List<string>();

        /// <summary>
        /// Whether the configuration is currently active
        /// </summary>
        /// <example>true</example>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Response model for AI provider configuration
    /// </summary>
    public class AIProviderConfigResponse : ConfigurationResponse
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
        /// Masked API key for security
        /// </summary>
        /// <example>sk-***...***def</example>
        public string MaskedApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Provider base URL
        /// </summary>
        /// <example>https://api.openai.com/v1</example>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Default model for this provider
        /// </summary>
        /// <example>gpt-5.1</example>
        public string DefaultModel { get; set; } = string.Empty;

        /// <summary>
        /// Default embedding model
        /// </summary>
        /// <example>text-embedding-3-small</example>
        public string DefaultEmbeddingModel { get; set; } = string.Empty;

        /// <summary>
        /// Available models for this provider
        /// </summary>
        /// <example>["gpt-5.1", "gpt-5", "gpt-4o", "gpt-4o-mini", "text-embedding-3-small", "text-embedding-3-large"]</example>
        public List<string> AvailableModels { get; set; } = new List<string>();

        /// <summary>
        /// Provider capabilities
        /// </summary>
        /// <example>["TextGeneration", "Embeddings", "ChatCompletion"]</example>
        public List<string> Capabilities { get; set; } = new List<string>();

        /// <summary>
        /// Maximum tokens supported
        /// </summary>
        /// <example>4000</example>
        public int MaxTokens { get; set; }

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        /// <example>30</example>
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// Maximum retry attempts
        /// </summary>
        /// <example>3</example>
        public int MaxRetries { get; set; }

        /// <summary>
        /// Whether the provider is enabled
        /// </summary>
        /// <example>true</example>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Connection health status
        /// </summary>
        public ProviderHealthStatus Health { get; set; } = new ProviderHealthStatus();

        /// <summary>
        /// Usage statistics for this provider
        /// </summary>
        public ProviderUsageStats Usage { get; set; } = new ProviderUsageStats();
    }

    /// <summary>
    /// Response model for storage configuration
    /// </summary>
    public class StorageConfigResponse : ConfigurationResponse
    {
        /// <summary>
        /// Storage provider type
        /// </summary>
        /// <example>Qdrant</example>
        public StorageProvider Provider { get; set; }

        /// <summary>
        /// Provider display name
        /// </summary>
        /// <example>Qdrant Vector Database</example>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Connection string (may be masked for security)
        /// </summary>
        /// <example>http://localhost:6333</example>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Collection or database name
        /// </summary>
        /// <example>smartrag_documents</example>
        public string CollectionName { get; set; } = string.Empty;

        /// <summary>
        /// Vector dimensions
        /// </summary>
        /// <example>1536</example>
        public int VectorDimensions { get; set; }

        /// <summary>
        /// Maximum search results
        /// </summary>
        /// <example>10</example>
        public int MaxSearchResults { get; set; }

        /// <summary>
        /// Connection pool size
        /// </summary>
        /// <example>10</example>
        public int ConnectionPoolSize { get; set; }

        /// <summary>
        /// Whether the provider is enabled
        /// </summary>
        /// <example>true</example>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Storage health status
        /// </summary>
        public StorageHealthStatus Health { get; set; } = new StorageHealthStatus();

        /// <summary>
        /// Storage usage statistics
        /// </summary>
        public StorageUsageStats Usage { get; set; } = new StorageUsageStats();
    }

    /// <summary>
    /// Response model for system configuration
    /// </summary>
    public class SystemConfigResponse : ConfigurationResponse
    {
        /// <summary>
        /// Maximum file size in MB
        /// </summary>
        /// <example>100</example>
        public int MaxFileSizeMB { get; set; }

        /// <summary>
        /// Allowed file extensions
        /// </summary>
        /// <example>[".pdf", ".docx", ".txt", ".md"]</example>
        public List<string> AllowedFileExtensions { get; set; } = new List<string>();

        /// <summary>
        /// Default processing language
        /// </summary>
        /// <example>en</example>
        public string DefaultLanguage { get; set; } = string.Empty;

        /// <summary>
        /// Whether detailed logging is enabled
        /// </summary>
        /// <example>true</example>
        public bool EnableDetailedLogging { get; set; }

        /// <summary>
        /// Whether analytics collection is enabled
        /// </summary>
        /// <example>true</example>
        public bool EnableAnalytics { get; set; }

        /// <summary>
        /// Analytics retention period in days
        /// </summary>
        /// <example>90</example>
        public int AnalyticsRetentionDays { get; set; }

        /// <summary>
        /// Whether CORS is enabled
        /// </summary>
        /// <example>true</example>
        public bool EnableCORS { get; set; }

        /// <summary>
        /// Allowed CORS origins
        /// </summary>
        /// <example>["https://localhost:3000", "https://myapp.com"]</example>
        public List<string> CORSOrigins { get; set; } = new List<string>();

        /// <summary>
        /// Rate limit per minute per user
        /// </summary>
        /// <example>100</example>
        public int RateLimitPerMinute { get; set; }

        /// <summary>
        /// Whether caching is enabled
        /// </summary>
        /// <example>true</example>
        public bool EnableCaching { get; set; }

        /// <summary>
        /// Cache TTL in minutes
        /// </summary>
        /// <example>30</example>
        public int CacheTTLMinutes { get; set; }

        /// <summary>
        /// System health status
        /// </summary>
        public SystemHealthStatus Health { get; set; } = new SystemHealthStatus();

        /// <summary>
        /// System performance metrics
        /// </summary>
        public SystemPerformanceMetrics Performance { get; set; } = new SystemPerformanceMetrics();
    }

    /// <summary>
    /// Response model for conversation configuration
    /// </summary>
    public class ConversationConfigResponse : ConfigurationResponse
    {
        /// <summary>
        /// Default maximum history length
        /// </summary>
        /// <example>50</example>
        public int DefaultMaxHistoryLength { get; set; }

        /// <summary>
        /// Maximum concurrent conversations per user
        /// </summary>
        /// <example>10</example>
        public int MaxConcurrentConversations { get; set; }

        /// <summary>
        /// Idle timeout in minutes
        /// </summary>
        /// <example>60</example>
        public int IdleTimeoutMinutes { get; set; }

        /// <summary>
        /// Auto-archive after days of inactivity
        /// </summary>
        /// <example>30</example>
        public int AutoArchiveDays { get; set; }

        /// <summary>
        /// Whether conversation analytics are enabled
        /// </summary>
        /// <example>true</example>
        public bool EnableAnalytics { get; set; }

        /// <summary>
        /// Whether conversation export is enabled
        /// </summary>
        /// <example>true</example>
        public bool EnableExport { get; set; }

        /// <summary>
        /// Default export format
        /// </summary>
        /// <example>json</example>
        public string DefaultExportFormat { get; set; } = string.Empty;

        /// <summary>
        /// Whether message editing is enabled
        /// </summary>
        /// <example>false</example>
        public bool EnableMessageEditing { get; set; }

        /// <summary>
        /// Whether conversation sharing is enabled
        /// </summary>
        /// <example>false</example>
        public bool EnableConversationSharing { get; set; }

        /// <summary>
        /// Conversation usage statistics
        /// </summary>
        public ConversationUsageStats Usage { get; set; } = new ConversationUsageStats();
    }

    /// <summary>
    /// Response model for configuration validation
    /// </summary>
    public class ConfigValidationResponse
    {
        /// <summary>
        /// Configuration section that was validated
        /// </summary>
        /// <example>AIProvider</example>
        public string Section { get; set; } = string.Empty;

        /// <summary>
        /// Whether the configuration is valid
        /// </summary>
        /// <example>true</example>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation errors (if any)
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();

        /// <summary>
        /// Validation warnings (non-critical issues)
        /// </summary>
        public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();

        /// <summary>
        /// Connection test results (if requested)
        /// </summary>
        public ConnectionTestResult ConnectionTest { get; set; } = new ConnectionTestResult();

        /// <summary>
        /// Schema validation results (if requested)
        /// </summary>
        public SchemaValidationResult SchemaValidation { get; set; } = new SchemaValidationResult();

        /// <summary>
        /// Validation timestamp
        /// </summary>
        /// <example>2024-01-15T10:30:00Z</example>
        public DateTime ValidatedAt { get; set; }

        /// <summary>
        /// Time taken for validation
        /// </summary>
        /// <example>1.25</example>
        public double ValidationTimeSeconds { get; set; }
    }

    /// <summary>
    /// Response model for configuration backup
    /// </summary>
    public class ConfigBackupResponse
    {
        /// <summary>
        /// Backup ID for tracking
        /// </summary>
        /// <example>backup_20240115_103000</example>
        public string BackupId { get; set; } = string.Empty;

        /// <summary>
        /// Backup creation timestamp
        /// </summary>
        /// <example>2024-01-15T10:30:00Z</example>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Configuration sections included in backup
        /// </summary>
        /// <example>["AIProvider", "Storage", "System"]</example>
        public List<string> IncludedSections { get; set; } = new List<string>();

        /// <summary>
        /// Backup file size in bytes
        /// </summary>
        /// <example>2048</example>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Whether backup is compressed
        /// </summary>
        /// <example>true</example>
        public bool IsCompressed { get; set; }

        /// <summary>
        /// Backup format
        /// </summary>
        /// <example>json</example>
        public string Format { get; set; } = string.Empty;

        /// <summary>
        /// Whether sensitive data is included
        /// </summary>
        /// <example>false</example>
        public bool IncludesSensitiveData { get; set; }

        /// <summary>
        /// Backup data (if requested inline)
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    #region Supporting Models

    /// <summary>
    /// Provider health status information
    /// </summary>
    public class ProviderHealthStatus
    {
        /// <summary>
        /// Whether the provider is healthy
        /// </summary>
        /// <example>true</example>
        public bool IsHealthy { get; set; } = true;

        /// <summary>
        /// Last health check timestamp
        /// </summary>
        /// <example>2024-01-15T10:25:00Z</example>
        public DateTime LastChecked { get; set; }

        /// <summary>
        /// Response time for last health check
        /// </summary>
        /// <example>0.85</example>
        public double ResponseTimeSeconds { get; set; }

        /// <summary>
        /// Health check error message (if unhealthy)
        /// </summary>
        /// <example>null</example>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Provider status code
        /// </summary>
        /// <example>200</example>
        public int StatusCode { get; set; }
    }

    /// <summary>
    /// Provider usage statistics
    /// </summary>
    public class ProviderUsageStats
    {
        /// <summary>
        /// Total requests made to this provider
        /// </summary>
        /// <example>1250</example>
        public long TotalRequests { get; set; }

        /// <summary>
        /// Successful requests
        /// </summary>
        /// <example>1200</example>
        public long SuccessfulRequests { get; set; }

        /// <summary>
        /// Failed requests
        /// </summary>
        /// <example>50</example>
        public long FailedRequests { get; set; }

        /// <summary>
        /// Average response time
        /// </summary>
        /// <example>1.35</example>
        public double AverageResponseTime { get; set; }

        /// <summary>
        /// Total tokens consumed
        /// </summary>
        /// <example>125000</example>
        public long TotalTokens { get; set; }

        /// <summary>
        /// Last request timestamp
        /// </summary>
        /// <example>2024-01-15T10:30:00Z</example>
        public DateTime LastRequest { get; set; }
    }

    /// <summary>
    /// Storage health status information
    /// </summary>
    public class StorageHealthStatus
    {
        /// <summary>
        /// Whether storage is healthy
        /// </summary>
        /// <example>true</example>
        public bool IsHealthy { get; set; } = true;

        /// <summary>
        /// Storage connection status
        /// </summary>
        /// <example>Connected</example>
        public string ConnectionStatus { get; set; } = string.Empty;

        /// <summary>
        /// Available storage space
        /// </summary>
        /// <example>85.5</example>
        public double AvailableSpaceGB { get; set; }

        /// <summary>
        /// Used storage space
        /// </summary>
        /// <example>14.5</example>
        public double UsedSpaceGB { get; set; }

        /// <summary>
        /// Last health check timestamp
        /// </summary>
        /// <example>2024-01-15T10:25:00Z</example>
        public DateTime LastChecked { get; set; }
    }

    /// <summary>
    /// Storage usage statistics
    /// </summary>
    public class StorageUsageStats
    {
        /// <summary>
        /// Total documents stored
        /// </summary>
        /// <example>1500</example>
        public long TotalDocuments { get; set; }

        /// <summary>
        /// Total vectors stored
        /// </summary>
        /// <example>15000</example>
        public long TotalVectors { get; set; }

        /// <summary>
        /// Average search response time
        /// </summary>
        /// <example>0.25</example>
        public double AverageSearchTime { get; set; }

        /// <summary>
        /// Total searches performed
        /// </summary>
        /// <example>5000</example>
        public long TotalSearches { get; set; }

        /// <summary>
        /// Last document upload timestamp
        /// </summary>
        /// <example>2024-01-15T10:15:00Z</example>
        public DateTime LastUpload { get; set; }
    }

    /// <summary>
    /// System health status information
    /// </summary>
    public class SystemHealthStatus
    {
        /// <summary>
        /// Overall system health
        /// </summary>
        /// <example>Healthy</example>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// System uptime
        /// </summary>
        /// <example>15.05:30:45</example>
        public TimeSpan Uptime { get; set; }

        /// <summary>
        /// Memory usage percentage
        /// </summary>
        /// <example>45.2</example>
        public double MemoryUsagePercent { get; set; }

        /// <summary>
        /// CPU usage percentage
        /// </summary>
        /// <example>18.5</example>
        public double CpuUsagePercent { get; set; }

        /// <summary>
        /// Disk usage percentage
        /// </summary>
        /// <example>65.8</example>
        public double DiskUsagePercent { get; set; }

        /// <summary>
        /// Active connections
        /// </summary>
        /// <example>25</example>
        public int ActiveConnections { get; set; }
    }

    /// <summary>
    /// System performance metrics
    /// </summary>
    public class SystemPerformanceMetrics
    {
        /// <summary>
        /// Requests per minute
        /// </summary>
        /// <example>45</example>
        public double RequestsPerMinute { get; set; }

        /// <summary>
        /// Average request duration
        /// </summary>
        /// <example>1.25</example>
        public double AverageRequestDuration { get; set; }

        /// <summary>
        /// Error rate percentage
        /// </summary>
        /// <example>2.1</example>
        public double ErrorRatePercent { get; set; }

        /// <summary>
        /// Cache hit rate percentage
        /// </summary>
        /// <example>85.5</example>
        public double CacheHitRatePercent { get; set; }

        /// <summary>
        /// Total requests processed
        /// </summary>
        /// <example>125000</example>
        public long TotalRequests { get; set; }
    }

    /// <summary>
    /// Conversation usage statistics
    /// </summary>
    public class ConversationUsageStats
    {
        /// <summary>
        /// Total active conversations
        /// </summary>
        /// <example>150</example>
        public int ActiveConversations { get; set; }

        /// <summary>
        /// Total archived conversations
        /// </summary>
        /// <example>500</example>
        public int ArchivedConversations { get; set; }

        /// <summary>
        /// Average conversation length
        /// </summary>
        /// <example>8.5</example>
        public double AverageConversationLength { get; set; }

        /// <summary>
        /// Total messages exchanged
        /// </summary>
        /// <example>5500</example>
        public long TotalMessages { get; set; }

        /// <summary>
        /// Average session duration
        /// </summary>
        /// <example>00:25:30</example>
        public TimeSpan AverageSessionDuration { get; set; }
    }

    /// <summary>
    /// Validation error information
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Error code
        /// </summary>
        /// <example>INVALID_API_KEY</example>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Error message
        /// </summary>
        /// <example>API key format is invalid</example>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Field that caused the error
        /// </summary>
        /// <example>apiKey</example>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Error severity
        /// </summary>
        /// <example>High</example>
        public string Severity { get; set; } = string.Empty;
    }

    /// <summary>
    /// Validation warning information
    /// </summary>
    public class ValidationWarning
    {
        /// <summary>
        /// Warning code
        /// </summary>
        /// <example>DEPRECATED_SETTING</example>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Warning message
        /// </summary>
        /// <example>This setting is deprecated and will be removed in future versions</example>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Field that caused the warning
        /// </summary>
        /// <example>legacySetting</example>
        public string Field { get; set; } = string.Empty;
    }

    /// <summary>
    /// Connection test result
    /// </summary>
    public class ConnectionTestResult
    {
        /// <summary>
        /// Whether connection test passed
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }

        /// <summary>
        /// Connection response time
        /// </summary>
        /// <example>0.85</example>
        public double ResponseTimeSeconds { get; set; }

        /// <summary>
        /// Error message if connection failed
        /// </summary>
        /// <example>null</example>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Connection status details
        /// </summary>
        /// <example>Connected successfully to OpenAI API</example>
        public string Details { get; set; } = string.Empty;
    }

    /// <summary>
    /// Schema validation result
    /// </summary>
    public class SchemaValidationResult
    {
        /// <summary>
        /// Whether schema validation passed
        /// </summary>
        /// <example>true</example>
        public bool IsValid { get; set; }

        /// <summary>
        /// Schema version used for validation
        /// </summary>
        /// <example>1.0.0</example>
        public string SchemaVersion { get; set; } = string.Empty;

        /// <summary>
        /// Schema validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Missing required fields
        /// </summary>
        public List<string> MissingFields { get; set; } = new List<string>();

        /// <summary>
        /// Invalid field types
        /// </summary>
        public List<string> InvalidTypes { get; set; } = new List<string>();
    }

    #endregion
}
