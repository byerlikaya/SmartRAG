---
layout: default
title: Katkıda Bulunma
description: SmartRAG projesine nasıl katkıda bulunabileceğinizi öğrenin
lang: tr
---

<div class="page-content">
    <div class="container">
        <!-- How to Contribute Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Nasıl Katkıda Bulunulur</h2>
                    <p>SmartRAG açık kaynak bir projedir ve topluluk katkılarını memnuniyetle karşılar. İşte projeye katkıda bulunmanın farklı yolları:</p>
                    
                    <h3>🐛 Hata Bildirimi</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-bug me-2"></i>Hata Buldunuz mu?</h4>
                        <p class="mb-0">Lütfen GitHub Issues'da detaylı bir hata raporu oluşturun.</p>
                    </div>
                    
                    <h3>✨ Özellik İsteği</h3>
                    <div class="alert alert-success">
                        <h4><i class="fas fa-lightbulb me-2"></i>Yeni Fikirleriniz mi Var?</h4>
                        <p class="mb-0">GitHub Discussions'da özellik isteklerinizi paylaşın.</p>
                    </div>
                    
                    <h3>📝 Dokümantasyon</h3>
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-book me-2"></i>Dokümantasyonu İyileştirin</h4>
                        <p class="mb-0">Eksik veya yanlış bilgileri düzeltin, yeni örnekler ekleyin.</p>
                    </div>
                    
                    <h3>💻 Kod Katkısı</h3>
                    <div class="alert alert-primary">
                        <h4><i class="fas fa-code me-2"></i>Kod Yazın</h4>
                        <p class="mb-0">Yeni özellikler ekleyin, hataları düzeltin, performansı iyileştirin.</p>
                    </div>
                </div>
            </div>
        </section>

        <!-- Prerequisites Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Ön Gereksinimler</h2>
                    <p>SmartRAG'e katkıda bulunmadan önce ihtiyacınız olan araçlar:</p>
                    
                    <h3>Gerekli Araçlar</h3>
                    <ul>
                        <li><strong>.NET 9.0 SDK</strong> veya üzeri</li>
                        <li><strong>Git</strong> versiyon kontrolü için</li>
                        <li><strong>Visual Studio 2022</strong> veya <strong>VS Code</strong></li>
                        <li><strong>Docker</strong> (Qdrant, Redis testleri için)</li>
                    </ul>
                    
                    <h3>Hesap Gereksinimleri</h3>
                    <ul>
                        <li><strong>GitHub hesabı</strong></li>
                        <li><strong>NuGet hesabı</strong> (paket yayınlama için)</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Development Workflow Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Geliştirme İş Akışı</h2>
                    <p>SmartRAG'e katkıda bulunmak için izlemeniz gereken adımlar:</p>
                    
                    <h3>1. Repository'yi Fork Edin</h3>
                    <div class="code-example">
                        <pre><code class="language-bash"># GitHub'da repository'yi fork edin
# Sonra kendi hesabınızda clone edin
git clone https://github.com/YOUR_USERNAME/SmartRAG.git
cd SmartRAG</code></pre>
                    </div>
                    
                    <h3>2. Upstream Remote Ekleyin</h3>
                    <div class="code-example">
                        <pre><code class="language-bash">git remote add upstream https://github.com/byerlikaya/SmartRAG.git
git fetch upstream</code></pre>
                    </div>
                    
                    <h3>3. Yeni Branch Oluşturun</h3>
                    <div class="code-example">
                        <pre><code class="language-bash"># Feature branch oluşturun
git checkout -b feature/your-feature-name

# Bug fix branch oluşturun
git checkout -b fix/issue-number-description</code></pre>
                    </div>
                    
                    <h3>4. Değişikliklerinizi Yapın</h3>
                    <p>Kodunuzu yazın, test edin ve commit edin:</p>
                    <div class="code-example">
                        <pre><code class="language-bash"># Değişiklikleri stage edin
git add .

# Commit edin
git commit -m "feat: add new feature description"

