<p align="center">
  <img src="icon.svg" alt="SmartRAG Logo" width="200" height="200">
</p>

<p align="center">
  <b>.NET için Multi-DB RAG — birden fazla veritabanı + belgeyi tek NL isteğinde sorgula</b>
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/SmartRAG"><img src="https://img.shields.io/nuget/v/SmartRAG.svg?style=for-the-badge&logo=nuget" alt="NuGet Version"/></a>
  <a href="https://www.nuget.org/packages/SmartRAG"><img src="https://img.shields.io/nuget/dt/SmartRAG.svg?style=for-the-badge&logo=nuget" alt="Downloads"/></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-green.svg?style=for-the-badge" alt="License"/></a>
  <a href="https://github.com/byerlikaya/SmartRAG/actions"><img src="https://img.shields.io/github/actions/workflow/status/byerlikaya/SmartRAG/build.yml?style=for-the-badge&logo=github" alt="Build Status"/></a>
  <a href="https://codecov.io/gh/byerlikaya/SmartRAG"><img src="https://img.shields.io/codecov/c/github/byerlikaya/SmartRAG?style=for-the-badge&logo=codecov" alt="Code Coverage"/></a>
</p>

<p align="center">
  <a href="https://byerlikaya.github.io/SmartRAG/tr/"><img src="https://img.shields.io/badge/📚-Tam_Dokümantasyon-blue?style=for-the-badge&logo=book" alt="Documentation"/></a>
  <a href="README.md"><img src="https://img.shields.io/badge/🇺🇸-English_README-blue?style=for-the-badge" alt="English README"/></a>
</p>

---

## 🚀 **Hızlı Kullanım Senaryoları**

### **🏦 Bankacılık**
```csharp
var cevap = await intelligence.QueryIntelligenceAsync(
    "Hangi müşterilerin vadesi geçmiş ödemeleri var ve toplam borçları ne kadar?"
);
// → Müşteri DB, Ödeme DB, Hesap DB'yi sorgular ve sonuçları birleştirir
```

### **🏥 Sağlık**
```csharp
var cevap = await intelligence.QueryIntelligenceAsync(
    "Diyabet hastalarından HbA1c kontrolü 6 aydır yapılmayanları göster"
);
// → Hasta DB, Lab Sonuçları DB, Randevu DB'yi birleştirir ve risk altındaki hastaları belirler
```

### **📦 Envanter**
```csharp
var cevap = await intelligence.QueryIntelligenceAsync(
    "Hangi ürünlerin stoku azalıyor ve hangi tedarikçiler en hızlı yeniden stoklayabilir?"
);
// → Envanter DB, Tedarikçi DB, Sipariş Geçmişi DB'yi analiz eder ve yeniden stoklama önerileri sağlar
```

---

## 🚀 **Hızlı Başlangıç**

```csharp
// 1. Kurulum
builder.Services.UseSmartRAG(builder.Configuration,
    aiProvider: AIProvider.OpenAI,
    storageProvider: StorageProvider.InMemory
);

// 2. Veritabanlarını bağla & belgeleri yükle
await connector.ConnectAsync(sqlServer: "Server=localhost;Database=Satis;");
await documents.UploadAsync(dosyalar);

// 3. Sorular sor
var cevap = await intelligence.QueryIntelligenceAsync(
    "Tüm veritabanlarından 100 bin TL üzeri cirosu olan müşterileri göster"
);
// → AI otomatik olarak SQL Server, MySQL, PostgreSQL sorgular ve sonuçları birleştirir
```

---

## 🧪 **Örnekler ve Test**

SmartRAG farklı kullanım senaryoları için kapsamlı örnek uygulamalar sağlar:

### **📁 Mevcut Örnekler**
```
examples/
├── SmartRAG.API/          # Swagger UI ile tam REST API
└── SmartRAG.Demo/         # Etkileşimli konsol uygulaması
```

### **🚀 Demo ile Hızlı Test**

SmartRAG'ı hemen görmek ister misiniz? İnteraktif konsol demo'muzu deneyin:

```bash
# Klonla ve demo'yu çalıştır
git clone https://github.com/byerlikaya/SmartRAG.git
cd SmartRAG/examples/SmartRAG.Demo
dotnet run
```

**Önkoşullar:** Yerel olarak veritabanları ve AI servisleri çalıştırmanız gerekiyor, veya kolay kurulum için Docker kullanabilirsiniz.

