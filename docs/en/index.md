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
                         The most powerful .NET library for document processing and AI-powered conversations. 
                         Upload your documents and chat with them using artificial intelligence.
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
 services.UseSmartRag(configuration,
     storageProvider: StorageProvider.InMemory,
     aiProvider: AIProvider.Gemini
 );

 // Upload and process documents
 var document = await documentService
     .UploadDocumentAsync(fileStream, fileName, contentType, "user123");

 // Chat with your documents using AI
 var answer = await documentSearchService
     .GenerateRagAnswerAsync("What is this document about?", maxResults: 5);</code></pre>
                         </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Quick Start Section -->
<section class="quick-start-section">
    <div class="container">
        <div class="section-header text-center">
            <h2 class="section-title">Get Started in Minutes</h2>
                                 <p class="section-description">
                         Follow these simple steps to upload documents and chat with them using AI
                     </p>
        </div>
        
        <div class="row g-4 mb-5">
            <div class="col-lg-6">
                <div class="steps">
                    <div class="step">
                        <div class="step-number">1</div>
                                                 <div class="step-content">
                             <h4>Install Package</h4>
                             <p>Add SmartRAG NuGet package to your project</p>
                         </div>
                     </div>
                     <div class="step">
                         <div class="step-number">2</div>
                         <div class="step-content">
                             <h4>Configure Services</h4>
                             <p>Set up AI and storage providers in your startup</p>
                         </div>
                     </div>
                     <div class="step">
                         <div class="step-number">3</div>
                         <div class="step-content">
                             <h4>Start Building</h4>
                             <p>Upload documents and chat with them using AI</p>
                         </div>
                    </div>
                </div>
            </div>
            <div class="col-lg-6">
                <div class="code-example">
                    <div class="code-tabs">
                        <button class="code-tab active" data-tab="setup">Setup</button>
                        <button class="code-tab" data-tab="usage">Usage</button>
                        <button class="code-tab" data-tab="search">Search</button>
                    </div>
                                         <div class="code-panel active" id="setup">
                         <pre><code class="language-csharp">// Program.cs
 services.UseSmartRag(configuration,
     storageProvider: StorageProvider.InMemory,
     aiProvider: AIProvider.Gemini
 );

 // Or with custom options
 services.AddSmartRag(configuration, options =>
 {
     options.AIProvider = AIProvider.Anthropic;
     options.StorageProvider = StorageProvider.Qdrant;
     options.MaxChunkSize = 1000;
     options.ChunkOverlap = 200;
 });</code></pre>
                     </div>
                     <div class="code-panel" id="usage">
                         <pre><code class="language-csharp">// Upload and process document
 var document = await documentService
     .UploadDocumentAsync(fileStream, fileName, contentType, "user123");

 // Document is automatically processed, chunked, and indexed
 // Ready for AI-powered conversations</code></pre>
                     </div>
                     <div class="code-panel" id="search">
                         <pre><code class="language-csharp">// Ask questions about your documents
 var ragResponse = await documentSearchService
     .GenerateRagAnswerAsync("What are the main topics discussed?", maxResults: 5);

 // Get AI-generated answer based on document content
 Console.WriteLine(ragResponse.Answer);

 // Or search for specific information
 var results = await documentSearchService
     .SearchDocumentsAsync("machine learning algorithms", maxResults: 3);</code></pre>
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
            <h2 class="section-title">Key Features</h2>
            <p class="section-description">
                Powerful capabilities for building intelligent applications
            </p>
        </div>
        
        <div class="row g-4">
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-brain"></i>
                    </div>
                    <h3>AI-Powered</h3>
                    <p>Integrate with leading AI providers for powerful embeddings and intelligent processing.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-file-alt"></i>
                    </div>
                    <h3>Multi-Format Support</h3>
                    <p>Process Word, PDF, Excel, and text documents with automatic format detection.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-search"></i>
                    </div>
                    <h3>Enhanced Semantic Search</h3>
                    <p>Hybrid scoring (80% semantic + 20% keyword) with context awareness and intelligent ranking.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-database"></i>
                    </div>
                    <h3>Flexible Storage</h3>
                    <p>Multiple storage backends for flexible deployment options.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-rocket"></i>
                    </div>
                    <h3>Easy Integration</h3>
                    <p>Simple setup with dependency injection. Get started in minutes.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-magic"></i>
                    </div>
                    <h3>Smart Query Intent</h3>
                    <p>Automatically routes queries to chat or document search based on intent detection.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-shield-alt"></i>
                    </div>
                    <h3>Production Ready</h3>
                    <p>Built for enterprise environments with performance and reliability.</p>
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
                    <p>Configure SmartRAG for your needs</p>
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
                         <p>Join thousands of developers using SmartRAG to chat with their documents using AI</p>
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