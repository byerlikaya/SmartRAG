---
layout: default
title: IContextExpansionService
description: IContextExpansionService arayüz dokümantasyonu
lang: tr
---

## IContextExpansionService

**Amaç:** Aynı dokümandaki bitişik chunk'ları dahil ederek doküman chunk context'ini genişletme

**Namespace:** `SmartRAG.Interfaces.Search`

### Metodlar

#### ExpandContextAsync

Aynı dokümandaki bitişik chunk'ları dahil ederek context'i genişletir. Bu, bir başlık bir chunk'ta ve içerik bir sonraki chunk'ta olsa bile, her ikisinin de arama sonuçlarına dahil edilmesini sağlar.

```csharp
Task<List<DocumentChunk>> ExpandContextAsync(
    List<DocumentChunk> chunks, 
    int contextWindow = 2
)
```

**Parametreler:**
- `chunks` (List<DocumentChunk>): Arama ile bulunan başlangıç chunk'ları
- `contextWindow` (int): Bulunan her chunk'ın öncesi ve sonrasına dahil edilecek bitişik chunk sayısı (varsayılan: 2, maksimum: 5)

**Döndürür:** Context ile genişletilmiş chunk listesi, doküman ID ve chunk index'e göre sıralanmış

**Örnek:**

```csharp
// İlgili chunk'ları ara
var chunks = await _searchService.SearchDocumentsAsync("SRS bakımı", maxResults: 5);

// Context'i genişletmek için bitişik chunk'ları dahil et
var expandedChunks = await _contextExpansion.ExpandContextAsync(chunks, contextWindow: 2);

// Artık expandedChunks başlık chunk'ını VE içerik chunk'larını içeriyor
foreach (var chunk in expandedChunks)
{
    Console.WriteLine($"Chunk {chunk.ChunkIndex}: {chunk.Content.Substring(0, 100)}...");
}
```

**Not:** Bu servis, RAG cevapları oluştururken `DocumentSearchService` tarafından otomatik olarak kullanılır. Sadece başlıkların bulunup karşılık gelen içeriğin bulunmadığı durumları önlemeye yardımcı olur.


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