📖 **[SmartRAG.Demo README](examples/SmartRAG.Demo/README.md)** - Tam demo uygulaması rehberi ve kurulum talimatları

#### **🐳 Docker Kurulumu (Önerilen)**

Tüm servislerin önceden yapılandırıldığı en kolay deneyim için:

```bash
# Tüm servisleri başlat (SQL Server, MySQL, PostgreSQL, Ollama, Qdrant, Redis)
docker-compose up -d

# AI modellerini kur
docker exec -it smartrag-ollama ollama pull llama3.2
docker exec -it smartrag-ollama ollama pull nomic-embed-text
```

📚 **[Tam Docker Kurulum Rehberi](examples/SmartRAG.Demo/README-Docker.md)** - Detaylı Docker konfigürasyonu, sorun giderme ve yönetim

### **📋 Demo Özellikleri ve Adımları:**

**🔗 Veritabanı Yönetimi:**
- **Adım 1-2**: Bağlantıları göster ve sistem sağlık kontrolü
- **Adım 3-5**: Test veritabanları oluştur (SQL Server, MySQL, PostgreSQL)
- **Adım 6**: Veritabanı şemalarını ve ilişkileri görüntüle

**🤖 AI ve Sorgu Testleri:**
- **Adım 7**: Sorgu analizi - doğal dilin SQL'e nasıl dönüştüğünü gör
- **Adım 8**: Otomatik test sorguları - önceden hazırlanmış senaryolar
- **Adım 9**: Çoklu Veritabanı AI Sorguları - tüm veritabanlarında sorular sor

**🏠 Yerel AI Kurulumu:**
- **Adım 10**: %100 yerel işleme için Ollama modellerini kur
- **Adım 11**: Vektör depolarını test et (InMemory, Redis, SQLite, Qdrant)

**📄 Belge İşleme:**
- **Adım 12**: Belgeleri yükle (PDF, Word, Excel, Görüntüler, Ses)
- **Adım 13**: Yüklenen belgeleri listele ve yönet
- **Adım 14**: Çoklu Modal RAG - belgeler + veritabanlarını birleştir
- **Adım 15**: Temiz test için belgeleri temizle

**İdeal için:** Hızlı değerlendirme, proof-of-concept, ekip demoları, SmartRAG yeteneklerini öğrenme

