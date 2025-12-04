using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Mcp.Client;
using SmartRAG.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Mcp.Client.Services
{
    /// <summary>
    /// Manages MCP server connections
    /// </summary>
    public class McpConnectionManager : IMcpConnectionManager
    {
        private readonly ILogger<McpConnectionManager> _logger;
        private readonly IMcpClient _mcpClient;
        private readonly SmartRagOptions _options;

        public McpConnectionManager(
            ILogger<McpConnectionManager> logger,
            IMcpClient mcpClient,
            IOptions<SmartRagOptions> options)
        {
            _logger = logger;
            _mcpClient = mcpClient;
            _options = options.Value;
        }

        /// <summary>
        /// Connects to all configured MCP servers with AutoConnect enabled
        /// </summary>
        public async Task ConnectAllAsync()
        {
            if (_options.McpServers == null || _options.McpServers.Count == 0)
            {
                _logger.LogInformation("No MCP servers configured");
                return;
            }

            var autoConnectServers = _options.McpServers.Where(s => s.AutoConnect).ToList();

            if (autoConnectServers.Count == 0)
            {
                _logger.LogInformation("No MCP servers with AutoConnect enabled");
                return;
            }

            _logger.LogInformation("Connecting to {Count} MCP servers", autoConnectServers.Count);

            foreach (var server in autoConnectServers)
            {
                try
                {
                    var connected = await _mcpClient.ConnectAsync(server);
                    if (connected)
                    {
                        _logger.LogInformation("Successfully connected to MCP server {ServerId}", server.ServerId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to connect to MCP server {ServerId}", server.ServerId);
                    }
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error connecting to MCP server {ServerId}", server.ServerId);
                }
            }
        }

        /// <summary>
        /// Connects to a specific MCP server
        /// </summary>
        public async Task<bool> ConnectAsync(McpServerConfig config)
        {
            return await _mcpClient.ConnectAsync(config);
        }

        /// <summary>
        /// Disconnects from all servers
        /// </summary>
        public async Task DisconnectAllAsync()
        {
            var connectedServers = _mcpClient.GetConnectedServers();
            foreach (var serverId in connectedServers)
            {
                try
                {
                    await _mcpClient.DisconnectAsync(serverId);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error disconnecting from MCP server {ServerId}", serverId);
                }
            }
        }

        /// <summary>
        /// Gets list of connected server IDs
        /// </summary>
        public List<string> GetConnectedServers()
        {
            return _mcpClient.GetConnectedServers();
        }
    }
}

