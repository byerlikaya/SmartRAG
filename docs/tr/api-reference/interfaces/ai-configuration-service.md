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

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

