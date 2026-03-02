---
layout: default
title: Versiyon Geçmişi
description: SmartRAG için eksiksiz versiyon geçmişi
lang: tr
---

## Versiyon Geçmişi

SmartRAG'deki tüm sürümler ve değişiklikler burada belgelenmiştir.

<div class="accordion mt-4" id="versionAccordion">
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion400">
            <button class="accordion-button" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion400" aria-expanded="true" aria-controls="collapseversion400">
                <strong>v4.0.0</strong> - 2026-02-06
            </button>
        </h2>
        <div id="collapseversion400" class="accordion-collapse collapse show" aria-labelledby="headingversion400" >
            <div class="accordion-body">
{% capture version_content %}

### Framework Göçü ve Yerleşik Dashboard

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> MAJOR Sürüm</h4>
    <p class="mb-0">
        Bu sürüm SmartRAG'ı .NET 6'ya taşıyor, Dashboard projesini ana pakete birleştiriyor ve v3 serisindeki deprecatation planını tamamlıyor (eski RAG ve orkestrasyon API'leri kaldırıldı).
    </p>
</div>

### ⚠️ Kırıcı Değişiklikler

- **.NET Standard → .NET 6**  
  - Hedef framework .NET Standard 2.1'den **.NET 6**'ya güncellendi  
  - Projeler artık **.NET 6 veya üzerini** hedeflemelidir  
  - .NET Core 3.0, .NET 5 ve .NET Standard 2.1 artık desteklenmiyor

- **Kaldırılan Eski API'ler**  
  - `IDocumentSearchService.GenerateRagAnswerAsync(...)` → yerine `QueryIntelligenceAsync(...)` kullanın  
  - `IRagAnswerGeneratorService.GenerateBasicRagAnswerAsync(string, int, ...)` → yerine `GenerateBasicRagAnswerAsync(GenerateRagAnswerRequest)` kullanın  
  - `IQueryStrategyExecutorService` için ayrı parametreli overload'lar → `QueryStrategyRequest` overload'larını kullanın  
  - `IDocumentService.UploadDocumentAsync(Stream, string, ...)` → yerine `UploadDocumentAsync(UploadDocumentRequest)` kullanın  
  - `IMultiDatabaseQueryCoordinator.AnalyzeQueryIntentAsync(...)` → yerine `IQueryIntentAnalyzer.AnalyzeQueryIntentAsync(...)` kullanın

- **Proje Yapısı**  
  - `SmartRAG.Dashboard` daha önce solution içinde ayrı bir projeydi (hiçbir zaman NuGet paketi olmadı)  
  - Artık **SmartRAG** paketi içine birleştirildi – `SmartRAG.Dashboard` için `<ProjectReference>` kullanıyorsanız kaldırın

### ✨ Eklenenler

- **NuGet Paketi İçinde Yerleşik Dashboard**
  - Doküman yönetimi ve chat arayüzü artık doğrudan SmartRAG paketi ile geliyor  
  - Kullanım API'si değişmedi:

```csharp
builder.Services.AddSmartRag(builder.Configuration);
builder.Services.AddSmartRagDashboard(options => { options.Path = "/smartrag"; });

app.UseSmartRagDashboard("/smartrag");
app.MapSmartRagDashboard("/smartrag");
```

- **Health Check Servisi**
  - Kütüphane seviyesinde sağlık raporlamak için yeni `IHealthCheckService` ve `HealthCheckResult`/`HealthStatus` modelleri  
  - Dashboard ve örnek API'lerin SmartRAG sağlık durumunu tutarlı ve test edilebilir şekilde göstermesi için tasarlandı

### 🔧 İyileştirmeler

- **Query Source Handler Mimarisi**
  - Doküman, veritabanı ve MCP kaynakları için yeni handler soyutlaması (`IQuerySourceHandler`, `QuerySourceHandlerRequest`)  
  - `DocumentRagService`, `DocumentRagSourceHandler`, `DatabaseQuerySourceHandler`, `McpSearchHandler` gibi yeni servisler  
  - “Sorgu nasıl çalıştırılır?” ile “veri nereden gelir?” sorularını ayırarak genişletilebilirliği artırır

- **Daha Güvenli ve Dayanıklı AI & Depolama Entegrasyonu**
  - Tüm AI provider'ları ve depolama servisleri .NET 6 için güncellendi (global using'ler, nullable reference types, file‑scoped namespace'ler)  
  - AI, veritabanı, arama, parser ve MCP servisleri boyunca `CancellationToken` akışı iyileştirildi  
  - Şema-farkındalıklı SQL üretimi rafine edildi (PostgreSQL identifier quoting için opsiyonel şema parametresi, kolon listeleme düzeltmeleri)

- **Güvenlik Sertleştirmesi**
  - CodeQL analizlerine dayalı düzeltmeler:
    - SQL prompt'ları ve şema kolon işleme kuralları  
    - Web/Dashboard senaryoları için path ve URL sanitizasyonu  
    - MCP istek doğrulaması ve yanıt işleme için daha güvenli kontroller  
  - Loglama, hassas verileri sızdırmayacak şekilde sıkılaştırıldı ancak tanılama için yeterince bilgi bırakıldı

- **LoggerMessage ile Merkezi Loglama**
  - Kalan tüm doğrudan `_logger.LogX(...)` kullanımları merkezi `*LogMessages` delegate'lerine taşındı  
  - `DatabaseLogMessages` ve `StartupLogMessages` gibi yeni sınıflar, veritabanı ve başlangıç log'larını proje genelindeki log standardı ile hizalar

### 📝 Notlar

- **Taşınma Yardımı**: Kod örnekleri için [Taşınma Kılavuzları]({{ site.baseurl }}/tr/changelog/migration-guides#v3xten-v400a-taşınma) ve [Kullanımdan Kaldırma Bildirimleri]({{ site.baseurl }}/tr/changelog/deprecation) sayfalarına bakın  
- **Changelog Kapsamı**: Burada yalnızca `src/SmartRAG/` altındaki ana kütüphane değişiklikleri listelenir (examples projeleri bilinçli olarak hariç tutulur)

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion390">
            <button class="accordion-button" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion390" aria-expanded="true" aria-controls="collapseversion390">
                <strong>v3.9.0</strong> - 2026-02-05
            </button>
        </h2>
        <div id="collapseversion390" class="accordion-collapse collapse show" aria-labelledby="headingversion390" >
            <div class="accordion-body">
{% capture version_content %}

### Konuşma Zaman Damgaları, RAG İyileştirmeleri, Qdrant 1.16

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> MINOR Sürüm</h4>
    <p class="mb-0">
        Bu sürüm konuşma zaman damgaları ve kaynakları, büyük RAG arama iyileştirmeleri, yinelenen yükleme önleme, Whisper bootstrap, MCP isteğe bağlı bağlantı ve Qdrant 1.16.1 uyumluluğu ekler.
        IStorageFactory ve IConversationRepository için kırıcı değişiklikler içerir.
    </p>
</div>

### ✨ Eklenenler

- **Konuşma Zaman Damgaları ve Kaynaklar**: GetSessionTimestampsAsync, AppendSourcesForTurnAsync, GetSourcesForSessionAsync, GetAllSessionIdsAsync
- **Açık Oturum RAG Overload**: sessionId ve conversationHistory ile QueryIntelligenceAsync
- **Yinelenen Yükleme Önleme**: Hash tabanlı atlama, DocumentSkippedException
- **Whisper Native Bootstrap**: Başlangıç başlatması için WhisperNativeBootstrap
- **MCP İsteğe Bağlı**: MCP sunucuları yalnızca -mcp etiketi kullanıldığında bağlanır

### 🔧 İyileştirmeler

- **Doküman RAG Arama**: Dosya adı erken dönüşü, phrase/morfolojik chunk önceliklendirme, dosya adı eşleştirmeli relevance skorlama, extraction retry modu
- **Takip Soruları**: Daha iyi konuşma context işleme
- **PDF ve OCR**: Türkçe encoding, para birimi pattern'leri
- **Storage Factory**: Scoped çözümleme için GetCurrentRepository(IServiceProvider)
- **Qdrant**: 1.16.1 API uyumluluğu, IQdrantCacheManager kaldırıldı
- **NuGet**: Qdrant.Client, StackExchange.Redis, MySql.Data, itext, EPPlus, PDFtoImage güncellendi

### ⚠️ Kırıcı Değişiklikler

- IStorageFactory: GetCurrentRepository(IServiceProvider scopedProvider)
- IConversationRepository: AppendSourcesForTurnAsync, GetSourcesForSessionAsync, GetAllSessionIdsAsync zorunlu
- IQdrantCacheManager: Kaldırıldı

### 📝 Notlar

- Kırıcı değişiklikler için migrasyon rehberine bakın
- 0 hata, 0 uyarı build politikası korunur

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion381">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion381" aria-expanded="false" aria-controls="collapseversion381">
                <strong>v3.8.1</strong> - 2026-01-28
            </button>
        </h2>
        <div id="collapseversion381" class="accordion-collapse collapse" aria-labelledby="headingversion381" >
            <div class="accordion-body">
{% capture version_content %}

### Schema RAG İyileştirmeleri ve Kod Temizliği

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> PATCH Sürüm</h4>
    <p class="mb-0">
        Bu sürüm, 3.8.0 Schema RAG implementasyonunun üzerine ek şema iyileştirmeleri, iç refactoring'ler ve kod temizliği getirir.
        Public API değişmeden davranış geriye dönük uyumlu kalır.
    </p>
</div>

### 🔧 İyileştirmeler

#### Şema Servislerinde Cancellation Desteği
- Şema migrasyonu ve ilişkili servislerde `CancellationToken` akışı iyileştirildi
- Daha sağlam async akışlar ve daha güvenli iptal davranışı

#### Kod Temizliği ve Bakım Kolaylığı
- Kullanılmayan SQL prompt ve dialect helper'ları kaldırıldı
- Doküman skorlama ve strateji helper'ları sadeleştirildi
- Context expansion ve Qdrant arama helper'ları temizlendi
- Kullanılmayan dosya izleyici event'leri ve konuşma helper'ları kaldırıldı

#### Logging ve Tanılama
- Repository log mesajları sadeleştirildi
- Veritabanı sorgu yürütücüsündeki gürültülü log'lar azaltıldı

### 📝 Notlar

- Geriye dönük uyumlu patch sürümü
- 0 hata, 0 uyarı build politikası korunur
- 3.8.0 Schema RAG implementasyonundaki tüm özellikleri içerir

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion380">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion380" aria-expanded="false" aria-controls="collapseversion380">
                <strong>v3.8.0</strong> - 2026-01-26
            </button>
        </h2>
        <div id="collapseversion380" class="accordion-collapse collapse" aria-labelledby="headingversion380" >
            <div class="accordion-body">
{% capture version_content %}

### Schema RAG Implementasyonu

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> MINOR Sürüm</h4>
    <p class="mb-0">
        Bu sürüm Schema RAG pattern'ini implemente eder, vektörleştirilmiş chunk'lar olarak saklanan veritabanı şema bilgilerinin semantik araması ile akıllı SQL üretimini sağlar.
        Tüm değişiklikler geriye dönük uyumludur.
    </p>
</div>

### ✨ Eklendi

#### Schema RAG Implementasyonu
- **Otomatik Şema Migrasyonu**: Veritabanı şemalarını vektörleştirilmiş chunk'lara migrate etmek için yeni servis
- **Şema Chunk Servisi**: Veritabanı şemalarını embedding'lerle vektörleştirilmiş doküman chunk'larına dönüştürür
- **Semantik Şema Arama**: Daha iyi SQL üretimi için RAG chunk'larından şema bilgisi alınması
- **Şema Metadata**: Metadata ile saklanan chunk'lar (databaseId, databaseName, documentType: "Schema")
- **Migrasyon Desteği**: Tüm şemaları veya tek tek veritabanı şemalarını migrate etme
- **Şema Güncellemeleri**: Güncelleme işlevselliği (eski chunk'ları sil ve yeni oluştur)
- **Semantik Anahtar Kelimeler**: Daha iyi sorgu eşleştirmesi için tablo ve kolon isimlerinden çıkarım
- **PostgreSQL Desteği**: Identifier'lar için çift tırnak ile özel formatlama
- **Tablo Sınıflandırması**: Satır sayısına göre tablo tipi sınıflandırması (TRANSACTIONAL, LOOKUP, MASTER)
- **Foreign Key Dokümantasyonu**: Chunk'larda kapsamlı foreign key ilişki dokümantasyonu
- **Eklenen Dosyalar**:
  - `src/SmartRAG/Interfaces/Database/ISchemaMigrationService.cs` - Şema migrasyon interface'i
  - `src/SmartRAG/Services/Database/SchemaMigrationService.cs` - Şema migrasyon servisi
  - `src/SmartRAG/Services/Database/SchemaChunkService.cs` - Şema chunk dönüştürme servisi

### 🔧 İyileştirildi

#### SQL Sorgu Üretimi
- **Şema Chunk Entegrasyonu**: Daha iyi doğruluk için şema chunk entegrasyonu ile geliştirildi
- **RAG Pattern**: Şema bilgileri RAG chunk'larından alınıyor (birincil kaynak)
- **Fallback Desteği**: Şema chunk'ları mevcut değilse DatabaseSchemaInfo fallback
- **Geliştirilmiş Prompt'lar**: Chunk'lardan şema context'i ile geliştirilmiş prompt oluşturma
- **Dosyalar Güncellendi**:
  - `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - Şema chunk entegrasyonu ile geliştirildi
  - `src/SmartRAG/Services/Database/Prompts/SqlPromptBuilder.cs` - İyileştirilmiş prompt yapısı

#### Veritabanı Bağlantı Yöneticisi
- **Şema Migrasyon Entegrasyonu**: Opsiyonel şema migrasyon servisi entegrasyonu eklendi
- **Dosyalar Güncellendi**:
  - `src/SmartRAG/Services/Database/DatabaseConnectionManager.cs` - Şema migrasyon desteği eklendi

#### Sonuç Birleştirici
- **Geliştirilmiş Birleştirme**: Daha iyi sonuç birleştirmesi için geliştirilmiş birleştirme mantığı
- **Dosyalar Güncellendi**:
  - `src/SmartRAG/Services/Database/ResultMerger.cs` - Geliştirilmiş birleştirme mantığı

#### Doküman Doğrulayıcı
- **Şema Doküman Doğrulaması**: Şema dokümanları için geliştirilmiş doğrulama
- **Dosyalar Güncellendi**:
  - `src/SmartRAG/Services/Helpers/DocumentValidator.cs` - Geliştirilmiş doğrulama mantığı

#### Servis Kaydı
- **DI Container**: DI container'a şema migrasyon ve chunk servisleri eklendi
- **Dosyalar Güncellendi**:
  - `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Servis kayıtları eklendi

#### Diğer İyileştirmeler
- **Storage Factory**: Şema ile ilgili servisler için güncellendi
- **Sorgu Stratejisi Yürütücü**: Şema-farkındalıklı sorgu yürütme ile geliştirildi
- **Qdrant Koleksiyon Yöneticisi**: Şema doküman desteği için güncellendi

### 📝 Notlar

- **Geriye Dönük Uyumluluk**: Tüm değişiklikler geriye dönük uyumludur
- **Taşınma**: Taşınma gerekmez
- **Breaking Changes**: Yok
- **Schema RAG Pattern**: Şema bilgileri artık vektörleştirilmiş chunk'lar olarak saklanıyor, daha iyi SQL üretimi için semantik arama sağlıyor

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion370">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion370" aria-expanded="false" aria-controls="collapseversion370">
                <strong>v3.7.0</strong> - 2026-01-19
            </button>
        </h2>
        <div id="collapseversion370" class="accordion-collapse collapse" aria-labelledby="headingversion370" >
            <div class="accordion-body">
{% capture version_content %}

### Cross-Database Mapping Detector & Güvenlik İyileştirmeleri

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> MINOR Sürüm</h4>
    <p class="mb-0">
        Bu sürüm cross-database ilişki tespiti ekler ve önemli güvenlik iyileştirmeleri içerir.
        Tüm değişiklikler geriye dönük uyumludur.
    </p>
</div>

### ✨ Eklendi

#### Cross-Database Mapping Detector
- **Otomatik İlişki Tespiti**: Farklı veritabanları arasındaki kolon ilişkilerini tespit etmek için yeni servis
- **Primary Key ve Foreign Key Analizi**: Şema analizine dayalı otomatik tespit
- **Semantik Kolon Eşleştirme**: Veritabanları arası ilişkili kolonların akıllı eşleştirilmesi
- **Dosyalar Güncellendi**:
  - `src/SmartRAG/Models/Configuration/CrossDatabaseMapping.cs` - Cross-database mapping'ler için yeni model
  - `src/SmartRAG/Services/Database/CrossDatabaseMappingDetector.cs` - Yeni tespit servisi
  - `src/SmartRAG/Models/Configuration/DatabaseConnectionConfig.cs` - CrossDatabaseMappings özelliği eklendi

### 🔧 İyileştirildi

#### SQL Script Çıkarma
- **DRY Prensibi Uygulandı**: Veritabanı oluşturucu sınıflarından SQL script'leri ayrı dosyalara çıkarıldı
- **Daha İyi Kod Organizasyonu**: Bakımı kolaylaştırmak için merkezi SQL script'leri
- **Dosyalar Güncellendi**:
  - `src/SmartRAG/Services/Database/DatabaseParserService.cs` - Çıkarılan script'leri kullanacak şekilde güncellendi
  - `src/SmartRAG/Services/Database/DatabaseSchemaAnalyzer.cs` - Şema işleme iyileştirildi

#### Veritabanı Sorgu Üretimi
- **Geliştirilmiş Sorgu Üretimi**: Üretilen sorguların doğruluğu ve doğrulaması iyileştirildi
- **Daha İyi Hata Önleme**: Geliştirilmiş doğrulama mantığı
- **Dosyalar Güncellendi**:
  - `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - Sorgu üretimi iyileştirmeleri
  - `src/SmartRAG/Services/Database/Validation/SqlValidator.cs` - Geliştirilmiş doğrulama
  - `src/SmartRAG/Services/Database/Prompts/SqlPromptBuilder.cs` - İyileştirilmiş prompt oluşturma

#### Veritabanı Parser ve Doküman Arama
- **Daha İyi Servis Entegrasyonu**: Veritabanı ve doküman servisleri arası koordinasyon iyileştirildi
- **Dosyalar Güncellendi**:
  - `src/SmartRAG/Services/Database/DatabaseParserService.cs` - Servis iyileştirmeleri
  - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Entegrasyon iyileştirmeleri

### 🐛 Düzeltildi

#### Güvenlik İyileştirmeleri
- **SQL Injection Önleme**: Geliştirilmiş girdi doğrulaması ve parametreli sorgu kullanımı
- **Command Injection Önleme**: Shell komut çalıştırma kaldırıldı, girdi sanitizasyonu geliştirildi
- **Hassas Veri Sızıntısı Önleme**: Hata mesajlarından ve log'lardan hassas veriler kaldırıldı
  - İstisna mesajlarından yedek dosya yolları kaldırıldı
  - Geliştirilmiş hata mesajı sanitizasyonu
- **Dosyalar Güncellendi**:
  - `src/SmartRAG/Services/Database/DatabaseConnectionManager.cs` - Geliştirilmiş hata yönetimi
  - `src/SmartRAG/Services/Database/DatabaseQueryExecutor.cs` - İyileştirilmiş hata mesajları

### 📝 Notlar

- **Geriye Dönük Uyumluluk**: Tüm değişiklikler geriye dönük uyumludur
- **Taşınma**: Taşınma gerekmez
- **Breaking Changes**: Yok
- **Güvenlik**: Önemli güvenlik iyileştirmeleri dahil edildi

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion360">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion360" aria-expanded="false" aria-controls="collapseversion360">
                <strong>v3.6.0</strong> - 2025-12-30
            </button>
        </h2>
        <div id="collapseversion360" class="accordion-collapse collapse" aria-labelledby="headingversion360" >
            <div class="accordion-body">
{% capture version_content %}

### CancellationToken Desteği ve Performans İyileştirmeleri

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> MINOR Sürüm</h4>
    <p class="mb-0">
        Bu sürüm daha iyi kaynak yönetimi ve iptal işleme için kapsamlı CancellationToken desteği eklerken, performans ve kod kalitesini de iyileştiriyor.
    </p>
</div>

### ✨ Eklendi

#### CancellationToken Desteği
- **Kapsamlı Destek**: Tüm async interface metodları artık `CancellationToken cancellationToken = default` parametresi kabul ediyor
- **Daha İyi Kaynak Yönetimi**: Geliştirilmiş kaynak yönetimi ve zarif iptal işleme
- **Özel Helper'lar**: Özel helper metodlar iptal desteği için güncellendi
- **XML Dokümantasyon**: CancellationToken içeren tüm metodlar için XML dokümantasyonu güncellendi
- **Değiştirilen Dosyalar**:
  - `src/SmartRAG/Interfaces/` - Tüm async interface metodları güncellendi
  - `src/SmartRAG/Services/` - Tüm servis implementasyonları güncellendi
  - `src/SmartRAG/Repositories/` - Tüm repository implementasyonları güncellendi
  - `src/SmartRAG/Providers/` - Tüm provider implementasyonları güncellendi

### 🔧 İyileştirildi

#### Performans
- **Native Async I/O**: Task.Run native async dosya I/O metodları ile değiştirildi
- **Daha İyi Kaynak Kullanımı**: Geliştirilmiş kaynak kullanımı ve azaltılmış overhead
- **Değiştirilen Dosyalar**:
  - `src/SmartRAG/Services/FileWatcher/FileWatcherService.cs` - Native async I/O
  - `src/SmartRAG/Services/Document/DocumentService.cs` - Native async I/O

#### Kod Kalitesi
- **Log Temizliği**: Gereksiz servis ve repository log'ları kaldırıldı
- **Geliştirilmiş Okunabilirlik**: Log okunabilirliği ve gürültü azaltma iyileştirildi
- **Değiştirilen Dosyalar**:
  - `src/SmartRAG/Services/Shared/ServiceLogMessages.cs` - Log temizliği
  - `src/SmartRAG/Repositories/RepositoryLogMessages.cs` - Log temizliği
  - Birden fazla servis ve repository dosyası - Log kaldırma

### 📝 Notlar

- **Geriye Dönük Uyumluluk**: Tüm CancellationToken parametreleri varsayılan değerlere sahip, tam geriye dönük uyumluluk sağlıyor
- **Geçiş**: Geçiş gerekli değil - mevcut kod değişiklik olmadan çalışmaya devam ediyor
- **Breaking Changes**: Yok
- **Kod Kalitesi**: 0 hata, 0 uyarı korundu
- **Teknik Detaylar**: 59 dosya değiştirildi: 635 ekleme(+), 802 silme(-)

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion350">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion350" aria-expanded="false" aria-controls="collapseversion350">
                <strong>v3.5.0</strong> - 2025-12-27
            </button>
        </h2>
        <div id="collapseversion350" class="accordion-collapse collapse" aria-labelledby="headingversion350" >
            <div class="accordion-body">
{% capture version_content %}

### Kod Kalitesi İyileştirmeleri ve Mimari Refactoring

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> MINOR Sürüm</h4>
    <p class="mb-0">
        Bu sürüm kod tabanı genelinde kapsamlı kod kalitesi iyileştirmeleri, mimari refactoring ve SOLID/DRY uyumluluğu geliştirmelerine odaklanır.
    </p>
</div>

### 🔧 İyileştirildi

#### Kod Kalitesi
- **Kapsamlı Refactoring**: Daha iyi SOLID/DRY uyumluluğu için servisler, provider'lar ve interface'ler refactor edildi
- **Kod Organizasyonu**: Geliştirilmiş kod organizasyonu ve sorumluluk ayrımı
- **Bakım Kolaylığı**: Kod tabanı genelinde artırılmış bakım kolaylığı ve okunabilirlik
- **Mimari Desenler**: Daha iyi mimari desen implementasyonu

#### Interface Tutarlılığı
- **İsimlendirme Kuralı**: PascalCase tutarlılığı için `ISQLQueryGenerator` `ISqlQueryGenerator` olarak yeniden adlandırıldı
- **Breaking Change**: Interface'i doğrudan kullananlar referansları güncellemeli

#### Kod Tekrarı Eliminasyonu
- **Wrapper Kaldırma**: Sadece diğer servislere delegate eden gereksiz wrapper metodları kaldırıldı
- **Tekrar Eliminasyonu**: DocumentSearchService ve ilgili servislerde kod tekrarı elimine edildi

#### Arama Stratejisi
- **Implementasyon İyileştirmeleri**: Geliştirilmiş sorgu stratejisi mantığı ve kod kalitesi
- **Daha İyi Organizasyon**: Strateji servislerinde geliştirilmiş kod organizasyonu

#### PDF Ayrıştırma ve OCR
- **Geliştirilmiş Sağlamlık**: PDF ayrıştırmada geliştirilmiş hata işleme
- **Daha İyi Güvenilirlik**: Geliştirilmiş OCR işleme güvenilirliği

### ✨ Eklendi

#### QueryIntentAnalysisResult Modeli
- **Yeni Model**: Sorgu niyet sınıflandırma sonuçları için yapılandırılmış sonuç modeli
- **Tip Güvenliği**: Niyet sınıflandırma için daha iyi tip güvenliği

#### SearchOptions Geliştirmeleri
- **Factory Metodları**: Yapılandırmadan SearchOptions oluşturmak için `FromConfig()` factory metodu eklendi
- **Clone Metodu**: SearchOptions kopyaları oluşturmak için `Clone()` metodu eklendi

#### QueryStrategyRequest Konsolidasyonu
- **Birleştirilmiş Model**: Birden fazla sorgu stratejisi istek DTO'su tek `QueryStrategyRequest` modelinde birleştirildi
- **Basitleştirilmiş API**: Basitleştirilmiş istek işleme

### 🔄 Değiştirildi

#### Interface Metod İmzaları
- **Parametre Kaldırma**: Interface metodlarından `preferredLanguage` parametresi kaldırıldı
- **Metod Birleştirme**: Daha iyi API tutarlılığı için metod overload'ları birleştirildi
- **Breaking Change**: `preferredLanguage` parametresini kullanan kod `SearchOptions` kullanmalı

#### Interface İsimlendirme
- **Yeniden Adlandırılan Interface**: `ISQLQueryGenerator` `ISqlQueryGenerator` olarak yeniden adlandırıldı
- **Breaking Change**: Interface'i doğrudan kullananlar referansları güncellemeli

### 🗑️ Kaldırılanlar

#### Kullanılmayan Servisler
- **ISourceSelectionService**: Kullanılmayan interface ve implementasyon kaldırıldı
- **SourceSelectionService**: Kullanılmayan servis implementasyonu kaldırıldı

#### Gereksiz Wrapper'lar
- **Wrapper Metodları**: Gereksiz wrapper metodları ve orchestration servisleri kaldırıldı
- **Kod Basitleştirme**: Azaltılmış kod karmaşıklığı

### ✨ Faydalar

- **Daha İyi Kod Kalitesi**: Kapsamlı refactoring bakım kolaylığı ve okunabilirliği artırır
- **Geliştirilmiş Mimari**: Daha iyi sorumluluk ayrımı ve SOLID/DRY uyumluluğu
- **Daha Temiz API**: Basitleştirilmiş interface'ler ve metod imzaları
- **Geliştirilmiş Performans**: Gereksiz wrapper'ların kaldırılması performansı artırır
- **Daha İyi Tip Güvenliği**: Yeni modeller daha iyi tip güvenliği sağlar

### 📝 Notlar

- **Breaking Changes**: 
  - `ISQLQueryGenerator` `ISqlQueryGenerator` olarak yeniden adlandırıldı (sadece doğrudan interface kullananlar)
  - Metodlardan `preferredLanguage` parametresi kaldırıldı (dil yapılandırması için `SearchOptions` kullanın)
- **Geçiş**: Interface referanslarını güncelleyin ve dil yapılandırması için `SearchOptions` kullanın
- **Geriye Dönük Uyumluluk**: Çoğu değişiklik dahili refactoring, public API büyük ölçüde uyumlu kalıyor

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion340">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion340" aria-expanded="false" aria-controls="collapseversion340">
                <strong>v3.4.0</strong> - 2025-12-12
            </button>
        </h2>
        <div id="collapseversion340" class="accordion-collapse collapse" aria-labelledby="headingversion340" >
            <div class="accordion-body">
{% capture version_content %}

### MCP Entegrasyonu, Dosya İzleyici ve Sorgu Stratejisi Optimizasyonu

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> MINOR Sürüm</h4>
    <p class="mb-0">
        Bu sürüm MCP (Model Context Protocol) entegrasyonu, dosya izleyici servisi ve erken çıkış ve paralel çalıştırma iyileştirmeleri ile önemli sorgu stratejisi optimizasyonları ekler.
    </p>
</div>

### ✨ Eklendi

#### MCP (Model Context Protocol) Entegrasyonu
- **Harici MCP Sunucu Entegrasyonu**: Harici MCP sunucuları aracılığıyla geliştirilmiş arama yetenekleri
- **Çoklu MCP Sunucuları**: Otomatik araç keşfi ile birden fazla MCP sunucusu desteği
- **Sorgu Zenginleştirme**: MCP sorguları için konuşma geçmişi bağlamı zenginleştirme

#### Dosya İzleyici Servisi
- **Otomatik Doküman İndeksleme**: Klasörleri izle ve yeni belgeleri otomatik indeksle
- **Çoklu İzlenen Klasörler**: Bağımsız yapılandırmalarla birden fazla izlenen klasör desteği
- **Dil-Spesifik İşleme**: Klasör başına dil yapılandırması

#### DocumentType Özelliği
- **İçerik Tipi Filtreleme**: İçerik tipine göre geliştirilmiş doküman chunk filtreleme (Document, Audio, Image)
- **Otomatik Algılama**: Dosya uzantısı ve içerik tipine dayalı doküman tipi algılama

#### DefaultLanguage Desteği
- **Global Varsayılan Dil**: Doküman işleme için global varsayılan dil yapılandırması
- **ISO 639-1 Desteği**: ISO 639-1 dil kodları desteği

#### Geliştirilmiş Arama Özellik Bayrakları
- **Granüler Kontrol**: `EnableMcpSearch`, `EnableAudioSearch`, `EnableImageSearch` bayrakları
- **İstek Başına ve Global Yapılandırma**: Hem istek başına hem global yapılandırma desteği

#### Erken Çıkış Optimizasyonu
- **Performans İyileştirmesi**: Yeterli yüksek kaliteli sonuç bulunduğunda erken çıkış
- **Paralel Çalıştırma**: Doküman araması ve sorgu intent analizinin paralel çalıştırılması
- **Akıllı Skip Mantığı**: Veritabanı intent güveni yüksek olduğunda eager doküman cevap üretimini atlama

#### IsExplicitlyNegative Kontrolü
- **Hızlı Başarısızlık Mekanizması**: `[NO_ANSWER_FOUND]` pattern'i ile açık başarısızlık pattern'lerini algılama
- **Yanlış Pozitifleri Önler**: Yüksek güvenli doküman eşleşmelerine rağmen AI'nın negatif cevaplar döndürmesi durumunda yanlış pozitifleri önler

### 🔧 İyileştirildi

#### Sorgu Stratejisi Optimizasyonu
- **Akıllı Kaynak Seçimi**: Akıllı kaynak seçimi ile geliştirilmiş sorgu çalıştırma stratejisi
- **StrongDocumentMatchThreshold**: Daha iyi doküman önceliklendirmesi için threshold sabiti (4.8) ile geliştirilmiş erken çıkış mantığı
- **Veritabanı Sorgu Skip Mantığı**: Doküman eşleşme gücü ve AI cevap kalitesine dayalı geliştirilmiş mantık

#### Kod Kalitesi
- **Kapsamlı Temizlik**: Gereksiz yorumlar ve dil-spesifik referanslar kaldırıldı
- **Geliştirilmiş İsimlendirme**: Daha iyi sabit isimlendirme ve generic kod pattern'leri
- **Geliştirilmiş Organizasyon**: Geliştirilmiş kod organizasyonu ve yapısı

#### Model Organizasyonu
- **Mantıksal Alt Klasörler**: Modeller mantıksal alt klasörlere yeniden organize edildi (Configuration/, RequestResponse/, Results/, Schema/)

### 🐛 Düzeltildi

- **Dil-Agnostik Eksik Veri Algılama**: Dil-spesifik pattern'ler düzeltildi
- **HttpClient Timeout**: Uzun süren AI işlemleri için timeout artırıldı
- **Türkçe Karakter Encoding**: PDF metin çıkarmada encoding sorunları düzeltildi
- **Chunk0 Alma**: Numara listesi işleme chunk alma düzeltildi
- **DI Scope Sorunları**: Dependency injection scope çakışmaları çözüldü
- **İçerik Tipi Algılama**: İçerik tipi algılama doğruluğu geliştirildi
- **Konuşma Intent Sınıflandırma**: Bağlam farkındalığı geliştirildi
- **Konuşma Geçmişi Tekrar Eden Girdiler**: Tekrar eden girdiler düzeltildi
- **Redis Doküman Alma**: Doküman listesi boş olduğunda doküman alma düzeltildi
- **SqlValidator DI Uyumluluğu**: Dependency injection uyumluluğu düzeltildi

### 🔄 Değiştirildi

- **Özellik Bayrağı İsimlendirme**: Tutarlılık için bayraklar yeniden adlandırıldı (`EnableMcpClient` → `EnableMcpSearch`, vb.)
- **Interface Yeniden Yapılandırma**: Daha iyi organizasyon için interface'ler yeniden organize edildi

### ✨ Faydalar

- **Genişletilmiş Arama Yetenekleri**: MCP entegrasyonu harici veri kaynağı sorgularını etkinleştirir
- **Otomatik Doküman İndeksleme**: Dosya izleyici servisi manuel doküman yüklemelerini azaltır
- **Daha İyi İçerik Filtreleme**: DocumentType özelliği kesin içerik tipi filtrelemeyi etkinleştirir
- **Geliştirilmiş Kod Kalitesi**: Kapsamlı kod temizliği ve organizasyon iyileştirmeleri
- **Geliştirilmiş Çok Dilli Destek**: DefaultLanguage yapılandırması dil işlemeyi basitleştirir
- **Performans Optimizasyonu**: Erken çıkış optimizasyonu arama yanıt sürelerini iyileştirir

### 📝 Notlar

- **MCP Entegrasyonu**: `SmartRagOptions.McpServers` içinde MCP sunucu yapılandırması gerektirir
- **Dosya İzleyici**: `SmartRagOptions.WatchedFolders` içinde izlenen klasör yapılandırması gerektirir
- **Geriye Dönük Uyumluluk**: Tüm değişiklikler geriye dönük uyumludur, breaking change yok

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion330">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion330" aria-expanded="false" aria-controls="collapseversion330">
                <strong>v3.3.0</strong> - 2025-12-01
            </button>
        </h2>
        <div id="collapseversion330" class="accordion-collapse collapse" aria-labelledby="headingversion330" >
            <div class="accordion-body">
{% capture version_content %}

### Redis Vector Search & Storage İyileştirmeleri

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> MINOR Sürüm</h4>
    <p class="mb-0">
        Bu sürüm Redis vector arama yeteneklerini geliştirir ve kullanılmayan storage implementasyonlarını kaldırır.
        Aktif storage provider'ları (Qdrant, Redis, InMemory) tam olarak çalışmaya devam eder.
    </p>
</div>

### ✨ Eklendi

#### Redis RediSearch Entegrasyonu
- **Gelişmiş Vector Similarity Search**: RediSearch modülü desteği ile gelişmiş vector arama yetenekleri
- **Vector Index Configuration**: Algoritma (HNSW), mesafe metriği (COSINE) ve boyut (varsayılan: 768) configuration
  - **Dosyalar Güncellendi**:
    - `src/SmartRAG/Models/RedisConfig.cs` - Vector arama configuration özellikleri
  - `src/SmartRAG/Repositories/RedisDocumentRepository.cs` - RediSearch vector arama implementasyonu

### 🔧 İyileştirildi

#### Redis Vector Search Doğruluğu
- **Doğru Relevance Scoring**: RelevanceScore artık Doküman Arama Service'i sıralaması için doğru hesaplanıyor ve atanıyor
- **Similarity Hesaplama**: RediSearch mesafe metrikleri similarity skorlarına doğru şekilde dönüştürülüyor
- **Debug Logging**: Skor doğrulama logging'i eklendi
- **Dosyalar Güncellendi**:
  - `src/SmartRAG/Repositories/RedisDocumentRepository.cs` - RelevanceScore ataması

#### Redis Embedding Üretimi
- **AI Configuration Yönetimi**: Doğru config almak için IAIConfigurationService injection'ı
  - **Zarif Geri Dönüş**: Config mevcut olmadığında text arama'ya geri dönüş
- **Dosyalar Güncellendi**:
  - `src/SmartRAG/Repositories/RedisDocumentRepository.cs` - AI config yönetimi
  - `src/SmartRAG/Factories/StorageFactory.cs` - IAIConfigurationService injection

#### StorageFactory Dependency Injection
- **Scope Çözümleme**: Lazy resolution kullanarak Singleton/Scoped lifetime uyumsuzluğu düzeltildi
- **IServiceProvider Pattern**: IServiceProvider aracılığıyla lazy dependency resolution'a geçildi
- **Dosyalar Güncellendi**:
  - `src/SmartRAG/Factories/StorageFactory.cs` - Lazy dependency resolution
  - `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - IAIProvider lifetime ayarı

### 🐛 Düzeltildi

- **StorageFactory DI Scope Sorunu**: IAIProvider çözümlerken InvalidOperationException düzeltildi
- **Redis Relevance Scoring**: Arama sonuçlarında RelevanceScore'un 0.0000 olması düzeltildi
- **Redis Embedding Config**: Embedding üretirken NullReferenceException düzeltildi

### 🗑️ Kaldırıldı

- **FileSystemDocumentRepository**: Kullanılmayan file system storage implementasyonu kaldırıldı
- **SqliteDocumentRepository**: Kullanılmayan SQLite storage implementasyonu kaldırıldı
- **StorageConfig Özellikleri**: FileSystemPath ve SqliteConfig kaldırıldı (kullanılmıyor)

### ⚠️ Breaking Changes

- **FileSystem ve SQLite Doküman Repository'leri Kaldırıldı**
  - Bunlar kullanılmayan implementasyonlardı
  - Aktif storage provider'ları (Qdrant, Redis, InMemory) tam olarak çalışmaya devam ediyor
  - FileSystem veya SQLite kullanıyorsanız, Qdrant, Redis veya InMemory'ye geçin

### 📝 Notlar

- **Redis Gereksinimleri**: Vector arama RediSearch modülü gerektirir
  - `redis/redis-stack-server:latest` Docker image'ını kullanın
  - Veya Redis sunucunuza RediSearch modülünü kurun
  - RediSearch olmadan sadece text arama çalışır (vector arama çalışmaz)

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion320">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion320" aria-expanded="false" aria-controls="collapseversion320">
                <strong>v3.2.0</strong> - 2025-11-27
            </button>
        </h2>
        <div id="collapseversion320" class="accordion-collapse collapse" aria-labelledby="headingversion320" >
            <div class="accordion-body">
{% capture version_content %}

### 🏗️ Mimari Refactoring - Modüler Tasarım

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> MINOR Sürüm</h4>
    <p class="mb-0">
        Bu sürüm, tam geriye dönük uyumluluk sağlarken önemli mimari iyileştirmeler sunar.
        Mevcut tüm kodlar değişiklik gerektirmeden çalışmaya devam eder.
    </p>
</div>

#### **Strategy Pattern Uygulaması**

##### SQL Diyalekt Stratejisi
- **`ISqlDialectStrategy`**: Veritabanına özgü SQL üretimi için interface
- **Diyalekt Uygulamaları**: 
  - `SqliteDialectStrategy` - SQLite için optimize edilmiş SQL üretimi
  - `PostgreSqlDialectStrategy` - PostgreSQL için optimize edilmiş SQL üretimi
  - `MySqlDialectStrategy` - MySQL/MariaDB için optimize edilmiş SQL üretimi
  - `SqlServerDialectStrategy` - SQL Server için optimize edilmiş SQL üretimi
- **`ISqlDialectStrategyFactory`**: Uygun diyalekt stratejisi oluşturmak için fabrika
- **Faydalar**: Açık/Kapalı Prensibi (OCP), yeni veritabanı desteği eklemeyi kolaylaştırır

##### Skorlama Stratejisi
- **`IScoringStrategy`**: Doküman ilgililik skorlaması için interface
- **`HybridScoringStrategy`**: Semantik ve anahtar kelime tabanlı skorlamayı birleştirir
- **Faydalar**: Takılabilir skorlama algoritmaları, arama davranışını özelleştirmeyi kolaylaştırır

##### Dosya Ayrıştırıcı Stratejisi
- **`IFileParser`**: Dosya formatı ayrıştırma için interface
- **Strateji tabanlı ayrıştırma**: Her dosya türü için özel ayrıştırıcı uygulaması
- **Faydalar**: Tek Sorumluluk Prensibi (SRP), yeni dosya formatları eklemeyi kolaylaştırır

#### **Repository Katmanı Ayrımı**

##### Konuşma Repository
- **`IConversationRepository`**: Konuşma veri erişimi için özel interface
- **Uygulamalar**:
  - `SqliteConversationRepository` - SQLite tabanlı konuşma depolama
  - `InMemoryConversationRepository` - Bellekte konuşma depolama
  - `FileSystemConversationRepository` - Dosya tabanlı konuşma depolama
  - `RedisConversationRepository` - Redis tabanlı konuşma depolama
- **`IConversationManagerService`**: Konuşma yönetimi için iş mantığı
- **Faydalar**: Sorumlulukların Ayrılması (SoC), Interface Ayrımı Prensibi (ISP)

##### Repository Temizliği
- **`IDocumentRepository`**: Konuşma ile ilgili metodlar kaldırıldı
- **Net ayrım**: Dokümanlar vs Konuşmalar
- **Faydalar**: Daha temiz interface'ler, daha iyi test edilebilirlik

#### **Servis Katmanı Refactoring**

##### AI Servis Ayrıştırması
- **`IAIConfigurationService`**: AI sağlayıcı configuration yönetimi
- **`IAIRequestExecutor`**: Yeniden deneme/yedekleme ile AI istek yürütme
- **`IPromptBuilderService`**: Prompt oluşturma ve optimizasyon
- **`IAIProviderFactory`**: AI sağlayıcı örnekleri oluşturmak için fabrika
- **Faydalar**: Tek Sorumluluk Prensibi (SRP), daha iyi test edilebilirlik

##### Veritabanı Servisleri
- **`IQueryIntentAnalyzer`**: Sorgu niyet analizi ve sınıflandırma
- **`IDatabaseQueryExecutor`**: Veritabanı sorgu yürütme
- **`IResultMerger`**: Çoklu veritabanı sonuç birleştirme
- **`ISqlQueryGenerator`**: Doğrulama ile SQL sorgu üretimi
- **`IDatabaseConnectionManager`**: Veritabanı bağlantı yaşam döngüsü yönetimi
- **`IDatabaseSchemaAnalyzer`**: Veritabanı şema analizi ve önbellekleme

##### Arama Servisleri
- **`IEmbeddingSearchService`**: Embedding tabanlı arama işlemleri
- **`ISourceBuilderService`**: Arama sonucu kaynak oluşturma

##### Ayrıştırıcı Servisleri
- **`IAudioParserService`**: Ses dosyası ayrıştırma ve transkripsiyon
- **`IImageParserService`**: Görüntü OCR işleme
- **`IAudioParserFactory`**: Ses ayrıştırıcı oluşturma fabrikası

##### Destek Servisleri
- **`IQueryIntentClassifierService`**: Sorgu niyet sınıflandırma
- **`ITextNormalizationService`**: Metin normalizasyonu ve temizleme

#### **Model Konsolidasyonu**

#### **Yeni Özellikler: Özelleştirme Desteği**

- **Özel SQL Diyalekt Stratejileri**: Özel veritabanı diyalektleri uygulama ve mevcut olanları genişletme desteği (SQLite, SQL Server, MySQL, PostgreSQL)
- **Özel Skorlama Stratejileri**: Özel arama ilgililik mantığı uygulama desteği
- **Özel Dosya Ayrıştırıcıları**: Özel dosya formatı ayrıştırıcıları uygulama desteği
- **Özel Konuşma Yönetimi**: Konuşma geçmişini yönetmek için yeni servis

### ✨ Eklenenler

- **SearchOptions Desteği**: İstek başına arama configuration'ı ile detaylı kontrol
  - Veritabanı, doküman, ses ve görüntü araması için özellik bayrakları
  - ISO 639-1 dil kodu desteği için `PreferredLanguage` özelliği
  - Özellik bayraklarına dayalı koşullu servis kaydı
  - **Bayrak Tabanlı Doküman Filtreleme**: Hızlı arama tipi seçimi için sorgu string bayrakları (`-db`, `-d`, `-a`, `-i`)
  - **Doküman Tipi Filtreleme**: İçerik tipine göre otomatik filtreleme (metin, ses, görüntü)

- **Native Qdrant Metin Arama**: Geliştirilmiş arama performansı için token tabanlı filtreleme
  - Token tabanlı OR filtreleme ile native Qdrant metin araması
  - Otomatik stopword filtreleme ve token eşleşme sayımı

- **ClearAllAsync Metodları**: Verimli toplu silme işlemleri
  - `IDocumentRepository.ClearAllAsync()` - Verimli toplu silme
  - `IDocumentService.ClearAllDocumentsAsync()` - Tüm dokümanları temizle
  - `IDocumentService.ClearAllEmbeddingsAsync()` - Sadece embedding'leri temizle

- **Tesseract İsteğe Bağlı Dil Verisi İndirme**: Otomatik dil desteği
  - Tesseract dil veri dosyalarının otomatik indirilmesi
  - ISO 639-1/639-2 kod eşleştirmesi ile 30+ dil desteği

- **Para Birimi Sembolü Düzeltme**: Finansal dokümanlar için geliştirilmiş OCR doğruluğu
  - Yaygın OCR yanlış okumalarının otomatik düzeltilmesi (`%`, `6`, `t`, `&` → para birimi sembolleri)
  - Hem OCR hem PDF ayrıştırmaya uygulanır

- **Ollama Embedding'leri için Paralel Toplu İşleme**: Performans optimizasyonu
  - Embedding üretimi için paralel toplu işleme
  - Büyük doküman setleri için geliştirilmiş verim

- **Sorgu Token Parametresi**: Önceden hesaplanmış token desteği
  - Gereksiz tokenizasyonu ortadan kaldırmak için isteğe bağlı `queryTokens` parametresi

- **FeatureToggles Modeli**: Global özellik bayrağı configuration'ı
  - Merkezi özellik yönetimi için `FeatureToggles` sınıfı
  - Kolay configuration için `SearchOptions.FromConfig()` statik metodu

- **ContextExpansionService**: Bitişik chunk bağlam genişletme
  - Bitişik chunk'ları dahil ederek doküman chunk bağlamını genişletir
  - Daha iyi AI yanıtları için yapılandırılabilir bağlam penceresi

- **FileParserResult Modeli**: Standartlaştırılmış parser sonuç yapısı
  - İçerik ve metadata ile tutarlı parser çıktı formatı

- **DatabaseFileParser**: SQLite veritabanı dosyası ayrıştırma desteği
  - Doğrudan veritabanı dosyası yükleme ve ayrıştırma (.db, .sqlite, .sqlite3, .db3)

- **Native Kütüphane Dahil Etme**: Tesseract OCR native kütüphaneleri paketlenmiş
  - Manuel kütüphane kurulumu gerekmez
  - Windows, macOS ve Linux desteği

- **Nullable Reference Types**: Geliştirilmiş null güvenliği
  - 14+ dosyada daha iyi derleme zamanı null kontrolü

### İyileştirmeler

- **Qdrant için Unicode Normalizasyonu**: Tüm dillerde daha iyi metin alımı
- **PDF OCR Kodlama Sorunu Tespiti**: Otomatik yedekleme işleme
- **Numaralı Liste Chunk Tespiti**: Geliştirilmiş sayma sorgusu doğruluğu
- **RAG Skorlama İyileştirmeleri**: Benzersiz anahtar kelime bonusu ile geliştirilmiş ilgililik hesaplama
- **Doküman Arama Uyarlanabilir Eşiği**: Dinamik ilgililik eşiği ayarlama
- **Prompt Builder Kuralları**: Geliştirilmiş AI cevap üretimi
- **QdrantDocumentRepository GetAllAsync**: Performans optimizasyonu
- **Metin İşleme ve AI Prompt Servisleri**: Genel iyileştirmeler
- **Görüntü Ayrıştırıcı Servisi**: Kapsamlı iyileştirmeler

### Düzeltmeler

- **SQL Üretiminde Tablo Takma Adı Zorunluluğu**: Belirsiz kolon hatalarını önler
- **EnableDatabaseSearch Configuration Uyumu**: Uygun özellik bayrağı işleme
- **macOS Native Kütüphaneleri**: OCR kütüphane dahil etme ve DYLD_LIBRARY_PATH configuration'ı
- **Eksik Metod İmzası**: Doküman Arama Service'i geri yükleme

### Değişiklikler

- **IEmbeddingSearchService Bağımlılık Kaldırma**: Basitleştirilmiş mimari
- **Kod Temizliği**: Satır içi yorumlar ve kullanılmayan direktiflerin kaldırılması
- **Günlükleme Temizliği**: Azaltılmış ayrıntılı günlükleme
- **NuGet Paket Güncellemeleri**: En son uyumlu sürümler
- **Servis Metod Açıklamaları**: `[AI Query]`, `[Document Query]`, `[DB Query]` etiketleri ile daha iyi kod dokümantasyonu

### 🔧 Kod Kalitesi

#### **Derleme Kalitesi**
- **Sıfır Uyarı**: Tüm projelerde 0 hata, 0 uyarı korundu
- **SOLID Uyumu**: SOLID prensiplerine tam uyum
- **Temiz Mimari**: Katmanlar arasında net sorumluluk ayrımı

#### **Değiştirilen Dosyalar**
- `src/SmartRAG/Interfaces/` - Strategy Pattern için yeni interface'ler
- `src/SmartRAG/Services/` - Servis katmanı refactoring
- `src/SmartRAG/Repositories/` - Repository ayrımı
- `src/SmartRAG/Models/` - Model konsolidasyonu
- `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Güncellenmiş DI kayıtları

### ✨ Faydalar

- **Bakım Yapılabilirlik**: Daha temiz, daha modüler kod tabanı
- **Genişletilebilirlik**: Yeni veritabanları, AI sağlayıcıları, dosya formatları eklemeyi kolaylaştırır
- **Test Edilebilirlik**: Net interface'lerle daha iyi birim testi
- **Performans**: Veritabanı diyalektine göre optimize edilmiş SQL üretimi
- **Esneklik**: Skorlama, ayrıştırma, SQL üretimi için takılabilir stratejiler
- **Geriye Dönük Uyumluluk**: Mevcut tüm kodlar değişiklik olmadan çalışır

### 📚 Geçiş Rehberi

#### Breaking Change Yok
Tüm değişiklikler geriye dönük uyumludur. Mevcut kodlar değişiklik gerektirmeden çalışmaya devam eder.

#### İsteğe Bağlı İyileştirmeler

**Yeni Konuşma Yönetimini Kullanın**:
```csharp
// Eski yaklaşım (hala çalışır)
await _documentSearchService.QueryIntelligenceAsync(query);

// Yeni yaklaşım (konuşma takibi için önerilir)
var sessionId = await _conversationManager.StartNewConversationAsync();
await _conversationManager.AddToConversationAsync(sessionId, userMessage, aiResponse);
var history = await _conversationManager.GetConversationHistoryAsync(sessionId);
```

#### Özelleştirme Örnekleri (İsteğe Bağlı)

**Özel SQL Diyalekt Stratejisi**:
```csharp
// Örnek: Özel doğrulama ile PostgreSQL desteğini genişletme
public class EnhancedPostgreSqlDialectStrategy : BaseSqlDialectStrategy
{
    public override DatabaseType DatabaseType => DatabaseType.PostgreSQL;
    
    public override string GetDialectName() => "Gelişmiş PostgreSQL";
    
    public override string BuildSystemPrompt(
        DatabaseSchemaInfo schema, 
        string userQuery)
    {
        // Gelişmiş PostgreSQL'e özgü SQL üretimi
        return $"PostgreSQL SQL oluştur: {userQuery}\\nŞema: {schema}";
    }
}
```

**Özel Skorlama Stratejisi**:
```csharp
// Örnek: Özel skorlama mantığı ekleme
public class CustomScoringStrategy : IScoringStrategy
{
    public double CalculateScore(DocumentChunk chunk, string query)
    {
        // Özel skorlama mantığı
    }
}
```

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion310">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion310" aria-expanded="false" aria-controls="collapseversion310">
                <strong>v3.1.0</strong> - 2025-11-11
            </button>
        </h2>
        <div id="collapseversion310" class="accordion-collapse collapse" aria-labelledby="headingversion310" >
            <div class="accordion-body">
{% capture version_content %}

### ✨ Birleşik Sorgu Zekası

#### **Önemli Özellik: Tüm Veri Kaynaklarında Birleşik Arama**
- **Birleşik Sorgu Zekası**: `QueryIntelligenceAsync` artık veritabanları, dokümanlar, görseller (OCR) ve ses (transkripsiyon) üzerinde tek bir sorguda birleşik arama destekliyor
- **Akıllı Hibrit Yönlendirme**: Güven skorlaması ile AI tabanlı niyet tespiti otomatik olarak optimal arama stratejisini belirler
  - Yüksek güven (>0.7) + veritabanı sorguları → Sadece veritabanı sorgusu
  - Yüksek güven (>0.7) + veritabanı sorgusu yok → Sadece doküman sorgusu
  - Orta güven (0.3-0.7) → Hem veritabanı hem doküman sorguları, birleştirilmiş sonuçlar
  - Düşük güven (<0.3) → Sadece doküman sorgusu (yedek)
- **QueryStrategy Enum**: Sorgu yürütme stratejileri için yeni enum (DatabaseOnly, DocumentOnly, Hybrid)
- **Yeni Servis Mimarisi**: QueryIntentAnalyzer, DatabaseQueryExecutor, ResultMerger ve SQLQueryGenerator servisleri ile modüler tasarım
- **Paralel Sorgu Yürütme**: Daha iyi performans için çoklu-veritabanı sorguları paralel olarak yürütülür
- **Akıllı Sonuç Birleştirme**: Birden fazla veritabanından gelen sonuçların AI destekli birleştirilmesi
- **Akıllı Yönlendirme**: Zarif bozulma ve yedek mekanizmalarla geliştirilmiş sorgu yönlendirme mantığı
- **Geliştirilmiş Hata Yönetimi**: Veritabanı sorgu hataları için daha iyi hata yönetimi

#### **Yeni Servisler & Interface'ler**
- `QueryIntentAnalyzer` - Kullanıcı sorgularını analiz eder ve hangi veritabanları/tabloları sorgulayacağını AI kullanarak belirler
- `DatabaseQueryExecutor` - Birden fazla veritabanında paralel sorgu yürütür
- `ResultMerger` - Birden fazla veritabanından gelen sonuçları AI kullanarak tutarlı yanıtlara birleştirir
- `SQLQueryGenerator` - Sorgu niyetine göre her veritabanı için optimize edilmiş SQL sorguları üretir

#### **Yeni Modeller**
- `AudioSegmentMetadata` - Zaman damgaları ve güven skorları ile ses transkripsiyon segmentleri için metadata modeli

#### **Geliştirilmiş Modeller**
- `SearchSource` - Kaynak tipi farklılaştırması ile geliştirildi (Database, Document, Image, Audio)

### 🔧 Kod Kalitesi & AI Prompt Optimizasyonu

#### **Kod Kalitesi İyileştirmeleri**
- **Build Kalitesi**: Tüm projelerde 0 hata, 0 uyarı elde edildi
- **Kod Standartları**: Proje kod standartlarına tam uyumluluk

#### **AI Prompt Optimizasyonu**
- **Emoji Azaltma**: AI prompt'larındaki emoji kullanımı 235'ten 5'e düşürüldü (sadece kritik: 🚨, ✓, ✗)
- **Token Verimliliği**: Token verimliliği iyileştirildi (prompt başına ~100 token tasarruf)
- **Stratejik Kullanım**: Stratejik emoji kullanımı ile daha iyi AI anlayışı

#### **Değiştirilen Dosyalar**
- `src/SmartRAG/Services/SQLQueryGenerator.cs` - AI prompt'larında emoji optimizasyonu
- `src/SmartRAG/Services/MultiDatabaseQueryCoordinator.cs` - Emoji optimizasyonu
- `src/SmartRAG/Services/QueryIntentAnalyzer.cs` - Emoji optimizasyonu
- `src/SmartRAG/Services/DocumentSearchService.cs` - Emoji optimizasyonu

### ✨ Faydalar
- **Temiz Kod Tabanı**: Tüm projelerde sıfır uyarı
- **Daha İyi Performans**: Daha verimli AI prompt işleme
- **Geliştirilmiş Bakım Kolaylığı**: Daha iyi kod kalitesi ve standart uyumluluğu
- **Maliyet Verimliliği**: AI prompt'larında azaltılmış token kullanımı

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion303">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion303" aria-expanded="false" aria-controls="collapseversion303">
                <strong>v3.0.3</strong> - 2025-11-06
            </button>
        </h2>
        <div id="collapseversion303" class="accordion-collapse collapse" aria-labelledby="headingversion303" >
            <div class="accordion-body">
{% capture version_content %}

### 🎯 Paket Optimizasyonu - Native Kütüphaneler

#### **Paket Boyutu Azaltma**
- **Native Kütüphaneler Hariç**: Whisper.net.Runtime native kütüphaneleri (ggml-*.dll, libggml-*.so, libggml-*.dylib) artık SmartRAG NuGet paketine dahil edilmiyor
- **Tessdata Hariç**: `tessdata/eng.traineddata` dosyası artık SmartRAG NuGet paketine dahil edilmiyor
- **Azaltılmış Paket Boyutu**: Önemli ölçüde daha küçük NuGet paket boyutu
- **Temiz Çıktı**: Proje çıktı dizininde gereksiz native kütüphane dosyaları yok

#### **Değiştirilen Dosyalar**
- `src/SmartRAG/SmartRAG.csproj` - Whisper.net.Runtime paket referansına `PrivateAssets="All"` eklendi
- `src/SmartRAG/SmartRAG.csproj` - tessdata/eng.traineddata içerik dosyasına `Pack="false"` eklendi

### ✨ Faydalar
- **Daha Küçük Paket Boyutu**: Native kütüphaneleri hariç tutarak NuGet paket boyutu azaltıldı
- **Temiz Projeler**: Proje çıktısında gereksiz native kütüphane dosyaları yok
- **Daha İyi Bağımlılık Yönetimi**: Native kütüphaneler kendi paketlerinden geliyor (Whisper.net.Runtime, Tesseract)
- **Tutarlı Davranış**: Whisper.net.Runtime paketini doğrudan referans ederkenki davranışla eşleşiyor

### 📚 Geçiş Rehberi
OCR veya Ses Transkripsiyonu özelliklerini kullanıyorsanız:

**Ses Transkripsiyonu için (Whisper.net):**
1. Projenize `Whisper.net.Runtime` paketini ekleyin:
   ```xml
   <PackageReference Include="Whisper.net.Runtime" Version="1.8.1" />
   ```
2. Native kütüphaneler Whisper.net.Runtime paketinden otomatik olarak dahil edilecek
3. Başka değişiklik gerekmiyor

**OCR için (Tesseract):**
1. Projenize `Tesseract` paketini ekleyin:
   ```xml
   <PackageReference Include="Tesseract" Version="5.2.0" />
   ```
2. Tesseract paketi tessdata dosyalarını otomatik olarak içerir
3. Başka değişiklik gerekmiyor

**Not**: OCR veya Ses Transkripsiyonu özelliklerini kullanmıyorsanız, herhangi bir işlem gerekmez. Paketler hala bağımlılık olarak indirilir, ancak native kütüphaneler paketleri açıkça referans etmediğiniz sürece dahil edilmez.

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion302">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion302" aria-expanded="false" aria-controls="collapseversion302">
                <strong>v3.0.2</strong> - 2025-10-24
            </button>
        </h2>
        <div id="collapseversion302" class="accordion-collapse collapse" aria-labelledby="headingversion302" >
            <div class="accordion-body">
{% capture version_content %}

### 🚀 BREAKING CHANGES - Google Speech-to-Text Kaldırıldı

#### **Ses İşleme Değişiklikleri**
- **Google Speech-to-Text Kaldırıldı**: Google Cloud Speech-to-Text entegrasyonunun tamamen kaldırılması
- **Sadece Whisper.net**: Ses transkripsiyonu artık sadece Whisper.net kullanıyor, %100 yerel işleme
- **Veri Gizliliği**: Tüm ses işleme artık tamamen yerel, GDPR/KVKK/HIPAA uyumluluğu sağlanıyor
- **Basitleştirilmiş Configuration**: GoogleSpeechConfig ve ilgili configuration seçenekleri kaldırıldı

#### **Kaldırılan Dosyalar**
- `src/SmartRAG/Services/GoogleAudioParserService.cs` - Google Speech-to-Text servisi
- `src/SmartRAG/Models/GoogleSpeechConfig.cs` - Google Speech configuration modeli

#### **Değiştirilen Dosyalar**
- `src/SmartRAG/SmartRAG.csproj` - Google.Cloud.Speech.V1 NuGet paketi kaldırıldı
- `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Google servis kaydı kaldırıldı
- `src/SmartRAG/Factories/AudioParserFactory.cs` - Sadece Whisper.net için basitleştirildi
- `src/SmartRAG/Models/SmartRagOptions.cs` - GoogleSpeechConfig özelliği kaldırıldı
- `src/SmartRAG/Enums/AudioProvider.cs` - GoogleCloud enum değeri kaldırıldı
- `src/SmartRAG/Services/ServiceLogMessages.cs` - Whisper.net için log mesajları güncellendi

### ✨ Faydalar
- **%100 Yerel İşleme**: Tüm ses transkripsiyonu Whisper.net ile yerel olarak yapılıyor
- **Geliştirilmiş Gizlilik**: Veri altyapınızı terk etmiyor
- **Basitleştirilmiş Kurulum**: Google Cloud kimlik bilgileri gerekmiyor
- **Maliyet Etkin**: Dakika başına transkripsiyon maliyeti yok
- **Çok Dilli**: Otomatik algılama ile 99+ dil desteği

### 🔧 Teknik Detaylar
- **Whisper.net Entegrasyonu**: Whisper.net bağlamaları aracılığıyla OpenAI'nin Whisper modelini kullanır
- **Model Seçenekleri**: Tiny (75MB), Base (142MB), Medium (1.5GB), Large-v3 (2.9GB)
- **Donanım Hızlandırması**: CPU, CUDA, CoreML, OpenVino desteği
- **Otomatik İndirme**: Modeller ilk kullanımda otomatik olarak indirilir
- **Format Desteği**: MP3, WAV, M4A, AAC, OGG, FLAC, WMA

### 📚 Geçiş Rehberi
Google Speech-to-Text kullanıyorsanız:
1. Configuration'ınızdan GoogleSpeechConfig'i kaldırın
2. WhisperConfig'in doğru yapılandırıldığından emin olun
3. Özel ses işleme kodunuzu Whisper.net kullanacak şekilde güncelleyin
4. Yerel Whisper.net modelleri ile ses transkripsiyonunu test edin

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion301">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion301" aria-expanded="false" aria-controls="collapseversion301">
                <strong>v3.0.1</strong> - 2025-10-22
            </button>
        </h2>
        <div id="collapseversion301" class="accordion-collapse collapse" aria-labelledby="headingversion301" >
            <div class="accordion-body">
{% capture version_content %}

### 🐛 Düzeltildi
- **LoggerMessage Parametre Uyumsuzluğu**: `LogAudioServiceInitialized` LoggerMessage tanımında eksik `configPath` parametresi düzeltildi
- **EventId Çakışmaları**: ServiceLogMessages.cs'deki çakışan EventId atamaları çözüldü (6006, 6008, 6009)
- **Logo Görüntüleme Sorunu**: NuGet'te görüntüleme sorunlarına neden olan README dosyalarındaki bozuk logo referansları kaldırıldı
- **TypeInitializationException**: Kritik başlatma hatası düzeltildi

### 🔧 Teknik İyileştirmeler
- **ServiceLogMessages.cs**: LoggerMessage tanımları parametre sayılarıyla doğru eşleşecek şekilde güncellendi
- **EventId Yönetimi**: Benzersiz log tanımlayıcıları için çakışan EventId'ler yeniden atandı

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion300">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion300" aria-expanded="false" aria-controls="collapseversion300">
                <strong>v3.0.0</strong> - 2025-10-22
            </button>
        </h2>
        <div id="collapseversion300" class="accordion-collapse collapse" aria-labelledby="headingversion300" >
            <div class="accordion-body">
{% capture version_content %}

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> BREAKING CHANGE'LER</h4>
    <p class="mb-0">Bu sürüm breaking API değişiklikleri içerir. Aşağıdaki taşınma kılavuzuna bakın.</p>
</div>

### 🚀 Zeka Kütüphanesi Devrimi

#### Önemli API Değişiklikleri
- **`GenerateRagAnswerAsync` → `QueryIntelligenceAsync`**: Akıllı sorgu işlemeyi daha iyi temsil etmek için metod yeniden adlandırıldı
- **Geliştirilmiş `IDocumentSearchService` interface'i**: Gelişmiş RAG pipeline ile yeni akıllı doküman sorgu işleme
- **Servis katmanı iyileştirmeleri**: Gelişmiş anlamsal arama ve konuşma yönetimi
- **Geriye dönük uyumluluk korundu**: Eski metodlar kullanımdan kaldırıldı olarak işaretlendi (v4.0.0'da kaldırılacak)

### 🔧 SQL Üretimi & Çok Dilli Destek

#### Dil-Güvenli SQL Üretimi
- **Otomatik doğrulama**: SQL sorgularında İngilizce olmayan metnin tespiti ve önlenmesi
- **Geliştirilmiş SQL doğrulaması**: SQL'de Türkçe/Almanca/Rusça karakterleri ve anahtar kelimeleri önleyen katı doğrulama
- **Çok dilli sorgu desteği**: AI, herhangi bir dilde sorguları işlerken saf İngilizce SQL üretir
- **Karakter doğrulaması**: İngilizce olmayan karakterleri tespit eder (Türkçe: ç, ğ, ı, ö, ş, ü; Almanca: ä, ö, ü, ß; Rusça: Kiril)
- **Anahtar kelime doğrulaması**: SQL'de İngilizce olmayan anahtar kelimeleri önler (sorgu, abfrage, запрос)
- **İyileştirilmiş hata mesajları**: Hata raporlarında veritabanı tipi bilgisiyle daha iyi tanılama

#### PostgreSQL Tam Desteği
- **Eksiksiz entegrasyon**: Canlı bağlantılarla tam PostgreSQL desteği
- **Şema analizi**: Akıllı şema çıkarma ve ilişki haritalama
- **Çoklu-veritabanı sorguları**: PostgreSQL ile çapraz-veritabanı sorgu koordinasyonu
- **Üretime hazır**: Kapsamlı test ve doğrulama

### 🔒 On-Premise & Şirket İçi AI Desteği

#### Tam On-Premise İşlem
- **On-premise AI modelleri**: Ollama, LM Studio ve herhangi bir OpenAI-uyumlu on-premise API için tam destek
- **Doküman işleme**: PDF, Word, Excel ayrıştırma - tamamen on-premise
- **OCR işleme**: Tesseract 5.2.0 - tamamen on-premise, buluta veri gönderilmez
- **Veritabanı entegrasyonu**: SQLite, SQL Server, MySQL, PostgreSQL - tüm on-premise bağlantılar
- **Depolama seçenekleri**: In-Memory, SQLite, FileSystem, Redis - tümü on-premise
- **Tam gizlilik**: Verileriniz altyapınızda kalır

#### Kurumsal Uyumluluk
- **GDPR uyumlu**: Tüm verileri altyapınızda tutun
- **KVKK uyumlu**: Türk veri koruma kanunu uyumluluğu
- **Hava boşluklu sistemler**: İnternetsiz çalışır (ses transkripsiyonu hariç)
- **Finansal kurumlar**: On-premise dağıtım ile banka düzeyinde güvenlik
- **Sağlık**: HIPAA uyumlu dağıtımlar mümkün
- **Devlet**: On-premise modellerle gizli veri işleme

### ⚠️ Önemli Kısıtlamalar

#### Ses Dosyaları
- **Google Speech-to-Text**: Ses transkripsiyonu kurumsal düzeyde konuşma tanıma için Google Cloud AI kullanır
- **Whisper.net**: Gizlilik hassas dağıtımlar için yerel ses transkripsiyonu seçeneği
- **Veri gizliliği**: Whisper.net sesi yerel olarak işler, Google Speech-to-Text buluta gönderir
- **Çok dilli**: Her iki sağlayıcı da otomatik algılama ile 99+ dil destekler
- **Diğer formatlar**: Diğer tüm dosya tipleri tamamen yerel kalır

#### OCR (Görsel'den Metne)
- **El yazısı kısıtlaması**: Tesseract OCR el yazısını tam olarak destekleyemez (düşük başarı oranı)
- **Mükemmel çalışır**: Basılı dokümanlar, taranmış basılı dokümanlar, yazılmış metinli dijital ekran görüntüleri
- **Sınırlı destek**: El yazısı notları, formlar, bitişik yazı (çok düşük doğruluk)
- **En iyi sonuçlar**: Basılı dokümanların yüksek kaliteli taramaları
- **100+ dil**: [Desteklenen tüm dilleri görüntüle](https://github.com/tesseract-ocr/tessdata)

### ✨ Eklenenler
- **Yerel AI kurulum örnekleri**: Ollama ve LM Studio için configuration
- **Kurumsal kullanım senaryoları**: Bankacılık, Sağlık, Hukuk, Devlet, Üretim

### 🔧 İyileştirmeler
- **Yeniden deneme mekanizması**: Dile özgü talimatlarla geliştirilmiş yeniden deneme istekleri
- **Hata yönetimi**: Veritabanı tipi bilgisiyle daha iyi hata mesajları
- **Kod kalitesi**: Boyunca sürdürülen SOLID/DRY prensipleri
- **Performans**: Optimize edilmiş çoklu-veritabanı sorgu koordinasyonu

### ✅ Kalite Güvencesi
- **Sıfır Uyarı Politikası**: 0 hata, 0 uyarı standardı korundu
- **SOLID Prensipleri**: Temiz kod mimarisi
- **Kapsamlı Test**: PostgreSQL entegrasyonu ile çoklu-veritabanı test kapsamı
- **Güvenlik sertleştirme**: Geliştirilmiş kimlik bilgisi koruması
- **Performans optimizasyonu**: Tüm özelliklerde yüksek performans

### 🔄 Taşınma Kılavuzu (v2.3.0 → v3.0.0)

#### Servis Katmanı Metod Değişiklikleri

**ESKİ (v2.3.0):**
```csharp
await _documentSearchService.GenerateRagAnswerAsync(query, maxResults);
```

**YENİ (v3.0.0):**
```csharp
await _documentSearchService.QueryIntelligenceAsync(query, maxResults);
```

#### Geriye Dönük Uyumluluk
- Eski metodlar kullanımdan kaldırıldı ancak hala çalışıyor (v4.0.0'da kaldırılacak)
- Metodları kendi hızınızda güncelleyin
- Eski metodları kullanmaya devam ederseniz ani breaking change yok

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion231">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion231" aria-expanded="false" aria-controls="collapseversion231">
                <strong>v2.3.1</strong> - 2025-10-20
            </button>
        </h2>
        <div id="collapseversion231" class="accordion-collapse collapse" aria-labelledby="headingversion231" >
            <div class="accordion-body">
{% capture version_content %}

### 🐛 Hata Düzeltmeleri
- **LoggerMessage Parametre Uyumsuzluğu**: ServiceLogMessages.LogAudioServiceInitialized parametre uyumsuzluğu düzeltildi
- **Format String Düzeltmesi**: Servis başlatma sırasında System.ArgumentException'ı önlemek için format string düzeltildi
- **Günlükleme Kararlılığı**: Google Speech-to-Text başlatma için geliştirilmiş günlükleme

### 🔧 Teknik İyileştirmeler
- **Günlükleme Altyapısı**: Geliştirilmiş güvenilirlik
- **Sıfır Uyarı Politikası**: Uyumluluk korundu
- **Test Kapsamı**: Tüm testler başarılı (8/8)

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion230">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion230" aria-expanded="false" aria-controls="collapseversion230">
                <strong>v2.3.0</strong> - 2025-09-16
            </button>
        </h2>
        <div id="collapseversion230" class="accordion-collapse collapse" aria-labelledby="headingversion230" >
            <div class="accordion-body">
{% capture version_content %}

### ✨ Eklenenler
- **Google Speech-to-Text Entegrasyonu**: Kurumsal düzeyde konuşma tanıma
- **Geliştirilmiş Dil Desteği**: Türkçe, İngilizce dahil 100+ dil
- **Gerçek Zamanlı Ses İşleme**: Güven puanlamalı gelişmiş konuşmadan-metne dönüşüm
- **Detaylı Transkripsiyon Sonuçları**: Zaman damgalı segment düzeyinde transkripsiyon
- **Otomatik Format Tespiti**: MP3, WAV, M4A, AAC, OGG, FLAC, WMA desteği
- **Akıllı Ses İşleme**: Akıllı ses doğrulama ve hata yönetimi
- **Performans Optimize**: Minimum bellek ayak iziyle verimli işleme
- **Yapılandırılmış Ses Çıktısı**: Aranabilir, sorgulanabilir bilgi tabanı
- **Kapsamlı XML Dokümantasyonu**: Eksiksiz API dokümantasyonu

### 🔧 İyileştirmeler
- **Ses İşleme Pipeline**: Google Cloud AI ile geliştirilmiş
- **Configuration Yönetimi**: GoogleSpeechConfig kullanacak şekilde güncellendi
- **Hata Yönetimi**: Ses transkripsiyonu için geliştirilmiş

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion220">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion220" aria-expanded="false" aria-controls="collapseversion220">
                <strong>v2.2.0</strong> - 2025-09-15
            </button>
        </h2>
        <div id="collapseversion220" class="accordion-collapse collapse" aria-labelledby="headingversion220" >
            <div class="accordion-body">
{% capture version_content %}

### ✨ Eklenenler
- **Kullanım Senaryosu Örnekleri**: Taranmış dokümanlar, makbuzlar, görsel içeriği

### 🔧 İyileştirmeler
- **Paket Metadata**: Güncellenmiş proje URL'leri ve sürüm notları

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion210">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion210" aria-expanded="false" aria-controls="collapseversion210">
                <strong>v2.1.0</strong> - 2025-09-05
            </button>
        </h2>
        <div id="collapseversion210" class="accordion-collapse collapse" aria-labelledby="headingversion210" >
            <div class="accordion-body">
{% capture version_content %}

### ✨ Eklenenler
- **Otomatik Oturum Yönetimi**: Manuel oturum ID işleme gerekmez
- **Kalıcı Konuşma Geçmişi**: Konuşmalar yeniden başlatmalarda hayatta kalır
- **Yeni Konuşma Komutları**: `/new`, `/reset`, `/clear`
- **Geliştirilmiş API**: İsteğe bağlı `startNewConversation` ile geriye dönük uyumlu
- **Depolama Entegrasyonu**: Redis, SQLite, FileSystem, InMemory ile çalışır

### 🔧 İyileştirmeler
- **Format Tutarlılığı**: Depolama sağlayıcıları arasında standardize edildi
- **Thread Güvenliği**: Geliştirilmiş eşzamanlı erişim yönetimi
- **Platform Agnostik**: .NET ortamlarında uyumlu

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion200">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion200" aria-expanded="false" aria-controls="collapseversion200">
                <strong>v2.0.0</strong> - 2025-08-27
            </button>
        </h2>
        <div id="collapseversion200" class="accordion-collapse collapse" aria-labelledby="headingversion200" >
            <div class="accordion-body">
{% capture version_content %}

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> BREAKING CHANGE</h4>
    <p class="mb-0">.NET 9.0'dan .NET Standard 2.1'e taşındı</p>
</div>

### 🔄 .NET Standard Taşınması
- **Hedef Framework**: .NET 9.0'dan .NET Standard 2.1'e taşındı
- **Framework Uyumluluğu**: Şimdi .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+ destekler
- **Maksimum Erişim**: Eski ve kurumsal ortamlarla geliştirilmiş uyumluluk

### ✨ Eklenenler
- **Çapraz Platform Desteği**: .NET Standard 2.1 hedef frameworkleri
- **Eski Framework Desteği**: Tam .NET Framework uyumluluğu
- **Kurumsal Entegrasyon**: Mevcut kurumsal çözümlerle sorunsuz entegrasyon

### 🔧 İyileştirmeler
- **Dil Uyumluluğu**: .NET Standard 2.1 için C# 7.3 sözdizimi
- **Paket Versiyonları**: .NET Standard uyumlu versiyonlara güncellendi
- **API Uyumluluğu**: Framework uyumluluğu sağlarken işlevselliği korundu

### 🧪 Test
- **Unit Testler**: Tüm yeni özellikler için kapsamlı test kapsamı
- **Entegrasyon Testleri**: Framework uyumluluğu doğrulaması
- **Performans Testleri**: .NET Standard performans optimizasyonu

### 🔒 Güvenlik
- **Paket Güvenliği**: Güvenlik açıkları için güncellenmiş bağımlılıklar
- **API Güvenliği**: Geliştirilmiş giriş doğrulama ve hata yönetimi
- **Veri Koruma**: Hassas veri işleme için geliştirilmiş güvenlik önlemleri

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion110">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion110" aria-expanded="false" aria-controls="collapseversion110">
                <strong>v1.1.0</strong> - 2025-08-22
            </button>
        </h2>
        <div id="collapseversion110" class="accordion-collapse collapse" aria-labelledby="headingversion110" >
            <div class="accordion-body">
{% capture version_content %}

### ✨ Eklenenler
- **Excel Doküman Desteği**: Kapsamlı Excel ayrıştırma (.xlsx, .xls)
- **EPPlus 8.1.0 Entegrasyonu**: Ticari olmayan lisanslı modern Excel kütüphanesi
- **Çalışma Sayfası Ayrıştırma**: Sekme ile ayrılmış veri korumayla akıllı ayrıştırma
- **Geliştirilmiş İçerik Doğrulama**: Excel'e özgü yedek işleme
- **Anthropic API Güvenilirliği**: HTTP 529 (Aşırı Yüklenmiş) hataları için geliştirilmiş yeniden deneme

### 🔧 İyileştirmeler
- **API Hata Yönetimi**: Hız sınırlama için daha iyi yeniden deneme mantığı
- **İçerik İşleme**: Daha sağlam doküman ayrıştırma
- **Performans**: Optimize edilmiş Excel çıkarma ve doğrulama

### 🧪 Test
- **Unit Testler**: Tüm yeni özellikler için kapsamlı test kapsamı
- **Entegrasyon Testleri**: Framework uyumluluğu doğrulaması
- **Performans Testleri**: .NET Standard performans optimizasyonu

### 🔒 Güvenlik
- **Paket Güvenliği**: Güvenlik açıkları için güncellenmiş bağımlılıklar
- **API Güvenliği**: Geliştirilmiş giriş doğrulama ve hata yönetimi
- **Veri Koruma**: Hassas veri işleme için geliştirilmiş güvenlik önlemleri

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion103">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion103" aria-expanded="false" aria-controls="collapseversion103">
                <strong>v1.0.3</strong> - 2025-08-20
            </button>
        </h2>
        <div id="collapseversion103" class="accordion-collapse collapse" aria-labelledby="headingversion103" >
            <div class="accordion-body">
{% capture version_content %}

### 🔧 Düzeltmeler
- LoggerMessage parametre sayısı uyumsuzlukları
- Sağlayıcı günlükleme mesajı uygulamaları
- Servis koleksiyonu kayıt sorunları

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion102">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion102" aria-expanded="false" aria-controls="collapseversion102">
                <strong>v1.0.2</strong> - 2025-08-19
            </button>
        </h2>
        <div id="collapseversion102" class="accordion-collapse collapse" aria-labelledby="headingversion102" >
            <div class="accordion-body">
{% capture version_content %}

### 📦 Paket Sürümü

#### **Sürüm Notları**
- **Versiyon Güncellemesi**: Paket versiyonu 1.0.2'ye güncellendi
- **Paket Metadata**: v1.0.2 özellikleri ile sürüm notları güncellendi

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion101">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion101" aria-expanded="false" aria-controls="collapseversion101">
                <strong>v1.0.1</strong> - 2025-08-17
            </button>
        </h2>
        <div id="collapseversion101" class="accordion-collapse collapse" aria-labelledby="headingversion101" >
            <div class="accordion-body">
{% capture version_content %}

### 🔧 İyileştirildi

- **Akıllı Sorgu Niyeti Tespiti**: Chat ve doküman arama arasında geliştirilmiş sorgu yönlendirme
- **Dil-Agnostik Tasarım**: Global uyumluluk için tüm hardcoded dil pattern'leri kaldırıldı
- **Geliştirilmiş Arama İlgililiği**: İsim tespiti ve içerik skorlama algoritmaları iyileştirildi
- **Unicode Normalizasyonu**: Özel karakter işleme sorunları düzeltildi (örn., Türkçe karakterler)
- **Hız Sınırlama & Yeniden Deneme Mantığı**: Exponential backoff ile sağlam API işleme
- **VoyageAI Entegrasyonu**: Optimize edilmiş Anthropic embedding desteği

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion100">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion100" aria-expanded="false" aria-controls="collapseversion100">
                <strong>v1.0.0</strong> - 2025-08-15
            </button>
        </h2>
        <div id="collapseversion100" class="accordion-collapse collapse" aria-labelledby="headingversion100" >
            <div class="accordion-body">
{% capture version_content %}

### 🚀 İlk Sürüm

#### **Özellikler**
- **Yüksek Performanslı RAG**: Çoklu sağlayıcı AI desteği implementasyonu
- **5 AI Sağlayıcı**: OpenAI, Anthropic, Gemini, Azure OpenAI, Custom
- **5 Depolama Backend**: Qdrant, Redis, SQLite, FileSystem, InMemory
- **Doküman Formatları**: Akıllı ayrıştırma ile PDF, Word, Metin
- **Kurumsal Mimari**: Dependency injection ve temiz mimari
- **CI/CD Pipeline**: Eksiksiz GitHub Actions iş akışı
- **Güvenlik**: CodeQL analizi ve Codecov kapsam raporlama
- **NuGet Paketi**: Modern metadata ile profesyonel paket

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
</div>

---

## Versiyon Geçmişi

<div class="table-responsive mt-4">
    <table class="table">
        <thead>
            <tr>
                <th>Versiyon</th>
                <th>Tarih</th>
                <th>Öne Çıkanlar</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td><strong>4.0.0</strong></td>
                <td>2026-02-06</td>
                <td>.NET 6 geçişi, yerleşik Dashboard, kaldırılan eski API'ler</td>
            </tr>
            <tr>
                <td><strong>3.1.0</strong></td>
                <td>2025-11-11</td>
                <td>Birleşik Sorgu Zekası, Akıllı Hibrit Yönlendirme, Yeni Servis Mimarisi</td>
            </tr>
            <tr>
                <td><strong>3.0.3</strong></td>
                <td>2025-11-06</td>
                <td>Paket Optimizasyonu - Native Kütüphaneler Hariç</td>
            </tr>
            <tr>
                <td><strong>3.0.0</strong></td>
                <td>2025-10-22</td>
                <td>Zeka Kütüphanesi Devrimi, SQL Üretimi, Yerinde Destek, PostgreSQL</td>
            </tr>
            <tr>
                <td><strong>2.3.1</strong></td>
                <td>2025-10-08</td>
                <td>Hata düzeltmeleri, Günlükleme kararlılığı iyileştirmeleri</td>
            </tr>
            <tr>
                <td><strong>2.3.0</strong></td>
                <td>2025-09-16</td>
                <td>Google Speech-to-Text entegrasyonu, Ses işleme</td>
            </tr>
            <tr>
                <td><strong>2.2.0</strong></td>
                <td>2025-09-15</td>
                <td>OCR özellikleri iyileştirmeleri</td>
            </tr>
            <tr>
                <td><strong>2.1.0</strong></td>
                <td>2025-09-05</td>
                <td>Otomatik oturum yönetimi, Kalıcı konuşma geçmişi</td>
            </tr>
            <tr>
                <td><strong>2.0.0</strong></td>
                <td>2025-08-27</td>
                <td>.NET Standard 2.1 taşınması</td>
            </tr>
            <tr>
                <td><strong>1.1.0</strong></td>
                <td>2025-08-22</td>
                <td>Excel desteği, EPPlus entegrasyonu</td>
            </tr>
            <tr>
                <td><strong>1.0.3</strong></td>
                <td>2025-08-20</td>
                <td>Hata düzeltmeleri ve günlükleme iyileştirmeleri</td>
            </tr>
            <tr>
                <td><strong>1.0.2</strong></td>
                <td>2025-08-19</td>
                <td>İlk kararlı sürüm</td>
            </tr>
            <tr>
                <td><strong>1.0.1</strong></td>
                <td>2025-08-17</td>
                <td>Beta sürümü</td>
            </tr>
            <tr>
                <td><strong>1.0.0</strong></td>
                <td>2025-08-15</td>
                <td>İlk sürüm</td>
            </tr>
        </tbody>
    </table>
</div>

---