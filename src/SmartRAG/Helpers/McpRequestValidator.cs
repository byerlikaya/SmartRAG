using System;
using SmartRAG.Models;

namespace SmartRAG.Helpers
{
    /// <summary>
    /// Utility class for validating MCP requests
    /// </summary>
    public static class McpRequestValidator
    {
        /// <summary>
        /// Validates MCP server configuration
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        /// <exception cref="ArgumentException">Thrown when endpoint is invalid</exception>
        public static void ValidateConfig(McpServerConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrWhiteSpace(config.Endpoint))
                throw new ArgumentException("Endpoint cannot be null or empty", nameof(config));

            if (!Uri.TryCreate(config.Endpoint, UriKind.Absolute, out _))
                throw new ArgumentException("Invalid endpoint URL", nameof(config));

            if (string.IsNullOrWhiteSpace(config.ServerId))
                throw new ArgumentException("ServerId cannot be null or empty", nameof(config));
        }
    }
}


