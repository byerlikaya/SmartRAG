---
layout: default
title: Taşınma Kılavuzları
description: SmartRAG için adım adım taşınma kılavuzları
lang: tr
---

## Taşınma Kılavuzları

<p>SmartRAG sürümleri arasında yükseltme için adım adım taşınma kılavuzları.</p>

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

<div class="alert alert-success">
    <h4><i class="fas fa-check-circle me-2"></i> Acil Eylem Gerekmez</h4>
    <p class="mb-0">
        Eski <code>GenerateRagAnswerAsync</code> metodu hala çalışıyor (kullanımdan kaldırıldı olarak işaretli). 
        v4.0.0 yayınlanmadan önce kademeli olarak taşınabilirsiniz.
    </p>
</div>

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
