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

```csharp
Task<string> GenerateSqlAsync(string userQuery, DatabaseSchemaInfo schema, DatabaseType databaseType);
bool ValidateSql(string sql, DatabaseSchemaInfo schema, out string errorMessage);
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

