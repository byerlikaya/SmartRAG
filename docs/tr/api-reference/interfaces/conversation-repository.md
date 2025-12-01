---
layout: default
title: IConversationRepository
description: IConversationRepository arayüz dokümantasyonu
lang: tr
---

## IConversationRepository

**Amaç:** Konuşma depolama için veri erişim katmanı

**Namespace:** `SmartRAG.Interfaces.Storage`

Daha iyi SRP uyumu için `IDocumentRepository`'den ayrıldı.

#### Metodlar

```csharp
Task<string> GetConversationHistoryAsync(string sessionId);
Task SaveConversationAsync(string sessionId, string history);
Task DeleteConversationAsync(string sessionId);
Task<bool> ConversationExistsAsync(string sessionId);
```

#### Uygulamalar

- `SqliteConversationRepository`
- `InMemoryConversationRepository`
- `FileSystemConversationRepository`
- `RedisConversationRepository`


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

