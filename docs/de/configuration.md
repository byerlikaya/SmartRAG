---
layout: default
title: Konfiguration
description: Konfigurieren Sie SmartRAG mit Ihren bevorzugten KI- und Speicheranbietern
lang: de
---

<div class="page-content">
    <div class="container">
        <!-- Basic Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Grundkonfiguration</h2>
                    <p>SmartRAG kann mit verschiedenen Optionen konfiguriert werden, um Ihren Bedürfnissen zu entsprechen:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
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
                                    <td><code>MaxChunkSize</code></td>
                                    <td><code>int</code></td>
                                    <td>1000</td>
                                    <td>Maximale Größe der Dokumentenabschnitte</td>
                                </tr>
                                <tr>
                                    <td><code>MinChunkSize</code></td>
                                    <td><code>int</code></td>
                                    <td>50</td>
                                    <td>Minimale Größe der Dokumentenabschnitte</td>
                                </tr>
                                <tr>
                                    <td><code>ChunkOverlap</code></td>
                                    <td><code>int</code></td>
                                    <td>200</td>
                                    <td>Überlappung zwischen Abschnitten</td>
                                </tr>
                                <tr>
                                    <td><code>MaxRetryAttempts</code></td>
                                    <td><code>int</code></td>
                                    <td>3</td>
                                    <td>Maximale Anzahl von Wiederholungsversuchen</td>
                                </tr>
                                <tr>
                                    <td><code>RetryDelayMs</code></td>
                                    <td><code>int</code></td>
                                    <td>1000</td>
                                    <td>Verzögerung zwischen Wiederholungsversuchen (ms)</td>
                                </tr>
                                <tr>
                                    <td><code>RetryPolicy</code></td>
                                    <td><code>RetryPolicy</code></td>
                                    <td><code>ExponentialBackoff</code></td>
                                    <td>Wiederholungsrichtlinie</td>
                                </tr>
                                <tr>
                                    <td><code>EnableFallbackProviders</code></td>
                                    <td><code>bool</code></td>
                                    <td>false</td>
                                    <td>Fallback-Anbieter aktivieren</td>
                                </tr>
                                <tr>
                                    <td><code>FallbackProviders</code></td>
                                    <td><code>AIProvider[]</code></td>
                                    <td>[]</td>
                                    <td>Liste der Fallback-KI-Anbieter</td>
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
                    
                    <div class="code-example">
                        <div class="code-tabs">
                            <button class="code-tab active" data-tab="ai-anthropic">Anthropic</button>
                            <button class="code-tab" data-tab="ai-openai">OpenAI</button>
                            <button class="code-tab" data-tab="ai-gemini">Gemini</button>
                        </div>
                        
                                                 <div class="code-panel active" data-tab="ai-anthropic">
                             <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "Anthropic": {
    "ApiKey": "your-anthropic-key",
    "Model": "claude-3-sonnet-20240229"
  }
}</code></pre>
                         </div>
                         
                         <div class="code-panel" data-tab="ai-openai">
                             <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "OpenAI": {
    "ApiKey": "your-openai-key",
    "Model": "gpt-4",
    "EmbeddingModel": "text-embedding-ada-002"
  }
}</code></pre>
                         </div>
                         
                         <div class="code-panel" data-tab="ai-gemini">
                             <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Gemini;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "Gemini": {
    "ApiKey": "your-gemini-key",
    "Model": "gemini-pro",
    "EmbeddingModel": "embedding-001"
  }
}</code></pre>
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
                    
                    <div class="code-example">
                        <div class="code-tabs">
                            <button class="code-tab active" data-tab="storage-qdrant">Qdrant</button>
                            <button class="code-tab" data-tab="storage-memory">In-Memory</button>
                        </div>
                        
                                                 <div class="code-panel active" data-tab="storage-qdrant">
                             <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "Storage": {
    "Qdrant": {
      "Host": "localhost",
      "ApiKey": "your-qdrant-key",
      "CollectionName": "smartrag_documents",
      "VectorSize": 768
    }
  }
}</code></pre>
                         </div>
                         
                         <div class="code-panel" data-tab="storage-memory">
                             <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.InMemory;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});
// Keine zusätzliche Konfiguration erforderlich</code></pre>
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
                        <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.MaxChunkSize = 500;
    options.MinChunkSize = 50;
    options.ChunkOverlap = 100;
});</code></pre>
                    </div>
                    
                    <h3>Wiederholungsversuche-Konfiguration</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new[] { AIProvider.Gemini, AIProvider.OpenAI };
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
   "Anthropic": {
     "ApiKey": "your-anthropic-key",
     "Model": "claude-3-sonnet-20240229"
   },
   "Storage": {
     "Qdrant": {
       "Host": "localhost",
       "ApiKey": "your-qdrant-key",
       "CollectionName": "smartrag_documents"
     }
   }
 }</code></pre>
                     </div>
                     
                     <h3>Umgebungsvariablen</h3>
                     <div class="code-example">
                         <pre><code class="language-bash">export ANTHROPIC_API_KEY=your-anthropic-key
export QDRANT_API_KEY=your-qdrant-key</code></pre>
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