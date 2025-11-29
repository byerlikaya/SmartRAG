---
layout: default
title: IAIConfigurationService
description: IAIConfigurationService arayüz dokümantasyonu
lang: tr
---

## IAIConfigurationService

**Amaç:** AI sağlayıcı yapılandırma yönetimi

**Namespace:** `SmartRAG.Interfaces.AI`

Daha iyi SRP için yapılandırma yürütmeden ayrıldı.

#### Metodlar

```csharp
AIProvider GetProvider();
string GetModel();
string GetEmbeddingModel();
int GetMaxTokens();
double GetTemperature();
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