# Push edin
git push origin feature/your-feature-name</code></pre>
                    </div>
                    
                    <h3>5. Pull Request Oluşturun</h3>
                    <p>GitHub'da Pull Request oluşturun ve değişikliklerinizi açıklayın.</p>
                </div>
            </div>
        </section>

        <!-- Code Style Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Kod Stili</h2>
                    <p>SmartRAG projesi belirli kod standartlarını takip eder:</p>
                    
                    <h3>C# Kod Standartları</h3>
                    <ul>
                        <li><strong>PascalCase</strong>: Sınıf, method ve property isimleri</li>
                        <li><strong>camelCase</strong>: Local variable ve parameter isimleri</li>
                        <li><strong>UPPER_CASE</strong>: Constant değerler</li>
                        <li><strong>Async/Await</strong>: Asenkron operasyonlar için</li>
                    </ul>
                    
                    <h3>Dosya Organizasyonu</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Dosya başında using statements
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// Namespace
namespace SmartRAG.Services
{
    // Class definition
    public class ExampleService : IExampleService
    {
        // Fields
        private readonly ILogger<ExampleService> _logger;
        
        // Constructor
        public ExampleService(ILogger<ExampleService> logger)
        {
            _logger = logger;
        }
        
        // Public methods
        public async Task<string> DoSomethingAsync()
        {
            // Implementation
        }
        
        // Private methods
        private void HelperMethod()
        {
            // Implementation
        }
    }
}</code></pre>
                    </div>
                    
                    <h3>XML Dokümantasyon</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">/// <summary>
