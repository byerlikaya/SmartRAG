---
layout: default
title: Strateji Deseni Arayüzleri
description: SQL diyalektleri, skorlama ve dosya ayrıştırma için özelleştirilebilir stratejiler
lang: tr
---

## Strateji Deseni Arayüzleri

SmartRAG genişletilebilirlik ve özelleştirme için Strateji Deseni'ni sağlar.

<div class="row g-4 mt-4">
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-code"></i>
            </div>
            <h3>ISqlDialectStrategy</h3>
            <p>Veritabanına özel SQL üretimi ve doğrulama</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/sql-dialect-strategy" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-star"></i>
            </div>
            <h3>IScoringStrategy</h3>
            <p>Özelleştirilebilir doküman ilgili skorlama</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/scoring-strategy" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-file"></i>
            </div>
            <h3>IFileParser</h3>
            <p>Belirli dosya formatlarını ayrıştırma stratejisi</p>
            <a href="{{ site.baseurl }}/tr/api-reference/interfaces/file-parser" class="btn btn-outline-primary btn-sm mt-3">
                Arayüzü Görüntüle
            </a>
        </div>
    </div>
</div>

## İlgili Kategoriler

<div class="row g-4 mt-4">
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-cube"></i>
            </div>
            <h3>Temel Arayüzler</h3>
            <p>Tüm temel arayüzleri görüntüle</p>
            <a href="{{ site.baseurl }}/tr/api-reference/core" class="btn btn-outline-primary btn-sm mt-3">
                Temel Arayüzler
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-rocket"></i>
            </div>
            <h3>Gelişmiş Arayüzler</h3>
            <p>Çoklu veritabanı koordinasyonu ve gelişmiş özellikler</p>
            <a href="{{ site.baseurl }}/tr/api-reference/advanced" class="btn btn-outline-primary btn-sm mt-3">
                Gelişmiş Arayüzler
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-wrench"></i>
            </div>
            <h3>Servis Arayüzleri</h3>
            <p>Repository'ler, factory'ler ve yardımcılar için ek servis arayüzleri</p>
            <a href="{{ site.baseurl }}/tr/api-reference/services" class="btn btn-outline-primary btn-sm mt-3">
                Servis Arayüzleri
            </a>
        </div>
    </div>
</div>

