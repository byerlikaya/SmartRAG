---
layout: default
title: SmartRAG Dokümantasyonu
description: .NET uygulamaları için kurumsal düzeyde RAG kütüphanesi
lang: tr
---

<div class="hero-section text-center py-5 mb-5">
    <div class="hero-content">
        <h1 class="hero-title display-4 fw-bold mb-4">
            <i class="fas fa-brain me-3"></i>
            SmartRAG
        </h1>
        <p class="hero-subtitle lead mb-4">
            .NET uygulamaları için kurumsal düzeyde RAG kütüphanesi
        </p>
        <p class="hero-description mb-5">
            Gelişmiş belge işleme, AI destekli embedding üretimi ve anlamsal arama yetenekleri ile akıllı uygulamalar geliştirin.
        </p>
        <div class="hero-buttons">
            <a href="{{ site.baseurl }}/tr/getting-started" class="btn btn-primary btn-lg me-3">
                <i class="fas fa-rocket me-2"></i>Başlayın
            </a>
            <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-primary btn-lg me-3" target="_blank" rel="noopener noreferrer">
                <i class="fab fa-github me-2"></i>GitHub'da Görüntüle
            </a>
            <a href="https://www.nuget.org/packages/SmartRAG" class="btn btn-outline-success btn-lg" target="_blank" rel="noopener noreferrer">
                <i class="fas fa-box me-2"></i>NuGet Paketi
            </a>
        </div>
    </div>
</div>

## 🚀 SmartRAG Nedir?

SmartRAG, akıllı belge işleme, embedding üretimi ve anlamsal arama yetenekleri sağlayan kapsamlı bir .NET kütüphanesidir. AI destekli uygulamalar geliştirmek için güçlü özellikler sunarken kullanımı kolay olacak şekilde tasarlanmıştır.

<div class="row mt-5 mb-5">
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-file-alt fa-3x text-primary"></i>
                </div>
                <h5 class="card-title">Çoklu Format Desteği</h5>
                <p class="card-text">Word, PDF, Excel ve metin belgelerini kolayca işleyin. Kütüphanemiz tüm ana belge formatlarını otomatik olarak ele alır.</p>
            </div>
        </div>
    </div>
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-robot fa-3x text-success"></i>
                </div>
                <h5 class="card-title">AI Sağlayıcı Entegrasyonu</h5>
                <p class="card-text">OpenAI, Anthropic, Azure OpenAI, Gemini ve özel AI sağlayıcıları ile güçlü embedding üretimi için sorunsuz entegrasyon.</p>
            </div>
        </div>
    </div>
</div>

<div class="row mb-5">
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-database fa-3x text-warning"></i>
                </div>
                <h5 class="card-title">Vektör Depolama</h5>
                <p class="card-text">Qdrant, Redis, SQLite, Bellek İçi, Dosya Sistemi ve özel depolama dahil çoklu depolama backend'leri ile esnek dağıtım.</p>
            </div>
        </div>
    </div>
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-search fa-3x text-info"></i>
                </div>
                <h5 class="card-title">Anlamsal Arama</h5>
                <p class="card-text">Daha iyi kullanıcı deneyimi için benzerlik puanlaması ve akıllı sonuç sıralaması ile gelişmiş arama yetenekleri.</p>
            </div>
        </div>
    </div>
</div>

## ⚡ Hızlı Başlangıç

Basit kurulum süreci ile dakikalar içinde çalışmaya başlayın:

```csharp
// Projenize SmartRAG ekleyin
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});

// Belge servisini kullanın
var documentService = serviceProvider.GetRequiredService<IDocumentService>();
var document = await documentService.UploadDocumentAsync(file);
```

## 🚀 Desteklenen Teknolojiler

SmartRAG, size en iyi deneyimi sunmak için önde gelen AI sağlayıcıları ve depolama çözümleri ile entegre olur.

### 🤖 AI Sağlayıcıları

<div class="row mt-4 mb-5">
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fab fa-google fa-3x text-warning"></i>
            </div>
            <h6 class="mb-1">Gemini</h6>
            <small class="text-muted">Google AI</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fab fa-openai fa-3x text-primary"></i>
            </div>
            <h6 class="mb-1">OpenAI</h6>
            <small class="text-muted">GPT Modelleri</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-cloud fa-3x text-secondary"></i>
            </div>
            <h6 class="mb-1">Azure OpenAI</h6>
            <small class="text-muted">Kurumsal</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-robot fa-3x text-success"></i>
            </div>
            <h6 class="mb-1">Anthropic</h6>
            <small class="text-muted">Claude Modelleri</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-cogs fa-3x text-dark"></i>
            </div>
            <h6 class="mb-1">Özel</h6>
            <small class="text-muted">Genişletilebilir</small>
        </div>
    </div>
</div>

### 🗄️ Depolama Sağlayıcıları

