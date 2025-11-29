---
layout: default
title: ISemanticSearchService
description: ISemanticSearchService arayüz dokümantasyonu
lang: tr
---

## ISemanticSearchService

**Amaç:** Gelişmiş semantik arama ve benzerlik hesaplama

**Namespace:** `SmartRAG.Interfaces.Search`

### Metodlar

#### CalculateEnhancedSemanticSimilarityAsync

Gelişmiş semantik benzerlik skoru hesaplayın.

```csharp
Task<double> CalculateEnhancedSemanticSimilarityAsync(
    string query, 
    string documentContent, 
    CancellationToken cancellationToken = default
)
```

**Örnek:**

```csharp
var similarity = await _semanticSearchService.CalculateEnhancedSemanticSimilarityAsync(
    "machine learning algorithms",
    "artificial intelligence and neural networks",
    cancellationToken
);

Console.WriteLine($"Benzerlik Skoru: {similarity:F2}");
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

