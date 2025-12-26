---
layout: default
title: Kullanımdan Kaldırma Bildirimleri
description: SmartRAG için kullanımdan kaldırılan özellikler ve planlanan kaldırmalar
lang: tr
---

## Kullanımdan Kaldırma Bildirimleri

### v3.5.0'da Breaking Changes

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Hemen Güncelleme Gerekli</h4>
    <p>Aşağıdaki değişiklikler hemen güncelleme gerektirir:</p>
    <ul class="mb-0">
        <li><code>ISQLQueryGenerator</code> → <code>ISqlQueryGenerator</code> - İsimlendirme tutarlılığı için interface yeniden adlandırıldı (sadece doğrudan interface kullananlar)</li>
        <li>Interface metodlarından <code>preferredLanguage</code> parametresi kaldırıldı - Bunun yerine <code>SearchOptions</code> kullanın</li>
    </ul>
</div>

### v3.0.0'da Kullanımdan Kaldırıldı (v4.0.0'da Kaldırılacak)

<div class="alert alert-warning">
    <h4><i class="fas fa-clock me-2"></i> Kaldırma Planlandı</h4>
    <p>Aşağıdaki metodlar kullanımdan kaldırıldı ve v4.0.0'da kaldırılacak:</p>
    <ul class="mb-0">
        <li><code>IDocumentSearchService.GenerateRagAnswerAsync()</code> - Yerine <code>QueryIntelligenceAsync()</code> kullanın</li>
    </ul>
</div>

---