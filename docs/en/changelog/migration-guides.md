---
layout: default
title: Migration Guides
description: Step-by-step migration guides for SmartRAG
lang: en
---

## Migration Guides

<p>Step-by-step migration guides for upgrading SmartRAG versions.</p>

---

### Migrating from v3.x to v4.0.0

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Breaking Changes</h4>
    <p class="mb-0">v4.0.0 targets .NET 6 and merges the SmartRAG.Dashboard project into the main SmartRAG package.</p>
</div>

<p>This migration guide covers the framework and project structure changes when upgrading from SmartRAG v3.x to v4.0.0.</p>

<p><strong>Note:</strong> SmartRAG.Dashboard was never published as a NuGet package. In v3.x it existed as a separate project within the solution; in v4.0 that project was removed and the Dashboard code is now included in the SmartRAG package.</p>

#### Step 1: Update Target Framework

<p>Your project must target .NET 6 or higher:</p>

```xml
<TargetFramework>net6.0</TargetFramework>
<!-- or net7.0, net8.0, net9.0 -->
```

<p>Projects targeting .NET Core 3.0, .NET 5, or .NET Standard 2.1 must upgrade to at least .NET 6.</p>

#### Step 2: Remove SmartRAG.Dashboard Project Reference

<p>The Dashboard is now included in the SmartRAG package. If you had a ProjectReference to the SmartRAG.Dashboard project, remove it:</p>

```xml
<!-- Remove this line -->
<ProjectReference Include="..\..\src\SmartRAG.Dashboard\SmartRAG.Dashboard.csproj" />
```

<p>If you only reference the SmartRAG NuGet package or the main SmartRAG project, no changes are needed.</p>

#### Step 3: Keep Using the Same API

<p>No code changes needed. The Dashboard API remains the same:</p>

```csharp
using SmartRAG.Dashboard;

builder.Services.AddSmartRag(builder.Configuration);
builder.Services.AddSmartRagDashboard(options => { options.Path = "/smartrag"; });

app.UseSmartRagDashboard("/smartrag");
app.MapSmartRagDashboard("/smartrag");
```

<p>The <code>SmartRAG.Dashboard</code> namespace and extension methods are now part of the SmartRAG package.</p>

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

### Migrating from v3.8.x to v3.9.0

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Breaking Changes</h4>
    <p class="mb-0">v3.9.0 contains breaking changes for <code>IStorageFactory</code>, <code>IConversationRepository</code>, and removes <code>IQdrantCacheManager</code>.</p>
</div>

#### Step 1: Update IStorageFactory Usage

<p>If you inject <code>IStorageFactory</code> and call <code>GetCurrentRepository()</code>, pass the scoped <code>IServiceProvider</code>:</p>

```csharp
// Before (v3.8.x)
var repository = _storageFactory.GetCurrentRepository();

// After (v3.9.0)
var repository = _storageFactory.GetCurrentRepository(_serviceProvider);
```

<p>When using DI, the registration already passes the scoped provider. If you resolve <code>IDocumentRepository</code> directly, no change is needed.</p>

#### Step 2: Update Custom IConversationRepository Implementations

<p>If you have a custom <code>IConversationRepository</code> implementation, add these methods:</p>

```csharp
Task AppendSourcesForTurnAsync(string sessionId, string sourcesJson, CancellationToken cancellationToken = default);
Task<string> GetSourcesForSessionAsync(string sessionId, CancellationToken cancellationToken = default);
Task<string[]> GetAllSessionIdsAsync(CancellationToken cancellationToken = default);
```

<p>Built-in implementations (Sqlite, Redis, FileSystem, InMemory) already include these.</p>

#### Step 3: Remove IQdrantCacheManager References (if used)

<p><code>IQdrantCacheManager</code> and <code>QdrantCacheManager</code> were removed. Search no longer uses query result caching. Remove any references to these types.</p>

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
<TargetFramework>net6.0</TargetFramework>
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
