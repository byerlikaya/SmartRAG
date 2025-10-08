---
layout: default
title: Örnekler
description: SmartRAG entegrasyonu için pratik örnekler ve kod örnekleri
lang: tr
---

<div class="page-content">
    <div class="container">
        <!-- Basic Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Hızlı Örnekler</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Dakikalar içinde SmartRAG ile başlayın.</p>
                    
                    <h3>Temel Kullanım</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// 1. Belge yükle
var document = await _documentService.UploadDocumentAsync(file);

// 2. Belgeleri ara
var results = await _searchService.SearchDocumentsAsync(query, 10);

// 3. Konuşma geçmişi ile RAG yanıtı üret
var response = await _searchService.GenerateRagAnswerAsync(question);</code></pre>
                    </div>

                    <h3>Controller Örneği</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IDocumentSearchService _searchService;
    
    [HttpPost("search")]
    public async Task<ActionResult> Search([FromBody] SearchRequest request)
    {
        var response = await _searchService.GenerateRagAnswerAsync(
            request.Query, request.MaxResults);
        return Ok(response);
    }
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 5;
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Advanced Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Gelişmiş Kullanım</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Üretim kullanımı için gelişmiş örnekler.</p>
                    
                    <h3>Toplu İşleme</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Birden fazla belge yükle
var documents = await _documentService.UploadDocumentsAsync(files);

// Depolama istatistiklerini al
var stats = await _documentService.GetStorageStatisticsAsync();

// Belgeleri yönet
var allDocs = await _documentService.GetAllDocumentsAsync();
await _documentService.DeleteDocumentAsync(documentId);</code></pre>
                    </div>

                    <h3>Bakım İşlemleri</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Embedding'leri yeniden oluştur
await _documentService.RegenerateAllEmbeddingsAsync();

// Verileri temizle
await _documentService.ClearAllEmbeddingsAsync();
await _documentService.ClearAllDocumentsAsync();</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Yapılandırma</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>SmartRAG'i ihtiyaçlarınıza göre yapılandırın.</p>
                    
                    <h3>Servis Kaydı</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Redis;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <div class="alert alert-info">
                        <h4><i class="fas fa-question-circle me-2"></i>Yardıma mı İhtiyacınız Var?</h4>
                        <p class="mb-0">Örneklerle ilgili yardıma ihtiyacınız varsa:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/tr/getting-started">Başlangıç Rehberi</a></li>
                            <li><a href="{{ site.baseurl }}/tr/api-reference">API Referansı</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub'da konu açın</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">E-posta ile destek alın</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>