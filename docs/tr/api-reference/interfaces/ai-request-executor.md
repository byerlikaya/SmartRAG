---
layout: default
title: IAIRequestExecutor
description: IAIRequestExecutor arayüz dokümantasyonu
lang: tr
---

## IAIRequestExecutor

**Amaç:** Yeniden deneme/yedekleme ile AI istek yürütme

**Namespace:** `SmartRAG.Interfaces.AI`

Otomatik yeniden deneme ve yedekleme mantığı ile AI isteklerini işler.

#### Metodlar

##### GenerateResponseAsync

Belirtilen sağlayıcıyı kullanarak yanıt üretir.

```csharp
Task<string> GenerateResponseAsync(
    AIProvider provider, 
    string query, 
    IEnumerable<string> context
)
```

**Parametreler:**
- `provider` (AIProvider): Kullanılacak AI sağlayıcı
- `query` (string): Kullanıcı sorgusu
- `context` (IEnumerable<string>): Bağlam string'leri

**Döndürür:** AI tarafından üretilmiş metin yanıtı

##### GenerateEmbeddingsAsync

Belirtilen sağlayıcıyı kullanarak embedding'ler üretir.

```csharp
Task<List<float>> GenerateEmbeddingsAsync(
    AIProvider provider, 
    string text
)
```

**Parametreler:**
- `provider` (AIProvider): Kullanılacak AI sağlayıcı
- `text` (string): Embedding üretilecek metin

**Döndürür:** Embedding vektörü

##### GenerateEmbeddingsBatchAsync

Belirtilen sağlayıcıyı kullanarak toplu embedding'ler üretir.

```csharp
Task<List<List<float>>> GenerateEmbeddingsBatchAsync(
    AIProvider provider, 
    IEnumerable<string> texts
)
```

**Parametreler:**
- `provider` (AIProvider): Kullanılacak AI sağlayıcı
- `texts` (IEnumerable<string>): Metin koleksiyonu

**Döndürür:** Embedding vektörleri listesi


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

