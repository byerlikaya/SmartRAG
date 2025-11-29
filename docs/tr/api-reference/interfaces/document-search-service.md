---
layout: default
title: IDocumentSearchService
description: IDocumentSearchService arayüz dokümantasyonu
lang: tr
---

## IDocumentSearchService

**Amaç:** RAG pipeline ve konuşma yönetimi ile AI destekli akıllı sorgu işleme

**Namespace:** `SmartRAG.Interfaces.Document`

### Metodlar

#### QueryIntelligenceAsync

Birleşik akıllı sorgu işleme ile RAG ve otomatik oturum yönetimi. Smart Hybrid yönlendirme kullanarak tek sorguda veritabanları, belgeler, görüntüler (OCR) ve ses dosyalarını (transkript) arar.

**Akıllı Hibrit Yönlendirme:**
- **Yüksek Güven (>0.7) + Veritabanı Sorguları**: Sadece veritabanı sorgusu çalıştırır
- **Yüksek Güven (>0.7) + Veritabanı Sorgusu Yok**: Sadece belge sorgusu çalıştırır
- **Orta Güven (0.3-0.7)**: Hem veritabanı hem belge sorgularını çalıştırır, sonuçları birleştirir
- **Düşük Güven (<0.3)**: Sadece belge sorgusu çalıştırır (fallback)

```csharp
Task<RagResponse> QueryIntelligenceAsync(
    string query, 
    int maxResults = 5, 
    bool startNewConversation = false,
    SearchOptions? options = null
)
```

**Parametreler:**
- `query` (string): Kullanıcının sorusu veya sorgusu
- `maxResults` (int): Alınacak maksimum doküman parçası sayısı (varsayılan: 5)
- `startNewConversation` (bool): Yeni bir konuşma oturumu başlat (varsayılan: false)
- `options` (SearchOptions?): Global yapılandırmayı geçersiz kılmak için isteğe bağlı arama seçenekleri (varsayılan: null)

**Döndürür:** Tüm mevcut veri kaynaklarından (veritabanları, belgeler, görüntüler, ses) AI cevabı, kaynaklar ve metadata içeren `RagResponse`

**Örnek:**

```csharp
// Tüm veri kaynaklarında birleşik sorgu
var response = await _searchService.QueryIntelligenceAsync(
    "En iyi müşterileri ve son geri bildirimlerini göster", 
    maxResults: 5
);

Console.WriteLine(response.Answer);
// Kaynaklar hem veritabanı hem belge kaynaklarını içerir
foreach (var source in response.Sources)
{
    Console.WriteLine($"Kaynak: {source.FileName}");
}
```

**SearchOptions Kullanımı:**

```csharp
// Sadece veritabanı araması
var dbOptions = new SearchOptions
{
    EnableDatabaseSearch = true,
    EnableDocumentSearch = false,
    EnableAudioSearch = false,
    EnableImageSearch = false
};

var dbResponse = await _searchService.QueryIntelligenceAsync(
    "En iyi müşterileri göster",
    maxResults: 5,
    options: dbOptions
);

// Sadece ses araması
var audioOptions = new SearchOptions
{
    EnableDatabaseSearch = false,
    EnableDocumentSearch = false,
    EnableAudioSearch = true,
    EnableImageSearch = false,
    PreferredLanguage = "tr"
};

var audioResponse = await _searchService.QueryIntelligenceAsync(
    "Toplantıda ne konuşuldu?",
    maxResults: 5,
    options: audioOptions
);
```

**Bayrak Tabanlı Filtreleme (Sorgu String Ayrıştırma):**

Sorgu string'lerinden bayrakları ayrıştırarak hızlı arama tipi seçimi yapabilirsiniz:

```csharp
// Sorgu string'inden bayrakları ayrıştır
string userQuery = "-db En iyi müşterileri göster";
var searchOptions = ParseSearchOptions(userQuery, out string cleanQuery);

// cleanQuery = "En iyi müşterileri göster"
// searchOptions.EnableDatabaseSearch = true
// Diğerleri = false

var response = await _searchService.QueryIntelligenceAsync(
    cleanQuery,
    maxResults: 5,
    options: searchOptions
);
```

**Mevcut Bayraklar:**
- `-db`: Sadece veritabanı araması
- `-d`: Sadece doküman (metin) araması
- `-a`: Sadece ses araması
- `-i`: Sadece görüntü araması
- Bayraklar birleştirilebilir (örn: `-db -a` = veritabanı + ses araması)

**Not:** Veritabanı coordinator yapılandırılmamışsa, metod otomatik olarak sadece belge aramasına geri döner, geriye dönük uyumluluğu korur.

#### SearchDocumentsAsync

AI cevabı üretmeden dokümanları anlamsal olarak arayın.

```csharp
Task<List<DocumentChunk>> SearchDocumentsAsync(
    string query, 
    int maxResults = 5
)
```

**Parametreler:**
- `query` (string): Arama sorgusu
- `maxResults` (int): Döndürülecek maksimum parça sayısı (varsayılan: 5)

**Döndürür:** İlgili doküman parçalarıyla `List<DocumentChunk>`

**Örnek:**

```csharp
var chunks = await _searchService.SearchDocumentsAsync("makine öğrenimi", maxResults: 10);

foreach (var chunk in chunks)
{
    Console.WriteLine($"Skor: {chunk.RelevanceScore}, İçerik: {chunk.Content}");
}
```

#### GenerateRagAnswerAsync (Kullanımdan Kaldırıldı)

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> v3.0.0'da Kullanımdan Kaldırıldı</h4>
    <p class="mb-0">
        Yerine <code>QueryIntelligenceAsync</code> kullanın. Bu metod v4.0.0'da kaldırılacak.
        Geriye dönük uyumluluk için sağlanan eski metod.
    </p>
                    </div>

```csharp
[Obsolete("Yerine QueryIntelligenceAsync kullanın")]
Task<RagResponse> GenerateRagAnswerAsync(
    string query, 
    int maxResults = 5, 
    bool startNewConversation = false
)
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

