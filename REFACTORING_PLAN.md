# Services ve Interfaces Klasör Yapısı Refactoring Planı

## Mevcut Durum Analizi

### Services Klasörü (29 dosya)
1. **AI Services**: AIService, AIConfigurationService, PromptBuilderService
2. **Document Services**: DocumentService, DocumentSearchService, DocumentParserService, DocumentScoringService
3. **Search Services**: SemanticSearchService, EmbeddingSearchService, SourceBuilderService
4. **Database Services**: DatabaseParserService, DatabaseSchemaAnalyzer, DatabaseConnectionManager, DatabaseQueryExecutor, SQLQueryGenerator, QueryIntentAnalyzer, ResultMerger, MultiDatabaseQueryCoordinator
5. **Storage Services (Qdrant)**: QdrantSearchService, QdrantEmbeddingService, QdrantCollectionManager, QdrantCacheManager
6. **Parser Services**: ImageParserService, WhisperAudioParserService
7. **Support Services**: TextNormalizationService, ConversationManagerService, QueryIntentClassifierService, AudioConversionService
8. **Shared**: ServiceLogMessages

### Interfaces Klasörü (31 dosya)
- Her service için interface var (iyi)
- SemanticSearchService için interface var (iyi)
- AudioConversionService için interface yok (internal helper, gerekli değil)

## Yeni Klasör Yapısı

```
src/SmartRAG/
├── Services/
│   ├── AI/
│   │   ├── AIService.cs
│   │   ├── AIConfigurationService.cs
│   │   └── PromptBuilderService.cs
│   ├── Document/
│   │   ├── DocumentService.cs
│   │   ├── DocumentSearchService.cs
│   │   ├── DocumentParserService.cs
│   │   └── DocumentScoringService.cs
│   ├── Search/
│   │   ├── SemanticSearchService.cs
│   │   ├── EmbeddingSearchService.cs
│   │   └── SourceBuilderService.cs
│   ├── Database/
│   │   ├── DatabaseParserService.cs
│   │   ├── DatabaseSchemaAnalyzer.cs
│   │   ├── DatabaseConnectionManager.cs
│   │   ├── DatabaseQueryExecutor.cs
│   │   ├── SQLQueryGenerator.cs
│   │   ├── QueryIntentAnalyzer.cs
│   │   ├── ResultMerger.cs
│   │   └── MultiDatabaseQueryCoordinator.cs
│   ├── Storage/
│   │   └── Qdrant/
│   │       ├── QdrantSearchService.cs
│   │       ├── QdrantEmbeddingService.cs
│   │       ├── QdrantCollectionManager.cs
│   │       └── QdrantCacheManager.cs
│   ├── Parser/
│   │   ├── ImageParserService.cs
│   │   ├── WhisperAudioParserService.cs
│   │   └── AudioConversionService.cs (Support)
│   ├── Support/
│   │   ├── TextNormalizationService.cs
│   │   ├── ConversationManagerService.cs
│   │   └── QueryIntentClassifierService.cs
│   └── Shared/
│       └── ServiceLogMessages.cs

├── Interfaces/
│   ├── AI/
│   │   ├── IAIService.cs
│   │   ├── IAIConfigurationService.cs
│   │   └── IPromptBuilderService.cs
│   ├── Document/
│   │   ├── IDocumentService.cs
│   │   ├── IDocumentSearchService.cs
│   │   ├── IDocumentParserService.cs
│   │   └── IDocumentScoringService.cs
│   ├── Search/
│   │   ├── ISemanticSearchService.cs
│   │   ├── IEmbeddingSearchService.cs
│   │   └── ISourceBuilderService.cs
│   ├── Database/
│   │   ├── IDatabaseParserService.cs
│   │   ├── IDatabaseSchemaAnalyzer.cs
│   │   ├── IDatabaseConnectionManager.cs
│   │   ├── IDatabaseQueryExecutor.cs
│   │   ├── ISQLQueryGenerator.cs
│   │   ├── IQueryIntentAnalyzer.cs
│   │   ├── IResultMerger.cs
│   │   └── IMultiDatabaseQueryCoordinator.cs
│   ├── Storage/
│   │   └── Qdrant/
│   │       ├── IQdrantSearchService.cs
│   │       ├── IQdrantEmbeddingService.cs
│   │       ├── IQdrantCollectionManager.cs
│   │       └── IQdrantCacheManager.cs
│   ├── Parser/
│   │   ├── IImageParserService.cs
│   │   └── IAudioParserService.cs
│   └── Support/
│       ├── ITextNormalizationService.cs
│       ├── IConversationManagerService.cs
│       └── IQueryIntentClassifierService.cs
```

## Birleştirme Önerileri

### ❌ Birleştirilmeyecekler (SRP'ye aykırı olur)
- Qdrant servisleri: Her biri farklı sorumluluğa sahip (Search, Embedding, Collection, Cache)
- Database servisleri: Her biri farklı sorumluluğa sahip (Parser, Schema, Connection, Query, SQL, Intent, Result, Coordinator)

### ✅ Kaldırılabilecekler
- Yok (tüm servisler gerekli)

## Namespace Değişiklikleri

Tüm dosyalarda namespace'ler güncellenecek:
- `SmartRAG.Services` → `SmartRAG.Services.AI`, `SmartRAG.Services.Document`, vb.
- `SmartRAG.Interfaces` → `SmartRAG.Interfaces.AI`, `SmartRAG.Interfaces.Document`, vb.

## Adımlar

1. Klasör yapısını oluştur
2. Dosyaları taşı
3. Namespace'leri güncelle
4. Using statement'ları güncelle
5. Build ve test et

