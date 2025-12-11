using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Helpers;
using SmartRAG.Interfaces.Mcp;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartRAG.Services.Mcp
{
    /// <summary>
    /// MCP Client implementation for connecting to external MCP servers
    /// </summary>
    public class McpClient : IMcpClient
    {
        private const int DefaultTimeoutSeconds = 30;
        private readonly ILogger<McpClient> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Dictionary<string, HttpClient> _connections = new Dictionary<string, HttpClient>();
        private readonly Dictionary<string, McpServerConfig> _serverConfigs = new Dictionary<string, McpServerConfig>();
        private bool _disposed = false;

        public McpClient(
            ILogger<McpClient> logger,
            IHttpClientFactory httpClientFactory,
            IOptions<SmartRagOptions> options)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Connects to an MCP server
        /// </summary>
        public Task<bool> ConnectAsync(McpServerConfig config)
        {
            McpRequestValidator.ValidateConfig(config);

            _logger.LogInformation("Connecting to MCP server at {Endpoint}", config.Endpoint);

            try
            {
                if (_connections.ContainsKey(config.ServerId))
                {
                    _logger.LogWarning("Already connected to server {ServerId}", config.ServerId);
                    return Task.FromResult(true);
                }

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds > 0 ? config.TimeoutSeconds : DefaultTimeoutSeconds);

                if (config.Headers != null)
                {
                    foreach (var header in config.Headers)
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                _connections[config.ServerId] = client;
                _serverConfigs[config.ServerId] = config;

                _logger.LogInformation("Successfully connected to MCP server");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to MCP server");
                return Task.FromException<bool>(ex);
            }
        }

        /// <summary>
        /// Discovers available tools on an MCP server
        /// </summary>
        public async Task<List<McpTool>> DiscoverToolsAsync(string serverId)
        {
            if (string.IsNullOrWhiteSpace(serverId))
                throw new ArgumentException("ServerId cannot be null or empty", nameof(serverId));

            if (!_connections.ContainsKey(serverId))
                throw new InvalidOperationException($"Not connected to server {serverId}");

            try
            {
                var request = new McpRequest
                {
                    JsonRpc = "2.0",
                    Method = "tools/list",
                    Id = Guid.NewGuid().ToString()
                };

                var response = await SendRequestAsync(serverId, request);

                if (!response.IsSuccess)
                {
                    _logger.LogError("Failed to discover tools: {Error}", response.Error?.Message);
                    return new List<McpTool>();
                }

                var tools = new List<McpTool>();
                if (response.Result is JsonElement resultElement && resultElement.TryGetProperty("tools", out var toolsElement))
                {
                    if (toolsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var toolElement in toolsElement.EnumerateArray())
                        {
                            var tool = new McpTool
                            {
                                Name = toolElement.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty,
                                Description = toolElement.TryGetProperty("description", out var descElement) ? descElement.GetString() ?? string.Empty : string.Empty,
                                ServerId = serverId
                            };

                            if (toolElement.TryGetProperty("inputSchema", out var schemaElement))
                            {
                                tool.InputSchema = JsonSerializer.Deserialize<Dictionary<string, object>>(schemaElement.GetRawText()) ?? new Dictionary<string, object>();
                            }

                            tools.Add(tool);
                        }
                    }
                }

                _logger.LogInformation("Discovered {Count} tools", tools.Count);
                return tools;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering tools");
                return new List<McpTool>();
            }
        }

        /// <summary>
        /// Calls a tool on an MCP server
        /// </summary>
        public async Task<McpResponse> CallToolAsync(string serverId, string toolName, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrWhiteSpace(serverId))
                throw new ArgumentException("ServerId cannot be null or empty", nameof(serverId));

            if (string.IsNullOrWhiteSpace(toolName))
                throw new ArgumentException("ToolName cannot be null or empty", nameof(toolName));

            if (!_connections.ContainsKey(serverId))
                throw new InvalidOperationException($"Not connected to server {serverId}");

            try
            {
                var request = new McpRequest
                {
                    JsonRpc = "2.0",
                    Method = "tools/call",
                    Id = Guid.NewGuid().ToString(),
                    Params = new Dictionary<string, object>
                    {
                        { "name", toolName },
                        { "arguments", parameters ?? new Dictionary<string, object>() }
                    }
                };

                return await SendRequestAsync(serverId, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling MCP tool");
                return new McpResponse
                {
                    Error = new McpError
                    {
                        Code = -32000,
                        Message = ex.Message
                    }
                };
            }
        }

        /// <summary>
        /// Gets list of connected server IDs
        /// </summary>
        public List<string> GetConnectedServers()
        {
            return new List<string>(_connections.Keys);
        }

        private async Task<McpResponse> SendRequestAsync(string serverId, McpRequest request)
        {
            var client = _connections[serverId];
            var config = _serverConfigs[serverId];

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, config.Endpoint)
            {
                Content = content
            };
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            var response = await client.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new McpResponse
                {
                    Error = new McpError
                    {
                        Code = (int)response.StatusCode,
                        Message = $"HTTP {response.StatusCode}: {responseContent}"
                    }
                };
            }

            try
            {
                var mcpResponse = JsonSerializer.Deserialize<McpResponse>(responseContent);
                return mcpResponse ?? new McpResponse { Error = new McpError { Code = -32700, Message = "Parse error" } };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing MCP response");
                return new McpResponse
                {
                    Error = new McpError
                    {
                        Code = -32700,
                        Message = $"Parse error: {ex.Message}"
                    }
                };
            }
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                foreach (var client in _connections.Values)
                {
                    client?.Dispose();
                }
                _connections.Clear();
                _serverConfigs.Clear();
                _disposed = true;
            }
        }
    }
}

