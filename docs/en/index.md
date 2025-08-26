---
layout: default
title: SmartRAG Documentation
description: Enterprise-grade RAG library for .NET applications
lang: en
hide_title: true
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
            <div class="col-lg-6 d-none d-lg-block">
                <div class="hero-image">
                    <img src="{{ site.baseurl }}/assets/images/logo.svg" alt="SmartRAG Logo" class="img-fluid">
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Features Section -->
<section class="features-section py-5">
    <div class="container">
        <h2 class="section-title text-center mb-5">Key Features</h2>
        <p class="section-description text-center mb-5">Powerful capabilities for building intelligent applications</p>
        <div class="row text-center">
            <div class="col-md-4 mb-4">
                <div class="feature-item">
                    <i class="fas fa-robot feature-icon mb-3"></i>
                    <h3 class="feature-title">AI-Powered</h3>
                    <p class="feature-description">Integrate with leading AI providers for powerful embeddings and intelligent processing.</p>
                </div>
            </div>
            <div class="col-md-4 mb-4">
                <div class="feature-item">
                    <i class="fas fa-file-alt feature-icon mb-3"></i>
                    <h3 class="feature-title">Multi-Format Support</h3>
                    <p class="feature-description">Process Word, PDF, Excel, and text documents with automatic format detection.</p>
                </div>
            </div>
            <div class="col-md-4 mb-4">
                <div class="feature-item">
                    <i class="fas fa-search feature-icon mb-3"></i>
                    <h3 class="feature-title">Semantic Search</h3>
                    <p class="feature-description">Advanced search with similarity scoring and intelligent result ranking.</p>
                </div>
            </div>
            <div class="col-md-4 mb-4">
                <div class="feature-item">
                    <i class="fas fa-database feature-icon mb-3"></i>
                    <h3 class="feature-title">Flexible Storage</h3>
                    <p class="feature-description">Multiple storage backends for flexible deployment options.</p>
                </div>
            </div>
            <div class="col-md-4 mb-4">
                <div class="feature-item">
                    <i class="fas fa-plug feature-icon mb-3"></i>
                    <h3 class="feature-title">Easy Integration</h3>
                    <p class="feature-description">Simple setup with dependency injection. Get started in minutes.</p>
                </div>
            </div>
            <div class="col-md-4 mb-4">
                <div class="feature-item">
                    <i class="fas fa-shield-alt feature-icon mb-3"></i>
                    <h3 class="feature-title">Production Ready</h3>
                    <p class="feature-description">Built for enterprise environments with performance and reliability.</p>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Quick Start Section -->
<section class="quick-start-section bg-light py-5">
    <div class="container">
        <h2 class="section-title text-center mb-5">Get Started in Minutes</h2>
        <p class="section-description text-center mb-5">Simple and powerful integration for your .NET applications.</p>
        <div class="row justify-content-center">
            <div class="col-lg-8">
                <div class="code-example mb-4">
                    <div class="code-header">
                        <span class="code-title">SmartRAG.cs</span>
                        <button class="copy-code-btn" data-clipboard-target="#quickStartCode">
                            <i class="fas fa-copy"></i> Copy
                        </button>
                    </div>
                    <pre><code class="language-csharp" id="quickStartCode">// Add SmartRAG to your project
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
</section>

<!-- Providers Section -->
<section class="providers-section py-5">
    <div class="container">
        <h2 class="section-title text-center mb-5">Supported Technologies</h2>
        <p class="section-description text-center mb-5">Choose from leading AI providers and storage solutions</p>

        <h3 class="text-center mb-4">🤖 AI Providers</h3>
        <div class="row text-center justify-content-center mb-5">
            <div class="col-4 col-md-2 mb-4">
                <div class="provider-logo">
                    <i class="fab fa-google fa-3x text-primary"></i>
                    <p class="provider-name mt-2">Gemini</p>
                </div>
            </div>
            <div class="col-4 col-md-2 mb-4">
                <div class="provider-logo">
                    <i class="fas fa-robot fa-3x text-primary"></i>
                    <p class="provider-name mt-2">OpenAI</p>
                </div>
            </div>
            <div class="col-4 col-md-2 mb-4">
                <div class="provider-logo">
                    <i class="fab fa-microsoft fa-3x text-primary"></i>
                    <p class="provider-name mt-2">Azure OpenAI</p>
                </div>
            </div>
            <div class="col-4 col-md-2 mb-4">
                <div class="provider-logo">
                    <i class="fas fa-feather-alt fa-3x text-primary"></i>
                    <p class="provider-name mt-2">Anthropic</p>
                </div>
            </div>
            <div class="col-4 col-md-2 mb-4">
                <div class="provider-logo">
                    <i class="fas fa-cogs fa-3x text-primary"></i>
                    <p class="provider-name mt-2">Custom</p>
                </div>
            </div>
        </div>

        <h3 class="text-center mb-4">🗄️ Storage Providers</h3>
        <div class="row text-center justify-content-center">
            <div class="col-4 col-md-2 mb-4">
                <div class="provider-logo">
                    <i class="fas fa-cube fa-3x text-secondary"></i>
                    <p class="provider-name mt-2">Qdrant</p>
                </div>
            </div>
            <div class="col-4 col-md-2 mb-4">
                <div class="provider-logo">
                    <i class="fas fa-server fa-3x text-secondary"></i>
                    <p class="provider-name mt-2">Redis</p>
                </div>
            </div>
            <div class="col-4 col-md-2 mb-4">
                <div class="provider-logo">
                    <i class="fas fa-database fa-3x text-secondary"></i>
                    <p class="provider-name mt-2">SQLite</p>
                </div>
            </div>
            <div class="col-4 col-md-2 mb-4">
                <div class="provider-logo">
                    <i class="fas fa-memory fa-3x text-secondary"></i>
                    <p class="provider-name mt-2">In-Memory</p>
                </div>
            </div>
            <div class="col-4 col-md-2 mb-4">
                <div class="provider-logo">
                    <i class="fas fa-folder-open fa-3x text-secondary"></i>
                    <p class="provider-name mt-2">File System</p>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Documentation Section -->
