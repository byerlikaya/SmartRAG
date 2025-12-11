---
layout: default
title: SmartRAG Documentation
description: Enterprise-Grade RAG Library for .NET - Multi-Database + Multi-Modal Intelligence Platform
lang: en
hide_title: true
---

<section class="hero-section">
    <div class="hero-background"></div>
    <div class="container">
        <div class="row align-items-center">
            <div class="col-lg-6">
                <div class="hero-content">
                    <div class="hero-badge">
                        <i class="fas fa-star"></i>
                        <span>.NET Standard 2.1</span>
                    </div>
                    <div class="hero-premise-badge">
                        <i class="fas fa-cloud-upload-alt"></i>
                        <span>100% On-Premise • Cloud • Hybrid</span>
                    </div>
                    <h1 class="hero-title">
                        <span class="text-gradient">SmartRAG</span> - Ask Questions About Your Data
                    </h1>
                    <p class="hero-subtitle">
                        Turn your documents, databases, images and audio into a conversational AI system.
                    </p>
                    <div class="hero-stats">
                        <div class="card card-sm">
                            <div class="stat-number">5</div>
                            <div class="stat-label">AI Providers</div>
                        </div>
                        <div class="card card-sm">
                            <div class="stat-number">4</div>
                            <div class="stat-label">Database Types</div>
                        </div>
                        <div class="card card-sm">
                            <div class="stat-number">7</div>
                            <div class="stat-label">Document Formats</div>
                        </div>
                    </div>
                    <div class="hero-buttons">
                        <a href="{{ site.baseurl }}/en/getting-started" class="btn btn-primary btn-lg">
                            <i class="fas fa-rocket"></i>
                            Get Started
                        </a>
                        <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-primary btn-lg" target="_blank">
                            <i class="fab fa-github"></i>
                            GitHub
                        </a>
                        <a href="https://www.nuget.org/packages/SmartRAG" class="btn btn-secondary btn-lg" target="_blank">
                            <i class="fas fa-box"></i>
                            NuGet
                        </a>
                    </div>
                </div>
            </div>
            <div class="col-lg-6">
                <div class="code-window fade-in-up">
                    <div class="code-header">
                        <div class="code-dots">
                            <span></span>
                            <span></span>
                            <span></span>
                        </div>
                        <div class="code-title">QuickStart.cs</div>
                    </div>
                    <div class="code-content">
                        <pre><code class="language-csharp">// Add SmartRAG to your .NET project
builder.Services.AddSmartRag(builder.Configuration, options =>
{
    options.StorageProvider = StorageProvider.InMemory;
    options.AIProvider = AIProvider.Gemini;
});

// Upload documents (PDF, Word, Excel, Images, Audio)
var document = await documentService.UploadDocumentAsync(
    fileStream, "contract.pdf", "application/pdf", "user-id"
);

// Ask questions with AI-powered intelligence
var answer = await searchService.QueryIntelligenceAsync(
    "What are the main benefits mentioned?", 
    maxResults: 5
);

