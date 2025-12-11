---
layout: default
title: Temel Arayüzler
description: Doküman arama, yönetim, ayrıştırma ve AI servisleri için temel SmartRAG arayüzleri
lang: tr
---

## Temel Arayüzler

> **Not:** Interface detayları kaynak kodda XML dokümantasyonu ile mevcuttur. Metod imzaları, parametreler ve dönüş tiplerini keşfetmek için IDE'nizin IntelliSense özelliğini kullanın. Pratik kullanım örnekleri için [Örnekler]({{ site.baseurl }}/tr/examples) bölümüne bakın.

Tüm interface'ler `SmartRAG.Interfaces` namespace'inde bulunur. Interface tanımlarını görüntülemek için:

1. **IDE'nizde**: "Go to Definition" (Visual Studio/VS Code'da F12) kullanarak interface'e gidin
2. **XML Dokümantasyonu**: Tüm interface'ler parametre ve dönüş tipi bilgileriyle detaylı XML yorumları içerir
3. **Kaynak Kod**: Repository'deki `src/SmartRAG/Interfaces/` dizinini inceleyin
4. **Örnekler**: Pratik kullanım için [Örnekler]({{ site.baseurl }}/tr/examples) bölümüne bakın

## Ana Public Interface'ler

- **`IDocumentSearchService`** - RAG pipeline ve konuşma yönetimi ile AI destekli akıllı sorgu işleme
- **`IDocumentService`** - Doküman CRUD işlemleri ve yönetimi
- **`IConversationManagerService`** - Konuşma oturumu yönetimi ve geçmiş takibi
- **`IDocumentParserService`** - Çoklu format doküman ayrıştırma ve metin çıkarma
- **`IDatabaseParserService`** - Canlı bağlantılarla evrensel veritabanı desteği
- **`IAIService`** - Metin üretimi ve embedding'ler için AI sağlayıcı iletişimi
- **`IStorageFactory`** - Depolama repository'leri oluşturmak için fabrika
- **`IPromptBuilderService`** - AI prompt'ları oluşturmak için servis

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-lightbulb"></i>
            </div>
            <h3>Örnekler</h3>
            <p>Pratik kod örnekleri ve gerçek dünya uygulamalarını görün</p>
            <a href="{{ site.baseurl }}/tr/examples/quick" class="btn btn-outline-primary btn-sm mt-3">
                Örnekleri Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-rocket"></i>
            </div>
            <h3>Başlangıç</h3>
            <p>Hızlı kurulum ve kurulum rehberi</p>
            <a href="{{ site.baseurl }}/tr/getting-started" class="btn btn-outline-primary btn-sm mt-3">
                Başlangıç
            </a>
        </div>
    </div>
</div>
