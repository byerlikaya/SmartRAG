---
layout: default
title: Sorun Giderme
description: SmartRAG için yaygın sorunlar ve çözümleri
lang: tr
---

<div class="page-content">
    <div class="container">
        <!-- Common Issues Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Yaygın Sorunlar</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Sık karşılaşılan sorunlara hızlı çözümler.</p>
                    
                    <h3>Yapılandırma Sorunları</h3>
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Geçersiz API Anahtarı</h5>
                        <p><strong>Sorun:</strong> "Unauthorized" veya "Invalid API key" hataları</p>
                        <p><strong>Çözüm:</strong> appsettings.json dosyasındaki API anahtarlarını kontrol edin</p>
                    </div>
                    
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Eksik Yapılandırma</h5>
                        <p><strong>Sorun:</strong> "Configuration not found" hataları</p>
                        <p><strong>Çözüm:</strong> appsettings.json dosyasında SmartRAG bölümünün mevcut olduğundan emin olun</p>
                    </div>

                    <h3>Servis Kayıt Sorunları</h3>
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Servis Kayıtlı Değil</h5>
                        <p><strong>Sorun:</strong> "Unable to resolve service" hataları</p>
                        <p><strong>Çözüm:</strong> Program.cs dosyasında SmartRAG servislerini ekleyin:</p>
                        <div class="code-example">
                            <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Redis;
});</code></pre>
                        </div>
                    </div>

                    <h3>Ses İşleme Sorunları</h3>
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Google Speech-to-Text Hataları</h5>
                        <p><strong>Sorun:</strong> Ses transkripsiyonu başarısız</p>
                        <p><strong>Çözüm:</strong> Google API anahtarını ve desteklenen ses formatını doğrulayın</p>
                    </div>

                    <h3>Depolama Sorunları</h3>
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Redis Bağlantı Hatası</h5>
                        <p><strong>Sorun:</strong> Redis'e bağlanılamıyor</p>
                        <p><strong>Çözüm:</strong> Redis bağlantı dizesini kontrol edin ve Redis'in çalıştığından emin olun</p>
                    </div>
                    
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Qdrant Bağlantı Hatası</h5>
                        <p><strong>Sorun:</strong> Qdrant'a bağlanılamıyor</p>
                        <p><strong>Çözüm:</strong> Qdrant host ve API anahtarı yapılandırmasını doğrulayın</p>
                    </div>
                </div>
            </div>
        </section>

        <!-- Performance Issues Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Performans Sorunları</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>SmartRAG performansını optimize edin.</p>
                    
                    <h3>Yavaş Belge İşleme</h3>
                    <div class="alert alert-info">
                        <h5><i class="fas fa-info-circle me-2"></i>Optimizasyon İpuçları</h5>
                        <ul class="mb-0">
                            <li>Uygun parça boyutları kullanın (500-1000 karakter)</li>
                            <li>Daha iyi performans için Redis önbelleklemesini etkinleştirin</li>
                            <li>Üretim vektör depolaması için Qdrant kullanın</li>
                            <li>Belgeleri toplu olarak işleyin</li>
                        </ul>
                    </div>

                    <h3>Bellek Sorunları</h3>
                    <div class="alert alert-info">
                        <h5><i class="fas fa-info-circle me-2"></i>Bellek Yönetimi</h5>
                        <ul class="mb-0">
                            <li>İşleme için belge boyutunu sınırlayın</li>
                            <li>Büyük dosyalar için streaming kullanın</li>
                            <li>Embeddings önbelleğini periyodik olarak temizleyin</li>
                            <li>Üretimde bellek kullanımını izleyin</li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>

        <!-- Debugging Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Hata Ayıklama</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Loglama ve hata ayıklamayı etkinleştirin.</p>
                    
                    <h3>Debug Loglama Etkinleştir</h3>
                    <div class="code-example">
                        <pre><code class="language-json">{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SmartRAG": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}</code></pre>
                    </div>

                    <h3>Servis Durumunu Kontrol Et</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Servislerin kayıtlı olup olmadığını kontrol et
var serviceProvider = services.BuildServiceProvider();
var documentService = serviceProvider.GetService<IDocumentService>();
var searchService = serviceProvider.GetService<IDocumentSearchService>();

if (documentService == null || searchService == null)
{
    Console.WriteLine("SmartRAG servisleri düzgün kayıtlı değil!");
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <div class="alert alert-info">
                        <h4><i class="fas fa-question-circle me-2"></i>Hala Yardıma mı İhtiyacınız Var?</h4>
                        <p class="mb-0">Çözüm bulamıyorsanız:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/tr/getting-started">Başlangıç Rehberi</a></li>
                            <li><a href="{{ site.baseurl }}/tr/configuration">Yapılandırma Rehberi</a></li>
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