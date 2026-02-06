using System.Collections.Generic;

namespace SmartRAG.Models;


/// <summary>
/// Represents an MCP tool available on a server
/// </summary>
public class McpTool
{
    /// <summary>
    /// Tool name identifier
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tool description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Tool input parameters schema
    /// </summary>
    public Dictionary<string, object> InputSchema { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Server ID that provides this tool
    /// </summary>
    public string ServerId { get; set; } = string.Empty;
}



