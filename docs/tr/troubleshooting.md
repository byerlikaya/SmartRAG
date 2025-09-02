---
layout: default
title: Sorun Giderme
nav_order: 5
---

# Sorun Giderme

Bu sayfa SmartRAG kullanırken karşılaşabileceğiniz yaygın sorunların çözümlerini sağlar.

<div class="troubleshooting-section">
## Yapılandırma Sorunları

### API Anahtarı Yapılandırması

<div class="problem-solution">
**Sorun**: AI veya depolama sağlayıcıları ile kimlik doğrulama hataları alıyorsunuz.

**Çözüm**: API anahtarlarınızın `appsettings.json` dosyasında doğru yapılandırıldığından emin olun:
</div>

```json
{
  "SmartRAG": {
    "AIProvider": "Anthropic",
    "StorageProvider": "Qdrant",
    "MaxChunkSize": 1000,
    "ChunkOverlap": 200
  },
  "Anthropic": {
    "ApiKey": "your-anthropic-api-key"
  },
  "Qdrant": {
    "ApiKey": "your-qdrant-api-key"
  }
}
```

Veya ortam değişkenlerini ayarlayın:

```bash
# Ortam değişkenlerini ayarlayın
export ANTHROPIC_API_KEY=your-anthropic-api-key
export QDRANT_API_KEY=your-qdrant-api-key
```

### Servis Kayıt Sorunları

<div class="problem-solution">
**Sorun**: Bağımlılık enjeksiyonu hataları alıyorsunuz.

**Çözüm**: SmartRAG servislerinin `Program.cs` dosyanızda doğru şekilde kaydedildiğinden emin olun:
</div>

```csharp
using SmartRAG.Extensions;

var builder = WebApplication.CreateBuilder(args);

// SmartRAG servislerini ekleyin
builder.Services.AddSmartRag(builder.Configuration);

var app = builder.Build();
app.UseSmartRag(builder.Configuration, StorageProvider.Qdrant, AIProvider.Anthropic);
app.Run();
```
</div>

<div class="troubleshooting-section">
## Belge Yükleme Sorunları

### Dosya Boyutu Sınırlamaları

<div class="problem-solution">
**Sorun**: Büyük belgeler yüklenemiyor veya işlenemiyor.

**Çözüm**: 
</div>

- Uygulamanızın dosya boyutu sınırlarını `appsettings.json` dosyasında kontrol edin
- Büyük belgeleri daha küçük parçalara bölmeyi düşünün
- İşleme için yeterli bellek olduğundan emin olun

### Desteklenmeyen Dosya Türleri

<div class="problem-solution">
**Sorun**: Belirli dosya formatları için hatalar alıyorsunuz.

**Çözüm**: SmartRAG yaygın metin formatlarını destekler. Dosyalarınızın desteklenen formatlarda olduğundan emin olun:
</div>

- PDF dosyaları
- Metin dosyaları (.txt)
- Word belgeleri (.docx)
- Markdown dosyaları (.md)
</div>

<div class="troubleshooting-section">
## Arama ve Alma Sorunları

### Arama Sonucu Yok

<div class="problem-solution">
**Sorun**: Arama sorguları sonuç döndürmüyor.

**Olası Çözümler**:
</div>

1. **Belge yüklemesini kontrol edin**: Belgelerin başarıyla yüklendiğinden emin olun
2. **Embedding'leri doğrulayın**: Embedding'lerin düzgün oluşturulduğunu kontrol edin
3. **Sorgu özgüllüğü**: Daha spesifik arama terimleri deneyin
4. **Depolama bağlantısı**: Depolama sağlayıcınızın erişilebilir olduğunu doğrulayın

### Düşük Arama Kalitesi

<div class="problem-solution">
**Sorun**: Arama sonuçları ilgili değil.

**Çözümler**:
</div>

- `MaxChunkSize` ve `ChunkOverlap` ayarlarını ayarlayın
- Daha spesifik arama sorguları kullanın
- Belgelerin düzgün formatlandığından emin olun
- Embedding'lerin güncel olduğunu kontrol edin
</div>

<div class="troubleshooting-section">
## Performans Sorunları

### Yavaş Belge İşleme

<div class="problem-solution">
**Sorun**: Belge yükleme ve işleme çok uzun sürüyor.

**Çözümler**:
</div>

- Chunk sayısını azaltmak için `MaxChunkSize`'ı artırın
- Daha güçlü bir AI sağlayıcısı kullanın
- Depolama sağlayıcı yapılandırmanızı optimize edin
- Uygulamanızda async operasyonları kullanmayı düşünün

### Bellek Sorunları

<div class="problem-solution">
**Sorun**: Uygulama işleme sırasında belleği tüketiyor.

**Çözümler**:
</div>

- Daha küçük chunk'lar oluşturmak için `MaxChunkSize`'ı azaltın
- Belgeleri toplu halde işleyin
- Bellek kullanımını izleyin ve optimize edin
- Büyük dosyalar için streaming operasyonları kullanmayı düşünün
</div>

<div class="troubleshooting-section">
## Depolama Sağlayıcısı Sorunları

