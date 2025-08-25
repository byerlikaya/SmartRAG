---
layout: default
title: SmartRAG Documentation
description: Enterprise-grade RAG library for .NET applications
lang: en
---

<div class="text-center py-5">
    <h1 class="display-4 mb-4">🌍 SmartRAG Documentation</h1>
    <p class="lead mb-4">Choose your language / Dilinizi seçin / Wählen Sie Ihre Sprache / Выберите ваш язык</p>
    
    <div class="row justify-content-center">
        <div class="col-md-3 mb-3">
            <a href="{{ site.baseurl }}/en/" class="btn btn-primary btn-lg w-100">
                <i class="fas fa-flag me-2"></i>English
            </a>
        </div>
        <div class="col-md-3 mb-3">
            <a href="{{ site.baseurl }}/tr/" class="btn btn-success btn-lg w-100">
                <i class="fas fa-flag me-2"></i>Türkçe
            </a>
        </div>
        <div class="col-md-3 mb-3">
            <a href="{{ site.baseurl }}/de/" class="btn btn-warning btn-lg w-100">
                <i class="fas fa-flag me-2"></i>Deutsch
            </a>
        </div>
        <div class="col-md-3 mb-3">
            <a href="{{ site.baseurl }}/ru/" class="btn btn-danger btn-lg w-100">
                <i class="fas fa-flag me-2"></i>Русский
            </a>
        </div>
    </div>
</div>

---

# SmartRAG Documentation

<div class="hero-section text-center py-5 mb-5">
    <div class="hero-content">
                            <h1 class="hero-title display-4 fw-bold mb-4">
                        <i class="fas fa-brain me-3"></i>
                        {{ site.data[site.lang].home.hero.title | default: "SmartRAG" }}
                    </h1>
                    <p class="hero-subtitle lead mb-4">
                        {{ site.data[site.lang].home.hero.subtitle | default: "Enterprise-grade RAG library for .NET applications" }}
                    </p>
                    <p class="hero-description mb-5">
                        {{ site.data[site.lang].home.hero.description | default: "Build intelligent applications with advanced document processing, AI-powered embeddings, and semantic search capabilities." }}
                    </p>
        <div class="hero-buttons">
                                    <a href="{{ site.baseurl }}/{{ site.lang }}/getting-started" class="btn btn-primary btn-lg me-3">
                            <i class="fas fa-rocket me-2"></i>{{ site.data[site.lang].home.hero.get_started | default: "Get Started" }}
                        </a>
                        <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-primary btn-lg me-3" target="_blank" rel="noopener noreferrer">
                            <i class="fab fa-github me-2"></i>{{ site.data[site.lang].home.hero.view_github | default: "View on GitHub" }}
                        </a>
                        <a href="https://www.nuget.org/packages/SmartRAG" class="btn btn-outline-success btn-lg" target="_blank" rel="noopener noreferrer">
                            <i class="fas fa-box me-2"></i>{{ site.data[site.lang].home.hero.nuget_package | default: "NuGet Package" }}
                        </a>
        </div>
    </div>
</div>

## 🚀 {{ site.data[site.lang].home.features.title | default: "What is SmartRAG?" }}

SmartRAG is a comprehensive .NET library that provides intelligent document processing, embedding generation, and semantic search capabilities. It's designed to be easy to use while offering powerful features for building AI-powered applications.

