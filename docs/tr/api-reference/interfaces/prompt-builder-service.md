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

```csharp
string BuildDocumentRagPrompt(string query, string context, string? conversationHistory = null);
string BuildHybridMergePrompt(string query, string? databaseContext, string? documentContext, string? conversationHistory = null);
string BuildConversationPrompt(string query, string? conversationHistory = null);
```


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

