# Güvenlik ve Yapılandırma Rehberi

## ⚠️ ÖNEMLİ: Yapılandırma Dosyaları Güvenliği

### Yapılandırma Dosyaları Yapısı

Bu proje **iki ayrı yapılandırma dosyası** kullanır:

1. **`appsettings.json`** (✅ Git'e commit edildi)
   - **Hassas bilgi içermez**
   - Placeholder değerler kullanır
   - Repository'ye commit etmek güvenlidir
   - Diğer geliştiriciler için şablon görevi görür

2. **`appsettings.Development.json`** (🔒 Git'te ignore edildi)
   - **GERÇEK API anahtarları ve kimlik bilgilerini içerir**
   - Development ortamında `appsettings.json`'ı override eder
   - **ASLA Git'e commit edilmez** (`.gitignore` tarafından korunur)
   - Her geliştirici kendi kopyasını tutmalıdır

### Nasıl Çalışır

.NET yapılandırma sistemi otomatik olarak:
1. `appsettings.json`'ı yükler (temel yapılandırma)
2. `appsettings.Development.json`'ı yükler (Development ortamında temeli override eder)
3. Bunları birleştirir, Development ayarları önceliklidir

### Kurulum Talimatları

1. **İlk Kurulum:**
   ```bash
   # appsettings.Development.json zaten projede
   # Sadece gerçek kimlik bilgilerinizle güncelleyin
   ```

2. **Gerekli Kimlik Bilgileri:**

   **AI Özellikleri İçin:**
   - Anthropic API Anahtarı: https://console.anthropic.com/
   - VoyageAI Embedding API Anahtarı: https://console.voyageai.com/

   **Docker Veritabanları İçin (otomatik yapılandırıldı):**
   - SQL Server: `sa` / `${SQLSERVER_SA_PASSWORD}` (ortam değişkeni gerekli)
   - MySQL: `root` / `${MYSQL_ROOT_PASSWORD}` (ortam değişkeni gerekli)
   - PostgreSQL: `postgres` / `${POSTGRES_PASSWORD}` (ortam değişkeni gerekli)

3. **appsettings.Development.json'ı Güncelleyin:**
   ```json
   {
     "AI": {
       "Anthropic": {
         "ApiKey": "sk-ant-GERÇEK-ANAHTARINIZ-BURADA",
         "EmbeddingApiKey": "pa-GERÇEK-VOYAGE-ANAHTARINIZ-BURADA"
       }
     }
   }
   ```

### Ne Korunuyor?

`.gitignore` dosyası şunları korur:
- ✅ `appsettings.Development.json`
- ✅ `appsettings.Production.json`
- ✅ `appsettings.Local.json`
- ✅ Tüm `.secrets.json` dosyaları
- ✅ `.env` dosyaları

### Commit Etmek Güvenli Olan Şeyler?

- ✅ `appsettings.json` - Sadece placeholder değerler kullanır
- ✅ `docker-compose.yml` - Test ortamı kimlik bilgileri (production değil)
- ✅ Test veritabanı oluşturucular - Kodda kimlik bilgisi yok

## 🔐 Güvenlik En İyi Uygulamaları

### YAPILACAKLAR:
- ✅ Gerçek API anahtarlarını `appsettings.Development.json`'da tutun
- ✅ `appsettings.json`'da placeholder değerler kullanın
- ✅ Yeni hassas config dosyalarını `.gitignore`'a ekleyin
- ✅ Production'da ortam değişkenlerini kullanın
- ✅ API anahtarlarını düzenli olarak rotate edin

### YAPILMAYACAKLAR:
- ❌ `appsettings.Development.json`'ı commit etmeyin
- ❌ `appsettings.json`'a gerçek API anahtarları koymayın
- ❌ API anahtarlarınızı chat/e-posta'da paylaşmayın
- ❌ Production'da development kimlik bilgilerini kullanmayın
- ❌ `.env` dosyalarını commit etmeyin

## 🚨 Eğer Yanlışlıkla Secret'ları Commit Ettiyseniz

Eğer yanlışlıkla secret içeren bir dosyayı commit ettiyseniz:

1. **Hemen açığa çıkan kimlik bilgilerini iptal edin:**
   - Anthropic: https://console.anthropic.com/ → API Keys → Revoke
   - VoyageAI: https://console.voyageai.com/ → Delete key

2. **Git geçmişinden kaldırın:**
   ```bash
   # Git'ten dosyayı kaldırın ancak yerel kopyayı tutun
   git rm --cached appsettings.Development.json
   
   # Kaldırmayı commit edin
   git commit -m "Remove sensitive configuration file"
   
   # Değişiklikleri push edin
   git push
   ```

3. **Yeni kimlik bilgileri oluşturun** ve yerel `appsettings.Development.json`'ı güncelleyin

## 📋 Kimlik Bilgisi Kontrol Listesi

Commit etmeden önce doğrulayın:
- [ ] Commit edilen dosyalarda API anahtarı yok
- [ ] Commit edilen dosyalarda veritabanı şifresi yok
- [ ] `appsettings.json` sadece placeholder değerler içeriyor
- [ ] `appsettings.Development.json` `.gitignore`'da
- [ ] `.env` dosyası commit edilmemiş

## 🐳 Docker Kimlik Bilgileri

Docker kimlik bilgileri **kasıtlı olarak basittir** çünkü:
- Sadece **yerel test** için
- Container'lar `localhost`'ta çalışır
- Veriler Docker volume'lerinde izole edilir
- **Production'da bu kimlik bilgilerini asla kullanmayın!**

### Docker için Ortam Değişkenleri

Daha iyi güvenlik için ortam değişkenlerini kullanın:

**Seçenek 1: .env dosyası kullanma (Önerilen)**
```bash
# Örnek dosyayı kopyalayın
cp env.example .env

# .env dosyasını güvenli şifrelerinizle düzenleyin
nano .env

# Docker'ı başlatın (otomatik olarak .env'i okur)
docker-compose up -d
```

**Seçenek 2: export komutlarını kullanma**
```bash
# Docker'ı başlatmadan önce güvenli şifreleri ayarlayın
export SQLSERVER_SA_PASSWORD="GüvenliŞifreniz123!"
export MYSQL_ROOT_PASSWORD="MySQLŞifreniz456!"
export POSTGRES_PASSWORD="PostgresŞifreniz789!"

# Özel şifrelerle Docker'ı başlatın
docker-compose up -d
```

Bu yaklaşım:
- ✅ Kodda hardcoded şifreleri önler
- ✅ Ortam başına farklı şifrelere izin verir
- ✅ Güvenlik en iyi uygulamalarını takip eder
- ✅ Yanlışlıkla kimlik bilgisi açığa çıkmasını önler

## 📚 Ek Kaynaklar

- [ASP.NET Core Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [.NET'te User Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Ortam Değişkenleri](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/#environment-variables)

## İletişim

Güvenlik endişeleri için:
- **E-posta:** b.yerlikaya@outlook.com
- **GitHub:** https://github.com/byerlikaya/SmartRAG
