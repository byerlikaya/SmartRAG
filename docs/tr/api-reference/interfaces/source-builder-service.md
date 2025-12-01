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

##### BuildSourcesAsync

Doküman chunk'larından arama kaynakları oluşturur.

```csharp
Task<List<SearchSource>> BuildSourcesAsync(
    List<DocumentChunk> chunks, 
    IDocumentRepository documentRepository
)
```

**Parametreler:**
- `chunks` (List<DocumentChunk>): Kaynak oluşturulacak doküman chunk'ları
- `documentRepository` (IDocumentRepository): Doküman işlemleri için repository

**Döndürür:** Metadata ile arama kaynakları listesi


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

