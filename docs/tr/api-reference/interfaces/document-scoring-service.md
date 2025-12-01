---
layout: default
title: IDocumentScoringService
description: IDocumentScoringService arayüz dokümantasyonu
lang: tr
---

## IDocumentScoringService

**Amaç:** Sorgu ilgisine göre doküman parçalarını puanlamak için servis

**Namespace:** `SmartRAG.Interfaces.Document`

Anahtar kelime ve semantik ilgi ile hibrit puanlama stratejisi.

#### Metodlar

```csharp
List<DocumentChunk> ScoreChunks(List<DocumentChunk> chunks, string query, List<string> queryWords, List<string> potentialNames);
double CalculateKeywordRelevanceScore(string query, string content);
```


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

