---
layout: default
title: Yapılandırma
description: SmartRAG'i tercih ettiğiniz AI ve depolama sağlayıcıları ile yapılandırın
lang: tr
---

<div class="page-content">
    <div class="container">
        <!-- Basic Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Temel Yapılandırma</h2>
                    <p>SmartRAG ihtiyaçlarınıza uygun çeşitli seçeneklerle yapılandırılabilir:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});</code></pre>
                    </div>

                    <h3>Yapılandırma Seçenekleri</h3>
                    <div class="table-responsive">
                        <table class="table table-striped">
                            <thead>
                                <tr>
                                    <th>Seçenek</th>
                                    <th>Tip</th>
                                    <th>Varsayılan</th>
                                    <th>Açıklama</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td><code>AIProvider</code></td>
                                    <td><code>AIProvider</code></td>
                                    <td><code>Anthropic</code></td>
                                    <td>Embedding'ler için kullanılacak AI provider</td>
                                </tr>
                                <tr>
                                    <td><code>StorageProvider</code></td>
                                    <td><code>StorageProvider</code></td>
                                    <td><code>Qdrant</code></td>
                                    <td>Vektörler için depolama provider'ı</td>
                                </tr>
                                <tr>
                                    <td><code>MaxChunkSize</code></td>
                                    <td><code>int</code></td>
                                    <td>1000</td>
                                    <td>Belge parçalarının maksimum boyutu</td>
                                </tr>
                                <tr>
                                    <td><code>MinChunkSize</code></td>
                                    <td><code>int</code></td>
                                    <td>50</td>
                                    <td>Belge parçalarının minimum boyutu</td>
                                </tr>
                                <tr>
                                    <td><code>ChunkOverlap</code></td>
                                    <td><code>int</code></td>
                                    <td>200</td>
                                    <td>Parçalar arasındaki örtüşme</td>
                                </tr>
                                <tr>
                                    <td><code>MaxRetryAttempts</code></td>
                                    <td><code>int</code></td>
                                    <td>3</td>
                                    <td>Maksimum yeniden deneme sayısı</td>
                                </tr>
                                <tr>
                                    <td><code>RetryDelayMs</code></td>
                                    <td><code>int</code></td>
                                    <td>1000</td>
                                    <td>Yeniden deneme arasındaki gecikme (ms)</td>
                                </tr>
                                <tr>
                                    <td><code>RetryPolicy</code></td>
                                    <td><code>RetryPolicy</code></td>
                                    <td><code>ExponentialBackoff</code></td>
                                    <td>Yeniden deneme politikası</td>
                                </tr>
                                <tr>
                                    <td><code>EnableFallbackProviders</code></td>
                                    <td><code>bool</code></td>
                                    <td>false</td>
                                    <td>Yedek provider'ları etkinleştir</td>
                                </tr>
                                <tr>
                                    <td><code>FallbackProviders</code></td>
                                    <td><code>AIProvider[]</code></td>
                                    <td>[]</td>
                                    <td>Yedek AI provider'ları listesi</td>
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
                    <h2>AI Provider Yapılandırması</h2>
                    <p>Embedding üretimi için birden fazla AI provider arasından seçim yapın:</p>
                    
                    <div class="code-tabs">
                        <div class="code-tab active" data-tab="anthropic">Anthropic</div>
                        <div class="code-tab" data-tab="openai">OpenAI</div>
                        <div class="code-tab" data-tab="gemini">Gemini</div>
                    </div>
                    
                    <div class="code-content">
                        <div class="code-panel active" id="anthropic">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "Anthropic": {
    "ApiKey": "your-anthropic-key"
  }
}</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="openai">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "OpenAI": {
    "ApiKey": "your-openai-key"
  }
}</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="gemini">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Gemini;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "Gemini": {
    "ApiKey": "your-gemini-key"
  }
}</code></pre>
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
                    <h2>Depolama Provider Yapılandırması</h2>
                    <p>İhtiyaçlarınıza en uygun depolama backend'ini seçin:</p>
                    
                    <div class="code-tabs">
                        <div class="code-tab active" data-tab="qdrant">Qdrant</div>
                        <div class="code-tab" data-tab="memory">Bellek İçi</div>
                    </div>
                    
                    <div class="code-content">
                        <div class="code-panel active" id="qdrant">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "Qdrant": {
    "ApiKey": "your-qdrant-key"
  }
}</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="memory">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.InMemory;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});
// Ek yapılandırma gerekmez</code></pre>
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
                    <h2>Gelişmiş Yapılandırma</h2>
                    <p>SmartRAG'i özel gereksinimleriniz için ince ayar yapın:</p>
                    
                    <h3>Özel Parçalama</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.MaxChunkSize = 500;
    options.MinChunkSize = 50;
    options.ChunkOverlap = 100;
});</code></pre>
                    </div>
                    
                    <h3>Yeniden Deneme Yapılandırması</h3>
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
                    <h2>Ortam Yapılandırması</h2>
                    <p>Ortam değişkenleri veya yapılandırma dosyaları kullanarak SmartRAG'i yapılandırın:</p>
                    
                    <h3>appsettings.json</h3>
                    <div class="code-example">
                        <pre><code class="language-json">{
  "SmartRAG": {
    "AIProvider": "Anthropic",
    "StorageProvider": "Qdrant",
    "MaxChunkSize": 1000,
    "MinChunkSize": 50,
    "ChunkOverlap": 200,
    "MaxRetryAttempts": 3,
    "RetryDelayMs": 1000,
    "RetryPolicy": "ExponentialBackoff",
    "EnableFallbackProviders": false
  },
  "Anthropic": {
    "ApiKey": "your-anthropic-key"
  },
  "Qdrant": {
    "ApiKey": "your-qdrant-key"
  }
}</code></pre>
                    </div>
                    
                    <h3>Ortam Değişkenleri</h3>
                    <div class="code-example">
                        <pre><code class="language-bash">export SmartRAG__AIProvider=Anthropic
export SmartRAG__StorageProvider=Qdrant
export SmartRAG__MaxChunkSize=1000
export SmartRAG__ChunkOverlap=200
export ANTHROPIC_API_KEY=your-anthropic-key
export QDRANT_API_KEY=your-qdrant-key</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Best Practices Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>En İyi Uygulamalar</h2>
                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="alert alert-warning">
                                <h4><i class="fas fa-key me-2"></i>API Anahtarları</h4>
                                <p class="mb-0">API anahtarlarını kaynak kodda asla hardcode yapmayın. Ortam değişkenleri veya güvenli yapılandırma kullanın.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-info">
                                <h4><i class="fas fa-balance-scale me-2"></i>Parça Boyutu</h4>
                                <p class="mb-0">Bağlam ve performans arasında denge kurun. Hassasiyet için küçük, bağlam için büyük parçalar.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-success">
                                <h4><i class="fas fa-database me-2"></i>Depolama</h4>
                                <p class="mb-0">Ölçeğinize ve gereksinimlerinize göre depolama provider'ı seçin.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-primary">
                                <h4><i class="fas fa-shield-alt me-2"></i>Güvenlik</h4>
                                <p class="mb-0">Üretim ortamları için uygun erişim kontrolleri ve izleme kullanın.</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>