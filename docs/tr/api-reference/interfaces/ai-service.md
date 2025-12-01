---
layout: default
title: IAIService
description: IAIService arayüz dokümantasyonu
lang: tr
---

## IAIService

**Amaç:** Metin üretimi ve embedding'ler için AI sağlayıcı iletişimi

**Namespace:** `SmartRAG.Interfaces.AI`

### Metodlar

#### GenerateResponseAsync

Sorgu ve bağlam temelinde AI yanıtı üretir.

```csharp
Task<string> GenerateResponseAsync(
    string query, 
    IEnumerable<string> context
)
```

**Örnek:**

```csharp
var contextChunks = new List<string>
{
    "Doküman parçası 1...",
    "Doküman parçası 2...",
    "Doküman parçası 3..."
};

var response = await _aiService.GenerateResponseAsync(
    "Ana konular nelerdir?",
    contextChunks
);

Console.WriteLine(response);
```

#### GenerateEmbeddingsAsync

Metin için embedding vektörü üretir.

```csharp
Task<List<float>> GenerateEmbeddingsAsync(string text)
```

**Döndürür:** Embedding vektörü (genellikle 768 veya 1536 boyut)

#### GenerateEmbeddingsBatchAsync

Birden fazla metin için toplu embedding üretir.

```csharp
Task<List<List<float>>> GenerateEmbeddingsBatchAsync(
    IEnumerable<string> texts
)
```

**Örnek:**

```csharp
var texts = new List<string> { "Metin 1", "Metin 2", "Metin 3" };
var embeddings = await _aiService.GenerateEmbeddingsBatchAsync(texts);

Console.WriteLine($"Oluşturulan {embeddings.Count} embedding");
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön
