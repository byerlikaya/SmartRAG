---
layout: default
title: Taşınma Kılavuzları
description: SmartRAG için adım adım taşınma kılavuzları
lang: tr
---

## Taşınma Kılavuzları

<p>SmartRAG sürümleri arasında yükseltme için adım adım taşınma kılavuzları.</p>

---

### v3.x'ten v4.0.0'a Taşınma

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Kırıcı Değişiklikler</h4>
    <p class="mb-0">v4.0.0 .NET 6 hedefler ve SmartRAG.Dashboard projesi ana SmartRAG paketine birleştirilmiştir.</p>
</div>

<p>Bu taşınma kılavuzu, SmartRAG v3.x'ten v4.0.0'a yükseltme sırasındaki framework ve proje yapısı değişikliklerini kapsar.</p>

<p><strong>Not:</strong> SmartRAG.Dashboard hiçbir zaman NuGet paketi olarak yayınlanmadı. v3.x'te solution içinde ayrı bir proje olarak yer alıyordu; v4.0'da bu proje kaldırıldı ve Dashboard kodu SmartRAG paketine dahil edildi.</p>

#### Adım 1: Hedef Framework'ü Güncelleyin

<p>Projeniz .NET 6 veya üzerini hedeflemelidir:</p>

```xml
<TargetFramework>net6.0</TargetFramework>
<!-- veya net7.0, net8.0, net9.0 -->
```

<p>.NET Core 3.0, .NET 5 veya .NET Standard 2.1 hedefleyen projeler en az .NET 6'ya yükseltmelidir.</p>

#### Adım 2: SmartRAG.Dashboard Proje Referansını Kaldırın

<p>Dashboard artık SmartRAG paketinde. SmartRAG.Dashboard projesine ProjectReference kullandıysanız, bu referansı kaldırın:</p>

```xml
<!-- Bu satırı kaldırın -->
<ProjectReference Include="..\..\src\SmartRAG.Dashboard\SmartRAG.Dashboard.csproj" />
```

<p>Sadece SmartRAG NuGet paketini veya ana SmartRAG projesini referans alıyorsanız, değişiklik yapmanız gerekmez.</p>

#### Adım 3: Aynı API'yi Kullanmaya Devam Edin

<p>Kod değişikliği gerekmez. Dashboard API aynı kalır:</p>

```csharp
using SmartRAG.Dashboard;

builder.Services.AddSmartRag(builder.Configuration);
builder.Services.AddSmartRagDashboard(options => { options.Path = "/smartrag"; });

app.UseSmartRagDashboard("/smartrag");
app.MapSmartRagDashboard("/smartrag");
```

<p><code>SmartRAG.Dashboard</code> namespace ve extension metodları artık SmartRAG paketinin bir parçasıdır.</p>

---

### v2.x'ten v3.0.0'a Taşınma

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Temel Değişiklikler</h4>
    <p class="mb-0">Birincil değişiklik, <code>GenerateRagAnswerAsync</code>'in <code>QueryIntelligenceAsync</code> olarak yeniden adlandırılmasıdır.</p>
</div>

<p>Bu taşınma kılavuzu, SmartRAG v2.x'ten v3.0.0'a yükseltme sırasında gerekli değişiklikleri kapsar.</p>

#### Adım 1: Metod Çağrısını Güncelleyin

<p>Service metod çağrınızı <code>GenerateRagAnswerAsync</code>'den <code>QueryIntelligenceAsync</code>'e güncelleyin:</p>

```csharp
// Önce (v2.x)
var response = await _searchService.GenerateRagAnswerAsync(query, maxResults);

// Sonra (v3.0.0)
var response = await _searchService.QueryIntelligenceAsync(query, maxResults);
```

#### Adım 2: API Endpoint'lerini Güncelleyin (Web API kullanıyorsanız)

<p>Web API controller'ınız varsa, sadece service method çağrısını güncelleyin:</p>

```csharp
// Önce (v2.x)
[HttpPost("generate-answer")]
public async Task<IActionResult> GenerateAnswer([FromBody] QueryRequest request)
{
    var response = await _searchService.GenerateRagAnswerAsync(request.Query);
    return Ok(response);
}

// Sonra (v3.0.0) - Sadece method adı değişti
[HttpPost("generate-answer")]
public async Task<IActionResult> GenerateAnswer([FromBody] QueryRequest request)
{
    var response = await _searchService.QueryIntelligenceAsync(request.Query);
    return Ok(response);
}
```

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Not</h4>
    <p class="mb-0">Mevcut endpoint yollarınızı ve controller method adlarınızı koruyabilirsiniz. Sadece service method çağrısını güncellemeniz yeterlidir.</p>
</div>

#### Adım 3: İstemci Kodunu Güncelleyin (uygunsa)

<p>API'yi çağıran istemci kodunuz varsa, endpoint'i güncelleyin:</p>

