---
layout: default
title: MCP Entegrasyon Örnekleri
description: SmartRAG ile MCP (Model Context Protocol) sunucularını entegre etme örnekleri
lang: tr
redirect_from: /tr/examples/mcp-integration.html
---

# MCP Entegrasyon Örnekleri

Bu kılavuz, SmartRAG ile MCP (Model Context Protocol) sunucularını entegre ederek sorgu yeteneklerini harici araçlar ve veri kaynaklarıyla genişletmeyi gösterir.

## Temel MCP Sunucu Yapılandırması

### Yapılandırma Dosyası

`appsettings.json` dosyasına MCP sunucu yapılandırması ekleyin:

```json
{
  "SmartRAG": {
    "EnableMcpClient": true,
    "McpServers": [
      {
        "ServerId": "hava-sunucu",
        "Endpoint": "https://api.hava.ornek.com/mcp",
        "AutoConnect": true,
        "TimeoutSeconds": 30,
        "Headers": {
          "Authorization": "Bearer api-token-buraya"
        }
      }
    ]
  }
}
```

### Programatik Yapılandırma

MCP sunucularını programatik olarak yapılandırın:

```csharp
using SmartRAG.Models;

var services = new ServiceCollection();
services.AddSmartRag(configuration, options =>
{
    options.EnableMcpClient = true;
    
    options.McpServers.Add(new McpServerConfig
    {
        ServerId = "hava-sunucu",
        Endpoint = "https://api.hava.ornek.com/mcp",
        AutoConnect = true,
        TimeoutSeconds = 30,
        Headers = new Dictionary<string, string>
        {
            { "Authorization", "Bearer api-token" }
        }
    });
});
```

## MCP Bağlantılarını Başlatma

Servis sağlayıcıyı oluşturduktan sonra, MCP bağlantılarını başlatın:

```csharp
var serviceProvider = services.BuildServiceProvider();
await serviceProvider.InitializeSmartRagAsync();
```

Bu, `AutoConnect: true` olan tüm sunuculara otomatik olarak bağlanacaktır.

## MCP Entegrasyonu ile Sorgulama

Yapılandırıldıktan sonra, MCP araçları otomatik olarak sorgu yanıtlarına entegre edilir:

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

Yanıt şunları içerecektir:
- Yerel dokümanlar
- Veritabanı sorguları
- MCP sunucu araçları (ilgiliyse)

## Manuel MCP Aracı Çalıştırma

MCP araçlarını manuel olarak da çağırabilirsiniz:

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

## Birden Fazla MCP Sunucusu

Birden fazla MCP sunucusu yapılandırın:

```json
{
  "SmartRAG": {
    "EnableMcpClient": true,
    "McpServers": [
      {
        "ServerId": "hava-sunucu",
        "Endpoint": "https://api.hava.ornek.com/mcp",
        "AutoConnect": true
      },
      {
        "ServerId": "hisse-sunucu",
        "Endpoint": "https://api.hisseler.ornek.com/mcp",
        "AutoConnect": true,
        "Headers": {
          "X-API-Key": "api-anahtari"
        }
      }
    ]
  }
}
```

## Hata Yönetimi

MCP entegrasyonu zarif hata yönetimi içerir:

```csharp
var response = await _searchService.QueryIntelligenceAsync(query);

if (response.Sources.Any(s => s.SourceType == "MCP"))
{
    var mcpSources = response.Sources
        .Where(s => s.SourceType == "MCP")
        .ToList();
    
    foreach (var source in mcpSources)
    {
        Console.WriteLine($"MCP Kaynağı: {source.FileName}");
        Console.WriteLine($"İçerik: {source.RelevantContent}");
    }
}
```

MCP sunucuları başarısız olursa, sorgu yalnızca doküman ve veritabanı sonuçlarıyla devam eder.

## Gerçek Dünya Örneği: Hava Durumu Sorgusu

```csharp
public class WeatherQueryExample
{
    private readonly IDocumentSearchService _searchService;
    
    public async Task<string> GetWeatherInfo(string location)
    {
        var query = $"{location} için hava durumu nedir?";
        var response = await _searchService.QueryIntelligenceAsync(query);
        
        return response.Answer;
    }
}
```

Sorgu şunları yapacaktır:
1. Yerel dokümanlarda hava durumu bilgisi kontrol eder
2. Bağlı MCP hava durumu sunucularını sorgular
3. Sonuçları kapsamlı bir yanıta birleştirir

## İlgili Dokümantasyon

- [MCP Client Yapılandırması]({{ site.baseurl }}/tr/configuration/mcp-client/)
- [File Watcher Yapılandırması]({{ site.baseurl }}/tr/configuration/file-watcher/)
- [Başlangıç]({{ site.baseurl }}/tr/getting-started/)


