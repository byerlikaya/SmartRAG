---
layout: default
title: ISqlDialectStrategy
description: ISqlDialectStrategy arayüz dokümantasyonu
lang: tr
---

## ISqlDialectStrategy

**Amaç:** Veritabanına özgü SQL üretimi ve doğrulama

**Namespace:** `SmartRAG.Interfaces.Database.Strategies`

Veritabanına özgü SQL optimizasyonu ve özel veritabanı desteği sağlar.

#### Özellikler

```csharp
DatabaseType DatabaseType { get; }
```

#### Metodlar

##### BuildSystemPrompt

Bu veritabanı diyalektine özgü SQL üretimi için AI sistem prompt'u oluşturur.

```csharp
string BuildSystemPrompt(DatabaseSchemaInfo schema, string userQuery)
```

##### ValidateSyntax

Bu belirli diyalekt için SQL sözdizimini doğrular.

```csharp
bool ValidateSyntax(string sql, out string errorMessage)
```

##### FormatSql

SQL sorgusunu diyalekte özgü kurallara göre biçimlendirir.

```csharp
string FormatSql(string sql)
```

##### GetLimitClause

Bu diyalekt için LIMIT cümlesi formatını alır.

```csharp
string GetLimitClause(int limit)
```

**Döndürür:**
- SQLite/MySQL: `LIMIT {limit}`
- SQL Server: `TOP {limit}`
- PostgreSQL: `LIMIT {limit}`

#### Yerleşik Uygulamalar

- `SqliteDialectStrategy` - SQLite için optimize edilmiş SQL
- `PostgreSqlDialectStrategy` - PostgreSQL için optimize edilmiş SQL
- `MySqlDialectStrategy` - MySQL/MariaDB için optimize edilmiş SQL
- `SqlServerDialectStrategy` - SQL Server için optimize edilmiş SQL

#### Özel Uygulama Örneği

```csharp
// Örnek: Belirli bir veritabanı varyantı için özel diyalekt
public class CustomPostgreSqlDialectStrategy : BaseSqlDialectStrategy
{
    public override DatabaseType DatabaseType => DatabaseType.PostgreSQL;
    
    public override string BuildSystemPrompt(DatabaseSchemaInfo schema, string userQuery)
    {
        return $"PostgreSQL SQL oluştur: {userQuery}\\nŞema: {schema}";
    }
    
    public override bool ValidateSyntax(string sql, out string errorMessage)
    {
        // PostgreSQL'e özgü doğrulama
        errorMessage = null;
        
        // Örnek: PostgreSQL'e özgü sözdizimi kontrolü
        if (sql.Contains("LIMIT") && !sql.Contains("OFFSET"))
        {
            // Geçerli PostgreSQL sözdizimi
            return true;
        }
        
        return true;
    }
    
    public override string FormatSql(string sql)
    {
        // PostgreSQL'e özgü biçimlendirme (opsiyonel)
        return sql;
    }
    
    public override string GetLimitClause(int limit)
    {
        return $"LIMIT {limit}";
    }
}
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

