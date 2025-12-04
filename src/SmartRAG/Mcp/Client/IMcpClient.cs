using System.Collections.Generic;
using System.Threading.Tasks;
using SmartRAG.Models;

namespace SmartRAG.Mcp.Client
{
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
        /// Disconnects from an MCP server
        /// </summary>
        /// <param name="serverId">Server identifier</param>
        /// <returns>Task representing the disconnect operation</returns>
        Task DisconnectAsync(string serverId);

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
        /// Checks if connected to an MCP server
        /// </summary>
        /// <param name="serverId">Server identifier</param>
        /// <returns>True if connected</returns>
        Task<bool> IsConnectedAsync(string serverId);

        /// <summary>
        /// Gets list of connected server IDs
        /// </summary>
        /// <returns>List of connected server identifiers</returns>
        List<string> GetConnectedServers();
    }
}


