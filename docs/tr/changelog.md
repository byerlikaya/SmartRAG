---
layout: default
title: Değişiklikler
description: SmartRAG için eksiksiz versiyon geçmişi, breaking change'ler ve taşınma kılavuzları
lang: tr
redirect_from: /tr/changelog.html
---

<script>
    window.location.href = "{{ site.baseurl }}/tr/changelog/";
</script>

Bu sayfa taşındı. Lütfen [Değişiklikler Ana Sayfası]({{ site.baseurl }}/tr/changelog/)'nı ziyaret edin.

## [3.3.0] - 2025-12-01

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

## [3.2.0] - 2025-11-27

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

## [3.1.0] - 2025-11-11

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

## [3.0.3] - 2025-11-06

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

## [3.0.2] - 2025-10-24

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

## [3.0.1] - 2025-10-22

### 🐛 Düzeltildi
- **LoggerMessage Parametre Uyumsuzluğu**: `LogAudioServiceInitialized` LoggerMessage tanımında eksik `configPath` parametresi düzeltildi
- **EventId Çakışmaları**: ServiceLogMessages.cs'deki çakışan EventId atamaları çözüldü (6006, 6008, 6009)
- **Logo Görüntüleme Sorunu**: NuGet'te görüntüleme sorunlarına neden olan README dosyalarındaki bozuk logo referansları kaldırıldı
- **TypeInitializationException**: Kritik başlatma hatası düzeltildi

### 🔧 Teknik İyileştirmeler
- **ServiceLogMessages.cs**: LoggerMessage tanımları parametre sayılarıyla doğru eşleşecek şekilde güncellendi
- **EventId Yönetimi**: Benzersiz log tanımlayıcıları için çakışan EventId'ler yeniden atandı

---

## [3.0.0] - 2025-10-22

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
- **Çok dilli README**: İngilizce, Türkçe, Almanca ve Rusça'da mevcut
- **Çok dilli CHANGELOG**: 4 dilde mevcut
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

## [2.3.1] - 2025-10-20

### 🐛 Hata Düzeltmeleri
- **LoggerMessage Parametre Uyumsuzluğu**: ServiceLogMessages.LogAudioServiceInitialized parametre uyumsuzluğu düzeltildi
- **Format String Düzeltmesi**: Servis başlatma sırasında System.ArgumentException'ı önlemek için format string düzeltildi
- **Günlükleme Kararlılığı**: Google Speech-to-Text başlatma için geliştirilmiş günlükleme

### 🔧 Teknik İyileştirmeler
- **Günlükleme Altyapısı**: Geliştirilmiş güvenilirlik
- **Sıfır Uyarı Politikası**: Uyumluluk korundu
- **Test Kapsamı**: Tüm testler başarılı (8/8)

---

## [2.3.0] - 2025-09-16

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

## [2.2.0] - 2025-09-15

### ✨ Eklenenler
- **Kullanım Senaryosu Örnekleri**: Taranmış dokümanlar, makbuzlar, görsel içeriği

### 🔧 İyileştirmeler
- **Paket Metadata**: Güncellenmiş proje URL'leri ve sürüm notları
- **Kullanıcı Rehberliği**: İyileştirilmiş görsel işleme iş akışları

---

## [2.1.0] - 2025-09-05

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

## [2.0.0] - 2025-08-27

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

## [1.1.0] - 2025-08-22

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

### 📚 Dokümantasyon
- **Kapsamlı API Referansı**: Tüm interface'ler ve metodlar dokümante edildi
- **Kullanım Örnekleri**: Gerçek dünya senaryolarıyla pratik örnekler
- **Configuration Rehberi**: Detaylı ayar seçenekleri ve örnekleri

### 🧪 Test
- **Unit Testler**: Tüm yeni özellikler için kapsamlı test kapsamı
- **Entegrasyon Testleri**: Framework uyumluluğu doğrulaması
- **Performans Testleri**: .NET Standard performans optimizasyonu

### 🔒 Güvenlik
- **Paket Güvenliği**: Güvenlik açıkları için güncellenmiş bağımlılıklar
- **API Güvenliği**: Geliştirilmiş giriş doğrulama ve hata yönetimi
- **Veri Koruma**: Hassas veri işleme için geliştirilmiş güvenlik önlemleri

---

## [1.0.3] - 2025-08-20

