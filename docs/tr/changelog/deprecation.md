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

### v4.0.0'da Kaldırıldı

<div class="alert alert-danger">
    <h4><i class="fas fa-times-circle me-2"></i> Kırıcı Değişiklikler</h4>
    <p>Aşağıdaki metodlar v4.0.0'da kaldırıldı. Yerine geçen metodlara taşının:</p>
    <ul class="mb-0">
        <li><code>IDocumentSearchService.GenerateRagAnswerAsync()</code> → <code>QueryIntelligenceAsync()</code></li>
        <li><code>IRagAnswerGeneratorService.GenerateBasicRagAnswerAsync(string, int, ...)</code> → <code>GenerateBasicRagAnswerAsync(GenerateRagAnswerRequest)</code></li>
        <li><code>IQueryStrategyExecutorService</code> ayrı parametreli overload'lar → <code>QueryStrategyRequest</code> overload'larını kullanın</li>
        <li><code>IDocumentService.UploadDocumentAsync(Stream, string, ...)</code> → <code>UploadDocumentAsync(UploadDocumentRequest)</code></li>
        <li><code>IMultiDatabaseQueryCoordinator.AnalyzeQueryIntentAsync()</code> → <code>IQueryIntentAnalyzer.AnalyzeQueryIntentAsync()</code></li>
    </ul>
</div>

---