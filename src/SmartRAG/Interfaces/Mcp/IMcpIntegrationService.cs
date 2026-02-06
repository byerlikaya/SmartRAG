using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartRAG.Models;

namespace SmartRAG.Interfaces.Mcp;


/// <summary>
/// Interface for integrating MCP server results with SmartRAG's query intelligence
/// </summary>
public interface IMcpIntegrationService
{
    /// <summary>
    /// Queries connected MCP servers and merges results with RAG response
    /// </summary>
    /// <param name="query">User query</param>
    /// <param name="maxResults">Maximum number of results</param>
    /// <param name="conversationHistory">Optional conversation history for context</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>List of MCP tool results</returns>
    Task<List<McpToolResult>> QueryWithMcpAsync(string query, int maxResults = 5, string? conversationHistory = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available tools from all connected MCP servers
    /// </summary>
    /// <returns>List of available tools</returns>
    Task<List<McpTool>> GetAvailableToolsAsync();

    /// <summary>
    /// Calls a specific MCP tool
    /// </summary>
    /// <param name="serverId">Server identifier</param>
    /// <param name="toolName">Tool name</param>
    /// <param name="parameters">Tool parameters</param>
    /// <returns>Tool result</returns>
    Task<McpToolResult> CallToolAsync(string serverId, string toolName, Dictionary<string, object> parameters);
}


