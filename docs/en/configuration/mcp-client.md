---
layout: default
title: MCP Client Configuration
description: Configure MCP (Model Context Protocol) client connections to external MCP servers
lang: en
redirect_from: /en/configuration/mcp-client.html
---

# MCP Client Configuration

SmartRAG supports connecting to external MCP (Model Context Protocol) servers to extend query capabilities with external tools and data sources.

## Overview

The MCP Client feature allows SmartRAG to:
- Connect to external MCP servers
- Discover and use tools provided by MCP servers
- Integrate MCP tool results with RAG responses
- Query multiple MCP servers simultaneously

## Configuration

### Enable MCP Client

Add the following to your `appsettings.json`:

```json
{
  "SmartRAG": {
    "EnableMcpClient": true,
    "McpServers": [
      {
        "ServerId": "example-server",
        "Endpoint": "https://mcp.example.com/api",
        "TransportType": "Http",
        "AutoConnect": true,
        "TimeoutSeconds": 30,
        "Headers": {
          "Authorization": "Bearer your-token-here"
        }
      }
    ]
  }
}
```

### Configuration Properties

#### EnableMcpClient

- **Type**: `bool`
- **Default**: `true`
- **Description**: Enables or disables MCP Client functionality

#### McpServers

- **Type**: `List<McpServerConfig>`
- **Default**: Empty list
- **Description**: List of MCP server configurations

#### McpServerConfig Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `ServerId` | `string` | Yes | Unique identifier for the server |
| `Endpoint` | `string` | Yes | Server endpoint URL (HTTP or WebSocket) |
| `TransportType` | `McpTransportType` | No | Transport type: `Http`, `WebSocket`, or `Stdio` (default: `Http`) |
| `AutoConnect` | `bool` | No | Whether to automatically connect on startup (default: `true`) |
| `TimeoutSeconds` | `int` | No | Connection timeout in seconds (default: `30`) |
| `Headers` | `Dictionary<string, string>` | No | Optional HTTP headers for authentication or custom configuration |

## Transport Types

### Http

Standard HTTP/HTTPS transport for REST-based MCP servers.

```json
{
  "ServerId": "http-server",
  "Endpoint": "https://api.example.com/mcp",
  "TransportType": "Http"
}
```

### WebSocket

WebSocket transport for real-time MCP server connections.

```json
{
  "ServerId": "ws-server",
  "Endpoint": "wss://ws.example.com/mcp",
  "TransportType": "WebSocket"
}
```

### Stdio

Standard input/output transport for local MCP server processes.

```json
{
  "ServerId": "stdio-server",
  "Endpoint": "stdio:///path/to/server",
  "TransportType": "Stdio"
}
```

## Programmatic Configuration

You can also configure MCP servers programmatically:

```csharp
services.AddSmartRag(configuration, options =>
{
    options.EnableMcpClient = true;
    options.McpServers.Add(new McpServerConfig
    {
        ServerId = "example-server",
        Endpoint = "https://mcp.example.com/api",
        TransportType = McpTransportType.Http,
        AutoConnect = true,
        TimeoutSeconds = 30,
        Headers = new Dictionary<string, string>
        {
            { "Authorization", "Bearer your-token" }
        }
    });
});
```

## Initialization

After building the service provider, initialize MCP connections:

```csharp
var serviceProvider = services.BuildServiceProvider();
await serviceProvider.InitializeSmartRagAsync();
```

This will automatically connect to all servers with `AutoConnect: true`.

## Usage

Once configured, MCP tools are automatically integrated into query responses. When you query SmartRAG, it will:

1. Query connected MCP servers for relevant tools
2. Execute tools that match the query
3. Merge MCP results with document and database results
4. Include MCP sources in the response

## Example Response

```json
{
  "Query": "What is the weather today?",
  "Answer": "The weather today is sunny with a temperature of 22°C...",
  "Sources": [
    {
      "SourceType": "MCP",
      "FileName": "weather-server:get_weather",
      "RelevantContent": "Temperature: 22°C, Condition: Sunny",
      "RelevanceScore": 1.0
    }
  ]
}
```

## Troubleshooting

### Connection Failures

If MCP servers fail to connect:
- Check the endpoint URL is correct
- Verify network connectivity
- Ensure authentication headers are valid
- Check server logs for errors

### Tool Discovery Issues

If tools are not discovered:
- Verify the server implements the MCP protocol correctly
- Check that the `tools/list` method is available
- Review server documentation for tool naming conventions

## Related Documentation

- [File Watcher Configuration]({{ site.baseurl }}/en/configuration/file-watcher/)
- [Getting Started]({{ site.baseurl }}/en/getting-started/)
- [API Reference]({{ site.baseurl }}/en/api-reference/)


