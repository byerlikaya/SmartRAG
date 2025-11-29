---
layout: default
title: Taşınma Kılavuzları
description: SmartRAG için adım adım taşınma kılavuzları
lang: tr
---

## Taşınma Kılavuzları

### v2.x'ten v3.0.0'a Taşınma

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Temel Değişiklikler</h4>
    <p class="mb-0">Birincil değişiklik, <code>GenerateRagAnswerAsync</code>'in <code>QueryIntelligenceAsync</code> olarak yeniden adlandırılmasıdır.</p>
</div>

**Adım 1: Metod çağrılarını güncelleyin**

```csharp
// Önce (v2.x)
var response = await _searchService.GenerateRagAnswerAsync(query, maxResults);

// Sonra (v3.0.0)
var response = await _searchService.QueryIntelligenceAsync(query, maxResults);
```

**Adım 2: API endpoint'lerini güncelleyin (Web API kullanıyorsanız)**

Web API controller'ınız varsa, sadece service method çağrısını güncelleyin:

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

**Not:** Mevcut endpoint yollarınızı ve controller method adlarınızı koruyabilirsiniz. Sadece service method çağrısını güncellemeniz yeterlidir.
```

<div class="alert alert-success">
    <h4><i class="fas fa-check-circle me-2"></i> Acil Eylem Gerekmez</h4>
    <p class="mb-0">
        Eski <code>GenerateRagAnswerAsync</code> metodu hala çalışıyor (kullanımdan kaldırıldı olarak işaretli). 
        v4.0.0 yayınlanmadan önce kademeli olarak taşınabilirsiniz.
    </p>
                    </div>

### v1.x'ten v2.0.0'a Taşınma

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Temel Değişiklikler</h4>
    <p>Birincil değişiklik, .NET 9.0'dan .NET Standard 2.1'e taşınmasıdır.</p>
</div>

**Adım 1: Hedef framework'ü güncelleyin**

```xml
<!-- Önce (.csproj) -->
<TargetFramework>net9.0</TargetFramework>

<!-- Sonra (.csproj) -->
<TargetFramework>netstandard2.1</TargetFramework>
```

**Adım 2: Paket referanslarını kontrol edin**

```xml
<!-- .NET Standard 2.1 uyumlu paketler -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
```

**Adım 3: Kod değişiklikleri**

```csharp
// Önce (v1.x)
using Microsoft.Extensions.DependencyInjection;

// Sonra (v2.0.0) - Aynı
using Microsoft.Extensions.DependencyInjection;
```

---