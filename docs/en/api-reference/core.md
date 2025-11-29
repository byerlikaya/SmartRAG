---
layout: default
title: Core Interfaces
description: Essential SmartRAG interfaces for document search, management, parsing, and AI services
lang: en
---

## Core Interfaces

SmartRAG provides well-defined interfaces for all operations. Inject these interfaces via dependency injection.

<div class="row g-4 mt-4">
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-search"></i>
            </div>
            <h3>IDocumentSearchService</h3>
            <p>AI-powered intelligent query processing with RAG pipeline and conversation management</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/document-search-service" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-file"></i>
            </div>
            <h3>IDocumentService</h3>
            <p>Document CRUD operations and management</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/document-service" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-comments"></i>
            </div>
            <h3>IConversationManagerService</h3>
            <p>Conversation session management and history tracking</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/conversation-manager-service" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-file-alt"></i>
            </div>
            <h3>IDocumentParserService</h3>
            <p>Multi-format document parsing and text extraction</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/document-parser-service" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-database"></i>
            </div>
            <h3>IDatabaseParserService</h3>
            <p>Universal database support with live connections</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/database-parser-service" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-brain"></i>
            </div>
            <h3>IAIService</h3>
            <p>AI provider communication for text generation and embeddings</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/ai-service" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-search-plus"></i>
            </div>
            <h3>ISemanticSearchService</h3>
            <p>Advanced semantic search with hybrid scoring</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/semantic-search-service" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-3">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-expand"></i>
            </div>
            <h3>IContextExpansionService</h3>
            <p>Expand document chunk context by including adjacent chunks</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/context-expansion-service" class="btn btn-outline-primary btn-sm mt-3">
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
                <i class="fas fa-database"></i>
            </div>
            <h3>Data Models</h3>
            <p>RagResponse, Document, DocumentChunk and other data structures</p>
            <a href="{{ site.baseurl }}/en/api-reference/models" class="btn btn-outline-primary btn-sm mt-3">
                Data Models
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-list"></i>
            </div>
            <h3>Enumerations</h3>
            <p>AIProvider, StorageProvider, DatabaseType and other enums</p>
            <a href="{{ site.baseurl }}/en/api-reference/enums" class="btn btn-outline-primary btn-sm mt-3">
                Enumerations
            </a>
        </div>
    </div>
</div>

