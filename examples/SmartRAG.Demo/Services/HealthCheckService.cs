using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using SmartRAG.Demo.Models;
using StackExchange.Redis;

namespace SmartRAG.Demo.Services;

/// <summary>
/// Service for checking health status of all SmartRAG Demo components
/// </summary>
public class HealthCheckService
{
    #region Constants

    private const int DefaultHttpTimeoutSeconds = 5;

    #endregion

    #region Fields

    private readonly HttpClient _httpClient;
    private readonly ILogger<HealthCheckService>? _logger;

    #endregion

    #region Constructor

    public HealthCheckService(ILogger<HealthCheckService>? logger = null)
    {
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(DefaultHttpTimeoutSeconds)
        };
    }

    #endregion

        #region Public Methods

        /// <summary>
        /// Checks Ollama service availability
        /// </summary>
        /// <returns>Health status for Ollama</returns>
        public async Task<HealthStatus> CheckOllamaAsync(string endpoint = "http://localhost:11434")
        {
            try
            {
                var response = await _httpClient.GetAsync($"{endpoint}/api/tags");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
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
                _logger?.LogWarning(ex, "Ollama service health check failed at {Endpoint}", endpoint);
                return new HealthStatus
                {
                    ServiceName = "Ollama",
                    IsHealthy = false,
                    Message = "Service not accessible",
                    Details = ex.Message
                };
            }
        }

        /// <summary>
        /// Checks Qdrant vector database availability
        /// </summary>
        /// <returns>Health status for Qdrant</returns>
        public async Task<HealthStatus> CheckQdrantAsync(string host = "http://localhost:6333")
        {
            try
            {
                var response = await _httpClient.GetAsync($"{host}/");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (content.Contains("qdrant"))
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
                _logger?.LogWarning(ex, "Qdrant service health check failed at {Host}", host);
                return new HealthStatus
                {
                    ServiceName = "Qdrant",
                    IsHealthy = false,
                    Message = "Vector database not accessible",
                    Details = ex.Message
                };
            }
        }

        /// <summary>
        /// Checks Redis cache availability
        /// </summary>
        /// <returns>Health status for Redis</returns>
        public async Task<HealthStatus> CheckRedisAsync(string connectionString = "localhost:6379")
        {
            try
            {
                var redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
                var db = redis.GetDatabase();
                await db.PingAsync();

                return new HealthStatus
                {
                    ServiceName = "Redis",
                    IsHealthy = true,
                    Message = "Cache service is healthy"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Redis service health check failed");
                return new HealthStatus
                {
                    ServiceName = "Redis",
                    IsHealthy = false,
                    Message = "Cache service not accessible",
                    Details = ex.Message
                };
            }
        }

        /// <summary>
        /// Checks SQL Server database availability
        /// </summary>
        /// <returns>Health status for SQL Server</returns>
        public async Task<HealthStatus> CheckSqlServerAsync(string connectionString)
        {
            return await CheckDatabaseAsync(
                "SQL Server",
                () => new SqlConnection(connectionString)
            );
        }

        /// <summary>
        /// Checks MySQL database availability
        /// </summary>
        /// <returns>Health status for MySQL</returns>
        public async Task<HealthStatus> CheckMySqlAsync(string connectionString)
        {
            return await CheckDatabaseAsync(
                "MySQL",
                () => new MySqlConnection(connectionString)
            );
        }

        /// <summary>
        /// Checks PostgreSQL database availability
        /// </summary>
        /// <returns>Health status for PostgreSQL</returns>
        public async Task<HealthStatus> CheckPostgreSqlAsync(string connectionString)
        {
            return await CheckDatabaseAsync(
                "PostgreSQL",
                () => new NpgsqlConnection(connectionString)
            );
        }

        /// <summary>
        /// Checks SQLite database availability
        /// </summary>
        /// <returns>Health status for SQLite</returns>
        public async Task<HealthStatus> CheckSqliteAsync(string connectionString)
        {
            try
            {
                var resolvedConnectionString = ResolveSqliteConnectionString(connectionString);
                return await CheckDatabaseAsync(
                    "SQLite",
                    () => new SqliteConnection(resolvedConnectionString)
                );
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "SQLite health check failed");
                return new HealthStatus
                {
                    ServiceName = "SQLite",
                    IsHealthy = false,
                    Message = "Database connection failed",
                    Details = ex.Message
                };
            }
        }
        
        /// <summary>
        /// Resolves SQLite connection string path to absolute path
        /// </summary>
        private string ResolveSqliteConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }
            
            if (!connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                return connectionString;
            }
            
            var parts = connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var resolvedParts = new List<string>();
            
            foreach (var part in parts)
            {
                if (part.TrimStart().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                {
                    var dataSource = part.Substring("Data Source=".Length).Trim();
                    
                    if (System.IO.Path.IsPathRooted(dataSource))
                    {
                        resolvedParts.Add(part);
                    }
                    else
                    {
                        var projectRoot = FindProjectRoot();
                        if (projectRoot != null)
                        {
                            var absolutePath = System.IO.Path.Combine(projectRoot, "examples", "SmartRAG.Demo", dataSource);
                            absolutePath = System.IO.Path.GetFullPath(absolutePath);
                            resolvedParts.Add($"Data Source={absolutePath}");
                        }
                        else
                        {
                            var currentDir = System.IO.Directory.GetCurrentDirectory();
                            var absolutePath = System.IO.Path.Combine(currentDir, dataSource);
                            absolutePath = System.IO.Path.GetFullPath(absolutePath);
                            resolvedParts.Add($"Data Source={absolutePath}");
                        }
                    }
                }
                else
                {
                    resolvedParts.Add(part);
                }
            }
            
            return string.Join(";", resolvedParts);
        }
        
        /// <summary>
        /// Finds the project root directory by searching upwards from the current directory
        /// </summary>
        private static string? FindProjectRoot()
        {
            var currentDir = System.IO.Directory.GetCurrentDirectory();
            var dir = new System.IO.DirectoryInfo(currentDir);
            
            while (dir != null)
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "SmartRAG.sln")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            
            return null;
        }

        /// <summary>
        /// Runs a comprehensive health check on all services
        /// </summary>
        /// <returns>List of health statuses for all services</returns>
        public async Task<List<HealthStatus>> CheckAllServicesAsync(
            string? ollamaEndpoint = null,
            string? qdrantHost = null,
            string? redisConnection = null)
        {
            var statuses = new List<HealthStatus>();

            var ollamaTask = CheckOllamaAsync(ollamaEndpoint ?? "http://localhost:11434");
            var qdrantTask = CheckQdrantAsync(qdrantHost ?? "http://localhost:6333");
            var redisTask = CheckRedisAsync(redisConnection ?? "localhost:6379");

            await Task.WhenAll(ollamaTask, qdrantTask, redisTask);

            statuses.Add(await ollamaTask);
            statuses.Add(await qdrantTask);
            statuses.Add(await redisTask);

            return statuses;
        }

        #endregion

        #region Private Methods

        private async Task<HealthStatus> CheckDatabaseAsync(string serviceName, Func<DbConnection> connectionFactory)
        {
            try
            {
                using var connection = connectionFactory();
                await connection.OpenAsync();

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
                _logger?.LogWarning(ex, "Database health check failed for {ServiceName}", serviceName);
                return new HealthStatus
                {
                    ServiceName = serviceName,
                    IsHealthy = false,
                    Message = "Database connection failed",
                    Details = ex.Message
                };
            }
        }

    #endregion
}

