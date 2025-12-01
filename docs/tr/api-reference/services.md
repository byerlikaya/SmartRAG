---
layout: default
title: Servis Arayüzleri
description: Repository'ler, factory'ler ve yardımcılar için ek servis arayüzleri
lang: tr
---

## Ek Servis Arayüzleri

<div class="row g-4 mt-4">
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-database"></i>
            </div>
            <h3>IConversationRepository</h3>
            <p>Konuşma depolama için veri erişim katmanı</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/conversation-repository" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-cog"></i>
            </div>
            <h3>IAIConfigurationService</h3>
            <p>AI sağlayıcı yapılandırma yönetimi</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/ai-configuration-service" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-play"></i>
            </div>
            <h3>IAIRequestExecutor</h3>
            <p>Yeniden deneme/fallback ile AI istek yürütme</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/ai-request-executor" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-search"></i>
            </div>
            <h3>IQueryIntentAnalyzer</h3>
            <p>Veritabanı yönlendirmesi için sorgu niyeti analizi</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/query-intent-analyzer" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-database"></i>
            </div>
            <h3>IDatabaseQueryExecutor</h3>
            <p>Birden fazla veritabanında sorgu yürütme</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/database-query-executor" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-compress"></i>
            </div>
            <h3>IResultMerger</h3>
            <p>Birden fazla veritabanından sonuçları birleştirme</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/result-merger" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-code"></i>
            </div>
            <h3>ISQLQueryGenerator</h3>
            <p>SQL sorguları üretme ve doğrulama</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/sql-query-generator" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-vector-square"></i>
            </div>
            <h3>IEmbeddingSearchService</h3>
            <p>Embedding tabanlı anlamsal arama</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/embedding-search-service" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-building"></i>
            </div>
            <h3>ISourceBuilderService</h3>
            <p>Arama sonucu kaynakları oluşturma</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/source-builder-service" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-brain"></i>
            </div>
            <h3>IAIProvider</h3>
            <p>Metin üretimi ve embedding'ler için düşük seviye AI sağlayıcı arayüzü</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/ai-provider" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-industry"></i>
            </div>
            <h3>IAIProviderFactory</h3>
            <p>AI sağlayıcı örnekleri oluşturma factory'si</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/ai-provider-factory" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-comment-dots"></i>
            </div>
            <h3>IPromptBuilderService</h3>
            <p>Farklı senaryolar için AI prompt'ları oluşturma servisi</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/prompt-builder-service" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-file-alt"></i>
            </div>
            <h3>IDocumentRepository</h3>
            <p>Doküman depolama işlemleri için repository arayüzü</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/document-repository" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-star"></i>
            </div>
            <h3>IDocumentScoringService</h3>
            <p>Sorgu ilgisine göre doküman chunk'larını skorlama servisi</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/document-scoring-service" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-microphone"></i>
            </div>
            <h3>IAudioParserFactory</h3>
            <p>Ses ayrıştırma servisi örnekleri oluşturma factory'si</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/audio-parser-factory" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-warehouse"></i>
            </div>
            <h3>IStorageFactory</h3>
            <p>Doküman ve konuşma depolama repository'leri oluşturma factory'si</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/storage-factory" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-memory"></i>
            </div>
            <h3>IQdrantCacheManager</h3>
            <p>Qdrant işlemlerinde arama sonucu önbelleğe alma yönetimi arayüzü</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/qdrant-cache-manager" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-folder"></i>
            </div>
            <h3>IQdrantCollectionManager</h3>
            <p>Qdrant koleksiyonları ve doküman depolama yönetimi arayüzü</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/qdrant-collection-manager" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-vector-square"></i>
            </div>
            <h3>IQdrantEmbeddingService</h3>
            <p>Metin içeriği için embedding'ler oluşturma arayüzü</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/qdrant-embedding-service" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-search"></i>
            </div>
            <h3>IQdrantSearchService</h3>
            <p>Qdrant vektör veritabanında arama yapma arayüzü</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/qdrant-search-service" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-filter"></i>
            </div>
            <h3>IQueryIntentClassifierService</h3>
            <p>Sorgu niyetini sınıflandırma servisi (konuşma vs bilgi)</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/query-intent-classifier-service" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-text-height"></i>
            </div>
            <h3>ITextNormalizationService</h3>
            <p>Metin normalleştirme ve temizleme</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/text-normalization-service" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
</div>

## İlgili Kategoriler

<div class="row g-4 mt-4">
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-cube"></i>
            </div>
            <h3>Temel Arayüzler</h3>
            <p>Tüm temel arayüzleri görüntüle</p>
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
            <p>Çoklu veritabanı koordinasyonu ve gelişmiş özellikler</p>
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
</div>

