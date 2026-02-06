---
layout: default
title: Deprecation Notices
description: Deprecated features and planned removals for SmartRAG
lang: en
---

## Deprecation Notices

### Breaking Changes in v3.5.0

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Immediate Action Required</h4>
    <p>The following changes require immediate updates:</p>
    <ul class="mb-0">
        <li><code>ISQLQueryGenerator</code> → <code>ISqlQueryGenerator</code> - Interface renamed for naming consistency (direct interface users only)</li>
        <li><code>preferredLanguage</code> parameter removed from interface methods - Use <code>SearchOptions</code> instead</li>
    </ul>
</div>

### Removed in v4.0.0

<div class="alert alert-danger">
    <h4><i class="fas fa-times-circle me-2"></i> Breaking Changes</h4>
    <p>The following methods were removed in v4.0.0. Migrate to the replacements:</p>
    <ul class="mb-0">
        <li><code>IDocumentSearchService.GenerateRagAnswerAsync()</code> → <code>QueryIntelligenceAsync()</code></li>
        <li><code>IRagAnswerGeneratorService.GenerateBasicRagAnswerAsync(string, int, ...)</code> → <code>GenerateBasicRagAnswerAsync(GenerateRagAnswerRequest)</code></li>
        <li><code>IQueryStrategyExecutorService</code> overloads with individual params → Use <code>QueryStrategyRequest</code> overloads</li>
        <li><code>IDocumentService.UploadDocumentAsync(Stream, string, ...)</code> → <code>UploadDocumentAsync(UploadDocumentRequest)</code></li>
        <li><code>IMultiDatabaseQueryCoordinator.AnalyzeQueryIntentAsync()</code> → <code>IQueryIntentAnalyzer.AnalyzeQueryIntentAsync()</code></li>
    </ul>
</div>

---