---
layout: default
title: Temel Arayüzler
description: Doküman arama, yönetim, ayrıştırma ve AI servisleri için temel SmartRAG arayüzleri
lang: tr
---

## Temel Arayüzler

SmartRAG tüm işlemler için iyi tanımlanmış interface'ler sağlar. Bu interface'leri dependency injection ile enjekte edin.

<div class="row g-4 mt-4">
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-search"></i>
            </div>
            <h3>IDocumentSearchService</h3>
            <p>RAG pipeline ve konuşma yönetimi ile AI destekli akıllı sorgu işleme</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/document-search-service" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-file"></i>
            </div>
            <h3>IDocumentService</h3>
            <p>Doküman CRUD işlemleri ve yönetimi</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/document-service" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-comments"></i>
            </div>
            <h3>IConversationManagerService</h3>
            <p>Konuşma oturumu yönetimi ve geçmiş takibi</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/conversation-manager-service" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-file-alt"></i>
            </div>
            <h3>IDocumentParserService</h3>
            <p>Çoklu format doküman ayrıştırma ve metin çıkarma</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/document-parser-service" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-database"></i>
            </div>
            <h3>IDatabaseParserService</h3>
            <p>Canlı bağlantılarla evrensel veritabanı desteği</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/database-parser-service" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-brain"></i>
            </div>
            <h3>IAIService</h3>
            <p>Metin üretimi ve embedding'ler için AI sağlayıcı iletişimi</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/ai-service" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-search-plus"></i>
            </div>
            <h3>ISemanticSearchService</h3>
            <p>Hibrit skorlama ile gelişmiş anlamsal arama</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/semantic-search-service" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-expand"></i>
            </div>
            <h3>IContextExpansionService</h3>
            <p>Bitişik chunk'ları dahil ederek doküman chunk bağlamını genişletme</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/context-expansion-service" class="btn btn-outline-primary btn-sm mt-3">
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
                <i class="fas fa-database"></i>
            </div>
            <h3>Veri Modelleri</h3>
            <p>RagResponse, Document, DocumentChunk ve diğer veri yapıları</p>
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
            <p>AIProvider, StorageProvider, DatabaseType ve diğer enum'lar</p>
            <a href="{{ site.baseurl }}/tr/api-reference/enums" class="btn btn-outline-primary btn-sm mt-3">
                Numaralandırmalar
            </a>
        </div>
    </div>
</div>

