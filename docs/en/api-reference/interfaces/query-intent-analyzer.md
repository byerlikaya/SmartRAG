---
layout: default
title: IQueryIntentAnalyzer
description: IQueryIntentAnalyzer interface documentation
lang: en
---
## IQueryIntentAnalyzer

**Purpose:** Query intent analysis for database routing

**Namespace:** `SmartRAG.Interfaces.Database`

Analyzes queries to determine database routing strategy.

#### Methods

```csharp
Task<QueryIntent> AnalyzeQueryIntentAsync(string userQuery);
```


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

