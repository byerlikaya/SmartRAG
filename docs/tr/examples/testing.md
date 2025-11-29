---
layout: default
title: Test Örnekleri
description: Test stratejileri ve örnekleri
lang: tr
---

## Test Örnekleri

### Unit Test Örneği

```csharp
[Test]
public async Task QueryIntelligenceAsync_ShouldReturnValidResponse_WhenValidQueryProvided()
{
    // Arrange
    var query = "Test sorgusu";
    var maxResults = 5;
    
    // Act
    var response = await _searchService.QueryIntelligenceAsync(query, maxResults);
    
    // Assert
    Assert.That(response, Is.Not.Null);
    Assert.That(response.Answer, Is.Not.Empty);
    Assert.That(response.Sources, Is.Not.Empty);
    Assert.That(response.Sources.Count, Is.LessThanOrEqualTo(maxResults));
}
```

---

## İlgili Örnekler

- [Örnekler Ana Sayfası]({{ site.baseurl }}/tr/examples) - Örnekler kategorilerine dön
