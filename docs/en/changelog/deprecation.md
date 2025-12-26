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

### Deprecated in v3.0.0 (Removed in v4.0.0)

<div class="alert alert-warning">
    <h4><i class="fas fa-clock me-2"></i> Planned for Removal</h4>
    <p>The following methods are deprecated and will be removed in v4.0.0:</p>
    <ul class="mb-0">
        <li><code>IDocumentSearchService.GenerateRagAnswerAsync()</code> - Use <code>QueryIntelligenceAsync()</code> instead</li>
    </ul>
</div>

---