<div class="row mt-5 mb-5">
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-file-alt fa-3x text-primary"></i>
                </div>
                                            <h5 class="card-title">{{ site.data[site.lang].home.features.multi_format | default: "Multi-Format Support" }}</h5>
                            <p class="card-text">{{ site.data[site.lang].home.features.multi_format_desc | default: "Process Word, PDF, Excel, and text documents with ease. Our library handles all major document formats automatically." }}</p>
            </div>
        </div>
    </div>
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-robot fa-3x text-success"></i>
                </div>
                                            <h5 class="card-title">{{ site.data[site.lang].home.features.ai_integration | default: "AI Provider Integration" }}</h5>
                            <p class="card-text">{{ site.data[site.lang].home.features.ai_integration_desc | default: "Seamlessly integrate with OpenAI, Anthropic, Azure OpenAI, Gemini, and custom AI providers for powerful embedding generation." }}</p>
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
                                            <h5 class="card-title">{{ site.data[site.lang].home.features.vector_storage | default: "Vector Storage" }}</h5>
                            <p class="card-text">{{ site.data[site.lang].home.features.vector_storage_desc | default: "Multiple storage backends including Qdrant, Redis, SQLite, In-Memory, File System, and custom storage for flexible deployment." }}</p>
            </div>
        </div>
    </div>
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-search fa-3x text-info"></i>
                </div>
                                            <h5 class="card-title">{{ site.data[site.lang].home.features.semantic_search | default: "Semantic Search" }}</h5>
                            <p class="card-text">{{ site.data[site.lang].home.features.semantic_search_desc | default: "Advanced search capabilities with similarity scoring and intelligent result ranking for better user experience." }}</p>
            </div>
        </div>
    </div>
</div>

## ⚡ {{ site.data[site.lang].home.quick_start.title | default: "Quick Start" }}

{{ site.data[site.lang].home.quick_start.description | default: "Get up and running in minutes with our simple setup process:" }}

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

## 🚀 {{ site.data[site.lang].home.supported_tech.title | default: "Supported Technologies" }}

{{ site.data[site.lang].home.supported_tech.description | default: "SmartRAG integrates with leading AI providers and storage solutions to give you the best possible experience." }}

### 🤖 {{ site.data[site.lang].home.supported_tech.ai_providers | default: "AI Providers" }}

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

### 🗄️ {{ site.data[site.lang].home.supported_tech.storage_providers | default: "Storage Providers" }}

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
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-cogs"></i>
            </div>
            <h6>Custom</h6>
            <small>Extensible Storage</small>
        </div>
    </div>
</div>

## 📚 {{ site.data[site.lang].home.documentation.title | default: "Documentation" }}

<div class="row mt-4">
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-rocket fa-2x text-primary mb-3"></i>
                                            <h5 class="card-title">{{ site.data[site.lang].home.documentation.getting_started | default: "Getting Started" }}</h5>
                            <p class="card-text">{{ site.data[site.lang].home.documentation.getting_started_desc | default: "Quick installation and setup guide to get you up and running." }}</p>
                            <a href="{{ site.baseurl }}/{{ site.lang }}/getting-started" class="btn btn-primary">{{ site.data[site.lang].home.hero.get_started | default: "Get Started" }}</a>
            </div>
        </div>
    </div>
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-cog fa-2x text-success mb-3"></i>
                                            <h5 class="card-title">{{ site.data[site.lang].home.documentation.configuration | default: "Configuration" }}</h5>
                            <p class="card-text">{{ site.data[site.lang].home.documentation.configuration_desc | default: "Detailed configuration options and best practices." }}</p>
                            <a href="{{ site.baseurl }}/{{ site.lang }}/configuration" class="btn btn-success">Configure</a>
            </div>
        </div>
    </div>
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-code fa-2x text-warning mb-3"></i>
                                            <h5 class="card-title">{{ site.data[site.lang].home.documentation.api_reference | default: "API Reference" }}</h5>
                            <p class="card-text">{{ site.data[site.lang].home.documentation.api_reference_desc | default: "Complete API documentation with examples and usage patterns." }}</p>
                            <a href="{{ site.baseurl }}/{{ site.lang }}/api-reference" class="btn btn-warning">View API</a>
            </div>
        </div>
    </div>
</div>

