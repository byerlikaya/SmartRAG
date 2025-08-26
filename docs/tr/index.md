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
            Gelişmiş belge işleme, AI destekli embedding'ler ve anlamsal arama yetenekleri ile akıllı uygulamalar oluşturun.
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

SmartRAG, akıllı belge işleme, embedding üretimi ve anlamsal arama yetenekleri sağlayan kapsamlı bir .NET kütüphanesidir. AI destekli uygulamalar oluşturmak için güçlü özellikler sunarken kullanımı kolay olacak şekilde tasarlanmıştır.

<div class="row mt-5 mb-5">
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body p-4">
                <h5 class="card-title">
                    <div class="feature-icon">
                        <i class="fas fa-file-alt text-primary"></i>
                    </div>
                    Çoklu Format Desteği
                </h5>
                <p class="card-text">Word, PDF, Excel ve metin belgelerini kolayca işleyin. Kütüphanemiz tüm büyük belge formatlarını otomatik olarak işler.</p>
            </div>
        </div>
    </div>
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body p-4">
                <h5 class="card-title">
                    <div class="feature-icon">
                        <i class="fas fa-robot text-success"></i>
                    </div>
                    AI Sağlayıcı Entegrasyonu
                </h5>
                <p class="card-text">Güçlü embedding üretimi için OpenAI, Anthropic, Azure OpenAI, Gemini ve özel AI sağlayıcıları ile sorunsuz entegrasyon.</p>
            </div>
        </div>
    </div>
</div>

<div class="row mb-5">
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body p-4">
                <h5 class="card-title">
                    <div class="feature-icon">
                        <i class="fas fa-database text-warning"></i>
                    </div>
                    Vektör Depolama
                </h5>
                <p class="card-text">Esnek dağıtım için Qdrant, Redis, SQLite, In-Memory ve File System dahil olmak üzere çoklu depolama arka uçları.</p>
            </div>
        </div>
    </div>
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body p-4">
                <h5 class="card-title">
                    <div class="feature-icon">
                        <i class="fas fa-search text-info"></i>
                    </div>
                    Anlamsal Arama
                </h5>
                <p class="card-text">Daha iyi kullanıcı deneyimi için benzerlik puanlaması ve akıllı sonuç sıralaması ile gelişmiş arama yetenekleri.</p>
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
    <h5><i class="fas fa-shield-alt me-2"></i>Üretimde Test Edilmiş</h5>
    <p class="mb-0">Kanıtlanmış geçmişe sahip gerçek dünya uygulamalarında kullanılmış ve aktif bakım yapılmaktadır.</p>
</div>

<div class="alert alert-warning">
    <h5><i class="fas fa-code me-2"></i>Açık Kaynak</h5>
    <p class="mb-0">Şeffaf geliştirme ve düzenli güncellemeler ile MIT lisanslı açık kaynak projesi.</p>
</div>

## ⚡ Hızlı Başlangıç

Basit kurulum sürecimizle dakikalar içinde çalışmaya başlayın:

```csharp
// SmartRAG'ı projenize ekleyin
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
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fab fa-google"></i>
            </div>
            <h6>Gemini</h6>
            <small>Google AI</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-brain"></i>
            </div>
            <h6>OpenAI</h6>
            <small>GPT Modelleri</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-cloud"></i>
            </div>
            <h6>Azure OpenAI</h6>
            <small>Kurumsal</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-robot"></i>
            </div>
            <h6>Anthropic</h6>
            <small>Claude Modelleri</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-cogs"></i>
            </div>
            <h6>Özel</h6>
            <small>Genişletilebilir</small>
        </div>
    </div>
</div>

### 🗄️ Depolama Sağlayıcıları

<div class="row mt-4 mb-5">
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-cube"></i>
            </div>
            <h6>Qdrant</h6>
            <small>Vektör Veritabanı</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-database"></i>
            </div>
            <h6>Redis</h6>
            <small>Bellek İçi Önbellek</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-hdd"></i>
            </div>
            <h6>SQLite</h6>
            <small>Yerel Veritabanı</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-microchip"></i>
            </div>
            <h6>In-Memory</h6>
            <small>Hızlı Geliştirme</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-folder-open"></i>
            </div>
            <h6>File System</h6>
            <small>Yerel Depolama</small>
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
                <p class="card-text">Hızlı kurulum ve kurulum kılavuzu ile çalışmaya başlayın.</p>
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
                <p class="card-text">Sürümler arasında yeni özellikler, iyileştirmeler ve hata düzeltmelerini takip edin.</p>
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

---

<div class="text-center mt-5">
    <p class="text-muted">
        <i class="fas fa-heart text-danger"></i> Barış Yerlikaya tarafından sevgiyle yapılmıştır
    </p>
</div>