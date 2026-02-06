namespace SmartRAG.Interfaces.Mcp;


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
}