```javascript
// Önce
const response = await fetch('/api/intelligence/generate-answer', { ... });

// Sonra
const response = await fetch('/api/intelligence/query', { ... });
```

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> v4.0.0 Güncellemesi</h4>
    <p class="mb-0">
        <code>GenerateRagAnswerAsync</code> v4.0.0'da kaldırıldı. v4'e yükseltirken <code>QueryIntelligenceAsync</code> kullanmanız gerekir.
    </p>
</div>

---

### v3.8.x'ten v3.9.0'a Taşınma

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Kırıcı Değişiklikler</h4>
    <p class="mb-0">v3.9.0, <code>IStorageFactory</code>, <code>IConversationRepository</code> için kırıcı değişiklikler içerir ve <code>IQdrantCacheManager</code>'ı kaldırır.</p>
</div>

#### Adım 1: IStorageFactory Kullanımını Güncelleyin

<p><code>IStorageFactory</code> inject edip <code>GetCurrentRepository()</code> çağırıyorsanız, scoped <code>IServiceProvider</code> geçirin:</p>

```csharp
// Önce (v3.8.x)
var repository = _storageFactory.GetCurrentRepository();

// Sonra (v3.9.0)
var repository = _storageFactory.GetCurrentRepository(_serviceProvider);
```

<p>DI kullanırken kayıt zaten scoped provider geçirir. <code>IDocumentRepository</code>'yi doğrudan çözümlüyorsanız değişiklik gerekmez.</p>

#### Adım 2: Özel IConversationRepository Implementasyonlarını Güncelleyin

<p>Özel <code>IConversationRepository</code> implementasyonunuz varsa bu metodları ekleyin:</p>

```csharp
Task AppendSourcesForTurnAsync(string sessionId, string sourcesJson, CancellationToken cancellationToken = default);
Task<string> GetSourcesForSessionAsync(string sessionId, CancellationToken cancellationToken = default);
Task<string[]> GetAllSessionIdsAsync(CancellationToken cancellationToken = default);
```

<p>Yerleşik implementasyonlar (Sqlite, Redis, FileSystem, InMemory) bunları zaten içerir.</p>

#### Adım 3: IQdrantCacheManager Referanslarını Kaldırın (kullanıyorsanız)

<p><code>IQdrantCacheManager</code> ve <code>QdrantCacheManager</code> kaldırıldı. Arama artık sorgu sonuç önbelleklemesi kullanmıyor. Bu tiplere referansları kaldırın.</p>

---

### v1.x'ten v2.0.0'a Taşınma

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Framework Değişikliği</h4>
    <p class="mb-0">Sürüm 2.0.0, .NET 9.0'dan .NET Standard 2.1'e taşınmıştır</p>
</div>

<p>Bu taşınma kılavuzu, SmartRAG v1.x'ten v2.0.0'a yükseltme sırasında framework uyumluluk değişikliklerini kapsar.</p>

#### Adım 1: Framework Uyumluluğunu Doğrulayın

<p>Projeniz şu framework'lerden birini hedeflemelidir:</p>

```xml
<TargetFramework>netstandard2.0</TargetFramework>
<TargetFramework>netstandard2.1</TargetFramework>
<TargetFramework>netcoreapp2.0</TargetFramework>
<TargetFramework>net461</TargetFramework>
<TargetFramework>net5.0</TargetFramework>
<TargetFramework>net6.0</TargetFramework>
<TargetFramework>net7.0</TargetFramework>
<TargetFramework>net8.0</TargetFramework>
<TargetFramework>net9.0</TargetFramework>
```

#### Adım 2: NuGet Paketini Güncelleyin

<p>SmartRAG paketini 2.0.0 sürümüne güncelleyin:</p>

```bash
dotnet add package SmartRAG --version 2.0.0
```

#### Adım 3: Kod Uyumluluğunu Doğrulayın

<p>API değişikliği yok - tüm işlevsellik aynı kalır. Sadece projenizin uyumlu framework'ü hedeflediğinden emin olun.</p>

---

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-history"></i>
            </div>
            <h3>Sürüm Geçmişi</h3>
            <p>Tüm sürümler ve değişikliklerle birlikte tam sürüm geçmişi</p>
            <a href="{{ site.baseurl }}/tr/changelog/version-history" class="btn btn-outline-primary btn-sm mt-3">
                Sürüm Geçmişi
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-exclamation-triangle"></i>
            </div>
            <h3>Kullanımdan Kaldırma Bildirimleri</h3>
            <p>Kullanımdan kaldırılan özellikler ve planlanan kaldırmalar</p>
            <a href="{{ site.baseurl }}/tr/changelog/deprecation" class="btn btn-outline-primary btn-sm mt-3">
                Kullanımdan Kaldırma Bildirimleri
            </a>
        </div>
    </div>
</div>