Console.WriteLine(answer.Answer);
// AI analyzes your documents and provides intelligent answers</code></pre>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<section class="section section-light">
    <div class="container">
        <div class="section-header">
            <h2 class="section-title">Key Features</h2>
            <p class="section-subtitle">
                Powerful capabilities for building intelligent enterprise applications
                     </p>
        </div>
        
        <div class="grid grid-auto">
            <div class="card card-accent">
                <div class="icon icon-lg icon-gradient">
                    <i class="fas fa-database"></i>
                </div>
                <h3>Multi-Database RAG</h3>
                <p>Query multiple database types simultaneously - SQL Server, MySQL, PostgreSQL, SQLite. AI-powered cross-database joins and intelligent query coordination.</p>
            </div>
            
            <div class="card card-accent">
                <div class="icon icon-lg icon-gradient">
                    <i class="fas fa-layer-group"></i>
                </div>
                <h3>Multi-Modal Intelligence</h3>
                <p>Process PDF, Excel, Word documents, Images (OCR), Audio files (Speech-to-Text), and Databases - all unified in a single intelligent platform.</p>
            </div>
            
            <div class="card card-accent">
                <div class="icon icon-lg icon-gradient">
                    <i class="fas fa-shield-alt"></i>
                </div>
                <h3>On-Premise & Local AI</h3>
                <p>100% local operation with Ollama, LM Studio support. GDPR/KVKK/HIPAA compliant. Your data never leaves your infrastructure.</p>
            </div>
            
            <div class="card card-accent">
                <div class="icon icon-lg icon-gradient">
                    <i class="fas fa-comments"></i>
                </div>
                <h3>Conversation History</h3>
                <p>Automatic session-based conversation management with context awareness. AI remembers previous questions for natural interactions.</p>
            </div>
            
            <div class="card card-accent">
                <div class="icon icon-lg icon-gradient">
                    <i class="fas fa-search"></i>
                </div>
                <h3>Advanced Semantic Search</h3>
                <p>Hybrid scoring system (80% semantic + 20% keyword) with context awareness and intelligent ranking for superior search results.</p>
            </div>
            
            <div class="card card-accent">
                <div class="icon icon-lg icon-gradient">
                    <i class="fas fa-plug"></i>
                </div>
                <h3>MCP Client Integration</h3>
                <p>Connect to external MCP (Model Context Protocol) servers to extend capabilities with external tools and data sources.</p>
            </div>
            
            <div class="card card-accent">
                <div class="icon icon-lg icon-gradient">
                    <i class="fas fa-folder-open"></i>
                </div>
                <h3>Automatic File Watching</h3>
                <p>Monitor folders for new documents and automatically index them without manual uploads. Real-time document processing with duplicate detection.</p>
            </div>
            
            <div class="card card-accent">
                <div class="icon icon-lg icon-gradient">
                    <i class="fas fa-route"></i>
                </div>
                <h3>Smart Query Intent</h3>
                <p>Automatically routes queries to chat or document search based on intent detection. Language-agnostic design works globally.</p>
            </div>
            
            <div class="card card-accent">
                <div class="icon icon-lg icon-gradient">
                    <i class="fas fa-lock"></i>
                </div>
                <h3>Enterprise Security</h3>
                <p>Automatic sensitive data sanitization, encryption support, configurable data protection, and compliance-ready deployments.</p>
            </div>
            
            <div class="card card-accent">
                <div class="icon icon-lg icon-gradient">
                    <i class="fas fa-check-circle"></i>
                </div>
                <h3>Production Ready</h3>
                <p>Zero warnings policy, SOLID/DRY principles, comprehensive error handling, thread-safe operations, and enterprise-grade quality.</p>
            </div>
        </div>
    </div>
</section>

<section class="section section-dark">
    <div class="container">
        <div class="section-header">
            <h2 class="section-title">Supported Technologies</h2>
            <p class="section-subtitle">
                Integrate with leading AI providers, storage solutions, and databases
            </p>
        </div>
        
        <div class="grid grid-2" style="gap: 3rem;">
            <div>
                <h3 class="text-center mb-4">AI Providers</h3>
                <div class="grid grid-auto">
                    <div class="card card-sm">
                        <div class="icon icon-md">
                            <i class="fas fa-brain"></i>
                        </div>
                        <h4>OpenAI</h4>
                        <p>GPT Models</p>
                    </div>
                    <div class="card card-sm">
                        <div class="icon icon-md">
                            <i class="fas fa-robot"></i>
                        </div>
                        <h4>Anthropic</h4>
                        <p>Claude Models</p>
                    </div>
                    <div class="card card-sm">
                        <div class="icon icon-md">
                            <i class="fab fa-google"></i>
                        </div>
                        <h4>Gemini</h4>
                        <p>Google AI Models</p>
                    </div>
                    <div class="card card-sm">
                        <div class="icon icon-md">
                            <i class="fas fa-cloud"></i>
                        </div>
                        <h4>Azure OpenAI</h4>
                        <p>Enterprise Models</p>
                    </div>
                    <div class="card card-sm">
                        <div class="icon icon-md">
                            <i class="fas fa-server"></i>
                        </div>
                        <h4>Custom</h4>
                        <p>Ollama / LM Studio</p>
                    </div>
                </div>
            </div>
            
            <div>
                <h3 class="text-center mb-4">Storage Providers</h3>
                <div class="grid grid-auto">
                    <div class="card card-sm">
                        <div class="icon icon-md">
                            <i class="fas fa-cube"></i>
                        </div>
                        <h4>Qdrant</h4>
                        <p>Vector Database</p>
                    </div>
                    <div class="card card-sm">
                        <div class="icon icon-md">
                            <i class="fas fa-bolt"></i>
                        </div>
                        <h4>Redis</h4>
                        <p>High-Performance Cache</p>
                    </div>
                    <div class="card card-sm">
                        <div class="icon icon-md">
                            <i class="fas fa-memory"></i>
                        </div>
                        <h4>InMemory</h4>
                        <p>In-Memory Storage</p>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<section class="section section-dark">
    <div class="container">
        <div class="section-header">
            <h2 class="section-title">Supported Database Types</h2>
            <p class="section-subtitle">
                Query multiple database types simultaneously with AI-powered coordination
            </p>
        </div>
        
        <div class="grid grid-auto">
            <div class="card card-sm">
                <div class="icon icon-md">
                    <i class="fas fa-database"></i>
                </div>
                <h4>SQL Server</h4>
                <p>Enterprise Database</p>
            </div>
            <div class="card card-sm">
                <div class="icon icon-md">
                    <i class="fas fa-leaf"></i>
                </div>
                <h4>MySQL</h4>
                <p>Open Source DB</p>
            </div>
            <div class="card card-sm">
                <div class="icon icon-md">
                    <i class="fas fa-database"></i>
                </div>
                <h4>PostgreSQL</h4>
                <p>Advanced DB</p>
            </div>
            <div class="card card-sm">
                <div class="icon icon-md">
                    <i class="fas fa-feather"></i>
                </div>
                <h4>SQLite</h4>
                <p>Embedded DB</p>
            </div>
        </div>
    </div>
