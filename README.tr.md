<p align="center">
  <img src="icon.svg" alt="SmartRAG Logo" width="200" height="200">
</p>

<p align="center">
  <b>.NET için Multi-Modal RAG — veritabanları, belgeler, görüntüler ve ses dosyalarını doğal dil ile sorgula</b>
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/SmartRAG"><img src="https://img.shields.io/nuget/v/SmartRAG.svg?style=for-the-badge&logo=nuget" alt="NuGet Version"/></a>
  <a href="https://www.nuget.org/packages/SmartRAG"><img src="https://img.shields.io/nuget/dt/SmartRAG.svg?style=for-the-badge&logo=nuget" alt="Downloads"/></a>
  <a href="https://github.com/byerlikaya/SmartRAG/stargazers"><img src="https://img.shields.io/github/stars/byerlikaya/SmartRAG?style=for-the-badge&logo=github" alt="GitHub Stars"/></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-green.svg?style=for-the-badge" alt="License"/></a>
</p>

<p align="center">
  <a href="https://github.com/byerlikaya/SmartRAG/actions"><img src="https://img.shields.io/github/actions/workflow/status/byerlikaya/SmartRAG/ci.yml?style=for-the-badge&logo=github" alt="Build Status"/></a>
  <a href="https://www.nuget.org/packages/SmartRAG"><img src="https://img.shields.io/badge/.NET%20Standard-2.1-blue?style=for-the-badge&logo=.net" alt=".NET Standard 2.1"/></a>
</p>

<p align="center">
  <a href="https://byerlikaya.github.io/SmartRAG/tr/"><img src="https://img.shields.io/badge/📚-Tam_Dokümantasyon-blue?style=for-the-badge&logo=book" alt="Documentation"/></a>
  <a href="README.md"><img src="https://img.shields.io/badge/🇺🇸-English_README-blue?style=for-the-badge" alt="English README"/></a>
</p>

## 🚀 **Hızlı Başlangıç**

### **1. SmartRAG'ı Kur**
```bash
dotnet add package SmartRAG
```

### **2. Kurulum**
```csharp
builder.Services.UseSmartRag(builder.Configuration,
    aiProvider: AIProvider.OpenAI,
    storageProvider: StorageProvider.InMemory
);
```

### **3. Veritabanlarını appsettings.json'da yapılandır**
```json
{
  "SmartRAG": {
    "DatabaseConnections": [
      {
        "Name": "Satış",
        "ConnectionString": "Server=localhost;Database=Satis;...",
        "DatabaseType": "SqlServer"
      }
    ]
  }
}
```

### **4. Belgeleri yükle ve sorular sor**
```csharp
// Belge yükle
var belge = await documentService.UploadDocumentAsync(
    dosyaStream, dosyaAdi, icerikTipi, "kullanici-123"
);

// Veritabanları, belgeler, görüntüler ve ses dosyalarında birleşik sorgu
var cevap = await searchService.QueryIntelligenceAsync(
    "Son çeyrekte 10.000 TL üzeri alışveriş yapan tüm müşterileri, ödeme geçmişlerini ve verdikleri şikayet veya geri bildirimleri göster"
);
// → AI otomatik olarak sorgu intent'ini analiz eder ve akıllıca yönlendirir:
//   - Yüksek güven + veritabanı sorguları → Sadece veritabanlarını arar
//   - Yüksek güven + belge sorguları → Sadece belgeleri arar
//   - Orta güven → Hem veritabanlarını hem belgeleri arar, sonuçları birleştirir
// → SQL Server (siparişler), MySQL (ödemeler), PostgreSQL (müşteri verileri) sorgular
// → Yüklenen PDF sözleşmeleri, OCR ile taranmış faturaları ve transkript edilmiş çağrı kayıtlarını analiz eder
// → Tüm kaynaklardan birleşik cevap sağlar
```

