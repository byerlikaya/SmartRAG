---
layout: default
title: Geçiş Rehberi
description: Önceki sürümlerden yükseltin ve mevcut uygulamaları geçirin
lang: tr
---

<div class="page-content">
    <div class="container">
        <!-- Overview Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Genel Bakış</h2>
                    <p>SmartRAG 2.0.0, .NET 9.0'dan .NET Standard 2.0/2.1'e büyük bir framework değişikliği getiriyor. Bu geçiş, mevcut tüm işlevselliği korurken eski ve kurumsal .NET uygulamalarıyla maksimum uyumluluk sağlıyor.</p>
                    
                    <div class="alert alert-info">
                        <h4>🎯 Temel Faydalar</h4>
                        <ul>
                            <li><strong>Maksimum Uyumluluk</strong>: .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+ desteği</li>
                            <li><strong>Kurumsal Entegrasyon</strong>: Mevcut kurumsal çözümlerle sorunsuz entegrasyon</li>
                            <li><strong>Eski Sistem Desteği</strong>: Eski .NET uygulamalarıyla tam uyumluluk</li>
                            <li><strong>Kırıcı Değişiklik Yok</strong>: Tüm mevcut API'lar değişmeden kalıyor</li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>

        <!-- Breaking Changes Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Kırıcı Değişiklikler</h2>
                    <p>Genel API değişmeden kalırken, farkında olunması gereken bazı framework seviyesi değişiklikler var:</p>
                    
                    <h3>Hedef Framework</h3>
                    <ul>
                        <li><strong>Önceki</strong>: <code>net9.0</code></li>
                        <li><strong>Sonraki</strong>: <code>netstandard2.0;netstandard2.1</code></li>
                    </ul>
                    
                    <h3>Dil Sürümü</h3>
                    <ul>
                        <li><strong>Önceki</strong>: C# 12 özellikleri (file-scoped namespaces, collection expressions, vb.)</li>
                        <li><strong>Sonraki</strong>: C# 7.3 uyumluluğu (geleneksel syntax)</li>
                    </ul>
                    
                    <h3>Paket Sürümleri</h3>
                    <ul>
                        <li><strong>Microsoft.Extensions.*</strong>: 9.0.8'den 7.0.0'a düşürüldü</li>
                        <li><strong>System.Text.Json</strong>: Güvenlik için 8.0.0'a güncellendi</li>
                        <li><strong>Qdrant.Client</strong>: 1.15.1'den 1.7.0'a düşürüldü</li>
                        <li><strong>StackExchange.Redis</strong>: 2.9.11'den 2.6.111'e düşürüldü</li>
                        <li><strong>Microsoft.Data.Sqlite</strong>: 9.0.8'den 7.0.0'a düşürüldü</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Migration Steps Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Geçiş Adımları</h2>
                    
                    <h3>Adım 1: Paket Referansını Güncelle</h3>
                    <p>SmartRAG paket referansını 2.0.0 sürümüne güncelleyin:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-xml">&lt;!-- Önceki --&gt;
&lt;PackageReference Include="SmartRAG" Version="1.1.0" /&gt;

&lt;!-- Sonraki --&gt;
&lt;PackageReference Include="SmartRAG" Version="2.0.0" /&gt;</code></pre>
                    </div>
                    
                    <h3>Adım 2: Framework Uyumluluğunu Doğrula</h3>
                    <p>Uygulamanızın uyumlu bir framework'ü hedeflediğinden emin olun:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-xml">&lt;!-- Uyumlu Hedef Framework'ler --&gt;
