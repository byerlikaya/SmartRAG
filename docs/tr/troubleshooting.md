---
layout: default
title: Sorun Giderme
description: SmartRAG ile ilgili yaygın sorunlar ve çözümler
lang: tr
---

# Sorun Giderme

SmartRAG ile ilgili yaygın sorunlar ve çözümler.

## Yaygın Sorunlar

### Derleme Hataları

#### CS0246: 'SmartRAG' türü veya namespace adı bulunamadı

**Sorun**: SmartRAG paketi düzgün referans edilmemiş.

**Çözüm**: 
1. Paketin kurulu olduğundan emin olun:
   ```bash
   dotnet add package SmartRAG
   ```
2. `.csproj` dosyanızda referansın olduğunu kontrol edin:
   ```xml
   <PackageReference Include="SmartRAG" Version="1.0.3" />
   ```
3. Paketleri geri yükleyin:
   ```bash
   dotnet restore
   ```

#### CS1061: 'IServiceCollection' 'AddSmartRAG' tanımını içermiyor

**Sorun**: SmartRAG uzantı metodu mevcut değil.

**Çözüm**:
1. Using ifadesini ekleyin:
   ```csharp
   using SmartRAG.Extensions;
   ```
2. Paketin düzgün kurulu ve referans edildiğinden emin olun.

### Çalışma Zamanı Hataları

#### InvalidOperationException: AI provider yapılandırılmamış

**Sorun**: SmartRAG bir AI provider ile düzgün yapılandırılmamış.

**Çözüm**:
```csharp
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic; // veya OpenAI, AzureOpenAI, vb.
    options.ApiKey = "your-api-key";
    options.StorageProvider = StorageProvider.Qdrant; // veya Redis, SQLite, vb.
});
```

#### UnauthorizedAccessException: Geçersiz API anahtarı

**Sorun**: API anahtarı geçersiz veya süresi dolmuş.

**Çözüm**:
1. API anahtarınızın doğru olduğunu doğrulayın
2. API anahtarının süresi dolup dolmadığını kontrol edin
3. API anahtarının gerekli izinlere sahip olduğundan emin olun
4. OpenAI için anahtarın doğru organizasyondan geldiğini doğrulayın

#### ConnectionException: Depolama provider'ına bağlanılamıyor

**Sorun**: Yapılandırılan depolama provider'ına bağlanılamıyor.

**Çözüm**:
1. **Qdrant**: Qdrant'ın çalıştığını ve erişilebilir olduğunu kontrol edin
   ```bash
   curl http://localhost:6333/collections
   ```
2. **Redis**: Redis bağlantısını doğrulayın
   ```bash
   redis-cli ping
   ```
3. **SQLite**: Dosya izinlerini ve yolu kontrol edin
4. **Ağ**: Güvenlik duvarı ayarlarını ve ağ bağlantısını doğrulayın

### Performans Sorunları

#### Yavaş Belge İşleme

**Sorun**: Belge işleme çok uzun sürüyor.

**Çözüm**:
1. Parça boyutunu azaltın:
   ```csharp
   options.ChunkSize = 500; // Varsayılan 1000
   ```
2. Daha küçük örtüşme kullanın:
   ```csharp
   options.ChunkOverlap = 100; // Varsayılan 200
   ```
3. Daha hızlı depolama provider'ları kullanmayı düşünün (SQLite yerine Redis)
4. Sık erişilen belgeler için önbellek uygulayın

#### Yüksek Bellek Kullanımı

**Sorun**: Uygulama çok fazla bellek tüketiyor.

**Çözüm**:
1. Belgeleri daha küçük gruplar halinde işleyin
2. Büyük dosyalar için akış uygulayın
3. Bellek verimli depolama provider'ları kullanın
4. Kaynakları düzgün izleyin ve dispose edin

### Yapılandırma Sorunları

#### Eksik Yapılandırma Değerleri

**Sorun**: Gerekli yapılandırma değerleri eksik.

**Çözüm**:
1. `appsettings.json`'u kontrol edin:
   ```json
   {
     "SmartRAG": {
       "AIProvider": "Anthropic",
       "StorageProvider": "Qdrant",
       "ApiKey": "your-api-key"
     }
   }
   ```
2. Ortam değişkenlerini kullanın:
   ```bash
   export SMARTRAG_API_KEY="your-api-key"
   export SMARTRAG_AI_PROVIDER="Anthropic"
   ```

#### Yanlış Provider Yapılandırması

**Sorun**: Provider'a özel yapılandırma yanlış.

**Çözüm**:
1. **Qdrant**:
   ```csharp
   options.QdrantUrl = "http://localhost:6333";
   options.CollectionName = "smartrag_documents";
   ```
2. **Redis**:
   ```csharp
   options.RedisConnectionString = "localhost:6379";
   options.DatabaseId = 0;
   ```
3. **SQLite**:
   ```csharp
   options.ConnectionString = "Data Source=smartrag.db";
   ```

## Hata Ayıklama

### Günlük Kaydını Etkinleştirin

```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

### Servis Kaydını Kontrol Edin

```csharp
// Program.cs veya Startup.cs'de
var serviceProvider = services.BuildServiceProvider();

// Servislerin kayıtlı olup olmadığını kontrol edin
var documentService = serviceProvider.GetService<IDocumentService>();
if (documentService == null)
{
    Console.WriteLine("IDocumentService kayıtlı değil!");
}

