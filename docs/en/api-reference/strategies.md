---
layout: default
title: Strategy Pattern Interfaces
description: Customizable strategies for SQL dialects, scoring, and file parsing
lang: en
---

## Strategy Pattern Interfaces

SmartRAG provides Strategy Pattern for extensibility and customization.

<div class="row g-4 mt-4">
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-code"></i>
            </div>
            <h3>ISqlDialectStrategy</h3>
            <p>Database-specific SQL generation and validation</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/sql-dialect-strategy" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-star"></i>
            </div>
            <h3>IScoringStrategy</h3>
            <p>Customizable document relevance scoring</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/scoring-strategy" class="btn btn-outline-primary btn-sm mt-3">
                View Interface
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-file"></i>
            </div>
            <h3>IFileParser</h3>
            <p>Strategy for parsing specific file formats</p>
            <a href="{{ site.baseurl }}/en/api-reference/interfaces/file-parser" class="btn btn-outline-primary btn-sm mt-3">
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
                <i class="fas fa-wrench"></i>
            </div>
            <h3>Service Interfaces</h3>
            <p>Additional service interfaces for repositories, factories, and utilities</p>
            <a href="{{ site.baseurl }}/en/api-reference/services" class="btn btn-outline-primary btn-sm mt-3">
                Service Interfaces
            </a>
        </div>
    </div>
</div>

