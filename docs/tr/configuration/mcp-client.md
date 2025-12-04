---
layout: default
title: MCP Client Yapılandırması
description: Harici MCP (Model Context Protocol) sunucularına bağlanmak için MCP client yapılandırması
lang: tr
redirect_from: /tr/configuration/mcp-client.html
---

# MCP Client Yapılandırması

SmartRAG, harici MCP (Model Context Protocol) sunucularına bağlanarak sorgu yeteneklerini harici araçlar ve veri kaynaklarıyla genişletmeyi destekler.

## Genel Bakış

MCP Client özelliği SmartRAG'ın şunları yapmasına olanak tanır:
- Harici MCP sunucularına bağlanma
- MCP sunucuları tarafından sağlanan araçları keşfetme ve kullanma
- MCP araç sonuçlarını RAG yanıtlarıyla entegre etme
- Birden fazla MCP sunucusunu aynı anda sorgulama

## Yapılandırma

### MCP Client'ı Etkinleştirme

`appsettings.json` dosyanıza aşağıdakini ekleyin:

```json
{
  "SmartRAG": {
    "EnableMcpClient": true,
    "McpServers": [
      {
        "ServerId": "ornek-sunucu",
        "Endpoint": "https://mcp.ornek.com/api",
        "TransportType": "Http",
        "AutoConnect": true,
        "TimeoutSeconds": 30,
        "Headers": {
          "Authorization": "Bearer token-buraya"
        }
      }
    ]
  }
}
```

### Yapılandırma Özellikleri

#### EnableMcpClient

- **Tip**: `bool`
- **Varsayılan**: `true`
- **Açıklama**: MCP Client işlevselliğini etkinleştirir veya devre dışı bırakır

#### McpServers

- **Tip**: `List<McpServerConfig>`
- **Varsayılan**: Boş liste
- **Açıklama**: MCP sunucu yapılandırmaları listesi

#### McpServerConfig Özellikleri

| Özellik | Tip | Gerekli | Açıklama |
|---------|-----|---------|----------|
| `ServerId` | `string` | Evet | Sunucu için benzersiz tanımlayıcı |
| `Endpoint` | `string` | Evet | Sunucu endpoint URL'i (HTTP veya WebSocket) |
| `TransportType` | `McpTransportType` | Hayır | Taşıma tipi: `Http`, `WebSocket` veya `Stdio` (varsayılan: `Http`) |
| `AutoConnect` | `bool` | Hayır | Başlangıçta otomatik bağlanıp bağlanmayacağı (varsayılan: `true`) |
| `TimeoutSeconds` | `int` | Hayır | Bağlantı zaman aşımı saniye cinsinden (varsayılan: `30`) |
| `Headers` | `Dictionary<string, string>` | Hayır | Kimlik doğrulama veya özel yapılandırma için isteğe bağlı HTTP başlıkları |

## Taşıma Tipleri

### Http

REST tabanlı MCP sunucuları için standart HTTP/HTTPS taşıması.

```json
{
  "ServerId": "http-sunucu",
  "Endpoint": "https://api.ornek.com/mcp",
  "TransportType": "Http"
}
```

### WebSocket

Gerçek zamanlı MCP sunucu bağlantıları için WebSocket taşıması.

```json
{
  "ServerId": "ws-sunucu",
  "Endpoint": "wss://ws.ornek.com/mcp",
  "TransportType": "WebSocket"
}
```

### Stdio

Yerel MCP sunucu işlemleri için standart girdi/çıktı taşıması.

```json
{
  "ServerId": "stdio-sunucu",
  "Endpoint": "stdio:///sunucu/yolu",
  "TransportType": "Stdio"
}
```

## Programatik Yapılandırma

MCP sunucularını programatik olarak da yapılandırabilirsiniz:

```csharp
services.AddSmartRag(configuration, options =>
{
    options.EnableMcpClient = true;
    options.McpServers.Add(new McpServerConfig
    {
        ServerId = "ornek-sunucu",
        Endpoint = "https://mcp.ornek.com/api",
        TransportType = McpTransportType.Http,
        AutoConnect = true,
        TimeoutSeconds = 30,
        Headers = new Dictionary<string, string>
        {
            { "Authorization", "Bearer token" }
        }
    });
});
```

## Başlatma

Servis sağlayıcıyı oluşturduktan sonra, MCP bağlantılarını başlatın:

```csharp
var serviceProvider = services.BuildServiceProvider();
await serviceProvider.InitializeSmartRagAsync();
```

Bu, `AutoConnect: true` olan tüm sunuculara otomatik olarak bağlanacaktır.

## Kullanım

Yapılandırıldıktan sonra, MCP araçları otomatik olarak sorgu yanıtlarına entegre edilir. SmartRAG'ı sorguladığınızda:

1. Bağlı MCP sunucularını ilgili araçlar için sorgular
2. Sorguyla eşleşen araçları çalıştırır
3. MCP sonuçlarını doküman ve veritabanı sonuçlarıyla birleştirir
4. MCP kaynaklarını yanıta dahil eder

## Örnek Yanıt

```json
{
  "Query": "Bugün hava nasıl?",
  "Answer": "Bugün hava güneşli, sıcaklık 22°C...",
  "Sources": [
    {
      "SourceType": "MCP",
      "FileName": "hava-sunucu:get_weather",
      "RelevantContent": "Sıcaklık: 22°C, Durum: Güneşli",
      "RelevanceScore": 1.0
    }
  ]
}
```

## Sorun Giderme

### Bağlantı Hataları

MCP sunucuları bağlanamazsa:
- Endpoint URL'inin doğru olduğunu kontrol edin
- Ağ bağlantısını doğrulayın
- Kimlik doğrulama başlıklarının geçerli olduğundan emin olun
- Hatalar için sunucu loglarını kontrol edin

### Araç Keşif Sorunları

Araçlar keşfedilmezse:
- Sunucunun MCP protokolünü doğru şekilde uyguladığını doğrulayın
- `tools/list` metodunun mevcut olduğunu kontrol edin
- Araç adlandırma kuralları için sunucu dokümantasyonunu inceleyin

## İlgili Dokümantasyon

- [File Watcher Yapılandırması]({{ site.baseurl }}/tr/configuration/file-watcher/)
- [Başlangıç]({{ site.baseurl }}/tr/getting-started/)
- [API Referansı]({{ site.baseurl }}/tr/api-reference/)


