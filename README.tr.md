<p align="center">
  <img src="icon.svg" alt="SmartRAG Logo" width="200"/>
</p>

<p align="center">
  <b>.NET için Multi-Database RAG Kütüphanesi</b><br>
  Verileriniz hakkında doğal dilde sorular sorun
</p>

<p align="center">
  <a href="#-hızlı-başlangıç">Hızlı Başlangıç</a> •
  <a href="#-neden-smartrag">Neden SmartRAG</a> •
  <a href="#-neler-yapabilirsiniz">Örnekler</a> •
  <a href="#-smartrag-vs-diğer-net-rag-kütüphaneleri">Karşılaştırma</a> •
  <a href="https://byerlikaya.github.io/SmartRAG/tr">Dokümantasyon</a>
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/SmartRAG"><img src="https://img.shields.io/nuget/v/SmartRAG.svg?style=for-the-badge&logo=nuget" alt="NuGet Versiyon"/></a>
  <a href="https://www.nuget.org/packages/SmartRAG"><img src="https://img.shields.io/nuget/dt/SmartRAG?style=for-the-badge&logo=nuget&label=İndirme&color=blue" alt="NuGet İndirme"/></a>
  <a href="https://github.com/byerlikaya/SmartRAG"><img src="https://img.shields.io/github/stars/byerlikaya/SmartRAG?style=for-the-badge&logo=github" alt="GitHub Yıldız"/></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/lisans-MIT-green.svg?style=for-the-badge" alt="Lisans"/></a>
</p>

---

# 🚀 SmartRAG - Verileriniz Hakkında Sorular Sorun

**Belgelerinizi, veritabanlarınızı, resimlerinizi ve seslerinizi konuşabilen bir AI sistemine dönüştürün.**

```csharp
// 1. Veritabanlarınızı bağlayın
await connector.ConnectAsync(sqlServer: "Server=localhost;Database=Satis;", 
                              mysql: "Server=localhost;Database=Musteriler;",
                              postgresql: "Host=localhost;Database=Analitik;");

// 2. Belgeleri, PDF'leri, Excel dosyalarını, resimleri yükleyin
await documents.UploadAsync(dosyalar);

// 3. Doğal dilde sorun
var cevap = await intelligence.QueryIntelligenceAsync(
    "100 bin TL üzeri cirosu olan müşterileri tüm veritabanlarından göster"
);
// → AI otomatik olarak SQL Server, MySQL, PostgreSQL sorgular ve sonuçları birleştirir
```

---

## 🎯 Neden SmartRAG?

SmartRAG, birden fazla veritabanını doğal dille sorgulayıp belge zekasıyla birleştirmenize olanak tanır.

✅ **Multi-Database RAG** - SQL Server, MySQL, PostgreSQL, SQLite'ı **tek bir doğal dil isteğinde birlikte** sorgulayın  
✅ **Multi-Modal Zeka** - PDF, Excel, Resim (OCR), Ses (Speech-to-Text) ve Veritabanlarını tek cevapta birleştirin  
✅ **On-Premise Hazır** - Ollama, LM Studio, Whisper.net ile %100 yerel çalışma → KVKK/GDPR/HIPAA uyumlu  
✅ **Üretime Hazır** - Kurumsal düzeyde hata yönetimi, kapsamlı test edilmiş, production-ready  
✅ **Konuşma Geçmişi** - Yerleşik otomatik bağlam yönetimi, birden fazla soru arasında süreklilik  
✅ **.NET Standard 2.1** - .NET Core 3.0+, .NET 5/6/7/8/9 ile çalışır

---

## 📊 Neler Yapabilirsiniz?

### **🏦 Bankacılık - Tam Finansal İstihbarat**
```csharp
"John'un kredi kartı limit artırımı için tam finansal profilini göster"
```
→ AI birleştirir:
- **SQL Server**: 36 ay işlem geçmişi, fatura ödemeleri
- **MySQL**: Kredi kartı kullanım kalıpları
- **PostgreSQL**: Kredi skoru, mevcut krediler
- **SQLite**: Şube ziyaret geçmişi
- **OCR**: Taranmış gelir belgeleri
- **PDF**: Hesap ekstreleri

**Sonuç:** 360° müşteri zekası saatler değil, saniyeler içinde.

---

### **🏥 Sağlık - Birleşik Hasta Kayıtları**
```csharp
"Emily'nin geçen yıla ait tam tıbbi geçmişini göster"
```
→ AI birleştirir:
- **PostgreSQL**: Hasta kayıtları, yatışlar
- **Excel**: 3 farklı laboratuvardan test sonuçları
- **OCR**: Taranmış reçeteler
- **Ses**: Doktorun sesli notları (Whisper.net transkripsiyon)

**Sonuç:** 4 kopuk sistemden tam hasta zaman çizelgesi.

---

### **📦 Envanter - Tahmine Dayalı Analitik**
```csharp
"Önümüzdeki 2 hafta içinde hangi ürünler tükenecek?"
```
→ AI birleştirir:
- **SQLite**: Ürün kataloğu (10.000 SKU)
- **SQL Server**: Satış verileri (ayda 2M işlem)
- **MySQL**: Gerçek zamanlı stok seviyeleri
- **PostgreSQL**: Tedarikçi teslim süreleri

**Sonuç:** Veritabanları arası tahmine dayalı analitik ile stok tükenmelerini önleme.

