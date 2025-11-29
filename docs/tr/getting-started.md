---
layout: default
title: Başlangıç
description: SmartRAG'i .NET uygulamanıza dakikalar içinde kurun ve yapılandırın
lang: tr
---


## Kurulum

SmartRAG bir NuGet paketi olarak mevcuttur ve **.NET Standard 2.1** destekler, bu da şunlarla uyumlu olduğu anlamına gelir:
- ✅ .NET Core 3.0+
- ✅ .NET 5, 6, 7, 8, 9+

### Kurulum Yöntemleri

<div class="code-tabs">
    <button class="code-tab active" data-tab="cli">.NET CLI</button>
    <button class="code-tab" data-tab="pm">Package Manager</button>
    <button class="code-tab" data-tab="xml">Package Reference</button>
</div>

<div class="code-panel active" data-tab="cli">
<pre><code class="language-bash">dotnet add package SmartRAG</code></pre>
</div>

<div class="code-panel" data-tab="pm">
<pre><code class="language-bash">Install-Package SmartRAG</code></pre>
</div>

<div class="code-panel" data-tab="xml">
<pre><code class="language-xml">&lt;PackageReference Include="SmartRAG" Version="3.3.0" /&gt;</code></pre>
</div>

## Temel Yapılandırma

SmartRAG'i `Program.cs` veya `Startup.cs` dosyanızda yapılandırın:

### Hızlı Kurulum (Önerilen)

```csharp
using SmartRAG.Extensions;
using SmartRAG.Enums;

var builder = WebApplication.CreateBuilder(args);

// Tek satırda basit yapılandırma
builder.Services.UseSmartRag(builder.Configuration,
    storageProvider: StorageProvider.InMemory,  // In-memory ile başlayın
    aiProvider: AIProvider.Gemini               // AI sağlayıcınızı seçin
);

var app = builder.Build();
app.Run();
```

### Gelişmiş Kurulum

```csharp
using SmartRAG.Extensions;
using SmartRAG.Enums;

var builder = WebApplication.CreateBuilder(args);

// Seçeneklerle gelişmiş yapılandırma
builder.Services.AddSmartRag(builder.Configuration, options =>
{
    // AI Sağlayıcı
    options.AIProvider = AIProvider.OpenAI;
    
    // Depolama Sağlayıcı
    options.StorageProvider = StorageProvider.Qdrant;
    
    // Parçalama Yapılandırması
    options.MaxChunkSize = 1000;
    options.MinChunkSize = 100;
    options.ChunkOverlap = 200;
    
    // Yeniden Deneme Yapılandırması
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
    
    // Yedek Sağlayıcılar
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new List<AIProvider> 
    { 
        AIProvider.Anthropic, 
        AIProvider.Gemini 
    };
});

var app = builder.Build();
app.Run();
```

## Yapılandırma Dosyası

`appsettings.json` veya `appsettings.Development.json` oluşturun:

