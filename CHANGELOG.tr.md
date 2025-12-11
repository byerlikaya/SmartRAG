
# Değişiklik Günlüğü

SmartRAG'deki tüm önemli değişiklikler bu dosyada belgelenecektir.

Format [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)'a dayanmaktadır
ve bu proje [Semantic Versioning](https://semver.org/spec/v2.0.0.html)'a uymaktadır.

## [3.4.0] - 2025-12-10

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
  - **Değiştirilen Dosyalar**:
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Erken çıkış mantığı implementasyonu
  - **Faydalar**: Daha hızlı arama yanıtları, azaltılmış kaynak kullanımı, geliştirilmiş kullanıcı deneyimi

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

### 🔧 İyileştirilenler
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

