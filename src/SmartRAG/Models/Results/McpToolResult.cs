namespace SmartRAG.Models;


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