```json
{
  "SmartRAG": {
    "AIProvider": "OpenAI",
    "StorageProvider": "InMemory",
    "MaxChunkSize": 1000,
    "MinChunkSize": 100,
    "ChunkOverlap": 200,
    "MaxRetryAttempts": 3,
    "RetryDelayMs": 1000,
    "RetryPolicy": "ExponentialBackoff",
    "EnableFallbackProviders": false
  },
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-proj-API_ANAHTARINIZ",
      "Endpoint": "https://api.openai.com/v1",
      "Model": "gpt-5.1",
      "EmbeddingModel": "text-embedding-3-small",
      "MaxTokens": 4096,
      "Temperature": 0.7
    },
    "Anthropic": {
      "ApiKey": "sk-ant-API_ANAHTARINIZ",
      "Endpoint": "https://api.anthropic.com",
      "Model": "claude-sonnet-4-5",
      "MaxTokens": 4096,
      "Temperature": 0.3,
      "EmbeddingApiKey": "pa-VOYAGE_ANAHTARINIZ",
      "EmbeddingModel": "voyage-3.5"
    },
    "Gemini": {
      "ApiKey": "GEMINI_ANAHTARINIZ",
      "Endpoint": "https://generativelanguage.googleapis.com/v1beta",
      "Model": "gemini-2.5-pro",
      "EmbeddingModel": "embedding-001",
      "MaxTokens": 4096,
      "Temperature": 0.3
    },
    "AzureOpenAI": {
      "ApiKey": "azure-openai-api-anahtariniz",
      "Endpoint": "https://your-resource.openai.azure.com/",
      "Model": "gpt-5.1",
      "EmbeddingModel": "text-embedding-3-small",
      "ApiVersion": "2024-10-21",
      "MaxTokens": 4096,
      "Temperature": 0.7
    },
    "Custom": {
      "ApiKey": "ozel-api-anahtariniz",
      "Endpoint": "https://api.yourprovider.com/v1/chat/completions",
      "Model": "model-adi",
      "MaxTokens": 4096,
      "Temperature": 0.7
    }
  },
  "Storage": {
    "InMemory": {
      "MaxDocuments": 1000
    },
    "Qdrant": {
      "Host": "localhost:6334",
      "UseHttps": false,
      "ApiKey": "",
      "CollectionName": "smartrag_documents",
      "VectorSize": 1536,
      "DistanceMetric": "Cosine"
    },
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Password": "",
      "Username": "",
      "Database": 0,
      "KeyPrefix": "smartrag:doc:",
      "ConnectionTimeout": 30,
      "EnableSsl": false
    }
  }
}
```

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Güvenlik Uyarısı</h4>
    <p class="mb-0">
        <strong>API anahtarlarını asla kaynak kontrolüne commit etmeyin!</strong> 
        Yerel geliştirme için <code>appsettings.Development.json</code> kullanın (.gitignore'a ekleyin).
        Üretim için environment variables veya Azure Key Vault kullanın.
    </p>
</div>

## Hızlı Kullanım Örneği

### 1. Doküman Yükleme

```csharp
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    
    public DocumentController(IDocumentService documentService)
    {
        _documentService = documentService;
    }
    
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var document = await _documentService.UploadDocumentAsync(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            "kullanici-123"
        );
        
        return Ok(new 
        { 
            id = document.Id,
            fileName = document.FileName,
            chunks = document.Chunks.Count,
            message = "Doküman başarıyla işlendi"
        });
    }
}
```

### 2. AI ile Soru Sorma

```csharp
public class IntelligenceController : ControllerBase
{
    private readonly IDocumentSearchService _searchService;
    
    public IntelligenceController(IDocumentSearchService searchService)
    {
        _searchService = searchService;
    }
    
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] QuestionRequest request)
    {
        var response = await _searchService.QueryIntelligenceAsync(
            request.Question,
            maxResults: 5
        );
        
        return Ok(response);
    }
}

public class QuestionRequest
{
    public string Question { get; set; } = string.Empty;
}
```

### 3. Yanıt Örneği

```json
{
  "query": "Ana faydalar nelerdir?",
  "answer": "Sözleşme belgesine göre ana faydalar şunlardır: 1) 7/24 müşteri desteği, 2) 30 gün para iade garantisi, 3) Ömür boyu ücretsiz güncellemeler...",
  "sources": [
    {
      "sourceType": "Document",
      "documentId": "00000000-0000-0000-0000-000000000000",
      "fileName": "sozlesme.pdf",
      "relevantContent": "Hizmetimiz 7/24 müşteri desteği içerir...",
      "relevanceScore": 0.94,
      "location": null
    }
  ],
  "searchedAt": "2025-10-18T14:30:00Z",
  "configuration": {
    "aiProvider": "OpenAI",
    "storageProvider": "Redis",
    "model": "gpt-5.1"
  }
}
```

## Konuşma Geçmişi

SmartRAG otomatik olarak konuşma geçmişini yönetir:

```csharp
// İlk soru
var q1 = await _searchService.QueryIntelligenceAsync("Makine öğrenimi nedir?");

// Takip sorusu - AI önceki bağlamı hatırlar
var q2 = await _searchService.QueryIntelligenceAsync("Denetimli öğrenmeyi açıklar mısın?");

// Yeni konuşma başlat
var newConv = await _searchService.QueryIntelligenceAsync(
    "Yeni konu", 
    startNewConversation: true
);
```

<div class="alert alert-success">
    <h4><i class="fas fa-lightbulb me-2"></i> İpucu</h4>
    <p class="mb-0">
        SmartRAG otomatik olarak oturum ID'lerini ve konuşma bağlamını yönetir. 
        Manuel oturum yönetimi gerekmez!
    </p>
</div>

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-cog"></i>
            </div>
            <h3>Yapılandırma</h3>
            <p>Tüm yapılandırma seçeneklerini, AI sağlayıcılarını, depolama backend'lerini ve gelişmiş ayarları keşfedin.</p>
            <a href="{{ site.baseurl }}/tr/configuration" class="btn btn-outline-primary btn-sm mt-3">
                SmartRAG'i Yapılandır <i class="fas fa-arrow-right ms-2"></i>
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-code"></i>
            </div>
            <h3>API Referans</h3>
            <p>Tüm interface'ler, metodlar, parametreler ve örneklerle eksiksiz API dokümantasyonu.</p>
            <a href="{{ site.baseurl }}/tr/api-reference" class="btn btn-outline-primary btn-sm mt-3">
                API Dokümanlarını Görüntüle <i class="fas fa-arrow-right ms-2"></i>
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-lightbulb"></i>
            </div>
            <h3>Örnekler</h3>
            <p>Çok veritabanlı sorgular, OCR işleme ve ses transkripsiyonu dahil gerçek dünya örnekleri.</p>
            <a href="{{ site.baseurl }}/tr/examples" class="btn btn-outline-primary btn-sm mt-3">
                Örnekleri Gör <i class="fas fa-arrow-right ms-2"></i>
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-history"></i>
            </div>
            <h3>Değişiklikler</h3>
            <p>Tüm versiyonlardaki yeni özellikleri, iyileştirmeleri ve breaking change'leri takip edin.</p>
            <a href="{{ site.baseurl }}/tr/changelog" class="btn btn-outline-primary btn-sm mt-3">
                Changelog'u Görüntüle <i class="fas fa-arrow-right ms-2"></i>
            </a>
        </div>
    </div>
</div>

## Yardıma İhtiyacınız Var mı?

<div class="alert alert-info mt-5">
    <h4><i class="fas fa-question-circle me-2"></i> Destek & Topluluk</h4>
    <p>Sorunla karşılaşırsanız veya yardıma ihtiyacınız olursa:</p>
    <ul class="mb-0">
        <li><strong>GitHub Issues:</strong> <a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">Hataları bildirin veya özellik isteyin</a></li>
        <li><strong>E-posta Desteği:</strong> <a href="mailto:b.yerlikaya@outlook.com">b.yerlikaya@outlook.com</a></li>
        <li><strong>LinkedIn:</strong> <a href="https://www.linkedin.com/in/barisyerlikaya/" target="_blank">Profesyonel sorular için bağlantı kurun</a></li>
        <li><strong>Dokümantasyon:</strong> Bu sitede tam dokümantasyonu keşfedin</li>
    </ul>
</div>

