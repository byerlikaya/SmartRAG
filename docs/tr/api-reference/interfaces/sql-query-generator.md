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

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

