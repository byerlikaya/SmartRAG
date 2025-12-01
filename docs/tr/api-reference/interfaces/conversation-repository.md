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

##### GetConversationHistoryAsync

Bir oturum için konuşma geçmişini alır.

```csharp
Task<string> GetConversationHistoryAsync(string sessionId)
```

**Parametreler:**
- `sessionId` (string): Oturum tanımlayıcısı

**Döndürür:** Formatlanmış string olarak konuşma geçmişi

##### AddToConversationAsync

Oturum geçmişine bir konuşma turu ekler.

```csharp
Task AddToConversationAsync(string sessionId, string question, string answer)
```

**Parametreler:**
- `sessionId` (string): Oturum tanımlayıcısı
- `question` (string): Kullanıcı sorusu
- `answer` (string): Asistan cevabı

##### ClearConversationAsync

Bir oturum için konuşma geçmişini temizler.

```csharp
Task ClearConversationAsync(string sessionId)
```

**Parametreler:**
- `sessionId` (string): Oturum tanımlayıcısı

##### SessionExistsAsync

Bir oturumun var olup olmadığını kontrol eder.

```csharp
Task<bool> SessionExistsAsync(string sessionId)
```

**Parametreler:**
- `sessionId` (string): Oturum tanımlayıcısı

**Döndürür:** Oturum varsa true, aksi takdirde false

#### Uygulamalar

- `SqliteConversationRepository`
- `InMemoryConversationRepository`
- `FileSystemConversationRepository`
- `RedisConversationRepository`


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

