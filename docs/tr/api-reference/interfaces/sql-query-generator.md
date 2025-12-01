---
layout: default
title: ISQLQueryGenerator
description: ISQLQueryGenerator arayüz dokümantasyonu
lang: tr
---

## ISQLQueryGenerator

**Amaç:** SQL sorguları oluşturma ve doğrulama

**Namespace:** `SmartRAG.Interfaces.Database`

Veritabanına özgü SQL için `ISqlDialectStrategy` kullanır.

#### Metodlar

##### GenerateDatabaseQueriesAsync

Intent'e göre her veritabanı için optimize edilmiş SQL sorguları oluşturur.

```csharp
Task<QueryIntent> GenerateDatabaseQueriesAsync(QueryIntent queryIntent)
```

**Parametreler:**
- `queryIntent` (QueryIntent): SQL oluşturulacak sorgu intent'i

**Döndürür:** Üretilmiş SQL sorguları ile güncellenmiş `QueryIntent`


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

