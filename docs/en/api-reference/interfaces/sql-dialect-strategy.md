---
layout: default
title: ISqlDialectStrategy
description: ISqlDialectStrategy interface documentation
lang: en
---
## ISqlDialectStrategy

**Purpose:** Database-specific SQL generation and validation

**Namespace:** `SmartRAG.Interfaces.Database.Strategies`

Enables database-specific SQL optimization and custom database support.

#### Properties

```csharp
DatabaseType DatabaseType { get; }
```

#### Methods

##### BuildSystemPrompt

Build AI system prompt for SQL generation specific to this database dialect.

```csharp
string BuildSystemPrompt(DatabaseSchemaInfo schema, string userQuery)
```

##### ValidateSyntax

Validate SQL syntax for this specific dialect.

```csharp
bool ValidateSyntax(string sql, out string errorMessage)
```

##### FormatSql

Format SQL query according to dialect-specific rules.

```csharp
string FormatSql(string sql)
```

##### GetLimitClause

Get the LIMIT clause format for this dialect.

```csharp
string GetLimitClause(int limit)
```

**Returns:**
- SQLite/MySQL: `LIMIT {limit}`
- SQL Server: `TOP {limit}`
- PostgreSQL: `LIMIT {limit}`

#### Built-in Implementations

- `SqliteDialectStrategy` - SQLite-optimized SQL
- `PostgreSqlDialectStrategy` - PostgreSQL-optimized SQL
- `MySqlDialectStrategy` - MySQL/MariaDB-optimized SQL
- `SqlServerDialectStrategy` - SQL Server-optimized SQL

#### Custom Implementation Example

```csharp
// Example: Custom dialect for a specific database variant
public class CustomPostgreSqlDialectStrategy : BaseSqlDialectStrategy
{
    public override DatabaseType DatabaseType => DatabaseType.PostgreSQL;
    
    public override string BuildSystemPrompt(DatabaseSchemaInfo schema, string userQuery)
    {
        return $"Generate PostgreSQL SQL for: {userQuery}\\nSchema: {schema}";
    }
    
    public override bool ValidateSyntax(string sql, out string errorMessage)
    {
        // PostgreSQL-specific validation
        errorMessage = null;
        
        // Example: Check for PostgreSQL-specific syntax
        if (sql.Contains("LIMIT") && !sql.Contains("OFFSET"))
        {
            // Valid PostgreSQL syntax
            return true;
        }
        
        return true;
    }
    
    public override string FormatSql(string sql)
    {
        // PostgreSQL-specific formatting (optional)
        return sql;
    }
    
    public override string GetLimitClause(int limit)
    {
        return $"LIMIT {limit}";
    }
}
```


## Related Interfaces

- [Strategy Interfaces]({{ site.baseurl }}/en/api-reference/strategies) - Browse all strategy interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

