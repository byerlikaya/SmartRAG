---
layout: default
title: Service Interfaces
description: Additional service interfaces for repositories, factories, and utilities
lang: en
---

## Additional Service Interfaces

<div class="row g-4 mt-4">
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-database"></i>
            </div>
            <h3>IConversationRepository</h3>
            <p>Data access layer for conversation storage</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/conversation-repository" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-cog"></i>
            </div>
            <h3>IAIConfigurationService</h3>
            <p>AI provider configuration management</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/ai-configuration-service" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-play"></i>
            </div>
            <h3>IAIRequestExecutor</h3>
            <p>AI request execution with retry/fallback</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/ai-request-executor" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-search"></i>
            </div>
            <h3>IQueryIntentAnalyzer</h3>
            <p>Query intent analysis for database routing</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/query-intent-analyzer" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-database"></i>
            </div>
            <h3>IDatabaseQueryExecutor</h3>
            <p>Execute queries across multiple databases</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/database-query-executor" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-compress"></i>
            </div>
            <h3>IResultMerger</h3>
            <p>Merge results from multiple databases</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/result-merger" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-code"></i>
            </div>
            <h3>ISQLQueryGenerator</h3>
            <p>Generate and validate SQL queries</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/sql-query-generator" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-vector-square"></i>
            </div>
            <h3>IEmbeddingSearchService</h3>
            <p>Embedding-based semantic search</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/embedding-search-service" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-building"></i>
            </div>
            <h3>ISourceBuilderService</h3>
            <p>Build search result sources</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/source-builder-service" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-brain"></i>
            </div>
            <h3>IAIProvider</h3>
            <p>Low-level AI provider interface for text generation and embeddings</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/ai-provider" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-industry"></i>
            </div>
            <h3>IAIProviderFactory</h3>
            <p>Factory for creating AI provider instances</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/ai-provider-factory" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-comment-dots"></i>
            </div>
            <h3>IPromptBuilderService</h3>
            <p>Service for building AI prompts for different scenarios</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/prompt-builder-service" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-file-alt"></i>
            </div>
            <h3>IDocumentRepository</h3>
            <p>Repository interface for document storage operations</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/document-repository" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-star"></i>
            </div>
            <h3>IDocumentScoringService</h3>
            <p>Service for scoring document chunks based on query relevance</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/document-scoring-service" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-microphone"></i>
            </div>
            <h3>IAudioParserFactory</h3>
            <p>Factory for creating audio parser service instances</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/audio-parser-factory" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-warehouse"></i>
            </div>
            <h3>IStorageFactory</h3>
            <p>Factory for creating document and conversation storage repositories</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/storage-factory" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-memory"></i>
            </div>
            <h3>IQdrantCacheManager</h3>
            <p>Interface for managing search result caching in Qdrant operations</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/qdrant-cache-manager" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-folder"></i>
            </div>
            <h3>IQdrantCollectionManager</h3>
            <p>Interface for managing Qdrant collections and document storage</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/qdrant-collection-manager" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-vector-square"></i>
            </div>
            <h3>IQdrantEmbeddingService</h3>
            <p>Interface for generating embeddings for text content</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/qdrant-embedding-service" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-search"></i>
            </div>
            <h3>IQdrantSearchService</h3>
            <p>Interface for performing searches in Qdrant vector database</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/qdrant-search-service" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-filter"></i>
            </div>
            <h3>IQueryIntentClassifierService</h3>
            <p>Service for classifying query intent (conversation vs information)</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/query-intent-classifier-service" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-text-height"></i>
            </div>
            <h3>ITextNormalizationService</h3>
            <p>Text normalization and cleaning</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/text-normalization-service" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
</div>

## Related Categories

<div class="row g-4 mt-4">
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-cube"></i>
            </div>
            <h3>Core Interfaces</h3>
            <p>Browse all core interfaces</p>
            <a href="{{ site.baseurl }}/en/api-reference/core" class="btn btn-outline-primary btn-sm mt-3">
                Core Interfaces
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-rocket"></i>
            </div>
            <h3>Advanced Interfaces</h3>
            <p>Multi-database coordination and advanced features</p>
            <a href="{{ site.baseurl }}/en/api-reference/advanced" class="btn btn-outline-primary btn-sm mt-3">
                Advanced Interfaces
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-puzzle-piece"></i>
            </div>
            <h3>Strategy Interfaces</h3>
            <p>Customizable strategies for SQL dialects, scoring, and file parsing</p>
            <a href="{{ site.baseurl }}/en/api-reference/strategies" class="btn btn-outline-primary btn-sm mt-3">
                Strategy Interfaces
            </a>
        </div>
    </div>
</div>