<section class="documentation-section bg-light py-5">
    <div class="container">
        <h2 class="section-title text-center mb-5">Documentation</h2>
        <p class="section-description text-center mb-5">Everything you need to build with SmartRAG</p>
        <div class="row">
            <div class="col-md-4 mb-4">
                <div class="doc-card">
                    <h3 class="doc-title">Getting Started</h3>
                    <p class="doc-description">Quick installation and setup guide</p>
                    <a href="{{ site.baseurl }}/en/getting-started" class="btn btn-outline-primary btn-sm">Get Started</a>
                </div>
            </div>
            <div class="col-md-4 mb-4">
                <div class="doc-card">
                    <h3 class="doc-title">Configuration</h3>
                    <p class="doc-description">Detailed configuration options</p>
                    <a href="{{ site.baseurl }}/en/configuration" class="btn btn-outline-primary btn-sm">Configure</a>
                </div>
            </div>
            <div class="col-md-4 mb-4">
                <div class="doc-card">
                    <h3 class="doc-title">API Reference</h3>
                    <p class="doc-description">Complete API documentation</p>
                    <a href="{{ site.baseurl }}/en/api-reference" class="btn btn-outline-primary btn-sm">View API</a>
                </div>
            </div>
            <div class="col-md-4 mb-4">
                <div class="doc-card">
                    <h3 class="doc-title">Examples</h3>
                    <p class="doc-description">Real-world examples and samples</p>
                    <a href="{{ site.baseurl }}/en/examples" class="btn btn-outline-primary btn-sm">View Examples</a>
                </div>
            </div>
            <div class="col-md-4 mb-4">
                <div class="doc-card">
                    <h3 class="doc-title">Troubleshooting</h3>
                    <p class="doc-description">Common issues and solutions</p>
                    <a href="{{ site.baseurl }}/en/troubleshooting" class="btn btn-outline-primary btn-sm">Get Help</a>
                </div>
            </div>
            <div class="col-md-4 mb-4">
                <div class="doc-card">
                    <h3 class="doc-title">Changelog</h3>
                    <p class="doc-description">Track new features, improvements, and bug fixes across versions</p>
                    <a href="{{ site.baseurl }}/en/changelog" class="btn btn-outline-primary btn-sm">View Changes</a>
                </div>
            </div>
            <div class="col-md-4 mb-4">
                <div class="doc-card">
                    <h3 class="doc-title">Contributing</h3>
                    <p class="doc-description">Learn how to contribute to SmartRAG development</p>
                    <a href="{{ site.baseurl }}/en/contributing" class="btn btn-outline-primary btn-sm">Contribute</a>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Call to Action Section -->
<section class="cta-section py-5 text-center">
    <div class="container">
        <h2 class="section-title mb-4">Ready to Build Something Amazing?</h2>
        <p class="section-description lead mb-5">Join thousands of developers using SmartRAG to build intelligent applications</p>
        <div class="cta-buttons">
            <a href="{{ site.baseurl }}/en/getting-started" class="btn btn-primary btn-lg me-3">
                <i class="fas fa-rocket me-2"></i>Get Started Now
            </a>
            <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-light btn-lg" target="_blank" rel="noopener noreferrer">
                <i class="fab fa-github me-2"></i>Star on GitHub
            </a>
        </div>
    </div>
</section>
