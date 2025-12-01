---
layout: default
title: IAIProvider
description: IAIProvider arayüz dokümantasyonu
lang: tr
---

## IAIProvider

**Amaç:** Metin üretimi ve embedding'ler için düşük seviye AI sağlayıcı arayüzü

**Namespace:** `SmartRAG.Interfaces.AI`

Birden fazla AI backend için sağlayıcı soyutlaması.

#### Metodlar

```csharp
Task<string> GenerateTextAsync(string prompt, AIProviderConfig config);
Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config);
Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, AIProviderConfig config);
Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000);
```


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

