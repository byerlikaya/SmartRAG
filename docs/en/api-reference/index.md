---
layout: default
title: API Reference
description: Complete API documentation for SmartRAG interfaces, methods, and models
lang: en
---

## API Reference Categories

SmartRAG provides well-defined interfaces for all operations. Browse by category:

<div class="row g-4 mt-4">
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-cube"></i>
            </div>
            <h3>Core Interfaces</h3>
            <p>Essential interfaces for document search, management, parsing, and AI services</p>
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
            <p>Multi-database coordination, schema analysis, audio and image parsing</p>
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
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-cogs"></i>
            </div>
            <h3>Service Interfaces</h3>
            <p>Additional service interfaces for repositories, factories, and utilities</p>
            <a href="{{ site.baseurl }}/en/api-reference/services" class="btn btn-outline-primary btn-sm mt-3">
                Service Interfaces
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-database"></i>
            </div>
            <h3>Data Models</h3>
            <p>RagResponse, Document, DocumentChunk, DatabaseConfig and other data structures</p>
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
            <p>AIProvider, StorageProvider, DatabaseType, RetryPolicy and other enums</p>
            <a href="{{ site.baseurl }}/en/api-reference/enums" class="btn btn-outline-primary btn-sm mt-3">
                Enumerations
            </a>
        </div>
    </div>
</div>

## Quick Reference

### Most Used Interfaces

- **[IDocumentSearchService]({{ site.baseurl }}/en/api-reference/interfaces/document-search-service)** - AI-powered intelligent query processing
- **[IDocumentService]({{ site.baseurl }}/en/api-reference/interfaces/document-service)** - Document CRUD operations
- **[IMultiDatabaseQueryCoordinator]({{ site.baseurl }}/en/api-reference/interfaces/multi-database-query-coordinator)** - Multi-database query coordination
- **[IAIService]({{ site.baseurl }}/en/api-reference/interfaces/ai-service)** - AI provider communication
- **[IDocumentRepository]({{ site.baseurl }}/en/api-reference/interfaces/document-repository)** - Document storage operations

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-lightbulb"></i>
            </div>
            <h3>Examples</h3>
            <p>See practical code examples and real-world implementations</p>
            <a href="{{ site.baseurl }}/en/examples" class="btn btn-outline-primary btn-sm mt-3">
                View Examples
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-rocket"></i>
            </div>
            <h3>Getting Started</h3>
            <p>Quick installation and setup guide</p>
            <a href="{{ site.baseurl }}/en/getting-started" class="btn btn-outline-primary btn-sm mt-3">
                Get Started
            </a>
        </div>
    </div>
</div>

