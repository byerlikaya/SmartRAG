
namespace SmartRAG.Interfaces.Mcp;


/// <summary>
/// Interface for MCP (Model Context Protocol) client operations
/// </summary>
public interface IMcpClient : System.IDisposable
{
    /// <summary>
    /// Connects to an MCP server
    /// </summary>
    /// <param name="config">MCP server configuration</param>
    /// <returns>True if connection successful</returns>
    Task<bool> ConnectAsync(McpServerConfig config);

    /// <summary>
    /// Discovers available tools on an MCP server
    /// </summary>
    /// <param name="serverId">Server identifier</param>
    /// <returns>List of available tools</returns>
    Task<List<McpTool>> DiscoverToolsAsync(string serverId);

    /// <summary>
    /// Calls a tool on an MCP server
    /// </summary>
    /// <param name="serverId">Server identifier</param>
    /// <param name="toolName">Name of the tool to call</param>
    /// <param name="parameters">Tool parameters</param>
    /// <returns>MCP response with tool result</returns>
    Task<McpResponse> CallToolAsync(string serverId, string toolName, Dictionary<string, object> parameters);

    /// <summary>
    /// Gets list of connected server IDs
    /// </summary>
    /// <returns>List of connected server identifiers</returns>
    List<string> GetConnectedServers();
}


