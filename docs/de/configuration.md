---
layout: default
title: Konfiguration
description: Konfigurieren Sie SmartRAG für Ihre spezifischen Bedürfnisse
lang: de
---

<div class="page-header">
    <div class="container">
        <div class="row">
            <div class="col-lg-8 mx-auto text-center">
                <h1 class="page-title">Konfiguration</h1>
                <p class="page-description">
                    Konfigurieren Sie SmartRAG für Ihre spezifischen Bedürfnisse
                </p>
            </div>
        </div>
    </div>
</div>

<div class="page-content">
    <div class="container">
        <!-- Basic Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Grundkonfiguration</h2>
                    <p>SmartRAG kann mit verschiedenen Optionen konfiguriert werden, um Ihren Bedürfnissen zu entsprechen:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});</code></pre>
                    </div>

                    <h3>Konfigurationsoptionen</h3>
                    <div class="table-responsive">
                        <table class="table table-striped">
                            <thead>
                                <tr>
                                    <th>Option</th>
                                    <th>Typ</th>
                                    <th>Standard</th>
                                    <th>Beschreibung</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td><code>AIProvider</code></td>
                                    <td><code>AIProvider</code></td>
                                    <td><code>Anthropic</code></td>
                                    <td>Der KI-Anbieter für Embeddings</td>
                                </tr>
                                <tr>
                                    <td><code>StorageProvider</code></td>
                                    <td><code>StorageProvider</code></td>
                                    <td><code>Qdrant</code></td>
                                    <td>Der Speicheranbieter für Vektoren</td>
                                </tr>
                                <tr>
                                    <td><code>ApiKey</code></td>
                                    <td><code>string</code></td>
                                    <td>Erforderlich</td>
                                    <td>Ihr API-Schlüssel für den KI-Anbieter</td>
                                </tr>
                                <tr>
                                    <td><code>ModelName</code></td>
                                    <td><code>string</code></td>
                                    <td>Anbieter-Standard</td>
                                    <td>Das spezifische zu verwendende Modell</td>
                                </tr>
                                <tr>
                                    <td><code>ChunkSize</code></td>
                                    <td><code>int</code></td>
                                    <td>1000</td>
                                    <td>Größe der Dokumentenabschnitte</td>
                                </tr>
                                <tr>
                                    <td><code>ChunkOverlap</code></td>
                                    <td><code>int</code></td>
                                    <td>200</td>
                                    <td>Überlappung zwischen Abschnitten</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </section>

        <!-- AI Providers Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>KI-Anbieter-Konfiguration</h2>
                    <p>Wählen Sie aus mehreren KI-Anbietern für die Embedding-Generierung:</p>
                    
                    <div class="provider-tabs">
                        <div class="provider-tab active" data-tab="anthropic">Anthropic</div>
                        <div class="provider-tab" data-tab="openai">OpenAI</div>
                        <div class="provider-tab" data-tab="azure">Azure OpenAI</div>
                        <div class="provider-tab" data-tab="gemini">Gemini</div>
                        <div class="provider-tab" data-tab="custom">Benutzerdefiniert</div>
                    </div>
                    
                    <div class="provider-content">
                        <div class="provider-panel active" id="anthropic">
                            <h3>Anthropic (Claude)</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.ApiKey = "your-anthropic-key";
    options.ModelName = "claude-3-sonnet-20240229";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="provider-panel" id="openai">
                            <h3>OpenAI</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.ApiKey = "your-openai-key";
    options.ModelName = "text-embedding-ada-002";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="provider-panel" id="azure">
                            <h3>Azure OpenAI</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.AzureOpenAI;
    options.ApiKey = "your-azure-key";
    options.Endpoint = "https://your-resource.openai.azure.com/";
    options.ModelName = "text-embedding-ada-002";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="provider-panel" id="gemini">
                            <h3>Google Gemini</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Gemini;
    options.ApiKey = "your-gemini-key";
    options.ModelName = "embedding-001";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="provider-panel" id="custom">
                            <h3>Benutzerdefinierter KI-Anbieter</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Custom;
    options.CustomEndpoint = "https://your-custom-api.com/v1/embeddings";
    options.ApiKey = "your-custom-key";
    options.ModelName = "your-custom-model";
});</code></pre>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!-- Storage Providers Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Speicher-Anbieter-Konfiguration</h2>
                    <p>Wählen Sie das Speicher-Backend, das am besten zu Ihren Bedürfnissen passt:</p>
                    
                    <div class="storage-tabs">
                        <div class="storage-tab active" data-tab="qdrant">Qdrant</div>
                        <div class="storage-tab" data-tab="redis">Redis</div>
                        <div class="storage-tab" data-tab="sqlite">SQLite</div>
                        <div class="storage-tab" data-tab="memory">In-Memory</div>
                        <div class="storage-tab" data-tab="filesystem">Dateisystem</div>
                    </div>
                    
                    <div class="storage-content">
                        <div class="storage-panel active" id="qdrant">
                            <h3>Qdrant (Vektor-Datenbank)</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.Qdrant;
    options.QdrantUrl = "http://localhost:6333";
    options.CollectionName = "smartrag_documents";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="storage-panel" id="redis">
                            <h3>Redis (In-Memory-Cache)</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.Redis;
    options.RedisConnectionString = "localhost:6379";
    options.DatabaseId = 0;
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="storage-panel" id="sqlite">
                            <h3>SQLite (Lokale Datenbank)</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.Sqlite;
    options.ConnectionString = "Data Source=smartrag.db";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="storage-panel" id="memory">
                            <h3>In-Memory (Entwicklung)</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.InMemory;
    // Keine zusätzliche Konfiguration erforderlich
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="storage-panel" id="filesystem">
                            <h3>Dateisystem (Lokaler Speicher)</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.FileSystem;
    options.StoragePath = "./data/smartrag";
});</code></pre>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!-- Advanced Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Erweiterte Konfiguration</h2>
                    <p>Feinabstimmung von SmartRAG für Ihre spezifischen Anforderungen:</p>
                    
                    <h3>Benutzerdefinierte Chunking</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.ChunkSize = 500;
    options.ChunkOverlap = 100;
    options.ChunkingStrategy = ChunkingStrategy.Sentence;
});</code></pre>
                    </div>
                    
                    <h3>Dokumentenverarbeitung</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.SupportedFormats = new[] { ".pdf", ".docx", ".txt" };
    options.MaxFileSize = 10 * 1024 * 1024; // 10MB
    options.EnableTextExtraction = true;
});</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Environment Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Umgebungskonfiguration</h2>
                    <p>Konfigurieren Sie SmartRAG mit Umgebungsvariablen oder Konfigurationsdateien:</p>
                    
                    <h3>appsettings.json</h3>
                    <div class="code-example">
                        <pre><code class="language-json">{
  "SmartRAG": {
    "AIProvider": "Anthropic",
    "StorageProvider": "Qdrant",
    "ApiKey": "your-api-key",
    "ChunkSize": 1000,
    "ChunkOverlap": 200
  }
}</code></pre>
                    </div>
                    
                    <h3>Umgebungsvariablen</h3>
                    <div class="code-example">
                        <pre><code class="language-bash">export SMARTRAG_AI_PROVIDER=Anthropic
export SMARTRAG_STORAGE_PROVIDER=Qdrant
export SMARTRAG_API_KEY=your-api-key</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Best Practices Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Bewährte Praktiken</h2>
                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="alert alert-warning">
                                <h4><i class="fas fa-key me-2"></i>API-Schlüssel</h4>
                                <p class="mb-0">Kodieren Sie API-Schlüssel niemals im Quellcode. Verwenden Sie Umgebungsvariablen oder sichere Konfiguration.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-info">
                                <h4><i class="fas fa-balance-scale me-2"></i>Chunk-Größe</h4>
                                <p class="mb-0">Balancieren Sie zwischen Kontext und Leistung. Kleinere Chunks für Präzision, größere für Kontext.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-success">
                                <h4><i class="fas fa-database me-2"></i>Speicher</h4>
                                <p class="mb-0">Wählen Sie den Speicheranbieter basierend auf Ihrer Skala und Ihren Anforderungen.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-primary">
                                <h4><i class="fas fa-shield-alt me-2"></i>Sicherheit</h4>
                                <p class="mb-0">Verwenden Sie angemessene Zugriffskontrollen und Überwachung für Produktionsumgebungen.</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>