[10 detaylı gerçek dünya örneğini aşağıda görün ↓](#-gerçek-dünya-örnekleri---smartrag-ile-neler-yapabilirsiniz)

---

## 🆚 SmartRAG vs Diğer .NET RAG Kütüphaneleri

| Özellik | SmartRAG | Semantic Kernel | Kernel Memory |
|---------|:--------:|:---------------:|:-------------:|
| **Multi-Database RAG** | ✅ | ❌ | ❌ |
| **On-Premise (Ollama)** | ✅ %100 | ⚠️ Sınırlı | ⚠️ Sınırlı |
| **OCR + Ses + DB** | ✅ Hepsi bir arada | ❌ Ayrı | ❌ Ayrı |
| **Konuşma Geçmişi** | ✅ Yerleşik | ⚠️ Manuel | ✅ Yerleşik |
| **Multi-Modal** | ✅ 7+ format | ⚠️ Basit | ✅ Multi-modal |
| **.NET Standard 2.1** | ✅ | ❌ (.NET 6+) | ❌ (.NET 6+) |
| **KVKK/HIPAA Hazır** | ✅ Yerel AI | ⚠️ Bulut öncelikli | ⚠️ Bulut öncelikli |
| **Odak** | Multi-DB + RAG | AI Orkestrasyon | RAG-özel |
| **Geliştirici** | Bağımsız | Microsoft | Microsoft |

**Temel Farklar:**
- **Semantic Kernel**: Genel AI orkestrasyon framework'ü, RAG-specific değil
- **Kernel Memory**: RAG odaklı ancak multi-database desteği yok
- **SmartRAG**: Multi-database RAG yeteneklerinde uzmanlaşmış

**Sonuç:** Birden fazla veritabanını AI ile sorgulamanız veya on-premise deployment gerekiyorsa, SmartRAG bunun için tasarlandı.

---

## 📦 Hızlı Başlangıç

### Kurulum
```bash
dotnet add package SmartRAG
```

### 5 Dakikada Kurulum
```csharp
// Program.cs
builder.Services.UseSmartRAG(builder.Configuration,
    aiProvider: AIProvider.OpenAI,
    storageProvider: StorageProvider.InMemory
);

// Controller veya Service
public class MyService
{
    private readonly IDocumentSearchService _intelligence;
    
    public MyService(IDocumentSearchService intelligence)
    {
        _intelligence = intelligence;
    }
    
    public async Task<string> SoruSor(string soru)
    {
        var sonuc = await _intelligence.QueryIntelligenceAsync(soru, maxResults: 5);
        return sonuc.Answer;
    }
}
```

### Konfigürasyon (appsettings.json)
```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-sizin-anahtariniz",
      "Model": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002"
    }
  }
}
```

**Bu kadar!** Artık production-ready bir RAG sisteminiz var. 🎉

[Tam dokümantasyon →](https://byerlikaya.github.io/SmartRAG/tr)

---

## 🔒 Yerinde ve Yerel AI Desteği

**KURUMSAL İÇİN ÖNEMLİ**: SmartRAG, **tam veri gizliliği** ile **tamamen yerinde dağıtım** için tasarlanmıştır. Verileri bulut hizmetlerine göndermeden her şeyi yerel olarak çalıştırabilirsiniz.

### ✅ **%100 Yerel Çalışma** (Bulut Gerekmez)
- **🏠 Yerel AI Modelleri**: Ollama, LM Studio ve OpenAI uyumlu tüm yerel API'ler için tam destek
- **📄 Belge İşleme**: PDF, Word, Excel ayrıştırma - **tamamen yerel**
- **🖼️ OCR İşleme**: Tesseract 5.2.0 - **tamamen yerel**, buluta veri gönderilmez
- **🎤 Ses Transkripsiyonu**: Whisper.net - **tamamen yerel**, 99'dan fazla dili destekler
- **🗄️ Veritabanı Entegrasyonu**: SQLite, SQL Server, MySQL, PostgreSQL - **tümü yerel bağlantılar**
- **💾 Depolama Seçenekleri**: Bellek İçi, SQLite, Dosya Sistemi, Redis - **tümü yerel**
- **🧠 Gömme ve AI**: CustomProvider aracılığıyla kendi yerel modellerinizi kullanın
- **🔐 Tam Gizlilik**: Tüm verileriniz altyapınızda kalır

### ⚠️ **Önemli Notlar**

#### **Ses Dosyaları - Yerel ve Bulut Seçenekleri**
**SmartRAG, maksimum esneklik için hem yerel hem de bulut ses transkripsiyonunu destekler:**

**🏠 Yerel Transkripsiyon (Whisper.net) - VARSAYILAN VE ÖNERİLEN:**
- ✅ **%100 Gizlilik**: Tüm ses işleme yerel olarak gerçekleşir, buluta veri gönderilmez
- ✅ **Çoklu Dil**: Türkçe, İngilizce, Almanca, Rusça, Çince, Arapça dahil 99'dan fazla dil
- ✅ **Model Seçenekleri**: Doğruluk ihtiyaçlarına göre küçük (75 MB) ile büyük (2,9 GB) arasında seçim yapın
- ✅ **Donanım Hızlandırma**: CPU, CUDA (NVIDIA GPU), CoreML (Apple), OpenVino (Intel)
- ✅ **Sıfır Kurulum**: Whisper modeli VE FFmpeg ikili dosyaları ilk kullanımda otomatik olarak indirilir
- ✅ **Maliyet**: Tamamen ücretsiz
- ✅ **GDPR/KVKK/HIPAA**: Yerinde dağıtımlar için tamamen uyumludur
- ⚙️ **Bağımsız**: Manuel kurulum gerekmez, her şey otomatik olarak indirilir

**☁️ Bulut Transkripsiyonu (Google Cloud Speech-to-Text) - İSTEĞE BAĞLI:**
- 📤 İşlenmek üzere Google Cloud'a gönderilen ses verileri
- 💰 Google Cloud API anahtarı ve faturalandırma gerektirir
- ⚡ Gerçek zamanlı transkripsiyon
- 🔒 Veri gizliliği kritik öneme sahipse, bunun yerine Whisper.net'i kullanın

#### **OCR (Görüntüden Metne) Sınırlaması**
**Tesseract OCR kütüphanesi el yazısı metinleri tam olarak destekleyemez (başarı oranı çok düşüktür)**:
- ✅ **Mükemmel çalışır**: Basılı belgeler, taranmış basılı belgeler, yazılmış metin içeren dijital ekran görüntüleri
- ⚠️ **Sınırlı destek**: El yazısı notlar, el yazısı formlar, el yazısı (doğruluk oranı çok düşük, önerilmez)
- 💡 **En iyi sonuçlar**: Basılı belgelerin yüksek kaliteli taramaları, basılı metin içeren net dijital görüntüler
- 🌍 **Desteklenen diller**: 100'den fazla dil - [Desteklenen tüm dilleri görüntüleyin](https://github.com/tesseract-ocr/tessdata)
- 📝 **Öneri**: En iyi OCR sonuçları için basılı metin belgeleri kullanın

### 🏢 **Kurumsal Yerinde Sistemler için Uygun**
- ✅ **GDPR Uyumlu**: Tüm verileri altyapınızda tutun
- ✅ **KVKK Uyumlu**: Türk veri koruma yasasına uygunluk
- ✅ **Air-Gapped Sistemler**: İnternet olmadan %100 çalışır (ses için Whisper.net)
- ✅ **Finansal Kurumlar**: Yerel dağıtım ile banka düzeyinde güvenlik
- ✅ **Sağlık Hizmetleri**: HIPAA uyumlu dağıtımlar mümkündür
- ✅ **Devlet**: Yerel modeller ile gizli veri işleme

### 🛠️ **Yerel AI Kurulum Örnekleri**

#### Ollama (Yerel Modeller)
```json
{
  “AI”: {
    “Custom”: {
      “ApiKey”: “not-needed”,
      “Endpoint”: “http://localhost:11434/v1/chat/completions”,
      “Model”: “llama2”,
      “EmbeddingModel”: “nomic-embed-text”
    }
  }
}
```

#### LM Studio (Yerel Modeller)
```json
{
  “AI”: {
    “Custom”: {
      “ApiKey”: “not-needed”,
      “Endpoint”: “http://localhost:1234/v1/chat/completions”,
      “Model”: “local-model”,
      “EmbeddingModel”: “local-embedding”
    }
  }
}
```
#### Whisper.net (Yerel Ses Transkripsiyonu)
```json
{
  “SmartRAG”: {
    “AudioProvider”: “Whisper”,
    “WhisperConfig”: {
      “ModelPath”: “models/ggml-base.bin”,
      “DefaultLanguage”: “auto”,
      “MinConfidenceThreshold”: 0.5
    }
  }
}
```

**Model Seçenekleri:**
- `ggml-tiny.bin` (75 MB) - Hızlı, iyi doğruluk
- `ggml-base.bin` (142 MB) - Çoğu kullanım durumu için **önerilir**
- `ggml-small.bin` (466 MB) - Daha iyi doğruluk
- `ggml-medium.bin` (1,5 GB) - Mükemmel doğruluk
- `ggml-large-v1.bin` / `ggml-large-v2.bin` / `ggml-large-v3.bin` (2,9 GB) - En iyi doğruluk

**Not**: Hem Whisper modeli hem de FFmpeg ikili dosyaları ilk kullanımda otomatik olarak indirilir.

**Otomatik Kurulum:**
- ✅ Whisper modeli: Hugging Face'ten indirilir (~142 MB temel model için)
- ✅ FFmpeg ikili dosyaları: Otomatik olarak indirilir ve yapılandırılır (~100 MB)
- ✅ Manuel kurulum gerekmez
- ✅ Tek seferlik indirme, ileride kullanmak üzere önbelleğe alınır

**İsteğe bağlı: FFmpeg'i önceden yükleyin** (daha hızlı ilk çalıştırma için):
- **Windows**: `choco install ffmpeg`
- **macOS**: `brew install ffmpeg`
- **Linux**: `sudo apt install ffmpeg`

FFmpeg zaten yüklüyse, SmartRAG bunu otomatik olarak algılar ve kullanır.

### 🎯 **Kurumsal Kullanım Örnekleri**
- **🏦 Bankacılık ve Finans**: Hassas finansal belgeleri yerel olarak işleyin
- **🏥 Sağlık**: Hasta kayıtlarını bulutta ifşa etmeden işleyin
- **⚖️ Hukuk**: Gizli yasal belgeleri şirket içinde yönetin
- **🏛️ Devlet**: Yerel AI ile gizli belge analizi
- **🏭 İmalat**: Endüstriyel sırları ağınız içinde tutun
- **💼 Danışmanlık**: Müşteri verileri altyapınızdan asla çıkmaz

**Verileriniz, altyapınız, kontrolünüz.** 🔐

---

### 💡 **Bu Örnekler Hakkında**

Aşağıdaki örnekler SmartRAG'ın gerçek dünya senaryolarındaki teknik yeteneklerini gösterir:

- ✅ **Tüm özellikler üretime hazır** - Multi-database sorgular, OCR, ses işleme gösterildiği gibi çalışır
- ✅ **Teknik olarak uygulanabilir** - SmartRAG gösterilen tüm gerekli özellikleri sağlar
- ✅ **Uyarlanabilir kalıplar** - Bunları kendi kullanım senaryolarınız için şablon olarak kullanın
- 📋 **Sizin sorumluluğunuz** - İş mantığı, doğrulama kuralları ve düzenleyici uyumluluk

**Öneri**: Bu kalıpları özel kullanım senaryonuza uyarlayın ve üretim dağıtımından önce ilgili düzenlemelere (KVKK, GDPR, HIPAA, finansal düzenlemeler vb.) uyumu sağlayın.

---

## 💡 Gerçek Hayattan Örnekler - SmartRAG ile Neler Yapabilirsiniz

SmartRAG'ın benzersiz çoklu veritabanı ve çoklu mod özelliklerini gösteren 10 güçlü gerçek hayattan örnek:

> 💡 **İpucu**: Soruları ve cevapları görmek için örnek başlıklarına tıklayın

<details>
<summary><h4>1️⃣ 🏥 Tıbbi Kayıtlar Zeka Sistemi</h4></summary>

**Zorluk**: Farklı departmanlara dağılmış olan hastanın tam tıbbi geçmişini elde etmek
```csharp
// Doktor sorar: “Emily Davis'in geçen bir yıldaki tam tıbbi geçmişini gösterin”
```
**SmartRAG'ın Sihirli Gücü**:
- 🗄️ PostgreSQL (hasta kayıtları, yatışlar, taburculuk özetleri)
- 📊 Excel dosyaları (3 farklı laboratuvardan alınan laboratuvar sonuçları)
- 🖼️ OCR ile taranmış reçeteler (el yazısı notlar - sınırlı, daktilo ile yazılmış reçeteler - mükemmel)
- 🎤 Konuşma-Metin Dönüştürme (doktorun randevulardan aldığı sesli notlar)

**AI Yanıtı**:
> "**Emily Davis - Yıllık Sağlık Zaman Çizelgesi**:  
> Mart 2024: Tip 2 Diyabet teşhisi (HbA1c: %8,2) - Dr. Smith  
> Nisan-Haziran: İlaç: Metformin 1000 mg günde iki kez - Laboratuvar sonuçları iyileşiyor (HbA1c: %7,1)  
> Temmuz 2024: Göğüs ağrısı atağı - EKG normal, kardiyolojiye sevk edildi  
> Ağustos 2024: Stres testi tamamlandı - Kalp problemi tespit edilmedi  
> Ekim 2024: Son HbA1c: %6,4 ✅ (kontrol altında) - Mevcut ilaç tedavisine devam"

**⚡ Güç**: 4 veri kaynağı birleştirildi (PostgreSQL + Excel + OCR + Ses) → 4 ayrı sistemden hastanın tam zaman çizelgesi oluşturuldu, doktorların manuel veri toplama için harcadıkları saatler tasarruf edildi.

</details>

<details>
<summary><h4>2️⃣ 🏦 Bankacılık Kredi Limiti Değerlendirmesi</h4></summary>

**Zorluk**: Kredi kartı limitinin artırılması için müşterinin finansal profilini değerlendirmek
```csharp
// İlişki Yöneticisi sorar: “John Smith'in kredi kartı limitini 8.000 dolardan 18.000 dolara çıkarmalı mıyız?”
```
**SmartRAG'ın Sihirli Formülü**:
- 🗄️ SQL Server (36 aylık işlem geçmişi, fatura ödemeleri, maaş yatırma işlemleri)
- 🗄️ MySQL (kredi kartı kullanımı - 18 ay, harcama alışkanlıkları, ödeme davranışı)
- 🗄️ PostgreSQL (mevcut krediler, ipotek, kredi puanı geçmişi)
- 🗄️ SQLite (şube ziyaret geçmişi, müşteri hizmetleri etkileşimleri, şikayetler)
- 🖼️ OCR (taranmış gelir belgeleri, vergi beyannameleri, istihdam doğrulaması)
- 📄 PDF (hesap özetleri, yatırım portföyleri, mülk sahipliği belgeleri)

**AI Yanıtı**:
> "**John Smith - Kredi Limiti Değerlendirme Raporu:**  
>   
> **Gelir ve İstikrar Analizi:**  
> Aylık maaş: 4.200 $ (36 ardışık mevduattan doğrulanmıştır - 8 yıldır istikrarlı işveren)  
> Ek gelir: 1.100 $/ay (kiralık mülk + serbest danışmanlık - vergi OCR ile doğrulanmıştır)  
> Toplam aylık gelir: 5.300 $ ✅ (+22% ilk başvuruya göre)  
>   
> **Mevcut Kredi Davranışı:**  
> Kredi Puanı: 795 (mükemmel) - son 18 ayda 720'den yükseldi  
> Mevcut kart limiti: 8.000 $  
> Ortalama aylık kullanım: 3.600 $ (kullanım oranı %45 - orta düzey)  
> Ödeme geçmişi: %100 zamanında, tam bakiye (18/18 ay)  
> **Önemli bilgi**: Müşteri aylık olarak tam bakiyeyi ödüyor, ödüller için kartı yoğun olarak kullanıyor  
>   
> **Harcama Kalıpları Analizi:**  
> - İş giderleri: 2.100 $/ay (danışmanlık giderleri - 15 gün içinde geri ödenir)  
> - Aile giderleri: 1.500 $/ay  
> - Seyahat sezonunda (Mart, Temmuz, Aralık) ara sıra limite ulaşır  
> - Hiçbir zaman geç ödeme, hiçbir zaman limit aşım ücreti  
>   
> **Mali Yükümlülükler:**  
> Mortgage: 1.000 $/ay (155.000 $ kalan, 18 yıl)  
> Otomobil kredisi yok, başka borç yok  
> Borç-gelir oranı: %19 - Mükemmel  
>   
> **Varlık Doğrulama:**  
> Çek hesabı bakiyesi: 6.700 $ (6 aylık ortalama)  
> Tasarruf: 25.000 $  
> Yatırım hesapları: 47.000 $ (PDF ekstreleri aracılığıyla)  
> Gayrimenkul değeri: ~103.000 $ (ev + kiralık dükkan)  
>   
> **⚠️ Risk Değerlendirmesi:**  
> Temerrüt riski: Çok düşük  
> Dolandırıcılık göstergeleri: Yok  
> Müşteri ömür boyu değeri: Yüksek (8 yıl, birden fazla ürün)  
> Şube geri bildirimi: “VIP müşteri, mükemmel ilişki”  
>   
> **💳 ÖNERİ: LİMİT ARTIRIMINI ONAYLAYIN** ✅  
> Yeni limit: 18.000 $ (125% artış haklı)  
> Ek teklif: Premium kart yükseltme mevcut  
> Çapraz satış fırsatı: Danışmanlık geliri için işletme kredi kartı  
> Risk Seviyesi: MINIMAL  
> Gerekçe: Güçlü gelir artışı, mükemmel ödeme geçmişi, risksiz yüksek kullanım, sağlam varlık tabanı"

**⚡ Güç**: 6 veri kaynağı koordine edildi (SQL Server + MySQL + PostgreSQL + SQLite + OCR belgeleri + PDF'ler) → İşlem kalıpları, kredi davranışı, gelir doğrulama, varlık analizi, müşteri hizmetleri geçmişini birleştiren 360° müşteri finansal istihbaratı - silo sistemlerle kapsamlı risksiz karar verme imkansızdır.

</details>

<details>
<summary><h4>3️⃣ ⚖️ Yasal Emsal Keşif Motoru</h4></summary>

**Zorluk**: 10 yıllık dava geçmişinden kazanan stratejileri bulmak
```csharp
// Avukat soruyor: “Son 5 yılda sözleşme uyuşmazlığı davalarımızı hangi argümanlar kazandı?”
```
**SmartRAG'ın Sihirli Formülü**:
- 📄 1.000'den fazla PDF yasal belge (davaları, özetleri, kararları)
- 🗄️ SQL Server dava veritabanı (sonuçlar, tarihler, hakimler, müvekkiller)
- 🖼️ OCR mahkeme kararları (taranmış resmi belgeler)

**AI Yanıtı**:
> "**1.247 dava analiz edildi, mücbir sebep nedeniyle kazanılan 23 dava tespit edildi:**  
> **Kazanma Modeli Keşfedildi**:  
> ✅ ‘Tanrı'nın işi’ + emsal içtihat hukuku atıfta bulunulduğunda %94 başarı  
> ✅ Belgelenmiş hafifletme çabalarıyla %87 başarı  
> ✅ Yargıç Martinez'in başkanlık ettiği davalarda %91 başarı  
> **Anahtar Argüman**: Öngörülemezlik kanıtı + hafifletme belgeleri birleştirin.  
> **Şablon**: Dava #2019-445 (2,3 milyon dolar kazanıldı) - referans şablonu olarak kullanın"

**⚡ Güç**: 2 veri kaynağı birleştirildi (1.000'den fazla PDF + SQL Server veritabanı) → AI, avukatların manuel olarak bulması haftalar süren kazançlı yasal kalıpları keşfeder.

</details>

<details>
<summary><h4>4️⃣ 📦 Tahmine Dayalı Envanter Zekası</h4></summary>

**Zorluk**: Stok tükenmelerini gerçekleşmeden önlemek
```csharp
// Yönetici sorar: “Önümüzdeki 2 hafta içinde hangi ürünlerin stoğu bitecek?”
```
**SmartRAG Sihirli Formülü**:
- 🗄️ SQLite (ürün kataloğu - 10.000 SKU)
- 🗄️ SQL Server (satış verileri - aylık 2 milyon işlem)
- 🗄️ MySQL (depo envanteri - gerçek zamanlı stok seviyeleri)
- 🗄️ PostgreSQL (tedarikçi verileri - teslimat süreleri, güvenilirlik)

**AI Yanıtı**:
> "**🚨 Stokta kalma riski olan 15 ürün:**  
> **KRİTİK (2-5 gün)**:  
> Ürün A: 12 adet kaldı, günde 8 adet satılıyor, tedarikçi teslim süresi 14 gün ⚠️ HEMEN SİPARİŞ VERİN!  
> Ürün B: 5 adet kaldı, günde 3 adet satılıyor, tedarikçi teslim süresi 7 gün ⚠️ BUGÜN SİPARİŞ VERİN!  
>   
> **UYARI (6-14 gün)**:  
> Ürün C: 45 adet kaldı, günde 5 adet satılıyor (+%52 büyüme trendi)  
> **Öneri**: Ürün A, B ve C için tedarikçilere otomatik satın alma siparişleri gönderildi"

**⚡ Güç**: 4 veritabanı koordinasyonu (SQLite + SQL Server + MySQL + PostgreSQL) → Tek veritabanı sorgularıyla imkansız olan, stok tükenmelerini önceden önleyen çapraz veritabanı tahmine dayalı analitik.

</details>

<details>
<summary><h4>5️⃣ 🏭 Üretimde Kök Neden Analizi</h4></summary>

**Zorluk**: Geçen hafta üretim kalitesinin düşmesinin nedenini bulmak
```csharp
// Kalite Müdürü soruyor: “Geçen haftaki üretim partisinde neden 47 kusur vardı?”
```

**SmartRAG Magic**:
- 📊 Excel üretim raporları (5 üretim hattı, saatlik veriler)
- 🗄️ PostgreSQL sensör verileri (sıcaklık, basınç, hız - 100.000'den fazla okuma)
- 🖼️ OCR kalite kontrol fotoğrafları (denetçi notları ile birlikte kusurlu görüntüler)
- 📄 PDF bakım günlükleri (ekipman servis geçmişi)

**AI Yanıtı**:
> "**Temel Neden Belirlendi: Makine #3 Sıcaklık Değişimi**  
> **Kanıt**:  
> - Kusurların %94'ü Makine #3 sıcaklığı > 185°C olduğunda meydana geldi  
> - Normal çalışma aralığı: 175-180°C  
> - Sıcaklık artışları, soğutma sistemi arızasıyla ilişkili  
> - Bakım 12 gün gecikmiş (son servis: 15 Ağustos)  
> **Eylem**: Acil bakım planlayın. Tahmini maliyet: 2.400 $ karşı 45.000 $ parti kaybı"

**⚡ Güç**: 4 veri kaynağı birleştirildi (Excel raporları + PostgreSQL 100.000+ sensör okuması + OCR fotoğrafları + PDF günlükleri) → AI, milyonlarca veri noktasındaki sıcaklık anormalliklerini ilişkilendirerek kesin kök nedeni belirler.

</details>

<details>
<summary><h4>6️⃣ 👔 AI Özgeçmiş Taraması Ölçeklendirme</h4></summary>

**Zorluk**: 500'den fazla başvurudan en uygun adayları bulmak
```csharp
// İK soruyor: “Python becerileri ve AWS sertifikaları olan kıdemli React geliştiricileri bulun”
```
**SmartRAG'ın Sihirli Gücü**:
- 📄 500'den fazla özgeçmiş PDF'si (birden çok dil, format)
- 🗄️ Başvuru sahibi veritabanı (SQL Server - beceriler, deneyim, eğitim)
- 🖼️ OCR ile taranmış sertifikalar (AWS, Azure, Google Cloud sertifikaları)
- 🎤 Konuşma-Metin video görüşme transkriptleri

**AI Yanıtı**:
> "**En Uygun 8 Aday Bulundu:**  
> **#1 - Sarah Chen** ⭐⭐⭐⭐⭐  
> React: 7 yıl (3 büyük projeyi yönetti), Python: 8 yıl (ML uzmanlığı)  
> AWS Çözüm Mimarı + DevOps Uzmanı (OCR ile doğrulanmıştır)  
> Mülakatın öne çıkan özelliği: '10 milyon kullanıcıya hizmet veren ölçeklenebilir mikro hizmetler oluşturdu'  
> Uygunluk: 2 hafta önceden haber verilmesi  
>   
> **Sarah'nın 1 numara olmasının nedeni**: Kanıtlanmış ölçek (10 milyon kullanıcı) + çift AWS sertifikasına sahip tek aday"

**⚡ Güç**: 4 veri kaynağı birleştirildi (500'den fazla PDF + SQL Server + OCR sertifikaları + Sesli mülakatlar) → AI, adayları birden fazla veri türüne göre tarar ve sıralar, böylece İK'nın adayları tarama süresini günlerden dakikalara indirir.

</details>

<details>
<summary><h4>7️⃣ 💼 Finansal Denetim Otomasyonu</h4></summary>

**Zorluk**: Departmanlar arasında gider anomalilerini tespit etmek
```csharp
// CFO soruyor: “Temmuz-Eylül aylarında onay durumuna sahip 10.000 doların üzerindeki tüm giderleri gösterin.”
```
**SmartRAG'ın Sihirli Gücü**:
- 📊 Excel finansal raporları (12 departman, 15.000 satır öğesi)
- 🗄️ SQL Server işlem veritabanı (500.000'den fazla işlem)
- 🖼️ OCR tedarikçi faturaları (taranmış faturalar, makbuzlar)
- 📄 PDF onay iş akışları (e-posta izleri, imzalar)

**AI Yanıtı**:
> "**Temmuz-Eylül Yüksek Değerli Giderler - 10.000 $ Üzerindeki 7 Kalem:**  
> ✅ 15.400 $ BT Ekipmanı - CFO tarafından onaylandı (normal satın alma)  
> ⚠️ 23.000 $ Pazarlama Kampanyası - **ONAY YOK** (politika ihlali!)  
> ✅ 12.500 $ Ofis Yenileme - CEO tarafından onaylandı (bütçenin %40 üzerinde ancak yetkilendirildi)  
> 🚨 18.900 $ Danışmanlık Ücretleri - Satıcı onaylı listede yok (uyum sorunu!)  
>   
> **Gerekli Eylem**: ⚠️🚨 ile işaretlenmiş kalemleri Finans Direktörü ile birlikte inceleyin"

**⚡ Güç**: 4 veri kaynağı çapraz doğrulandı (Excel 15K satır öğesi + SQL Server 500K işlem + OCR faturaları + PDF onayları) → AI denetçisi, insanların gözden kaçırabileceği politika ihlallerini tespit eder.

</details>

<details>
<summary><h4>8️⃣ 🏛️ Akıllı Devlet Hizmetleri</h4></summary>

**Zorluk**: Binlerce vatandaş başvurusunu verimli bir şekilde işleme
```csharp
// Yetkili soruyor: “Eylül 2024'te İstanbul'da kaç tane inşaat ruhsatı var? Ortalama işlem süresi nedir?”
```
**SmartRAG'ın Sihirli Gücü**:
- 🗄️ PostgreSQL vatandaş veritabanı (15 milyon vatandaş kaydı)
- 🖼️ OCR başvuru formları (taranmış belgeler, planlar)
- 🎤 Konuşma-metin dönüştürme çağrı merkezi kayıtları (vatandaşların soruları)
- 📄 PDF düzenleme belgeleri (imar kanunları, bina yönetmelikleri)

**AI Yanıtı**:
> "**İstanbul İnşaat İzinleri - Eylül 2024:**  
> Toplam başvuru sayısı: 512 | Onaylanan: 342 (%67) | Reddedilen: 125 | Bekleyen: 45  
> Ortalama işlem süresi: 18 gün (hedef: 15 gün)  
> En yaygın onay nedeni: Konut inşaatı (78%)  
> En yaygın reddetme nedeni: İmar yönetmeliği ihlalleri (62%)  
> **İçgörü**: Reddedilen başvuruların %34'ü daha iyi bir ilk rehberlikle önlenebilirdi."

**⚡ Güç**: 4 veri kaynağı birleştirildi (PostgreSQL 15 milyon kayıt + OCR formları + Sesli aramalar + PDF düzenlemeleri) → Tek bir sorguda tüm vatandaş temas noktalarında hükümet analitiği.

</details>

<details>
<summary><h4>9️⃣ 🔬 Akademik Araştırma Desen Keşfi</h4></summary>

**Zorluk**: Yıllarca süren araştırmalardan içgörüler elde etmek
```csharp
// Profesör soruyor: “Son 5 yılda makine öğrenimi araştırmalarımızda ortak başarı faktörleri nelerdir?”
```
**SmartRAG'ın Sihirli Formülü**:
- 📄 50'den fazla araştırma makalesi PDF'si (toplam 300'den fazla sayfa)
- 📊 Deney verileri Excel dosyaları (100.000'den fazla veri noktası)
- 🖼️ OCR laboratuvar defterleri (yazılı deney notları - harika çalışıyor!)
- 🗄️ SQLite araştırma veritabanı (deney parametreleri, sonuçlar)

**AI Yanıtı**:
> "**50 Makine Öğrenimi Araştırma Makalesinin Meta Analizi (2020-2024):**  
> **Belirlenen Başarı Modeli**:  
> ✅ Transfer öğrenimi: +%18 ortalama doğruluk artışı (42/50 makale)  
> ✅ Veri artırma: 10.000'den az örnek içeren veri kümeleri için kritik öneme sahip (38/50 makale)  
> ✅ Ensemble yöntemleri: 47/50 deneyde tekli modelleri geride bıraktı  
> **Altın Kombinasyon**: Transfer öğrenimi + Veri artırma + Ensemble = %34 iyileşme  
> **Sonraki araştırma yönü**: Bu kombinasyonu tıbbi görüntülemeye uygulamak (keşfedilmemiş alan)"

**⚡ Güç**: 4 veri kaynağı birleştirildi (50 PDF 300+ sayfa + Excel 100K veri noktası + OCR defterleri + SQLite deneyleri) → 5 yıllık araştırma boyunca yapılan AI meta analizi, insanların hiç fark etmediği kalıpları keşfetti.

</details>

<details>
<summary><h4>🔟 🚚 Akıllı Lojistik Komuta Merkezi</h4></summary>

**Zorluk**: Gerçek zamanlı tedarik zinciri görünürlüğü ve optimizasyonu
```csharp
// Lojistik Direktörü sorar: “Sevkiyatlar neden gecikiyor? Bana darboğazları gösterin.”
```
**SmartRAG Sihirli Formülü**:
- 🗄️ PostgreSQL sevkiyat takibi (gerçek zamanlı GPS verileri, 5000'den fazla aktif sevkiyat)
- 🗄️ MySQL depo envanteri (stok seviyeleri, 50 depo)
- 🗄️ SQL Server sipariş veritabanı (müşteri siparişleri, öncelikler, SLA'lar)
- 🖼️ OCR teslimat makbuzları (taranmış POD - Teslimat Belgesi belgeleri)

**AI Yanıtı**:
> "**18 Geciken Sevkiyat - Temel Neden Analizi:**  
> **Birincil Engelleme Noktası: Depo B Envanter Eksikliği** (12 sevkiyat etkilendi)  
> - Ürün X: 10 Ekim'den beri stokta yok (tedarikçi gecikti)  
> - Etki: 125.000 $ gelir riski  
> - Alternatif: Depo D'de Ürün X var (85 adet) - rota değişikliği mümkün  
>   
> **İkincil Sorun: Rota Optimizasyonu** (4 sevkiyat)  
> - İstanbul→Ankara rotası: +2,3 gün ortalama gecikme (trafik düzeni değişti)  
> - **Çözüm**: Eskişehir üzerinden alternatif rota (-1,5 gün, uygulandı)  
>   
> **Gümrük Gecikmeleri** (2 sevkiyat): Normal işlem, işlem gerekmez"

**⚡ Güç**: 4 veri kaynağı koordine edildi (PostgreSQL GPS izleme + MySQL envanter 50 depo + SQL Server siparişleri + OCR makbuzları) → Tüm lojistik ağında gerçek zamanlı tedarik zinciri optimizasyonu.

</details>

---

### 🎯 **SmartRAG'ı Güçlü Kılan Nedir**

#### **🗄️ Çoklu Veritabanı RAG Yetenekleri**
- Birden fazla veritabanı türünü aynı anda sorgular (SQL Server, MySQL, PostgreSQL, SQLite)
- Tek bir akıllı istekle veritabanları arası sorguları koordine eder
- AI destekli veritabanları arası birleştirme ve korelasyonlar
- Heterojen veritabanı sistemleri arasında birleşik sorgu arayüzü

#### **📊 Çok Modlu Zeka**
- PDF + Excel + Görüntüler (OCR) + Ses (Konuşma) + Veritabanlarını tek bir cevapta birleştirir
- Tüm veri türlerinizde birleşik zeka
- Yapılandırılmış ve yapılandırılmamış veriler arasında sorunsuz entegrasyon

#### **🔒 Yerinde Gizlilik**
- Ollama/LM Studio + Whisper.net ile %100 yerel çalışma
- GDPR/KVKK/HIPAA uyumlu dağıtımlar
- Hassas verileriniz ASLA altyapınızı terk etmez
- Finans kurumları, sağlık hizmetleri, hukuk, devlet kurumları için çok uygundur

#### **🌍 Dilden Bağımsız**
- Türkçe, İngilizce, Almanca, Rusça, Çince, Arapça - **HERHANGİ BİR** dilde çalışır
- Sabit kodlanmış dil kalıpları veya anahtar kelimeler yoktur
- Gerçekten uluslararası bir RAG çözümü

#### **✅ Üretime Hazır**
- Kapsamlı hata işleme ve yeniden deneme mekanizmaları
- Kurumsal düzeyde günlük kaydı ve izleme
- Production-ready, kapsamlı testlerle doğrulanmış

**Akıllı belge işlemenin geleceğini inşa edin - BUGÜN!** 🚀

---

## 🎯 SmartRAG'ı Özel Kılan Nedir?

### 🚀 **Eksiksiz RAG İş Akışı**
```
📄 Belge Yükleme → 🔍 Akıllı Parçalama → 🧠 AI Gömme → 💾 Vektör Depolama
                                                                        ↓
🙋‍♂️ Kullanıcı Sorusu → 🎯 Niyet Algılama → 🔍 İlgili Parçaları Bulma → 🧠 QueryIntelligenceAsync → ✨ Akıllı Yanıt
```

### 🏆 **Anahtar Özellikler**
- **Gelişmiş OCR Yetenekleri**: Tesseract 5.2.0 + SkiaSharp entegrasyonu ile kurumsal seviye görüntü işleme
- **Akıllı Parçalama**: Kelime sınırı doğrulaması ile belge segmentleri arasında bağlam sürekliliğini korur
- **Akıllı Sorgu Yönlendirme**: Genel konuşmaları otomatik olarak AI sohbetine, belge sorgularını QueryIntelligenceAsync'e yönlendirir
- **Konuşma Geçmişi**: Akıllı bağlam kısaltma ile otomatik oturum tabanlı konuşma yönetimi
- **Dilden Bağımsız Tasarım**: Sabit kodlanmış dil kalıpları yok - herhangi bir dille global olarak çalışır
- **Çoklu Depolama Seçenekleri**: Bellek içinden kurumsal vektör veritabanlarına kadar
- **AI Sağlayıcı Esnekliği**: Kod değişikliği olmadan sağlayıcılar arasında geçiş yapın
- **Evrensel Belge Zekası**: PDF, Word, Excel, metin formatları VE OCR ile görseller için gelişmiş ayrıştırma
- **Yapılandırma Öncelikli**: Mantıklı varsayılanlarla ortam tabanlı yapılandırma
- **Bağımlılık Enjeksiyonu**: Tam DI container entegrasyonu
- **Gelişmiş Semantik Arama**: Semantik benzerlik ve anahtar kelime uygunluğunu birleştiren gelişmiş hibrit puanlama (%80 semantik + %20 anahtar kelime)
- **VoyageAI Entegrasyonu**: Anthropic Claude modelleri için yüksek kaliteli embedding desteği
- **Çapraz Platform Uyumluluğu**: .NET Standard 2.1 desteği (.NET Core 3.0+ ve .NET 5/6/7/8/9)
- **Üretime Hazır**: Thread-safe işlemler, merkezi günlükleme, düzgün hata işleme
- **Profesyonel Dokümantasyon**: GitHub Pages entegrasyonu ile kapsamlı dokümantasyon sitesi

### 🧠 **Temel Servisler**
- **`IDocumentSearchService`**: RAG pipeline ve konuşma yönetimi ile akıllı sorgu işleme
- **`ISemanticSearchService`**: Hibrit puanlama ile gelişmiş semantik arama
- **`IAIService`**: Evrensel AI sağlayıcı entegrasyonu (OpenAI, Anthropic, Gemini, Azure, Custom)
- **`IDocumentParserService`**: Çoklu format belge ayrıştırma (PDF, Word, Excel, OCR ile Görüntüler, Konuşmadan Metne ile Ses)
- **`IDatabaseParserService`**: Canlı bağlantılarla evrensel veritabanı desteği (SQLite, SQL Server, MySQL, PostgreSQL)
- **`IStorageProvider`**: Kurumsal depolama seçenekleri (Vektör veritabanları, Redis, SQL, FileSystem)
- **`IAIProvider`**: Otomatik yük devretme ile takılabilir AI sağlayıcı mimarisi

### 🎯 **Pratik OCR Kullanım Durumları**
- **📄 Taranmış Belgeler**: Taranmış sözleşmeleri, raporları, formları yükleyin ve anında akıllı yanıtlar alın
- **🧾 Makbuz İşleme**: Makbuzları, faturaları ve finansal belgeleri OCR + RAG zekasıyla işleyin
- **📊 Görüntü Tabanlı Raporlar**: Grafiklerden, çizelgelerden ve görsel raporlardan veri çıkarın ve sorgulayın
- **✍️ El Yazısı Notlar**: El yazısı notları, açıklamaları aranabilir bilgi tabanına dönüştürün
- **📱 Ekran Görüntüsü Analizi**: Metin içeriği olan ekran görüntülerini, UI yakalamaları ve dijital görselleri işleyin
- **🏥 Tıbbi Belgeler**: Tıbbi raporları, reçeteleri ve sağlık belgelerini işleyin
- **📚 Eğitim Materyalleri**: Ders kitaplarından, el ilanlarından ve eğitim görsellerinden içerik çıkarın
- **🏢 İş Belgeleri**: Kartvizitleri, sunumları ve kurumsal materyalleri işleyin

## 🧠 Akıllı Sorgu Niyeti Algılama

SmartRAG, sorgunuzun genel bir konuşma mı yoksa belge arama isteği mi olduğunu otomatik olarak algılar:

### **Genel Konuşma** (Doğrudan AI Sohbeti)
- ✅ **"Nasılsın?"** → Doğrudan AI yanıtı
- ✅ **"Hava nasıl?"** → Doğrudan AI yanıtı
- ✅ **"Bana bir fıkra anlat"** → Doğrudan AI yanıtı
- ✅ **"Emin misin?"** → Doğrudan AI yanıtı (Türkçe)
- ✅ **"你好吗？"** → Doğrudan AI yanıtı (Çince)

### **Belge Arama** (Belgelerinizle RAG)
- 🔍 **"Sözleşmedeki ana faydalar nelerdir?"** → Belgelerinizi arar
- 🔍 **"Çalışan maaş bilgileri nedir?"** → Belgelerinizi arar (Türkçe)
- 🔍 **"2025年第一季度报告的主要发现是什么？"** → Belgelerinizi arar (Çince)
- 🔍 **"Çalışan maaş verilerini göster"** → Belgelerinizi arar

**Nasıl çalışır:** Sistem, herhangi bir sabit kodlanmış dil kalıbı olmadan niyeti belirlemek için sorgu yapısını (sayılar, tarihler, formatlar, uzunluk) analiz eder.

## 🎯 Gelişmiş Semantik Arama & Parçalama

### **🧠 Gelişmiş Semantik Arama**
SmartRAG, birden fazla uygunluk faktörünü birleştiren sofistike bir **hibrit puanlama sistemi** kullanır:

```csharp
// Hibrit Puanlama Algoritması (%80 Semantik + %20 Anahtar Kelime)
var hybridScore = (enhancedSemanticScore * 0.8) + (keywordScore * 0.2);

// Gelişmiş Semantik Benzerlik
var enhancedSemanticScore = await _semanticSearchService
    .CalculateEnhancedSemanticSimilarityAsync(query, chunk.Content);

// Anahtar Kelime Uygunluğu
var keywordScore = CalculateKeywordRelevanceScore(query, chunk.Content);
```

**Puanlama Bileşenleri:**
- **Semantik Benzerlik (%80)**: Bağlam farkındalığı ile gelişmiş metin analizi
- **Anahtar Kelime Uygunluğu (%20)**: Geleneksel metin eşleştirme ve frekans analizi
- **Bağlamsal Geliştirme**: Semantik tutarlılık ve bağlamsal anahtar kelime tespiti
- **Alan Bağımsızlığı**: Sabit kodlanmış alan kalıpları olmadan genel puanlama

### **🔍 Akıllı Belge Parçalama**
Bağlamı koruyan ve kelime bütünlüğünü sağlayan gelişmiş parçalama algoritması:

```csharp
// Kelime Sınırı Doğrulama
private static int ValidateWordBoundary(string content, int breakPoint)
{
    // Parçaların kelimeleri ortasından kesmemesini sağlar
    // Cümle, paragraf veya kelime sınırlarında en uygun kesme noktalarını bulur
    // Parçalar arasında anlamsal sürekliliği korur
}

// Optimum Kesme Noktası Algılama
private static int FindOptimalBreakPoint(string content, int startIndex, int maxChunkSize)
{
    // 1. Cümle sınırları (tercih edilen)
    // 2. Paragraf sınırları (ikincil)
    // 3. Kelime sınırları (yedek)
    // 4. Karakter sınırları (son çare)
}
```

**Parçalama Özellikleri:**
- **Kelime Sınırı Koruması**: Kelimeleri asla ortasından kesmez
- **Bağlam Koruması**: Parçalar arasında anlamsal sürekliliği korur
- **Optimum Kesme Noktaları**: Parça sınırlarının akıllı seçimi
- **Çakışma Yönetimi**: Bağlam sürekliliği için yapılandırılabilir çakışma
- **Boyut Optimizasyonu**: İçerik yapısına göre dinamik parçalama boyutlandırma

## 📦 Kurulum

### NuGet Paket Yöneticisi
```bash
Install-Package SmartRAG
```

### .NET CLI
```bash
dotnet add package SmartRAG
```

### PackageReference
```xml
<PackageReference Include="SmartRAG" Version="3.0.0" />
```

## 📄 Desteklenen Belge Biçimleri

SmartRAG, akıllı ayrıştırma ve metin çıkarma özelliği ile çok çeşitli belge biçimlerini destekler:

### **📊 Excel Dosyaları (.xlsx, .xls)**
- **Gelişmiş Ayrıştırma**: Tüm çalışma sayfalarından ve hücrelerden metin çıkarır
- **Yapılandırılmış Veriler**: Sekmeyle ayrılmış değerlerle tablo yapısını korur
- **Çalışma Sayfası Adları**: Bağlam için çalışma sayfası adlarını içerir
- **Hücre İçeriği**: Boş olmayan tüm hücre değerlerini çıkarır
- **Biçim Koruma**: Daha iyi bağlam için veri düzenini korur

### **📝 Word Belgeleri (.docx, .doc)**
- **Zengin Metin Çıkarma**: Biçimlendirme ve yapıyı korur
- **Tablo Desteği**: Tablolardan ve listelerden içerik çıkarır
- **Paragraf İşleme**: Paragraf sonlarını ve akışını korur
- **Meta Veri Koruma**: Belge yapısını bozulmadan korur

### **📋 PDF Belgeleri (.pdf)**
- **Çok Sayfalı Destek**: Metin çıkarma ile tüm sayfaları işler
- **Düzen Koruma**: Belge yapısını ve akışını korur
- **Metin Kalitesi**: Analiz için yüksek kaliteli metin çıkarma
- **Sayfa Ayırma**: Bağlam için net sayfa sınırları

### **📄 Metin Dosyaları (.txt, .md, .json, .xml, .csv, .html, .htm)**
- **Evrensel Destek**: Tüm metin tabanlı formatları işler
- **Kodlama Algılama**: Otomatik UTF-8 ve kodlama algılama
- **Yapı Koruma**: Orijinal biçimlendirmeyi korur
- **Hızlı İşleme**: Metin tabanlı içerik için optimize edilmiştir

### **🖼️ Görüntü Dosyaları (.jpg, .jpeg, .png, .gif, .bmp, .tiff, .webp) - GELİŞMİŞ OCR İŞLEME**
- **🚀 Gelişmiş OCR Motoru**: SkiaSharp 3.119.0 entegrasyonlu kurumsal düzeyde Tesseract 5.2.0
- **🌍 Çok Dilli OCR**: İngilizce (eng), Türkçe (tur) ve genişletilebilir dil çerçevesi
- **🔄 WebP'den PNG'ye Dönüştürme**: Tesseract uyumluluğu için SkiaSharp kullanılarak kesintisiz WebP görüntü işleme
- **📊 Akıllı Tablo Çıkarma**: Görüntülerden gelişmiş tablo algılama ve yapılandırılmış veri ayrıştırma
- **🎯 Karakter Beyaz Listesi**: Yüksek doğruluk için optimize edilmiş OCR karakter tanıma
- **⚡ Görüntü Ön İşleme Boru Hattı**: Maksimum OCR performansı için gelişmiş görüntü iyileştirme
- **📈 Güven Puanı**: İşlem süresi takibi ile ayrıntılı OCR güven ölçütleri
- **🔍 Otomatik Format Algılama**: Desteklenen tüm türlerde otomatik görüntü formatı algılama ve doğrulama
- **🏗️ Yapılandırılmış Veri Çıktısı**: Görüntüleri aranabilir, sorgulanabilir bilgi tabanı içeriğine dönüştürür

### **🎵 Ses Dosyaları (.mp3, .wav, .m4a, .aac, .ogg, .flac, .wma) - YEREL VE BULUT TRANSCRIPTION**
- **🏠 Whisper.net (Yerel - VARSAYILAN)**: OpenAI'nin Whisper modelini kullanarak %100 gizlilik koruyan yerel transkripsiyon
- **🌍 Çoklu Dil Desteği**: Türkçe, İngilizce, Almanca, Rusça, Çince, Arapça dahil 99'dan fazla dil
- **⚙️ Donanım Hızlandırma**: CPU, CUDA (NVIDIA GPU), CoreML (Apple Silicon), OpenVino (Intel)
- **📦 Model Seçenekleri**: Küçük (75 MB), Temel (142 MB - Önerilen), Küçük (466 MB), Orta (1,5 GB), Büyük-v1/v2/v3 (2,9 GB)
- **🔄 Otomatik İndirme**: Modeller, Hugging Face'ten ilk kullanımda otomatik olarak indirilir
- **☁️ Google Cloud (İsteğe Bağlı)**: Kurumsal düzeyde bulut transkripsiyon alternatifi
- **📊 Güven Puanı**: Ayrıntılı transkripsiyon güven ölçütleri
- **⏱️ Zaman Damgaları**: İsteğe bağlı kelime düzeyinde ve segment düzeyinde zaman damgası çıkarma
- **🔍 Format Algılama**: Otomatik ses formatı doğrulama ve içerik türü tanıma
- **🏗️ Yapılandırılmış Çıktı**: Ses içeriğini aranabilir, sorgulanabilir bilgi tabanına dönüştürür

### **🗄️ Çoklu Veritabanı Desteği (SQLite, SQL Server, MySQL, PostgreSQL)**
- **🚀 Canlı Veritabanı Bağlantıları**: SQLite, SQL Server, MySQL, PostgreSQL'e gerçek zamanlı veri erişimi ile bağlanın
- **📊 Akıllı Şema Analizi**: Veri türleri ve kısıtlamaları ile otomatik tablo şeması çıkarma
- **🔗 İlişki Eşleme**: Yabancı anahtar ilişkileri ve dizin bilgisi çıkarma
- **🛡️ Güvenlik Öncelikli**: Otomatik hassas veri temizleme ve yapılandırılabilir veri koruma
- **⚡ Performans Optimizasyonu**: Yapılandırılabilir satır sınırları, sorgu zaman aşımları ve bağlantı havuzu
- **🎯 Akıllı Filtreleme**: Gelişmiş filtreleme seçenekleriyle belirli tabloları dahil etme/hariç tutma
- **📈 Kurumsal Özellikler**: Bağlantı doğrulama, özel SQL sorgu yürütme ve hata işleme
- **🌐 Çapraz Platform**: Bulut veritabanlarıyla çalışır (Azure SQL, AWS RDS, Google Cloud SQL)
- **🔍 Meta Veri Çıkarma**: Sütun ayrıntıları, birincil anahtarlar, dizinler ve veritabanı sürüm bilgileri
- **🏗️ Yapılandırılmış Çıktı**: Veritabanı içeriğini aranabilir, sorgulanabilir bilgi tabanına dönüştürür

### **🔍 İçerik Türü Desteği**
SmartRAG, hem dosya uzantılarını hem de MIME içerik türlerini kullanarak dosya türlerini otomatik olarak algılar:
- **Excel**: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, `application/vnd.ms-excel`
- **Word**: `application/vnd.openxmlformats-officedocument.wordprocessingml.document`, `application/msword`
- **PDF**: `application/pdf`
- **Metin**: `text/*`, `application/json`, `application/xml`, `application/csv`
- **Görüntüler**: `image/jpeg`, `image/png`, `image/gif`, `image/bmp`, `image/tiff`, `image/webp`
- **Ses**: `audio/mpeg`, `audio/wav`, `audio/mp4`, `audio/aac`, `audio/ogg`, `audio/flac`, `audio/x-ms-wma`
- **Veritabanları**: `application/x-sqlite3`, `application/vnd.sqlite3`, `application/octet-stream`

## 🚀 Hızlı Başlangıç

### 1. **Geliştirme Kurulumu**
```bash
# Depoyu klonlayın
git clone https://github.com/byerlikaya/SmartRAG.git
cd SmartRAG

# Geliştirme yapılandırma şablonunu kopyalayın
cp examples/WebAPI/appsettings.Development.template.json examples/WebAPI/appsettings.Development.json

# appsettings.Development.json dosyasını API anahtarlarınızla düzenleyin
# - OpenAI API Anahtarı
# - Azure OpenAI kimlik bilgileri
# - Veritabanı bağlantı dizeleri
```

### 2. **Temel Kurulum**
```csharp
using SmartRAG.Extensions;
using SmartRAG.Enums;

var builder = WebApplication.CreateBuilder(args);

// Minimum yapılandırma ile SmartRAG'ı ekleyin
builder.Services.UseSmartRAG(builder.Configuration,
    storageProvider: StorageProvider.InMemory,  // Basit bir şekilde başlayın
    aiProvider: AIProvider.OpenAI               // Tercih ettiğiniz AI
);

var app = builder.Build();
```

### 3. **Belgeleri Yükleyin ve Veritabanlarını Bağlayın**
```csharp
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDatabaseParserService _databaseService;

    // Dosyaları yükleyin (PDF, Word, Excel, Görüntüler, Ses, SQLite veritabanları)
    [HttpPost(“upload”)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var document = await _documentService.UploadDocumentAsync(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            “user-123”
        );
        
        return Ok(document);
    }

    // Canlı veritabanlarına bağlan (SQL Server, MySQL, PostgreSQL)
    [HttpPost(“connect-database”)]
    public async Task<IActionResult> ConnectDatabase([FromBody] DatabaseRequest request)
    {
        var config = new DatabaseConfig
        {
            Type = request.DatabaseType,
            ConnectionString = request.ConnectionString,
            IncludedTables = request.Tables,
            MaxRowsPerTable = 1000,
            SanitizeSensitiveData = true
        };

        var content = await _databaseService.ParseDatabaseConnectionAsync(
            request.ConnectionString, 
            config);
        
        return Ok(new { content, message = “Database connected successfully” });
    }
}
```

### 4. **Konuşma Geçmişiyle AI Destekli Soru Yanıtlama**
```csharp
public class QAController : ControllerBase
{
    private readonly IDocumentSearchService _documentSearchService;

    [HttpPost(“ask”)]
    public async Task<IActionResult> AskQuestion([FromBody] QuestionRequest request)
    {
        // Kullanıcı soruyor: “Sözleşmede belirtilen başlıca avantajlar nelerdir?”
        var response = await _documentSearchService.QueryIntelligenceAsync(
            request.Question,
            maxResults: 5
        );
        
        // Belge içeriği + konuşma bağlamına dayalı akıllı yanıt döndürür
        return Ok(response);
    }
}

public class QuestionRequest
{
    public string Question { get; set; } = string.Empty;
}
```

### 5. **Yapılandırma**

⚠️ **Güvenlik Notu**: Asla gerçek API anahtarlarını kaydetmeyin! Yerel geliştirme için `appsettings.Development.json` kullanın.

```bash
# Şablonu kopyalayın ve gerçek anahtarlarınızı ekleyin.
cp examples/WebAPI/appsettings.json examples/WebAPI/appsettings.Development.json
```

**appsettings.Development.json** (gerçek anahtarlarınız):
```json
{
  “AI”: {
    “OpenAI”: {
      “ApiKey”: “sk-proj-YOUR_REAL_KEY”,
      “Model”: “gpt-4”,
      “EmbeddingModel”: “text-embedding-ada-002”
    },
    “Anthropic”: {
      “ApiKey”: “sk-ant-YOUR_REAL_KEY”,
      “Model”: “claude-3.5-sonnet”,
      “EmbeddingApiKey”: “voyage-YOUR_REAL_KEY”,
      “EmbeddingModel”: “voyage-large-2”
    }
  },
  “Storage”: {
    “InMemory”: {
      “MaxDocuments”: 1000
    }
  },
  "Database": {
    "MaxRowsPerTable": 1000,
    "QueryTimeoutSeconds": 30,
    "SanitizeSensitiveData": true,
    "SensitiveColumns": ["password", "ssn", "credit_card", "email"]
  }
}
```


### 🔑 **Anthropic Kullanıcıları için Önemli Not**
**Anthropic Claude modelleri, embedding için ayrı bir VoyageAI API anahtarı gerektirir:**
- **Neden?** Anthropic embedding modeli sağlamadığından, VoyageAI'nin yüksek kaliteli embedding'lerini kullanıyoruz
- **Resmi Belgeler:** [Anthropic Embedding Kılavuzu](https://docs.anthropic.com/en/docs/build-with-claude/embeddings#how-to-get-embeddings-with-anthropic)
- **API Anahtarı Alın:** [VoyageAI API Anahtarları](https://console.voyageai.com/)
- **Modeller:** `voyage-large-2` (önerilen), `voyage-code-2`, `voyage-01`
- **Belgeler:** [VoyageAI Embedding API](https://docs.voyageai.com/embeddings/)

## 🤖 AI Sağlayıcıları - Evrensel Destek

### 🎯 **Özel Sağlayıcılar** (Optimize Edilmiş ve Üretimde Doğrulanmış)

| Sağlayıcı | Yetenekler | Özel Özellikler |
|----------|-------------|------------------|
| **🤖 OpenAI** | ✅ En yeni GPT modelleri<br/>✅ Gelişmiş embedding modelleri | Endüstri standardı, güvenilir, kapsamlı model ailesi |
| **🧠 Anthropic** | ✅ Claude ailesi modelleri<br/>✅ VoyageAI embedding desteği | Güvenlik odaklı, anayasal AI, uzun bağlam, ayrı VoyageAI API anahtarı gerektirir |
| **🌟 Google Gemini** | ✅ Gemini modelleri<br/>✅ Çok modlu embedding | Çok modlu destek, en son Google AI yenilikleri |
| **☁️ Azure OpenAI** | ✅ Kurumsal GPT modelleri<br/>✅ Kurumsal embedding | GDPR uyumlu, kurumsal güvenlik, SLA desteği |

### 🛠️ **CustomProvider** - Evrensel API Desteği
**Hepsini tek bir sağlayıcıyla yönetin!** OpenAI uyumlu herhangi bir API'ye bağlanın:

```json
{
  “AI”: {
  “Custom”: {
    “ApiKey”: “your-api-key”,
      “Endpoint”: “https://api.openrouter.ai/v1/chat/completions”,
      “Model”: “anthropic/claude-3.5-sonnet”,
      “EmbeddingModel”: “text-embedding-ada-002”
    }
  }
}
```

**CustomProvider aracılığıyla desteklenen API'ler:**
- 🔗 **OpenRouter** - 100'den fazla modele erişim
- ⚡ **Groq** - Yıldırım hızında çıkarım  
- 🌐 **Together AI** - Açık kaynaklı modeller
- 🚀 **Perplexity** - Arama + AI
- 🇫🇷 **Mistral AI** - Avrupa'nın AI lideri
- 🔥 **Fireworks AI** - Ultra hızlı çıkarım
- 🦙 **Ollama** - Yerel modeller
- 🏠 **LM Studio** - Yerel AI oyun alanı
- 🛠️ **OpenAI uyumlu herhangi bir API**

## 🗄️ Depolama Çözümleri - Kurumsal Sınıf

### 🎯 **Vektör Veritabanları**
```json
{
  “Storage”: {
    “Qdrant”: {
      “Host”: “your-qdrant-host.com”,
      “ApiKey”: “your-api-key”,
      “CollectionName”: “documents”,
      “VectorSize”: 1536
    },
    “Redis”: {
      “ConnectionString”: “localhost:6379”,
      “KeyPrefix”: “smartrag:”,
      “Database”: 0
    }
  }
}
```

### 🏢 **Geleneksel Veritabanları**
```json
{
  “Storage”: {
    “Sqlite”: {
      “DatabasePath”: “smartrag.db”,
      “EnableForeignKeys”: true
    },
    “FileSystem”: {
      “FileSystemPath”: “Documents”
    }
  }
}
```

### ⚡ **Geliştirme**
```json
{
  “Storage”: {
    “InMemory”: {
      “MaxDocuments”: 1000
    }
  }
}
```

## 📄 Belge İşleme

### **Desteklenen Biçimler**
- **📄 PDF**: iText7 ile gelişmiş metin çıkarma
- **📝 Word**: OpenXML ile .docx ve .doc desteği
- **📋 Metin**: .txt, .md, .json, .xml, .csv, .html
- **🔤 Düz Metin**: BOM algılama ile UTF-8 kodlama

### **Akıllı Belge Ayrıştırma**
```csharp
// Otomatik format algılama ve ayrıştırma
var document = await documentService.UploadDocumentAsync(
    fileStream,
    “contract.pdf”,     // PDF'yi otomatik olarak algılar
    “application/pdf”,
    “legal-team”
);

// Bağlamın korunması için çakışmalı akıllı parçalama
var chunks = document.Chunks; // Akıllı sınırlarla otomatik olarak parçalanır
```

### **Gelişmiş Parçalama Seçenekleri**
```csharp
services.AddSmartRAG(configuration, options =>
{
    options.MaxChunkSize = 1000;      // Maksimum parçalama boyutu
    options.MinChunkSize = 100;       // Minimum parçalama boyutu  
    options.ChunkOverlap = 200;       // Parçalar arasında çakışma
    options.SemanticSearchThreshold = 0.3; // Benzerlik eşiği
});
```

## 💬 Konuşma Geçmişi

SmartRAG, bir oturumdaki birden fazla soru arasında bağlamı koruyan **otomatik konuşma geçmişi yönetimi** özelliğine sahiptir. Bu, AI sisteminizle daha doğal ve bağlamsal konuşmalar yapmanızı sağlar.

### **Temel Özellikler**
- **Oturum Tabanlı**: Her konuşma benzersiz bir oturum kimliğine bağlıdır
- **Otomatik Yönetim**: Manuel konuşma yönetimi gerekmez
- **Bağlam Farkındalığı**: Önceki sorular ve cevaplar mevcut yanıtları etkiler
- **Akıllı Kesme**: Token sınırlarını önlemek için konuşma uzunluğunu otomatik olarak yönetir
- **Depolama Entegrasyonu**: Kalıcılık için yapılandırılmış depolama sağlayıcınızı kullanır

### **Nasıl Çalışır**
```csharp
// Oturumdaki ilk soru
var response1 = await _documentSearchService.QueryIntelligenceAsync(
    “Şirketin iade politikası nedir?”,
    maxResults: 5
);

// Takip sorusu - AI önceki bağlamı hatırlar
var response2 = await _documentSearchService.QueryIntelligenceAsync(
    “Uluslararası siparişler ne olacak?”,  // AI bunun iade politikasıyla ilgili olduğunu bilir
    maxResults: 5
);
```

### **Konuşma Akışı Örneği**
```
Kullanıcı: “Şirketin iade politikası nedir?”
AI: “Politika belgesine göre, müşteriler 30 gün içinde geri ödeme talep edebilir...”

Kullanıcı: “Uluslararası siparişler ne olacak?”  // AI önceki bağlamı hatırlar
AI: “Uluslararası siparişler için, nakliye koşulları nedeniyle geri ödeme politikası 45 güne kadar uzar...”

Kullanıcı: “Geri ödemeyi nasıl başlatabilirim?”  // AI konuşmanın tüm bağlamını korur
AI: “Geri ödemeyi başlatmak için müşteri hizmetlerine başvurabilir veya çevrimiçi portalı kullanabilirsiniz...”
```

### **Oturum Yönetimi**
- **Benzersiz Oturum Kimlikleri**: Her kullanıcı/konuşma için benzersiz tanımlayıcılar oluşturun
- **Otomatik Temizleme**: Performansı korumak için eski konuşmalar otomatik olarak kesilir
- **Çapraz İstek Kalıcılığı**: Konuşma geçmişi birden fazla API çağrısında kalıcıdır
- **Gizlilik**: Her oturum izole edilir - kullanıcılar arasında çapraz bulaşma olmaz

## 🔧 Gelişmiş Yapılandırma

### **Tam Yapılandırma Örneği**
```json
{
  “AI”: {
    “OpenAI”: {
      “ApiKey”: “sk-...”,
      “Endpoint”: “https://api.openai.com/v1”,
      “Model”: “gpt-4”,
      “EmbeddingModel”: “text-embedding-ada-002”,
      “MaxTokens”: 4096,
      “Temperature”: 0.7
    },
    “Anthropic”: {
      “ApiKey”: “sk-ant-...”,
      “Model”: “claude-3.5-sonnet”,
      “MaxTokens”: 4096,
      “Temperature”: 0.3,
      “EmbeddingApiKey”: “voyage-...”,
      “EmbeddingModel”: “voyage-large-2”
    }
  },
  "Storage": {
    "Qdrant": {
      "Host": "localhost:6334",
      "UseHttps": false,
      "CollectionName": "smartrag_docs",
      "VectorSize": 1536,
      "DistanceMetric": "Cosine"
    },
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Password": "",
      "Database": 0,
      "KeyPrefix": "smartrag:",
      "ConnectionTimeout": 30,
      "EnableSsl": false
    }
  }
}
```

### **Çalışma Zamanı Sağlayıcı Değiştirme**
```csharp
services.AddSmartRAG(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Qdrant;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = [AIProvider.Anthropic, AIProvider.Gemini];
});
```

## 🏗️ Mimari

SmartRAG, net bir şekilde ayrılmış ilgi alanları ve kurumsal düzeyde tasarım modelleri ile temiz mimari ilkelerini takip eder.

### **🎯 Temel Mimariye Genel Bakış**

SmartRAG, her biri belirli sorumluluklara ve net arayüzlere sahip 5 farklı katmandan oluşan **katmanlı bir kurumsal mimari** olarak inşa edilmiştir:

| Hizmet Katmanı | Sorumluluk | Anahtar Arayüzler |
|---------------|---------------|----------------|
| **🧠 Zeka Hizmetleri** | Sorgu işleme, RAG boru hattı, konuşma zekası | `IDocumentSearchService`, `ISemanticSearchService` |
| **📄 Belge Hizmetleri** | Belge işleme, ayrıştırma ve yönetim | `IDocumentParserService`, `IDocumentService`, `IImageParserService`, `IAudioParserService` |
| **🤖 AI ve Sağlayıcı Hizmetleri** | AI sağlayıcı yönetimi, analitik, izleme | `IAIProvider`, `IAIProviderFactory`, `IAIService` |
| **🗄️ Veri ve Depolama Hizmetleri** | Veritabanı entegrasyonu, depolama yönetimi | `IDatabaseParserService`, `IStorageProvider`, `IStorageFactory`, `IDocumentRepository` |
| **⚙️ Altyapı Hizmetleri** | Yapılandırma, konuşma yönetimi, sistem hizmetleri | `IQdrantCacheManager`, `IQdrantCollectionManager`, `IQdrantEmbeddingService` |

### **🔄 Veri Akışı Mimarisi**

```
📱 İstemci İsteği
    ↓
🧠 IDocumentSearchService.QueryIntelligenceAsync()
    ↓
📊 Çok Modlu Arama (Belgeler + Veritabanları + Konuşmalar)
    ↓
🤖 AI Sağlayıcı Seçimi (OpenAI, Anthropic, Gemini, vb.)
    ↓
💾 Depolama Katmanı (Qdrant, Redis, SQLite, vb.)
    ↓
✨ Kaynaklarla Akıllı Yanıt
```

### **🎯 Temel Mimari Modeller**

#### **1. 🧠 Zeka Öncelikli Tasarım**
- **Sorgu Niyeti Algılama**: Sorguları otomatik olarak uygun işleyicilere yönlendirir
- **Çok Modlu İşleme**: Belgeleri, veritabanlarını ve konuşmaları sorunsuz bir şekilde işler
- **Bağlam Duyarlı Yanıtlar**: Konuşma geçmişini ve bağlamı korur

#### **2. 🏭 Sağlayıcı Deseni Uygulaması**
- **AI Sağlayıcıları**: Birleşik arayüze sahip 5'ten fazla sağlayıcı (OpenAI, Anthropic, Gemini, Azure, Özel)
- **Depolama Sağlayıcıları**: Birden fazla depolama seçeneği (Vektör Veritabanları, Geleneksel Veritabanları, Dosya Sistemi)
- **Veritabanı Sağlayıcıları**: Evrensel veritabanı desteği (SQLite, SQL Server, MySQL, PostgreSQL)

#### **3. 🔧 Hizmet Odaklı Mimari**
- **Gevşek Bağlantı**: Hizmetler, iyi tanımlanmış arayüzler aracılığıyla iletişim kurar
- **Bağımlılık Enjeksiyonu**: Test edilebilirlik için tam DI konteyner entegrasyonu
- **Yapılandırma Odaklı**: Mantıklı varsayılan ayarlarla ortam tabanlı yapılandırma

#### **4. 📊 Kurumsal Düzeyde Özellikler**
- **Analitik ve İzleme**: Kapsamlı kullanım izleme ve performans ölçümleri
- **Yapılandırma Yönetimi**: Çalışma zamanı yapılandırma güncellemeleri ve doğrulama
- **Depolama Yönetimi**: Yedekleme, geri yükleme, taşıma yetenekleri
- **Güvenlik**: Otomatik hassas veri temizleme ve koruma

### **Anahtar Bileşenler**

#### **🧠 Zeka Hizmetleri:**
- **`IDocumentSearchService`**: RAG ve konuşma zekası ile gelişmiş sorgu işleme
- **DocumentSearchService**: `QueryIntelligenceAsync` yöntemi ile temel RAG işlemleri
- **SemanticSearchService**: Hibrit puanlama ile gelişmiş semantik arama

#### **📄 Belge Hizmetleri:**
- **`IDocumentParserService`**: Çok formatlı belge ayrıştırma ve işleme
- **DocumentService**: Belge işlemleri için ana düzenleyici
- **DocumentParserService**: Çok formatlı ayrıştırma (PDF, Word, Excel, Görüntüler, Ses, Veritabanları)

#### **🤖 AI ve Sağlayıcı Hizmetleri:**
- **`IAIProvider`**: OpenAI, Anthropic, Gemini, Azure desteği ile evrensel AI sağlayıcı arayüzü
- **AnalyticsController**: Kullanım izleme, performans izleme ve içgörüler
- **AIService**: AI sağlayıcı etkileşimleri ve embedding işlemleri

#### **🗄️ Veri ve Depolama Hizmetleri:**
- **`IDatabaseParserService`**: Evrensel veritabanı entegrasyonu (SQLite, SQL Server, MySQL, PostgreSQL)
- **StorageController**: Depolama sağlayıcı yönetimi, yedekleme, geri yükleme, taşıma
- **DatabaseParserService**: Canlı veritabanı bağlantıları ve akıllı veri çıkarma

#### **⚙️ Altyapı Hizmetleri:**
- **`IQdrantCacheManager`**: Vektör veritabanı önbellek yönetimi ve optimizasyonu
- **ConfigurationController**: Çalışma zamanı yapılandırma güncellemeleri ve doğrulama
- **ConfigurationService**: Sistem yapılandırması ve durum izleme

#### **🏗️ Fabrika Hizmetleri:**
- **`IAIProviderFactory`**: Dinamik AI sağlayıcı örneklendirme ve yapılandırma
- **Depolar**: Depolama soyutlama katmanı (Redis, Qdrant, SQLite, FileSystem)
- **Uzantılar**: Bağımlılık enjeksiyonu yapılandırması

## 🎨 Kütüphane Kullanım Örnekleri

### **Hizmet Kaydı ve Yapılandırma**
```csharp
// Program.cs veya Startup.cs
services.AddSmartRAG(options => {
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Qdrant;
    options.OpenAI.ApiKey = “your-openai-api-key”;
    options.Qdrant.Endpoint = “http://localhost:6333”;
});

// Birden fazla sağlayıcı ve yedekleme ile
services.AddSmartRAG(options => {
    options.AIProvider = AIProvider.OpenAI;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = [AIProvider.Anthropic, AIProvider.Gemini];
});
```

### **Temel Hizmet Kullanımı**
```csharp
public class MyApplicationService
{
    private readonly IDocumentSearchService _documentSearchService;
    private readonly IDocumentParserService _documentParserService;
    private readonly IDatabaseParserService _databaseParserService;
    
    public MyApplicationService(
        IDocumentSearchService documentSearchService,
        IDocumentParserService documentParserService,
        IDatabaseParserService databaseParserService)
    {
        _documentSearchService = documentSearchService;
        _documentParserService = documentParserService;
        _databaseParserService = databaseParserService;
    }
    
    public async Task<string> QueryIntelligence(string query)
    {
        var result = await _documentSearchService.QueryIntelligenceAsync(query, maxResults: 5);
        return result.Answer;
    }
    
    public async Task<List<DocumentChunk>> ProcessDocument(IFormFile file)
    {
        var result = await _documentParserService.ParseDocumentAsync(file);
        return result.Chunks;
    }
}
```

### **Veritabanı Entegrasyonu Örnekleri**
```csharp
// Canlı SQL Server veritabanına bağlan
var sqlServerConfig = new DatabaseConfig
{
    ConnectionString = “Server=localhost;Database=Northwind;Trusted_Connection=true;”,
    DatabaseType = DatabaseType.SqlServer,
    IncludedTables = new List<string> { "Customers", "Orders", "Products" },
    MaxRows = 1000,
    SanitizeSensitiveData = true
};

var result = await _databaseParserService.ConnectToDatabaseAsync(sqlServerConfig);

// MySQL veritabanına bağlan
var mySqlConfig = new DatabaseConfig
{
    ConnectionString = “Server=localhost;Database=sakila;Uid=root;Pwd=password;”,
    DatabaseType = DatabaseType.MySQL,
    IncludedTables = new List<string> { "actor", "film", "customer" }
};

var mySqlResult = await _databaseParserService.ConnectToDatabaseAsync(mySqlConfig);

// SQLite veritabanı dosyasını ayrıştır
var sqliteResult = await _databaseParserService.ParseDatabaseFileAsync(fileStream, DatabaseType.SQLite);

// Özel SQL sorgusunu yürüt
var queryResult = await _databaseParserService.ExecuteQueryAsync(
    connectionString: “Server=localhost;Database=Northwind;Trusted_Connection=true;”,
    query: “SELECT TOP 10 CustomerID, CompanyName FROM Customers WHERE Country = ‘USA’”,
    databaseType: DatabaseType.SqlServer,
    maxRows: 10
);
```

### **İsteğe Bağlı API Örnekleri (Yalnızca Referans Amaçlı)**
```bash
# Bunlar isteğe bağlı API uç noktalarıdır - SmartRAG öncelikle bir kütüphanedir.
# API aracılığıyla belge yükleme (denetleyicileri uygulamayı seçerseniz)
curl -X POST “http://localhost:5000/api/documents/upload” \
  -F “file=@research-paper.pdf”

# API üzerinden sorgulama (denetleyicileri uygulamayı seçerseniz)  
curl -X POST “http://localhost:5000/api/intelligence/query” \
  -H “Content-Type: application/json” \
  -d ‘{“query”: “Ana avantajları nelerdir?”, “maxResults”: 5}’
```

### **Kütüphane Entegrasyon Örnekleri**

SmartRAG, hizmet katmanı aracılığıyla hem belge aramasını hem de genel sohbeti otomatik olarak gerçekleştirir:

```csharp
// Belgelerinizle ilgili sorular sorun (RAG modu)
var ragResult = await _documentSearchService.QueryIntelligenceAsync(
    “Finansal raporda belirtilen başlıca riskler nelerdir?”, 
    maxResults: 5
);

// Genel konuşma (Doğrudan AI sohbet modu)
var chatResult = await _documentSearchService.QueryIntelligenceAsync(
    “Bugün nasılsınız?”, 
    maxResults: 1
);
```

**Belge Arama Yanıtı Örneği:**
```json
{
  "query": "Finansal raporda belirtilen başlıca riskler nelerdir?",
  "answer": "Finansal belgelere göre, belirlenen ana riskler şunlardır: 1) Gelir tahminlerini etkileyen piyasa oynaklığı, 2) Avrupa pazarındaki düzenleyici değişiklikler, 3) Döviz kuru dalgalanmaları ve 4) Tedarik zinciri kesintileri. Rapor, piyasa oynaklığının çeyrek kazançları üzerinde %15-20'lik potansiyel etkiyle en yüksek riski oluşturduğunu vurgulamaktadır...",
  "sources": [
    {
      "documentId": "doc-456",
      "fileName": "Q3-financial-report.pdf", 
      "chunkContent": "Piyasa oynaklığı, çeyrek kazançları üzerinde %15-20'lik bir etki öngörülerek, başlıca endişe kaynağımız olmaya devam etmektedir...",
      "relevanceScore": 0.94
    }
  ],
  "searchedAt": "2025-08-16T14:57:06.2312433Z",
  "configuration": {
    "aiProvider": "Anthropic",
    "storageProvider": "Redis",
    "model": "Claude + VoyageAI"
  }
}
```

**Genel Sohbet Yanıtı Örneği:**
```json
{
  “query”: “Bugün nasılsınız?”,
  “answer”: “İyiyim, sorduğunuz için teşekkürler! Belgelerinizle ilgili sorularınız veya genel sohbetleriniz için size yardımcı olmak için buradayım. Bugün size nasıl yardımcı olabilirim?”,
  “sources”: [],
  “searchedAt”: “2025-08-16T14:57:06.2312433Z”,
  “configuration”: {
    “aiProvider”: “Anthropic”,
    “storageProvider”: “Redis”, 
    “model”: “Claude + VoyageAI”
  }
}
```


## 🧪 Testler ve Örnekler

SmartRAG, test ve öğrenme için kapsamlı örnek uygulamalar sunar:

### **Örnek Uygulamalar**
```
examples/
├── SmartRAG.API/              # Swagger ile tam özellikli REST API
├── SmartRAG.Console/          # Test için konsol uygulaması
└── SmartRAG.DatabaseTests/    # Docker desteği ile çoklu veritabanı RAG testi
```

### **SmartRAG.API** - REST API Örneği
- ✅ Tüm SmartRAG özelliklerine sahip eksiksiz REST API uygulaması
- ✅ Swagger/OpenAPI belgeleri
- ✅ Belge yükleme, arama ve zeka uç noktaları
- ✅ Veritabanı bağlantısı ve çoklu veritabanı sorgu uç noktaları
- ✅ Etkileşimli Swagger UI ile gerçek zamanlı test

### **SmartRAG.Console** - Konsol Uygulaması
- ✅ Basit konsol tabanlı test
- ✅ Belge işleme örnekleri
- ✅ AI sağlayıcı entegrasyon demoları
- ✅ Hızlı prototip oluşturma ve deneme

### **SmartRAG.DatabaseTests** - Çoklu Veritabanı Testi
- ✅ Çoklu veritabanı sorgu koordinasyon testi
- ✅ SQL Server, MySQL, PostgreSQL için Docker Compose kurulumu
- ✅ Desteklenen tüm veritabanları için test veritabanı oluşturucuları
- ✅ Gerçek dünya çoklu veritabanı senaryoları
- ✅ Test sorguları için dil seçimi

### **Çalışan Örnekler**
```bash
# Swagger ile REST API'yi çalıştırın
cd examples/SmartRAG.API
dotnet run
# https://localhost:7001/swagger adresine gidin

# Konsol uygulamasını çalıştırın
cd examples/SmartRAG.Console
dotnet run

# Veritabanı Testlerini Çalıştırın
cd examples/SmartRAG.DatabaseTests
dotnet run
```

## 🛠️ Geliştirme

### **Kaynak Koddan Derleme**
```bash
git clone https://github.com/byerlikaya/SmartRAG.git
cd SmartRAG
dotnet restore
dotnet build
```

### **Örnek Uygulamaları Çalıştırma**
```bash
# REST API örneğini çalıştır
cd examples/SmartRAG.API
dotnet run

# Konsol örneğini çalıştır
cd examples/SmartRAG.Console
dotnet run

# Veritabanı Testlerini Çalıştır
cd examples/SmartRAG.DatabaseTests
dotnet run
```

## 🤝 Katkı Sağlama

Katkılarınızı bekliyoruz!

### **Geliştirme Kurulumu**
1. Depoyu çatallayın
2. Bir özellik dalı oluşturun
3. Değişikliklerinizi yapın
4. Testler ekleyin
5. Çekme isteği gönderin

## 🆕 Yenilikler

### **Son Sürüm (v3.0.0) - 2025-10-18**

**Önemli Özellikler:**
- 🚀 **ÖNEMLİ DEĞİŞİKLİK**: `GenerateRagAnswerAsync` → `QueryIntelligenceAsync` (geriye dönük uyumlu)
- 🔧 **Dil Güvenli SQL Oluşturma**: SQL'de İngilizce olmayan metinleri önleyen otomatik doğrulama
- 🗄️ **PostgreSQL Tam Desteği**: Çoklu veritabanı sorguları ile tam entegrasyon
- 🔒 **Yerinde AI Desteği**: Ollama/LM Studio ile tam yerel çalışma
- ⚠️ **Önemli Sınırlamalar**: Ses için Google Cloud gerekir, OCR el yazısı için sınırlıdır
- 📚 **Geliştirilmiş Belgeler**: Kapsamlı şirket içi dağıtım kılavuzu

**📋 [Tam Değişiklik Günlüğünü Görüntüle](CHANGELOG.md)** ayrıntılı sürüm notları ve geçiş kılavuzu için.


## 📚 Kaynaklar

### **📖 Kütüphane Belgeleri**
- **📚 [SmartRAG Belgeleri](https://byerlikaya.github.io/SmartRAG)** - Kapsamlı hizmet katmanı API referansı ve entegrasyon kılavuzları
- **🔧 [Hizmet Katmanı API Referansı](https://byerlikaya.github.io/SmartRAG/api-reference)** - Ayrıntılı arayüz belgeleri
- **🚀 [Başlangıç Kılavuzu](https://byerlikaya.github.io/SmartRAG/getting-started)** - Adım adım kütüphane entegrasyonu
- **📝 [Kullanım Örnekleri](https://byerlikaya.github.io/SmartRAG/examples)** - Gerçek dünya uygulama senaryoları

### **📦 Paket ve Dağıtım**
- **📦 [NuGet Paketi](https://www.nuget.org/packages/SmartRAG)** - Paket Yöneticisi veya .NET CLI aracılığıyla yükleyin
- **🐙 [GitHub Deposu](https://github.com/byerlikaya/SmartRAG)** - Kaynak kodu, sorunlar ve katkılar
- **📊 [Paket İstatistikleri](https://www.nuget.org/profiles/barisyerlikaya)** - İndirme istatistikleri ve sürüm geçmişi

### **💼 Profesyonel Destek**
- **📧 [İletişim ve Destek](mailto:b.yerlikaya@outlook.com)** - Teknik destek ve danışmanlık
- **💼 [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/)** - Profesyonel ağ ve güncellemeler
- **🌐 [Proje Web Sitesi](https://byerlikaya.github.io/SmartRAG/en/)** - Resmi proje ana sayfası

### **🔧 Üçüncü Taraf Kütüphaneler ve Teknolojiler**

SmartRAG, aşağıdaki mükemmel açık kaynak kütüphaneler ve bulut hizmetleri ile oluşturulmuştur:

#### **Belge İşleme**
- **📄 [iText7](https://github.com/itext/itext7-dotnet)** - PDF işleme ve metin çıkarma
- **📊 [EPPlus](https://github.com/EPPlusSoftware/EPPlus)** - Excel dosyası ayrıştırma ve işleme
- **📝 [Open XML SDK](https://github.com/dotnet/Open-XML-SDK)** - Word belge işleme

#### **OCR ve Görüntü İşleme**
- **🔍 [Tesseract OCR](https://github.com/tesseract-ocr/tesseract)** - Kurumsal düzeyde OCR motoru (v5.2.0)
- **🎨 [SkiaSharp](https://github.com/mono/SkiaSharp)** - Görüntü ön işleme için çapraz platform 2D grafik kütüphanesi

#### **Konuşmayı Metne Dönüştürme**
- **🎤 [Whisper.net](https://github.com/sandrohanea/whisper.net)** - Yerel konuşmayı metne dönüştürme (.NET bağlamaları için OpenAI Whisper)
- **☁️ [Google Cloud Speech-to-Text](https://cloud.google.com/speech-to-text)** - Kurumsal konuşma tanıma API'si (isteğe bağlı)

#### **Vektör Veritabanları ve Depolama**
- **🗄️ [Qdrant](https://github.com/qdrant/qdrant)** - Vektör benzerlik arama motoru
- **⚡ [Redis](https://redis.io/)** - Bellek içi veri yapısı deposu
- **💾 [SQLite](https://www.sqlite.org/)** - Gömülü ilişkisel veritabanı

#### **Veritabanı Bağlantısı**
- **🗄️ [Npgsql](https://github.com/npgsql/npgsql)** - PostgreSQL .NET sürücüsü
- **🗄️ [MySqlConnector](https://github.com/mysql-net/MySqlConnector)** - MySQL .NET sürücüsü
- **🗄️ [Microsoft.Data.SqlClient](https://github.com/dotnet/SqlClient)** - SQL Server .NET sürücüsü

#### **AI Sağlayıcıları**
- **🤖 [OpenAI API](https://platform.openai.com/)** - GPT modelleri ve embedding
- **🧠 [Anthropic Claude](https://www.anthropic.com/)** - Claude modelleri
- **🌟 [Google Gemini](https://ai.google.dev/)** - Gemini AI modelleri
- **☁️ [Azure OpenAI](https://azure.microsoft.com/en-us/products/ai-services/openai-service)** - Kurumsal OpenAI hizmeti
- **🚀 [VoyageAI](https://www.voyageai.com/)** - Anthropic için yüksek kaliteli embedding

#### **Yerel AI Desteği**
- **🦙 [Ollama](https://ollama.ai/)** - AI modellerini yerel olarak çalıştırın
- **🏠 [LM Studio](https://lmstudio.ai/)** - Yerel AI modeli oyun alanı

## 📄 Lisans

Bu proje MIT Lisansı altında lisanslanmıştır - ayrıntılar için [LİSANS](LICENSE) dosyasına bakın.



**Barış Yerlikaya tarafından ❤️ ile oluşturulmuştur**

Türkiye'de üretilmiştir 🇹🇷 | [İletişim](mailto:b.yerlikaya@outlook.com) | [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/)