<div class="row mt-4 mb-5">
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-cube fa-3x text-primary"></i>
            </div>
            <h6 class="mb-1">Qdrant</h6>
            <small class="text-muted">Vektör Veritabanı</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fab fa-redis fa-3x text-success"></i>
            </div>
            <h6 class="mb-1">Redis</h6>
            <small class="text-muted">Bellek İçi Önbellek</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-hdd fa-3x text-info"></i>
            </div>
            <h6 class="mb-1">SQLite</h6>
            <small class="text-muted">Yerel Veritabanı</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-microchip fa-3x text-warning"></i>
            </div>
            <h6 class="mb-1">Bellek İçi</h6>
            <small class="text-muted">Hızlı Geliştirme</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-folder-open fa-3x text-secondary"></i>
            </div>
            <h6 class="mb-1">Dosya Sistemi</h6>
            <small class="text-muted">Yerel Depolama</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-cogs fa-3x text-dark"></i>
            </div>
            <h6 class="mb-1">Özel</h6>
            <small class="text-muted">Genişletilebilir Depolama</small>
        </div>
    </div>
</div>

## 📚 Dokümantasyon

<div class="row mt-4">
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-rocket fa-2x text-primary mb-3"></i>
                <h5 class="card-title">Başlangıç</h5>
                <p class="card-text">Sizi çalışır duruma getirmek için hızlı kurulum ve kurulum rehberi.</p>
                <a href="{{ site.baseurl }}/tr/getting-started" class="btn btn-primary">Başlayın</a>
            </div>
        </div>
    </div>
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-cog fa-2x text-success mb-3"></i>
                <h5 class="card-title">Yapılandırma</h5>
                <p class="card-text">Detaylı yapılandırma seçenekleri ve en iyi uygulamalar.</p>
                <a href="{{ site.baseurl }}/tr/configuration" class="btn btn-success">Yapılandır</a>
            </div>
        </div>
    </div>
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-code fa-2x text-warning mb-3"></i>
                <h5 class="card-title">API Referansı</h5>
                <p class="card-text">Örnekler ve kullanım desenleri ile tam API dokümantasyonu.</p>
                <a href="{{ site.baseurl }}/tr/api-reference" class="btn btn-warning">API'yi Görüntüle</a>
            </div>
        </div>
    </div>
</div>

<div class="row mt-4">
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-lightbulb fa-2x text-info mb-3"></i>
                <h5 class="card-title">Örnekler</h5>
                <p class="card-text">Öğrenmek için gerçek dünya örnekleri ve örnek uygulamalar.</p>
                <a href="{{ site.baseurl }}/tr/examples" class="btn btn-info">Örnekleri Görüntüle</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-tools fa-2x text-danger mb-3"></i>
                <h5 class="card-title">Sorun Giderme</h5>
                <p class="card-text">Sorunları çözmenize yardımcı olacak yaygın sorunlar ve çözümler.</p>
                <a href="{{ site.baseurl }}/tr/troubleshooting" class="btn btn-danger">Yardım Al</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-history fa-2x text-secondary mb-3"></i>
                <h5 class="card-title">Değişiklik Günlüğü</h5>
                <p class="card-text">Sürümler arasında yeni özellikleri, iyileştirmeleri ve hata düzeltmelerini takip edin.</p>
                <a href="{{ site.baseurl }}/tr/changelog" class="btn btn-secondary">Değişiklikleri Görüntüle</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-hands-helping fa-2x text-dark mb-3"></i>
                <h5 class="card-title">Katkıda Bulunma</h5>
                <p class="card-text">SmartRAG geliştirmesine nasıl katkıda bulunacağınızı öğrenin.</p>
                <a href="{{ site.baseurl }}/tr/contributing" class="btn btn-dark">Katkıda Bulun</a>
            </div>
        </div>
    </div>
</div>

## 🌟 Neden SmartRAG?

<div class="alert alert-info">
    <h5><i class="fas fa-star me-2"></i>Kurumsal Hazır</h5>
    <p class="mb-0">Performans, ölçeklenebilirlik ve güvenilirlik göz önünde bulundurularak üretim ortamları için inşa edilmiştir.</p>
</div>

<div class="alert alert-success">
    <h5><i class="fas fa-shield-alt me-2"></i>Üretimde Test Edildi</h5>
    <p class="mb-0">Gerçek dünya uygulamalarında kullanılan, kanıtlanmış geçmişe ve aktif bakıma sahip.</p>
</div>

<div class="alert alert-warning">
    <h5><i class="fas fa-code me-2"></i>Açık Kaynak</h5>
    <p class="mb-0">MIT lisanslı açık kaynak proje, şeffaf geliştirme ve düzenli güncellemeler.</p>
</div>

## 📦 Kurulum

SmartRAG'ı NuGet üzerinden kurun:

```bash
dotnet add package SmartRAG
```

Veya Package Manager kullanarak:

```bash
Install-Package SmartRAG
```

## 🤝 Katkıda Bulunma

Katkılarınızı bekliyoruz! Detaylar için [Katkıda Bulunma Rehberi]({{ site.baseurl }}/tr/contributing) sayfamıza bakın.

## 📄 Lisans

Bu proje MIT Lisansı altında lisanslanmıştır - detaylar için [LICENSE](https://github.com/byerlikaya/SmartRAG/blob/main/LICENSE) dosyasına bakın.

---

<div class="text-center mt-5">
    <p class="text-muted">
        <i class="fas fa-heart text-danger"></i> Barış Yerlikaya tarafından sevgiyle inşa edildi
    </p>
</div>
