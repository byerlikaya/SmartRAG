using System.Data.Common;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using Npgsql;
using StackExchange.Redis;
using SmartRAG.Interfaces.Database;
using SmartRAG.Interfaces.Health;
using SmartRAG.Models.Configuration;
using SmartRAG.Models.Health;

namespace SmartRAG.Services.Health;

/// <summary>
/// Service for checking health status of SmartRAG components (AI, storage, databases)
/// </summary>
public sealed class HealthCheckService : IHealthCheckService
{
    private const int DefaultHttpTimeoutSeconds = 5;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IOptions<SmartRagOptions> _options;
    private readonly IDatabaseConnectionManager? _connectionManager;

    public HealthCheckService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IOptions<SmartRagOptions> options,
        IDatabaseConnectionManager? connectionManager = null)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _options = options;
        _connectionManager = connectionManager;
    }

    public async Task<HealthCheckResult> RunFullHealthCheckAsync(CancellationToken cancellationToken = default)
    {
        var result = new HealthCheckResult();

        result.Ai = await CheckAiProviderAsync(cancellationToken).ConfigureAwait(false);

        var storageProvider = _options.Value.StorageProvider;
        if (storageProvider == StorageProvider.Qdrant)
        {
            var qdrantHost = _configuration["Storage:Qdrant:Host"] ?? "http://localhost:6333";
            var useHttps = _configuration.GetValue<bool>("Storage:Qdrant:UseHttps");
            var qdrantUrl = BuildQdrantUrl(qdrantHost, useHttps);
            result.Storage = await CheckQdrantAsync(qdrantUrl, cancellationToken).ConfigureAwait(false);
        }

        var redisConnection = _configuration["Storage:Redis:ConnectionString"]
            ?? _configuration["ConnectionStrings:Redis"]
            ?? "localhost:6379";
        result.Conversation = await CheckRedisAsync(redisConnection, cancellationToken).ConfigureAwait(false);

        if (_connectionManager != null && _options.Value.Features.EnableDatabaseSearch)
        {
            var connections = await _connectionManager.GetAllConnectionsAsync(cancellationToken).ConfigureAwait(false);
            foreach (var conn in connections)
            {
                var dbStatus = await CheckDatabaseAsync(conn.ConnectionString, conn.DatabaseType, cancellationToken).ConfigureAwait(false);
                dbStatus.ServiceName = conn.Name ?? dbStatus.ServiceName;
                result.Databases.Add(dbStatus);
            }
        }

        return result;
    }

    private async Task<HealthStatus> CheckAiProviderAsync(CancellationToken cancellationToken)
    {
        var provider = _options.Value.AIProvider;
        var displayName = GetAiProviderDisplayName(provider);

        switch (provider)
        {
            case AIProvider.Custom:
                var endpoint = _configuration["AI:Custom:Endpoint"] ?? "http://localhost:11434";
                var baseUrl = ExtractBaseEndpoint(endpoint);
                return await CheckOllamaAsync(baseUrl, cancellationToken).ConfigureAwait(false);
            case AIProvider.Gemini:
                var geminiKey = _configuration["AI:Gemini:ApiKey"];
                return new HealthStatus
                {
                    ServiceName = displayName,
                    IsHealthy = !string.IsNullOrWhiteSpace(geminiKey),
                    Message = !string.IsNullOrWhiteSpace(geminiKey) ? "API key configured" : "API key not configured",
                    Details = !string.IsNullOrWhiteSpace(geminiKey) ? "Endpoint: https://generativelanguage.googleapis.com" : "Please configure AI:Gemini:ApiKey in appsettings"
                };
            case AIProvider.OpenAI:
                var openAiKey = _configuration["AI:OpenAI:ApiKey"];
                return new HealthStatus
                {
                    ServiceName = displayName,
                    IsHealthy = !string.IsNullOrWhiteSpace(openAiKey),
                    Message = !string.IsNullOrWhiteSpace(openAiKey) ? "API key configured" : "API key not configured",
                    Details = !string.IsNullOrWhiteSpace(openAiKey) ? "Endpoint: https://api.openai.com" : "Please configure AI:OpenAI:ApiKey in appsettings"
                };
            case AIProvider.AzureOpenAI:
                var azureKey = _configuration["AI:AzureOpenAI:ApiKey"];
                var azureEndpoint = _configuration["AI:AzureOpenAI:Endpoint"];
                var azureOk = !string.IsNullOrWhiteSpace(azureKey) && !string.IsNullOrWhiteSpace(azureEndpoint);
                return new HealthStatus
                {
                    ServiceName = displayName,
                    IsHealthy = azureOk,
                    Message = azureOk ? "API key and endpoint configured" : "API key or endpoint not configured",
                    Details = azureOk ? $"Endpoint: {azureEndpoint}" : "Please configure AI:AzureOpenAI:ApiKey and AI:AzureOpenAI:Endpoint in appsettings"
                };
            case AIProvider.Anthropic:
                var anthropicKey = _configuration["AI:Anthropic:ApiKey"];
                return new HealthStatus
                {
                    ServiceName = displayName,
                    IsHealthy = !string.IsNullOrWhiteSpace(anthropicKey),
                    Message = !string.IsNullOrWhiteSpace(anthropicKey) ? "API key configured" : "API key not configured",
                    Details = !string.IsNullOrWhiteSpace(anthropicKey) ? "Endpoint: https://api.anthropic.com" : "Please configure AI:Anthropic:ApiKey in appsettings"
                };
            default:
                return new HealthStatus
                {
                    ServiceName = displayName,
                    IsHealthy = false,
                    Message = "Unknown AI provider",
                    Details = $"Provider: {provider}"
                };
        }
    }

    private async Task<HealthStatus> CheckOllamaAsync(string endpoint, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(DefaultHttpTimeoutSeconds);
            var response = await client.GetAsync($"{endpoint}/api/tags", cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                var json = await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
                var modelCount = 0;
                if (json.TryGetProperty("models", out var models))
                {
                    modelCount = models.GetArrayLength();
                }
                return new HealthStatus
                {
                    ServiceName = "Ollama",
                    IsHealthy = true,
                    Message = $"Service running - {modelCount} model(s) installed",
                    Details = $"Endpoint: {endpoint}"
                };
            }
            return new HealthStatus
            {
                ServiceName = "Ollama",
                IsHealthy = false,
                Message = "Service returned error",
                Details = $"Status: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new HealthStatus
            {
                ServiceName = "Ollama",
                IsHealthy = false,
                Message = "Service not accessible",
                Details = ex.Message
            };
        }
    }

    private async Task<HealthStatus> CheckQdrantAsync(string host, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(DefaultHttpTimeoutSeconds);
            var response = await client.GetAsync($"{host}/", cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (content.Contains("qdrant", StringComparison.OrdinalIgnoreCase))
                {
                    return new HealthStatus
                    {
                        ServiceName = "Qdrant",
                        IsHealthy = true,
                        Message = "Vector database is healthy",
                        Details = $"Host: {host}"
                    };
                }
            }
            return new HealthStatus
            {
                ServiceName = "Qdrant",
                IsHealthy = false,
                Message = "Service returned error",
                Details = $"Status: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new HealthStatus
            {
                ServiceName = "Qdrant",
                IsHealthy = false,
                Message = "Vector database not accessible",
                Details = ex.Message
            };
        }
    }

    private async Task<HealthStatus> CheckRedisAsync(string connectionString, CancellationToken cancellationToken)
    {
        try
        {
            var redis = await ConnectionMultiplexer.ConnectAsync(connectionString).ConfigureAwait(false);
            var db = redis.GetDatabase();
            await db.PingAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            return new HealthStatus
            {
                ServiceName = "Redis",
                IsHealthy = true,
                Message = "Cache service is healthy"
            };
        }
        catch (Exception ex)
        {
            return new HealthStatus
            {
                ServiceName = "Redis",
                IsHealthy = false,
                Message = "Cache service not accessible",
                Details = ex.Message
            };
        }
    }

    private async Task<HealthStatus> CheckDatabaseAsync(string connectionString, DatabaseType databaseType, CancellationToken cancellationToken)
    {
        var serviceName = databaseType switch
        {
            DatabaseType.SqlServer => "SQL Server",
            DatabaseType.MySQL => "MySQL",
            DatabaseType.PostgreSQL => "PostgreSQL",
            DatabaseType.SQLite => "SQLite",
            _ => databaseType.ToString()
        };

        try
        {
            await using DbConnection connection = databaseType switch
            {
                DatabaseType.SqlServer => new SqlConnection(connectionString),
                DatabaseType.MySQL => new MySqlConnection(connectionString),
                DatabaseType.PostgreSQL => new NpgsqlConnection(connectionString),
                DatabaseType.SQLite => new SqliteConnection(connectionString),
                _ => throw new NotSupportedException($"Database type {databaseType} is not supported for health check")
            };
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return new HealthStatus
            {
                ServiceName = serviceName,
                IsHealthy = true,
                Message = "Database connection successful",
                Details = $"State: {connection.State}"
            };
        }
        catch (Exception ex)
        {
            return new HealthStatus
            {
                ServiceName = serviceName,
                IsHealthy = false,
                Message = "Database connection failed",
                Details = ex.Message
            };
        }
    }

    private static string GetAiProviderDisplayName(AIProvider provider)
    {
        return provider switch
        {
            AIProvider.Custom => "Ollama",
            AIProvider.Gemini => "Google Gemini",
            AIProvider.OpenAI => "OpenAI GPT",
            AIProvider.AzureOpenAI => "Azure OpenAI",
            AIProvider.Anthropic => "Anthropic Claude",
            _ => provider.ToString()
        };
    }

    private static string ExtractBaseEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return "http://localhost:11434";
        }
        try
        {
            var uri = new Uri(endpoint);
            return $"{uri.Scheme}://{uri.Authority}";
        }
        catch
        {
            return endpoint.Contains("localhost:11434", StringComparison.OrdinalIgnoreCase)
                ? "http://localhost:11434"
                : endpoint;
        }
    }

    private static string BuildQdrantUrl(string host, bool useHttps)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return "http://localhost:6333";
        }
        if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return host.TrimEnd('/');
        }
        var scheme = useHttps ? "https" : "http";
        return $"{scheme}://{host.TrimEnd('/')}";
    }
}
