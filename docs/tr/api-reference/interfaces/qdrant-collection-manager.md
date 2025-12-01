---
layout: default
title: IQdrantCollectionManager
description: IQdrantCollectionManager arayüz dokümantasyonu
lang: tr
---

## IQdrantCollectionManager

**Amaç:** Qdrant koleksiyonlarını ve doküman depolamayı yönetmek için arayüz

**Namespace:** `SmartRAG.Interfaces.Storage.Qdrant`

Qdrant vektör veritabanı için koleksiyon yaşam döngüsü yönetimi.

#### Metodlar

##### EnsureCollectionExistsAsync

Ana koleksiyonun var olduğundan ve işlemler için hazır olduğundan emin olur.

```csharp
Task EnsureCollectionExistsAsync()
```

##### CreateCollectionAsync

Belirtilen vektör parametreleri ile yeni bir koleksiyon oluşturur.

```csharp
Task CreateCollectionAsync(string collectionName, int vectorDimension)
```

**Parametreler:**
- `collectionName` (string): Oluşturulacak koleksiyon adı
- `vectorDimension` (int): Depolanacak vektörlerin boyutu

##### EnsureDocumentCollectionExistsAsync

Doküman-spesifik bir koleksiyonun var olduğundan emin olur.

```csharp
Task EnsureDocumentCollectionExistsAsync(
    string collectionName, 
    Document document
)
```

**Parametreler:**
- `collectionName` (string): Doküman koleksiyon adı
- `document` (Document): Depolanacak doküman

##### GetVectorDimensionAsync

Koleksiyonlar için vektör boyutunu alır.

```csharp
Task<int> GetVectorDimensionAsync()
```

**Döndürür:** Vektör boyutu

##### DeleteCollectionAsync

Bir koleksiyonu tamamen siler.

```csharp
Task DeleteCollectionAsync(string collectionName)
```

**Parametreler:**
- `collectionName` (string): Silinecek koleksiyon adı

##### RecreateCollectionAsync

Bir koleksiyonu yeniden oluşturur (siler ve yeniden oluşturur).

```csharp
Task RecreateCollectionAsync(string collectionName)
```

**Parametreler:**
- `collectionName` (string): Yeniden oluşturulacak koleksiyon adı


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