📚 **[Tam Örnekler ve Test Rehberi](https://byerlikaya.github.io/SmartRAG/tr/examples)** - Adım adım öğreticiler ve test senaryoları

---

## 🚀 SmartRAG'ı Özel Kılan Nedir?

✅ **Multi-Database RAG** - Doğal dil ile birden fazla veritabanını sorgula  
✅ **Çoklu Modal Intelligence** - PDF + Excel + Görüntü + Ses + Veritabanları  
✅ **On-Premise Ready** - Ollama/LM Studio/Whisper.net ile %100 yerel  
✅ **Production Ready** - Kurumsal düzeyde hata yönetimi ve test  

📚 **[Tam Teknik Dokümantasyon](https://byerlikaya.github.io/SmartRAG/tr)** - Mimari, API referansı, gelişmiş örnekler

---

## 📦 Kurulum

### NuGet Package Manager
```bash
Install-Package SmartRAG
```

### .NET CLI
```bash
dotnet add package SmartRAG
```

### Package Manager Console
```powershell
Install-Package SmartRAG
```

---

## 🏆 **Neden SmartRAG?**

### **🎯 Multi-Database RAG**
- **Çoklu Veritabanı Sorguları**: Doğal dil ile birden fazla veritabanını aynı anda sorgula
- **Akıllı Veri Füzyonu**: Farklı veri kaynaklarından sonuçları otomatik olarak birleştirir
- **Şema Farkında İşleme**: Veritabanı ilişkilerini ve yabancı anahtarları anlar
- **Gerçek Zamanlı Veri Erişimi**: Sadece statik dışa aktarımlar değil, canlı veritabanı bağlantıları ile çalışır

### **🧠 Çoklu Modal Intelligence**
- **Evrensel Belge Desteği**: PDF, Word, Excel, PowerPoint, Görüntüler, Ses ve daha fazlası
- **Gelişmiş OCR**: Görüntülerden, taranmış belgelerden ve el yazısı notlardan metin çıkarır
- **Ses Transkripsiyonu**: Whisper.net ile konuşmayı metne dönüştürür (99+ dil)
- **Akıllı Parçalama**: Bağlamı koruyan akıllı belge segmentasyonu

### **🏠 On-Premise Ready**
- **%100 Yerel İşleme**: Her şeyi kendi altyapınızda çalıştırın
- **Gizlilik ve Uyumluluk**: Yerel veri işleme ile GDPR, KVKK, HIPAA uyumlu
- **Bulut Bağımlılığı Yok**: Yerel AI modelleri (Ollama, LM Studio) ile çevrimdışı çalışır
- **Kurumsal Güvenlik**: Veri ve işleme üzerinde tam kontrol

### **🚀 Production Ready**
- **Kurumsal Düzey**: Thread-safe işlemler, kapsamlı hata yönetimi
- **Yüksek Performans**: Hız ve ölçeklenebilirlik için optimize edilmiş
- **Kapsamlı Test**: Geniş test kapsamı ve kalite güvencesi
- **Profesyonel Destek**: Ticari destek ve danışmanlık mevcut

---

## 🔧 **Konfigürasyon ve Kurulum**

Detaylı konfigürasyon örnekleri, yerel AI kurulumu ve kurumsal dağıtım rehberleri için:

📚 **[Tam Konfigürasyon Rehberi](https://byerlikaya.github.io/SmartRAG/tr/configuration)**  
🏠 **[Yerel AI Kurulumu](https://byerlikaya.github.io/SmartRAG/tr/configuration/local-ai)**  
🏢 **[Kurumsal Dağıtım](https://byerlikaya.github.io/SmartRAG/tr/configuration/enterprise)**  
🎤 **[Ses Konfigürasyonu](https://byerlikaya.github.io/SmartRAG/tr/configuration/audio-ocr)**  
🗄️ **[Veritabanı Kurulumu](https://byerlikaya.github.io/SmartRAG/tr/configuration/database)**

---

## 📊 **Diğer RAG Kütüphaneleri ile Karşılaştırma**

| Özellik | SmartRAG | Semantic Kernel | LangChain.NET | Diğer RAG Kütüphaneleri |
|---------|----------|----------------|---------------|-------------------|
| **Multi-Database RAG** | ✅ Native | ❌ Manuel | ❌ Manuel | ❌ Desteklenmiyor |
| **Multi-Modal Destek** | ✅ PDF+Excel+Görüntü+Ses+DB | ❌ Sınırlı | ❌ Sınırlı | ❌ Sınırlı |
| **On-Premise Ready** | ✅ %100 Yerel | ❌ Bulut gerekli | ❌ Bulut gerekli | ❌ Bulut gerekli |
| **Production Ready** | ✅ Kurumsal düzey | ⚠️ Temel | ⚠️ Temel | ⚠️ Temel |
| **Çoklu Veritabanı Sorguları** | ✅ Otomatik | ❌ Desteklenmiyor | ❌ Desteklenmiyor | ❌ Desteklenmiyor |
| **Yerel AI Desteği** | ✅ Ollama/LM Studio | ❌ Sınırlı | ❌ Sınırlı | ❌ Sınırlı |
| **Ses İşleme** | ✅ Whisper.net | ❌ Desteklenmiyor | ❌ Desteklenmiyor | ❌ Desteklenmiyor |
| **OCR Yetenekleri** | ✅ Tesseract 5.2.0 | ❌ Desteklenmiyor | ❌ Desteklenmiyor | ❌ Desteklenmiyor |
| **Veritabanı Entegrasyonu** | ✅ SQL Server+MySQL+PostgreSQL+SQLite | ❌ Manuel | ❌ Manuel | ❌ Manuel |
| **Kurumsal Özellikler** | ✅ Thread-safe, DI, Logging | ⚠️ Temel | ⚠️ Temel | ⚠️ Temel |

**SmartRAG, çoklu veritabanı sorgu yetenekleri ile gerçek multi-database RAG sağlayan TEK kütüphanedir.**

---

## 🎯 **Gerçek Dünya Kullanım Senaryoları**

### **1. Finansal Hizmetler - Risk Değerlendirmesi**
```csharp
var cevap = await intelligence.QueryIntelligenceAsync(
    "Kredi skoru 600'ün altında olan ve son 3 ayda ödeme kaçıran müşterileri bul"
);
// → Kredi DB, Ödeme Geçmişi DB, Hesap DB ve Risk Değerlendirme DB'yi sorgular
// → Proaktif müdahale için yüksek riskli müşterileri belirler
```

### **2. Sağlık - Önleyici Bakım**
```csharp
var cevap = await intelligence.QueryIntelligenceAsync(
    "Diyabet hastalarından yıllık göz muayenesi ve ayak kontrolü yaptırmayanları göster"
);
// → Hasta DB, Randevu DB, Tanı DB ve Sigorta DB'yi sorgular
// → Önleyici bakım uyumunu sağlar ve komplikasyonları azaltır
```

### **3. E-ticaret - Envanter Optimizasyonu**
```csharp
var cevap = await intelligence.QueryIntelligenceAsync(
    "Hangi ürünler birlikte sık iade ediliyor ve yüksek iade oranının nedeni ne?"
);
// → Sipariş DB, İade DB, Ürün DB ve Müşteri Geri Bildirimi DB'yi sorgular
// → Ürün kalite sorunlarını ve paketleme problemlerini belirler
```

### **4. Üretim - Öngörülü Bakım**
```csharp
var cevap = await intelligence.QueryIntelligenceAsync(
    "Hangi makineler gelecek 30 gün içinde arıza riski gösteren titreşim kalıplarına sahip?"
);
// → Sensör DB, Bakım DB, Üretim DB ve Ekipman DB'yi sorgular
// → Planlanmamış duruşları önler ve bakım maliyetlerini azaltır
```

### **5. Eğitim - Erken Müdahale**
```csharp
var cevap = await intelligence.QueryIntelligenceAsync(
    "Hangi öğrencilerin devam durumu düşüyor ve aynı derslerde notları düşüyor?"
);
// → Devam DB, Notlar DB, Öğrenci Destek DB ve Aile DB'yi sorgular
// → Öğrenciler okulu bırakmadan önce erken müdahale sağlar
```

### **6. Emlak - Pazar Analizi**
```csharp
var cevap = await intelligence.QueryIntelligenceAsync(
    "Hangi mahallelerde piyasa değerinin %20 altında satılan ve iyi okul puanları olan mülkler var?"
);
// → Mülk DB, Satış DB, Okul DB ve Pazar Trendleri DB'yi sorgular
// → Büyüme potansiyeli olan değeri düşük mülkleri belirler
```

### **7. Devlet - Dolandırıcılık Tespiti**
```csharp
var cevap = await intelligence.QueryIntelligenceAsync(
    "Farklı departmanlardan çakışan uygunluk dönemlerinde birden fazla yardım alan vatandaşları bul"
);
// → Yardım DB, Vatandaş DB, Uygunluk DB ve Ödeme DB'yi sorgular
// → Çift yardım ödemelerini önler ve dolandırıcılığı azaltır
```

### **8. Otomotiv - Güvenlik Analizi**
```csharp
var cevap = await intelligence.QueryIntelligenceAsync(
    "Hangi araç modellerinin belirli hava koşullarında en yüksek kaza oranları var?"
);
// → Kaza DB, Araç DB, Hava Durumu DB ve Sigorta DB'yi sorgular
// → Güvenlik önerilerini iyileştirir ve sigorta fiyatlandırmasını geliştirir
```

### **9. Perakende - Müşteri Sadakati**
```csharp
var cevap = await intelligence.QueryIntelligenceAsync(
    "Premium ürün satın alan müşterilerden 90 gündür alışveriş yapmayanları göster"
);
// → Müşteri DB, Satın Alma DB, Ürün DB ve Etkileşim DB'yi sorgular
// → Hedefli sadakat kampanyaları için risk altındaki müşterileri belirler
```

### **10. Araştırma - Trend Analizi**
```csharp
var cevap = await intelligence.QueryIntelligenceAsync(
    "Hangi araştırma konuları momentum kazanıyor ama sınırlı fonlama fırsatlarına sahip?"
);
// → Yayın DB, Fonlama DB, Atıf DB ve Hibe DB'yi sorgular
// → Fon tahsisi için gelişmekte olan araştırma alanlarını belirler
```

---

## 🎯 **Desteklenen Veri Kaynakları**

### **📄 Belge Türleri**
- **PDF Dosyaları** - Metin çıkarma ve akıllı parçalama
- **Word Belgeleri** - Biçimlendirme korunması ile .docx, .doc desteği
- **Excel Elektronik Tabloları** - Veri analizi yetenekleri ile .xlsx, .xls
- **PowerPoint Sunumları** - Slayt içerik çıkarma ile .pptx, .ppt
- **Metin Dosyaları** - Kodlama algılama ile .txt, .md, .csv
- **Görüntüler** - OCR metin çıkarma ile .jpg, .png, .gif, .bmp, .tiff
- **Ses Dosyaları** - Whisper.net ile yerel transkripsiyon (99+ dil)

### **🗄️ Veritabanı Türleri**
- **SQL Server** - Canlı bağlantılarla tam destek
- **MySQL** - Tüm veri türleri ile tam entegrasyon
- **PostgreSQL** - JSON ve özel türlerle gelişmiş destek
- **SQLite** - Dosya tabanlı depolama ile yerel veritabanı desteği

### **🧠 AI Sağlayıcıları**
- **OpenAI** - Fonksiyon çağırma ile GPT-4, GPT-3.5-turbo
- **Anthropic** - VoyageAI embeddings ile Claude 3.5 Sonnet
- **Google** - Gelişmiş akıl yürütme ile Gemini Pro
- **Azure OpenAI** - Kurumsal düzeyde OpenAI hizmetleri
- **Özel Sağlayıcılar** - Herhangi bir AI hizmeti için genişletilebilir mimari

### **💾 Depolama Sağlayıcıları**
- **In-Memory** - Hızlı geliştirme ve test
- **Redis** - Yüksek performanslı önbellekleme ve depolama
- **Qdrant** - Semantik arama için vektör veritabanı
- **SQLite** - Yerel dosya tabanlı depolama
- **Dosya Sistemi** - Basit dosya tabanlı belge depolama

---

## 🏆 **Gelişmiş Özellikler**

### **🧠 Akıllı Sorgu Niyet Algılama**
SmartRAG sorgunuzun genel konuşma mı yoksa belge arama mı olduğunu otomatik olarak algılar:

- **Genel Konuşma**: "Nasılsın?" → Doğrudan AI yanıtı
- **Belge Arama**: "Ana faydalar neler?" → Belgelerinizi arar
- **Çoklu Veritabanı Sorgu**: "Satış verilerini göster" → Bağlı veritabanlarını sorgular
- **Çoklu Veritabanı Analizi**: "Departmanlar arası performansı karşılaştır" → Birden fazla kaynaktan veriyi birleştirir

### **🔍 Gelişmiş Semantik Arama**
- **Hibrit Puanlama**: Semantik benzerlik (%80) ile anahtar kelime ilgisini (%20) birleştirir
- **Bağlam Farkındalığı**: Sorgular arası konuşma bağlamını korur
- **Çok Dilli Destek**: Sabit kodlanmış kalıplar olmadan herhangi bir dilde çalışır
- **Akıllı Parçalama**: Bağlamı koruyan ve kelime sınırlarını koruyan akıllı belge segmentasyonu

### **🎯 Multi-Modal Intelligence**
- **Belge İşleme**: PDF, Word, Excel, PowerPoint, Görüntüler, Ses
- **OCR Yetenekleri**: Görüntülerden, taranmış belgelerden, el yazısı notlardan metin çıkarır
- **Ses Transkripsiyonu**: Whisper.net ile konuşmayı metne dönüştürür
- **Akıllı Parçalama**: Bağlamı koruyan akıllı belge segmentasyonu

### **🏠 On-Premise Dağıtım**
- **Yerel AI Modelleri**: Tam gizlilik için Ollama, LM Studio entegrasyonu
- **Bulut Bağımlılığı Yok**: Yerel işleme ile çevrimdışı çalışır
- **Kurumsal Güvenlik**: Veri ve işleme üzerinde tam kontrol
- **Uyumluluk Hazır**: Yerel veri işleme ile GDPR, KVKK, HIPAA uyumlu

---

## 📄 Lisans

Bu proje MIT Lisansı altında lisanslanmıştır - detaylar için [LICENSE](LICENSE) dosyasına bakın.

**Barış Yerlikaya tarafından ❤️ ile yapıldı**

Türkiye'de yapıldı 🇹🇷 | [İletişim](mailto:b.yerlikaya@outlook.com) | [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/)