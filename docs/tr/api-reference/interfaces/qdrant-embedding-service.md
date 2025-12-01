---
layout: default
title: IQdrantEmbeddingService
description: IQdrantEmbeddingService arayüz dokümantasyonu
lang: tr
---

## IQdrantEmbeddingService

**Amaç:** Metin içeriği için embedding'ler oluşturmak için arayüz

**Namespace:** `SmartRAG.Interfaces.Storage.Qdrant`

Qdrant vektör depolama için embedding oluşturma.

#### Metodlar

```csharp
Task<List<float>> GenerateEmbeddingAsync(string text);
Task<int> GetVectorDimensionAsync();
```


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

