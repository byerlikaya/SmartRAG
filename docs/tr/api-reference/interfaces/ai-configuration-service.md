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

##### GetAIProviderConfig

Şu anda yapılandırılmış sağlayıcı için AI sağlayıcı yapılandırmasını alır.

```csharp
AIProviderConfig? GetAIProviderConfig()
```

**Döndürür:** AI sağlayıcı yapılandırması veya yapılandırılmamışsa null

##### GetProviderConfig

Belirli bir sağlayıcı için AI sağlayıcı yapılandırmasını alır.

```csharp
AIProviderConfig? GetProviderConfig(AIProvider provider)
```

**Parametreler:**
- `provider` (AIProvider): Yapılandırması alınacak AI sağlayıcı

**Döndürür:** AI sağlayıcı yapılandırması veya bulunamazsa null


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

