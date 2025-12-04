using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartRAG.Models
{
    /// <summary>
    /// MCP request model for JSON-RPC 2.0 protocol
    /// </summary>
    public class McpRequest
    {
        /// <summary>
        /// JSON-RPC version (always "2.0")
        /// </summary>
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        /// <summary>
        /// Request method name
        /// </summary>
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Request parameters
        /// </summary>
        [JsonPropertyName("params")]
        public Dictionary<string, object> Params { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Request ID for correlation
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }
}


