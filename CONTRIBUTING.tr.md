# 🤝 SmartRAG'a Katkıda Bulunma

SmartRAG'a katkıda bulunmaya ilgi gösterdiğiniz için teşekkür ederiz! Topluluktan gelen katkıları memnuniyetle karşılıyoruz ve projeyi iyileştirmek için gösterdiğiniz çabayı takdir ediyoruz.

## 📋 İçindekiler

- [Başlangıç](#başlangıç)
- [Geliştirme Kurulumu](#geliştirme-kurulumu)
- [Katkıda Bulunma Süreci](#katkıda-bulunma-süreci)
- [Kod Kuralları](#kod-kuralları)
- [Test](#test)
- [Değişiklikleri Gönderme](#değişiklikleri-gönderme)
- [Topluluk Kuralları](#topluluk-kuralları)

## 🚀 Başlangıç

### Önkoşullar

- **.NET SDK** (örnekler ve kütüphane için 6.0 veya üzeri)
- **Git**
- **Visual Studio 2022**, **VS Code** veya **JetBrains Rider**
- **C#** ve **RAG (Retrieval-Augmented Generation)** temel bilgisi

**Not**: SmartRAG kütüphanesi (`src/SmartRAG/`) geniş uyumluluk için **.NET 6** hedefler. Örnek projeler daha yeni .NET sürümlerini hedefleyebilir.

### Geliştirme Kurulumu

1. **Repository'yi fork edin**
   ```bash
   # GitHub'da fork edin, ardından fork'unuzu clone edin
   git clone https://github.com/KULLANICI-ADINIZ/SmartRAG.git
   cd SmartRAG
   ```

2. **Geliştirme ortamını kurun**
   ```bash
   # Bağımlılıkları geri yükleyin
   dotnet restore
   
   # Solution'ı derleyin
   dotnet build
   ```

3. **Geliştirme araçlarınızı yapılandırın**
   - IDE'niz için ilgili eklentileri yükleyin
   - `.editorconfig` dosyasına göre kod formatlamayı ayarlayın

## 🔄 Katkıda Bulunma Süreci

### 1. **Bir Issue Seçin veya Oluşturun**
- [Mevcut issue'ları](https://github.com/byerlikaya/SmartRAG/issues) gözden geçirin
- Hatalar için: Bug report şablonunu kullanın
- Özellikler için: Feature request şablonunu kullanın
- Issue'ya yorum yaparak üzerinde çalıştığınızı belirtin

### 2. **Bir Branch Oluşturun**
```bash
# Main'den yeni bir branch oluşturun
git checkout main
git pull origin main
git checkout -b feature/ozellik-adiniz
```

Branch isimlendirme kuralları:
- `feature/açıklama` - yeni özellikler için
- `bugfix/açıklama` - hata düzeltmeleri için
- `docs/açıklama` - dokümantasyon değişiklikleri için
- `refactor/açıklama` - kod refactoring için

### 3. **Değişikliklerinizi Yapın**
- Temiz, bakımı kolay kod yazın
- Mevcut kod stilini takip edin
- Uygun yorumlar ve dokümantasyon ekleyin

### 4. **Değişikliklerinizi Commit Edin**
```bash
# Değişikliklerinizi stage edin
git add .

# Açıklayıcı bir mesaj ile commit edin (MUTLAKA İngilizce)
git commit -m "feat: add support for new AI provider"
```

**Commit Mesaj Formatı:**
- **MUTLAKA İngilizce olmalı** - Asla Türkçe veya başka diller kullanmayın
- Format: `<type>[optional scope]: <description>`
- Tipler:
  - `feat:` - Yeni özellikler
  - `fix:` - Hata düzeltmeleri
  - `docs:` - Dokümantasyon değişiklikleri
  - `style:` - Kod stil değişiklikleri
  - `refactor:` - Kod refactoring
  - `perf:` - Performans iyileştirmeleri
  - `test:` - Test eklemeleri veya değişiklikleri
  - `build:` - Build sistemi değişiklikleri
  - `ci:` - CI/CD yapılandırması
  - `chore:` - Bakım görevleri
  - `revert:` - Önceki bir commit'i geri al

**Kategorilendirme Kuralı (Opsiyonel):**
Birden fazla kategoride değişikliğiniz varsa, daha iyi organizasyon için bunları ayrı ayrı commit edebilirsiniz:
```bash
# 1. Dokümantasyon
git add docs/en/*.md docs/tr/*.md
git commit -m "docs: update API documentation"

# 2. Kod değişiklikleri
git add src/SmartRAG/**/*.cs
git commit -m "feat: add new feature"
```

**⚠️ Release Tagging:**
- **ASLA** açıkça söylenmedikçe `[release]` etiketi eklemeyin
- `[release]` etiketi otomatik NuGet paket yayınlamayı tetikler
- Format: `[release] feat: add feature v3.2.0`

## 📝 Kod Kuralları

### **C# Kodlama Standartları**

1. **İsimlendirme Kuralları**
   - Sınıflar, metodlar, özellikler için `PascalCase` kullanın
   - Yerel değişkenler ve parametreler için `camelCase` kullanın
   - Sabitler için `UPPER_CASE` kullanın
   - Interface'lere `I` öneki ekleyin (örn. `IAIProvider`)

2. **Constructor'lar**
   - Primary constructor (C# 12+) ve standart constructor'ların ikisi de kullanılabilir. Kod tabanında tutarlı kullanın.

3. **Logging**
   - **MUTLAKA** logging için `ILogger<T>` kullanın
   - **ASLA** `Console.WriteLine` kullanmayın
   - Mesaj şablonları ile structured logging kullanın

4. **Dokümantasyon**
   - Tüm public member'lar için XML dokümantasyonu kullanın
   - Açık olanı tekrar eden gereksiz yorumlardan kaçının

5. **Hata Yönetimi**
   - Özel exception tipleri kullanın
   - Anlamlı hata mesajları sağlayın
   - `ILogger<T>` ile hataları uygun şekilde loglayın

6. **Dil Gereksinimleri**
   - **TÜM kod İngilizce olmalı** (değişken isimleri, yorumlar, dokümantasyon)
   - Kodda Türkçe veya başka dil kelimeleri olmamalı

7. **Build Gereksinimleri**
   - **MUTLAKA** 0 hata, 0 uyarı, 0 mesaj ile derlenmeli
   - Her zaman `dotnet build` öncesi `dotnet clean` çalıştırın
   - Commit etmeden önce tüm uyarıları düzeltin

**Detaylı kodlama standartları için**, bkz. [Kod Standartları](.cursor/rules/03-KOD-STANDARTLARI.mdc)

### **Mimari Desenler**

- **SOLID prensipleri** ve **DRY prensibi**ne uyun
- **Dependency Injection**'ı tutarlı şekilde kullanın
- Uygun **separation of concerns** implementasyonu yapın

### **Generic Kod Gereksinimleri**

**KRİTİK**: SmartRAG generic bir kütüphanedir - asla domain-specific kod yazmayın:
- ❌ Hardcoded tablo isimleri yok (örn. "Products", "Orders", "Customers")
- ❌ Hardcoded veritabanı isimleri yok (örn. "ProductCatalog", "SalesManagement")
- ❌ Domain-specific senaryolar yok (örn. e-ticaret, envanter yönetimi)
- ✅ Generic placeholder'lar kullanın: "TableA", "ColumnX", "Database1"
- ✅ Herhangi bir veritabanı yapısı için çalışan şema tabanlı logic kullanın
- ✅ Provider-agnostic kod için interface'leri kullanın

## 🧪 Örnek Projeler Doğrulama

Kütüphane için zorunlu unit test'ler olmasa da, değişiklikleri göndermeden önce **örnek projeler doğrulanmalıdır**.

### **SmartRAG.Demo Doğrulama**

Demo projesini etkileyen değişiklikleri göndermeden önce:

1. **Build Doğrulama**
   ```bash
   cd examples/SmartRAG.Demo
   dotnet clean
   dotnet build
   ```
   - 0 hata, 0 uyarı ile derlenmeli

2. **Runtime Doğrulama**
   - Demo uygulamasını çalıştırın
   - Başlatma menüsünün çalıştığını doğrulayın
   - En az bir sorgu senaryosu test edin
   - Test sorgu oluşturmanın çalıştığını doğrulayın (değiştirildiyse)

3. **Yapılandırma Doğrulama**
   - `appsettings.json` ve `appsettings.Development.json` dosyalarını kontrol edin
   - Tüm gerekli ayarların mevcut olduğunu doğrulayın
   - Farklı storage provider'ları ile test edin (Qdrant, Redis, InMemory)

### **SmartRAG.API Doğrulama**

API projesini etkileyen değişiklikleri göndermeden önce:

1. **Build Doğrulama**
   ```bash
   cd examples/SmartRAG.API
   dotnet clean
   dotnet build
   ```
   - 0 hata, 0 uyarı ile derlenmeli

2. **API Doğrulama**
   - API uygulamasını başlatın
   - Swagger UI'nin doğru yüklendiğini doğrulayın (`/swagger`)
   - En az bir endpoint'i manuel olarak test edin
   - Dosya yükleme işlevselliğini doğrulayın (değiştirildiyse)
   - CORS yapılandırmasının çalıştığını kontrol edin

3. **Yapılandırma Doğrulama**
   - `appsettings.json` ve `appsettings.Development.json` dosyalarını kontrol edin
   - SmartRAG servislerinin düzgün yapılandırıldığını doğrulayın
   - Farklı AI ve storage provider'ları ile test edin

### **Ne Doğrulanmalı**

- ✅ Uygulama hata olmadan başlar
- ✅ Yapılandırma dosyaları geçerlidir
- ✅ Ana özellikler beklenen şekilde çalışır
- ✅ Konsol/log'larda runtime exception yok
- ✅ API endpoint'leri doğru yanıt verir (API projesi için)

## 📤 Değişiklikleri Gönderme

### **Pull Request Süreci**

1. **Branch'inizin güncel olduğundan emin olun**
   ```bash
   git checkout main
   git pull origin main
   git checkout branch-adiniz
   git rebase main
   ```

2. **Değişikliklerinizi push edin**
   ```bash
   git push origin branch-adiniz
   ```

3. **Bir Pull Request oluşturun**
   - PR şablonunu kullanın
   - Net bir açıklama sağlayın
   - İlgili issue'ları bağlayın
   - Maintainer'lardan review isteyin

4. **Review Geri Bildirimlerini Ele Alın**
   - İstenen değişiklikleri yapın
   - Aynı branch'e güncellemeleri push edin
   - Review yorumlarına yanıt verin

### **PR Gereksinimleri**

- [ ] Build başarılı (0 hata, 0 uyarı, 0 mesaj)
- [ ] Kod stil kurallarına uyuyor
- [ ] Kod generic ve provider-agnostic (domain-specific kod yok)
- [ ] Tüm kod İngilizce (Türkçe veya başka diller yok)
- [ ] Dokümantasyon güncellendi (uygulanabilirse hem EN hem TR)
- [ ] Breaking change yok (tartışılmadıkça)
- [ ] Commit mesajları İngilizce
- [ ] `[release]` etiketi yok (açıkça onaylanmadıkça)
- [ ] Örnek projeler doğrulandı (değişiklikler onları etkiliyorsa)

## 🌟 Katkı Türleri

### **Kod Katkıları**
- Yeni AI provider implementasyonları
- Storage backend entegrasyonları
- Performans iyileştirmeleri
- Hata düzeltmeleri

**Önemli Notlar:**
- Tüm kod değişiklikleri `src/SmartRAG/` dizininde olmalıdır
- `examples/` projelerindeki değişiklikler NuGet paketine dahil edilmez
- Changelog girdileri yalnızca `src/SmartRAG/` değişikliklerine referans vermelidir

### **Dokümantasyon**
- API dokümantasyonu
- Kullanım örnekleri
- Öğreticiler ve rehberler
- README iyileştirmeleri

### **Test** (Opsiyonel)
- Örnek projeler için test iyileştirmeleri
- Performans benchmark'ları

### **Topluluk**
- Issue'lardaki soruları yanıtlama
- Diğer katkıda bulunanlara yardım etme
- Bug bildirme
- İyileştirme önerileri sunma

## 📞 Yardım Alma

### **İletişim Kanalları**
- **GitHub Issues**: Bug'lar ve feature request'ler için
- **GitHub Discussions**: Sorular ve genel tartışmalar için
- **E-posta**: [b.yerlikaya@outlook.com](mailto:b.yerlikaya@outlook.com)

### **Kaynaklar**
- [Proje README](README.tr.md)
- [Dokümantasyon Sitesi](https://byerlikaya.github.io/SmartRAG/tr/)
- [Proje Kuralları](.cursor/rules/00-ANA-INDEKS.mdc) - Tam proje kuralları ve rehberleri
- [Kod Standartları](.cursor/rules/03-KOD-STANDARTLARI.mdc) - Detaylı C# kodlama standartları
- [Git Commit Kuralları](.cursor/rules/02-GIT-COMMIT-RULES.mdc) - Commit mesaj rehberleri

## 🏷️ Topluluk Kuralları

### **Davranış Kuralları**
- Saygılı ve kapsayıcı olun
- Yapıcı geri bildirime odaklanın
- Hoş karşılama ortamı oluşturmaya yardımcı olun
- GitHub'ın topluluk kurallarını takip edin

### **Kalite Standartları**
- Temiz, okunabilir kod yazın
- Dokümantasyonu güncelleyin
- Geriye dönük uyumluluğu düşünün

## 🎉 Tanınma

Katkıda bulunanlar şu şekillerde tanınır:
- GitHub katkıda bulunanlar listesi
- Önemli katkılar için release notları
- Dokümantasyonda özel anılar

SmartRAG'a katkıda bulunduğunuz için teşekkür ederiz! Çabalarınız bu projeyi herkes için daha iyi hale getirmeye yardımcı oluyor. 🚀

---

**Sorularınız mı var?** Açıklama için [Barış Yerlikaya](mailto:b.yerlikaya@outlook.com)'ya ulaşmaktan veya bir issue açmaktan çekinmeyin.
