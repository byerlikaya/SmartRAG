---
layout: default
title: IStorageFactory
description: IStorageFactory arayüz dokümantasyonu
lang: tr
---

## IStorageFactory

**Amaç:** Doküman ve konuşma depolama repository'leri oluşturmak için fabrika

**Namespace:** `SmartRAG.Interfaces.Storage`

Tüm depolama işlemleri için birleşik fabrika.

#### Metodlar

```csharp
IDocumentRepository CreateRepository(StorageConfig config);
IDocumentRepository CreateRepository(StorageProvider provider);
StorageProvider GetCurrentProvider();
IDocumentRepository GetCurrentRepository();
IConversationRepository CreateConversationRepository(StorageConfig config);
IConversationRepository CreateConversationRepository(StorageProvider provider);
IConversationRepository GetCurrentConversationRepository();
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

