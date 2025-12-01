---
layout: default
title: Migration Guides
description: Step-by-step migration guides for SmartRAG
lang: en
---

## Migration Guides

<p>Step-by-step migration guides for upgrading SmartRAG versions.</p>

---

### Migrating from v2.x to v3.0.0

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Key Changes</h4>
    <p class="mb-0">The primary change is the renaming of <code>GenerateRagAnswerAsync</code> to <code>QueryIntelligenceAsync</code>.</p>
</div>

<p>This migration guide covers the changes needed when upgrading from SmartRAG v2.x to v3.0.0.</p>

#### Step 1: Update Method Call

<p>Update your service method call from <code>GenerateRagAnswerAsync</code> to <code>QueryIntelligenceAsync</code>:</p>

```csharp
// Before (v2.x)
var response = await _searchService.GenerateRagAnswerAsync(query, maxResults);

// After (v3.0.0)
var response = await _searchService.QueryIntelligenceAsync(query, maxResults);
```

#### Step 2: Update API Endpoints (if using Web API)

<p>If you have a Web API controller, simply update the service method call:</p>

```csharp
// Before (v2.x)
[HttpPost("generate-answer")]
public async Task<IActionResult> GenerateAnswer([FromBody] QueryRequest request)
{
    var response = await _searchService.GenerateRagAnswerAsync(request.Query);
    return Ok(response);
}

// After (v3.0.0) - Only the method name changed
[HttpPost("generate-answer")]
public async Task<IActionResult> GenerateAnswer([FromBody] QueryRequest request)
{
    var response = await _searchService.QueryIntelligenceAsync(request.Query);
    return Ok(response);
}
```

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Note</h4>
    <p class="mb-0">You can keep your existing endpoint paths and controller method names. Only the service method call needs to be updated.</p>
</div>

#### Step 3: Update Client Code (if applicable)

<p>If you have client code that calls the API, update the endpoint:</p>

```javascript
// Before
const response = await fetch('/api/intelligence/generate-answer', { ... });

// After
const response = await fetch('/api/intelligence/query', { ... });
```

<div class="alert alert-success">
    <h4><i class="fas fa-check-circle me-2"></i> No Immediate Action Required</h4>
    <p class="mb-0">
        The old <code>GenerateRagAnswerAsync</code> method still works (marked as deprecated). 
        You can migrate gradually before v4.0.0 is released.
    </p>
</div>

---

### Migrating from v1.x to v2.0.0

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Framework Change</h4>
    <p class="mb-0">Version 2.0.0 migrated from .NET 9.0 to .NET Standard 2.1</p>
</div>

<p>This migration guide covers the framework compatibility changes when upgrading from SmartRAG v1.x to v2.0.0.</p>

#### Step 1: Verify Framework Compatibility

<p>Your project must target one of these frameworks:</p>

```xml
<TargetFramework>netstandard2.0</TargetFramework>
<TargetFramework>netstandard2.1</TargetFramework>
<TargetFramework>netcoreapp2.0</TargetFramework>
<TargetFramework>net461</TargetFramework>
<TargetFramework>net5.0</TargetFramework>
<TargetFramework>net6.0</TargetFramework>
<TargetFramework>net7.0</TargetFramework>
<TargetFramework>net8.0</TargetFramework>
<TargetFramework>net9.0</TargetFramework>
```

#### Step 2: Update NuGet Package

<p>Update the SmartRAG package to version 2.0.0:</p>

```bash
dotnet add package SmartRAG --version 2.0.0
```

#### Step 3: Verify Code Compatibility

<p>No API changes - all functionality remains the same. Just ensure your project targets compatible framework.</p>

---

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-history"></i>
            </div>
            <h3>Version History</h3>
            <p>Complete version history with all releases and changes</p>
            <a href="{{ site.baseurl }}/en/changelog/version-history" class="btn btn-outline-primary btn-sm mt-3">
                Version History
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-exclamation-triangle"></i>
            </div>
            <h3>Deprecation Notices</h3>
            <p>Deprecated features and planned removals</p>
            <a href="{{ site.baseurl }}/en/changelog/deprecation" class="btn btn-outline-primary btn-sm mt-3">
                Deprecation Notices
            </a>
        </div>
    </div>
</div>
