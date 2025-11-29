---
layout: default
title: IAIService
description: IAIService arayüz dokümantasyonu
lang: tr
---

## IAIService

**Amaç:** AI provider'ları ile etkileşim

**Namespace:** `SmartRAG.Interfaces.AI`

### Metodlar

#### GenerateResponseAsync

AI'dan yanıt oluşturun.

```csharp
Task<string> GenerateResponseAsync(
    string prompt, 
    CancellationToken cancellationToken = default
)
```

#### GenerateEmbeddingsAsync

Tek bir metin için embedding oluşturun.

```csharp
Task<float[]> GenerateEmbeddingsAsync(
    string text, 
    CancellationToken cancellationToken = default
)
```

#### GenerateEmbeddingsBatchAsync

Birden fazla metin için toplu embedding oluşturun.

```csharp
Task<List<float[]>> GenerateEmbeddingsBatchAsync(
    List<string> texts, 
    CancellationToken cancellationToken = default
)
```

**Örnek:**

```csharp
var texts = new List<string>
{
    "Machine learning is fascinating",
    "AI will change the world",
    "Deep learning models are powerful"
};

var embeddings = await _aiService.GenerateEmbeddingsBatchAsync(texts);
Console.WriteLine($"Oluşturulan embedding sayısı: {embeddings.Count}");
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