</section>

<section class="section section-dark">
    <div class="container">
        <div class="section-header">
            <h2 class="section-title">Real-World Use Cases</h2>
            <p class="section-subtitle">
                See what you can build with SmartRAG's multi-database and multi-modal capabilities
            </p>
        </div>
        
        <div class="row g-4">
            <div class="col-lg-6">
                <details>
                    <summary>
                        <h4><i class="fas fa-hospital-alt me-2"></i> Medical Records Intelligence</h4>
                    </summary>
                    <div style="margin-top: 1rem;">
                        <p><strong>Challenge:</strong> Get complete patient history scattered across departments</p>
                        <p><strong>SmartRAG Solution:</strong></p>
                        <ul>
                            <li>PostgreSQL: Patient records, admissions, discharge summaries</li>
                            <li>Excel: Lab results from multiple labs</li>
                            <li>OCR: Scanned prescriptions and medical documents</li>
                            <li>Audio: Doctor's voice notes from appointments</li>
                        </ul>
                        <p><strong>Result:</strong> Complete patient timeline from 4 disconnected systems, saving hours of manual data gathering.</p>
                    </div>
                </details>
                </div>
            
            <div class="col-lg-6">
                <details>
                    <summary>
                        <h4><i class="fas fa-university me-2"></i> Banking Credit Evaluation</h4>
                    </summary>
                    <div style="margin-top: 1rem;">
                        <p><strong>Challenge:</strong> Evaluate customer financial profile for credit decisions</p>
                        <p><strong>SmartRAG Solution:</strong></p>
                        <ul>
                            <li>SQL Server: Transaction history (36 months)</li>
                            <li>MySQL: Credit card usage and spending patterns</li>
                            <li>PostgreSQL: Loans, mortgage, credit score history</li>
                            <li>SQLite: Branch visit history, customer interactions</li>
                            <li>OCR: Scanned income documents, tax returns</li>
                            <li>PDF: Account statements, investment portfolios</li>
                        </ul>
                        <p><strong>Result:</strong> 360° customer financial intelligence for comprehensive risk assessment.</p>
            </div>
                </details>
                    </div>
            
            <div class="col-lg-6">
                <details>
                    <summary>
                        <h4><i class="fas fa-balance-scale me-2"></i> Legal Precedent Discovery</h4>
                    </summary>
                    <div style="margin-top: 1rem;">
                        <p><strong>Challenge:</strong> Find winning strategies from years of case history</p>
                        <p><strong>SmartRAG Solution:</strong></p>
                        <ul>
                            <li>1,000+ PDF legal documents (cases, briefs, judgments)</li>
                            <li>SQL Server case database (outcomes, dates, judges)</li>
                            <li>OCR: Scanned court orders</li>
                        </ul>
                        <p><strong>Result:</strong> AI discovers winning legal patterns in minutes vs. weeks of manual research.</p>
                </div>
                </details>
            </div>
            
            <div class="col-lg-6">
                <details>
                    <summary>
                        <h4><i class="fas fa-boxes me-2"></i> Predictive Inventory Intelligence</h4>
                    </summary>
                    <div style="margin-top: 1rem;">
                        <p><strong>Challenge:</strong> Prevent stockouts before they happen</p>
                        <p><strong>SmartRAG Solution:</strong></p>
                        <ul>
                            <li>SQLite: Product catalog (10,000 SKUs)</li>
                            <li>SQL Server: Sales data (2M transactions/month)</li>
                            <li>MySQL: Warehouse inventory (real-time)</li>
                            <li>PostgreSQL: Supplier data (lead times)</li>
                        </ul>
                        <p><strong>Result:</strong> Cross-database predictive analytics preventing stockouts across entire supply chain.</p>
                    </div>
                </details>
                </div>
            
            <div class="col-lg-6">
                <details>
                    <summary>
                        <h4><i class="fas fa-industry me-2"></i> Manufacturing Root Cause Analysis</h4>
                    </summary>
                    <div style="margin-top: 1rem;">
                        <p><strong>Challenge:</strong> Find why production quality dropped</p>
                        <p><strong>SmartRAG Solution:</strong></p>
                        <ul>
                            <li>Excel: Production reports (5 lines, hourly data)</li>
                            <li>PostgreSQL: Sensor data (100K+ readings)</li>
                            <li>OCR: Quality control photos with inspector notes</li>
                            <li>PDF: Equipment maintenance logs</li>
                        </ul>
                        <p><strong>Result:</strong> AI correlates temperature anomalies across millions of data points to pinpoint exact root cause.</p>
            </div>
                </details>
                    </div>
            
            <div class="col-lg-6">
                <details>
                    <summary>
                        <h4><i class="fas fa-user-tie me-2"></i> AI Resume Screening</h4>
                    </summary>
                    <div style="margin-top: 1rem;">
                        <p><strong>Challenge:</strong> Find best candidates from 500+ applications</p>
                        <p><strong>SmartRAG Solution:</strong></p>
                        <ul>
                            <li>500+ Resume PDFs (multiple languages, formats)</li>
                            <li>SQL Server: Applicant database (skills, experience)</li>
                            <li>OCR: Scanned certificates (AWS, Azure, Cloud)</li>
                            <li>Audio: Video interview transcripts</li>
                        </ul>
                        <p><strong>Result:</strong> AI screens and ranks candidates across multiple data types in minutes.</p>
                </div>
                </details>
                </div>
            </div>

        <div class="text-center mt-5">
            <a href="{{ site.baseurl }}/en/examples/quick" class="btn btn-primary btn-lg">
                <i class="fas fa-lightbulb"></i>
                Explore More Examples
            </a>
        </div>
    </div>
