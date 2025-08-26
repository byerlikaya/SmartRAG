---
layout: default
title: SmartRAG Documentation
description: Enterprise-grade RAG library for .NET applications
lang: en
---

<!-- Hero Section -->
<section class="hero-section">
    <div class="hero-background"></div>
    <div class="container">
        <div class="row align-items-center min-vh-100">
            <div class="col-lg-6">
                <div class="hero-content">
                    <div class="hero-badge">
                        <i class="fas fa-star"></i>
                        <span>Enterprise Ready</span>
                    </div>
                    <h1 class="hero-title">
                        Build Intelligent Applications with 
                        <span class="text-gradient">SmartRAG</span>
                    </h1>
                    <p class="hero-description">
                        The most powerful .NET library for document processing, AI embeddings, and semantic search. 
                        Transform your applications with enterprise-grade RAG capabilities.
                    </p>
                    <div class="hero-stats">
                        <div class="stat-item">
                            <div class="stat-number">5+</div>
                            <div class="stat-label">AI Providers</div>
                        </div>
                        <div class="stat-item">
                            <div class="stat-number">5+</div>
                            <div class="stat-label">Storage Options</div>
                        </div>
                        <div class="stat-item">
                            <div class="stat-number">100%</div>
                            <div class="stat-label">Open Source</div>
                        </div>
                    </div>
                    <div class="hero-buttons">
                        <a href="{{ site.baseurl }}/en/getting-started" class="btn btn-primary btn-lg">
                            <i class="fas fa-rocket"></i>
                            Get Started
                        </a>
                        <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-light btn-lg" target="_blank">
                            <i class="fab fa-github"></i>
                            View on GitHub
                        </a>
                    </div>
                </div>
            </div>
            <div class="col-lg-6">
                <div class="hero-visual">
                    <div class="code-window">
                        <div class="code-header">
                            <div class="code-dots">
                                <span></span>
                                <span></span>
                                <span></span>
                            </div>
                            <div class="code-title">SmartRAG.cs</div>
                        </div>
                        <div class="code-content">
                            <pre><code class="language-csharp">// Add SmartRAG to your project
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});

// Upload and process documents
var document = await documentService
    .UploadDocumentAsync(file);

// Perform semantic search
var results = await documentService
    .SearchAsync("your query");</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Features Section -->
<section class="features-section">
    <div class="container">
        <div class="section-header text-center">
            <h2 class="section-title">Why Choose SmartRAG?</h2>
            <p class="section-description">
                Everything you need to build intelligent applications with RAG capabilities
            </p>
        </div>
        
        <div class="row g-4">
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-brain"></i>
                    </div>
                    <h3>AI-Powered</h3>
                    <p>Integrate with leading AI providers including OpenAI, Anthropic, Gemini, and Azure OpenAI for powerful embeddings.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-file-alt"></i>
                    </div>
                    <h3>Multi-Format Support</h3>
                    <p>Process Word, PDF, Excel, and text documents with ease. Automatic format detection and text extraction.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-search"></i>
                    </div>
                    <h3>Semantic Search</h3>
                    <p>Advanced search capabilities with similarity scoring and intelligent result ranking for better user experience.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-database"></i>
                    </div>
                    <h3>Flexible Storage</h3>
                    <p>Multiple storage backends including Qdrant, Redis, SQLite, In-Memory, and File System for flexible deployment.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-rocket"></i>
                    </div>
                    <h3>Easy Integration</h3>
                    <p>Simple setup with dependency injection. Get started in minutes with our comprehensive documentation.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-shield-alt"></i>
                    </div>
                    <h3>Production Ready</h3>
                    <p>Built for enterprise environments with performance, scalability, and reliability in mind.</p>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Providers Section -->
<section class="providers-section">
    <div class="container">
        <div class="section-header text-center">
            <h2 class="section-title">Supported Technologies</h2>
            <p class="section-description">
                Integrate with leading AI providers and storage solutions
            </p>
        </div>
        
        <div class="providers-grid">
            <div class="provider-category">
                <h3>AI Providers</h3>
                <div class="provider-cards">
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fab fa-google"></i>
                        </div>
                        <h4>Gemini</h4>
                        <p>Google AI</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-brain"></i>
                        </div>
                        <h4>OpenAI</h4>
                        <p>GPT Models</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-cloud"></i>
                        </div>
                        <h4>Azure OpenAI</h4>
                        <p>Enterprise</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-robot"></i>
                        </div>
                        <h4>Anthropic</h4>
                        <p>Claude Models</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-cogs"></i>
                        </div>
                        <h4>Custom</h4>
                        <p>Extensible</p>
                    </div>
                </div>
            </div>
            
            <div class="provider-category">
                <h3>Storage Providers</h3>
                <div class="provider-cards">
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-cube"></i>
                        </div>
                        <h4>Qdrant</h4>
                        <p>Vector Database</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-database"></i>
                        </div>
                        <h4>Redis</h4>
                        <p>In-Memory Cache</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-hdd"></i>
                        </div>
                        <h4>SQLite</h4>
                        <p>Local Database</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-microchip"></i>
                        </div>
                        <h4>In-Memory</h4>
                        <p>Fast Development</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-folder-open"></i>
                        </div>
                        <h4>File System</h4>
                        <p>Local Storage</p>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Quick Start Section -->