var aiService = serviceProvider.GetService<IAIService>();
if (aiService == null)
{
    Console.WriteLine("IAIService kayıtlı değil!");
}
```

### Yapılandırmayı Doğrulayın

```csharp
public class ConfigurationValidator
{
    public static bool ValidateSmartRagOptions(SmartRagOptions options)
    {
        if (string.IsNullOrEmpty(options.ApiKey))
        {
            throw new ArgumentException("API anahtarı gerekli");
        }
        
        if (options.ChunkSize <= 0)
        {
            throw new ArgumentException("Parça boyutu pozitif olmalı");
        }
        
        if (options.ChunkOverlap < 0)
        {
            throw new ArgumentException("Parça örtüşmesi negatif olamaz");
        }
        
        return true;
    }
}
```

## Test Etme

### Birim Test Kurulumu

```csharp
[TestFixture]
public class SmartRAGTests
{
    private ServiceCollection _services;
    private ServiceProvider _serviceProvider;
    
    [SetUp]
    public void Setup()
    {
        _services = new ServiceCollection();
        
        // Test yapılandırmasını ekleyin
        _services.AddSmartRAG(options =>
        {
            options.AIProvider = AIProvider.InMemory; // Test için in-memory kullanın
            options.StorageProvider = StorageProvider.InMemory;
            options.ApiKey = "test-key";
        });
        
        _serviceProvider = _services.BuildServiceProvider();
    }
    
    [Test]
    public async Task UploadDocument_ValidFile_ReturnsDocument()
    {
        // Arrange
        var documentService = _serviceProvider.GetRequiredService<IDocumentService>();
        var file = CreateTestFile();
        
        // Act
        var result = await documentService.UploadDocumentAsync(file);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotEmpty(result.Id);
    }
}
```

### Entegrasyon Test Kurulumu

```csharp
[TestFixture]
public class SmartRAGIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    
    [SetUp]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["SmartRAG:AIProvider"] = "InMemory",
                        ["SmartRAG:StorageProvider"] = "InMemory",
                        ["SmartRAG:ApiKey"] = "test-key"
                    });
                });
            });
    }
    
    [Test]
    public async Task UploadEndpoint_ValidFile_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        var file = CreateTestFile();
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(file.OpenReadStream()), "file", file.FileName);
        
        // Act
        var response = await client.PostAsync("/api/documents/upload", content);
        
        // Assert
        Assert.IsTrue(response.IsSuccessStatusCode);
    }
}
```

## Yardım Alma

### Dokümantasyonu Kontrol Edin

- [Başlangıç]({{ site.baseurl }}/tr/getting-started) - Temel kurulum rehberi
- [Yapılandırma]({{ site.baseurl }}/tr/configuration) - Yapılandırma seçenekleri
- [API Referansı]({{ site.baseurl }}/tr/api-reference) - API dokümantasyonu

### Topluluk Desteği

- [GitHub Issues](https://github.com/byerlikaya/SmartRAG/issues) - Hataları bildirin ve özellik isteyin
- [GitHub Discussions](https://github.com/byerlikaya/SmartRAG/discussions) - Sorular sorun ve çözümleri paylaşın

### Destek İletişimi

- **E-posta**: [b.yerlikaya@outlook.com](mailto:b.yerlikaya@outlook.com)
- **Yanıt Süresi**: Genellikle 24 saat içinde

### Bilgi Sağlayın

Bir sorun bildirirken lütfen şunları dahil edin:

1. **Ortam**: .NET sürümü, İşletim Sistemi, SmartRAG sürümü
2. **Yapılandırma**: SmartRAG yapılandırmanız
3. **Hata Detayları**: Tam hata mesajı ve stack trace
4. **Yeniden Üretme Adımları**: Sorunu yeniden üretmek için net adımlar
5. **Beklenen vs Gerçek**: Ne beklediğiniz vs ne olduğu

## Önleme

### En İyi Uygulamalar

1. **Yapılandırmayı her zaman doğrulayın** uygulamayı başlatmadan önce
2. **Farklı dağıtım ortamları için ortam özel ayarları** kullanın
3. **Uygun hata yönetimi ve günlük kaydı** uygulayın
4. **Büyük dosyaları işlemeden önce küçük belgelerle test edin**
5. **Üretimde performans metriklerini izleyin**
6. **Bağımlılıkları en son kararlı sürümlere güncel tutun**

### Yapılandırma Doğrulama

```csharp
public static class SmartRAGConfigurationValidator
{
    public static void ValidateConfiguration(SmartRagOptions options)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrEmpty(options.ApiKey))
            errors.Add("API anahtarı gerekli");
            
        if (options.ChunkSize <= 0)
            errors.Add("Parça boyutu pozitif olmalı");
            
        if (options.ChunkOverlap < 0)
            errors.Add("Parça örtüşmesi negatif olamaz");
            
        if (options.ChunkOverlap >= options.ChunkSize)
            errors.Add("Parça örtüşmesi parça boyutundan küçük olmalı");
        
        if (errors.Any())
        {
            throw new InvalidOperationException(
                $"SmartRAG yapılandırma doğrulaması başarısız: {string.Join(", ", errors)}");
        }
    }
}
```

## Yardıma mı ihtiyacınız var?

Hala sorun yaşıyorsanız:

- [Ana Dokümantasyona Dön]({{ site.baseurl }}/tr/) - Ana dokümantasyon
- [GitHub'da issue açın](https://github.com/byerlikaya/SmartRAG/issues) - GitHub Issues
- [Destek için iletişime geçin](mailto:b.yerlikaya@outlook.com) - E-posta desteği
