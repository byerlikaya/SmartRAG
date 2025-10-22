# SmartRAG TODO List

## 🚀 High Priority

### Parametreli Sorgu Sistemi (Parameterized Query System)
**Branch:** `feature/parameterized-queries`

**Neden gerekli:**
- SQL Injection koruması (güvenlik)
- Query plan caching (performans)
- Tip güvenliği (type safety)
- Production-ready özellik

**Şu anki durum:**
- ✅ AI literal values kullanıyor (çalışıyor ama ideal değil)
- ✅ @param, :param, ? syntax yasaklandı (geçici çözüm)
- ✅ Test success rate: %72.7

**Implementation Plan:**

1. **Model Class Oluştur**
   ```csharp
   public class SqlQueryWithParameters
   {
       public string Sql { get; set; }
       public Dictionary<string, object> Parameters { get; set; }
   }
   ```

2. **AI Prompt Değiştir**
   - AI JSON output üretsin: `{"sql": "...", "parameters": {...}}`
   - Örnek: `{"sql": "SELECT * FROM Orders WHERE CustomerID = @p1", "parameters": {"@p1": 5}}`

3. **DatabaseParserService Güncelle**
   - `ExecuteQueryAsync` metodu parametre desteği alsın
   - Her database türü için parametre ekleme:
     - SQL Server: `@p1, @p2`
     - PostgreSQL: `$1, $2`
     - MySQL: `?, ?`
     - SQLite: `?, ?`

4. **MultiDatabaseQueryCoordinator Güncelle**
   - `GenerateDatabaseQueriesAsync`: JSON parse et
   - Parametreleri AI'dan al
   - `ExecuteSingleDatabaseQueryAsync`: Parametreleri geç

5. **Test Et**
   - Cross-database queries
   - Parametre formatları (string, int, decimal, date)
   - Başarı oranını koru (%70+)

**Dosyalar:**
- `src/SmartRAG/Models/SqlQueryWithParameters.cs` (yeni)
- `src/SmartRAG/Services/MultiDatabaseQueryCoordinator.cs` (değişecek)
- `src/SmartRAG/Services/DatabaseParserService.cs` (değişecek)
- `src/SmartRAG/Interfaces/IDatabaseParserService.cs` (değişecek)

**Risk:**
- AI JSON üretimi başarısız olabilir
- Başarı oranı düşebilir
- Breaking change (major version)

**Süre Tahmini:** 2-3 saat

---

## 🛠️ Code Quality

### SOLID/DRY Refactoring - MultiDatabaseQueryCoordinator
**Issue:** `MultiDatabaseQueryCoordinator.cs` çok büyük (2346 satır) - God Class anti-pattern

**Refactoring Plan:**

```
src/SmartRAG/Services/
  ├── MultiDatabaseQueryCoordinator.cs (orchestrator only ~300 lines)
  ├── QueryAnalysis/
  │   ├── QueryIntentAnalyzer.cs
  │   └── QueryIntentParser.cs
  ├── SqlGeneration/
  │   ├── SqlQueryGenerator.cs
  │   ├── SqlPromptBuilder.cs
  │   └── SqlPromptTemplates.cs
  ├── SqlValidation/
  │   ├── SqlValidator.cs
  │   ├── SqlSyntaxValidator.cs
  │   ├── SqlColumnValidator.cs
  │   └── SqlTableValidator.cs
  └── ResultMerging/
      ├── QueryResultMerger.cs
      ├── QueryResultParser.cs
      └── SmartJoinEngine.cs
```

**Prensipler:**
- **SRP (Single Responsibility):** Her sınıf tek bir iş yapsın
- **OCP (Open/Closed):** Extension için açık, modification için kapalı
- **DRY (Don't Repeat Yourself):** Tekrarlayan kod extraction

**Süre Tahmini:** 3-4 saat

---

## 📊 Feature Enhancements

### Test Success Rate İyileştirme
**Hedef:** %72.7 → %90+

**Stratejiler:**
1. AI prompt daha da netleştir
2. SQL validation katmanı güçlendir
3. Retry logic optimize et
4. Smart merging geliştir

### Multi-Database Transaction Support
**Özellik:** Cross-database transaction yönetimi

**Use Case:**
```csharp
using (var transaction = await coordinator.BeginTransactionAsync())
{
    await coordinator.ExecuteAsync(db1Query);
    await coordinator.ExecuteAsync(db2Query);
    await transaction.CommitAsync();
}
```

### Query Caching
**Özellik:** Aynı sorguları cache'le

**Faydalar:**
- Performans artışı
- AI token tasarrufu
- Hızlı yanıt

---

## 🐛 Known Issues

### API Warnings (14 warnings)
- XML documentation hataları
- Async method warnings
- Nullable reference warnings

**Hedef:** 0 warnings

---

## 📝 Notes

- Her TODO için ayrı branch açılmalı
- Test coverage artırılmalı
- Documentation güncel tutulmalı
- Breaking changes için major version bump

**Last Updated:** 2025-10-21

