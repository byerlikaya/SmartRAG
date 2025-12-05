---
layout: default
title: API Referans
description: SmartRAG interface'leri, metodları ve modelleri için eksiksiz API dokümantasyonu
lang: tr
---

## API Referans

> **Not:** Tüm API detayları (interface'ler, modeller, enum'lar) kaynak kodda XML dokümantasyonu ile mevcuttur. Metod imzaları, parametreler, dönüş tipleri ve property tanımlarını keşfetmek için IDE'nizin IntelliSense özelliğini kullanın.

SmartRAG tüm işlemler için iyi tanımlanmış interface'ler, modeller ve enum'lar sağlar. Tüm kod elementleri aşağıdaki namespace'lerde bulunur:

- **`SmartRAG.Interfaces`** - Tüm servis interface'leri
- **`SmartRAG.Models`** - Veri modelleri (RagResponse, Document, DocumentChunk, vb.)
- **`SmartRAG.Enums`** - Enum'lar (AIProvider, StorageProvider, DatabaseType, vb.)

### API Detaylarını Görüntüleme

1. **IDE'nizde**: Herhangi bir class, interface veya enum'a "Go to Definition" (Visual Studio/VS Code'da F12) kullanarak gidin
2. **XML Dokümantasyonu**: Tüm kod elementleri parametre, dönüş tipi ve property bilgileriyle detaylı XML yorumları içerir
3. **Kaynak Kod**: Repository dizinlerini inceleyin:
   - `src/SmartRAG/Interfaces/` - Tüm interface'ler
   - `src/SmartRAG/Models/` - Tüm veri modelleri
   - `src/SmartRAG/Enums/` - Tüm enum'lar
4. **Örnekler**: Pratik kullanım için [Örnekler]({{ site.baseurl }}/tr/examples) bölümüne bakın

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
