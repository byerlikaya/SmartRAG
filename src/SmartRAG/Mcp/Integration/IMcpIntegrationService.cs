#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using SmartRAG.Models;

namespace SmartRAG.Mcp.Integration
{
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
        /// <returns>List of MCP tool results</returns>
        Task<List<McpToolResult>> QueryWithMcpAsync(string query, int maxResults = 5, string? conversationHistory = null);

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

    /// <summary>
    /// Result from an MCP tool call
    /// </summary>
    public class McpToolResult
    {
        /// <summary>
        /// Server ID that provided this result
        /// </summary>
        public string ServerId { get; set; } = string.Empty;

        /// <summary>
        /// Tool name that was called
        /// </summary>
        public string ToolName { get; set; } = string.Empty;

        /// <summary>
        /// Tool result content
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Whether the tool call was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Error message if call failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}


