---
layout: default
title: Migration Guides
description: Step-by-step migration guides for SmartRAG
lang: en
---

## Migration Guides

### Migrating from v2.x to v3.0.0
                    
<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Key Changes</h4>
    <p class="mb-0">The primary change is the renaming of <code>GenerateRagAnswerAsync</code> to <code>QueryIntelligenceAsync</code>.</p>
</div>

**Step 1: Update method calls**

```csharp
// Before (v2.x)
var response = await _searchService.GenerateRagAnswerAsync(query, maxResults);

// After (v3.0.0)
var response = await _searchService.QueryIntelligenceAsync(query, maxResults);
```

**Step 2: Update API endpoints (if using Web API)**

If you have a Web API controller, simply update the service method call:

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

**Note:** You can keep your existing endpoint paths and controller method names. Only the service method call needs to be updated.
```

**Step 3: Update client code (if applicable)**

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

### Migrating from v1.x to v2.0.0

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Framework Change</h4>
    <p class="mb-0">Version 2.0.0 migrated from .NET 9.0 to .NET Standard 2.1</p>
</div>

**Step 1: Verify framework compatibility**

```xml
<!-- Your project must target one of these frameworks -->
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

**Step 2: Update NuGet package**

```bash
dotnet add package SmartRAG --version 2.0.0
```

**Step 3: Verify code compatibility**

No API changes - all functionality remains the same. Just ensure your project targets compatible framework.

---