**SmartRAG'ı hemen test etmek ister misiniz?** → [Örnekler ve Test'e Git](#-örnekler-ve-test)


## 🏆 **Neden SmartRAG?**

🎯 **Birleşik Sorgu Zekası** - Tek sorgu ile veritabanları, belgeler, görüntüler ve ses dosyalarını otomatik olarak arar

🧠 **Akıllı Hibrit Yönlendirme** - AI sorgu intent'ini analiz eder ve optimal arama stratejisini otomatik belirler

🗄️ **Multi-Database RAG** - Birden fazla veritabanını doğal dil ile aynı anda sorgula

📄 **Çoklu Modal Zeka** - PDF, Word, Excel, Görüntü (OCR), Ses (Konuşma-Metin), ve daha fazlası  

🏠 **%100 Yerel İşleme** - GDPR, KVKK, HIPAA uyumlu

🚀 **Üretim Hazır** - Kurumsal kalite, thread-safe, yüksek performans

## 🎯 **Gerçek Dünya Kullanım Senaryoları**

### **1. Bankacılık - Müşteri Finansal Profili**
```csharp
var cevap = await searchService.QueryIntelligenceAsync(
    "Hangi müşterilerin vadesi geçmiş ödemeleri var ve toplam borçları ne kadar?"
);
// → Müşteri DB, Ödeme DB, Hesap DB'yi sorgular ve sonuçları birleştirir
// → Kredi kararları için kapsamlı finansal risk değerlendirmesi sağlar
```

### **2. Sağlık - Hasta Bakım Yönetimi**
```csharp
var cevap = await searchService.QueryIntelligenceAsync(
    "Diyabet hastalarından HbA1c kontrolü 6 aydır yapılmayanları göster"
);
// → Hasta DB, Lab Sonuçları DB, Randevu DB'yi birleştirir ve risk altındaki hastaları belirler
// → Önleyici bakım uyumunu sağlar ve komplikasyonları azaltır
```

### **3. Envanter - Tedarik Zinciri Optimizasyonu**
```csharp
var cevap = await searchService.QueryIntelligenceAsync(
    "Hangi ürünlerin stoku azalıyor ve hangi tedarikçiler en hızlı yeniden stoklayabilir?"
);
// → Envanter DB, Tedarikçi DB, Sipariş Geçmişi DB'yi analiz eder ve yeniden stoklama önerileri sağlar
// → Stok tükenmesini önler ve tedarik zinciri verimliliğini optimize eder
```

## 🚀 **SmartRAG'ı Özel Kılan Nedir?**

- **Tek .NET kütüphanesi** gerçek çoklu veritabanı RAG yetenekleri ile
- **Otomatik şema algılama** farklı veritabanı türleri arasında  
- **%100 yerel işleme** Ollama ve Whisper.net ile
- **Kurumsal hazır** kapsamlı hata yönetimi ve loglama ile
- **Çapraz veritabanı sorguları** manuel SQL yazmadan
- **Çoklu modal zeka** belgeler, veritabanları ve AI'yı birleştirerek

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

📖 **[SmartRAG.Demo README](examples/SmartRAG.Demo/README.tr.md)** - Tam demo uygulaması rehberi ve kurulum talimatları

#### **🐳 Docker Kurulumu (Önerilen)**

Tüm servislerin önceden yapılandırıldığı en kolay deneyim için:

```bash
# Tüm servisleri başlat (SQL Server, MySQL, PostgreSQL, Ollama, Qdrant, Redis)
docker-compose up -d

# AI modellerini kur
docker exec -it smartrag-ollama ollama pull llama3.2
docker exec -it smartrag-ollama ollama pull nomic-embed-text
```

📚 **[Tam Docker Kurulum Rehberi](examples/SmartRAG.Demo/README-Docker.tr.md)** - Detaylı Docker konfigürasyonu, sorun giderme ve yönetim

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
- **Adım 11**: Vektör depolarını test et (InMemory, Redis, Qdrant)

**📄 Belge İşleme:**
- **Adım 12**: Belgeleri yükle (PDF, Word, Excel, Görüntüler, Ses)
- **Adım 13**: Yüklenen belgeleri listele ve yönet
- **Adım 14**: Çoklu Modal RAG - belgeler + veritabanlarını birleştir
- **Adım 15**: Temiz test için belgeleri temizle

**İdeal için:** Hızlı değerlendirme, proof-of-concept, ekip demoları, SmartRAG yeteneklerini öğrenme

📚 **[Tam Örnekler ve Test Rehberi](https://byerlikaya.github.io/SmartRAG/tr/examples)** - Adım adım öğreticiler ve test senaryoları

## 🎯 **Desteklenen Veri Kaynakları**

**📊 Veritabanları:** SQL Server, MySQL, PostgreSQL, SQLite  
**📄 Belgeler:** PDF, Word, Excel, PowerPoint, Görüntü, Ses  
**🤖 AI Modelleri:** OpenAI, Anthropic, Gemini, Azure OpenAI, Ollama (yerel), LM Studio  
**🗄️ Vektör Depoları:** Qdrant, Redis, InMemory  
**💬 Konuşma Depolama:** Redis, SQLite, FileSystem, InMemory (belge depolamadan bağımsız)

## 📄 Lisans

Bu proje MIT Lisansı altında lisanslanmıştır - detaylar için [LICENSE](LICENSE) dosyasına bakın.

**Barış Yerlikaya tarafından ❤️ ile yapıldı**

Türkiye'de yapıldı 🇹🇷 | [İletişim](mailto:b.yerlikaya@outlook.com) | [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/)