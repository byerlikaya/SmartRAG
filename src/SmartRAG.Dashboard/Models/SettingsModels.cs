using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartRAG.Dashboard.Models;

/// <summary>
/// Full settings response for the Settings dashboard view (appsettings-derived).
/// </summary>
public sealed class SettingsResponse
{
    [JsonPropertyName("providers")]
    public SettingsProviders Providers { get; set; } = new SettingsProviders();

    [JsonPropertyName("features")]
    public SettingsFeatures Features { get; set; } = new SettingsFeatures();

    [JsonPropertyName("chunking")]
    public SettingsChunking Chunking { get; set; } = new SettingsChunking();

    [JsonPropertyName("retry")]
    public SettingsRetry Retry { get; set; } = new SettingsRetry();

    [JsonPropertyName("whisper")]
    public SettingsWhisper Whisper { get; set; } = new SettingsWhisper();

    [JsonPropertyName("activeAi")]
    public SettingsActiveAi ActiveAi { get; set; } = new SettingsActiveAi();

    [JsonPropertyName("mcpServers")]
    public List<SettingsMcpServer> McpServers { get; set; } = new List<SettingsMcpServer>();

    [JsonPropertyName("watchedFolders")]
    public List<SettingsWatchedFolder> WatchedFolders { get; set; } = new List<SettingsWatchedFolder>();

    [JsonPropertyName("databaseConnections")]
    public List<SettingsDatabaseConnection> DatabaseConnections { get; set; } = new List<SettingsDatabaseConnection>();

    /// <summary>
    /// Config keys not shown in the categories above, grouped by first path segment (e.g. "MaxSearchResults", "EnableAutoSchemaAnalysis").
    /// </summary>
    [JsonPropertyName("remainingByCategory")]
    public Dictionary<string, List<SettingsEntry>> RemainingByCategory { get; set; } = new Dictionary<string, List<SettingsEntry>>();
}

public sealed class SettingsEntry
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public sealed class SettingsProviders
{
    [JsonPropertyName("ai")]
    public string Ai { get; set; } = string.Empty;

    [JsonPropertyName("storage")]
    public string Storage { get; set; } = string.Empty;

    [JsonPropertyName("conversation")]
    public string Conversation { get; set; } = string.Empty;
}

public sealed class SettingsFeatures
{
    [JsonPropertyName("enableDatabaseSearch")]
    public bool EnableDatabaseSearch { get; set; }

    [JsonPropertyName("enableDocumentSearch")]
    public bool EnableDocumentSearch { get; set; }

    [JsonPropertyName("enableAudioSearch")]
    public bool EnableAudioSearch { get; set; }

    [JsonPropertyName("enableImageSearch")]
    public bool EnableImageSearch { get; set; }

    [JsonPropertyName("enableMcpSearch")]
    public bool EnableMcpSearch { get; set; }

    [JsonPropertyName("enableFileWatcher")]
    public bool EnableFileWatcher { get; set; }
}

public sealed class SettingsChunking
{
    [JsonPropertyName("maxChunkSize")]
    public int MaxChunkSize { get; set; }

    [JsonPropertyName("minChunkSize")]
    public int MinChunkSize { get; set; }

    [JsonPropertyName("chunkOverlap")]
    public int ChunkOverlap { get; set; }
}

public sealed class SettingsRetry
{
    [JsonPropertyName("maxRetryAttempts")]
    public int MaxRetryAttempts { get; set; }

    [JsonPropertyName("retryDelayMs")]
    public int RetryDelayMs { get; set; }

    [JsonPropertyName("retryPolicy")]
    public string RetryPolicy { get; set; } = string.Empty;

    [JsonPropertyName("enableFallbackProviders")]
    public bool EnableFallbackProviders { get; set; }

    [JsonPropertyName("fallbackProviders")]
    public List<string> FallbackProviders { get; set; } = new List<string>();
}

public sealed class SettingsWhisper
{
    [JsonPropertyName("modelPath")]
    public string ModelPath { get; set; } = string.Empty;

    [JsonPropertyName("defaultLanguage")]
    public string DefaultLanguage { get; set; } = string.Empty;

    [JsonPropertyName("minConfidenceThreshold")]
    public double MinConfidenceThreshold { get; set; }

    [JsonPropertyName("includeWordTimestamps")]
    public bool IncludeWordTimestamps { get; set; }

    [JsonPropertyName("maxThreads")]
    public int MaxThreads { get; set; }
}

public sealed class SettingsActiveAi
{
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("maxTokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;
}

public sealed class SettingsMcpServer
{
    [JsonPropertyName("serverId")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [JsonPropertyName("autoConnect")]
    public bool AutoConnect { get; set; }

    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; }
}

public sealed class SettingsWatchedFolder
{
    [JsonPropertyName("folderPath")]
    public string FolderPath { get; set; } = string.Empty;

    [JsonPropertyName("allowedExtensions")]
    public List<string> AllowedExtensions { get; set; } = new List<string>();

    [JsonPropertyName("includeSubdirectories")]
    public bool IncludeSubdirectories { get; set; }

    [JsonPropertyName("autoUpload")]
    public bool AutoUpload { get; set; }

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
}

public sealed class SettingsDatabaseConnection
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("databaseType")]
    public string DatabaseType { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}
