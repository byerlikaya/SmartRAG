---
layout: default
title: IAIProvider
description: IAIProvider arayüz dokümantasyonu
lang: tr
---

## IAIProvider

**Amaç:** Metin üretimi ve embedding'ler için düşük seviye AI sağlayıcı arayüzü

**Namespace:** `SmartRAG.Interfaces.AI`

Birden fazla AI backend için sağlayıcı soyutlaması.

#### Metodlar

##### GenerateTextAsync

AI sağlayıcıyı kullanarak metin yanıtı üretir.

```csharp
Task<string> GenerateTextAsync(string prompt, AIProviderConfig config)
```

**Parametreler:**
- `prompt` (string): AI sağlayıcıya gönderilecek metin prompt'u
- `config` (AIProviderConfig): AI sağlayıcı yapılandırma ayarları

**Döndürür:** AI tarafından üretilmiş metin yanıtı

##### GenerateEmbeddingAsync

Verilen metin için embedding vektörü üretir.

```csharp
Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config)
```

**Parametreler:**
- `text` (string): Embedding üretilecek metin
- `config` (AIProviderConfig): AI sağlayıcı yapılandırma ayarları

**Döndürür:** Embedding vektörünü temsil eden float değerleri listesi

##### GenerateEmbeddingsBatchAsync

Tek bir istekte birden fazla metin için embedding'ler üretir (destekleniyorsa).

```csharp
Task<List<List<float>>> GenerateEmbeddingsBatchAsync(
    IEnumerable<string> texts, 
    AIProviderConfig config
)
```

**Parametreler:**
- `texts` (IEnumerable<string>): Embedding üretilecek metin koleksiyonu
- `config` (AIProviderConfig): AI sağlayıcı yapılandırma ayarları

**Döndürür:** Her girdi metni için bir embedding vektörü listesi

##### ChunkTextAsync

Metni işleme için daha küçük parçalara böler.

```csharp
Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000)
```

**Parametreler:**
- `text` (string): Bölünecek metin
- `maxChunkSize` (int): Her chunk'ın maksimum karakter boyutu (varsayılan: 1000)

**Döndürür:** Metin chunk'ları listesi


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

