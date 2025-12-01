---
layout: default
title: IPromptBuilderService
description: IPromptBuilderService arayüz dokümantasyonu
lang: tr
---

## IPromptBuilderService

**Amaç:** Farklı senaryolar için AI prompt'ları oluşturmak için servis

**Namespace:** `SmartRAG.Interfaces.AI`

Konuşma geçmişi desteği ile merkezi prompt oluşturma.

#### Metodlar

##### BuildDocumentRagPrompt

Doküman tabanlı RAG cevap üretimi için prompt oluşturur.

```csharp
string BuildDocumentRagPrompt(
    string query, 
    string context, 
    string? conversationHistory = null, 
    string? preferredLanguage = null
)
```

**Parametreler:**
- `query` (string): Kullanıcı sorgusu
- `context` (string): Doküman bağlamı
- `conversationHistory` (string?, isteğe bağlı): İsteğe bağlı konuşma geçmişi
- `preferredLanguage` (string?, isteğe bağlı): Tercih edilen dil kodu (örn. "tr", "en") açık AI yanıt dili için

**Döndürür:** Oluşturulmuş prompt string'i

##### BuildHybridMergePrompt

Hibrit sonuçları (veritabanı + dokümanlar) birleştirmek için prompt oluşturur.

```csharp
string BuildHybridMergePrompt(
    string query, 
    string? databaseContext, 
    string? documentContext, 
    string? conversationHistory = null, 
    string? preferredLanguage = null
)
```

**Parametreler:**
- `query` (string): Kullanıcı sorgusu
- `databaseContext` (string?, isteğe bağlı): Veritabanı bağlamı
- `documentContext` (string?, isteğe bağlı): Doküman bağlamı
- `conversationHistory` (string?, isteğe bağlı): İsteğe bağlı konuşma geçmişi
- `preferredLanguage` (string?, isteğe bağlı): Tercih edilen dil kodu (örn. "tr", "en") açık AI yanıt dili için

**Döndürür:** Oluşturulmuş prompt string'i

##### BuildConversationPrompt

Genel konuşma için prompt oluşturur.

```csharp
string BuildConversationPrompt(
    string query, 
    string? conversationHistory = null, 
    string? preferredLanguage = null
)
```

**Parametreler:**
- `query` (string): Kullanıcı sorgusu
- `conversationHistory` (string?, isteğe bağlı): İsteğe bağlı konuşma geçmişi
- `preferredLanguage` (string?, isteğe bağlı): Tercih edilen dil kodu (örn. "tr", "en") açık AI yanıt dili için

**Döndürür:** Oluşturulmuş prompt string'i


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

