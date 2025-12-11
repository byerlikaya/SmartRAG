using System.Text.Json.Serialization;

namespace SmartRAG.Models
{
    /// <summary>
    /// MCP response model for JSON-RPC 2.0 protocol
    /// </summary>
    public class McpResponse
    {
        /// <summary>
        /// Response result (null if error)
        /// </summary>
        [JsonPropertyName("result")]
        public object Result { get; set; }

        /// <summary>
        /// Error information (null if success)
        /// </summary>
        [JsonPropertyName("error")]
        public McpError Error { get; set; }

        /// <summary>
        /// Whether the response indicates success
        /// </summary>
        public bool IsSuccess => Error == null;
    }

    /// <summary>
    /// MCP error information
    /// </summary>
    public class McpError
    {
        /// <summary>
        /// Error code
        /// </summary>
        [JsonPropertyName("code")]
        public int Code { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}


