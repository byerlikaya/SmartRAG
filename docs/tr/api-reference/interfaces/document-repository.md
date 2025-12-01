---
layout: default
title: IDocumentRepository
description: IDocumentRepository arayüz dokümantasyonu
lang: tr
---

## IDocumentRepository

**Amaç:** Doküman depolama işlemleri için repository arayüzü

**Namespace:** `SmartRAG.Interfaces.Document`

İş mantığından ayrılmış repository katmanı.

#### Metodlar

##### AddAsync

Depolamaya yeni bir doküman ekler.

```csharp
Task<Document> AddAsync(Document document)
```

**Parametreler:**
- `document` (Document): Eklenecek doküman varlığı

**Döndürür:** Eklenen doküman varlığı

##### GetByIdAsync

Benzersiz tanımlayıcıya göre dokümanı alır.

```csharp
Task<Document> GetByIdAsync(Guid id)
```

**Parametreler:**
- `id` (Guid): Benzersiz doküman tanımlayıcısı

**Döndürür:** Doküman varlığı veya bulunamazsa null

##### GetAllAsync

Depolamadan tüm dokümanları alır.

```csharp
Task<List<Document>> GetAllAsync()
```

**Döndürür:** Tüm doküman varlıklarının listesi

##### DeleteAsync

ID'ye göre depolamadan dokümanı kaldırır.

```csharp
Task<bool> DeleteAsync(Guid id)
```

**Parametreler:**
- `id` (Guid): Benzersiz doküman tanımlayıcısı

**Döndürür:** Doküman başarıyla silindiyse true

##### GetCountAsync

Depolamadaki toplam doküman sayısını alır.

```csharp
Task<int> GetCountAsync()
```

**Döndürür:** Toplam doküman sayısı

##### SearchAsync

Sorgu string'i kullanarak dokümanları arar.

```csharp
Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = 5)
```

**Parametreler:**
- `query` (string): Arama sorgu string'i
- `maxResults` (int): Döndürülecek maksimum sonuç sayısı (varsayılan: 5)

**Döndürür:** İlgili doküman chunk'larının listesi

##### ClearAllAsync

Depolamadan tüm dokümanları temizler (verimli toplu silme).

```csharp
Task<bool> ClearAllAsync()
```

**Döndürür:** Tüm dokümanlar başarıyla temizlendiyse true


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

