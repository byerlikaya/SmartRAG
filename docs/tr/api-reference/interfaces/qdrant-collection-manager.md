---
layout: default
title: IQdrantCollectionManager
description: IQdrantCollectionManager arayüz dokümantasyonu
lang: tr
---

## IQdrantCollectionManager

**Amaç:** Qdrant koleksiyonlarını ve doküman depolamayı yönetmek için arayüz

**Namespace:** `SmartRAG.Interfaces.Storage.Qdrant`

Qdrant vektör veritabanı için koleksiyon yaşam döngüsü yönetimi.

#### Metodlar

```csharp
Task EnsureCollectionExistsAsync();
Task CreateCollectionAsync(string collectionName, int vectorDimension);
Task EnsureDocumentCollectionExistsAsync(string collectionName, Document document);
Task<int> GetVectorDimensionAsync();
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

