---
layout: default
title: ISourceBuilderService
description: ISourceBuilderService arayüz dokümantasyonu
lang: tr
---

## ISourceBuilderService

**Amaç:** Arama sonucu kaynakları oluşturma

**Namespace:** `SmartRAG.Interfaces.Search`

Chunk'lardan `SearchSource` nesneleri oluşturur.

#### Metodlar

```csharp
List<SearchSource> BuildSources(List<DocumentChunk> chunks);
```


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

