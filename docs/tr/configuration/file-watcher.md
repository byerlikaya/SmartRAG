---
layout: default
title: File Watcher Yapılandırması
description: İzlenen klasörlerden otomatik doküman indeksleme yapılandırması
lang: tr
redirect_from: /tr/configuration/file-watcher.html
---

# File Watcher Yapılandırması

SmartRAG, klasörleri yeni dokümanlar için otomatik olarak izleyebilir ve bunları otomatik olarak indeksleyebilir, böylece manuel doküman yüklemelerine gerek kalmaz.

## Genel Bakış

File Watcher özelliği SmartRAG'ın şunları yapmasına olanak tanır:
- Belirtilen klasörleri dosya değişiklikleri için izleme
- Yeni, değiştirilmiş veya silinmiş dosyaları otomatik olarak algılama
- Yeni dokümanları otomatik olarak yükleme ve indeksleme
- Birden fazla izlenen klasörü aynı anda destekleme

## Yapılandırma

### File Watcher'ı Etkinleştirme

`appsettings.json` dosyanıza aşağıdakini ekleyin:

```json
{
  "SmartRAG": {
    "EnableFileWatcher": true,
    "WatchedFolders": [
      {
        "FolderPath": "/dokumanlar/yolu",
        "AllowedExtensions": [".pdf", ".docx", ".txt"],
        "IncludeSubdirectories": true,
        "AutoUpload": true,
        "UserId": "system",
        "Language": "tr"
      }
    ]
  }
}
```

### Yapılandırma Özellikleri

#### EnableFileWatcher

- **Tip**: `bool`
- **Varsayılan**: `true`
- **Açıklama**: File Watcher işlevselliğini etkinleştirir veya devre dışı bırakır

#### WatchedFolders

- **Tip**: `List<WatchedFolderConfig>`
- **Varsayılan**: Boş liste
- **Açıklama**: İzlenecek klasör yapılandırmaları listesi

#### WatchedFolderConfig Özellikleri

| Özellik | Tip | Gerekli | Açıklama |
|---------|-----|---------|----------|
| `FolderPath` | `string` | Evet | İzlenecek klasörün mutlak veya göreli yolu |
| `AllowedExtensions` | `List<string>` | Hayır | İzin verilen dosya uzantıları listesi (örn., `[".pdf", ".docx"]`). Boşsa, tüm desteklenen dosya tipleri izin verilir |
| `IncludeSubdirectories` | `bool` | Hayır | Alt dizinlerin izlenip izlenmeyeceği (varsayılan: `true`) |
| `AutoUpload` | `bool` | Hayır | Yeni dosyaların otomatik olarak yüklenip yüklenmeyeceği (varsayılan: `true`) |
| `UserId` | `string` | Hayır | Doküman sahipliği için kullanıcı ID'si (varsayılan: `"system"`) |
| `Language` | `string` | Hayır | Doküman işleme için dil kodu (isteğe bağlı) |

## Programatik Yapılandırma

İzlenen klasörleri programatik olarak da yapılandırabilirsiniz:

```csharp
services.AddSmartRag(configuration, options =>
{
    options.EnableFileWatcher = true;
    options.WatchedFolders.Add(new WatchedFolderConfig
    {
        FolderPath = "/dokumanlar/yolu",
        AllowedExtensions = new List<string> { ".pdf", ".docx", ".txt" },
        IncludeSubdirectories = true,
        AutoUpload = true,
        UserId = "system",
        Language = "tr"
    });
});
```

## Başlatma

Servis sağlayıcıyı oluşturduktan sonra, dosya izleyicilerini başlatın:

```csharp
var serviceProvider = services.BuildServiceProvider();
await serviceProvider.InitializeSmartRagAsync();
```

Bu, yapılandırılmış tüm klasörleri otomatik olarak izlemeye başlayacaktır.

## Desteklenen Dosya Tipleri

File Watcher, SmartRAG'ın ayrıştırabileceği tüm dosya tiplerini destekler:
- **Dokümanlar**: `.pdf`, `.docx`, `.txt`, `.xlsx`
- **Görüntüler**: `.jpg`, `.png`, `.gif`, `.bmp` (OCR ile)
- **Ses**: `.mp3`, `.wav`, `.m4a`, `.flac` (transkripsiyon ile)

`AllowedExtensions` boşsa, tüm desteklenen dosya tipleri izin verilir.

## Güvenlik

File Watcher yerleşik güvenlik özellikleri içerir:
- **Path Traversal Önleme**: Tüm yollar dizin geçiş saldırılarını önlemek için temizlenir
- **Temel Dizin Doğrulama**: İzlenen klasörler uygulamanın temel dizini içinde olmalıdır
- **Dosya Uzantısı Doğrulama**: Sadece izin verilen dosya uzantıları işlenir

## Olaylar

File Watcher, dosya işlemleri için olaylar yükseltir:

```csharp
var fileWatcher = serviceProvider.GetRequiredService<IFileWatcherService>();

fileWatcher.FileCreated += (sender, e) =>
{
    Console.WriteLine($"Dosya oluşturuldu: {e.FileName} konum: {e.FilePath}");
};

fileWatcher.FileChanged += (sender, e) =>
{
    Console.WriteLine($"Dosya değiştirildi: {e.FileName} konum: {e.FilePath}");
};

fileWatcher.FileDeleted += (sender, e) =>
{
    Console.WriteLine($"Dosya silindi: {e.FileName} konum: {e.FilePath}");
};
```

## Manuel Kontrol

Dosya izleyicilerini manuel olarak da kontrol edebilirsiniz:

```csharp
var fileWatcher = serviceProvider.GetRequiredService<IFileWatcherService>();

// Bir klasörü izlemeye başla
await fileWatcher.StartWatchingAsync(new WatchedFolderConfig
{
    FolderPath = "/klasor/yolu",
    AutoUpload = true
});

// Bir klasörü izlemeyi durdur
await fileWatcher.StopWatchingAsync("/klasor/yolu");

// Tüm izleyicileri durdur
await fileWatcher.StopAllWatchingAsync();

// İzlenen klasörlerin listesini al
var watchedFolders = fileWatcher.GetWatchedFolders();
```

## Sorun Giderme

### Dosyalar İndekslenmiyor

Dosyalar otomatik olarak indekslenmiyorsa:
- Klasör yolunun var olduğunu ve erişilebilir olduğunu doğrulayın
- `AutoUpload`'un `true` olarak ayarlandığını kontrol edin
- Dosya uzantılarının `AllowedExtensions` listesinde olduğundan emin olun (veya liste boş)
- Hatalar için uygulama loglarını kontrol edin

### Path Traversal Hataları

Path traversal hatalarıyla karşılaşırsanız:
- Göreli yollar yerine mutlak yollar kullanın
- Yolların uygulamanın temel dizini içinde olduğundan emin olun
- Klasör yollarında `..` kullanmaktan kaçının

### Performans Hususları

- Çok sayıda klasörü veya büyük dizin ağaçlarını izlemek performansı etkileyebilir
- Daha iyi performans için `IncludeSubdirectories: false` kullanmayı düşünün
- `AllowedExtensions`'ı sadece gerekli dosya tipleriyle sınırlandırın

## İlgili Dokümantasyon

- [MCP Client Yapılandırması]({{ site.baseurl }}/tr/configuration/mcp-client/)
- [Başlangıç]({{ site.baseurl }}/tr/getting-started/)
- [API Referansı]({{ site.baseurl }}/tr/api-reference/)


