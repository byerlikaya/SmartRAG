---
layout: default
title: ITextNormalizationService
description: ITextNormalizationService interface documentation
lang: en
---
## ITextNormalizationService

**Purpose:** Text normalization and cleaning

**Namespace:** `SmartRAG.Interfaces.Support`

#### Methods

##### NormalizeText

Normalizes text for better search matching (handles Unicode encoding issues).

```csharp
string NormalizeText(string text)
```

**Parameters:**
- `text` (string): Text to normalize

**Returns:** Normalized text

##### NormalizeForMatching

Normalizes text for matching purposes (removes control characters and normalizes whitespace).

```csharp
string NormalizeForMatching(string value)
```

**Parameters:**
- `value` (string): Text to normalize

**Returns:** Normalized text

##### ContainsNormalizedName

Checks if content contains normalized name (handles encoding issues).

```csharp
bool ContainsNormalizedName(string content, string searchName)
```

**Parameters:**
- `content` (string): Content to search in
- `searchName` (string): Name to search for

**Returns:** True if name is found in content

##### SanitizeForLog

Sanitizes user input for safe logging by removing control characters and limiting length.

```csharp
string SanitizeForLog(string input)
```

**Parameters:**
- `input` (string): Input to sanitize

**Returns:** Sanitized input


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

