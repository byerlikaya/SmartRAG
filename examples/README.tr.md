# SmartRAG Örnekler

Bu klasör, SmartRAG'ın farklı senaryolarda nasıl kullanılacağını gösteren örnek projeler içerir.

## 📁 Mevcut Örnekler

### **SmartRAG.API** - ASP.NET Core Web API Örneği
- **Konum**: `SmartRAG.API/`
- **Açıklama**: Doküman yükleme, arama ve RAG işlemlerini gösteren tam web API implementasyonu

- **Özellikler**: 
  - **Birleşik Sorgu Zekası**: Tek endpoint ile belgeler, görüntüler (OCR), ses (transkripsiyon) ve veritabanlarında arama
  - Çoklu doküman yükleme (PDF, Word, Excel, metin dosyaları)
  - OCR desteği ile görüntü işleme (.jpg, .png, .gif, .bmp, .tiff, .webp)
  - Whisper.net ile ses işleme (yerel, 99+ dil)
  - Smart Hybrid routing ile AI destekli soru-cevap
  - Güven tabanlı routing ile akıllı sorgu intent algılama
  - Otomatik kaynak seçimi (veritabanı, belgeler veya her ikisi)
  - Konuşma geçmişi yönetimi
  - Çoklu depolama sağlayıcıları (Belgeler için: Qdrant, Redis, InMemory; Konuşmalar için: Redis, SQLite, FileSystem, InMemory)
  - Hybrid scoring ile geliştirilmiş semantik arama
  - Kapsamlı API dokümantasyonu

### **SmartRAG.Demo** - İnteraktif Çoklu Veritabanı RAG Demo
- **Konum**: `SmartRAG.Demo/`
- **Açıklama**: SmartRAG'ın deployment esnekliğini ve çoklu veritabanı yeteneklerini sergileyen kapsamlı demo
- **Özellikler**:
  - **Birleşik Sorgu Zekası**: Tek sorgu ile otomatik olarak belgeler, görüntüler, ses ve veritabanlarında arama
  - **Deployment Modları**: %100 Yerel, %100 Bulut veya Hybrid konfigürasyonlar
  - **Smart Hybrid Routing**: AI otomatik olarak veritabanları, belgeler veya her ikisini de arayıp aramayacağına karar verir
  - **Çoklu Veritabanı Sorguları**: Çapraz veritabanı doğal dil sorguları (SQL Server, MySQL, PostgreSQL, SQLite)
  - **Çoklu Modal Destek**: Belgeler (PDF, Word, Excel), Görüntüler (OCR), Ses (Speech-to-Text), Veritabanları
  - **Yerel AI**: Tam şirket içi deployment için Ollama entegrasyonu (GDPR/KVKK/HIPAA uyumlu)
  - **Bulut AI**: Anthropic Claude, OpenAI GPT, Google Gemini desteği
  - **Docker Orchestration**: docker-compose ile tam containerize ortam
  - **Test Veritabanları**: Çapraz veritabanı ilişkileri ile önceden yapılandırılmış test veritabanları
  - **Sistem Sağlık İzleme**: Tüm bileşenler için servis sağlık kontrolleri
  - **Model Yönetimi**: Ollama model indirme ve yönetimi
  - **Çoklu dil**: ISO 639-1 dil kodları ile birden fazla dilde sorgu desteği

## 🚀 Örnekleri Çalıştırma

### SmartRAG.API Örneği
```bash
cd examples/SmartRAG.API
dotnet restore
dotnet run
```

İnteraktif API dokümantasyonu için `https://localhost:5001/swagger` adresine gidin.

### SmartRAG.Demo Örneği
```bash
cd examples/SmartRAG.Demo

# Docker servislerini başlat (yerel mod için)
docker-compose up -d

# Uygulamayı çalıştır
dotnet restore
dotnet run
```

Deployment modunuzu seçin (Yerel/Bulut/Hybrid) ve çoklu veritabanı RAG yeteneklerini keşfedin!

## 🔧 Yapılandırma

Her örnek kendi yapılandırma dosyalarını içerir. Şablon dosyalarını ihtiyacınıza göre kopyalayıp düzenleyin:

```bash
# Geliştirme yapılandırma şablonunu kopyala
cp appsettings.Development.template.json appsettings.Development.json

# API anahtarlarınız ve yapılandırmanızla düzenleyin
```

## 📚 Dokümantasyon

- **Ana Dokümantasyon**: [SmartRAG README](../../README.tr.md)
- **API Referansı**: [API Dokümantasyonu](../../docs/tr/api-reference.md)
- **Yapılandırma Rehberi**: [Yapılandırma Rehberi](../../docs/tr/configuration/basic.md)

## 🤝 Katkıda Bulunma

Daha fazla örnek eklemek ister misiniz? Yeni bir klasör oluşturun ve bir pull request gönderin!

### Düşünülebilecek Örnek Tipleri:
- **Blazor WebAssembly** - Görüntü ve ses yükleme ile istemci tarafı web uygulaması
- **WPF Uygulaması** - Doküman ve ses işleme ile masaüstü uygulaması
- **Azure Functions** - Vektör araması ile serverless implementasyon
- **Minimal API** - Konuşma yönetimi ile hafif web API
- **OCR Servisi** - Bağımsız OCR işleme servisi
- **Speech-to-Text Servisi** - Whisper.net ile bağımsız ses transkripsiyon servisi
- **Doküman Analizörü** - Tablo çıkarma ile gelişmiş doküman analizi
- **Mobil Uygulama** - SmartRAG entegrasyonu ile çapraz platform mobil uygulama

## 📞 Destek

Sorular, sorunlar veya katkılar için lütfen [GitHub repository](https://github.com/byerlikaya/SmartRAG)'mizi ziyaret edin.

### İletişim Bilgileri
- **📧 [İletişim ve Destek](mailto:b.yerlikaya@outlook.com)**
- **💼 [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/)**
- **🐙 [GitHub Profili](https://github.com/byerlikaya)**
- **📦 [NuGet Paketleri](https://www.nuget.org/profiles/barisyerlikaya)**
- **📖 [Dokümantasyon](https://byerlikaya.github.io/SmartRAG/tr/)** - Kapsamlı rehberler ve API referansı

---
**Made in Turkey 🇹🇷 | [Contact](mailto:b.yerlikaya@outlook.com) | [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/)**