### 🔧 Düzeltmeler
- LoggerMessage parametre sayısı uyumsuzlukları
- Sağlayıcı günlükleme mesajı uygulamaları
- Servis koleksiyonu kayıt sorunları

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
                <td><strong>3.6.0</strong></td>
                <td>2025-12-30</td>
                <td>CancellationToken Desteği, Performans İyileştirmeleri, Kod Kalitesi Geliştirmeleri</td>
            </tr>
            <tr>
                <td><strong>3.5.0</strong></td>
                <td>2025-12-27</td>
                <td>Kod Kalitesi İyileştirmeleri, Mimari Refactoring, SOLID/DRY Uyumluluğu</td>
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
                <td>OCR yetenekleri ve görsel işleme</td>
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

## Taşınma Kılavuzları

### v2.x'ten v3.0.0'a Taşınma

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Temel Değişiklikler</h4>
    <p class="mb-0">Birincil değişiklik, <code>GenerateRagAnswerAsync</code>'in <code>QueryIntelligenceAsync</code> olarak yeniden adlandırılmasıdır.</p>
</div>

**Adım 1: Metod çağrılarını güncelleyin**

```csharp
// Önce (v2.x)
var response = await _searchService.GenerateRagAnswerAsync(query, maxResults);

// Sonra (v3.0.0)
var response = await _searchService.QueryIntelligenceAsync(query, maxResults);
```

**Adım 2: API endpoint'lerini güncelleyin (Web API kullanıyorsanız)**

Web API controller'ınız varsa, sadece service method çağrısını güncelleyin:

```csharp
// Önce (v2.x)
[HttpPost("generate-answer")]
public async Task<IActionResult> GenerateAnswer([FromBody] QueryRequest request)
{
    var response = await _searchService.GenerateRagAnswerAsync(request.Query);
    return Ok(response);
}

// Sonra (v3.0.0) - Sadece method adı değişti
[HttpPost("generate-answer")]
public async Task<IActionResult> GenerateAnswer([FromBody] QueryRequest request)
{
    var response = await _searchService.QueryIntelligenceAsync(request.Query);
    return Ok(response);
}
```

**Not:** Mevcut endpoint yollarınızı ve controller method adlarınızı koruyabilirsiniz. Sadece service method çağrısını güncellemeniz yeterlidir.
```

<div class="alert alert-success">
    <h4><i class="fas fa-check-circle me-2"></i> Acil Eylem Gerekmez</h4>
    <p class="mb-0">
        Eski <code>GenerateRagAnswerAsync</code> metodu hala çalışıyor (kullanımdan kaldırıldı olarak işaretli). 
        v4.0.0 yayınlanmadan önce kademeli olarak taşınabilirsiniz.
    </p>
                    </div>

### v1.x'ten v2.0.0'a Taşınma

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Temel Değişiklikler</h4>
    <p>Birincil değişiklik, .NET 9.0'dan .NET Standard 2.1'e taşınmasıdır.</p>
</div>

**Adım 1: Hedef framework'ü güncelleyin**

```xml
<!-- Önce (.csproj) -->
<TargetFramework>net9.0</TargetFramework>

<!-- Sonra (.csproj) -->
<TargetFramework>netstandard2.1</TargetFramework>
```

**Adım 2: Paket referanslarını kontrol edin**

```xml
<!-- .NET Standard 2.1 uyumlu paketler -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
```

**Adım 3: Kod değişiklikleri**

```csharp
// Önce (v1.x)
using Microsoft.Extensions.DependencyInjection;

// Sonra (v2.0.0) - Aynı
using Microsoft.Extensions.DependencyInjection;
```

---

## Kullanımdan Kaldırma Bildirimleri

### v3.0.0'da Kullanımdan Kaldırıldı (v4.0.0'da Kaldırılacak)
    <h4><i class="fas fa-clock me-2"></i> Kaldırma Planlandı</h4>
    <p>Aşağıdaki metodlar kullanımdan kaldırıldı ve v4.0.0'da kaldırılacak:</p>
    <ul class="mb-0">
        <li><code>IDocumentSearchService.GenerateRagAnswerAsync()</code> - Yerine <code>QueryIntelligenceAsync()</code> kullanın</li>
                        </ul>
                    </div>

---

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-rocket"></i>
                            </div>
            <h3>Başlangıç</h3>
            <p>SmartRAG'i kurun ve akıllı uygulamalar oluşturmaya başlayın</p>
            <a href="{{ site.baseurl }}/tr/getting-started" class="btn btn-outline-primary btn-sm mt-3">
                Başlayın
            </a>
                        </div>
                    </div>

                        <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fab fa-github"></i>
                                </div>
            <h3>GitHub Repository</h3>
            <p>Kaynak kodunu görüntüleyin, sorunları bildirin ve katkıda bulunun</p>
            <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-primary btn-sm mt-3" target="_blank">
                GitHub'da Görüntüle
            </a>
                    </div>
                </div>
            </div>

