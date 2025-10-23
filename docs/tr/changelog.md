---
layout: default
title: Değişiklikler
description: SmartRAG için eksiksiz versiyon geçmişi, breaking change'ler ve taşınma kılavuzları
lang: tr
---


SmartRAG'deki tüm önemli değişiklikler burada belgelenmiştir. Proje [Anlamsal Versiyonlama](https://semver.org/spec/v2.0.0.html)'ya uymaktadır.

---

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

---

## [3.0.1] - 2025-10-22

### 🐛 Düzeltildi
- **LoggerMessage Parametre Uyumsuzluğu**: `LogAudioServiceInitialized` LoggerMessage tanımında eksik `configPath` parametresi düzeltildi
- **EventId Çakışmaları**: ServiceLogMessages.cs'deki çakışan EventId atamaları çözüldü (6006, 6008, 6009)
- **Logo Görüntüleme Sorunu**: NuGet'te görüntüleme sorunlarına neden olan README dosyalarındaki bozuk logo referansları kaldırıldı
- **TypeInitializationException**: SmartRAG.Demo'nun çalışmasını engelleyen kritik başlatma hatası düzeltildi

### 🔧 Teknik İyileştirmeler
- **ServiceLogMessages.cs**: LoggerMessage tanımları parametre sayılarıyla doğru eşleşecek şekilde güncellendi
- **EventId Yönetimi**: Benzersiz log tanımlayıcıları için çakışan EventId'ler yeniden atandı
- **Dokümantasyon**: Daha iyi NuGet paket görüntüleme için README dosyaları temizlendi

---

## [3.0.0] - 2025-10-22

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> BREAKING CHANGE'LER</h4>
    <p class="mb-0">Bu sürüm breaking API değişiklikleri içerir. Aşağıdaki taşınma kılavuzuna bakın.</p>
                    </div>

### 🚀 Zeka Kütüphanesi Devrimi

#### Önemli API Değişiklikleri
- **`GenerateRagAnswerAsync` → `QueryIntelligenceAsync`**: Akıllı sorgu işlemeyi daha iyi temsil etmek için metod yeniden adlandırıldı
- **Geliştirilmiş `IDocumentSearchService` interface'i**: Gelişmiş RAG pipeline ile yeni akıllı sorgu işleme
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
- **Geliştirilmiş dokümantasyon**: Kapsamlı yerinde dağıtım dokümantasyonu
- **Yerel AI kurulum örnekleri**: Ollama ve LM Studio için yapılandırma
- **Kurumsal kullanım senaryoları**: Bankacılık, Sağlık, Hukuk, Devlet, Üretim

### 🔧 İyileştirmeler
- **Yeniden deneme mekanizması**: Dile özgü talimatlarla geliştirilmiş yeniden deneme istekleri
- **Hata yönetimi**: Veritabanı tipi bilgisiyle daha iyi hata mesajları
- **Dokümantasyon yapısı**: CHANGELOG bağlantılarıyla daha temiz README
- **Kod kalitesi**: Boyunca sürdürülen SOLID/DRY prensipleri
- **Performans**: Optimize edilmiş çoklu-veritabanı sorgu koordinasyonu

### 📚 Dokümantasyon
- **Yerinde kılavuz**: Kapsamlı dağıtım dokümantasyonu
- **Gizlilik kılavuzu**: Veri gizliliği ve uyumluluk dokümantasyonu
- **OCR kısıtlamaları**: Net yetenekler ve kısıtlamalar
- **Ses işleme**: Net gereksinimler ve kısıtlamalar
- **Kurumsal senaryolar**: Gerçek dünya kullanım senaryoları

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
- **Yapılandırma Yönetimi**: GoogleSpeechConfig kullanacak şekilde güncellendi
- **Hata Yönetimi**: Ses transkripsiyonu için geliştirilmiş
- **Dokümantasyon**: Speech-to-Text örnekleriyle güncellendi

---

## [2.2.0] - 2025-09-15

### ✨ Eklenenler
- **Geliştirilmiş OCR Dokümantasyonu**: Gerçek dünya kullanım senaryolarıyla kapsamlı
- **İyileştirilmiş README**: Detaylı görsel işleme özellikleri
- **Kullanım Senaryosu Örnekleri**: Taranmış dokümanlar, makbuzlar, görsel içeriği

### 🔧 İyileştirmeler
- **Paket Metadata**: Güncellenmiş proje URL'leri ve sürüm notları
- **Dokümantasyon Yapısı**: Geliştirilmiş OCR vitrini
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
                <td>Geliştirilmiş OCR dokümantasyonu</td>
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
    <p>Birincil değişiklik, <code>GenerateRagAnswerAsync</code>'in <code>QueryIntelligenceAsync</code> olarak yeniden adlandırılmasıdır.</p>
                    </div>

**Adım 1: Metod çağrılarını güncelleyin**

```csharp
// Önce (v2.x)
var response = await _searchService.GenerateRagAnswerAsync(query, maxResults);

// Sonra (v3.0.0)
var response = await _searchService.QueryIntelligenceAsync(query, maxResults);
```

**Adım 2: API endpoint'lerini güncelleyin (Web API kullanıyorsanız)**

```csharp
// Önce
[HttpPost("generate-answer")]
public async Task<IActionResult> GenerateAnswer([FromBody] QueryRequest request)
{
    var response = await _searchService.GenerateRagAnswerAsync(request.Query);
    return Ok(response);
}

// Sonra
[HttpPost("query")]
public async Task<IActionResult> Query([FromBody] QueryRequest request)
{
    var response = await _searchService.QueryIntelligenceAsync(request.Query);
    return Ok(response);
}
```

<div class="alert alert-success">
    <h4><i class="fas fa-check-circle me-2"></i> Acil Eylem Gerekmez</h4>
    <p class="mb-0">
        Eski <code>GenerateRagAnswerAsync</code> metodu hala çalışıyor (kullanımdan kaldırıldı olarak işaretli). 
        v4.0.0 yayınlanmadan önce kademeli olarak taşınabilirsiniz.
    </p>
                    </div>

---

## Kullanımdan Kaldırma Bildirimleri

### v3.0.0'da Kullanımdan Kaldırıldı (v4.0.0'da Kaldırılacak)

<div class="alert alert-warning">
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
        <div class="feature-card">
            <div class="feature-icon">
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
        <div class="feature-card">
            <div class="feature-icon">
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

