using System.Collections.Generic;
using System.Threading.Tasks;
using SmartRAG.Models;

namespace SmartRAG.Mcp.Client.Services
{
    /// <summary>
    /// Interface for managing MCP server connections
    /// </summary>
    public interface IMcpConnectionManager
    {
        /// <summary>
        /// Connects to all configured MCP servers with AutoConnect enabled
        /// </summary>
        /// <returns>Task representing the connection operation</returns>
        Task ConnectAllAsync();

        /// <summary>
        /// Connects to a specific MCP server
        /// </summary>
        /// <param name="config">Server configuration</param>
        /// <returns>True if connection successful</returns>
        Task<bool> ConnectAsync(McpServerConfig config);

        /// <summary>
        /// Disconnects from all servers
        /// </summary>
        /// <returns>Task representing the disconnect operation</returns>
        Task DisconnectAllAsync();

        /// <summary>
        /// Gets list of connected server IDs
        /// </summary>
        /// <returns>List of connected server identifiers</returns>
        List<string> GetConnectedServers();
    }
}