### Qdrant Bağlantı Sorunları

<div class="problem-solution">
**Sorun**: Qdrant'a bağlanamıyorsunuz.

**Çözümler**:
</div>

- Qdrant API anahtarının doğru olduğunu doğrulayın
- Qdrant servisine ağ bağlantısını kontrol edin
- Qdrant servisinin çalıştığından ve erişilebilir olduğundan emin olun
- Güvenlik duvarı ayarlarını kontrol edin

### Redis Bağlantı Sorunları

<div class="problem-solution">
**Sorun**: Redis'e bağlanamıyorsunuz.

**Çözümler**:
</div>

- Redis bağlantı dizesini doğrulayın
- Redis sunucusunun çalıştığından emin olun
- Ağ bağlantısını kontrol edin
- `appsettings.json` dosyasındaki Redis yapılandırmasını doğrulayın

### SQLite Sorunları

<div class="problem-solution">
**Sorun**: SQLite veritabanı hataları.

**Çözümler**:
</div>

- Veritabanı dizini için dosya izinlerini kontrol edin
- Yeterli disk alanı olduğundan emin olun
- Veritabanı dosya yolunun doğru olduğunu doğrulayın
- Veritabanı bozulmasını kontrol edin
</div>

<div class="troubleshooting-section">
## AI Sağlayıcısı Sorunları

### Anthropic API Hataları

<div class="problem-solution">
**Sorun**: Anthropic API'den hatalar alıyorsunuz.

**Çözümler**:
</div>

- API anahtarının geçerli olduğunu ve yeterli kredisi olduğunu doğrulayın
- API hız sınırlarını kontrol edin
- Doğru API endpoint yapılandırmasından emin olun
- API kullanımını ve kotaları izleyin

### OpenAI API Hataları

<div class="problem-solution">
**Sorun**: OpenAI API'den hatalar alıyorsunuz.

**Çözümler**:
</div>

- API anahtarının geçerli olduğunu doğrulayın
- API hız sınırlarını ve kotaları kontrol edin
- Doğru model yapılandırmasından emin olun
- API kullanımını izleyin
</div>

<div class="troubleshooting-section">
## Test ve Hata Ayıklama

### Birim Testleri

<div class="problem-solution">
**Sorun**: SmartRAG bağımlılıkları nedeniyle testler başarısız oluyor.

**Çözüm**: Birim testlerinde SmartRAG servisleri için mocking kullanın:
</div>

```csharp
[Test]
public async Task TestDocumentUpload()
{
    // Arrange
    var mockDocumentService = new Mock<IDocumentService>();
    var mockSearchService = new Mock<IDocumentSearchService>();
    
    var controller = new DocumentsController(
        mockDocumentService.Object, 
        mockSearchService.Object, 
        Mock.Of<ILogger<DocumentsController>>());

    // Act & Assert
    // Test mantığınız burada
}
```

### Entegrasyon Testleri

<div class="problem-solution">
**Sorun**: Entegrasyon testleri başarısız oluyor.

**Çözüm**: Test yapılandırması kullanın ve doğru kurulumdan emin olun:
</div>

```csharp
[Test]
public async Task TestEndToEndWorkflow()
{
    // Arrange
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.test.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    var services = new ServiceCollection();
    services.AddSmartRag(configuration);
    
    var serviceProvider = services.BuildServiceProvider();
    
    // Act & Assert
    // Entegrasyon test mantığınız burada
}
```
</div>

<div class="troubleshooting-section">
## Yaygın Hata Mesajları

### "Document not found"
- Belge ID'sinin doğru olduğunu doğrulayın
- Belgenin başarıyla yüklendiğini kontrol edin
- Belgenin silinmediğinden emin olun

### "Storage provider not configured"
- Yapılandırmadaki `StorageProvider` ayarını doğrulayın
- Gerekli tüm depolama ayarlarının sağlandığından emin olun
- Servis kaydını kontrol edin

### "AI provider not configured"
- Yapılandırmadaki `AIProvider` ayarını doğrulayın
- Seçilen sağlayıcı için API anahtarının sağlandığından emin olun
- Servis kaydını kontrol edin

### "Invalid file format"
- Dosyanın desteklenen bir formatta olduğundan emin olun
- Dosya uzantısını ve içeriğini kontrol edin
- Dosyanın bozulmadığını doğrulayın
</div>

<div class="troubleshooting-section">
## Yardım Alma

Hala sorun yaşıyorsanız:

1. **Logları kontrol edin**: Detaylı hata mesajları için uygulama loglarını inceleyin
2. **Yapılandırmayı doğrulayın**: Tüm yapılandırma ayarlarını tekrar kontrol edin
3. **Minimal kurulumla test edin**: Önce basit bir yapılandırma ile deneyin
4. **Bağımlılıkları kontrol edin**: Gerekli tüm servislerin çalıştığından emin olun
5. **Dokümantasyonu inceleyin**: Rehberlik için diğer dokümantasyon sayfalarını kontrol edin

Ek destek için lütfen projenin GitHub deposuna başvurun veya sorununuzla ilgili detaylı bilgi ile bir issue oluşturun.
</div>
