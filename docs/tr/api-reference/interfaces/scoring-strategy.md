---
layout: default
title: IScoringStrategy
description: IScoringStrategy arayüz dokümantasyonu
lang: tr
---

## IScoringStrategy

**Amaç:** Özelleştirilebilir doküman ilgililik skorlaması

**Namespace:** `SmartRAG.Interfaces.Search.Strategies`

Arama sonuçları için özel skorlama algoritmaları sağlar.

#### Metodlar

##### CalculateScoreAsync

Bir doküman parçası için ilgililik skoru hesaplar.

```csharp
Task<double> CalculateScoreAsync(
    string query, 
    DocumentChunk chunk, 
    List<float> queryEmbedding
)
```

**Parametreler:**
- `query` (string): Arama sorgusu
- `chunk` (DocumentChunk): Skorlanacak doküman parçası
- `queryEmbedding` (List<float>): Sorgu embedding vektörü

**Döndürür:** 0.0 ile 1.0 arasında skor

#### Yerleşik Uygulama

**HybridScoringStrategy** (varsayılan):
- %80 semantik benzerlik (embedding'lerin kosinüs benzerliği)
- %20 anahtar kelime eşleşmesi (BM25 benzeri skorlama)

#### Özel Uygulama Örneği

```csharp
public class SemanticOnlyScoringStrategy : IScoringStrategy
{
    public async Task<double> CalculateScoreAsync(
        string query, 
        DocumentChunk chunk, 
        List<float> queryEmbedding)
    {
        // Saf semantik benzerlik (%100 embedding tabanlı)
        return CosineSimilarity(queryEmbedding, chunk.Embedding);
    }
    
    private double CosineSimilarity(List<float> a, List<float> b)
    {
        double dotProduct = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Count; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

