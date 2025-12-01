---
layout: default
title: IDocumentRepository
description: IDocumentRepository arayüz dokümantasyonu
lang: tr
---

## IDocumentRepository

**Amaç:** Doküman depolama işlemleri için repository arayüzü

**Namespace:** `SmartRAG.Interfaces.Document`

İş mantığından ayrılmış repository katmanı.

#### Metodlar

```csharp
Task<Document> AddAsync(Document document);
Task<Document> GetByIdAsync(Guid id);
Task<List<Document>> GetAllAsync();
Task<bool> DeleteAsync(Guid id);
Task<int> GetCountAsync();
Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = 5);
```


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

