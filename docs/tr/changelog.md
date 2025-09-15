---
layout: default
title: Değişiklik Günlüğü
description: SmartRAG için sürüm geçmişi ve sürüm notları
lang: tr
---

<div class="page-content">
    <div class="container">
        <!-- Version History Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Sürüm Geçmişi</h2>
                    <p>Detaylı değişiklik bilgileri ile SmartRAG sürümlerinin tam geçmişi.</p>

                    <h3>Sürüm 2.2.0 - 2025-01-15</h3>
                    <div class="alert alert-success">
                        <h4><i class="fas fa-star me-2"></i>En Son Sürüm</h4>
                        <p class="mb-0">Geliştirilmiş OCR dokümantasyonu ve görünürlük iyileştirmeleri ile mevcut kararlı sürüm.</p>
                    </div>
                    <ul>
                        <li><strong>Geliştirilmiş OCR Dokümantasyonu</strong>: Gerçek kullanım örnekleri ile OCR yeteneklerini sergileyen kapsamlı dokümantasyon</li>
                        <li><strong>İyileştirilmiş README</strong>: Tesseract 5.2.0 + SkiaSharp entegrasyonunu vurgulayan detaylı görüntü işleme özellikleri</li>
                        <li><strong>Kullanım Örnekleri</strong>: Taranmış belgeler, fişler ve görüntü içeriği işleme için detaylı örnekler</li>
                        <li><strong>Paket Metadata</strong>: Daha iyi kullanıcı deneyimi için proje URL'leri ve sürüm notları güncellendi</li>
                        <li><strong>Dokümantasyon Yapısı</strong>: OCR'ı temel farklılaştırıcı olarak sergileyen geliştirilmiş dokümantasyon</li>
                        <li><strong>Kullanıcı Rehberliği</strong>: Görüntü tabanlı belge işleme iş akışları için iyileştirilmiş rehberlik</li>
                        <li><strong>WebP Desteği</strong>: WebP'den PNG'ye dönüştürme ve çok dilli OCR desteği vurgulandı</li>
                        <li><strong>Geliştirici Deneyimi</strong>: Görüntü işleme özelliklerinin geliştiriciler için daha iyi görünürlüğü</li>
                    </ul>

                    <h3>Sürüm 2.1.0 - 2025-01-20</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-info-circle me-2"></i>Önceki Sürüm</h4>
                        <p class="mb-0">Otomatik oturum yönetimi ve konuşma geçmişi ile önceki kararlı sürüm.</p>
                    </div>
                    <ul>
                        <li><strong>Otomatik Oturum Yönetimi</strong>: Manuel oturum ID yönetimi artık gerekli değil</li>
                        <li><strong>Kalıcı Konuşma Geçmişi</strong>: Konuşmalar uygulama yeniden başlatmalarında korunur</li>
                        <li><strong>Yeni Konuşma Komutları</strong>: /new, /reset, /clear ile konuşma kontrolü</li>
                        <li><strong>Gelişmiş API</strong>: Geriye uyumlu startNewConversation parametresi</li>
                        <li><strong>Depolama Entegrasyonu</strong>: Tüm sağlayıcılarla (Redis, SQLite, FileSystem, InMemory) uyumlu</li>
                        <li><strong>Format Tutarlılığı</strong>: Tüm depolama sağlayıcılarında standart konuşma formatı</li>
                        <li><strong>Thread Güvenliği</strong>: Konuşma işlemleri için gelişmiş eşzamanlı erişim</li>
                        <li><strong>Platform Bağımsız</strong>: Tüm .NET ortamlarıyla uyumluluk</li>
                        <li><strong>Dokümantasyon Güncellemeleri</strong>: Tüm dil sürümleri (EN, TR, DE, RU) gerçek örneklerle güncellendi</li>
                        <li><strong>%100 Uyumluluk</strong>: Tüm kurallar sıfır uyarı politikasıyla korundu</li>
                    </ul>

                    <h3>Sürüm 2.0.0 - 2025-08-27</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-info-circle me-2"></i>Önceki Sürüm</h4>
                        <p class="mb-0">.NET Standard 2.0/2.1'e geçiş ile önceki kararlı sürüm.</p>
                    </div>
                    <ul>
                        <li><strong>.NET Standard 2.0/2.1</strong>: .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+ uyumluluğu</li>
                        <li><strong>Maksimum Uyumluluk</strong>: Eski ve kurumsal .NET uygulamalarıyla uyumluluk</li>
                        <li><strong>Framework Değişikliği</strong>: .NET 9.0'dan .NET Standard'a geçiş</li>
                        <li><strong>Paket Bağımlılıkları</strong>: Uyumluluk için paket versiyonları güncellendi</li>
                    </ul>

                    <h3>Sürüm 1.1.0 - 2025-08-22</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-info-circle me-2"></i>Önceki Sürüm</h4>
                        <p class="mb-0">Excel desteği ve gelişmiş özelliklerle önceki kararlı sürüm.</p>
                    </div>

                    <h4>Eklenenler</h4>
                    <ul>
                        <li><strong>💬 Konuşma Geçmişi</strong>: Bağlam farkındalığı ile otomatik oturum tabanlı konuşma yönetimi</li>
                        <li><strong>Oturum Yönetimi</strong>: Birden fazla soru arasında konuşma bağlamını korumak için benzersiz oturum kimlikleri</li>
                        <li><strong>Akıllı Bağlam Kısaltma</strong>: Optimal performansı korumak için akıllı konuşma geçmişi kısaltma</li>
                        <li><strong>Depolama Entegrasyonu</strong>: Yapılandırılan depolama sağlayıcıları (Redis, SQLite, vb.) kullanarak konuşma verisi depolama</li>
                        <li><strong>Gelişmiş API</strong>: sessionId parametresi ile güncellenmiş GenerateRagAnswerAsync metodu</li>
                        <li><strong>Gerçek Örnekler</strong>: Tüm dokümantasyon örnekleri gerçek implementasyon kodunu kullanacak şekilde güncellendi</li>
                    </ul>

                    <h4>İyileştirmeler</h4>
                    <ul>
                        <li><strong>Dokümantasyon Gerçekliği</strong>: Tüm örnekler artık gerçek kod tabanı implementasyonu ile eşleşiyor</li>
                        <li><strong>Çoklu Dil Desteği</strong>: Tüm dil versiyonları (EN, TR, DE, RU) konuşma özellikleri ile güncellendi</li>
                        <li><strong>API Tutarlılığı</strong>: Tüm API örneklerinin gerçek SearchController ve SearchRequest modellerini kullandığından emin olundu</li>
                        <li><strong>Kod Kalitesi</strong>: 0 hata, 0 uyarı, 0 mesaj ile Sıfır Uyarı Politikası uygulandı</li>
                    </ul>

                    <h4>Düzeltmeler</h4>
                    <ul>
                        <li><strong>Dokümantasyon Doğruluğu</strong>: Tüm hayali örnekler kaldırıldı ve gerçek implementasyon ile değiştirildi</li>
                        <li><strong>Build Uyumluluğu</strong>: SOLID ve DRY prensipleri ile %100 uyumluluk sağlandı</li>
                        <li><strong>Sihirli Sayılar</strong>: Tüm sihirli sayılar isimli sabitlere dönüştürüldü</li>
                        <li><strong>Loglama Standartları</strong>: Tüm konuşma işlemleri için LoggerMessage delegeleri uygulandı</li>
                    </ul>

                    <h3>Sürüm 1.1.0 - 2025-08-22</h3>

                    <h3>Sürüm 1.1.0 - 2025-08-22</h3>
                    <div class="alert alert-success">
                        <h4><i class="fas fa-star me-2"></i>Yeni Özellikler</h4>
                        <ul class="mb-0">
                            <li><strong>Excel Desteği</strong>: Kapsamlı Excel dosya işleme (.xlsx, .xls) EPPlus 8.1.0 entegrasyonu ile</li>
                            <li><strong>Gelişmiş API Güvenilirliği</strong>: HTTP 529 (Overloaded) hataları için Anthropic API retry mekanizması</li>
                            <li><strong>İçerik İşleme</strong>: Daha sağlam belge ayrıştırma ve fallback hata mesajları</li>
                            <li><strong>Performans</strong>: Excel içerik çıkarma ve doğrulama optimizasyonu</li>
                        </ul>
                    </div>

                    <h3>Sürüm 1.0.3 - 2025-08-20</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-globe me-2"></i>Yeni Özellikler</h4>
                        <ul class="mb-0">
                            <li><strong>Çoklu Dil Desteği</strong>: Türkçe, Almanca, Rusça dil desteği eklendi</li>
                            <li><strong>GitHub Pages Entegrasyonu</strong>: Otomatik dokümantasyon sitesi</li>
                            <li><strong>Gelişmiş SEO</strong>: Meta etiketleri ve yapılandırılmış veri desteği</li>
                            <li><strong>Responsive Tasarım</strong>: Mobil cihazlarda mükemmel görünüm</li>
                        </ul>
                    </div>

                    <div class="alert alert-warning">
                        <h4><i class="fas fa-tools me-2"></i>İyileştirmeler</h4>
                        <ul class="mb-0">
                            <li><strong>Dokümantasyon</strong>: Kapsamlı API referansı ve örnekler</li>
                            <li><strong>Navigasyon</strong>: Dile bağlı menü ve link sistemi</li>
                            <li><strong>Performans</strong>: Sayfa yükleme hızı optimizasyonu</li>
                            <li><strong>Erişilebilirlik</strong>: WCAG 2.1 uyumluluğu</li>
                        </ul>
                    </div>

                    <div class="alert alert-danger">
                        <h4><i class="fas fa-bug me-2"></i>Hata Düzeltmeleri</h4>
                        <ul class="mb-0">
                            <li><strong>Dil Seçimi</strong>: Dil değiştirme işlevselliği düzeltildi</li>
                            <li><strong>Mobil Uyumluluk</strong>: Küçük ekranlarda görüntüleme sorunları giderildi</li>
                            <li><strong>Link Sorunları</strong>: İç ve dış linklerin doğru çalışması sağlandı</li>
                            <li><strong>Tema Sorunları</strong>: Koyu tema uyumluluğu iyileştirildi</li>
                        </ul>
                    </div>

                    <h3>Sürüm 1.0.2 - 2025-08-19</h3>
                    <div class="alert alert-primary">
                        <h4><i class="fas fa-rocket me-2"></i>Yeni Özellikler</h4>
                        <ul class="mb-0">
                            <li><strong>AI Provider Desteği</strong>: OpenAI, Anthropic, Azure OpenAI, Gemini desteği</li>
                            <li><strong>Depolama Seçenekleri</strong>: Qdrant, Redis, SQLite, In-Memory, File System</li>
                            <li><strong>Belge Formatları</strong>: PDF, Word, Excel, TXT desteği</li>
                            <li><strong>Anlamsal Arama</strong>: Gelişmiş arama algoritmaları</li>
                        </ul>
                    </div>

                    <h3>Sürüm 1.0.1 - 2025-08-17</h3>
                    <div class="alert alert-secondary">
                        <h4><i class="fas fa-cog me-2"></i>Temel Özellikler</h4>
                        <ul class="mb-0">
                            <li><strong>Temel RAG</strong>: Retrieval-Augmented Generation implementasyonu</li>
                            <li><strong>Embedding</strong>: AI destekli metin embedding'leri</li>
                            <li><strong>Chunking</strong>: Akıllı metin parçalama</li>
                            <li><strong>Vector Search</strong>: Vektör tabanlı arama</li>
                        </ul>
                    </div>

                    <h3>Sürüm 1.0.0 - 2025-08-15</h3>
                    <div class="alert alert-dark">
                        <h4><i class="fas fa-birthday-cake me-2"></i>İlk Sürüm</h4>
                        <ul class="mb-0">
                            <li><strong>SmartRAG</strong>: .NET için RAG kütüphanesi</li>
                            <li><strong>Temel Özellikler</strong>: Belge yükleme, işleme ve arama</li>
                            <li><strong>AI Entegrasyonu</strong>: OpenAI API desteği</li>
                            <li><strong>Basit Depolama</strong>: SQLite ile temel depolama</li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>

        <!-- Version Information Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Sürüm Bilgileri</h2>
                    <p>SmartRAG sürüm numaralandırması ve destek politikası hakkında bilgiler.</p>

                    <h3>Sürüm Numaralandırması</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-info-circle me-2"></i>Semantic Versioning</h4>
                        <p class="mb-0">SmartRAG, <a href="https://semver.org/" target="_blank">Semantic Versioning</a> kullanır:</p>
                        <ul class="mt-2 mb-0">
                            <li><strong>MAJOR</strong>: Geriye dönük uyumsuz API değişiklikleri</li>
                            <li><strong>MINOR</strong>: Geriye dönük uyumlu yeni özellikler</li>
                            <li><strong>PATCH</strong>: Geriye dönük uyumlu hata düzeltmeleri</li>
                        </ul>
                    </div>

                    <h3>Desteklenen Sürümler</h3>
                    <div class="row g-4">
                        <div class="col-md-4">
                            <div class="alert alert-success">
                                <h4><i class="fas fa-check-circle me-2"></i>Aktif</h4>
                                <p class="mb-0"><strong>1.1.x</strong><br>En son özellikler ve güvenlik güncellemeleri</p>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <div class="alert alert-warning">
                                <h4><i class="fas fa-shield-alt me-2"></i>LTS</h4>
                                <p class="mb-0"><strong>1.0.x</strong><br>Uzun süreli destek, sadece kritik hata düzeltmeleri</p>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <div class="alert alert-danger">
                                <h4><i class="fas fa-times-circle me-2"></i>Eski</h4>
                                <p class="mb-0"><strong>0.x.x</strong><br>Artık desteklenmiyor</p>
                            </div>
                        </div>
                    </div>

                    <h3>Geçiş Rehberleri</h3>
                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="card border-0 shadow-sm">
                                <div class="card-body text-center p-4">
                                    <i class="fas fa-arrow-right fa-2x text-primary mb-3"></i>
                                    <h5 class="card-title">1.0.x'den 1.1.x'e</h5>
                                    <p class="card-text">Excel desteği ve yeni özellikler için geçiş rehberi.</p>
                                    <a href="{{ site.baseurl }}/tr/migration/1.0-to-1.1" class="btn btn-primary">Geçiş Rehberi</a>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="card border-0 shadow-sm">
                                <div class="card-body text-center p-4">
                                    <i class="fas fa-arrow-up fa-2x text-success mb-3"></i>
                                    <h5 class="card-title">0.x.x'den 1.0.x'e</h5>
                                    <p class="card-text">İlk sürümden kararlı sürüme geçiş rehberi.</p>
                                    <a href="{{ site.baseurl }}/tr/migration/0.x-to-1.0" class="btn btn-success">Geçiş Rehberi</a>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!-- Feedback Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Geri Bildirim</h2>
                    <p>Sürümler hakkında geri bildirimlerinizi bizimle paylaşın.</p>

                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="alert alert-info">
                                <h4><i class="fas fa-github me-2"></i>GitHub Issues</h4>
                                <p class="mb-0">Hata raporları ve özellik istekleri için GitHub Issues kullanın.</p>
                                <a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank" class="btn btn-sm btn-outline-info mt-2">Issue Aç</a>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-success">
                                <h4><i class="fas fa-envelope me-2"></i>E-posta</h4>
                                <p class="mb-0">Doğrudan geri bildirim için e-posta gönderin.</p>
                                <a href="mailto:b.yerlikaya@outlook.com" class="btn btn-sm btn-outline-success mt-2">İletişim</a>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>