<div class="row mt-4">
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-lightbulb fa-2x text-info mb-3"></i>
                <h5 class="card-title">{{ site.data[site.lang].home.documentation.examples | default: "Examples" }}</h5>
                <p class="card-text">{{ site.data[site.lang].home.documentation.examples_desc | default: "Real-world examples and sample applications to learn from." }}</p>
                <a href="{{ site.baseurl }}/{{ site.lang }}/examples" class="btn btn-info">View Examples</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-tools fa-2x text-danger mb-3"></i>
                <h5 class="card-title">{{ site.data[site.lang].home.documentation.troubleshooting | default: "Troubleshooting" }}</h5>
                <p class="card-text">{{ site.data[site.lang].home.documentation.troubleshooting_desc | default: "Common issues and solutions to help you resolve problems." }}</p>
                <a href="{{ site.baseurl }}/{{ site.lang }}/troubleshooting" class="btn btn-danger">Get Help</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-history fa-2x text-secondary mb-3"></i>
                <h5 class="card-title">{{ site.data[site.lang].home.documentation.changelog | default: "Changelog" }}</h5>
                <p class="card-text">{{ site.data[site.lang].home.documentation.changelog_desc | default: "Track new features, improvements, and bug fixes across versions." }}</p>
                <a href="{{ site.baseurl }}/{{ site.lang }}/changelog" class="btn btn-secondary">View Changes</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-hands-helping fa-2x text-dark mb-3"></i>
                <h5 class="card-title">{{ site.data[site.lang].home.documentation.contributing | default: "Contributing" }}</h5>
                <p class="card-text">{{ site.data[site.lang].home.documentation.contributing_desc | default: "Learn how to contribute to SmartRAG development." }}</p>
                <a href="{{ site.baseurl }}/{{ site.lang }}/contributing" class="btn btn-dark">Contribute</a>
            </div>
        </div>
    </div>
</div>

## 🔧 Examples & Troubleshooting

<div class="row mt-4">
    <div class="col-md-6 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-lightbulb fa-2x text-info mb-3"></i>
                <h5 class="card-title">Examples</h5>
                <p class="card-text">Real-world examples and sample applications to learn from.</p>
                <a href="{{ site.baseurl }}/examples" class="btn btn-info">View Examples</a>
            </div>
        </div>
    </div>
    <div class="col-md-6 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-tools fa-2x text-danger mb-3"></i>
                <h5 class="card-title">Troubleshooting</h5>
                <p class="card-text">Common issues and solutions to help you resolve problems.</p>
                <a href="{{ site.baseurl }}/troubleshooting" class="btn btn-danger">Get Help</a>
            </div>
        </div>
    </div>
</div>

## 🌟 {{ site.data[site.lang].home.why_choose.title | default: "Why Choose SmartRAG?" }}

<div class="alert alert-info">
    <h5><i class="fas fa-star me-2"></i>{{ site.data[site.lang].home.why_choose.enterprise_ready | default: "Enterprise Ready" }}</h5>
    <p class="mb-0">{{ site.data[site.lang].home.why_choose.enterprise_ready_desc | default: "Built for production environments with performance, scalability, and reliability in mind." }}</p>
</div>

<div class="alert alert-success">
    <h5><i class="fas fa-shield-alt me-2"></i>{{ site.data[site.lang].home.why_choose.production_tested | default: "Production Tested" }}</h5>
    <p class="mb-0">{{ site.data[site.lang].home.why_choose.production_tested_desc | default: "Used in real-world applications with proven track record and active maintenance." }}</p>
</div>

<div class="alert alert-warning">
    <h5><i class="fas fa-code me-2"></i>{{ site.data[site.lang].home.why_choose.open_source | default: "Open Source" }}</h5>
    <p class="mb-0">{{ site.data[site.lang].home.why_choose.open_source_desc | default: "MIT licensed open source project with transparent development and regular updates." }}</p>
</div>

## 📦 {{ site.data[site.lang].home.installation.title | default: "Installation" }}

{{ site.data[site.lang].home.installation.description | default: "Install SmartRAG via NuGet:" }}

```bash
dotnet add package SmartRAG
```

Or using Package Manager:

```bash
Install-Package SmartRAG
```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/byerlikaya/SmartRAG/blob/main/CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/byerlikaya/SmartRAG/blob/main/LICENSE) file for details.

---

<div class="text-center mt-5">
                    <p class="text-muted">
                    <i class="fas fa-heart text-danger"></i> {{ site.data[site.lang].home.footer.built_by | default: "Built with love by Barış Yerlikaya" }}
                </p>
</div>
