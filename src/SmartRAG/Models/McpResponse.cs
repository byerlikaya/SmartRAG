using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartRAG.Models
{
    /// <summary>
    /// MCP response model for JSON-RPC 2.0 protocol
    /// </summary>
    public class McpResponse
    {
        /// <summary>
        /// JSON-RPC version (always "2.0")
        /// </summary>
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

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
        /// Request ID for correlation
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

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

        /// <summary>
        /// Additional error data
        /// </summary>
        [JsonPropertyName("data")]
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }
}


