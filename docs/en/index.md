---
layout: default
title: SmartRAG Documentation
description: Enterprise-grade RAG library for .NET applications
lang: en
---

<div class="hero-section text-center py-5 mb-5">
    <div class="hero-content">
        <h1 class="hero-title display-4 fw-bold mb-4">
            <i class="fas fa-brain me-3"></i>
            SmartRAG
        </h1>
        <p class="hero-subtitle lead mb-4">
            Enterprise-grade RAG library for .NET applications
        </p>
        <p class="hero-description mb-5">
            Build intelligent applications with advanced document processing, AI-powered embeddings, and semantic search capabilities.
        </p>
        <div class="hero-buttons">
            <a href="{{ site.baseurl }}/en/getting-started" class="btn btn-primary btn-lg me-3">
                <i class="fas fa-rocket me-2"></i>Get Started
            </a>
            <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-primary btn-lg me-3" target="_blank" rel="noopener noreferrer">
                <i class="fab fa-github me-2"></i>View on GitHub
            </a>
            <a href="https://www.nuget.org/packages/SmartRAG" class="btn btn-outline-success btn-lg" target="_blank" rel="noopener noreferrer">
                <i class="fas fa-box me-2"></i>NuGet Package
            </a>
        </div>
    </div>
</div>

## 🚀 What is SmartRAG?

SmartRAG is a comprehensive .NET library that provides intelligent document processing, embedding generation, and semantic search capabilities. It's designed to be easy to use while offering powerful features for building AI-powered applications.

<div class="row mt-5 mb-5">
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body p-4">
                <h5 class="card-title">
                    <div class="feature-icon">
                        <i class="fas fa-file-alt text-primary"></i>
                    </div>
                    Multi-Format Support
                </h5>
                <p class="card-text">Process Word, PDF, Excel, and text documents with ease. Our library handles all major document formats automatically.</p>
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
                    AI Provider Integration
                </h5>
                <p class="card-text">Seamlessly integrate with OpenAI, Anthropic, Azure OpenAI, Gemini, and custom AI providers for powerful embedding generation.</p>
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
                    Vector Storage
                </h5>
                <p class="card-text">Multiple storage backends including Qdrant, Redis, SQLite, In-Memory, and File System for flexible deployment.</p>
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
                    Semantic Search
                </h5>
                <p class="card-text">Advanced search capabilities with similarity scoring and intelligent result ranking for better user experience.</p>
            </div>
        </div>
    </div>
</div>

## 🌟 Why Choose SmartRAG?

<div class="alert alert-info">
    <h5><i class="fas fa-star me-2"></i>Enterprise Ready</h5>
    <p class="mb-0">Built for production environments with performance, scalability, and reliability in mind.</p>
</div>

<div class="alert alert-success">
    <h5><i class="fas fa-shield-alt me-2"></i>Production Tested</h5>
    <p class="mb-0">Used in real-world applications with proven track record and active maintenance.</p>
</div>

<div class="alert alert-warning">
    <h5><i class="fas fa-code me-2"></i>Open Source</h5>
    <p class="mb-0">MIT licensed open source project with transparent development and regular updates.</p>
</div>

## ⚡ Quick Start

Get up and running in minutes with our simple setup process:

```csharp
// Add SmartRAG to your project
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});

// Use the document service
var documentService = serviceProvider.GetRequiredService<IDocumentService>();
var document = await documentService.UploadDocumentAsync(file);
```

## 🚀 Supported Technologies

SmartRAG integrates with leading AI providers and storage solutions to give you the best possible experience.

### 🤖 AI Providers

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
            <small>GPT Models</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-cloud"></i>
            </div>
            <h6>Azure OpenAI</h6>
            <small>Enterprise</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-robot"></i>
            </div>
            <h6>Anthropic</h6>
            <small>Claude Models</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-cogs"></i>
            </div>
            <h6>Custom</h6>
            <small>Extensible</small>
        </div>
    </div>
</div>

### 🗄️ Storage Providers

<div class="row mt-4 mb-5">
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-cube"></i>
            </div>
            <h6>Qdrant</h6>
            <small>Vector Database</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-database"></i>
            </div>
            <h6>Redis</h6>
            <small>In-Memory Cache</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-hdd"></i>
            </div>
            <h6>SQLite</h6>
            <small>Local Database</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-microchip"></i>
            </div>
            <h6>In-Memory</h6>
            <small>Fast Development</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-folder-open"></i>
            </div>
            <h6>File System</h6>
            <small>Local Storage</small>
        </div>
    </div>
</div>

## 📚 Documentation

<div class="row mt-4">
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-rocket fa-2x text-primary mb-3"></i>
                <h5 class="card-title">Getting Started</h5>
                <p class="card-text">Quick installation and setup guide to get you up and running.</p>
                <a href="{{ site.baseurl }}/en/getting-started" class="btn btn-primary">Get Started</a>
            </div>
        </div>
    </div>
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-cog fa-2x text-success mb-3"></i>
                <h5 class="card-title">Configuration</h5>
                <p class="card-text">Detailed configuration options and best practices.</p>
                <a href="{{ site.baseurl }}/en/configuration" class="btn btn-success">Configure</a>
            </div>
        </div>
    </div>
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-code fa-2x text-warning mb-3"></i>
                <h5 class="card-title">API Reference</h5>
                <p class="card-text">Complete API documentation with examples and usage patterns.</p>
                <a href="{{ site.baseurl }}/en/api-reference" class="btn btn-warning">View API</a>
            </div>
        </div>
    </div>
</div>

<div class="row mt-4">
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-lightbulb fa-2x text-info mb-3"></i>
                <h5 class="card-title">Examples</h5>
                <p class="card-text">Real-world examples and sample applications to learn from.</p>
                <a href="{{ site.baseurl }}/en/examples" class="btn btn-info">View Examples</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-tools fa-2x text-danger mb-3"></i>
                <h5 class="card-title">Troubleshooting</h5>
                <p class="card-text">Common issues and solutions to help you resolve problems.</p>
                <a href="{{ site.baseurl }}/en/troubleshooting" class="btn btn-danger">Get Help</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-history fa-2x text-secondary mb-3"></i>
                <h5 class="card-title">Changelog</h5>
                <p class="card-text">Track new features, improvements, and bug fixes across versions.</p>
                <a href="{{ site.baseurl }}/en/changelog" class="btn btn-secondary">View Changes</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-hands-helping fa-2x text-dark mb-3"></i>
                <h5 class="card-title">Contributing</h5>
                <p class="card-text">Learn how to contribute to SmartRAG development.</p>
                <a href="{{ site.baseurl }}/en/contributing" class="btn btn-dark">Contribute</a>
            </div>
        </div>
    </div>
</div>

---

<div class="text-center mt-5">
    <p class="text-muted">
        <i class="fas fa-heart text-danger"></i> Built with love by Barış Yerlikaya
    </p>
</div>