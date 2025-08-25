---
layout: default
title: Değişiklik Günlüğü
description: SmartRAG sürümlerinde yapılan değişiklikler, yeni özellikler ve hata düzeltmeleri
lang: tr
---

# 📋 Değişiklik Günlüğü

SmartRAG projesinde yapılan tüm önemli değişiklikler bu sayfada takip edilir.

## 🚀 [1.1.0] - 2025-08-22

### ✨ Yeni Özellikler
- **Excel Dosya Desteği**: Excel dosya işleme (.xlsx, .xls) EPPlus 8.1.0 entegrasyonu ile
- **Gelişmiş Retry Mantığı**: HTTP 529 (Overloaded) hataları için Anthropic API retry mekanizması
- **İçerik Doğrulama**: Gelişmiş belge içerik doğrulama
- **Excel Dokümantasyonu**: Kapsamlı Excel format dokümantasyonu

## 🚀 [1.0.3] - 2025-08-20

### ✨ Yeni Özellikler
- **Çoklu Dil Desteği**: Türkçe, Almanca, Rusça dil desteği eklendi
- **GitHub Pages Entegrasyonu**: Otomatik dokümantasyon sitesi
- **Gelişmiş SEO**: Meta etiketleri ve yapılandırılmış veri desteği
- **Responsive Tasarım**: Mobil cihazlarda mükemmel görünüm

### 🔧 İyileştirmeler
- **Dokümantasyon**: Kapsamlı API referansı ve örnekler
- **Navigasyon**: Dile bağlı menü ve link sistemi
- **Performans**: Hızlı sayfa yükleme ve optimizasyon

### 🐛 Hata Düzeltmeleri
- **Layout Sorunları**: Çoklu dil desteği için layout düzeltmeleri
- **Link Sorunları**: İç sayfa linklerinin düzeltilmesi
- **Build Hataları**: Jekyll build sorunlarının çözülmesi

## 🚀 [1.0.2] - 2025-08-19

### ✨ Yeni Özellikler
- **GlobalUsings Desteği**: C# 10 GlobalUsings özelliği
- **Test Projesi**: xUnit test senaryoları
- **API Projesi**: Web API örnek uygulaması

### 🔧 İyileştirmeler
- **Kod Organizasyonu**: SOLID ve DRY prensipleri
- **Logging**: ILogger entegrasyonu
- **Error Handling**: Gelişmiş hata yönetimi

### 🐛 Hata Düzeltmeleri
- **Type Conflicts**: Document tipi çakışmalarının çözülmesi
- **Dependency Issues**: NuGet paket bağımlılık sorunları

## 🚀 [1.0.1] - 2025-08-17

### ✨ Yeni Özellikler
- **AI Provider Entegrasyonu**: OpenAI, Anthropic, Azure OpenAI, Gemini
- **Storage Provider Desteği**: Qdrant, Redis, SQLite, In-Memory, File System
- **Document Processing**: Word, PDF, Excel, Text format desteği

### 🔧 İyileştirmeler
- **Performance**: Embedding üretimi optimizasyonu
- **Scalability**: Çoklu thread desteği
- **Reliability**: Hata toleransı ve retry mekanizmaları

## 🚀 [1.0.0] - 2025-08-15

### ✨ İlk Sürüm
- **Core Library**: SmartRAG temel kütüphanesi
- **Document Service**: Belge yükleme ve işleme
- **Embedding Generation**: AI destekli embedding üretimi
- **Semantic Search**: Anlamsal arama yetenekleri
- **Vector Storage**: Vektör veritabanı entegrasyonu

### 🔧 Temel Özellikler
- **Multi-format Support**: Word, PDF, Excel, Text
- **AI Integration**: OpenAI, Anthropic, Azure OpenAI, Gemini
- **Storage Backends**: Qdrant, Redis, SQLite, In-Memory, File System
- **Extensible Architecture**: Plugin sistemi ve özel provider desteği

---

## 📝 Sürüm Numaralandırma

SmartRAG [Semantic Versioning](https://semver.org/) kullanır:

- **MAJOR**: Geriye uyumsuz API değişiklikleri
- **MINOR**: Geriye uyumlu yeni özellikler
- **PATCH**: Geriye uyumlu hata düzeltmeleri

## 🔗 İlgili Bağlantılar

- [GitHub Releases](https://github.com/byerlikaya/SmartRAG/releases)
- [NuGet Package](https://www.nuget.org/packages/SmartRAG)
- [API Reference]({{ site.baseurl }}/tr/api-reference)
- [Getting Started]({{ site.baseurl }}/tr/getting-started)

---

<div class="text-center mt-5">
    <p class="text-muted">
        <i class="fas fa-info-circle me-2"></i>
        Daha fazla bilgi için <a href="https://github.com/byerlikaya/SmartRAG">GitHub repository</a>'mizi ziyaret edin.
    </p>
</div>
