#nullable enable

using SmartRAG.Enums;
using System.Collections.Generic;

namespace SmartRAG.Models
{

    /// <summary>
    /// Configuration options for SmartRag library
    /// </summary>
    public class SmartRagOptions
    {
        /// <summary>
        /// AI provider to use for text generation and embeddings
        /// </summary>
        public AIProvider AIProvider { get; set; }

        /// <summary>
        /// Storage provider to use for document storage
        /// </summary>
        public StorageProvider StorageProvider { get; set; }

        /// <summary>
        /// Storage provider to use for conversation history storage
        /// If not specified, uses the same as StorageProvider (excluding Qdrant)
        /// </summary>
        public ConversationStorageProvider? ConversationStorageProvider { get; set; }

        /// <summary>
        /// Maximum size of each document chunk in characters
        /// </summary>
        public int MaxChunkSize { get; set; } = 1000;

        /// <summary>
        /// Minimum size of each document chunk in characters
        /// </summary>
        public int MinChunkSize { get; set; } = 100;

        /// <summary>
        /// Number of characters to overlap between adjacent chunks (for better context preservation)
        /// </summary>
        public int ChunkOverlap { get; set; } = 200;

        /// <summary>
        /// Maximum number of retry attempts for AI provider requests
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts in milliseconds
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Retry policy for failed requests
        /// </summary>
        public RetryPolicy RetryPolicy { get; set; } = RetryPolicy.ExponentialBackoff;

        /// <summary>
        /// Whether to enable fallback providers when primary provider fails
        /// </summary>
        public bool EnableFallbackProviders { get; set; }

        /// <summary>
        /// List of fallback AI providers to try when primary provider fails
        /// </summary>
        public List<AIProvider> FallbackProviders { get; set; } = new List<AIProvider>();

        /// <summary>
        /// Whisper configuration for local audio transcription
        /// </summary>
        public WhisperConfig WhisperConfig { get; set; } = new WhisperConfig();

        /// <summary>
        /// Multi-database connections for intelligent cross-database querying
        /// </summary>
        public List<DatabaseConnectionConfig> DatabaseConnections { get; set; } = new List<DatabaseConnectionConfig>();

        /// <summary>
        /// Enable automatic schema analysis on startup
        /// </summary>
        public bool EnableAutoSchemaAnalysis { get; set; } = true;

        /// <summary>
        /// Feature toggles for enabling/disabling specific capabilities
        /// </summary>
        public FeatureToggles Features { get; set; } = new FeatureToggles();

        /// <summary>
        /// MCP Server configurations for external MCP server connections
        /// </summary>
        public List<McpServerConfig> McpServers { get; set; } = new List<McpServerConfig>();

        /// <summary>
        /// Watched folder configurations for automatic document indexing
        /// </summary>
        public List<WatchedFolderConfig> WatchedFolders { get; set; } = new List<WatchedFolderConfig>();

        /// <summary>
        /// Default language code for document processing (ISO 639-1 format, e.g., "tr", "en", "de")
        /// Used when language is not specified in WatchedFolderConfig or document upload
        /// </summary>
        public string? DefaultLanguage { get; set; }
    }

    /// <summary>
    /// Feature toggles configuration
    /// </summary>
    public class FeatureToggles
    {
        /// <summary>
        /// Enable database search functionality
        /// </summary>
        public bool EnableDatabaseSearch { get; set; } = true;

        /// <summary>
        /// Enable document search functionality
        /// </summary>
        public bool EnableDocumentSearch { get; set; } = true;

        /// <summary>
        /// Enable audio file parsing and transcription
        /// </summary>
        public bool EnableAudioParsing { get; set; } = true;

        /// <summary>
        /// Enable image parsing with OCR
        /// </summary>
        public bool EnableImageParsing { get; set; } = true;

        /// <summary>
        /// Enable MCP Client support
        /// </summary>
        public bool EnableMcpClient { get; set; } = false;

        /// <summary>
        /// Enable File Watcher support
        /// </summary>
        public bool EnableFileWatcher { get; set; } = false;
    }
}
