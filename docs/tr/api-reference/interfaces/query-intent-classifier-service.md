---
layout: default
title: IQueryIntentClassifierService
description: IQueryIntentClassifierService arayüz dokümantasyonu
lang: tr
---

## IQueryIntentClassifierService

**Amaç:** Sorgu niyetini sınıflandırmak için servis (konuşma vs bilgi)

**Namespace:** `SmartRAG.Interfaces.Support`

Hibrit yönlendirme için AI tabanlı sorgu niyet sınıflandırması.

#### Metodlar

```csharp
Task<bool> IsGeneralConversationAsync(string query, string? conversationHistory = null);
bool TryParseCommand(string input, out QueryCommandType commandType, out string payload);
```

**Komut Türleri:**
- `QueryCommandType.None`: Komut algılanmadı
- `QueryCommandType.NewConversation`: `/new` veya `/reset` komutu
- `QueryCommandType.ForceConversation`: `/conv` komutu


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

