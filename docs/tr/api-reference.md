---
layout: default
title: API Referansı
description: SmartRAG servisleri ve arayüzleri için tam API dokümantasyonu
lang: tr
---

<div class="page-content">
    <div class="container">
        <!-- Core Interfaces Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Temel Arayüzler</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>SmartRAG, belge işleme ve yönetimi için temel arayüzler sağlar.</p>
                    
                    <h3>IDocumentSearchService</h3>
                    <p>RAG (Retrieval-Augmented Generation) ile belge arama ve AI destekli yanıt üretimi.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IDocumentSearchService
{
    Task&lt;List&lt;DocumentChunk&gt;&gt; SearchDocumentsAsync(string query, int maxResults = 5);
    Task&lt;RagResponse&gt; GenerateRagAnswerAsync(string query, int maxResults = 5, bool startNewConversation = false);
}</code></pre>
                    </div>

                    <h4>GenerateRagAnswerAsync</h4>
                    <p>Otomatik oturum yönetimi ve konuşma geçmişi ile AI destekli yanıtlar üretir.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">Task&lt;RagResponse&gt; GenerateRagAnswerAsync(string query, int maxResults = 5, bool startNewConversation = false)</code></pre>
                    </div>
                    
                    <p><strong>Parametreler:</strong></p>
                    <ul>
                        <li><code>query</code> (string): Kullanıcının sorusu</li>
                        <li><code>maxResults</code> (int): Alınacak maksimum belge parçası sayısı (varsayılan: 5)</li>
                        <li><code>startNewConversation</code> (bool): Yeni konuşma oturumu başlat (varsayılan: false)</li>
                    </ul>
                    
                    <p><strong>Döner:</strong> <code>RagResponse</code> AI yanıtı, kaynaklar ve metadata ile</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Temel kullanım
var response = await documentSearchService.GenerateRagAnswerAsync("Hava nasıl?");

// Yeni konuşma başlat
var response = await documentSearchService.GenerateRagAnswerAsync("/new");</code></pre>
                    </div>

                    <h3>Diğer Önemli Arayüzler</h3>
                    <p>Belge işleme ve depolama için ek servisler.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Belge ayrıştırma ve işleme
IDocumentParserService - Belgeleri ayrıştır ve metin çıkar
IDocumentRepository - Belge depolama işlemleri
IAIService - AI sağlayıcı iletişimi
IAudioParserService - Ses transkripsiyonu (Google Speech-to-Text)</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Models Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Ana Modeller</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>SmartRAG işlemleri için temel veri modelleri.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Ana yanıt modeli
public class RagResponse
{
    public string Query { get; set; }
    public string Answer { get; set; }
    public List&lt;SearchSource&gt; Sources { get; set; }
    public DateTime SearchedAt { get; set; }
}

// Arama sonuçları için belge parçası
public class DocumentChunk
{
    public string Id { get; set; }
    public string DocumentId { get; set; }
    public string Content { get; set; }
    public double RelevanceScore { get; set; }
}</code></pre>
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
                    <p>SmartRAG için temel yapılandırma seçenekleri.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// AI Sağlayıcıları
AIProvider.Anthropic    // Claude modelleri
AIProvider.OpenAI       // GPT modelleri
AIProvider.Gemini       // Google modelleri

// Depolama Sağlayıcıları  
StorageProvider.Qdrant  // Vektör veritabanı
StorageProvider.Redis   // Yüksek performanslı önbellek
StorageProvider.Sqlite  // Yerel veritabanı</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Quick Start Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Hızlı Başlangıç</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>SmartRAG ile dakikalar içinde başlayın.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// 1. Servisleri kaydet
services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Redis;
});

// 2. Enjekte et ve kullan
public class MyController : ControllerBase
{
    private readonly IDocumentSearchService _searchService;
    
    public async Task<ActionResult> Ask(string question)
    {
        var response = await _searchService.GenerateRagAnswerAsync(question);
        return Ok(response);
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Common Patterns Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Yaygın Kalıplar</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Sık kullanılan kalıplar ve yapılandırmalar.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Belge yükleme
var document = await _documentService.UploadDocumentAsync(file);

// Belge arama  
var results = await _searchService.SearchDocumentsAsync(query, 10);

// RAG konuşması
var response = await _searchService.GenerateRagAnswerAsync(question);

// Yapılandırma
services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Redis;
});</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Error Handling Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Hata Yönetimi</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Yaygın hatalar ve hata yönetimi kalıpları.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">try
{
    var response = await _searchService.GenerateRagAnswerAsync(query);
    return Ok(response);
}
catch (SmartRagException ex)
{
    return BadRequest(ex.Message);
}
catch (Exception ex)
{
    return StatusCode(500, "Internal server error");
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Performance Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Performans İpuçları</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>SmartRAG performansını optimize edin.</p>
                    
                    <div class="alert alert-info">
                        <ul class="mb-0">
                            <li><strong>Parça Boyutu</strong>: Optimal denge için 500-1000 karakter</li>
                            <li><strong>Toplu İşlemler</strong>: Birden fazla belgeyi birlikte işle</li>
                            <li><strong>Önbellekleme</strong>: Daha iyi performans için Redis kullan</li>
                            <li><strong>Vektör Depolama</strong>: Üretim için Qdrant kullan</li>
                        </ul>
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
                        <p class="mb-0">API ile ilgili yardıma ihtiyacınız varsa:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/tr/getting-started">Başlangıç Rehberi</a></li>
                            <li><a href="{{ site.baseurl }}/tr/configuration">Yapılandırma Seçenekleri</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub'da konu açın</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">E-posta ile destek alın</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>