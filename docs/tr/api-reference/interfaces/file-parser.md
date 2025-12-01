---
layout: default
title: IFileParser
description: IFileParser arayüz dokümantasyonu
lang: tr
---

## IFileParser

**Amaç:** Belirli dosya formatlarını ayrıştırma stratejisi

**Namespace:** `SmartRAG.Interfaces.Parser.Strategies`

Özel dosya formatı ayrıştırıcıları sağlar.

#### Metodlar

##### ParseAsync

Bir dosyayı ayrıştırır ve içeriği çıkarır.

```csharp
Task<FileParserResult> ParseAsync(Stream fileStream, string fileName)
```

##### CanParse

Bu ayrıştırıcının verilen dosyayı işleyip işleyemeyeceğini kontrol eder.

```csharp
bool CanParse(string fileName, string contentType)
```

#### Yerleşik Uygulamalar

- `PdfFileParser` - PDF dokümanları
- `WordFileParser` - Word dokümanları (.docx)
- `ExcelFileParser` - Excel elektronik tabloları (.xlsx)
- `TextFileParser` - Düz metin dosyaları
- `ImageFileParser` - OCR ile görseller
- `AudioFileParser` - Ses transkripsiyon
- `DatabaseFileParser` - SQLite veritabanları

#### Özel Uygulama Örneği

```csharp
public class MarkdownFileParser : IFileParser
{
    public bool CanParse(string fileName, string contentType)
    {
        return fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
               contentType == "text/markdown";
    }
    
    public async Task<FileParserResult> ParseAsync(Stream fileStream, string fileName)
    {
        using var reader = new StreamReader(fileStream);
        var content = await reader.ReadToEndAsync();
        
        // Düz metin için markdown sözdizimini kaldır
        var plainText = StripMarkdownSyntax(content);
        
        return new FileParserResult
        {
            Content = plainText,
            Success = true
        };
    }
    
    private string StripMarkdownSyntax(string markdown)
    {
        // Markdown biçimlendirmesini kaldır
        return Regex.Replace(markdown, @"[#*`\[\]()]", "");
    }
}
```


## İlgili Arayüzler

- [Strateji Arayüzleri]({{ site.baseurl }}/tr/api-reference/strategies) - Tüm strateji arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