/// Belgeyi yükler ve işler
/// </summary>
/// <param name="file">Yüklenecek dosya</param>
/// <returns>İşlenmiş belge</returns>
/// <exception cref="ArgumentException">Geçersiz dosya formatı</exception>
public async Task<Document> UploadDocumentAsync(IFormFile file)
{
    // Implementation
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Testing Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Test Etme</h2>
                    <p>Tüm katkılar test edilmelidir:</p>
                    
                    <h3>Unit Testler</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task UploadDocument_ValidFile_ReturnsDocument()
{
    // Arrange
    var mockFile = new Mock<IFormFile>();
    var service = new DocumentService(mockLogger.Object);
    
    // Act
    var result = await service.UploadDocumentAsync(mockFile.Object);
    
    // Assert
    Assert.IsNotNull(result);
    Assert.IsNotEmpty(result.Id);
}</code></pre>
                    </div>
                    
                    <h3>Test Çalıştırma</h3>
                    <div class="code-example">
                        <pre><code class="language-bash"># Tüm testleri çalıştır
dotnet test

# Belirli bir projeyi test et
dotnet test tests/SmartRAG.Tests/

# Coverage raporu al
dotnet test --collect:"XPlat Code Coverage"</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Documentation Standards Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Dokümantasyon Standartları</h2>
                    <p>Dokümantasyon katkıları için standartlar:</p>
                    
                    <h3>Markdown Formatı</h3>
                    <ul>
                        <li><strong>Başlıklar</strong>: Hiyerarşik yapı kullanın (H1, H2, H3)</li>
                        <li><strong>Kod Blokları</strong>: Dil belirtin (```csharp, ```bash)</li>
                        <li><strong>Linkler</strong>: Açıklayıcı link metinleri kullanın</li>
                        <li><strong>Listeler</strong>: Tutarlı format kullanın</li>
                    </ul>
                    
                    <h3>Çoklu Dil Desteği</h3>
                    <p>Tüm dokümantasyon 4 dilde mevcut olmalıdır:</p>
                    <ul>
                        <li><strong>İngilizce</strong> (en) - Ana dil</li>
                        <li><strong>Türkçe</strong> (tr) - Yerel dil</li>
                        <li><strong>Almanca</strong> (de) - Uluslararası</li>
                        <li><strong>Rusça</strong> (ru) - Uluslararası</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Issue Reporting Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Hata Bildirimi</h2>
                    <p>Hata bildirirken lütfen şu bilgileri dahil edin:</p>
                    
                    <h3>Gerekli Bilgiler</h3>
                    <ul>
                        <li><strong>SmartRAG Versiyonu</strong>: Hangi versiyonu kullanıyorsunuz?</li>
                        <li><strong>.NET Versiyonu</strong>: .NET 9.0</li>
                        <li><strong>İşletim Sistemi</strong>: Windows, Linux, macOS?</li>
                        <li><strong>Hata Mesajı</strong>: Tam hata mesajı ve stack trace</li>
                        <li><strong>Adımlar</strong>: Hatayı yeniden üretme adımları</li>
                        <li><strong>Beklenen Davranış</strong>: Ne olmasını bekliyordunuz?</li>
                        <li><strong>Gerçek Davranış</strong>: Ne oldu?</li>
                    </ul>
                    
                    <h3>Hata Şablonu</h3>
                    <div class="code-example">
                        <pre><code class="language-markdown">## Hata Açıklaması
Kısa ve net hata açıklaması

## Yeniden Üretme Adımları
1. '...' adımına gidin
2. '...' tıklayın
3. '...' hatası görünür

## Beklenen Davranış
Ne olmasını bekliyordunuz

## Gerçek Davranış
Ne oldu

## Ekran Görüntüleri
Varsa ekran görüntüleri ekleyin

## Ortam Bilgileri
- SmartRAG Versiyonu: 2.1.0
- .NET Versiyonu: 9.0
- İşletim Sistemi: Windows 11
- Tarayıcı: Chrome 120</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- PR Process Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Pull Request Süreci</h2>
                    <p>Pull Request oluştururken dikkat edilecek noktalar:</p>
                    
                    <h3>PR Şablonu</h3>
                    <div class="code-example">
                        <pre><code class="language-markdown">## Değişiklik Açıklaması
Bu PR ne yapıyor?

## Değişiklik Türü
- [ ] Bug fix
- [ ] Yeni özellik
- [ ] Breaking change
- [ ] Dokümantasyon güncellemesi

## Test Edildi mi?
- [ ] Unit testler geçiyor
- [ ] Integration testler geçiyor
- [ ] Manuel test yapıldı

## Checklist
- [ ] Kod standartlarına uygun
- [ ] Dokümantasyon güncellendi
- [ ] Testler eklendi
- [ ] Breaking change yoksa</code></pre>
                    </div>
                    
                    <h3>Review Süreci</h3>
                    <ul>
                        <li><strong>Otomatik Testler</strong>: CI/CD pipeline'ı geçmeli</li>
                        <li><strong>Code Review</strong>: En az 1 onay gerekli</li>
                        <li><strong>Dokümantasyon</strong>: Gerekirse güncellenmeli</li>
                        <li><strong>Breaking Changes</strong>: Özel dikkat gerektirir</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Release Process Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Yayın Süreci</h2>
                    <p>SmartRAG sürümleri nasıl yayınlanır:</p>
                    
                    <h3>Sürüm Türleri</h3>
                    <ul>
                        <li><strong>Patch (1.0.1)</strong>: Hata düzeltmeleri</li>
                        <li><strong>Minor (1.1.0)</strong>: Yeni özellikler</li>
                        <li><strong>Major (2.0.0)</strong>: Breaking changes</li>
                    </ul>
                    
                    <h3>Yayın Adımları</h3>
                    <ol>
                        <li><strong>Changelog Güncelleme</strong>: Tüm değişiklikler listelenir</li>
                        <li><strong>Version Bump</strong>: .csproj dosyaları güncellenir</li>
                        <li><strong>Git Tag</strong>: Yeni sürüm için tag oluşturulur</li>
                        <li><strong>NuGet Yayını</strong>: Paket NuGet'e yüklenir</li>
                        <li><strong>GitHub Release</strong>: Release notes ile yayınlanır</li>
                    </ol>
                </div>
            </div>
        </section>

        <!-- Community Guidelines Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Topluluk Kuralları</h2>
                    <p>SmartRAG topluluğunda uyulması gereken kurallar:</p>
                    
                    <h3>Davranış Kuralları</h3>
                    <ul>
                        <li><strong>Saygılı Olun</strong>: Herkese saygı gösterin</li>
                        <li><strong>Yapıcı Olun</strong>: Yapıcı geri bildirim verin</li>
                        <li><strong>Öğrenmeye Açık Olun</strong>: Yeni fikirleri kabul edin</li>
                        <li><strong>Profesyonel Olun</strong>: Profesyonel dil kullanın</li>
                    </ul>
                    
                    <h3>İletişim Kanalları</h3>
                    <ul>
                        <li><strong>GitHub Issues</strong>: Hata bildirimi ve özellik istekleri</li>
                        <li><strong>GitHub Discussions</strong>: Genel tartışmalar</li>
                        <li><strong>Email</strong>: b.yerlikaya@outlook.com</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <div class="alert alert-info">
                        <h4><i class="fas fa-question-circle me-2"></i>Yardıma mı İhtiyacınız Var?</h4>
                        <p class="mb-0">Katkıda bulunma konusunda yardıma ihtiyacınız varsa:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/tr/getting-started">Başlangıç Kılavuzu</a></li>
                            <li><a href="{{ site.baseurl }}/tr/api-reference">API Referansı</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub Issues</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">E-posta Desteği</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>
