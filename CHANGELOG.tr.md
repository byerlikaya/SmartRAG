
# Değişiklik Günlüğü

SmartRAG'deki tüm önemli değişiklikler bu dosyada belgelenecektir.

Format [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)'a dayanmaktadır
ve bu proje [Semantic Versioning](https://semver.org/spec/v2.0.0.html)'a uymaktadır.

## [4.0.1] - 2026-03-09

### Düzeltmeler
- **LM Studio / OpenAI-Uyumlu Embeddings**: `CustomProvider` için embedding isteği payload'ı OpenAI tarzı sunucularla daha uyumlu hale getirildi ve batch embedding yanıtları için hem Ollama stilindeki `embeddings` dizilerini hem de OpenAI stilindeki `data[].embedding` yapılarını esnek şekilde parse eden geliştirilmiş JSON ayrıştırma eklendi.
  - **Değiştirilen Dosyalar**: `src/SmartRAG/Providers/CustomProvider.cs`

- **SQLite Şema Yol Çözümleme**: SQLite şema analizi ve ayrıştırması, veritabanı dosyalarını çözüm kök dizinine göre çözecek şekilde düzeltildi; böylece boş veritabanı dosyalarının yanlışlıkla oluşturulması engellendi ve mevcut `.db` dosyalarının şema migrasyonu için doğru şekilde kullanılması sağlandı.
  - **Değiştirilen Dosyalar**: `src/SmartRAG/Services/Database/DatabaseSchemaAnalyzer.cs`, `src/SmartRAG/Services/Database/DatabaseParserService.cs`

### 📝 Notlar
- **Geriye Dönük Uyumluluk**: Tüm değişiklikler geriye dönük uyumludur; public API yüzeyinde değişiklik yoktur.
- **Kod Kalitesi**: 0 hata, 0 uyarı build politikası korunmuştur.

## [4.0.0] - 2026-03-02

### Kırıcı Değişiklikler
- **Hedef Framework**: .NET Standard 2.1'den .NET 6'ya geçiş. Projeler .NET 6 veya üzerini hedeflemelidir.
- **SmartRAG.Dashboard Projesi Birleştirildi**: Dashboard ayrı bir projeydi (NuGet paketi hiç yayınlanmadı). Artık ana SmartRAG paketinde. `SmartRAG.Dashboard` proje referansı kullandıysanız kaldırın; sadece `SmartRAG` kullanın.
- **Minimum .NET Sürümü**: .NET 6+ gerekli. .NET Core 3.0, .NET 5 ve .NET Standard 2.1 artık desteklenmiyor.

### Eklenenler
- **Yerleşik Dashboard**: Doküman yönetimi ve chat UI artık SmartRAG'ın parçası. Ayrı paket gerekmez.
- Aynı API: `AddSmartRagDashboard()`, `UseSmartRagDashboard()`, `MapSmartRagDashboard()` - tüketiciler için kod değişikliği yok.

### Taşınma
- Yükseltme talimatları için [taşınma kılavuzuna](https://byerlikaya.github.io/SmartRAG/tr/changelog/migration-guides.html#v3xten-v400a-taşınma) bakın.

## [3.9.0] - 2026-02-05

### ✨ Eklenenler
- **Konuşma Zaman Damgaları ve Kaynaklar**: Dashboard ve chat UI'ları için konuşma depolama genişletildi
  - `IConversationRepository.GetSessionTimestampsAsync` - Oturum oluşturulma/son güncelleme zamanları
  - `IConversationRepository.AppendSourcesForTurnAsync` - Asistan turu başına kaynak JSON'u saklama
  - `IConversationRepository.GetSourcesForSessionAsync` - Oturum için saklanan kaynakları getirme
  - `IConversationRepository.GetAllSessionIdsAsync` - Tüm bilinen oturum ID'lerini listeleme
  - **Değiştirilen Dosyalar**: `IConversationRepository`, `SqliteConversationRepository`, `RedisConversationRepository`, `FileSystemConversationRepository`, `InMemoryConversationRepository`

- **Açık Oturum RAG Overload**: Dashboard/API entegrasyonu için `sessionId` ve `conversationHistory` ile `IDocumentSearchService.QueryIntelligenceAsync` overload'u

- **Yinelenen Yükleme Önleme**: Özdeş dokümanlar için hash tabanlı atlama
  - Yinelenen içerik hash'i nedeniyle atlandığında yeni `DocumentSkippedException`
  - **Değiştirilen Dosyalar**: `DocumentService`, `DocumentParserService`, `FileWatcherService`, `SchemaChunkService`, `AudioFileParser`, `ImageParserService`

- **Whisper Native Bootstrap**: Başlangıçta Whisper.net native kütüphane başlatması için `WhisperNativeBootstrap` servisi
  - **Değiştirilen Dosyalar**: `SmartRagStartupService`, `WhisperConfig`, `WhisperAudioParserService`

- **MCP İsteğe Bağlı Bağlantı**: MCP sunucuları yalnızca sorguda `-mcp` etiketi kullanıldığında bağlanır
  - **Değiştirilen Dosyalar**: `McpIntegrationService`

### 🔧 İyileştirmeler
- **Doküman RAG Arama**: Arama stratejisi, relevance skorlama ve yanıt oluşturmada büyük iyileştirmeler
  - Sorgu doküman adlarıyla eşleştiğinde dosya adı tabanlı erken dönüş
  - Phrase kelimeleri ve morfolojik eşleştirme ile chunk önceliklendirme (`IChunkPrioritizerService`, `ChunkPrioritizerService`)
  - Dosya adı phrase çıkarımı ile doküman relevance skorlama (`IDocumentRelevanceCalculatorService`, `DocumentRelevanceCalculatorService`)
  - Şema chunk'ları için `-db` kapalıyken geliştirilmiş chunk seçimi
  - Kaynaklar veri içerdiğinde ancak ilk yanıt eksik gösterdiğinde `IPromptBuilderService.BuildDocumentRagPrompt` içinde extraction retry modu
  - **Değiştirilen Dosyalar**: `DocumentSearchService`, `DocumentSearchStrategyService`, `DocumentScoringService`, `ResponseBuilderService`, `QueryStrategyExecutorService`, `QueryIntentClassifierService`, `SearchTextExtensions`, `SearchSourceHelper`, `RagMessages`, `QueryTokenizer`

- **Takip Sorusu İşleme**: Takip sorguları için daha iyi konuşma context'i
  - **Değiştirilen Dosyalar**: `PromptBuilderService`, `QueryIntentAnalyzer`, `ConversationManagerService`, `DocumentSearchService`

- **PDF ve OCR**: Geliştirilmiş metin çıkarımı ve encoding
  - Türkçe encoding, OCR para birimi pattern'leri
  - **Değiştirilen Dosyalar**: `PdfFileParser`, `ImageParserService`

- **Storage Factory Scoping**: Doğru scoped çözümleme için `IStorageFactory.GetCurrentRepository(IServiceProvider scopedProvider)` (örn. `IAIConfigurationService`)
  - **Değiştirilen Dosyalar**: `IStorageFactory`, `StorageFactory`, `ServiceCollectionExtensions`

- **Qdrant Entegrasyonu**: Qdrant.Client 1.16.1 uyumluluğu ve arama iyileştirmeleri
  - Vector okuma yolu: dense/sparse API için `Vector.Dense.Data`
  - Doğrudan dizi ataması ile sadeleştirilmiş nokta oluşturma
  - `VectorsCount` yerine `PointsCount`
  - `IQdrantCacheManager` ve `QdrantCacheManager` kaldırıldı (sadeleştirilmiş arama yolu)
  - **Değiştirilen Dosyalar**: `QdrantDocumentRepository`, `QdrantSearchService`

- **Veritabanı ve Sonuç Birleştirme**: Küçük iyileştirmeler
  - **Değiştirilen Dosyalar**: `DatabaseQueryExecutor`, `ResultMerger`, `SchemaChunkService`

- **NuGet Paket Güncellemeleri**: Bağımlılıklar güncellendi
  - Qdrant.Client: 1.15.1 → 1.16.1
  - StackExchange.Redis: 2.10.1 → 2.10.14
  - MySql.Data: 9.5.0 → 9.6.0
  - itext: 9.4.0 → 9.5.0
  - EPPlus: 8.4.1 → 8.4.2
  - PDFtoImage: 5.0.0 → 5.2.0

### ⚠️ Kırıcı Değişiklikler
- **IStorageFactory**: `GetCurrentRepository()` yerine `GetCurrentRepository(IServiceProvider scopedProvider)` - doküman repository çözümlerken scoped `IServiceProvider` geçirin
- **IConversationRepository**: Yeni zorunlu metodlar `AppendSourcesForTurnAsync`, `GetSourcesForSessionAsync`, `GetAllSessionIdsAsync` - özel implementasyonlar bunları implement etmeli
- **IQdrantCacheManager**: Interface ve `QdrantCacheManager` kaldırıldı - arama artık sorgu sonuç önbelleklemesi kullanmıyor

### 📝 Notlar
- **Migrasyon**: `IStorageFactory` ve `IConversationRepository` değişiklikleri için migrasyon rehberine bakın
- **Kod Kalitesi**: 0 hata, 0 uyarı build politikası korunmuştur

## [3.8.1] - 2026-01-28

### 🔧 İyileştirmeler
- **Şema Servislerinde Cancellation Desteği**: Şema migrasyonu ve ilişkili servislerde `CancellationToken` akışı iyileştirildi
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Database/SchemaMigrationService.cs` - Cancellation token propagasyonu
    - `src/SmartRAG/Services/Database/QueryIntentAnalyzer.cs` - Küçük cancellation ile ilgili iyileştirmeler
  - **Faydalar**: Daha güvenli iptal davranışı ve daha sağlam async akışlar

- **Kod Temizliği ve Bakım Kolaylığı**: Veritabanı, arama ve watcher servisleri genelinde kullanılmayan helper'lar, stratejiler ve event'ler kaldırıldı
  - **Değiştirilen Dosyalar** (yüksek seviye):
    - `src/SmartRAG/Services/Database/Prompts/SqlPromptBuilder.cs` - Kullanılmayan prompt helper'ları ve dead code path'ler kaldırıldı
    - `src/SmartRAG/Services/Database/Strategies/*` - Kullanılmayan SQL dialect helper metodları kaldırıldı
    - `src/SmartRAG/Services/Document/*` - Skorlama ve strateji helper'ları sadeleştirildi, kullanılmayan kodlar kaldırıldı
    - `src/SmartRAG/Services/Search/ContextExpansionService.cs` - Genişletme mantığı sadeleştirildi
    - `src/SmartRAG/Services/Storage/Qdrant/QdrantSearchService.cs` - Kullanılmayan arama helper'ları kaldırıldı, davranış korunarak sadeleştirildi
    - `src/SmartRAG/Services/FileWatcher/FileWatcherService.cs` ve `FileWatcherEventArgs.cs` - Kullanılmayan event ve özellikler kaldırıldı
    - `src/SmartRAG/Services/Support/ConversationManagerService.cs` - Kullanılmayan helper'lar kaldırıldı
    - `src/SmartRAG/Helpers/QueryTokenizer.cs` - Kullanılmayan token/helper'lar kaldırıldı
  - **Faydalar**: Davranışı değiştirmeden daha küçük, bakımı kolay kod tabanı

- **Logging ve Repository Mesajları**: Repository ve servis log mesajları sadeleştirildi, gürültü azaltıldı
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Repositories/RepositoryLogMessages.cs` - Gürültülü log tanımları azaltıldı
    - `src/SmartRAG/Services/Database/DatabaseQueryExecutor.cs` - Küçük log temizliği
  - **Faydalar**: Üretim ortamlarında daha okunabilir ve daha az gürültülü log'lar

### 📝 Notlar
- **Geriye Dönük Uyumluluk**: Breaking change yok; tüm güncellemeler iç refactoring ve davranış koruyan iyileştirmelerdir
- **Kod Kalitesi**: 0 hata, 0 uyarı build politikası korunmuştur

## [3.8.0] - 2026-01-26

### ✨ Eklenenler
- **Schema RAG Implementasyonu**: Akıllı SQL üretimi için veritabanı şemalarının otomatik olarak vektörleştirilmiş chunk'lara dönüştürülmesi
  - Şema migrasyonu için yeni `ISchemaMigrationService` interface'i ve `SchemaMigrationService` implementasyonu
  - Veritabanı şemalarını vektörleştirilmiş doküman chunk'larına dönüştürmek için yeni `SchemaChunkService`
  - Semantik arama için embedding'lerle otomatik şema chunk üretimi
  - Metadata ile saklanan şema chunk'ları (`databaseId`, `databaseName`, `documentType: "Schema"`)
  - Tüm şemaları veya tek tek veritabanı şemalarını migrate etme desteği
  - Şema güncelleme işlevselliği (eski chunk'ları sil ve yeni oluştur)
  - Daha iyi sorgu eşleştirmesi için tablo ve kolon isimlerinden semantik anahtar kelime çıkarımı
  - PostgreSQL için identifier'lar için çift tırnak ile özel formatlama
  - Satır sayısına göre tablo tipi sınıflandırması (TRANSACTIONAL, LOOKUP, MASTER)
  - Chunk'larda kapsamlı foreign key ilişki dokümantasyonu
  - **Eklenen Dosyalar**:
    - `src/SmartRAG/Interfaces/Database/ISchemaMigrationService.cs` - Şema migrasyonu için interface
    - `src/SmartRAG/Services/Database/SchemaMigrationService.cs` - Şema migrasyon servisi implementasyonu
    - `src/SmartRAG/Services/Database/SchemaChunkService.cs` - Şema chunk dönüştürme servisi
  - **Faydalar**: Şema bilgilerinin semantik araması ile daha doğru SQL üretimi, daha iyi sorgu intent anlama

### 🔧 İyileştirmeler
- **SQL Sorgu Üretimi**: Daha iyi doğruluk için şema chunk entegrasyonu ile geliştirildi
  - Şema bilgileri artık RAG chunk'larından alınıyor (birincil kaynak)
  - Şema chunk'ları mevcut değilse `DatabaseSchemaInfo` fallback
  - Chunk'lardan şema context'i ile geliştirilmiş prompt oluşturma
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - Şema chunk entegrasyonu ile geliştirildi
    - `src/SmartRAG/Services/Database/Prompts/SqlPromptBuilder.cs` - Şema context'i ile geliştirilmiş prompt yapısı
  - **Faydalar**: Daha doğru SQL sorguları, veritabanı yapısının daha iyi anlaşılması

- **Veritabanı Bağlantı Yöneticisi**: Opsiyonel şema migrasyon servisi entegrasyonu eklendi
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Database/DatabaseConnectionManager.cs` - Şema migrasyon servisi desteği eklendi
  - **Faydalar**: Şema migrasyon yetenekleri ile daha iyi entegrasyon

- **Sonuç Birleştirici**: Daha iyi sonuç birleştirmesi için geliştirilmiş birleştirme mantığı
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Database/ResultMerger.cs` - Geliştirilmiş birleştirme mantığı
  - **Faydalar**: Birden fazla kaynaktan daha iyi sonuç birleştirmesi

- **Doküman Doğrulayıcı**: Şema dokümanları için geliştirilmiş doğrulama
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Helpers/DocumentValidator.cs` - Geliştirilmiş doğrulama mantığı
  - **Faydalar**: Şema dokümanlarının daha iyi doğrulanması

- **Servis Kaydı**: DI container'a şema migrasyon ve chunk servisleri eklendi
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Servis kayıtları eklendi
  - **Faydalar**: Uygun dependency injection kurulumu

- **Storage Factory**: Şema ile ilgili servisler için güncellendi
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Factories/StorageFactory.cs` - Factory yapılandırması güncellendi
  - **Faydalar**: Daha iyi factory entegrasyonu

- **Sorgu Stratejisi Yürütücü**: Şema-farkındalıklı sorgu yürütme ile geliştirildi
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Document/QueryStrategyExecutorService.cs` - Geliştirilmiş sorgu stratejisi
  - **Faydalar**: Daha iyi sorgu yönlendirme ve yürütme

- **Qdrant Koleksiyon Yöneticisi**: Şema doküman desteği için güncellendi
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Storage/Qdrant/QdrantCollectionManager.cs` - Geliştirilmiş koleksiyon yönetimi
  - **Faydalar**: Vektör deposunda şema dokümanları için daha iyi destek

### 📝 Notlar
- **Geriye Dönük Uyumluluk**: Tüm değişiklikler geriye dönük uyumludur
- **Geçiş**: Geçiş gerekli değil - mevcut kod değişiklik olmadan çalışmaya devam ediyor
- **Breaking Changes**: Yok
- **Kod Kalitesi**: 0 hata, 0 uyarı korundu
- **Schema RAG Pattern**: Şema bilgileri artık vektörleştirilmiş chunk'lar olarak saklanıyor, daha iyi SQL üretimi için semantik arama sağlıyor

## [3.6.0] - 2025-12-30

### ✨ Eklenenler
- **CancellationToken Desteği**: Tüm async metodlar ve interface'ler genelinde kapsamlı CancellationToken desteği
  - Tüm async interface metodları artık `CancellationToken cancellationToken = default` parametresi kabul ediyor
  - Özel helper metodlar iptal desteği için güncellendi
  - Daha iyi kaynak yönetimi ve zarif iptal işleme
  - CancellationToken içeren tüm metodlar için XML dokümantasyonu güncellendi
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Interfaces/` - Tüm async interface metodları güncellendi
    - `src/SmartRAG/Services/` - Tüm servis implementasyonları güncellendi
    - `src/SmartRAG/Repositories/` - Tüm repository implementasyonları güncellendi
    - `src/SmartRAG/Providers/` - Tüm provider implementasyonları güncellendi
  - **Faydalar**: Daha iyi kaynak yönetimi, zarif iptal, geliştirilmiş async/await desenleri

### 🔧 İyileştirmeler
- **Performans**: Task.Run native async dosya I/O metodları ile değiştirildi
  - Native async metodlar kullanılarak geliştirilmiş dosya I/O işlemleri
  - Daha iyi kaynak kullanımı ve azaltılmış overhead
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/FileWatcher/FileWatcherService.cs` - Native async I/O
    - `src/SmartRAG/Services/Document/DocumentService.cs` - Native async I/O
  - **Faydalar**: Daha iyi performans, azaltılmış bellek ayırma, geliştirilmiş ölçeklenebilirlik

- **Kod Kalitesi**: Gereksiz servis ve repository log'ları kaldırıldı
  - Servis katmanında aşırı loglama temizlendi
  - Gereksiz repository log'ları kaldırıldı
  - Log okunabilirliği ve gürültü azaltma iyileştirildi
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Shared/ServiceLogMessages.cs` - Log temizliği
    - `src/SmartRAG/Repositories/RepositoryLogMessages.cs` - Log temizliği
    - Birden fazla servis ve repository dosyası - Log kaldırma
  - **Faydalar**: Daha temiz log'lar, daha iyi performans, geliştirilmiş okunabilirlik

### 📝 Notlar
- **Geriye Dönük Uyumluluk**: Tüm CancellationToken parametreleri varsayılan değerlere sahip, tam geriye dönük uyumluluk sağlıyor
- **Geçiş**: Geçiş gerekli değil - mevcut kod değişiklik olmadan çalışmaya devam ediyor
- **Breaking Changes**: Yok
- **Kod Kalitesi**: 0 hata, 0 uyarı korundu

## [3.5.0] - 2025-12-27

### 🔧 İyileştirmeler
- **Kod Kalitesi**: SOLID/DRY uyumluluğu için servisler, provider'lar ve interface'ler genelinde kapsamlı refactoring
  - Geliştirilmiş kod organizasyonu ve sorumluluk ayrımı
  - Artırılmış bakım kolaylığı ve okunabilirlik
  - Daha iyi mimari desen implementasyonu
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/` - Birden fazla servis dosyası refactor edildi
    - `src/SmartRAG/Providers/` - Provider kod kalitesi iyileştirmeleri
    - `src/SmartRAG/Interfaces/` - Interface temizliği ve tutarlılık
  - **Faydalar**: Daha iyi bakım kolaylığı, daha temiz kod tabanı, geliştirilmiş test edilebilirlik

- **Interface Tutarlılığı**: İsimlendirme tutarlılığı için interface yeniden adlandırıldı
  - `ISQLQueryGenerator` → `ISqlQueryGenerator` (PascalCase isimlendirme kuralı)
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Interfaces/Database/ISqlQueryGenerator.cs` - Interface yeniden adlandırıldı
    - `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - Implementasyon güncellendi
    - `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Kayıt güncellendi
  - **Faydalar**: Tutarlı isimlendirme kuralları, daha iyi kod okunabilirliği
  - **Breaking Change**: Interface'i doğrudan kullananlar referansları güncellemeli

- **Kod Tekrarı Eliminasyonu**: Gereksiz wrapper metodları ve servisler kaldırıldı
  - Sadece diğer servislere delegate eden gereksiz wrapper metodları kaldırıldı
  - DocumentSearchService ve ilgili servislerde kod tekrarı elimine edildi
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Wrapper kaldırma
    - `src/SmartRAG/Services/Document/` - Birden fazla servis dosyası temizlendi
  - **Faydalar**: Azaltılmış kod karmaşıklığı, daha iyi performans, geliştirilmiş bakım kolaylığı

- **Arama Stratejisi**: Geliştirilmiş arama stratejisi implementasyonu ve kod kalitesi
  - Geliştirilmiş sorgu stratejisi mantığı
  - Strateji servislerinde daha iyi kod organizasyonu
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Document/QueryStrategyOrchestratorService.cs` - Strateji iyileştirmeleri
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Strateji optimizasyonu
  - **Faydalar**: Daha iyi sorgu yönlendirme, geliştirilmiş performans

- **PDF Ayrıştırma ve OCR**: Geliştirilmiş PDF ayrıştırma ve OCR sağlamlığı
  - PDF ayrıştırmada geliştirilmiş hata işleme
  - Daha iyi OCR işleme güvenilirliği
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Document/Parsers/PdfFileParser.cs` - Ayrıştırma iyileştirmeleri
    - `src/SmartRAG/Services/Parser/ImageParserService.cs` - OCR sağlamlığı
  - **Faydalar**: Daha güvenilir doküman işleme, daha iyi hata kurtarma

### ✨ Eklenenler
- **QueryIntentAnalysisResult Modeli**: Sorgu niyet sınıflandırma sonuçları için yeni model
  - Sorgu niyet analizi için yapılandırılmış sonuç modeli
  - Niyet sınıflandırma için daha iyi tip güvenliği
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Models/Results/QueryIntentAnalysisResult.cs` - Yeni model
  - **Faydalar**: Daha iyi tip güvenliği, geliştirilmiş kod netliği

- **SearchOptions Geliştirmeleri**: Factory metodları ve Clone metodu eklendi
  - Yapılandırmadan SearchOptions oluşturmak için `FromConfig()` factory metodu
  - SearchOptions kopyaları oluşturmak için `Clone()` metodu
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Models/Schema/SearchOptions.cs` - Factory ve Clone metodları
  - **Faydalar**: Daha kolay yapılandırma, daha iyi nesne yönetimi

- **QueryStrategyRequest Konsolidasyonu**: Birleştirilmiş sorgu stratejisi istek DTO'ları
  - Birden fazla sorgu stratejisi istek DTO'su tek `QueryStrategyRequest` modelinde birleştirildi
  - Basitleştirilmiş istek işleme
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Models/RequestResponse/QueryStrategyRequest.cs` - Birleştirilmiş model
  - **Faydalar**: Basitleştirilmiş API, daha iyi tutarlılık

### 🔄 Değişiklikler
- **Interface Metod İmzaları**: preferredLanguage parametresi kaldırıldı ve metod overload'ları birleştirildi
  - Interface metodlarından `preferredLanguage` parametresi kaldırıldı
  - Daha iyi API tutarlılığı için metod overload'ları birleştirildi
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Interfaces/Document/IDocumentSearchService.cs` - Metod imza güncellemeleri
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Implementasyon güncellemeleri
  - **Faydalar**: Daha temiz API, daha iyi tutarlılık
  - **Breaking Change**: `preferredLanguage` parametresini kullanan kod `SearchOptions` kullanmalı

- **Interface İsimlendirme**: ISQLQueryGenerator ISqlQueryGenerator olarak yeniden adlandırıldı
  - **Breaking Change**: Interface'i doğrudan kullananlar referansları güncellemeli
  - **Geçiş**: Kodunuzda `ISQLQueryGenerator` yerine `ISqlQueryGenerator` kullanın

### 🗑️ Kaldırılanlar
- **Kullanılmayan Servisler**: Kullanılmayan servis interface'leri ve implementasyonları kaldırıldı
  - `ISourceSelectionService` interface'i kaldırıldı
  - `SourceSelectionService` implementasyonu kaldırıldı
  - **Kaldırılan Dosyalar**:
    - `src/SmartRAG/Interfaces/Document/ISourceSelectionService.cs`
    - `src/SmartRAG/Services/Document/SourceSelectionService.cs`
  - **Faydalar**: Daha temiz kod tabanı, azaltılmış karmaşıklık

- **Gereksiz Wrapper'lar**: Gereksiz wrapper metodları ve orchestration servisleri kaldırıldı
  - Sadece diğer servislere delegate eden wrapper metodları kaldırıldı
  - Ek değer katmayan orchestration servisleri kaldırıldı
  - **Faydalar**: Azaltılmış kod karmaşıklığı, daha iyi performans

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

## [3.4.0] - 2025-12-12

### ✨ Eklenenler
- **MCP (Model Context Protocol) Entegrasyonu**: Gelişmiş arama yetenekleri için harici MCP sunucu entegrasyonu
  - MCP sunucu bağlantıları için `IMcpClient` interface'i ve `McpClient` servisi
  - Bağlantı yaşam döngüsü yönetimi için `IMcpConnectionManager` interface'i ve `McpConnectionManager` servisi
  - MCP sunucularını sorgulamak için `IMcpIntegrationService` interface'i ve `McpIntegrationService` servisi
  - Otomatik araç keşfi ile birden fazla MCP sunucusu desteği
  - Konuşma geçmişi bağlamı ile sorgu zenginleştirme
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Interfaces/Mcp/IMcpClient.cs` - MCP client interface
    - `src/SmartRAG/Interfaces/Mcp/IMcpConnectionManager.cs` - Connection manager interface
    - `src/SmartRAG/Interfaces/Mcp/IMcpIntegrationService.cs` - Integration service interface
    - `src/SmartRAG/Services/Mcp/McpClient.cs` - MCP client implementasyonu
    - `src/SmartRAG/Services/Mcp/McpConnectionManager.cs` - Connection manager implementasyonu
    - `src/SmartRAG/Services/Mcp/McpIntegrationService.cs` - Integration service implementasyonu
    - `src/SmartRAG/Models/Configuration/McpServerConfig.cs` - MCP sunucu yapılandırma modeli
    - `src/SmartRAG/Models/RequestResponse/McpRequest.cs` - MCP request modeli
    - `src/SmartRAG/Models/RequestResponse/McpResponse.cs` - MCP response modeli
    - `src/SmartRAG/Models/Results/McpTool.cs` - MCP tool modeli
    - `src/SmartRAG/Models/Results/McpToolResult.cs` - MCP tool result modeli
  - **Faydalar**: Genişletilebilir arama yetenekleri, harici veri kaynakları entegrasyonu, geliştirilmiş sorgu bağlamı

- **Dosya İzleyici Servisi**: İzlenen klasörlerden otomatik doküman indeksleme
  - `IFileWatcherService` interface'i ve `FileWatcherService` implementasyonu
  - Belirtilen klasörler için otomatik dosya izleme ve indeksleme
  - Bağımsız yapılandırmalarla birden fazla izlenen klasör desteği
  - İzlenen klasör başına dil-spesifik işleme
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Interfaces/FileWatcher/IFileWatcherService.cs` - Dosya izleyici interface
    - `src/SmartRAG/Services/FileWatcher/FileWatcherService.cs` - Dosya izleyici implementasyonu
    - `src/SmartRAG/Services/FileWatcher/Events/FileWatcherEventArgs.cs` - Dosya izleyici event argümanları
    - `src/SmartRAG/Models/Configuration/WatchedFolderConfig.cs` - İzlenen klasör yapılandırma modeli
  - **Faydalar**: Otomatik doküman indeksleme, manuel yüklemelerin azalması, gerçek zamanlı güncellemeler

- **DocumentType Özelliği**: İçerik tipine göre geliştirilmiş doküman chunk filtreleme
  - `DocumentChunk` entity'sine `DocumentType` özelliği eklendi (Document, Audio, Image)
  - Dosya uzantısı ve içerik tipine göre otomatik doküman tipi algılama
  - Arama işlemlerinde ses ve görüntü chunk'ları için filtreleme desteği
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Entities/DocumentChunk.cs` - DocumentType özelliği eklendi
    - `src/SmartRAG/Services/Document/DocumentParserService.cs` - Doküman tipi belirleme
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Doküman tipi filtreleme
    - `src/SmartRAG/Services/Document/DocumentSearchStrategyService.cs` - Tip tabanlı filtreleme
    - `src/SmartRAG/Repositories/QdrantDocumentRepository.cs` - Doküman tipi depolama
    - `src/SmartRAG/Services/Storage/Qdrant/QdrantSearchService.cs` - Doküman tipi alma
  - **Faydalar**: Daha iyi içerik tipi filtreleme, geliştirilmiş arama doğruluğu, geliştirilmiş chunk organizasyonu

- **DefaultLanguage Desteği**: Doküman işleme için global varsayılan dil yapılandırması
  - Varsayılan işleme dili ayarlamak için `SmartRagOptions` içinde `DefaultLanguage` özelliği
  - Dil belirtilmediğinde otomatik dil algılama fallback'i
  - ISO 639-1 dil kodları desteği (örn. "tr", "en", "de")
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Models/Configuration/SmartRagOptions.cs` - DefaultLanguage özelliği eklendi
    - `src/SmartRAG/Services/FileWatcher/FileWatcherService.cs` - Varsayılan dil kullanımı
    - `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Varsayılan dil yapılandırması
  - **Faydalar**: Tutarlı dil işleme, azaltılmış yapılandırma yükü, daha iyi çok dilli destek

- **Geliştirilmiş Arama Özellik Bayrakları**: Arama yetenekleri üzerinde granüler kontrol
  - MCP entegrasyon kontrolü için `EnableMcpSearch` bayrağı
  - Ses transkripsiyon araması için `EnableAudioSearch` bayrağı
  - Görüntü OCR araması için `EnableImageSearch` bayrağı
  - İstek başına ve global yapılandırma desteği
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Models/Schema/SearchOptions.cs` - EnableMcpSearch, EnableAudioSearch, EnableImageSearch bayrakları eklendi
    - `src/SmartRAG/Models/Configuration/SmartRagOptions.cs` - FeatureToggles'a özellik bayrakları eklendi
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Özellik bayrağı entegrasyonu
  - **Faydalar**: İnce taneli arama kontrolü, performans optimizasyonu, kaynak yönetimi

- **Erken Çıkış Optimizasyonu**: Doküman araması için performans iyileştirmesi
  - Yeterli yüksek kaliteli sonuç bulunduğunda erken çıkış
  - Net sonuçları olan sorgular için gereksiz işlemenin azaltılması
  - Geliştirilmiş performans için doküman araması ve sorgu intent analizinin paralel çalıştırılması
  - Veritabanı intent güveni yüksek olduğunda eager doküman cevap üretimini atlayan akıllı skip mantığı
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Paralel çalıştırma ile erken çıkış mantığı implementasyonu
    - `src/SmartRAG/Services/Document/QueryStrategyOrchestratorService.cs` - Strateji optimizasyonu
  - **Faydalar**: Daha hızlı arama yanıtları, azaltılmış kaynak kullanımı, geliştirilmiş kullanıcı deneyimi, optimize edilmiş sorgu işleme

- **SmartRagStartupService**: Başlatma için merkezi başlangıç servisi
  - Başlangıçta otomatik MCP sunucu bağlantısı
  - Dosya izleyici başlatma
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Startup/SmartRagStartupService.cs` - Başlangıç servisi implementasyonu
  - **Faydalar**: Basitleştirilmiş başlatma, daha iyi servis koordinasyonu

- **ClearAllConversationsAsync**: Konuşma geçmişi yönetimi geliştirmesi
  - `IConversationManagerService` ve `IConversationRepository`'ye `ClearAllConversationsAsync` metodu eklendi
  - Tüm depolama sağlayıcılarında tüm konuşma geçmişini temizleme desteği
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Interfaces/Support/IConversationManagerService.cs` - ClearAllConversationsAsync metodu eklendi
    - `src/SmartRAG/Services/Support/ConversationManagerService.cs` - ClearAllConversationsAsync implementasyonu
    - `src/SmartRAG/Interfaces/Storage/IConversationRepository.cs` - ClearAllConversationsAsync metodu eklendi
    - `src/SmartRAG/Repositories/FileSystemConversationRepository.cs` - ClearAllConversationsAsync implementasyonu
    - `src/SmartRAG/Repositories/InMemoryConversationRepository.cs` - ClearAllConversationsAsync implementasyonu
    - `src/SmartRAG/Repositories/RedisConversationRepository.cs` - ClearAllConversationsAsync implementasyonu
    - `src/SmartRAG/Repositories/SqliteConversationRepository.cs` - ClearAllConversationsAsync implementasyonu
  - **Faydalar**: Daha iyi konuşma yönetimi, toplu temizleme desteği, geliştirilmiş veri kontrolü

- **Arama Metadata Takibi**: Geliştirilmiş arama sonucu metadata'sı
  - Yanıtlarda arama metadata takibi ve görüntüleme
  - Metadata arama istatistikleri ve performans metriklerini içerir
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Interfaces/Document/IResponseBuilderService.cs` - Metadata desteği
    - `src/SmartRAG/Models/RequestResponse/RagResponse.cs` - Metadata özellikleri
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Metadata takibi
    - `src/SmartRAG/Services/Document/ResponseBuilderService.cs` - Metadata görüntüleme
  - **Faydalar**: Daha iyi arama görünürlüğü, performans izleme, geliştirilmiş hata ayıklama

- **IsExplicitlyNegative Kontrolü**: Negatif cevaplar için hızlı başarısızlık mekanizması
  - Açık başarısızlık pattern'lerini algılamak için `IResponseBuilderService` interface'ine `IsExplicitlyNegative` metodu eklendi
  - Açık başarısızlık algılama için `[NO_ANSWER_FOUND]` pattern desteği
  - Yüksek güvenli doküman eşleşmelerine rağmen AI'nın negatif cevaplar döndürmesi durumunda yanlış pozitifleri önler
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Interfaces/Document/IResponseBuilderService.cs` - IsExplicitlyNegative metodu eklendi
    - `src/SmartRAG/Services/Document/ResponseBuilderService.cs` - IsExplicitlyNegative implementasyonu
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Erken çıkış mantığında IsExplicitlyNegative kullanımı
    - `src/SmartRAG/Services/AI/PromptBuilderService.cs` - Prompt'lara [NO_ANSWER_FOUND] pattern'i eklendi
    - `src/SmartRAG/Services/Database/ResultMerger.cs` - Veritabanı prompt'larına [NO_ANSWER_FOUND] pattern'i eklendi
  - **Faydalar**: Daha doğru başarısızlık algılama, azaltılmış yanlış pozitifler, daha iyi sorgu stratejisi kararları

### 🔧 İyileştirilenler
- **Sorgu Stratejisi Optimizasyonu**: Akıllı kaynak seçimi ile geliştirilmiş sorgu çalıştırma stratejisi
  - `ResponseBuilderService`'i `IsExplicitlyNegative` metodunu tutarlı şekilde kullanacak şekilde refactor edildi
  - Daha iyi doküman önceliklendirmesi için `StrongDocumentMatchThreshold` (4.8) sabiti ile geliştirilmiş erken çıkış mantığı
  - Doküman eşleşme gücü ve AI cevap kalitesine dayalı geliştirilmiş veritabanı sorgu skip mantığı
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Document/ResponseBuilderService.cs` - Kod sadeleştirme ve tutarlılık iyileştirmeleri
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Sorgu stratejisi optimizasyonu
    - `src/SmartRAG/Services/Document/SourceSelectionService.cs` - Seçim mantığı iyileştirmeleri
  - **Faydalar**: Daha iyi sorgu performansı, daha doğru kaynak seçimi, azaltılmış gereksiz işleme
- **Kod Kalitesi**: Kod tabanı genelinde kapsamlı kod kalitesi iyileştirmeleri
  - Gereksiz yorumlar ve dil-spesifik referanslar kaldırıldı
  - Geliştirilmiş sabit isimlendirme ve generic kod pattern'leri
  - Geliştirilmiş kod organizasyonu ve yapısı
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/` - Birden fazla servis dosyası temizlendi
    - `src/SmartRAG/Repositories/` - Repository kod kalitesi iyileştirmeleri
    - `src/SmartRAG/Providers/` - Provider kod iyileştirmeleri
    - `src/SmartRAG/Interfaces/` - Interface temizliği
    - `src/SmartRAG/Helpers/QueryTokenizer.cs` - Kod kalitesi iyileştirmeleri
  - **Faydalar**: Daha iyi bakım kolaylığı, daha temiz kod tabanı, geliştirilmiş okunabilirlik

- **Model Organizasyonu**: Modeller mantıksal alt klasörlere yeniden organize edildi
  - Yapılandırma ile ilgili modeller için modeller `Configuration/` alt klasörüne taşındı
  - Request/response modelleri için modeller `RequestResponse/` alt klasörüne taşındı
  - Sonuç modelleri için modeller `Results/` alt klasörüne taşındı
  - Şema ile ilgili modeller için modeller `Schema/` alt klasörüne taşındı
  - **Değiştirilen Dosyalar**:
    - Birden fazla model dosyası alt klasörlere yeniden organize edildi
  - **Faydalar**: Daha iyi kod organizasyonu, daha kolay navigasyon, geliştirilmiş bakım kolaylığı

- **Dependency Injection**: Geliştirilmiş DI pattern'leri ve hata yönetimi
  - Daha iyi servis yaşam süresi yönetimi
  - Servis başlatmada geliştirilmiş hata yönetimi
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - DI iyileştirmeleri
    - Birden fazla servis dosyası - Hata yönetimi iyileştirmeleri
  - **Faydalar**: Daha güvenilir servis başlatma, daha iyi hata kurtarma

- **Görüntü Ayrıştırma ve Bağlam Genişletme**: Geliştirilmiş görüntü işleme yetenekleri
  - Görüntü chunk'ları için geliştirilmiş bağlam genişletme
  - Daha iyi görüntü ayrıştırma hata yönetimi
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Parser/ImageParserService.cs` - Görüntü ayrıştırma iyileştirmeleri
    - `src/SmartRAG/Services/Search/ContextExpansionService.cs` - Bağlam genişletme iyileştirmeleri
  - **Faydalar**: Daha iyi görüntü içerik çıkarma, geliştirilmiş OCR doğruluğu

- **Veritabanı Sorgu Hata Yönetimi**: Geliştirilmiş hata yönetimi ve yanıt doğrulama
  - Veritabanı sorgu hataları için daha iyi hata mesajları
  - Geliştirilmiş yanıt doğrulama
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Database/MultiDatabaseQueryCoordinator.cs` - Hata yönetimi iyileştirmeleri
  - **Faydalar**: Daha iyi hata tanılama, geliştirilmiş güvenilirlik

- **Eksik Veri Algılama**: Dil-agnostik eksik veri algılama
  - Eksik veri göstergeleri için geliştirilmiş pattern eşleştirme
  - Eksik veri algılama için generic dil desteği
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Database/MultiDatabaseQueryCoordinator.cs` - Eksik veri algılama iyileştirmeleri
  - **Faydalar**: Daha iyi veri kalitesi algılama, dil-agnostik pattern'ler

### 🐛 Düzeltilenler
- **Dil-Agnostik Eksik Veri Algılama**: Eksik veri algılamada dil-spesifik pattern'ler düzeltildi
  - Hardcoded dil-spesifik pattern'ler kaldırıldı
  - Generic eksik veri algılama pattern'leri implementasyonu
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Database/MultiDatabaseQueryCoordinator.cs` - Dil-agnostik algılama
  - **Faydalar**: Tüm dillerle çalışır, daha iyi pattern eşleştirme

- **HttpClient Timeout**: Uzun süren AI işlemleri için timeout artırıldı
  - `GenerateTextAsync` işlemleri için timeout 10 dakikaya çıkarıldı
  - Karmaşık sorgular için erken timeout'u önler
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Providers/BaseAIProvider.cs` - Timeout yapılandırması
  - **Faydalar**: Uzun süren işlemlerin daha iyi yönetimi, azaltılmış timeout hataları

- **Türkçe Karakter Kodlaması**: PDF metin çıkarmada kodlama sorunları düzeltildi
  - Türkçe karakterler için geliştirilmiş karakter kodlama işleme
  - PDF ayrıştırmada daha iyi Unicode desteği
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Document/Parsers/PdfFileParser.cs` - Kodlama iyileştirmeleri
  - **Faydalar**: Türkçe dokümanlar için daha iyi metin çıkarma, geliştirilmiş çok dilli destek

- **Chunk0 Alma**: Numaralandırılmış liste işleme chunk alma düzeltildi
  - Numaralandırılmış liste işlemede chunk0 alma mantığı düzeltildi
  - Numaralandırılmış listeler için geliştirilmiş bağlam genişletme
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Chunk alma düzeltmesi
  - **Faydalar**: Daha iyi numaralandırılmış liste işleme, geliştirilmiş bağlam doğruluğu

- **DI Scope Sorunları**: Dependency injection scope çakışmaları çözüldü
  - Döngüsel bağımlılık sorunları düzeltildi
  - Geliştirilmiş servis başlatma sırası
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - DI scope düzeltmeleri
  - **Faydalar**: Daha güvenilir servis başlatma, daha iyi hata yönetimi

- **İçerik Tipi Algılama**: Geliştirilmiş içerik tipi algılama doğruluğu
  - Daha iyi MIME tipi algılama
  - Geliştirilmiş dosya uzantısı eşleştirme
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Document/DocumentParserService.cs` - İçerik tipi algılama iyileştirmeleri
  - **Faydalar**: Daha doğru doküman tipi algılama, daha iyi dosya işleme

- **Konuşma Niyet Sınıflandırması**: Geliştirilmiş bağlam farkındalığı
  - Daha iyi bağlam anlayışı ile geliştirilmiş konuşma niyet sınıflandırması
  - Geliştirilmiş sorgu niyet algılama doğruluğu
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Support/QueryIntentClassifierService.cs` - Bağlam-farkında sınıflandırma
  - **Faydalar**: Daha iyi niyet algılama, geliştirilmiş konuşma akışı, geliştirilmiş doğruluk

### 🐛 Düzeltilenler
- **Konuşma Geçmişi Tekrarlanan Girdiler**: Konuşma geçmişinde tekrarlanan girdiler düzeltildi
  - Tüm depolama sağlayıcılarında tekrarlanan konuşma geçmişi girdileri çözüldü
  - Geliştirilmiş konuşma geçmişi kesme mantığı
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Interfaces/Storage/IConversationRepository.cs` - Kesme desteği
    - `src/SmartRAG/Repositories/FileSystemConversationRepository.cs` - Tekrar önleme
    - `src/SmartRAG/Repositories/InMemoryConversationRepository.cs` - Tekrar önleme
    - `src/SmartRAG/Repositories/RedisConversationRepository.cs` - Tekrar önleme
    - `src/SmartRAG/Repositories/SqliteConversationRepository.cs` - Tekrar önleme
    - `src/SmartRAG/Services/AI/PromptBuilderService.cs` - Kesme iyileştirmeleri
    - `src/SmartRAG/Services/Support/ConversationManagerService.cs` - Geçmiş yönetimi
  - **Faydalar**: Daha temiz konuşma geçmişi, azaltılmış depolama kullanımı, daha iyi performans

- **Redis Doküman Alma**: Doküman listesi boş olduğunda doküman alma düzeltildi
  - Redis'te doküman listesi boş olduğunda chunk'lardan doküman alma iyileştirildi
  - Geliştirilmiş doküman alma için fallback mekanizması
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Repositories/RedisDocumentRepository.cs` - Doküman alma iyileştirmeleri
  - **Faydalar**: Daha iyi doküman erişimi, geliştirilmiş güvenilirlik, geliştirilmiş veri tutarlılığı

- **SqlValidator DI Uyumluluğu**: Dependency injection uyumluluğu düzeltildi
  - `SqlValidator`'ın doğru DI uyumluluğu için `ILogger<SqlValidator>` kullanması sağlandı
  - Geliştirilmiş servis kaydı ve yaşam süresi yönetimi
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Database/Validation/SqlValidator.cs` - DI uyumluluk düzeltmesi
  - **Faydalar**: Daha iyi DI entegrasyonu, geliştirilmiş servis kaydı, geliştirilmiş bakım kolaylığı

### 🔄 Değiştirilenler
- **Özellik Bayrağı İsimlendirme**: Tutarlılık için özellik bayrakları yeniden adlandırıldı
  - `EnableMcpClient` → `EnableMcpSearch`
  - `EnableAudioParsing` → `EnableAudioSearch`
  - `EnableImageParsing` → `EnableImageSearch`
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Models/Schema/SearchOptions.cs` - Bayrak yeniden adlandırma
    - `src/SmartRAG/Models/Configuration/SmartRagOptions.cs` - Bayrak yeniden adlandırma
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Bayrak kullanım güncellemeleri
  - **Faydalar**: Tutarlı isimlendirme, daha net semantik

- **Interface Yeniden Yapılandırma**: Daha iyi organizasyon için interface'ler yeniden organize edildi
  - MCP interface'leri `Interfaces/Mcp/` klasörüne taşındı
  - Dosya izleyici interface'leri `Interfaces/FileWatcher/` klasörüne taşındı
  - **Değiştirilen Dosyalar**:
    - Birden fazla interface dosyası yeniden organize edildi
  - **Faydalar**: Daha iyi kod organizasyonu, daha kolay navigasyon

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

## [3.3.0] - 2025-12-01

### ✨ Eklenenler
- **ConversationStorageProvider Ayrımı**: Konuşma depolaması doküman depolamasından ayrıldı
  - Konuşma geçmişi depolaması için yeni `ConversationStorageProvider` enum'u (Redis, SQLite, FileSystem, InMemory)
  - `StorageProvider` artık sadece doküman/vektör depolaması için kullanılıyor (InMemory, Redis, Qdrant)
  - Konuşma ve doküman depolaması için bağımsız yapılandırma
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Enums/ConversationStorageProvider.cs` - Konuşma depolaması için yeni enum
    - `src/SmartRAG/Enums/StorageProvider.cs` - Konuşma ile ilgili provider'lar kaldırıldı (SQLite, FileSystem)
    - `src/SmartRAG/Models/SmartRagOptions.cs` - ConversationStorageProvider özelliği eklendi
    - `src/SmartRAG/Factories/StorageFactory.cs` - Konuşma ve doküman repository'leri için ayrı metodlar
    - `src/SmartRAG/Interfaces/Storage/IStorageFactory.cs` - CreateConversationRepository metodu eklendi
    - `src/SmartRAG/Services/Support/ConversationManagerService.cs` - ConversationStorageProvider kullanımı için güncellendi
  - **Faydalar**: Net separation of concerns, bağımsız ölçeklendirme, daha iyi mimari
- **Redis RediSearch Entegrasyonu**: RediSearch modül desteği ile geliştirilmiş vektör benzerlik araması
  - Gelişmiş vektör arama yetenekleri için RediSearch modül desteği
  - Vektör indeks algoritması yapılandırması (HNSW)
  - Mesafe metrik yapılandırması (COSINE)
  - Vektör boyut yapılandırması (varsayılan: 768)
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Models/RedisConfig.cs` - Vektör arama yapılandırma özellikleri eklendi
    - `src/SmartRAG/Repositories/RedisDocumentRepository.cs` - RediSearch vektör arama implementasyonu

### 🔧 İyileştirilenler
- **Redis Vektör Arama**: DocumentSearchService için doğru relevance score hesaplama ve atama
  - RelevanceScore artık RedisDocumentRepository'de doğru şekilde ranking için ayarlanıyor
  - RediSearch mesafe metriklerinden benzerlik skoru hesaplama
  - Skor doğrulama için debug logging
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Repositories/RedisDocumentRepository.cs` - RelevanceScore atama

- **Redis Embedding Üretimi**: Embedding üretimi için doğru AIProviderConfig geçişi
  - Doğru config alımı için IAIConfigurationService injection
  - Config eksik olduğunda null kontrolü ve text search'e fallback
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Repositories/RedisDocumentRepository.cs` - AI config handling
    - `src/SmartRAG/Factories/StorageFactory.cs` - IAIConfigurationService injection

- **StorageFactory Dependency Injection**: IAIProvider ile scope sorunları çözüldü
  - Lazy resolution için IServiceProvider kullanımına geçildi
  - Singleton/Scoped lifetime uyumsuzluğunu önler
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Factories/StorageFactory.cs` - Lazy dependency resolution
    - `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - IAIProvider lifetime ayarlaması

### 🐛 Düzeltilenler
- **StorageFactory DI Scope Sorunu**: IAIProvider çözülürken InvalidOperationException düzeltildi
  - Doğrudan injection'dan IServiceProvider aracılığıyla lazy resolution'a geçildi
  - Singleton factory'nin Scoped service inject etmeye çalışmasını önler

- **Redis Relevance Scoring**: Arama sonuçlarında RelevanceScore'un 0.0000 olması düzeltildi
  - RelevanceScore artık benzerlik hesaplamasından doğru şekilde atanıyor
  - DocumentSearchService sonuçları doğru şekilde sıralayabiliyor

- **Redis Embedding Config**: Embedding üretirken NullReferenceException düzeltildi
  - AIProviderConfig artık doğru şekilde alınıyor ve GenerateEmbeddingAsync'e geçiriliyor
  - Config mevcut olmadığında zarif text search fallback'i

### 🗑️ Kaldırılanlar
- **FileSystemDocumentRepository**: Kullanılmayan dosya sistemi depolama implementasyonu kaldırıldı
  - Repository dosyası silindi (388 satır kaldırıldı)
  - **Kaldırılan Dosyalar**:
    - `src/SmartRAG/Repositories/FileSystemDocumentRepository.cs`

- **SqliteDocumentRepository**: Kullanılmayan SQLite depolama implementasyonu kaldırıldı
  - Repository dosyası silindi (618 satır kaldırıldı)
  - **Kaldırılan Dosyalar**:
    - `src/SmartRAG/Repositories/SqliteDocumentRepository.cs`

- **StorageConfig Özellikleri**: Kullanılmayan yapılandırma özellikleri kaldırıldı
  - FileSystemPath özelliği kaldırıldı
  - SqliteConfig özelliği kaldırıldı
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Models/StorageConfig.cs` - Özellik kaldırma

### ✨ Faydalar
- **Geliştirilmiş Redis Vektör Arama**: Doğru benzerlik skorlama ve relevance ranking
- **Daha İyi Geliştirici Deneyimi**: RediSearch gereksinimleri için net uyarılar ve dokümantasyon
- **Daha Temiz Kod Tabanı**: 1000+ satır kullanılmayan kod kaldırıldı
- **Geliştirilmiş Güvenilirlik**: DI scope sorunları ve null reference exception'ları düzeltildi

### 📝 Notlar
- **Breaking Changes**: FileSystem ve SQLite doküman repository'leri kaldırıldı
  - Bunlar kullanılmayan implementasyonlardı
  - Aktif depolama provider'ları (Qdrant, Redis, InMemory) tamamen çalışır durumda
  - FileSystem veya SQLite kullanıyorsanız, Qdrant, Redis veya InMemory'ye geçin

- **Redis Gereksinimleri**: Vektör arama RediSearch modülü gerektirir
  - `redis/redis-stack-server:latest` Docker image'ını kullanın
  - Veya Redis sunucunuza RediSearch modülünü kurun
  - RediSearch olmadan sadece text search çalışır (vektör arama çalışmaz)

## [3.2.0] - 2025-11-27

### Performans İyileştirmeleri
- **AI Sorgu Niyeti Analizi Optimizasyonu**: Pre-analyzed query intent kabul eden overload method ekleyerek gereksiz AI çağrılarını ortadan kaldırdı
  - `IMultiDatabaseQueryCoordinator.QueryMultipleDatabasesAsync(string, QueryIntent, int)` - Gereksiz AI analizini önlemek için yeni overload method
  - `DocumentSearchService` artık pre-analyzed query intent'i `MultiDatabaseQueryCoordinator`'a geçirerek duplicate AI çağrılarını önlüyor
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Interfaces/Database/IMultiDatabaseQueryCoordinator.cs` - Pre-analyzed intent parametreli overload method eklendi
    - `src/SmartRAG/Services/Database/MultiDatabaseQueryCoordinator.cs` - Null safety validation ile overload method implementasyonu
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Pre-analyzed query intent'i coordinator'a geçirmek için güncellendi

### Düzeltilenler
- **SQL Sorgu Validasyonu**: GROUP BY sorgularında SELECT alias'larını doğru şekilde işlemek için ORDER BY alias validasyonu düzeltildi
  - Validasyon artık ORDER BY clause'larında SELECT alias'larını (örn. `SUM(Quantity) AS TotalQuantity`) tanıyor
  - Önceden ORDER BY'da aggregate alias kullanımını hata olarak işaretliyordu
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - SELECT alias'larını extract ve validate eden geliştirilmiş validasyon mantığı

### İyileştirilenler
- **Cross-Database Query Prompt İyileştirmesi**: Cross-database query'ler için AI prompt rehberliği iyileştirildi
  - Veritabanları arası ilişkileri işlemek için daha net örnekler eklendi (örn. "en çok satılan kategori" sales data + category names gerektirir)
  - Application-level merging için foreign key ve aggregate döndürme rehberliği geliştirildi
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - AI prompt'larında cross-database query pattern örnekleri güncellendi

### Değiştirilenler
- **Kod Mimari Refactoring**: Servisler ve interface'ler daha iyi organizasyon ve bakım kolaylığı için modüler klasör yapısına yeniden organize edildi
  - Interface'ler kategorilere göre organize edildi: `AI/`, `Database/`, `Document/`, `Parser/`, `Search/`, `Storage/`, `Support/`
  - Servisler kategorilere göre organize edildi: `AI/`, `Database/`, `Document/`, `Parser/`, `Search/`, `Storage/Qdrant/`, `Support/`, `Shared/`
  - Namespace'ler güncellendi: `SmartRAG.Interfaces` → `SmartRAG.Interfaces.{Category}`, `SmartRAG.Services` → `SmartRAG.Services.{Category}`
  - Dosya yolları güncellendi:
    - `src/SmartRAG/Services/MultiDatabaseQueryCoordinator.cs` → `src/SmartRAG/Services/Database/MultiDatabaseQueryCoordinator.cs`
    - `src/SmartRAG/Services/DocumentSearchService.cs` → `src/SmartRAG/Services/Document/DocumentSearchService.cs`
    - `src/SmartRAG/Services/AIService.cs` → `src/SmartRAG/Services/AI/AIService.cs`
    - `src/SmartRAG/Services/SemanticSearchService.cs` → `src/SmartRAG/Services/Search/SemanticSearchService.cs`
    - Tüm interface'ler `src/SmartRAG/Interfaces/` → `src/SmartRAG/Interfaces/{Category}/` taşındı
  - **Breaking Changes**: Namespace değişiklikleri tüketen kodda using statement güncellemeleri gerektirebilir
  - **Faydalar**: Daha iyi kod organizasyonu, geliştirilmiş bakım kolaylığı, daha net separation of concerns

### Eklenenler
- **Birleşik Sorgu Zekası**: `QueryIntelligenceAsync` artık veritabanları, dokümanlar, görseller (OCR) ve ses (transkripsiyon) üzerinde tek bir sorguda birleşik arama destekliyor
- **Akıllı Hibrit Yönlendirme**: Güven skorlaması ile AI tabanlı niyet tespiti otomatik olarak optimal arama stratejisini belirler
  - Yüksek güven (>0.7) + veritabanı sorguları → Sadece veritabanı sorgusu
  - Yüksek güven (>0.7) + veritabanı sorgusu yok → Sadece doküman sorgusu
  - Orta güven (0.3-0.7) → Hem veritabanı hem doküman sorguları, birleştirilmiş sonuçlar
  - Düşük güven (<0.3) → Sadece doküman sorgusu (yedek)
- **QueryStrategy Enum**: Sorgu yürütme stratejileri için yeni enum (DatabaseOnly, DocumentOnly, Hybrid)

### Değiştirilenler
- `QueryIntelligenceAsync` metodu artık doküman sorgularının yanı sıra veritabanı sorgularını da entegre ediyor
- Zarif bozulma ve yedek mekanizmalarla geliştirilmiş sorgu yönlendirme mantığı
- Veritabanı sorgu hataları için geliştirilmiş hata yönetimi

### Notlar
- Geriye dönük uyumlu: Mevcut `QueryIntelligenceAsync` imzası değişmedi
- Veritabanı koordinatörü mevcut değilse, davranış önceki implementasyonla aynı
- `RagResponse` modelinde breaking change yok

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
- **Akıllı Yönlendirme**: Zarif bozulma ve yedek mekanizmalarla geliştirilmiş sorgu yönlendirme mantığı
- **Geliştirilmiş Hata Yönetimi**: Veritabanı sorgu hataları için daha iyi hata yönetimi

#### **Yeni Servisler & Interface'ler**
- `src/SmartRAG/Services/Database/QueryIntentAnalyzer.cs` - Kullanıcı sorgularını analiz eder ve hangi veritabanları/tabloları sorgulayacağını AI kullanarak belirler
- `src/SmartRAG/Services/Database/DatabaseQueryExecutor.cs` - Daha iyi performans için birden fazla veritabanında paralel sorgu yürütür
- `src/SmartRAG/Services/Database/ResultMerger.cs` - Birden fazla veritabanından gelen sonuçları AI kullanarak tutarlı yanıtlara birleştirir
- `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - Sorgu niyetine göre her veritabanı için optimize edilmiş SQL sorguları üretir
- `src/SmartRAG/Interfaces/Database/IQueryIntentAnalyzer.cs` - Sorgu niyet analizi için interface
- `src/SmartRAG/Interfaces/Database/IDatabaseQueryExecutor.cs` - Çoklu-veritabanı sorgu yürütme için interface
- `src/SmartRAG/Interfaces/Database/IResultMerger.cs` - Sonuç birleştirme için interface
- `src/SmartRAG/Interfaces/Database/ISQLQueryGenerator.cs` - SQL sorgu üretimi için interface

#### **Yeni Enum'lar**
- `src/SmartRAG/Enums/QueryStrategy.cs` - Sorgu yürütme stratejileri için yeni enum (DatabaseOnly, DocumentOnly, Hybrid)

#### **Yeni Modeller**
- `src/SmartRAG/Models/AudioSegmentMetadata.cs` - Zaman damgaları ve güven skorları ile ses transkripsiyon segmentleri için metadata modeli

#### **Geliştirilmiş Modeller**
- `src/SmartRAG/Models/SearchSource.cs` - Kaynak tipi farklılaştırması ile geliştirildi (Database, Document, Image, Audio)

#### **Değiştirilen Dosyalar**
- `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Büyük refactoring: Hibrit yönlendirme ile birleşik sorgu zekası implementasyonu (918+ satır değişiklik)
- `src/SmartRAG/Services/Database/MultiDatabaseQueryCoordinator.cs` - Daha iyi separation of concerns için yeni servis mimarisini kullanacak şekilde refactor edildi (355+ satır değişiklik)
- `src/SmartRAG/Services/AI/AIService.cs` - Daha iyi hata yönetimi ile geliştirilmiş AI servisi
- `src/SmartRAG/Services/Document/DocumentParserService.cs` - Ses segment metadata desteği ile geliştirilmiş doküman ayrıştırma
- `src/SmartRAG/Interfaces/Document/IDocumentSearchService.cs` - Interface dokümantasyonu güncellendi
- `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - DI container'da yeni servisler kaydedildi

### 🔧 Kod Kalitesi & AI Prompt Optimizasyonu

#### **Kod Kalitesi İyileştirmeleri**
- **Build Kalitesi**: Tüm projelerde 0 hata, 0 uyarı elde edildi
- **Kod Standartları**: Proje kod standartlarına tam uyumluluk

#### **AI Prompt Optimizasyonu**
- **Emoji Azaltma**: AI prompt'larındaki emoji kullanımı 235'ten 5'e düşürüldü (sadece kritik: 🚨, ✓, ✗)
- **Token Verimliliği**: Token verimliliği iyileştirildi (prompt başına ~100 token tasarruf)
- **Stratejik Kullanım**: Stratejik emoji kullanımı ile daha iyi AI anlayışı

#### **Değiştirilen Dosyalar**
- `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - AI prompt'larında emoji optimizasyonu
- `src/SmartRAG/Services/Database/MultiDatabaseQueryCoordinator.cs` - Emoji optimizasyonu
- `src/SmartRAG/Services/Database/QueryIntentAnalyzer.cs` - Emoji optimizasyonu
- `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Emoji optimizasyonu

### ✨ Faydalar
- **Tek Sorgu Arayüzü**: Tüm veri kaynaklarını (veritabanları, dokümanlar, görseller, ses) tek bir metodla sorgula
- **Akıllı Yönlendirme**: AI sorgu niyetine ve güven skorlamasına göre otomatik olarak en iyi arama stratejisini seçer
- **Paralel Yürütme**: Daha iyi performans için çoklu-veritabanı sorguları paralel olarak yürütülür
- **Modüler Mimari**: Yeni servis tabanlı mimari bakım kolaylığı ve test edilebilirliği artırır
- **Daha İyi Separation of Concerns**: Her servisin tek bir sorumluluğu var (SOLID prensipleri)
- **Temiz Kod Tabanı**: Tüm projelerde sıfır uyarı
- **Daha İyi Performans**: Daha verimli AI prompt işleme ve paralel sorgu yürütme
- **Geliştirilmiş Bakım Kolaylığı**: Daha iyi kod kalitesi ve standart uyumluluğu
- **Maliyet Verimliliği**: AI prompt'larında azaltılmış token kullanımı (prompt başına ~100 token tasarruf)

### 📝 Notlar
- Geriye dönük uyumlu: Mevcut `QueryIntelligenceAsync` imzası değişmedi
- Veritabanı koordinatörü mevcut değilse, davranış önceki implementasyonla aynı
- `RagResponse` modelinde breaking change yok

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

## [3.0.2] - 2025-10-24

### 🚀 BREAKING CHANGES - Google Speech-to-Text Kaldırıldı

#### **Ses İşleme Değişiklikleri**
- **Google Speech-to-Text Kaldırıldı**: Google Cloud Speech-to-Text entegrasyonunun tamamen kaldırılması
- **Sadece Whisper.net**: Ses transkripsiyonu artık sadece Whisper.net kullanıyor, %100 yerel işleme
- **Veri Gizliliği**: Tüm ses işleme artık tamamen yerel, GDPR/KVKK/HIPAA uyumluluğu sağlanıyor
- **Basitleştirilmiş Yapılandırma**: GoogleSpeechConfig ve ilgili yapılandırma seçenekleri kaldırıldı

#### **Kaldırılan Dosyalar**
- `src/SmartRAG/Services/GoogleAudioParserService.cs` - Google Speech-to-Text servisi
- `src/SmartRAG/Models/GoogleSpeechConfig.cs` - Google Speech yapılandırma modeli

#### **Değiştirilen Dosyalar**
- `src/SmartRAG/SmartRAG.csproj` - Google.Cloud.Speech.V1 NuGet paketi kaldırıldı
- `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Google servis kaydı kaldırıldı
- `src/SmartRAG/Factories/AudioParserFactory.cs` - Sadece Whisper.net için basitleştirildi
- `src/SmartRAG/Models/SmartRagOptions.cs` - GoogleSpeechConfig özelliği kaldırıldı
- `src/SmartRAG/Enums/AudioProvider.cs` - GoogleCloud enum değeri kaldırıldı
- `src/SmartRAG/Services/ServiceLogMessages.cs` - Whisper.net için log mesajları güncellendi

#### **Dokümantasyon Güncellemeleri**
- **README.md**: Whisper.net-only ses işleme için güncellendi
- **README.tr.md**: Türkçe dokümantasyon güncellendi
- **docs/**: Tüm dokümantasyon dosyalarından Google Speech referansları kaldırıldı
- **Examples**: Örnek yapılandırmalar ve dokümantasyon güncellendi

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
1. Yapılandırmanızdan GoogleSpeechConfig'i kaldırın
2. WhisperConfig'in doğru yapılandırıldığından emin olun
3. Özel ses işleme kodunuzu Whisper.net kullanacak şekilde güncelleyin
4. Yerel Whisper.net modelleri ile ses transkripsiyonunu test edin

## [3.0.1] - 2025-10-22

### 🐛 Düzeltildi
- **LoggerMessage Parametre Uyumsuzluğu**: `LogAudioServiceInitialized` LoggerMessage tanımında eksik `configPath` parametresi düzeltildi
- **EventId Çakışmaları**: ServiceLogMessages.cs'deki çakışan EventId atamaları çözüldü (6006, 6008, 6009)
- **Logo Görüntüleme Sorunu**: NuGet'te görüntüleme sorunlarına neden olan README dosyalarındaki bozuk logo referansları kaldırıldı
- **TypeInitializationException**: Kritik başlatma hatası düzeltildi

### 🔧 Teknik İyileştirmeler
- **ServiceLogMessages.cs**: LoggerMessage tanımları parametre sayılarıyla doğru eşleşecek şekilde güncellendi
- **EventId Yönetimi**: Benzersiz log tanımlayıcıları için çakışan EventId'ler yeniden atandı
- **Dokümantasyon**: Daha iyi NuGet paket görüntüleme için README dosyaları temizlendi

## [3.0.0] - 2025-10-22

### 🚀 BREAKING CHANGES - Zeka Kütüphanesi Devrimi

#### **Framework Gereksinimleri**
- **Minimum .NET Versiyonu**: Artık .NET Standard 2.1 (.NET Core 3.0+) gerektiriyor
- **Destek Kaldırıldı**: .NET Framework 4.x ve .NET Standard 2.0 artık desteklenmiyor
- **Neden**: Modern API özelliklerini etkinleştirmek, daha iyi performans ve mevcut AI provider SDK gereksinimleriyle uyum
- **Uyumlu**: .NET Core 3.0+, .NET 5, .NET 6, .NET 7, .NET 8, .NET 9

#### **Önemli API Değişiklikleri**
- **`GenerateRagAnswerAsync` → `QueryIntelligenceAsync`**: Akıllı sorgu işlemeyi daha iyi temsil etmek için metod yeniden adlandırıldı
- **Geliştirilmiş `IDocumentSearchService` interface'i**: Gelişmiş RAG pipeline ile yeni akıllı sorgu işleme metodu
- **Servis katmanı iyileştirmeleri**: Gelişmiş anlamsal arama ve konuşma yönetimi
- **Geriye dönük uyumluluk korundu**: Eski metodlar kullanımdan kaldırıldı olarak işaretlendi (v4.0.0'da kaldırılacak)

### 🔧 SQL Üretimi & Çok Dilli Destek

#### **Dil-Güvenli SQL Üretimi**
- **Otomatik doğrulama**: SQL sorgularında İngilizce olmayan metnin tespiti ve önlenmesi
- **Geliştirilmiş SQL doğrulaması**: SQL'de Türkçe/Almanca/Rusça karakterleri ve anahtar kelimeleri önleyen katı doğrulama
- **Çok dilli sorgu desteği**: AI, herhangi bir dilde sorguları işlerken saf İngilizce SQL üretir
- **Karakter doğrulaması**: İngilizce olmayan karakterleri tespit eder (Türkçe: ç, ğ, ı, ö, ş, ü; Almanca: ä, ö, ü, ß; Rusça: Kiril)
- **Anahtar kelime doğrulaması**: SQL'de İngilizce olmayan anahtar kelimeleri önler (sorgu, abfrage, запрос)
- **İyileştirilmiş hata mesajları**: Hata raporlarında veritabanı tipi bilgisiyle daha iyi tanılama

#### **PostgreSQL Tam Desteği**
- **Eksiksiz entegrasyon**: Canlı bağlantılarla tam PostgreSQL desteği
- **Şema analizi**: Akıllı şema çıkarma ve ilişki haritalama
- **Çoklu-veritabanı sorguları**: PostgreSQL ile çapraz-veritabanı sorgu koordinasyonu
- **Üretime hazır**: Kapsamlı test ve doğrulama

### 🔒 On-Premise & Şirket İçi AI Desteği

#### **Tam On-Premise İşlem**
- **On-premise AI modelleri**: Ollama, LM Studio ve herhangi bir OpenAI-uyumlu on-premise API için tam destek
- **Doküman işleme**: PDF, Word, Excel ayrıştırma - tamamen on-premise
- **OCR işleme**: Tesseract 5.2.0 - tamamen on-premise, buluta veri gönderilmez
- **Veritabanı entegrasyonu**: SQLite, SQL Server, MySQL, PostgreSQL - tüm on-premise bağlantılar
- **Depolama seçenekleri**: In-Memory, SQLite, FileSystem, Redis - tümü on-premise
- **Tam gizlilik**: Verileriniz altyapınızda kalır

#### **Kurumsal Uyumluluk**
- **GDPR uyumlu**: Tüm verileri altyapınızda tutun
- **KVKK uyumlu**: Türk veri koruma kanunu uyumluluğu
- **Hava boşluklu sistemler**: İnternetsiz çalışır (ses transkripsiyonu hariç)
- **Finansal kurumlar**: On-premise dağıtım ile banka düzeyinde güvenlik
- **Sağlık**: HIPAA uyumlu dağıtımlar mümkün
- **Devlet**: On-premise modellerle gizli veri işleme

### ⚠️ Önemli Kısıtlamalar

#### **Ses Dosyaları**
- **Whisper.net**: Ses transkripsiyonu artık sadece Whisper.net kullanıyor, %100 yerel işleme
- **Veri gizliliği**: Whisper.net sesi yerel olarak işler
- **Çok dilli**: Otomatik algılama ile 99+ dil desteği
- **Diğer formatlar**: Diğer tüm dosya tipleri tamamen yerel kalır

#### **OCR (Görsel'den Metne)**
- **El yazısı kısıtlaması**: Tesseract OCR el yazısını tam olarak destekleyemez (düşük başarı oranı)
- **Mükemmel çalışır**: Basılı dokümanlar, taranmış basılı dokümanlar, yazılmış metinli dijital ekran görüntüleri
- **Sınırlı destek**: El yazısı notları, formlar, bitişik yazı (çok düşük doğruluk)
- **En iyi sonuçlar**: Basılı dokümanların yüksek kaliteli taramaları
- **100+ dil**: [Desteklenen tüm dilleri görüntüle](https://github.com/tesseract-ocr/tessdata)

### ✨ Eklenenler
- **Çok dilli README**: İngilizce, Türkçe, Almanca ve Rusça'da mevcut
- **Çok dilli CHANGELOG**: 4 dilde mevcut
- **Geliştirilmiş dokümantasyon**: Kapsamlı yerinde dağıtım dokümantasyonu
- **Yerel AI kurulum örnekleri**: Ollama ve LM Studio için yapılandırma
- **Kurumsal kullanım senaryoları**: Bankacılık, Sağlık, Hukuk, Devlet, Üretim

### 🔧 İyileştirilenler
- **Yeniden deneme mekanizması**: Dil-spesifik talimatlarla geliştirilmiş yeniden deneme prompt'ları
- **Hata yönetimi**: Veritabanı tipi bilgisiyle daha iyi hata mesajları
- **Dokümantasyon yapısı**: CHANGELOG linkleriyle daha temiz README yapısı
- **Kod kalitesi**: SOLID/DRY prensipleri korundu
- **Performans**: Optimize edilmiş çoklu-veritabanı sorgu koordinasyonu

### ✅ Kalite Güvencesi
- **Sıfır Uyarı Politikası**: Tüm değişiklikler 0 hata, 0 uyarı standardını koruyor
- **SOLID Prensipleri**: Temiz kod mimarisi korundu
- **Kapsamlı Test**: PostgreSQL entegrasyonu ile çoklu-veritabanı test kapsamı
- **Güvenlik sertleştirme**: Geliştirilmiş yapılandırma dosyası yönetimi ve kimlik bilgisi koruması
- **Performans optimizasyonu**: Tüm özelliklerde yüksek performans korundu

### 🔄 Geçiş Rehberi (v2.3.0 → v3.0.0)

#### **Servis Katmanı Metod Değişiklikleri**
```csharp
// ESKİ (v2.3.0)
await _documentSearchService.GenerateRagAnswerAsync(query, maxResults);

// YENİ (v3.0.0)  
await _documentSearchService.QueryIntelligenceAsync(query, maxResults);
```

#### **Geriye Dönük Uyumluluk**
- Eski metodlar kullanımdan kaldırıldı ama hala çalışıyor (v4.0.0'da kaldırılacak)
- Endpoint'leri ve metodları kendi hızınızda güncelleyin
- Eski metodları kullanmaya devam ederseniz anında breaking change yok

---

## Versiyon Geçmişi

- **3.5.0** (2025-12-27) - Kod Kalitesi İyileştirmeleri ve Mimari Refactoring
- **3.4.0** (2025-12-12) - MCP Entegrasyonu, Dosya İzleyici, Sorgu Stratejisi Optimizasyonu
- **3.3.0** (2025-12-01) - Redis Vector Search ve Depolama İyileştirmeleri
- **3.2.0** (2025-11-27) - Mimari Refactoring, Strateji Deseni Implementasyonu
- **3.1.0** (2025-11-11) - Birleşik Sorgu Zekası, Akıllı Hibrit Yönlendirme, Yeni Servis Mimarisi
- **3.0.3** (2025-11-06) - Paket optimizasyonu, native kütüphaneler hariç
- **3.0.2** (2025-10-24) - Google Speech-to-Text kaldırıldı, sadece Whisper.net
- **3.0.1** (2025-10-22) - Hata düzeltmeleri, Logging stabilite iyileştirmeleri
- **3.0.0** (2025-10-22) - Zeka Kütüphanesi Devrimi, SQL Üretimi, On-Premise Desteği
- **2.3.1** (2025-10-20) - Hata düzeltmeleri, Logging stabilite iyileştirmeleri
- **2.3.0** (2025-09-16) - Google Speech-to-Text entegrasyonu, Ses işleme
- **2.2.0** (2025-09-15) - Geliştirilmiş OCR dokümantasyonu
- **2.1.0** (2025-09-05) - Otomatik oturum yönetimi, Kalıcı konuşma geçmişi
- **2.0.0** (2025-08-27) - .NET Standard 2.0/2.1 geçişi
- **1.1.0** (2025-08-22) - Excel desteği, EPPlus entegrasyonu
- **1.0.3** (2025-08-20) - Hata düzeltmeleri ve logging iyileştirmeleri
- **1.0.2** (2025-08-19) - İlk kararlı sürüm
- **1.0.1** (2025-08-17) - Beta sürüm
- **1.0.0** (2025-08-15) - İlk sürüm

