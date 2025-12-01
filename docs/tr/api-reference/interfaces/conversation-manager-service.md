---
layout: default
title: IConversationManagerService
description: IConversationManagerService arayüz dokümantasyonu
lang: tr
---

## IConversationManagerService

**Amaç:** Konuşma oturumu yönetimi ve geçmiş takibi

**Namespace:** `SmartRAG.Interfaces.Support`

Bu interface, daha iyi sorumluluk ayrımı için doküman işlemlerinden ayrılmış özel konuşma yönetimi sağlar.

### Metodlar

#### StartNewConversationAsync

Yeni bir konuşma oturumu başlatır.

```csharp
Task<string> StartNewConversationAsync()
```

**Döndürür:** Yeni oturum ID'si (string)

**Örnek:**

```csharp
var sessionId = await _conversationManager.StartNewConversationAsync();
Console.WriteLine($"Oturum başlatıldı: {sessionId}");
```

#### GetOrCreateSessionIdAsync

Mevcut oturum ID'sini alır veya otomatik olarak yeni bir tane oluşturur.

```csharp
Task<string> GetOrCreateSessionIdAsync()
```

**Döndürür:** Oturum ID'si (string)

**Kullanım Senaryosu:** Manuel oturum yönetimi olmadan otomatik oturum sürekliliği

**Örnek:**

```csharp
// Otomatik olarak oturumu yönetir - yoksa yeni oluşturur
var sessionId = await _conversationManager.GetOrCreateSessionIdAsync();
```

#### AddToConversationAsync

Oturum geçmişine bir konuşma turu (soru + cevap) ekler.

```csharp
Task AddToConversationAsync(
    string sessionId, 
    string question, 
    string answer
)
```

**Parametreler:**
- `sessionId` (string): Oturum tanımlayıcısı
- `question` (string): Kullanıcının sorusu
- `answer` (string): AI'ın cevabı

**Örnek:**

```csharp
await _conversationManager.AddToConversationAsync(
    sessionId,
    "Makine öğrenimi nedir?",
    "Makine öğrenimi, sistemlerin öğrenmesini sağlayan AI'ın bir alt kümesidir..."
);
```

#### GetConversationHistoryAsync

Bir oturum için tam konuşma geçmişini alır.

```csharp
Task<string> GetConversationHistoryAsync(string sessionId)
```

**Parametreler:**
- `sessionId` (string): Oturum tanımlayıcısı

**Döndürür:** String olarak biçimlendirilmiş konuşma geçmişi

**Format:**
```
User: [soru]
Assistant: [cevap]
User: [sonraki soru]
Assistant: [sonraki cevap]
```

**Örnek:**

```csharp
var history = await _conversationManager.GetConversationHistoryAsync(sessionId);
Console.WriteLine(history);
```

#### TruncateConversationHistory

Sadece son turları tutmak için konuşma geçmişini kısaltır (bellek yönetimi).

```csharp
string TruncateConversationHistory(
    string history, 
    int maxTurns = 3
)
```

**Parametreler:**
- `history` (string): Tam konuşma geçmişi
- `maxTurns` (int): Tutulacak maksimum konuşma turu sayısı (varsayılan: 3)

**Döndürür:** Kısaltılmış konuşma geçmişi

**Kullanım Senaryosu:** AI prompt'larında context window taşmasını önler

**Örnek:**

```csharp
var fullHistory = await _conversationManager.GetConversationHistoryAsync(sessionId);
var recentHistory = _conversationManager.TruncateConversationHistory(fullHistory, maxTurns: 5);
```

### Tam Kullanım Örneği

```csharp
public class ChatService
{
    private readonly IConversationManagerService _conversationManager;
    private readonly IDocumentSearchService _searchService;
    
    public ChatService(
        IConversationManagerService conversationManager,
        IDocumentSearchService searchService)
    {
        _conversationManager = conversationManager;
        _searchService = searchService;
    }
    
    public async Task<string> HandleChatAsync(string userMessage)
    {
        // Oturum al veya oluştur
        var sessionId = await _conversationManager.GetOrCreateSessionIdAsync();
        
        // Context için konuşma geçmişini al
        var history = await _conversationManager.GetConversationHistoryAsync(sessionId);
        
        // Context ile sorgu
        var response = await _searchService.QueryIntelligenceAsync(userMessage);
        
        // Konuşma geçmişine kaydet
        await _conversationManager.AddToConversationAsync(
            sessionId, 
            userMessage, 
            response.Answer
        );
        
        return response.Answer;
    }
    
    public async Task<string> StartNewChatAsync()
    {
        var newSessionId = await _conversationManager.StartNewConversationAsync();
        return $"Yeni konuşma başlatıldı: {newSessionId}";
    }
}
```

### Depolama Backend'leri

Konuşma geçmişi yapılandırılmış `IConversationRepository` kullanılarak depolanır:
- **SQLite**: `SqliteConversationRepository` - Kalıcı dosya tabanlı depolama
- **InMemory**: `InMemoryConversationRepository` - Hızlı, kalıcı olmayan (geliştirme)
- **FileSystem**: `FileSystemConversationRepository` - JSON dosya tabanlı depolama
- **Redis**: `RedisConversationRepository` - Yüksek performanslı dağıtık depolama

Depolama backend'i `StorageProvider` yapılandırmanıza göre otomatik olarak seçilir.


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

