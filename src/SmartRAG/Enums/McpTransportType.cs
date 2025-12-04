namespace SmartRAG.Enums
{
    /// <summary>
    /// Transport types for MCP server connections
    /// </summary>
    public enum McpTransportType
    {
        /// <summary>
        /// HTTP/HTTPS transport
        /// </summary>
        Http,

        /// <summary>
        /// WebSocket transport
        /// </summary>
        WebSocket,

        /// <summary>
        /// Standard input/output transport
        /// </summary>
        Stdio
    }
}

