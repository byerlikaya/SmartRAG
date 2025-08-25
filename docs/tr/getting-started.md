---
layout: default
title: Başlangıç
description: SmartRAG ile hızlı kurulum ve kurulum rehberi
lang: tr
---

# Başlangıç

SmartRAG ile hızlı kurulum ve kurulum rehberi.

## Kurulum

SmartRAG, NuGet paketi olarak mevcuttur ve birkaç farklı şekilde kurulabilir.

### .NET CLI kullanarak:

```bash
dotnet add package SmartRAG
```

### Package Manager kullanarak:

```bash
Install-Package SmartRAG
```

### Doğrudan paket referansı:

```xml
<PackageReference Include="SmartRAG" Version="1.0.3" />
```

## Temel Kurulum

Kurulduktan sonra, SmartRAG'ı uygulamanızda yapılandırabilirsiniz.

### Temel Yapılandırma:

```csharp
// SmartRAG'ı projenize ekleyin
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});
```

### Gelişmiş Yapılandırma:

```csharp
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Redis;
    options.ApiKey = "your-openai-key";
    options.ModelName = "text-embedding-ada-002";
    options.ChunkSize = 1000;
    options.ChunkOverlap = 200;
});
```

## Hızlı Başlangıç Örneği

İşte başlamanız için basit bir örnek:

```csharp
// SmartRAG servislerini DI container'ınıza ekleyin
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});

// Belge servisini kullanın
var documentService = serviceProvider.GetRequiredService<IDocumentService>();
var document = await documentService.UploadDocumentAsync(file);
```

## Sonraki Adımlar

Artık SmartRAG kuruldu ve yapılandırıldı, şunları yapabilirsiniz:

- Belgeleri yükleyin ve işleyin
- AI provider'lar kullanarak embedding'ler oluşturun
- Anlamsal arama sorguları gerçekleştirin
- Gelişmiş özellikleri keşfedin

## Yardıma mı ihtiyacınız var?

Herhangi bir sorunla karşılaşırsanız veya yardıma ihtiyacınız olursa:

- [Ana Dokümantasyona Dön]({{ site.baseurl }}/tr/) - Ana dokümantasyon
- [GitHub'da issue açın](https://github.com/byerlikaya/SmartRAG/issues) - GitHub Issues
- [Destek için iletişime geçin](mailto:b.yerlikaya@outlook.com) - E-posta desteği