&lt;TargetFramework&gt;net48&lt;/TargetFramework&gt;           &lt;!-- .NET Framework 4.8 --&gt;
&lt;TargetFramework&gt;netcoreapp2.0&lt;/TargetFramework&gt;    &lt;!-- .NET Core 2.0 --&gt;
&lt;TargetFramework&gt;net5.0&lt;/TargetFramework&gt;           &lt;!-- .NET 5 --&gt;
&lt;TargetFramework&gt;net6.0&lt;/TargetFramework&gt;           &lt;!-- .NET 6 --&gt;
&lt;TargetFramework&gt;net7.0&lt;/TargetFramework&gt;           &lt;!-- .NET 7 --&gt;
&lt;TargetFramework&gt;net8.0&lt;/TargetFramework&gt;           &lt;!-- .NET 8 --&gt;
&lt;TargetFramework&gt;net9.0&lt;/TargetFramework&gt;           &lt;!-- .NET 9 --&gt;</code></pre>
                    </div>
                    
                    <h3>Adım 3: Uygulamanızı Test Edin</h3>
                    <p>Güncellemeden sonra, her şeyin doğru çalıştığından emin olmak için uygulamanızı test edin:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-bash"># Projenizi derleyin
dotnet build

# Testlerinizi çalıştırın
dotnet test

# Uygulamanızı test edin
dotnet run</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Compatibility Matrix Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Framework Uyumluluk Matrisi</h2>
                    
                    <div class="table-responsive">
                        <table class="table table-striped">
                            <thead>
                                <tr>
                                    <th>Framework</th>
                                    <th>Sürüm</th>
                                    <th>Uyumluluk</th>
                                    <th>Notlar</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td>.NET Framework</td>
                                    <td>4.6.1+</td>
                                    <td>✅ Tam</td>
                                    <td>Önerilen: 4.7.2+</td>
                                </tr>
                                <tr>
                                    <td>.NET Core</td>
                                    <td>2.0+</td>
                                    <td>✅ Tam</td>
                                    <td>Önerilen: 3.1+</td>
                                </tr>
                                <tr>
                                    <td>.NET</td>
                                    <td>5.0+</td>
                                    <td>✅ Tam</td>
                                    <td>Tüm sürümler destekleniyor</td>
                                </tr>
                                <tr>
                                    <td>.NET Standard</td>
                                    <td>2.0/2.1</td>
                                    <td>✅ Tam</td>
                                    <td>Doğrudan destek</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </section>

        <!-- Troubleshooting Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Sorun Giderme</h2>
                    
                    <h3>Yaygın Sorunlar</h3>
                    
                    <h4>Paket Sürüm Çakışmaları</h4>
                    <p>Paket sürüm çakışmalarıyla karşılaşırsanız, projenizin uyumlu paket sürümlerini kullandığından emin olun:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-xml">&lt;!-- .NET Standard uyumluluğu için önerilen paket sürümleri --&gt;
&lt;PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="7.0.0" /&gt;
&lt;PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="7.0.0" /&gt;
&lt;PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="7.0.0" /&gt;
&lt;PackageReference Include="System.Text.Json" Version="8.0.0" /&gt;</code></pre>
                    </div>
                    
                    <h4>Derleme Hataları</h4>
                    <p>Derleme hatalarıyla karşılaşırsanız, projenizin uyumlu bir framework sürümünü hedeflediğinden emin olun.</p>
                    
                    <h4>Çalışma Zamanı Hataları</h4>
                    <p>Tüm çalışma zamanı davranışı aynı kalır. Sorunlarla karşılaşırsanız, framework sürüm uyumluluğunuzu kontrol edin.</p>
                </div>
            </div>
        </section>

        <!-- Support Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Yardıma mı İhtiyacınız Var?</h2>
                    <p>Geçiş sırasında herhangi bir sorunla karşılaşırsanız:</p>
                    
                    <ul>
                        <li>📖 <strong>Dokümantasyon</strong>: Kapsamlı dokümantasyonumuzu kontrol edin</li>
                        <li>🐛 <strong>Sorunlar</strong>: Hataları <a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub</a>'da bildirin</li>
                        <li>💬 <strong>Tartışmalar</strong>: <a href="https://github.com/byerlikaya/SmartRAG/discussions" target="_blank">GitHub Discussions</a>'da topluluk tartışmalarına katılın</li>
                        <li>⭐ <strong>Yıldız</strong>: Repository'yi yıldızlayarak desteğinizi gösterin</li>
                    </ul>
                </div>
            </div>
        </section>
    </div>
</div>
