---
layout: default
title: Gelişmiş Örnekler
description: Gelişmiş özellikler ve özelleştirme örnekleri
lang: tr
---

## Gelişmiş Örnekler

### Konuşma Bağlamı Yönetimi

```csharp
// İlk soru
var q1 = await _searchService.QueryIntelligenceAsync(
    "Şirketin iade politikası nedir?"
);

// Takip - AI bağlamı hatırlar
var q2 = await _searchService.QueryIntelligenceAsync(
    "Uluslararası siparişler ne olacak?"
);

// Başka bir takip - tam bağlamı korur
var q3 = await _searchService.QueryIntelligenceAsync(
    "İade nasıl başlatırım?"
);

// Yeni konuşma başlat
var newConv = await _searchService.QueryIntelligenceAsync(
    "Kargo hakkında konuşalım",
    startNewConversation: true
);
```

### Toplu Doküman İşleme

```csharp
// Birden fazla dokümanı aynı anda yükle
var files = new List<(Stream, string, string)>
{
    (File.OpenRead("rapor1.pdf"), "rapor1.pdf", "application/pdf"),
    (File.OpenRead("rapor2.docx"), "rapor2.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
    (File.OpenRead("rapor3.xlsx"), "rapor3.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
};

var documents = await _documentService.UploadDocumentsAsync(files, "user-123");

Console.WriteLine($"Yüklenen doküman sayısı: {documents.Count}");
```

### Özel SQL Çalıştırma

```csharp
// Belirli bir veritabanında özel SQL sorgusu çalıştır
var result = await _databaseService.ExecuteQueryAsync(
    "Server=localhost;Database=Sales;Trusted_Connection=true;",
    "SELECT TOP 10 CustomerID, CompanyName, TotalOrders FROM CustomerSummary ORDER BY TotalOrders DESC",
    DatabaseType.SqlServer,
    maxRows: 10
);

Console.WriteLine($"SQL Sonucu: {result}");
```

### Depolama İstatistikleri

```csharp
// Depolama durumunu kontrol et
var stats = await _documentService.GetStorageStatisticsAsync();

Console.WriteLine($"Toplam Doküman: {stats["TotalDocuments"]}");
Console.WriteLine($"Toplam Parça: {stats["TotalChunks"]}");
Console.WriteLine($"Depolama Boyutu: {stats["StorageSizeMB"]} MB");
Console.WriteLine($"Son Güncelleme: {stats["LastUpdated"]}");
```

### Embedding'leri Yeniden Oluştur

```csharp
// AI provider değiştikten sonra embedding'leri yenile
var success = await _documentService.RegenerateAllEmbeddingsAsync();

if (success)
{
    Console.WriteLine("Tüm embedding'ler başarıyla yenilendi");
}
else
{
    Console.WriteLine("Embedding yenileme başarısız");
}
```

## İlgili Örnekler

- [Örnekler Ana Sayfası]({{ site.baseurl }}/tr/examples) - Örnekler kategorilerine dön
