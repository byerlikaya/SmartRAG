---
layout: default
title: API Referans
description: SmartRAG interface'leri, metodları ve modelleri için eksiksiz API dokümantasyonu
lang: tr
---

## API Referans Kategorileri

SmartRAG tüm işlemler için iyi tanımlanmış interface'ler sağlar. Kategoriye göre göz atın:

<div class="row g-4 mt-4">
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-cube"></i>
            </div>
            <h3>Temel Arayüzler</h3>
            <p>Doküman arama, yönetim, ayrıştırma ve AI servisleri için temel arayüzler</p>
            <a href="{{ site.baseurl }}/tr/api-reference/core" class="btn btn-outline-primary btn-sm mt-3">
                Temel Arayüzler
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-rocket"></i>
            </div>
            <h3>Gelişmiş Arayüzler</h3>
            <p>Çoklu veritabanı koordinasyonu, şema analizi, ses ve görüntü ayrıştırma</p>
            <a href="{{ site.baseurl }}/tr/api-reference/advanced" class="btn btn-outline-primary btn-sm mt-3">
                Gelişmiş Arayüzler
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-puzzle-piece"></i>
            </div>
            <h3>Strateji Arayüzleri</h3>
            <p>SQL diyalektleri, skorlama ve dosya ayrıştırma için özelleştirilebilir stratejiler</p>
            <a href="{{ site.baseurl }}/tr/api-reference/strategies" class="btn btn-outline-primary btn-sm mt-3">
                Strateji Arayüzleri
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-cogs"></i>
            </div>
            <h3>Servis Arayüzleri</h3>
            <p>Repository'ler, factory'ler ve yardımcılar için ek servis arayüzleri</p>
            <a href="{{ site.baseurl }}/tr/api-reference/services" class="btn btn-outline-primary btn-sm mt-3">
                Servis Arayüzleri
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-database"></i>
            </div>
            <h3>Veri Modelleri</h3>
            <p>RagResponse, Document, DocumentChunk, DatabaseConfig ve diğer veri yapıları</p>
            <a href="{{ site.baseurl }}/tr/api-reference/models" class="btn btn-outline-primary btn-sm mt-3">
                Veri Modelleri
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-list"></i>
            </div>
            <h3>Numaralandırmalar</h3>
            <p>AIProvider, StorageProvider, DatabaseType, RetryPolicy ve diğer enum'lar</p>
            <a href="{{ site.baseurl }}/tr/api-reference/enums" class="btn btn-outline-primary btn-sm mt-3">
                Numaralandırmalar
            </a>
        </div>
    </div>
</div>

## Hızlı Referans

### En Çok Kullanılan Arayüzler

- **[IDocumentSearchService]({{ site.baseurl }}/tr/api-reference/interfaces/document-search-service)** - AI destekli akıllı sorgu işleme
- **[IDocumentService]({{ site.baseurl }}/tr/api-reference/interfaces/document-service)** - Doküman CRUD işlemleri
- **[IMultiDatabaseQueryCoordinator]({{ site.baseurl }}/tr/api-reference/interfaces/multi-database-query-coordinator)** - Çoklu veritabanı sorgu koordinasyonu
- **[IAIService]({{ site.baseurl }}/tr/api-reference/interfaces/ai-service)** - AI sağlayıcı iletişimi
- **[IDocumentRepository]({{ site.baseurl }}/tr/api-reference/interfaces/document-repository)** - Doküman depolama işlemleri

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-lightbulb"></i>
            </div>
            <h3>Örnekler</h3>
            <p>Pratik kod örnekleri ve gerçek dünya uygulamalarını görün</p>
            <a href="{{ site.baseurl }}/tr/examples" class="btn btn-outline-primary btn-sm mt-3">
                Örnekleri Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-rocket"></i>
            </div>
            <h3>Başlangıç</h3>
            <p>Hızlı kurulum ve kurulum rehberi</p>
            <a href="{{ site.baseurl }}/tr/getting-started" class="btn btn-outline-primary btn-sm mt-3">
                Başlangıç
            </a>
        </div>
    </div>
</div>

