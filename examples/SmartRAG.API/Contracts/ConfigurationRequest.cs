using SmartRAG.Enums;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SmartRAG.API.Contracts
{
    /// <summary>
    /// Request model for updating AI provider configuration
    /// </summary>
    public class AIProviderConfigRequest
    {
        /// <summary>
        /// AI provider to configure
        /// </summary>
        /// <example>OpenAI</example>
        [Required]
        public AIProvider Provider { get; set; }

        /// <summary>
        /// API key for the provider
        /// </summary>
        /// <example>sk-1234567890abcdef...</example>
        [Required]
        [MinLength(10)]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Base URL for the provider (optional for standard providers)
        /// </summary>
        /// <example>https://api.openai.com/v1</example>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Default model to use with this provider
        /// </summary>
        /// <example>gpt-5.1</example>
        public string DefaultModel { get; set; } = string.Empty;

        /// <summary>
        /// Default embedding model for this provider
        /// </summary>
        /// <example>text-embedding-3-small</example>
        public string DefaultEmbeddingModel { get; set; } = string.Empty;

        /// <summary>
        /// Maximum tokens per request
        /// </summary>
        /// <example>4000</example>
        [Range(1, 32000)]
        [DefaultValue(4000)]
        public int MaxTokens { get; set; } = 4000;

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        /// <example>30</example>
        [Range(1, 300)]
        [DefaultValue(30)]
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum retry attempts for failed requests
        /// </summary>
        /// <example>3</example>
        [Range(0, 10)]
        [DefaultValue(3)]
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Whether this provider is enabled
        /// </summary>
        /// <example>true</example>
        [DefaultValue(true)]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Custom provider settings
        /// </summary>
        /// <example>{"temperature": "0.7", "top_p": "1.0"}</example>
        public Dictionary<string, string> CustomSettings { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Request model for updating storage configuration
    /// </summary>
    public class StorageConfigRequest
    {
        /// <summary>
        /// Storage provider to configure
        /// </summary>
        /// <example>Qdrant</example>
        [Required]
        public StorageProvider Provider { get; set; }

        /// <summary>
        /// Connection string for the storage provider
        /// </summary>
        /// <example>http://localhost:6333</example>
        [Required]
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Collection or database name
        /// </summary>
        /// <example>smartrag_documents</example>
        public string CollectionName { get; set; } = string.Empty;

        /// <summary>
        /// Vector dimensions for embeddings
        /// </summary>
        /// <example>1536</example>
        [Range(1, 4096)]
        [DefaultValue(1536)]
        public int VectorDimensions { get; set; } = 1536;

        /// <summary>
        /// Maximum number of results to return in searches
        /// </summary>
        /// <example>10</example>
        [Range(1, 100)]
        [DefaultValue(10)]
        public int MaxSearchResults { get; set; } = 10;

        /// <summary>
        /// Connection pool size
        /// </summary>
        /// <example>10</example>
        [Range(1, 100)]
        [DefaultValue(10)]
        public int ConnectionPoolSize { get; set; } = 10;

        /// <summary>
        /// Whether the storage provider is enabled
        /// </summary>
        /// <example>true</example>
        [DefaultValue(true)]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Custom storage settings
        /// </summary>
        /// <example>{"distance_metric": "cosine", "index_type": "hnsw"}</example>
        public Dictionary<string, string> CustomSettings { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Request model for updating system configuration
    /// </summary>
    public class SystemConfigRequest
    {
        /// <summary>
        /// Maximum file size for uploads in MB
        /// </summary>
        /// <example>100</example>
        [Range(1, 1000)]
        [DefaultValue(100)]
        public int MaxFileSizeMB { get; set; } = 100;

        /// <summary>
        /// Allowed file extensions for upload
        /// </summary>
        /// <example>[".pdf", ".docx", ".txt", ".md"]</example>
        public List<string> AllowedFileExtensions { get; set; } = new List<string>();

        /// <summary>
        /// Default language for processing
        /// </summary>
        /// <example>en</example>
        [DefaultValue("en")]
        public string DefaultLanguage { get; set; } = "en";

        /// <summary>
        /// Enable detailed logging
        /// </summary>
        /// <example>true</example>
        [DefaultValue(true)]
        public bool EnableDetailedLogging { get; set; } = true;

        /// <summary>
        /// Enable analytics collection
        /// </summary>
        /// <example>true</example>
        [DefaultValue(true)]
        public bool EnableAnalytics { get; set; } = true;

        /// <summary>
        /// Analytics retention period in days
        /// </summary>
        /// <example>90</example>
        [Range(1, 3650)]
        [DefaultValue(90)]
        public int AnalyticsRetentionDays { get; set; } = 90;

        /// <summary>
        /// Enable CORS for web applications
        /// </summary>
        /// <example>true</example>
        [DefaultValue(true)]
        public bool EnableCORS { get; set; } = true;

        /// <summary>
        /// Allowed CORS origins
        /// </summary>
        /// <example>["https://localhost:3000", "https://myapp.com"]</example>
        public List<string> CORSOrigins { get; set; } = new List<string>();

        /// <summary>
        /// Rate limiting: requests per minute per user
        /// </summary>
        /// <example>100</example>
        [Range(1, 10000)]
        [DefaultValue(100)]
        public int RateLimitPerMinute { get; set; } = 100;

        /// <summary>
        /// Enable request caching
        /// </summary>
        /// <example>true</example>
        [DefaultValue(true)]
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Cache TTL in minutes
        /// </summary>
        /// <example>30</example>
        [Range(1, 1440)]
        [DefaultValue(30)]
        public int CacheTTLMinutes { get; set; } = 30;
    }

    /// <summary>
    /// Request model for updating conversation configuration
    /// </summary>
    public class ConversationConfigRequest
    {
        /// <summary>
        /// Default maximum conversation history length
        /// </summary>
        /// <example>50</example>
        [Range(1, 1000)]
        [DefaultValue(50)]
        public int DefaultMaxHistoryLength { get; set; } = 50;

        /// <summary>
        /// Maximum concurrent conversations per user
        /// </summary>
        /// <example>10</example>
        [Range(1, 100)]
        [DefaultValue(10)]
        public int MaxConcurrentConversations { get; set; } = 10;

        /// <summary>
        /// Conversation idle timeout in minutes
        /// </summary>
        /// <example>60</example>
        [Range(1, 1440)]
        [DefaultValue(60)]
        public int IdleTimeoutMinutes { get; set; } = 60;

        /// <summary>
        /// Auto-archive conversations after days of inactivity
        /// </summary>
        /// <example>30</example>
        [Range(1, 365)]
        [DefaultValue(30)]
        public int AutoArchiveDays { get; set; } = 30;

        /// <summary>
        /// Enable conversation analytics
        /// </summary>
        /// <example>true</example>
        [DefaultValue(true)]
        public bool EnableAnalytics { get; set; } = true;

        /// <summary>
        /// Enable conversation export
        /// </summary>
        /// <example>true</example>
        [DefaultValue(true)]
        public bool EnableExport { get; set; } = true;

        /// <summary>
        /// Default export format
        /// </summary>
        /// <example>json</example>
        [DefaultValue("json")]
        public string DefaultExportFormat { get; set; } = "json";

        /// <summary>
        /// Enable message editing
        /// </summary>
        /// <example>false</example>
        [DefaultValue(false)]
        public bool EnableMessageEditing { get; set; } = false;

        /// <summary>
        /// Enable conversation sharing
        /// </summary>
        /// <example>false</example>
        [DefaultValue(false)]
        public bool EnableConversationSharing { get; set; } = false;
    }

    /// <summary>
    /// Request model for configuration validation
    /// </summary>
    public class ConfigValidationRequest
    {
        /// <summary>
        /// Configuration section to validate
        /// </summary>
        /// <example>AIProvider</example>
        [Required]
        public string Section { get; set; } = string.Empty;

        /// <summary>
        /// Configuration data to validate
        /// </summary>
        /// <example>{"provider": "OpenAI", "apiKey": "sk-123..."}</example>
        [Required]
        public Dictionary<string, object> ConfigData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Whether to test connectivity/functionality
        /// </summary>
        /// <example>true</example>
        [DefaultValue(true)]
        public bool TestConnection { get; set; } = true;

        /// <summary>
        /// Whether to validate against schema
        /// </summary>
        /// <example>true</example>
        [DefaultValue(true)]
        public bool ValidateSchema { get; set; } = true;
    }

    /// <summary>
    /// Request model for configuration backup and restore
    /// </summary>
    public class ConfigBackupRequest
    {
        /// <summary>
        /// Configuration sections to include in backup
        /// </summary>
        /// <example>["AIProvider", "Storage", "System"]</example>
        public List<string> Sections { get; set; } = new List<string>();

        /// <summary>
        /// Whether to include sensitive data (API keys, passwords)
        /// </summary>
        /// <example>false</example>
        [DefaultValue(false)]
        public bool IncludeSensitiveData { get; set; } = false;

        /// <summary>
        /// Backup format (json, yaml, xml)
        /// </summary>
        /// <example>json</example>
        [DefaultValue("json")]
        public string Format { get; set; } = "json";

        /// <summary>
        /// Whether to compress the backup
        /// </summary>
        /// <example>true</example>
        [DefaultValue(true)]
        public bool Compress { get; set; } = true;
    }

    /// <summary>
    /// Request model for configuration restore
    /// </summary>
    public class ConfigRestoreRequest
    {
        /// <summary>
        /// Backup data to restore
        /// </summary>
        /// <example>{"AIProvider": {...}, "Storage": {...}}</example>
        [Required]
        public Dictionary<string, object> BackupData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Configuration sections to restore
        /// </summary>
        /// <example>["AIProvider", "Storage"]</example>
        public List<string> Sections { get; set; } = new List<string>();

        /// <summary>
        /// Whether to validate configuration before restore
        /// </summary>
        /// <example>true</example>
        [DefaultValue(true)]
        public bool ValidateBeforeRestore { get; set; } = true;

        /// <summary>
        /// Whether to backup current configuration before restore
        /// </summary>
        /// <example>true</example>
        [DefaultValue(true)]
        public bool BackupCurrent { get; set; } = true;

        /// <summary>
        /// Whether to restart services after restore
        /// </summary>
        /// <example>false</example>
        [DefaultValue(false)]
        public bool RestartServices { get; set; } = false;
    }
}
