---
layout: default
title: IAIRequestExecutor
description: IAIRequestExecutor arayüz dokümantasyonu
lang: tr
---

## IAIRequestExecutor

**Amaç:** Yeniden deneme/yedekleme ile AI istek yürütme

**Namespace:** `SmartRAG.Interfaces.AI`

Otomatik yeniden deneme ve yedekleme mantığı ile AI isteklerini işler.

#### Metodlar

```csharp
Task<string> ExecuteRequestAsync(string prompt, CancellationToken cancellationToken = default);
Task<List<float>> ExecuteEmbeddingRequestAsync(string text, CancellationToken cancellationToken = default);
```


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

