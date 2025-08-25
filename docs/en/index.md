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
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-file-alt fa-3x text-primary"></i>
                </div>
                <h5 class="card-title">Multi-Format Support</h5>
                <p class="card-text">Process Word, PDF, Excel, and text documents with ease. Our library handles all major document formats automatically.</p>
            </div>
        </div>
    </div>
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-robot fa-3x text-success"></i>
                </div>
                <h5 class="card-title">AI Provider Integration</h5>
                <p class="card-text">Seamlessly integrate with OpenAI, Anthropic, Azure OpenAI, Gemini, and custom AI providers for powerful embedding generation.</p>
            </div>
        </div>
    </div>
</div>

<div class="row mb-5">
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-database fa-3x text-warning"></i>
                </div>
                <h5 class="card-title">Vector Storage</h5>
                <p class="card-text">Multiple storage backends including Qdrant, Redis, SQLite, In-Memory, File System, and custom storage for flexible deployment.</p>
            </div>
        </div>
    </div>
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-search fa-3x text-info"></i>
                </div>
                <h5 class="card-title">Semantic Search</h5>
                <p class="card-text">Advanced search capabilities with similarity scoring and intelligent result ranking for better user experience.</p>
            </div>
        </div>
    </div>
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
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fab fa-google fa-3x text-warning"></i>
            </div>
            <h6 class="mb-1">Gemini</h6>
            <small class="text-muted">Google AI</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fab fa-openai fa-3x text-primary"></i>
            </div>
            <h6 class="mb-1">OpenAI</h6>
            <small class="text-muted">GPT Models</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-cloud fa-3x text-secondary"></i>
            </div>
            <h6 class="mb-1">Azure OpenAI</h6>
            <small class="text-muted">Enterprise</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-robot fa-3x text-success"></i>
            </div>
            <h6 class="mb-1">Anthropic</h6>
            <small class="text-muted">Claude Models</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-cogs fa-3x text-dark"></i>
            </div>
            <h6 class="mb-1">Custom</h6>
            <small class="text-muted">Extensible</small>
        </div>
    </div>
</div>

### 🗄️ Storage Providers

<div class="row mt-4 mb-5">
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-cube fa-3x text-primary"></i>
            </div>
            <h6 class="mb-1">Qdrant</h6>
            <small class="text-muted">Vector Database</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fab fa-redis fa-3x text-success"></i>
            </div>
            <h6 class="mb-1">Redis</h6>
            <small class="text-muted">In-Memory Cache</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-hdd fa-3x text-info"></i>
            </div>
            <h6 class="mb-1">SQLite</h6>
            <small class="text-muted">Local Database</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-microchip fa-3x text-warning"></i>
            </div>
            <h6 class="mb-1">In-Memory</h6>
            <small class="text-muted">Fast Development</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-folder-open fa-3x text-secondary"></i>
            </div>
            <h6 class="mb-1">File System</h6>
            <small class="text-muted">Local Storage</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-cogs fa-3x text-dark"></i>
            </div>
            <h6 class="mb-1">Custom</h6>
            <small class="text-muted">Extensible Storage</small>
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

## 📦 Installation

Install SmartRAG via NuGet:

```bash
dotnet add package SmartRAG
```

Or using Package Manager:

```bash
Install-Package SmartRAG
```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide]({{ site.baseurl }}/en/contributing) for details.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/byerlikaya/SmartRAG/blob/main/LICENSE) file for details.

---

<div class="text-center mt-5">
    <p class="text-muted">
        <i class="fas fa-heart text-danger"></i> Built with love by Barış Yerlikaya
    </p>
</div>
