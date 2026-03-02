using SmartRAG.Services.Shared;

namespace SmartRAG.Services.Mcp;


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
        if (_options.McpServers.Count == 0)
        {
            ServiceLogMessages.LogMcpConnectionNoServersConfigured(_logger, null);
            return;
        }

        var autoConnectServers = _options.McpServers.Where(s => s.AutoConnect).ToList();

        if (autoConnectServers.Count == 0)
        {
            ServiceLogMessages.LogMcpConnectionNoAutoConnectServers(_logger, null);
            return;
        }

        ServiceLogMessages.LogMcpConnectionConnectingToServers(_logger, autoConnectServers.Count, null);

        foreach (var server in autoConnectServers)
        {
            try
            {
                var connected = await _mcpClient.ConnectAsync(server);
                if (connected)
                {
                    ServiceLogMessages.LogMcpConnectionSuccess(_logger, null);
                }
                else
                {
                    ServiceLogMessages.LogMcpConnectionFailed(_logger, null);
                }
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogMcpConnectionError(_logger, ex);
            }
        }
    }
}


