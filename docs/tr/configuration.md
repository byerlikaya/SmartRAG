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
                        <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
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
                                    <td><code>ApiKey</code></td>
                                    <td><code>string</code></td>
                                    <td>Gerekli</td>
                                    <td>AI provider için API anahtarınız</td>
                                </tr>
                                <tr>
                                    <td><code>ModelName</code></td>
                                    <td><code>string</code></td>
                                    <td>Provider varsayılanı</td>
                                    <td>Kullanılacak spesifik model</td>
                                </tr>
                                <tr>
                                    <td><code>ChunkSize</code></td>
                                    <td><code>int</code></td>
                                    <td>1000</td>
                                    <td>Belge parçalarının boyutu</td>
                                </tr>
                                <tr>
                                    <td><code>ChunkOverlap</code></td>
                                    <td><code>int</code></td>
                                    <td>200</td>
                                    <td>Parçalar arasındaki örtüşme</td>
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
                        <div class="code-tab" data-tab="azure">Azure OpenAI</div>
                        <div class="code-tab" data-tab="gemini">Gemini</div>
                        <div class="code-tab" data-tab="custom">Özel</div>
                    </div>
                    
                    <div class="code-content">
                        <div class="code-panel active" id="anthropic">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.ApiKey = "your-anthropic-key";
    options.ModelName = "claude-3-sonnet-20240229";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="openai">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.ApiKey = "your-openai-key";
    options.ModelName = "text-embedding-ada-002";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="azure">
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
                        
                        <div class="code-panel" id="gemini">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Gemini;
    options.ApiKey = "your-gemini-key";
    options.ModelName = "embedding-001";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="custom">
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
                    <h2>Depolama Provider Yapılandırması</h2>
                    <p>İhtiyaçlarınıza en uygun depolama backend'ini seçin:</p>
                    
                    <div class="code-tabs">
                        <div class="code-tab active" data-tab="qdrant">Qdrant</div>
                        <div class="code-tab" data-tab="redis">Redis</div>
                        <div class="code-tab" data-tab="sqlite">SQLite</div>
                        <div class="code-tab" data-tab="memory">Bellek İçi</div>
                        <div class="code-tab" data-tab="filesystem">Dosya Sistemi</div>
                    </div>
                    
                    <div class="code-content">
                        <div class="code-panel active" id="qdrant">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.Qdrant;
    options.QdrantUrl = "http://localhost:6333";
    options.CollectionName = "smartrag_documents";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="redis">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.Redis;
    options.RedisConnectionString = "localhost:6379";
    options.DatabaseId = 0;
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="sqlite">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.Sqlite;
    options.ConnectionString = "Data Source=smartrag.db";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="memory">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.InMemory;
    // Ek yapılandırma gerekmez
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="filesystem">
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
                    <h2>Gelişmiş Yapılandırma</h2>
                    <p>SmartRAG'i özel gereksinimleriniz için ince ayar yapın:</p>
                    
                    <h3>Özel Parçalama</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.ChunkSize = 500;
    options.ChunkOverlap = 100;
    options.ChunkingStrategy = ChunkingStrategy.Sentence;
});</code></pre>
                    </div>
                    
                    <h3>Belge İşleme</h3>
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
                    <h2>Ortam Yapılandırması</h2>
                    <p>Ortam değişkenleri veya yapılandırma dosyaları kullanarak SmartRAG'i yapılandırın:</p>
                    
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
                    
                    <h3>Ortam Değişkenleri</h3>
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