</section>

<section class="section section-light">
    <div class="container">
        <div class="section-header">
            <h2 class="section-title">Why Choose SmartRAG?</h2>
        </div>
        
        <div class="row g-4">
            <div class="col-lg-3 col-md-6">
                <div class="card card-accent text-center">
                    <div class="icon icon-lg icon-gradient mx-auto">
                        <i class="fas fa-database"></i>
                    </div>
                    <h3>Multi-Database RAG</h3>
                    <p>Query SQL Server, MySQL, PostgreSQL, SQLite simultaneously with AI-powered coordination</p>
                    </div>
            </div>
            <div class="col-lg-3 col-md-6">
                <div class="card card-accent text-center">
                    <div class="icon icon-lg icon-gradient mx-auto">
                        <i class="fas fa-layer-group"></i>
                    </div>
                    <h3>Multi-Modal</h3>
                    <p>Unified intelligence across PDF, Excel, Word, Images, Audio, and Databases</p>
                    </div>
            </div>
            <div class="col-lg-3 col-md-6">
                <div class="card card-accent text-center">
                    <div class="icon icon-lg icon-gradient mx-auto">
                        <i class="fas fa-home"></i>
                    </div>
                    <h3>100% Local</h3>
                    <p>Complete on-premise deployment with Ollama/LM Studio for total data privacy</p>
                    </div>
            </div>
            <div class="col-lg-3 col-md-6">
                <div class="card card-accent text-center">
                    <div class="icon icon-lg icon-gradient mx-auto">
                        <i class="fas fa-globe"></i>
                    </div>
                    <h3>Language Agnostic</h3>
                    <p>Works in any language - Turkish, English, German, Russian, Chinese, Arabic</p>
                    </div>
            </div>
        </div>
    </div>
</section>

<section class="section section-dark">
    <div class="container text-center">
        <div class="section-header">
            <h2 class="section-title">Ready to Build Something Amazing?</h2>
            <p class="section-subtitle">
                Join developers building intelligent applications with SmartRAG
            </p>
        </div>
        
        <div class="hero-buttons">
                <a href="{{ site.baseurl }}/en/getting-started" class="btn btn-primary btn-lg">
                    <i class="fas fa-rocket"></i>
                    Get Started Now
                </a>
            <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-primary btn-lg" target="_blank">
                    <i class="fab fa-github"></i>
                    GitHub
                </a>
            <a href="https://www.nuget.org/packages/SmartRAG" class="btn btn-secondary btn-lg" target="_blank">
                <i class="fas fa-download"></i>
                NuGet
            </a>
        </div>
    </div>
</section>

