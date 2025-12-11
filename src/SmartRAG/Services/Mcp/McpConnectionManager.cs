using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Interfaces.Mcp;
using SmartRAG.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services.Mcp
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
                        _logger.LogInformation("Successfully connected to MCP server");
                    }
                    else
                    {
                        _logger.LogWarning("Failed to connect to MCP server {ServerId}", server.ServerId);
                    }
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error connecting to MCP server");
                }
            }
        }
    }
}