<section class="quick-start-section">
    <div class="container">
        <div class="row align-items-center">
            <div class="col-lg-6">
                <div class="quick-start-content">
                    <h2>Get Started in Minutes</h2>
                    <p>SmartRAG is designed to be simple and powerful. Follow these steps to integrate it into your .NET application.</p>
                    
                    <div class="steps">
                        <div class="step">
                            <div class="step-number">1</div>
                            <div class="step-content">
                                <h4>Install Package</h4>
                                <p>Add SmartRAG to your project via NuGet</p>
                            </div>
                        </div>
                        <div class="step">
                            <div class="step-number">2</div>
                            <div class="step-content">
                                <h4>Configure Services</h4>
                                <p>Set up your AI and storage providers</p>
                            </div>
                        </div>
                        <div class="step">
                            <div class="step-number">3</div>
                            <div class="step-content">
                                <h4>Start Building</h4>
                                <p>Upload documents and perform searches</p>
                            </div>
                        </div>
                    </div>
                    
                    <a href="{{ site.baseurl }}/en/getting-started" class="btn btn-primary btn-lg">
                        <i class="fas fa-play"></i>
                        Start Building
                    </a>
                </div>
            </div>
            <div class="col-lg-6">
                <div class="code-example">
                    <div class="code-tabs">
                        <button class="code-tab active" data-tab="install">Install</button>
                        <button class="code-tab" data-tab="configure">Configure</button>
                        <button class="code-tab" data-tab="use">Use</button>
                    </div>
                    <div class="code-content">
                        <div class="code-panel active" id="install">
                            <pre><code class="language-bash"># Install via Package Manager
Install-Package SmartRAG

# Or via .NET CLI
dotnet add package SmartRAG</code></pre>
                        </div>
                        <div class="code-panel" id="configure">
                            <pre><code class="language-csharp">// Program.cs
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});</code></pre>
                        </div>
                        <div class="code-panel" id="use">
                            <pre><code class="language-csharp">// Use the service
var documentService = serviceProvider
    .GetRequiredService&lt;IDocumentService&gt;();

var document = await documentService
    .UploadDocumentAsync(file);

var results = await documentService
    .SearchAsync("your query");</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Documentation Section -->
<section class="documentation-section">
    <div class="container">
        <div class="section-header text-center">
            <h2 class="section-title">Documentation</h2>
            <p class="section-description">
                Everything you need to build with SmartRAG
            </p>
        </div>
        
        <div class="row g-4">
            <div class="col-lg-3 col-md-6">
                <a href="{{ site.baseurl }}/en/getting-started" class="doc-card">
                    <div class="doc-icon">
                        <i class="fas fa-rocket"></i>
                    </div>
                    <h3>Getting Started</h3>
                    <p>Quick installation and setup guide</p>
                </a>
            </div>
            <div class="col-lg-3 col-md-6">
                <a href="{{ site.baseurl }}/en/configuration" class="doc-card">
                    <div class="doc-icon">
                        <i class="fas fa-cog"></i>
                    </div>
                    <h3>Configuration</h3>
                    <p>Detailed configuration options</p>
                </a>
            </div>
            <div class="col-lg-3 col-md-6">
                <a href="{{ site.baseurl }}/en/api-reference" class="doc-card">
                    <div class="doc-icon">
                        <i class="fas fa-code"></i>
                    </div>
                    <h3>API Reference</h3>
                    <p>Complete API documentation</p>
                </a>
            </div>
            <div class="col-lg-3 col-md-6">
                <a href="{{ site.baseurl }}/en/examples" class="doc-card">
                    <div class="doc-icon">
                        <i class="fas fa-lightbulb"></i>
                    </div>
                    <h3>Examples</h3>
                    <p>Real-world examples and samples</p>
                </a>
            </div>
        </div>
    </div>
</section>

<!-- CTA Section -->
<section class="cta-section">
    <div class="container">
        <div class="cta-content text-center">
            <h2>Ready to Build Something Amazing?</h2>
            <p>Join thousands of developers using SmartRAG to build intelligent applications</p>
            <div class="cta-buttons">
                <a href="{{ site.baseurl }}/en/getting-started" class="btn btn-primary btn-lg">
                    <i class="fas fa-rocket"></i>
                    Get Started Now
                </a>
                <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-light btn-lg" target="_blank">
                    <i class="fab fa-github"></i>
                    Star on GitHub
                </a>
            </div>
        </div>
    </div>
</section>