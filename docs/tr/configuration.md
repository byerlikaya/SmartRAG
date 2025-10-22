---
layout: default
title: Yapılandırma
description: SmartRAG için eksiksiz yapılandırma kılavuzu - AI sağlayıcıları, depolama, veritabanları ve gelişmiş seçenekler
lang: tr
---

## Yapılandırma Kategorileri

SmartRAG yapılandırması aşağıdaki kategorilere ayrılmıştır:

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-cog"></i>
            </div>
            <h3>Temel Yapılandırma</h3>
            <p>Yapılandırma yöntemleri, temel seçenekler, parçalama ve yeniden deneme ayarları</p>
            <a href="{{ site.baseurl }}/tr/configuration/basic" class="btn btn-outline-primary btn-sm mt-3">
                Temel Yapılandırma
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-brain"></i>
            </div>
            <h3>AI Sağlayıcıları</h3>
            <p>OpenAI, Anthropic, Google Gemini, Azure OpenAI ve özel sağlayıcılar</p>
            <a href="{{ site.baseurl }}/tr/configuration/ai-providers" class="btn btn-outline-primary btn-sm mt-3">
                AI Sağlayıcıları
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-database"></i>
            </div>
            <h3>Depolama Sağlayıcıları</h3>
            <p>Qdrant, Redis, SQLite, FileSystem ve InMemory depolama seçenekleri</p>
            <a href="{{ site.baseurl }}/tr/configuration/storage" class="btn btn-outline-primary btn-sm mt-3">
                Depolama Sağlayıcıları
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-server"></i>
            </div>
            <h3>Veritabanı Yapılandırması</h3>
            <p>Çoklu veritabanı bağlantıları, şema analizi ve güvenlik ayarları</p>
            <a href="{{ site.baseurl }}/tr/configuration/database" class="btn btn-outline-primary btn-sm mt-3">
                Veritabanı Yapılandırması
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-microphone"></i>
            </div>
            <h3>Ses & OCR</h3>
            <p>Google Speech-to-Text ve Tesseract OCR yapılandırması</p>
            <a href="{{ site.baseurl }}/tr/configuration/audio-ocr" class="btn btn-outline-primary btn-sm mt-3">
                Ses & OCR
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-cogs"></i>
            </div>
            <h3>Gelişmiş Yapılandırma</h3>
            <p>Yedek sağlayıcılar, en iyi pratikler ve sonraki adımlar</p>
            <a href="{{ site.baseurl }}/tr/configuration/advanced" class="btn btn-outline-primary btn-sm mt-3">
                Gelişmiş Yapılandırma
            </a>
        </div>
    </div>
</div>

## Hızlı Başlangıç

### Basit Yapılandırma

```csharp
builder.Services.UseSmartRag(configuration,
    storageProvider: StorageProvider.InMemory,
    aiProvider: AIProvider.Gemini
);
```

### Gelişmiş Yapılandırma

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
    options.MaxRetryAttempts = 3;
});
```

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-rocket"></i>
            </div>
            <h3>Başlangıç</h3>
            <p>SmartRAG'ı projenize entegre edin</p>
            <a href="{{ site.baseurl }}/tr/getting-started" class="btn btn-outline-primary btn-sm mt-3">
                Başlangıç Kılavuzu
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-code"></i>
            </div>
            <h3>Örnekler</h3>
            <p>Pratik örnekleri ve gerçek dünya kullanım senaryolarını görün</p>
            <a href="{{ site.baseurl }}/tr/examples" class="btn btn-outline-primary btn-sm mt-3">
                Örnekleri Gör
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-book"></i>
            </div>
            <h3>API Referansı</h3>
            <p>Detaylı API dokümantasyonu ve metod referansları</p>
            <a href="{{ site.baseurl }}/tr/api-reference" class="btn btn-outline-primary btn-sm mt-3">
                API Referansı
            </a>
        </div>
    </div>
</div>