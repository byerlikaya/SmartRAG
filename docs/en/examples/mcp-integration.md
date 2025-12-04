---
layout: default
title: MCP Integration Examples
description: Examples of integrating MCP (Model Context Protocol) servers with SmartRAG
lang: en
redirect_from: /en/examples/mcp-integration.html
---

# MCP Integration Examples

This guide demonstrates how to integrate MCP (Model Context Protocol) servers with SmartRAG to extend query capabilities with external tools and data sources.

## Basic MCP Server Configuration

### Configuration File

Add MCP server configuration to `appsettings.json`:

```json
{
  "SmartRAG": {
    "EnableMcpClient": true,
    "McpServers": [
      {
        "ServerId": "weather-server",
        "Endpoint": "https://api.weather.example.com/mcp",
        "TransportType": "Http",
        "AutoConnect": true,
        "TimeoutSeconds": 30,
        "Headers": {
          "Authorization": "Bearer your-api-token"
        }
      }
    ]
  }
}
```

### Programmatic Configuration

Configure MCP servers programmatically:

```csharp
using SmartRAG.Enums;
using SmartRAG.Models;

var services = new ServiceCollection();
services.AddSmartRag(configuration, options =>
{
    options.EnableMcpClient = true;
    
    options.McpServers.Add(new McpServerConfig
    {
        ServerId = "weather-server",
        Endpoint = "https://api.weather.example.com/mcp",
        TransportType = McpTransportType.Http,
        AutoConnect = true,
        TimeoutSeconds = 30,
        Headers = new Dictionary<string, string>
        {
            { "Authorization", "Bearer your-api-token" }
        }
    });
});
```

## Initializing MCP Connections

After building the service provider, initialize MCP connections:

```csharp
var serviceProvider = services.BuildServiceProvider();
await serviceProvider.InitializeSmartRagAsync();
```

This automatically connects to all servers with `AutoConnect: true`.

## Querying with MCP Integration

Once configured, MCP tools are automatically integrated into query responses:

```csharp
public class QueryController : ControllerBase
{
    private readonly IDocumentSearchService _searchService;
    
    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] QueryRequest request)
    {
        var response = await _searchService.QueryIntelligenceAsync(
            request.Query,
            maxResults: 5
        );
        
        return Ok(response);
    }
}
```

The response will include results from:
- Local documents
- Database queries
- MCP server tools (if relevant)

## Manual MCP Tool Execution

You can also manually call MCP tools:

```csharp
using SmartRAG.Mcp.Integration;

public class McpController : ControllerBase
{
    private readonly IMcpIntegrationService _mcpIntegration;
    
    [HttpGet("tools")]
    public async Task<IActionResult> GetAvailableTools()
    {
        var tools = await _mcpIntegration.GetAvailableToolsAsync();
        return Ok(tools);
    }
    
    [HttpPost("tools/{serverId}/{toolName}")]
    public async Task<IActionResult> CallTool(
        string serverId, 
        string toolName, 
        [FromBody] Dictionary<string, object> parameters)
    {
        var result = await _mcpIntegration.CallToolAsync(
            serverId, 
            toolName, 
            parameters
        );
        
        return Ok(result);
    }
}
```

## Multiple MCP Servers

Configure multiple MCP servers:

```json
{
  "SmartRAG": {
    "EnableMcpClient": true,
    "McpServers": [
      {
        "ServerId": "weather-server",
        "Endpoint": "https://api.weather.example.com/mcp",
        "TransportType": "Http",
        "AutoConnect": true
      },
      {
        "ServerId": "stock-server",
        "Endpoint": "https://api.stocks.example.com/mcp",
        "TransportType": "Http",
        "AutoConnect": true,
        "Headers": {
          "X-API-Key": "your-api-key"
        }
      }
    ]
  }
}
```

## Error Handling

MCP integration includes graceful error handling:

```csharp
var response = await _searchService.QueryIntelligenceAsync(query);

if (response.Sources.Any(s => s.SourceType == "MCP"))
{
    var mcpSources = response.Sources
        .Where(s => s.SourceType == "MCP")
        .ToList();
    
    foreach (var source in mcpSources)
    {
        Console.WriteLine($"MCP Source: {source.FileName}");
        Console.WriteLine($"Content: {source.RelevantContent}");
    }
}
```

If MCP servers fail, the query continues with document and database results only.

## Real-World Example: Weather Query

```csharp
public class WeatherQueryExample
{
    private readonly IDocumentSearchService _searchService;
    
    public async Task<string> GetWeatherInfo(string location)
    {
        var query = $"What is the weather in {location}?";
        var response = await _searchService.QueryIntelligenceAsync(query);
        
        return response.Answer;
    }
}
```

The query will:
1. Check local documents for weather information
2. Query connected MCP weather servers
3. Merge results into a comprehensive answer

## Related Documentation

- [MCP Client Configuration]({{ site.baseurl }}/en/configuration/mcp-client/)
- [File Watcher Configuration]({{ site.baseurl }}/en/configuration/file-watcher/)
- [Getting Started]({{ site.baseurl }}/en/getting-started/)


