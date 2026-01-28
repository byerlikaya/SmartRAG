
# Changelog

All notable changes to SmartRAG will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.8.1] - 2026-01-28

### 🔧 Improved
- **Schema Services Cancellation Support**: Propagated `CancellationToken` through schema migration and related services for better cancellation handling
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/SchemaMigrationService.cs` - Cancellation token propagation
    - `src/SmartRAG/Services/Database/QueryIntentAnalyzer.cs` - Minor cancellation-related refinements
  - **Benefits**: Safer cancellation behavior and more robust async flows

- **Codebase Cleanup and Maintainability**: Removed unused helpers, strategies, and events across database, search, and watcher services
  - **Files Modified** (high level):
    - `src/SmartRAG/Services/Database/Prompts/SqlPromptBuilder.cs` - Removed unused prompt helpers and dead code paths
    - `src/SmartRAG/Services/Database/Strategies/*` - Removed unused SQL dialect helper methods
    - `src/SmartRAG/Services/Document/*` - Simplified scoring and strategy helpers, removed unused code
    - `src/SmartRAG/Services/Search/ContextExpansionService.cs` - Simplified expansion logic
    - `src/SmartRAG/Services/Storage/Qdrant/QdrantSearchService.cs` - Removed unused search helpers, kept behavior intact
    - `src/SmartRAG/Services/FileWatcher/FileWatcherService.cs` and `FileWatcherEventArgs.cs` - Removed unused events and properties
    - `src/SmartRAG/Services/Support/ConversationManagerService.cs` - Removed unused helpers
    - `src/SmartRAG/Helpers/QueryTokenizer.cs` - Removed unused tokens/helpers
  - **Benefits**: Smaller, easier-to-maintain codebase with no change to public API

- **Logging and Repository Messages**: Simplified and de-noised repository and service log messages
  - **Files Modified**:
    - `src/SmartRAG/Repositories/RepositoryLogMessages.cs` - Reduced noisy log definitions
    - `src/SmartRAG/Services/Database/DatabaseQueryExecutor.cs` - Minor log cleanup
  - **Benefits**: Clearer logs and reduced noise in production environments

### 📝 Notes
- **Backward Compatibility**: No breaking changes; all updates are internal refactors and behavior-preserving improvements
- **Code Quality**: Maintains 0 errors, 0 warnings build policy

## [3.8.0] - 2026-01-26

### ✨ Added
- **Schema RAG Implementation**: Automatic migration of database schemas to vectorized chunks for intelligent SQL generation
  - New `ISchemaMigrationService` interface and `SchemaMigrationService` implementation for schema migration
  - New `SchemaChunkService` for converting database schemas to vectorized document chunks
  - Automatic schema chunk generation with embeddings for semantic search
  - Schema chunks stored with metadata (`databaseId`, `databaseName`, `documentType: "Schema"`)
  - Support for migrating all schemas or individual database schemas
  - Schema update functionality (delete old and create new chunks)
  - Semantic keyword extraction from table and column names for better query matching
  - PostgreSQL-specific formatting with double quotes for identifiers
  - Table type classification (TRANSACTIONAL, LOOKUP, MASTER) based on row count
  - Comprehensive foreign key relationship documentation in chunks
  - **Files Added**:
    - `src/SmartRAG/Interfaces/Database/ISchemaMigrationService.cs` - Interface for schema migration
    - `src/SmartRAG/Services/Database/SchemaMigrationService.cs` - Schema migration service implementation
    - `src/SmartRAG/Services/Database/SchemaChunkService.cs` - Schema chunk conversion service
  - **Benefits**: More accurate SQL generation through semantic search of schema information, better query intent understanding

### 🔧 Improved
- **SQL Query Generation**: Enhanced with schema chunk integration for better accuracy
  - Schema information now retrieved from RAG chunks (primary source)
  - Fallback to `DatabaseSchemaInfo` when schema chunks are not available
  - Improved prompt building with schema context from chunks
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - Enhanced with schema chunk integration
    - `src/SmartRAG/Services/Database/Prompts/SqlPromptBuilder.cs` - Improved prompt structure with schema context
  - **Benefits**: More accurate SQL queries, better understanding of database structure

- **Database Connection Manager**: Added optional schema migration service integration
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/DatabaseConnectionManager.cs` - Added schema migration service support
  - **Benefits**: Better integration with schema migration capabilities

- **Result Merger**: Enhanced merging logic for better result combination
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/ResultMerger.cs` - Improved merging logic
  - **Benefits**: Better result combination from multiple sources

- **Document Validator**: Enhanced validation for schema documents
  - **Files Modified**:
    - `src/SmartRAG/Services/Helpers/DocumentValidator.cs` - Enhanced validation logic
  - **Benefits**: Better validation of schema documents

- **Service Registration**: Added schema migration and chunk services to DI container
  - **Files Modified**:
    - `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Added service registrations
  - **Benefits**: Proper dependency injection setup

- **Storage Factory**: Updated for schema-related services
  - **Files Modified**:
    - `src/SmartRAG/Factories/StorageFactory.cs` - Updated factory configuration
  - **Benefits**: Better factory integration

- **Query Strategy Executor**: Enhanced with schema-aware query execution
  - **Files Modified**:
    - `src/SmartRAG/Services/Document/QueryStrategyExecutorService.cs` - Enhanced query strategy
  - **Benefits**: Better query routing and execution

- **Qdrant Collection Manager**: Updated for schema document support
  - **Files Modified**:
    - `src/SmartRAG/Services/Storage/Qdrant/QdrantCollectionManager.cs` - Enhanced collection management
  - **Benefits**: Better support for schema documents in vector store

### 📝 Notes
- **Backward Compatibility**: All changes are backward compatible
- **Migration**: No migration required - existing code continues to work without changes
- **Breaking Changes**: None
- **Code Quality**: Maintained 0 errors, 0 warnings
- **Schema RAG Pattern**: Schema information is now stored as vectorized chunks, enabling semantic search for better SQL generation

## [3.7.0] - 2026-01-19

### ✨ Added
- **Cross-Database Mapping Detector**: Automatic detection of relationships between columns across different databases
  - New `CrossDatabaseMapping` model for defining cross-database relationships
  - New `CrossDatabaseMappingDetector` service for automatic relationship detection
  - Automatic detection based on Primary Key and Foreign Key analysis
  - Support for Primary Key and Foreign Key relationship types
  - Semantic column name matching for relationship detection
  - **Files Modified**:
    - `src/SmartRAG/Models/Configuration/CrossDatabaseMapping.cs` - New model for cross-database mappings
    - `src/SmartRAG/Services/Database/CrossDatabaseMappingDetector.cs` - New service for automatic detection
    - `src/SmartRAG/Models/Configuration/DatabaseConnectionConfig.cs` - Added CrossDatabaseMappings property
  - **Benefits**: Better cross-database query coordination, automatic relationship discovery, improved query accuracy

### 🔧 Improved
- **SQL Script Extraction**: Extracted SQL scripts from database creator classes to separate files
  - Applied DRY principle by centralizing SQL scripts
  - Better maintainability and reusability of database setup scripts
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/DatabaseParserService.cs` - Updated to use extracted scripts
    - `src/SmartRAG/Services/Database/DatabaseSchemaAnalyzer.cs` - Improved schema handling
  - **Benefits**: Better code organization, reduced duplication, easier maintenance

- **Database Query Generation**: Enhanced query generation and validation logic
  - Improved database query generation accuracy
  - Better validation logic for generated queries
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - Query generation improvements
    - `src/SmartRAG/Services/Database/Validation/SqlValidator.cs` - Enhanced validation
    - `src/SmartRAG/Services/Database/Prompts/SqlPromptBuilder.cs` - Improved prompt building
  - **Benefits**: More accurate queries, better error prevention, improved reliability

- **Database Parser and Document Search**: Updated services for better integration
  - Improved database parser service integration
  - Enhanced document search service coordination with database queries
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/DatabaseParserService.cs` - Service improvements
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Integration improvements
  - **Benefits**: Better service coordination, improved query accuracy

### 🐛 Fixed
- **Security**: Prevented SQL injection in database creator classes
  - Enhanced input validation and parameterized query usage
  - **Benefits**: Improved security, prevention of SQL injection attacks

- **Security**: Prevented command injection in database creator classes
  - Removed shell command execution
  - Enhanced input sanitization
  - **Benefits**: Improved security, prevention of command injection attacks

- **Security**: Prevented sensitive data leakage in logs and database handlers
  - Removed sensitive data from error messages and logs
  - Enhanced error message sanitization
  - Removed backup file paths from exception messages
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/DatabaseConnectionManager.cs` - Enhanced error handling
    - `src/SmartRAG/Services/Database/DatabaseQueryExecutor.cs` - Improved error messages
  - **Benefits**: Better data privacy, reduced information disclosure, improved security

### 📝 Notes
- **Backward Compatibility**: All changes are backward compatible
- **Migration**: No migration required - existing code continues to work without changes
- **Breaking Changes**: None
- **Code Quality**: Maintained 0 errors, 0 warnings

## [3.6.0] - 2025-12-30

### ✨ Added
- **CancellationToken Support**: Comprehensive CancellationToken support across all async methods and interfaces
  - All async interface methods now accept `CancellationToken cancellationToken = default` parameter
  - Private helper methods updated for cancellation support
  - Better resource management and graceful cancellation handling
  - XML documentation updated for all methods with CancellationToken
  - **Files Modified**:
    - `src/SmartRAG/Interfaces/` - All async interface methods updated
    - `src/SmartRAG/Services/` - All service implementations updated
    - `src/SmartRAG/Repositories/` - All repository implementations updated
    - `src/SmartRAG/Providers/` - All provider implementations updated
  - **Benefits**: Better resource management, graceful cancellation, improved async/await patterns

### 🔧 Improved
- **Performance**: Replaced Task.Run with native async file I/O methods
  - Improved file I/O operations using native async methods
  - Better resource utilization and reduced overhead
  - **Files Modified**:
    - `src/SmartRAG/Services/FileWatcher/FileWatcherService.cs` - Native async I/O
    - `src/SmartRAG/Services/Document/DocumentService.cs` - Native async I/O
  - **Benefits**: Better performance, reduced memory allocation, improved scalability

- **Code Quality**: Removed unnecessary service and repository logs
  - Cleaned up excessive logging in service layer
  - Removed unnecessary repository logs
  - Improved log readability and reduced noise
  - **Files Modified**:
    - `src/SmartRAG/Services/Shared/ServiceLogMessages.cs` - Log cleanup
    - `src/SmartRAG/Repositories/RepositoryLogMessages.cs` - Log cleanup
    - Multiple service and repository files - Log removal
  - **Benefits**: Cleaner logs, better performance, improved readability

### 📝 Notes
- **Backward Compatibility**: All CancellationToken parameters have default values, ensuring full backward compatibility
- **Migration**: No migration required - existing code continues to work without changes
- **Breaking Changes**: None
- **Code Quality**: Maintained 0 errors, 0 warnings

## [3.5.0] - 2025-12-27

### 🔧 Improved
- **Code Quality**: Comprehensive refactoring across services, providers, and interfaces for better SOLID/DRY compliance
  - Improved code organization and separation of concerns
  - Enhanced maintainability and readability
  - Better architecture patterns implementation
  - **Files Modified**:
    - `src/SmartRAG/Services/` - Multiple service files refactored
    - `src/SmartRAG/Providers/` - Provider code quality improvements
    - `src/SmartRAG/Interfaces/` - Interface cleanup and consistency
  - **Benefits**: Better maintainability, cleaner codebase, improved testability

- **Interface Consistency**: Renamed interface for naming consistency
  - `ISQLQueryGenerator` → `ISqlQueryGenerator` (PascalCase naming convention)
  - **Files Modified**:
    - `src/SmartRAG/Interfaces/Database/ISqlQueryGenerator.cs` - Interface renamed
    - `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - Implementation updated
    - `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Registration updated
  - **Benefits**: Consistent naming conventions, better code readability
  - **Breaking Change**: Direct interface users need to update references

- **Code Duplication Elimination**: Removed unnecessary wrapper methods and services
  - Removed unnecessary wrapper methods that only delegate to other services
  - Eliminated code duplication across DocumentSearchService and related services
  - **Files Modified**:
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Wrapper removal
    - `src/SmartRAG/Services/Document/` - Multiple service files cleaned up
  - **Benefits**: Reduced code complexity, better performance, improved maintainability

- **Search Strategy**: Improved search strategy implementation and code quality
  - Enhanced query strategy logic
  - Better code organization in strategy services
  - **Files Modified**:
    - `src/SmartRAG/Services/Document/QueryStrategyOrchestratorService.cs` - Strategy improvements
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Strategy optimization
  - **Benefits**: Better query routing, improved performance

- **PDF Parsing and OCR**: Enhanced PDF parsing and OCR robustness
  - Improved error handling in PDF parsing
  - Better OCR processing reliability
  - **Files Modified**:
    - `src/SmartRAG/Services/Document/Parsers/PdfFileParser.cs` - Parsing improvements
    - `src/SmartRAG/Services/Parser/ImageParserService.cs` - OCR robustness
  - **Benefits**: More reliable document processing, better error recovery

### ✨ Added
- **QueryIntentAnalysisResult Model**: New model for query intent classification results
  - Structured result model for query intent analysis
  - Better type safety for intent classification
  - **Files Modified**:
    - `src/SmartRAG/Models/Results/QueryIntentAnalysisResult.cs` - New model
  - **Benefits**: Better type safety, improved code clarity

- **SearchOptions Enhancements**: Added factory methods and Clone method
  - `FromConfig()` factory method for creating SearchOptions from configuration
  - `Clone()` method for creating copies of SearchOptions
  - **Files Modified**:
    - `src/SmartRAG/Models/Schema/SearchOptions.cs` - Factory and Clone methods
  - **Benefits**: Easier configuration, better object management

- **QueryStrategyRequest Consolidation**: Unified query strategy request DTOs
  - Consolidated multiple query strategy request DTOs into single `QueryStrategyRequest` model
  - Simplified request handling
  - **Files Modified**:
    - `src/SmartRAG/Models/RequestResponse/QueryStrategyRequest.cs` - Unified model
  - **Benefits**: Simplified API, better consistency

### 🔄 Changed
- **Interface Method Signatures**: Removed preferredLanguage parameter and consolidated method overloads
  - Removed `preferredLanguage` parameter from interface methods
  - Consolidated method overloads for better API consistency
  - **Files Modified**:
    - `src/SmartRAG/Interfaces/Document/IDocumentSearchService.cs` - Method signature updates
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Implementation updates
  - **Benefits**: Cleaner API, better consistency
  - **Breaking Change**: Code using `preferredLanguage` parameter needs to use `SearchOptions` instead

- **Interface Naming**: ISQLQueryGenerator renamed to ISqlQueryGenerator
  - **Breaking Change**: Direct interface users need to update references
  - **Migration**: Replace `ISQLQueryGenerator` with `ISqlQueryGenerator` in your code

### 🗑️ Removed
- **Unused Services**: Removed unused service interfaces and implementations
  - `ISourceSelectionService` interface removed
  - `SourceSelectionService` implementation removed
  - **Files Removed**:
    - `src/SmartRAG/Interfaces/Document/ISourceSelectionService.cs`
    - `src/SmartRAG/Services/Document/SourceSelectionService.cs`
  - **Benefits**: Cleaner codebase, reduced complexity

- **Unnecessary Wrappers**: Removed unnecessary wrapper methods and orchestration services
  - Removed wrapper methods that only delegate to other services
  - Removed orchestration services with no added value
  - **Benefits**: Reduced code complexity, better performance

### ✨ Benefits
- **Better Code Quality**: Comprehensive refactoring improves maintainability and readability
- **Improved Architecture**: Better separation of concerns and SOLID/DRY compliance
- **Cleaner API**: Simplified interfaces and method signatures
- **Enhanced Performance**: Removed unnecessary wrappers improve performance
- **Better Type Safety**: New models provide better type safety

### 📝 Notes
- **Breaking Changes**: 
  - `ISQLQueryGenerator` renamed to `ISqlQueryGenerator` (direct interface users only)
  - `preferredLanguage` parameter removed from methods (use `SearchOptions` instead)
- **Migration**: Update interface references and use `SearchOptions` for language configuration
- **Backward Compatibility**: Most changes are internal refactoring, public API remains largely compatible

## [3.4.0] - 2025-12-12

### ✨ Added
- **MCP (Model Context Protocol) Integration**: External MCP server integration for enhanced search capabilities
  - `IMcpClient` interface and `McpClient` service for MCP server connections
  - `IMcpConnectionManager` interface and `McpConnectionManager` service for connection lifecycle management
  - `IMcpIntegrationService` interface and `McpIntegrationService` service for querying MCP servers
  - Support for multiple MCP servers with automatic tool discovery
  - Query enrichment with conversation history context
  - **Files Modified**:
    - `src/SmartRAG/Interfaces/Mcp/IMcpClient.cs` - MCP client interface
    - `src/SmartRAG/Interfaces/Mcp/IMcpConnectionManager.cs` - Connection manager interface
    - `src/SmartRAG/Interfaces/Mcp/IMcpIntegrationService.cs` - Integration service interface
    - `src/SmartRAG/Services/Mcp/McpClient.cs` - MCP client implementation
    - `src/SmartRAG/Services/Mcp/McpConnectionManager.cs` - Connection manager implementation
    - `src/SmartRAG/Services/Mcp/McpIntegrationService.cs` - Integration service implementation
    - `src/SmartRAG/Models/Configuration/McpServerConfig.cs` - MCP server configuration model
    - `src/SmartRAG/Models/RequestResponse/McpRequest.cs` - MCP request model
    - `src/SmartRAG/Models/RequestResponse/McpResponse.cs` - MCP response model
    - `src/SmartRAG/Models/Results/McpTool.cs` - MCP tool model
    - `src/SmartRAG/Models/Results/McpToolResult.cs` - MCP tool result model
  - **Benefits**: Extensible search capabilities, integration with external data sources, enhanced query context

- **File Watcher Service**: Automatic document indexing from watched folders
  - `IFileWatcherService` interface and `FileWatcherService` implementation
  - Automatic file monitoring and indexing for specified folders
  - Support for multiple watched folders with independent configurations
  - Language-specific processing per watched folder
  - **Files Modified**:
    - `src/SmartRAG/Interfaces/FileWatcher/IFileWatcherService.cs` - File watcher interface
    - `src/SmartRAG/Services/FileWatcher/FileWatcherService.cs` - File watcher implementation
    - `src/SmartRAG/Services/FileWatcher/Events/FileWatcherEventArgs.cs` - File watcher event arguments
    - `src/SmartRAG/Models/Configuration/WatchedFolderConfig.cs` - Watched folder configuration model
  - **Benefits**: Automatic document indexing, reduced manual uploads, real-time updates

- **DocumentType Property**: Enhanced document chunk filtering by content type
  - `DocumentType` property added to `DocumentChunk` entity (Document, Audio, Image)
  - Automatic document type detection based on file extension and content type
  - Filtering support for audio and image chunks in search operations
  - **Files Modified**:
    - `src/SmartRAG/Entities/DocumentChunk.cs` - Added DocumentType property
    - `src/SmartRAG/Services/Document/DocumentParserService.cs` - Document type determination
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Document type filtering
    - `src/SmartRAG/Services/Document/DocumentSearchStrategyService.cs` - Type-based filtering
    - `src/SmartRAG/Repositories/QdrantDocumentRepository.cs` - Document type storage
    - `src/SmartRAG/Services/Storage/Qdrant/QdrantSearchService.cs` - Document type retrieval
  - **Benefits**: Better content type filtering, improved search accuracy, enhanced chunk organization

- **DefaultLanguage Support**: Global default language configuration for document processing
  - `DefaultLanguage` property in `SmartRagOptions` for setting default processing language
  - Automatic language detection fallback when language not specified
  - Support for ISO 639-1 language codes (e.g., "tr", "en", "de")
  - **Files Modified**:
    - `src/SmartRAG/Models/Configuration/SmartRagOptions.cs` - Added DefaultLanguage property
    - `src/SmartRAG/Services/FileWatcher/FileWatcherService.cs` - Default language usage
    - `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Default language configuration
  - **Benefits**: Consistent language processing, reduced configuration overhead, better multilingual support

- **Enhanced Search Feature Flags**: Granular control over search capabilities
  - `EnableMcpSearch` flag for MCP integration control
  - `EnableAudioSearch` flag for audio transcription search
  - `EnableImageSearch` flag for image OCR search
  - Per-request and global configuration support
  - **Files Modified**:
    - `src/SmartRAG/Models/Schema/SearchOptions.cs` - Added EnableMcpSearch, EnableAudioSearch, EnableImageSearch flags
    - `src/SmartRAG/Models/Configuration/SmartRagOptions.cs` - Added feature flags to FeatureToggles
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Feature flag integration
  - **Benefits**: Fine-grained search control, performance optimization, resource management

- **Early Exit Optimization**: Performance improvement for document search
  - Early exit when sufficient high-quality results are found
  - Reduced unnecessary processing for queries with clear results
  - Parallel execution of document search and query intent analysis for improved performance
  - Smart skip logic for eager document answer generation when database intent confidence is high
  - **Files Modified**:
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Early exit logic implementation with parallel execution
    - `src/SmartRAG/Services/Document/QueryStrategyOrchestratorService.cs` - Strategy optimization
  - **Benefits**: Faster search responses, reduced resource usage, improved user experience, optimized query processing

- **IsExplicitlyNegative Check**: Fast-fail mechanism for negative answers
  - `IsExplicitlyNegative` method added to `IResponseBuilderService` interface for detecting explicit failure patterns
  - Support for `[NO_ANSWER_FOUND]` pattern for explicit failure detection
  - Prevents false positives when AI returns negative answers despite high-confidence document matches
  - **Files Modified**:
    - `src/SmartRAG/Interfaces/Document/IResponseBuilderService.cs` - Added IsExplicitlyNegative method
    - `src/SmartRAG/Services/Document/ResponseBuilderService.cs` - IsExplicitlyNegative implementation
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - IsExplicitlyNegative usage in early exit logic
    - `src/SmartRAG/Services/AI/PromptBuilderService.cs` - Added [NO_ANSWER_FOUND] pattern to prompts
    - `src/SmartRAG/Services/Database/ResultMerger.cs` - Added [NO_ANSWER_FOUND] pattern to database prompts
  - **Benefits**: More accurate failure detection, reduced false positives, better query strategy decisions

- **SmartRagStartupService**: Centralized startup service for initialization
  - Automatic MCP server connection on startup
  - File watcher initialization
  - **Files Modified**:
    - `src/SmartRAG/Services/Startup/SmartRagStartupService.cs` - Startup service implementation
  - **Benefits**: Simplified initialization, better service coordination

- **ClearAllConversationsAsync**: Conversation history management enhancement
  - `ClearAllConversationsAsync` method added to `IConversationManagerService` and `IConversationRepository`
  - Support for clearing all conversation history across all storage providers
  - **Files Modified**:
    - `src/SmartRAG/Interfaces/Support/IConversationManagerService.cs` - Added ClearAllConversationsAsync method
    - `src/SmartRAG/Services/Support/ConversationManagerService.cs` - ClearAllConversationsAsync implementation
    - `src/SmartRAG/Interfaces/Storage/IConversationRepository.cs` - Added ClearAllConversationsAsync method
    - `src/SmartRAG/Repositories/FileSystemConversationRepository.cs` - ClearAllConversationsAsync implementation
    - `src/SmartRAG/Repositories/InMemoryConversationRepository.cs` - ClearAllConversationsAsync implementation
    - `src/SmartRAG/Repositories/RedisConversationRepository.cs` - ClearAllConversationsAsync implementation
    - `src/SmartRAG/Repositories/SqliteConversationRepository.cs` - ClearAllConversationsAsync implementation
  - **Benefits**: Better conversation management, bulk clearing support, improved data control

- **Search Metadata Tracking**: Enhanced search result metadata
  - Search metadata tracking and display in responses
  - Metadata includes search statistics and performance metrics
  - **Files Modified**:
    - `src/SmartRAG/Interfaces/Document/IResponseBuilderService.cs` - Metadata support
    - `src/SmartRAG/Models/RequestResponse/RagResponse.cs` - Metadata properties
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Metadata tracking
    - `src/SmartRAG/Services/Document/ResponseBuilderService.cs` - Metadata display
  - **Benefits**: Better search visibility, performance monitoring, enhanced debugging

### 🔧 Improved
- **Query Strategy Optimization**: Enhanced query execution strategy with intelligent source selection
  - Refactored `ResponseBuilderService` to use `IsExplicitlyNegative` method consistently
  - Improved early exit logic with `StrongDocumentMatchThreshold` (4.8) constant for better document prioritization
  - Enhanced database query skip logic based on document match strength and AI answer quality
  - **Files Modified**:
    - `src/SmartRAG/Services/Document/ResponseBuilderService.cs` - Code simplification and consistency improvements
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Query strategy optimization
    - `src/SmartRAG/Services/Document/SourceSelectionService.cs` - Selection logic improvements
  - **Benefits**: Better query performance, more accurate source selection, reduced unnecessary processing

- **Code Quality**: Comprehensive code quality improvements across the codebase
  - Removed redundant comments and language-specific references
  - Improved constant naming and generic code patterns
  - Enhanced code organization and structure
  - **Files Modified**:
    - `src/SmartRAG/Services/` - Multiple service files cleaned up
    - `src/SmartRAG/Repositories/` - Repository code quality improvements
    - `src/SmartRAG/Providers/` - Provider code improvements
    - `src/SmartRAG/Interfaces/` - Interface cleanup
    - `src/SmartRAG/Helpers/QueryTokenizer.cs` - Code quality improvements
  - **Benefits**: Better maintainability, cleaner codebase, improved readability

- **Model Organization**: Reorganized models into logical subfolders
  - Models moved to `Configuration/` subfolder for configuration-related models
  - Models moved to `RequestResponse/` subfolder for request/response models
  - Models moved to `Results/` subfolder for result models
  - Models moved to `Schema/` subfolder for schema-related models
  - **Files Modified**:
    - Multiple model files reorganized into subfolders
  - **Benefits**: Better code organization, easier navigation, improved maintainability

- **Dependency Injection**: Improved DI patterns and error handling
  - Better service lifetime management
  - Enhanced error handling in service initialization
  - **Files Modified**:
    - `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - DI improvements
    - Multiple service files - Error handling improvements
  - **Benefits**: More reliable service initialization, better error recovery

- **Image Parsing and Context Expansion**: Enhanced image processing capabilities
  - Improved context expansion for image chunks
  - Better image parsing error handling
  - **Files Modified**:
    - `src/SmartRAG/Services/Parser/ImageParserService.cs` - Image parsing improvements
    - `src/SmartRAG/Services/Search/ContextExpansionService.cs` - Context expansion improvements
  - **Benefits**: Better image content extraction, improved OCR accuracy

- **Database Query Error Handling**: Enhanced error handling and response validation
  - Better error messages for database query failures
  - Improved response validation
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/MultiDatabaseQueryCoordinator.cs` - Error handling improvements
  - **Benefits**: Better error diagnostics, improved reliability

- **Missing Data Detection**: Language-agnostic missing data detection
  - Improved pattern matching for missing data indicators
  - Generic language support for missing data detection
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/MultiDatabaseQueryCoordinator.cs` - Missing data detection improvements
  - **Benefits**: Better data quality detection, language-agnostic patterns

### 🐛 Fixed
- **Language-Agnostic Missing Data Detection**: Fixed language-specific patterns in missing data detection
  - Removed hardcoded language-specific patterns
  - Implemented generic missing data detection patterns
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/MultiDatabaseQueryCoordinator.cs` - Language-agnostic detection
  - **Benefits**: Works with all languages, better pattern matching

- **HttpClient Timeout**: Increased timeout for long-running AI operations
  - Timeout increased to 10 minutes for `GenerateTextAsync` operations
  - Prevents premature timeout for complex queries
  - **Files Modified**:
    - `src/SmartRAG/Providers/BaseAIProvider.cs` - Timeout configuration
  - **Benefits**: Better handling of long-running operations, reduced timeout errors

- **Turkish Character Encoding**: Fixed encoding issues in PDF text extraction
  - Improved character encoding handling for Turkish characters
  - Better Unicode support in PDF parsing
  - **Files Modified**:
    - `src/SmartRAG/Services/Document/Parsers/PdfFileParser.cs` - Encoding improvements
  - **Benefits**: Better text extraction for Turkish documents, improved multilingual support

- **Chunk0 Retrieval**: Fixed numbered list processing chunk retrieval
  - Corrected chunk0 retrieval logic in numbered list processing
  - Improved context expansion for numbered lists
  - **Files Modified**:
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Chunk retrieval fix
  - **Benefits**: Better numbered list processing, improved context accuracy

- **DI Scope Issues**: Resolved dependency injection scope conflicts
  - Fixed circular dependency issues
  - Improved service initialization order
  - **Files Modified**:
    - `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - DI scope fixes
  - **Benefits**: More reliable service initialization, better error handling

- **Content Type Detection**: Improved content type detection accuracy
  - Better MIME type detection
  - Enhanced file extension mapping
  - **Files Modified**:
    - `src/SmartRAG/Services/Document/DocumentParserService.cs` - Content type detection improvements
  - **Benefits**: More accurate document type detection, better file handling

- **Conversation Intent Classification**: Enhanced context awareness
  - Improved conversation intent classification with better context understanding
  - Enhanced query intent detection accuracy
  - **Files Modified**:
    - `src/SmartRAG/Services/Support/QueryIntentClassifierService.cs` - Context-aware classification
  - **Benefits**: Better intent detection, improved conversation flow, enhanced accuracy

### 🐛 Fixed
- **Conversation History Duplicate Entries**: Fixed duplicate entries in conversation history
  - Resolved duplicate conversation history entries across all storage providers
  - Improved conversation history truncation logic
  - **Files Modified**:
    - `src/SmartRAG/Interfaces/Storage/IConversationRepository.cs` - Truncation support
    - `src/SmartRAG/Repositories/FileSystemConversationRepository.cs` - Duplicate prevention
    - `src/SmartRAG/Repositories/InMemoryConversationRepository.cs` - Duplicate prevention
    - `src/SmartRAG/Repositories/RedisConversationRepository.cs` - Duplicate prevention
    - `src/SmartRAG/Repositories/SqliteConversationRepository.cs` - Duplicate prevention
    - `src/SmartRAG/Services/AI/PromptBuilderService.cs` - Truncation improvements
    - `src/SmartRAG/Services/Support/ConversationManagerService.cs` - History management
  - **Benefits**: Cleaner conversation history, reduced storage usage, better performance

- **Redis Document Retrieval**: Fixed document retrieval when document list is empty
  - Improved document retrieval from chunks when document list is empty in Redis
  - Enhanced fallback mechanism for document retrieval
  - **Files Modified**:
    - `src/SmartRAG/Repositories/RedisDocumentRepository.cs` - Document retrieval improvements
  - **Benefits**: Better document access, improved reliability, enhanced data consistency

- **SqlValidator DI Compatibility**: Fixed dependency injection compatibility
  - Changed `SqlValidator` to use `ILogger<SqlValidator>` for proper DI compatibility
  - Improved service registration and lifetime management
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/Validation/SqlValidator.cs` - DI compatibility fix
  - **Benefits**: Better DI integration, improved service registration, enhanced maintainability

### 🔄 Changed
- **Feature Flag Naming**: Renamed feature flags for consistency
  - `EnableMcpClient` → `EnableMcpSearch`
  - `EnableAudioParsing` → `EnableAudioSearch`
  - `EnableImageParsing` → `EnableImageSearch`
  - **Files Modified**:
    - `src/SmartRAG/Models/Schema/SearchOptions.cs` - Flag renaming
    - `src/SmartRAG/Models/Configuration/SmartRagOptions.cs` - Flag renaming
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Flag usage updates
  - **Benefits**: Consistent naming, clearer semantics

- **Interface Restructuring**: Reorganized interfaces for better organization
  - MCP interfaces moved to `Interfaces/Mcp/` folder
  - File watcher interfaces moved to `Interfaces/FileWatcher/` folder
  - **Files Modified**:
    - Multiple interface files reorganized
  - **Benefits**: Better code organization, easier navigation

### ✨ Benefits
- **Extended Search Capabilities**: MCP integration enables external data source queries
- **Automatic Document Indexing**: File watcher service reduces manual document uploads
- **Better Content Filtering**: DocumentType property enables precise content type filtering
- **Improved Code Quality**: Comprehensive code cleanup and organization improvements
- **Enhanced Multilingual Support**: DefaultLanguage configuration simplifies language handling
- **Performance Optimization**: Early exit optimization improves search response times

### 📝 Notes
- **MCP Integration**: Requires MCP server configuration in `SmartRagOptions.McpServers`
- **File Watcher**: Requires watched folder configuration in `SmartRagOptions.WatchedFolders`
- **Backward Compatibility**: All changes are backward compatible, no breaking changes

## [3.3.0] - 2025-12-01

### ✨ Added
- **ConversationStorageProvider Separation**: Separated conversation storage from document storage
  - New `ConversationStorageProvider` enum for conversation history storage (Redis, SQLite, FileSystem, InMemory)
  - `StorageProvider` now only used for document/vector storage (InMemory, Redis, Qdrant)
  - Independent configuration for conversation and document storage
  - **Files Modified**:
    - `src/SmartRAG/Enums/ConversationStorageProvider.cs` - New enum for conversation storage
    - `src/SmartRAG/Enums/StorageProvider.cs` - Removed conversation-related providers (SQLite, FileSystem)
    - `src/SmartRAG/Models/SmartRagOptions.cs` - Added ConversationStorageProvider property
    - `src/SmartRAG/Factories/StorageFactory.cs` - Separate methods for conversation and document repositories
    - `src/SmartRAG/Interfaces/Storage/IStorageFactory.cs` - Added CreateConversationRepository method
    - `src/SmartRAG/Services/Support/ConversationManagerService.cs` - Updated to use ConversationStorageProvider
  - **Benefits**: Clear separation of concerns, independent scaling, better architecture
- **Redis RediSearch Integration**: Enhanced vector similarity search with RediSearch module support
  - RediSearch module support for advanced vector search capabilities
  - Vector index algorithm configuration (HNSW)
  - Distance metric configuration (COSINE)
  - Vector dimension configuration (default: 768)
  - **Files Modified**:
    - `src/SmartRAG/Models/RedisConfig.cs` - Added vector search configuration properties
    - `src/SmartRAG/Repositories/RedisDocumentRepository.cs` - RediSearch vector search implementation

### 🔧 Improved
- **Redis Vector Search**: Proper relevance score calculation and assignment for DocumentSearchService
  - RelevanceScore now correctly set in RedisDocumentRepository for proper ranking
  - Similarity score calculation from RediSearch distance metrics
  - Debug logging for score verification
  - **Files Modified**:
    - `src/SmartRAG/Repositories/RedisDocumentRepository.cs` - RelevanceScore assignment

- **Redis Embedding Generation**: Correct AIProviderConfig passing for embedding generation
  - IAIConfigurationService injection for proper config retrieval
  - Null check and fallback to text search when config missing
  - **Files Modified**:
    - `src/SmartRAG/Repositories/RedisDocumentRepository.cs` - AI config handling
    - `src/SmartRAG/Factories/StorageFactory.cs` - IAIConfigurationService injection

- **StorageFactory Dependency Injection**: Resolved scope issues with IAIProvider
  - Changed to use IServiceProvider for lazy resolution
  - Prevents Singleton/Scoped lifetime mismatch
  - **Files Modified**:
    - `src/SmartRAG/Factories/StorageFactory.cs` - Lazy dependency resolution
    - `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - IAIProvider lifetime adjustment

### 🐛 Fixed
- **StorageFactory DI Scope Issue**: Fixed InvalidOperationException when resolving IAIProvider
  - Changed from direct injection to lazy resolution via IServiceProvider
  - Prevents Singleton factory trying to inject Scoped service

- **Redis Relevance Scoring**: Fixed RelevanceScore being 0.0000 in search results
  - RelevanceScore now properly assigned from similarity calculation
  - DocumentSearchService can correctly rank results

- **Redis Embedding Config**: Fixed NullReferenceException when generating embeddings
  - AIProviderConfig now correctly retrieved and passed to GenerateEmbeddingAsync
  - Graceful fallback to text search when config unavailable

### 🗑️ Removed
- **FileSystemDocumentRepository**: Removed unused file system storage implementation
  - Repository file deleted (388 lines removed)
  - **Files Removed**:
    - `src/SmartRAG/Repositories/FileSystemDocumentRepository.cs`

- **SqliteDocumentRepository**: Removed unused SQLite storage implementation
  - Repository file deleted (618 lines removed)
  - **Files Removed**:
    - `src/SmartRAG/Repositories/SqliteDocumentRepository.cs`

- **StorageConfig Properties**: Removed unused configuration properties
  - FileSystemPath property removed
  - SqliteConfig property removed
  - **Files Modified**:
    - `src/SmartRAG/Models/StorageConfig.cs` - Property removal

### ✨ Benefits
- **Enhanced Redis Vector Search**: Proper similarity scoring and relevance ranking
- **Better Developer Experience**: Clear warnings and documentation for RediSearch requirements
- **Cleaner Codebase**: Removed 1000+ lines of unused code
- **Improved Reliability**: Fixed DI scope issues and null reference exceptions

### 📝 Notes
- **Breaking Changes**: FileSystem and SQLite document repositories removed
  - These were unused implementations
  - Active storage providers (Qdrant, Redis, InMemory) remain fully functional
  - If you were using FileSystem or SQLite, migrate to Qdrant, Redis, or InMemory

- **Redis Requirements**: Vector search requires RediSearch module
  - Use `redis/redis-stack-server:latest` Docker image
  - Or install RediSearch module on your Redis server
  - Without RediSearch, only text search works (no vector search)

## [3.2.0] - 2025-11-27

### 🏗️ Architectural Refactoring - Modular Design

#### **Strategy Pattern Implementation**
- **`ISqlDialectStrategy`**: Interface for database-specific SQL generation
- **Dialect Implementations**: SqliteDialectStrategy, PostgreSqlDialectStrategy, MySqlDialectStrategy, SqlServerDialectStrategy
- **`ISqlDialectStrategyFactory`**: Factory for creating appropriate dialect strategies
- **`IScoringStrategy`**: Interface for document relevance scoring
- **`IFileParser`**: Interface for file format parsing
- **Benefits**: Open/Closed Principle (OCP), easier to add new database support

#### **Repository Layer Separation**
- **`IConversationRepository`**: Dedicated interface for conversation data access
- **`IConversationManagerService`**: Business logic for conversation management
- **Repository Cleanup**: `IDocumentRepository` removed conversation-related methods
- **Benefits**: Separation of Concerns (SoC), Interface Segregation Principle (ISP)

#### **Service Layer Refactoring**
- **AI Service Decomposition**: `IAIConfigurationService`, `IAIRequestExecutor`, `IPromptBuilderService`, `IAIProviderFactory`
- **Database Services**: `IQueryIntentAnalyzer`, `IDatabaseQueryExecutor`, `IResultMerger`, `ISQLQueryGenerator`, `IDatabaseConnectionManager`, `IDatabaseSchemaAnalyzer`
- **Search Services**: `IEmbeddingSearchService`, `ISourceBuilderService`
- **Parser Services**: `IAudioParserService`, `IImageParserService`, `IAudioParserFactory`
- **Support Services**: `IQueryIntentClassifierService`, `ITextNormalizationService`
- **Benefits**: Single Responsibility Principle (SRP), better testability

#### **Model Consolidation**
- DatabaseSchema and DatabaseSchemaInfo unified

#### **Files Modified**
- `src/SmartRAG/Interfaces/` - New interfaces for Strategy Pattern
- `src/SmartRAG/Services/` - Service layer refactoring
- `src/SmartRAG/Repositories/` - Repository separation
- `src/SmartRAG/Models/` - Model consolidation
- `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Updated DI registrations

### Improved
- **Language-Agnostic Code Improvements**: Removed domain-specific and language-specific references from codebase
  - Replaced domain-specific examples ("orders", "products") with generic placeholders ("items", "TableName")
  - Removed hardcoded language-specific instructions from `PromptBuilderService` (Turkish, German, Russian, etc.)
  - Updated language handling to use generic ISO 639-1 code-based approach
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/Prompts/SqlPromptBuilder.cs` - Generic examples instead of domain-specific
    - `src/SmartRAG/Services/AI/PromptBuilderService.cs` - Generic language instruction instead of hardcoded languages
    - `src/SmartRAG/Models/SearchOptions.cs` - Generic language code documentation
    - `src/SmartRAG/Services/Database/Validation/SqlValidator.cs` - Generic table name in comment
  - **Benefits**: Full compliance with Rule 1 (Generic Code) and Rule 6 (Language References)

- **FilterStopWords Language-Agnostic Enhancement**: Improved keyword extraction to be fully language-agnostic
  - Added language-agnostic comments to `FilterStopWords` list
  - Added XML documentation to `ExtractFilterKeywords` method
  - Added `IsNumeric` helper to filter pure numeric keywords
  - Added more delimiters for better word extraction
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/Prompts/SqlPromptBuilder.cs` - Enhanced keyword extraction with language-agnostic approach
  - **Benefits**: Better keyword extraction across all languages, compliance with Rule 6

### Performance Improvements
- **Optimized AI Query Intent Analysis**: Eliminated redundant AI calls by adding overload method to `QueryMultipleDatabasesAsync` that accepts pre-analyzed query intent
  - `IMultiDatabaseQueryCoordinator.QueryMultipleDatabasesAsync(string, QueryIntent, int)` - New overload method to avoid redundant AI analysis
  - `DocumentSearchService` now passes pre-analyzed query intent to `MultiDatabaseQueryCoordinator` to prevent duplicate AI calls
  - **Files Modified**:
    - `src/SmartRAG/Interfaces/Database/IMultiDatabaseQueryCoordinator.cs` - Added overload method with pre-analyzed intent parameter
    - `src/SmartRAG/Services/Database/MultiDatabaseQueryCoordinator.cs` - Implemented overload method with null safety validation
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Updated to pass pre-analyzed query intent to coordinator

### Fixed
- **SQL Query Validation**: Fixed ORDER BY alias validation to correctly handle SELECT aliases in GROUP BY queries
  - Validation now recognizes SELECT aliases (e.g., `SUM(Quantity) AS TotalQuantity`) in ORDER BY clauses
  - Previously flagged valid SQL as errors when using aggregate aliases in ORDER BY
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - Enhanced validation logic to extract and validate SELECT aliases

### Improved
- **Cross-Database Query Prompt Enhancement**: Improved AI prompt guidance for cross-database queries
  - Added clearer examples for handling relationships across databases (e.g., "most sold category" requires sales data + category names)
  - Enhanced guidance on returning foreign keys and aggregates for application-level merging
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - Updated cross-database query pattern examples in AI prompts

- **Async/Await Pattern Improvements**: Eliminated blocking calls (`.Result`, `.Wait()`) and improved thread safety
  - `AudioConversionService` now uses `SemaphoreSlim` instead of `lock` for async initialization
  - `SqliteConversationRepository` now uses `SemaphoreSlim` for thread-safe async operations
  - `DocumentService.GetStorageStatisticsAsync` properly uses `await` instead of `.Result`
  - **Files Modified**:
    - `src/SmartRAG/Services/Parser/AudioConversionService.cs` - Replaced `lock` + `.Wait()` with `SemaphoreSlim`
    - `src/SmartRAG/Repositories/SqliteConversationRepository.cs` - Replaced `lock` + `.Result` with `SemaphoreSlim`
    - `src/SmartRAG/Services/Document/DocumentService.cs` - Changed `GetStorageStatisticsAsync` to use `await`
  - **Benefits**: Better async/await compliance, improved thread safety, no blocking operations


### Added
- **SearchOptions Support**: Per-request search configuration with granular control
  - `SearchOptions` model with flags: `EnableDatabaseSearch`, `EnableDocumentSearch`, `EnableAudioSearch`, `EnableImageSearch`
  - `PreferredLanguage` property for ISO 639-1 language code support
  - `IDocumentSearchService` methods now accept optional `SearchOptions?` parameter
  - Conditional service registration based on feature flags
  - **Flag-Based Document Filtering**: Query string flags for quick search type selection
    - `-db` flag: Enable database search only
    - `-d` flag: Enable document (text) search only
    - `-a` flag: Enable audio search only
    - `-i` flag: Enable image search only
    - Flags can be combined (e.g., `-db -a` for database + audio search)
    - Flags are parsed from query string and converted to `SearchOptions`
  - **Document Type Filtering**: Automatic filtering by content type
    - `IsTextDocument()`, `IsAudioDocument()`, `IsImageDocument()` helper methods
    - `GetAllDocumentsFilteredAsync()` filters documents based on enabled search types
  - **Files Modified**:
    - `src/SmartRAG/Models/SearchOptions.cs` - New model with feature flags
    - `src/SmartRAG/Interfaces/Document/IDocumentSearchService.cs` - Added `SearchOptions?` parameter
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Implemented SearchOptions support with document type filtering
    - `src/SmartRAG/Models/SmartRagOptions.cs` - Added feature toggles
  - **Benefits**: Fine-grained control over search behavior, per-request customization, quick flag-based filtering

- **Native Qdrant Text Search**: Token-based filtering for improved search performance
  - Native Qdrant text search with token-based OR filtering
  - Automatic stopword filtering (words < 3 characters)
  - Token match counting for relevance scoring
  - Fallback to vector search when text search fails
  - **Files Modified**:
    - `src/SmartRAG/Services/Storage/Qdrant/QdrantSearchService.cs` - Native text search implementation
    - `src/SmartRAG/Repositories/QdrantDocumentRepository.cs` - Integrated native text search
  - **Benefits**: Faster text-based searches, better keyword matching, reduced AI dependency

- **ClearAllAsync Methods**: Efficient bulk deletion operations
  - `IDocumentRepository.ClearAllAsync()` - Efficient bulk delete for all repositories
  - `IDocumentService.ClearAllDocumentsAsync()` - Clear all documents and embeddings
  - `IDocumentService.ClearAllEmbeddingsAsync()` - Clear embeddings while preserving documents
  - Qdrant implementation uses collection recreation for efficiency
  - **Files Modified**:
    - `src/SmartRAG/Interfaces/Document/IDocumentRepository.cs` - Added ClearAllAsync method
    - `src/SmartRAG/Interfaces/Document/IDocumentService.cs` - Added ClearAll methods
    - `src/SmartRAG/Repositories/QdrantDocumentRepository.cs` - Efficient collection recreation
    - `src/SmartRAG/Repositories/SqliteDocumentRepository.cs` - Bulk delete implementation
    - `src/SmartRAG/Repositories/RedisDocumentRepository.cs` - Pattern-based deletion
    - `src/SmartRAG/Repositories/InMemoryDocumentRepository.cs` - Clear implementation
    - `src/SmartRAG/Repositories/FileSystemDocumentRepository.cs` - Directory cleanup
    - `src/SmartRAG/Services/Document/DocumentService.cs` - Service-level clear methods
  - **Benefits**: Efficient bulk operations, better resource management

- **Collection Management Methods**: Enhanced Qdrant collection operations
  - `QdrantCollectionManager.RecreateCollectionAsync()` - Efficient collection recreation
  - `QdrantCollectionManager.DeleteCollectionAsync()` - Collection deletion
  - Improved collection initialization and management
  - **Files Modified**:
    - `src/SmartRAG/Services/Storage/Qdrant/QdrantCollectionManager.cs` - Collection management methods
  - **Benefits**: Better collection lifecycle management, efficient cleanup

- **Tesseract On-Demand Language Data Download**: Automatic language data management
  - Automatic download of Tesseract language data files when needed
  - Language code mapping (ISO 639-1/639-2 to Tesseract codes)
  - Support for 30+ languages with automatic detection
  - Normalized language code handling
  - **Files Modified**:
    - `src/SmartRAG/Services/Parser/ImageParserService.cs` - On-demand language download
  - **Benefits**: Reduced package size, automatic language support, better user experience

- **Currency Symbol Correction**: Improved OCR accuracy for financial documents
  - Automatic correction of common OCR misreads: `%`, `6`, `t`, `&` → currency symbols
  - Context-aware pattern matching for accurate correction
  - Applied to both OCR and PDF parsing
  - **Files Modified**:
    - `src/SmartRAG/Services/Parser/ImageParserService.cs` - Currency symbol correction
    - `src/SmartRAG/Services/Document/Parsers/PdfFileParser.cs` - PDF currency correction
  - **Benefits**: Better accuracy for financial documents, reduced manual correction

- **Parallel Batch Processing for Ollama Embeddings**: Performance optimization
  - Parallel batch processing for embedding generation with Ollama
  - Configurable batch size and parallelism
  - Improved throughput for large document sets
  - **Files Modified**:
    - `src/SmartRAG/Providers/BaseAIProvider.cs` - Parallel batch processing
    - `src/SmartRAG/Providers/CustomProvider.cs` - Ollama-specific optimizations
  - **Benefits**: Faster embedding generation, better resource utilization

- **Query Tokens Parameter**: Pre-computed token support for performance
  - `SearchDocumentsAsync` and `QueryIntelligenceAsync` now accept optional `queryTokens` parameter
  - Eliminates redundant tokenization when tokens are already computed
  - **Files Modified**:
    - `src/SmartRAG/Interfaces/Document/IDocumentSearchService.cs` - Added queryTokens parameter
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Token reuse implementation
  - **Benefits**: Reduced CPU usage, better performance for repeated queries

- **FeatureToggles Model**: Global feature flag configuration
  - `FeatureToggles` class for enabling/disabling features at global level
  - Properties: `EnableDatabaseSearch`, `EnableDocumentSearch`, `EnableAudioParsing`, `EnableImageParsing`
  - `SmartRagOptions.Features` property for centralized feature control
  - `SearchOptions.FromConfig()` static method to create SearchOptions from global config
  - **Files Modified**:
    - `src/SmartRAG/Models/SmartRagOptions.cs` - Added FeatureToggles class
    - `src/SmartRAG/Models/SearchOptions.cs` - Added FromConfig static method
    - `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Conditional service registration
  - **Benefits**: Centralized feature management, easier configuration

- **ContextExpansionService**: Adjacent chunk context expansion
  - `IContextExpansionService` interface for expanding document chunk context
  - Includes adjacent chunks from the same document for better context understanding
  - Configurable context window (default: 2 chunks before/after)
  - **Files Modified**:
    - `src/SmartRAG/Interfaces/Search/IContextExpansionService.cs` - New interface
    - `src/SmartRAG/Services/Search/ContextExpansionService.cs` - Implementation
  - **Benefits**: Better context for AI responses, improved answer quality

- **FileParserResult Model**: Standardized parser result structure
  - `FileParserResult` class for consistent parser output
  - Contains extracted content and metadata dictionary
  - Used by all file parsers for uniform result format
  - **Files Modified**:
    - `src/SmartRAG/Services/Document/Parsers/FileParserResult.cs` - New model
  - **Benefits**: Consistent parser interface, easier to extend

- **DatabaseFileParser**: SQLite database file parsing support
  - `DatabaseFileParser` implements `IFileParser` for SQLite database files
  - Supports `.db`, `.sqlite`, `.sqlite3`, `.db3` file extensions
  - Extracts schema and data information from database files
  - **Files Modified**:
    - `src/SmartRAG/Services/Document/Parsers/DatabaseFileParser.cs` - New parser
  - **Benefits**: Direct database file upload and parsing support

- **Native Library Inclusion**: Tesseract OCR native libraries bundled
  - Native libraries for Tesseract OCR and Leptonica included in NuGet package
  - Supports Windows (x64), macOS (x64, ARM64), and Linux (x64)
  - Automatic library loading with proper path configuration
  - DYLD_LIBRARY_PATH configuration for macOS
  - **Files Modified**:
    - `src/SmartRAG/SmartRAG.csproj` - Native library packaging
    - `src/SmartRAG/runtimes/` - Native library files
  - **Benefits**: No manual library installation required, works out-of-the-box

- **Nullable Reference Types**: Enhanced null safety
  - Enabled nullable reference types in 14+ files for better null safety
  - Improved compile-time null checking and warnings
  - Better API contracts and documentation
  - **Files Modified**:
    - Multiple interface and service files with `#nullable enable`
  - **Benefits**: Fewer null reference exceptions, better code quality

### Improved
- **Unicode Normalization for Qdrant**: Better text retrieval across languages
  - Unicode normalization for Qdrant text retrieval
  - Proper encoding preservation for all languages
  - **Files Modified**:
    - `src/SmartRAG/Repositories/QdrantDocumentRepository.cs` - Unicode normalization
    - `src/SmartRAG/Services/Storage/Qdrant/QdrantSearchService.cs` - Encoding preservation
  - **Benefits**: Better search results for non-ASCII characters, improved multilingual support

- **PDF OCR Encoding Issue Detection**: Automatic fallback handling
  - Automatic detection of PDF OCR encoding issues
  - Automatic fallback to alternative parsing methods
  - Better error handling and recovery
  - **Files Modified**:
    - `src/SmartRAG/Services/Document/Parsers/PdfFileParser.cs` - Encoding detection and fallback
  - **Benefits**: More reliable PDF processing, automatic error recovery

- **Numbered List Chunk Detection**: Improved counting query accuracy
  - Enhanced numbered list pattern detection
  - Prioritization of chunks containing numbered lists for counting queries
  - Better context expansion for list-based queries
  - **Files Modified**:
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Numbered list detection
    - `src/SmartRAG/Providers/CustomProvider.cs` - List-aware chunk selection
  - **Benefits**: More accurate counting queries, better list extraction

- **RAG Scoring Improvements**: Enhanced relevance calculation
  - Unique keyword bonus for better relevance scoring
  - Dynamic chunk boosting based on query type
  - Improved keyword matching algorithms
  - **Files Modified**:
    - `src/SmartRAG/Services/AI/PromptBuilderService.cs` - Scoring improvements
    - `src/SmartRAG/Helpers/QueryTokenizer.cs` - Enhanced tokenization
  - **Benefits**: More relevant search results, better answer quality

- **Document Search Adaptive Threshold**: Dynamic relevance threshold adjustment
  - Adaptive threshold based on query complexity
  - Improved chunk prioritization
  - Better handling of low-confidence results
  - **Files Modified**:
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Adaptive threshold logic
  - **Benefits**: Better balance between precision and recall

- **Prompt Builder Rules**: Enhanced AI answer generation
  - Improved prompt rules for better answer quality
  - Better context handling and instruction clarity
  - **Files Modified**:
    - `src/SmartRAG/Services/AI/PromptBuilderService.cs` - Enhanced prompt rules
  - **Benefits**: More accurate AI responses, better context understanding

- **QdrantDocumentRepository GetAllAsync**: Performance optimization
  - Optimized GetAllAsync implementation for Qdrant
  - Better memory usage and performance
  - **Files Modified**:
    - `src/SmartRAG/Repositories/QdrantDocumentRepository.cs` - Optimized GetAllAsync
  - **Benefits**: Faster document retrieval, lower memory footprint

- **Text Processing and AI Prompt Services**: General improvements
  - Enhanced text cleaning and normalization
  - Improved prompt construction
  - Better error handling
  - **Files Modified**:
    - `src/SmartRAG/Services/AI/PromptBuilderService.cs` - Text processing improvements
    - `src/SmartRAG/Services/Helpers/TextCleaningHelper.cs` - Enhanced cleaning
  - **Benefits**: Better text quality, improved AI understanding

- **Image Parser Service**: Comprehensive improvements
  - Better error handling and recovery
  - Improved configuration management
  - Enhanced OCR processing pipeline
  - **Files Modified**:
    - `src/SmartRAG/Services/Parser/ImageParserService.cs` - Comprehensive improvements
  - **Benefits**: More reliable OCR, better image processing

### Fixed
- **Table Alias Enforcement in SQL Generation**: Prevents ambiguous column errors
  - Enforced table aliases in SQL generation to prevent ambiguous column references
  - Better SQL validation and error prevention
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/Prompts/SqlPromptBuilder.cs` - Table alias enforcement
  - **Benefits**: More reliable SQL generation, fewer runtime errors

- **EnableDatabaseSearch Config Respect**: Configuration compliance
  - Fixed issue where `EnableDatabaseSearch` config was not being respected
  - Proper feature flag handling in conversational assistant
  - **Files Modified**:
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Config flag handling
  - **Benefits**: Proper feature control, better configuration compliance

- **macOS Native Libraries**: OCR library inclusion
  - Fixed macOS native library inclusion for Tesseract OCR
  - Proper DYLD_LIBRARY_PATH configuration
  - **Files Modified**:
    - `src/SmartRAG/Services/Parser/ImageParserService.cs` - macOS library handling
    - `src/SmartRAG/SmartRAG.csproj` - Native library packaging
  - **Benefits**: OCR works correctly on macOS, proper library loading

- **Missing Method Signature**: DocumentSearchService restoration
  - Restored missing method signature in DocumentSearchService
  - **Files Modified**:
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Method signature restoration
  - **Benefits**: API completeness, backward compatibility

### Changed
- **IEmbeddingSearchService Dependency Removal**: Simplified architecture
  - Removed `IEmbeddingSearchService` dependency from `DocumentSearchService`
  - Direct embedding search integration
  - **Files Modified**:
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Dependency removal
    - `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Registration cleanup
  - **Benefits**: Simpler architecture, reduced dependencies


- **Code Cleanup**: Inline comments and unused directives
  - Removed unnecessary inline comments
  - Cleaned up unused using directives
  - Improved code readability
  - **Files Modified**:
    - Multiple files across the codebase
  - **Benefits**: Cleaner codebase, better maintainability

- **Logging Cleanup**: Reduced verbose logging
  - Removed unnecessary verbose log messages
  - Improved log message quality
  - **Files Modified**:
    - `src/SmartRAG/Services/Shared/ServiceLogMessages.cs` - Log cleanup
    - Multiple service files - Logging improvements
  - **Benefits**: Cleaner logs, better performance

- **NuGet Package Updates**: Latest compatible versions
  - Updated all NuGet packages to latest compatible versions
  - **Files Modified**:
    - `src/SmartRAG/SmartRAG.csproj` - Package updates
  - **Benefits**: Latest features, security fixes, bug fixes

- **Service Method Annotations**: Better code documentation
  - Added `[AI Query]`, `[Document Query]`, and `[DB Query]` annotations to service methods
  - Better code organization and understanding
  - **Files Modified**:
    - `src/SmartRAG/Services/AI/AIRequestExecutor.cs` - Method annotations
    - `src/SmartRAG/Services/AI/AIService.cs` - Method annotations
    - `src/SmartRAG/Services/Document/DocumentService.cs` - Method annotations
  - **Benefits**: Better code documentation, clearer method purposes

### ✨ Benefits
- **Maintainability**: Cleaner, more modular codebase
- **Extensibility**: Easy to add new databases, AI providers, file formats
- **Testability**: Better unit testing with clear interfaces
- **Performance**: Optimized SQL generation, parallel processing, native text search
- **Flexibility**: Pluggable strategies for scoring, parsing, SQL generation
- **Backward Compatibility**: All existing code works without changes
- **Better Search**: Native text search, improved scoring, adaptive thresholds
- **Better OCR**: Currency correction, on-demand language support, encoding fixes
- **Better Management**: ClearAll methods, collection management, feature flags

### 📚 Migration Guide
All changes are backward compatible. Existing code continues to work without modifications.

#### Optional Enhancements

**Use SearchOptions for Per-Request Control**:
```csharp
// Old approach (still works)
var response = await _searchService.QueryIntelligenceAsync(query);

// New approach (recommended for fine-grained control)
var options = new SearchOptions
{
    EnableDatabaseSearch = true,
    EnableDocumentSearch = true,
    EnableAudioSearch = false,
    EnableImageSearch = false,
    PreferredLanguage = "en"
};
var response = await _searchService.QueryIntelligenceAsync(query, options: options);
```

**Use ClearAllAsync for Efficient Cleanup**:
```csharp
// Clear all documents efficiently
await _documentService.ClearAllDocumentsAsync();

// Clear only embeddings
await _documentService.ClearAllEmbeddingsAsync();
```

## [3.1.0] - 2025-11-11

### ✨ Unified Query Intelligence

#### **Major Feature: Unified Search Across All Data Sources**
- **Unified Query Intelligence**: `QueryIntelligenceAsync` now supports unified search across databases, documents, images (OCR), and audio (transcription) in a single query
- **Smart Hybrid Routing**: AI-based intent detection with confidence scoring automatically determines optimal search strategy
  - High confidence (>0.7) + database queries → Database query only
  - High confidence (>0.7) + no database queries → Document query only
  - Medium confidence (0.3-0.7) → Both database and document queries, merged results
  - Low confidence (<0.3) → Document query only (fallback)
- **QueryStrategy Enum**: New enum for query execution strategies (DatabaseOnly, DocumentOnly, Hybrid)
- **Intelligent Routing**: Improved query routing logic with graceful degradation and fallback mechanisms
- **Enhanced Error Handling**: Better error handling for database query failures

#### **New Services & Interfaces**
- `src/SmartRAG/Services/Database/QueryIntentAnalyzer.cs` - Analyzes user queries and determines which databases/tables to query using AI
- `src/SmartRAG/Services/Database/DatabaseQueryExecutor.cs` - Executes queries across multiple databases in parallel for better performance
- `src/SmartRAG/Services/Database/ResultMerger.cs` - Merges results from multiple databases into coherent responses using AI
- `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - Generates optimized SQL queries for each database based on query intent
- `src/SmartRAG/Interfaces/Database/IQueryIntentAnalyzer.cs` - Interface for query intent analysis
- `src/SmartRAG/Interfaces/Database/IDatabaseQueryExecutor.cs` - Interface for multi-database query execution
- `src/SmartRAG/Interfaces/Database/IResultMerger.cs` - Interface for result merging
- `src/SmartRAG/Interfaces/Database/ISQLQueryGenerator.cs` - Interface for SQL query generation

#### **New Enums**
- `src/SmartRAG/Enums/QueryStrategy.cs` - New enum for query execution strategies (DatabaseOnly, DocumentOnly, Hybrid)

#### **New Models**
- `src/SmartRAG/Models/AudioSegmentMetadata.cs` - Metadata model for audio transcription segments with timestamps and confidence scores

#### **Enhanced Models**
- `src/SmartRAG/Models/SearchSource.cs` - Enhanced with source type differentiation (Database, Document, Image, Audio)

#### **Files Modified**
- `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Major refactoring: Unified query intelligence implementation with hybrid routing (918+ lines changed)
- `src/SmartRAG/Services/Database/MultiDatabaseQueryCoordinator.cs` - Refactored to use new service architecture for better separation of concerns (355+ lines changed)
- `src/SmartRAG/Services/AI/AIService.cs` - Enhanced AI service with better error handling
- `src/SmartRAG/Services/Document/DocumentParserService.cs` - Improved document parsing with audio segment metadata support
- `src/SmartRAG/Interfaces/Document/IDocumentSearchService.cs` - Updated interface documentation
- `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Registered new services in DI container

### 🔧 Code Quality & AI Prompt Optimization

#### **Code Quality Improvements**
- **Build Quality**: Achieved 0 errors, 0 warnings across all projects
- **Code Standards**: Full compliance with project coding standards

#### **AI Prompt Optimization**
- **Emoji Reduction**: Reduced emoji usage in AI prompts from 235 to 5 (only critical: 🚨, ✓, ✗)
- **Token Efficiency**: Improved token efficiency (~100 tokens saved per prompt)
- **Strategic Usage**: Better AI comprehension through strategic emoji usage

#### **Files Modified**
- `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - Emoji optimization in AI prompts
- `src/SmartRAG/Services/Database/MultiDatabaseQueryCoordinator.cs` - Emoji optimization
- `src/SmartRAG/Services/Database/QueryIntentAnalyzer.cs` - Emoji optimization
- `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Emoji optimization

### ✨ Benefits
- **Single Query Interface**: Query all data sources (databases, documents, images, audio) with one method
- **Intelligent Routing**: AI automatically selects the best search strategy based on query intent and confidence scoring
- **Parallel Execution**: Multi-database queries execute in parallel for better performance
- **Modular Architecture**: New service-based architecture improves maintainability and testability
- **Better Separation of Concerns**: Each service has a single responsibility (SOLID principles)
- **Cleaner Codebase**: Zero warnings across all projects
- **Better Performance**: More efficient AI prompt processing and parallel query execution
- **Improved Maintainability**: Better code quality and standards compliance
- **Cost Efficiency**: Reduced token usage in AI prompts (~100 tokens saved per prompt)

### 📝 Notes
- Backward compatible: Existing `QueryIntelligenceAsync` signature unchanged
- If database coordinator not available, behavior identical to previous implementation
- No breaking changes to `RagResponse` model

## [3.0.3] - 2025-11-06

### 🎯 Package Optimization - Native Libraries

#### **Package Size Reduction**
- **Native Libraries Excluded**: Whisper.net.Runtime native libraries (ggml-*.dll, libggml-*.so, libggml-*.dylib) are no longer included in SmartRAG NuGet package
- **Tessdata Excluded**: `tessdata/eng.traineddata` file is no longer included in SmartRAG NuGet package
- **Reduced Package Size**: Significantly smaller NuGet package footprint
- **Cleaner Output**: No unnecessary native library files in project output directory

#### **Files Modified**
- `src/SmartRAG/SmartRAG.csproj` - Added `PrivateAssets="All"` to Whisper.net.Runtime package reference
- `src/SmartRAG/SmartRAG.csproj` - Added `Pack="false"` to tessdata/eng.traineddata content file

### ✨ Benefits
- **Smaller Package Size**: Reduced NuGet package size by excluding native libraries
- **Cleaner Projects**: No unnecessary native library files in project output
- **Better Dependency Management**: Native libraries come from their respective packages (Whisper.net.Runtime, Tesseract)
- **Consistent Behavior**: Matches behavior when directly referencing Whisper.net.Runtime package

### 📚 Migration Guide
If you're using OCR or Audio Transcription features:

**For Audio Transcription (Whisper.net):**
1. Add `Whisper.net.Runtime` package to your project:
   ```xml
   <PackageReference Include="Whisper.net.Runtime" Version="1.8.1" />
   ```
2. Native libraries will be automatically included from Whisper.net.Runtime package
3. No other changes required

**For OCR (Tesseract):**
1. Add `Tesseract` package to your project:
   ```xml
   <PackageReference Include="Tesseract" Version="5.2.0" />
   ```
2. Tesseract package includes tessdata files automatically
3. No other changes required

**Note**: If you're not using OCR or Audio Transcription features, no action is required. The packages are still downloaded as dependencies, but native libraries won't be included unless you explicitly reference the packages.

## [3.0.2] - 2025-10-24

### 🚀 BREAKING CHANGES - Google Speech-to-Text Removal

#### **Audio Processing Changes**
- **Google Speech-to-Text Removed**: Complete removal of Google Cloud Speech-to-Text integration
- **Whisper.net Only**: Audio transcription now exclusively uses Whisper.net for 100% local processing
- **Data Privacy**: All audio processing is now completely local, ensuring GDPR/KVKK/HIPAA compliance
- **Simplified Configuration**: Removed GoogleSpeechConfig and related configuration options

#### **Files Removed**
- `src/SmartRAG/Services/GoogleAudioParserService.cs` - Google Speech-to-Text service
- `src/SmartRAG/Models/GoogleSpeechConfig.cs` - Google Speech configuration model

#### **Files Modified**
- `src/SmartRAG/SmartRAG.csproj` - Removed Google.Cloud.Speech.V1 NuGet package
- `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Removed Google service registration
- `src/SmartRAG/Factories/AudioParserFactory.cs` - Simplified to Whisper.net only
- `src/SmartRAG/Models/SmartRagOptions.cs` - Removed GoogleSpeechConfig property
- `src/SmartRAG/Enums/AudioProvider.cs` - Removed GoogleCloud enum value
- `src/SmartRAG/Services/ServiceLogMessages.cs` - Updated log messages for Whisper.net

### ✨ Benefits
- **100% Local Processing**: All audio transcription happens locally with Whisper.net
- **Enhanced Privacy**: No data leaves your infrastructure
- **Simplified Setup**: No Google Cloud credentials required
- **Cost Effective**: No per-minute transcription costs
- **Multi-Language**: 99+ languages supported with automatic detection

### 🔧 Technical Details
- **Whisper.net Integration**: Uses OpenAI's Whisper model via Whisper.net bindings
- **Model Options**: Tiny (75MB), Base (142MB), Medium (1.5GB), Large-v3 (2.9GB)
- **Hardware Acceleration**: CPU, CUDA, CoreML, OpenVino support
- **Auto-Download**: Models automatically download on first use
- **Format Support**: MP3, WAV, M4A, AAC, OGG, FLAC, WMA

### 📚 Migration Guide
If you were using Google Speech-to-Text:
1. Remove any GoogleSpeechConfig from your configuration
2. Ensure WhisperConfig is properly configured
3. Update any custom audio processing code to use Whisper.net
4. Test audio transcription with local Whisper.net models

## [3.0.1] - 2025-10-22

### 🐛 Fixed
- **LoggerMessage Parameter Mismatch**: Fixed `LogAudioServiceInitialized` LoggerMessage definition with missing `configPath` parameter
- **EventId Conflicts**: Resolved duplicate EventId assignments in ServiceLogMessages.cs (6006, 6008, 6009)
- **TypeInitializationException**: Fixed critical startup error

### 🔧 Technical Improvements
- **ServiceLogMessages.cs**: Updated LoggerMessage definitions to match parameter counts correctly
- **EventId Management**: Reassigned conflicting EventIds to ensure unique logging identifiers

## [3.0.0] - 2025-10-22

### 🚀 BREAKING CHANGES - Intelligence Library Revolution

#### **Framework Requirements**
- **Minimum .NET Version**: Now requires .NET Standard 2.1 (.NET Core 3.0+)
- **Dropped Support**: .NET Framework 4.x and .NET Standard 2.0 no longer supported
- **Reason**: Enable modern API features, better performance, and align with current AI provider SDK requirements
- **Compatible With**: .NET Core 3.0+, .NET 5, .NET 6, .NET 7, .NET 8, .NET 9

#### **Major API Changes**
- **`GenerateRagAnswerAsync` → `QueryIntelligenceAsync`**: Method renamed to better represent intelligent query processing
- **Enhanced `IDocumentSearchService` interface**: New intelligent query processing method with advanced RAG pipeline
- **Service layer improvements**: Advanced semantic search and conversation management
- **Backward compatibility maintained**: Legacy methods marked as deprecated (will be removed in v4.0.0)

### 🔧 SQL Generation & Multi-Language Support

#### **Language-Safe SQL Generation**
- **Automatic validation**: Detection and prevention of non-English text in SQL queries
- **Enhanced SQL validation**: Strict validation preventing Turkish/German/Russian characters and keywords in SQL
- **Multi-language query support**: AI handles queries in any language while generating pure English SQL
- **Character validation**: Detection of non-English characters (Turkish: ç, ğ, ı, ö, ş, ü; German: ä, ö, ü, ß; Russian: Cyrillic)
- **Keyword validation**: Prevention of non-English keywords in SQL (sorgu, abfrage, запрос)
- **Improved error messages**: Better diagnostics with database type information in error reports

#### **PostgreSQL Full Support**
- **Complete integration**: Full PostgreSQL database support with live connections
- **Schema analysis**: Intelligent schema extraction and relationship mapping
- **Multi-database queries**: Cross-database query coordination with PostgreSQL
- **Production ready**: Comprehensive testing and validation

### 🔒 On-Premise & Local AI Support

#### **Complete Local Operation**
- **Local AI models**: Full support for Ollama, LM Studio, and any OpenAI-compatible local API
- **Document processing**: PDF, Word, Excel parsing - completely local
- **OCR processing**: Tesseract 5.2.0 - completely local, no data sent to cloud
- **Database integration**: SQLite, SQL Server, MySQL, PostgreSQL - all local connections
- **Storage options**: In-Memory, SQLite, FileSystem, Redis - all local
- **Complete privacy**: All your data stays on your infrastructure

#### **Enterprise Compliance**
- **GDPR compliant**: Keep all data within your infrastructure
- **KVKK compliant**: Turkish data protection law compliance
- **Air-gapped systems**: Works without internet (except for audio transcription)
- **Financial institutions**: Bank-grade security with local deployment
- **Healthcare**: HIPAA-compliant deployments possible
- **Government**: Classified data handling with local models

### ⚠️ Important Limitations Documented

#### **Audio Files**
- **Google Speech-to-Text**: Audio transcription uses Google Cloud AI for enterprise-grade speech recognition
- **Whisper.net**: Local audio transcription option for privacy-sensitive deployments
- **Data privacy**: Whisper.net processes audio locally, Google Speech-to-Text sends to cloud
- **Multi-language**: Both providers support 99+ languages with automatic detection
- **Other formats**: All other file types (PDF, Word, Excel, Images, Databases) remain completely local

#### **OCR (Image to Text)**
- **Handwriting limitation**: Tesseract OCR library cannot fully support handwritten text (success rate is very low)
- **Works perfectly**: Printed documents, scanned printed documents, digital screenshots with typed text
- **Limited support**: Handwritten notes, handwritten forms, cursive writing (very low accuracy, not recommended)
- **Best results**: High-quality scans of printed documents, clear digital images with printed text
- **Supported languages**: 100+ languages - [View all supported languages](https://github.com/tesseract-ocr/tessdata)
- **Recommendation**: Use printed text documents for optimal OCR results

### ✨ Added
- **Local AI setup examples**: Configuration examples for Ollama and LM Studio
- **Enterprise use cases**: Documented use cases for Banking, Healthcare, Legal, Government, Manufacturing, and Consulting

### 🔧 Improved
- **Retry mechanism**: Enhanced retry prompts with language-specific instructions
- **Error handling**: Better error messages with database type information
- **Code quality**: SOLID/DRY principles maintained throughout
- **Performance**: Optimized multi-database query coordination

### ✅ Quality Assurance
- **Zero Warnings Policy**: All changes maintain 0 errors, 0 warnings standard
- **SOLID Principles**: Clean code architecture maintained throughout
- **Comprehensive Testing**: Multi-database test coverage with PostgreSQL integration
- **Security hardening**: Enhanced configuration file management and credential protection
- **Performance optimization**: Maintained high performance across all features

### 🔄 Migration Guide (v2.3.0 → v3.0.0)

#### **Service Layer Method Changes**
```csharp
// OLD (v2.3.0)
await _documentSearchService.GenerateRagAnswerAsync(query, maxResults);

// NEW (v3.0.0)  
await _documentSearchService.QueryIntelligenceAsync(query, maxResults);
```

#### **Backward Compatibility**
- Legacy methods are deprecated but still work (will be removed in v4.0.0)
- Update endpoints and methods at your own pace
- No immediate breaking changes if you continue using old methods

## [2.3.1] - 2025-10-20

### 🐛 Bug Fixes
- **LoggerMessage Parameter Mismatch**: Fixed LoggerMessage.Define parameter mismatch in ServiceLogMessages.LogAudioServiceInitialized
- **Format String Correction**: Corrected format string parameter count to prevent System.ArgumentException during service initialization
- **Logging Stability**: Improved logging stability for Google Speech-to-Text service initialization

### 🔧 Technical Improvements
- **Logging Infrastructure**: Enhanced logging infrastructure reliability
- **Zero Warnings Policy**: Compliance maintained
- **Test Coverage**: All tests passing (8/8)

## [2.3.0] - 2025-09-16

### ✨ Added
- **Google Speech-to-Text Integration**: Enterprise-grade speech recognition with Google Cloud AI
- **Enhanced Language Support**: 100+ languages including Turkish, English, and global languages
- **Real-time Audio Processing**: Advanced speech-to-text conversion with confidence scoring
- **Detailed Transcription Results**: Segment-level transcription with timestamps and confidence metrics
- **Automatic Format Detection**: Support for MP3, WAV, M4A, AAC, OGG, FLAC, WMA formats
- **Intelligent Audio Processing**: Smart audio stream validation and error handling
- **Performance Optimized**: Efficient audio processing with minimal memory footprint
- **Structured Audio Output**: Converts audio content to searchable, queryable knowledge base
- **Comprehensive XML Documentation**: Complete API documentation for all public classes and methods

### 🔧 Improved
- **Audio Processing Pipeline**: Enhanced audio processing with Google Cloud AI
- **Configuration Management**: Updated all configuration files to use GoogleSpeechConfig
- **Error Handling**: Enhanced error handling for audio transcription operations

## [2.2.0] - 2025-09-15

### ✨ Added
- **Use Case Examples**: Added detailed examples for scanned documents, receipts, and image content processing

### 🔧 Improved
- **Package Metadata**: Updated project URLs and release notes for better user experience

## [2.1.0] - 2025-09-05

### ✨ Added
- **Automatic Session Management**: No more manual session ID handling required
- **Persistent Conversation History**: Conversations survive application restarts
- **New Conversation Commands**: `/new`, `/reset`, `/clear` for conversation control
- **Enhanced API**: Backward-compatible with optional `startNewConversation` parameter
- **Storage Integration**: Works seamlessly with all providers (Redis, SQLite, FileSystem, InMemory)

### 🔧 Improved
- **Format Consistency**: Standardized conversation format across all storage providers
- **Thread Safety**: Enhanced concurrent access handling for conversation operations
- **Platform Agnostic**: Maintains compatibility across all .NET environments

## [2.0.0] - 2025-08-27

### 🔄 **BREAKING CHANGE: .NET Standard Migration**
- **Target Framework**: Migrated from .NET 9.0 to .NET Standard 2.0/2.1
- **Framework Compatibility**: Now supports .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+
- **Maximum Reach**: Enhanced compatibility with legacy and enterprise environments

### ✨ Added
- **Cross-Platform Support**: .NET Standard 2.0/2.1 target frameworks for maximum compatibility
- **Legacy Framework Support**: Full compatibility with .NET Framework applications
- **Enterprise Integration**: Seamless integration with existing enterprise .NET solutions

### 🔧 Improved
- **Language Compatibility**: C# 7.3 syntax compatibility for .NET Standard 2.0/2.1
- **Package Versions**: Updated all NuGet packages to .NET Standard compatible versions
- **API Compatibility**: Maintained all existing functionality while ensuring framework compatibility

### 🧪 Testing
- **Framework Compatibility**: Verified compatibility with .NET Standard 2.0/2.1
- **Backward Compatibility**: Ensured all existing functionality remains intact
- **Package Compatibility**: Tested all NuGet packages with target frameworks

### 🔒 Security
- **Zero Warnings**: Maintained strict code quality standards
- **SOLID Principles**: Preserved enterprise-grade architecture
- **Package Security**: Updated packages to address security vulnerabilities

## [1.1.0] - 2025-08-22

### ✨ Added
- **Excel Document Support**: Comprehensive Excel file parsing (.xlsx, .xls) with intelligent content extraction
- **EPPlus 8.1.0 Integration**: Modern Excel processing library with proper non-commercial license setup
- **Worksheet Parsing**: Intelligent parsing of all worksheets with tab-separated data preservation
- **Enhanced Content Validation**: Improved content quality checks with Excel-specific fallback handling
- **Anthropic API Reliability**: Enhanced retry mechanism for HTTP 529 (Overloaded) errors

### 🔧 Improved
- **API Error Handling**: Better retry logic for rate limiting and server overload scenarios
- **Content Processing**: More robust document parsing with fallback error messages
- **Performance**: Optimized Excel content extraction and validation

### 🧪 Testing
- **Excel Parsing**: Verified with various Excel formats and content types
- **API Retry**: Tested retry mechanism with error scenarios
- **Backward Compatibility**: Ensured all existing functionality remains intact

### 🔒 Security
- **License Compliance**: Proper EPPlus non-commercial license setup
- **Zero Warnings**: Maintained strict code quality standards

## [1.0.3] - Previous Release

### 🔧 Fixed
- LoggerMessage parameter count mismatches
- Provider logging message implementations
- Service collection registration issues

---

## Version History

- **3.7.0** (2026-01-19) - Cross-Database Mapping Detector, Security Improvements, SQL Script Extraction
- **3.6.0** (2025-12-30) - CancellationToken Support, Performance Improvements, Code Quality Enhancements
- **3.5.0** (2025-12-27) - Code Quality Improvements & Architecture Refactoring
- **3.4.0** (2025-12-12) - MCP Integration, File Watcher, Query Strategy Optimization
- **3.3.0** (2025-12-01) - Redis Vector Search & Storage Improvements
- **3.2.0** (2025-11-27) - Architectural Refactoring, Strategy Pattern Implementation
- **3.1.0** (2025-11-11) - Unified Query Intelligence, Smart Hybrid Routing, New Service Architecture
- **3.0.3** (2025-11-06) - Package Optimization - Native Libraries Excluded
- **3.0.2** (2025-10-24) - Google Speech-to-Text removal, Whisper.net only
- **3.0.1** (2025-10-22) - Bug fixes, Logging stability improvements
- **3.0.0** (2025-10-22) - Intelligence Library Revolution, SQL Generation, On-Premise Support
- **2.3.1** (2025-10-20) - Bug fixes, Logging stability improvements
- **2.3.0** (2025-09-16) - Google Speech-to-Text integration, Audio processing
- **2.2.0** (2025-09-15) - OCR use case examples
- **2.1.0** (2025-09-05) - Automatic session management, Persistent conversation history
- **2.0.0** (2025-08-27) - .NET Standard 2.0/2.1 migration
- **1.1.0** (2025-08-22) - Excel support, EPPlus integration
- **1.0.3** (2025-08-20) - Bug fixes and logging improvements
- **1.0.2** (2025-08-19) - Initial stable release
- **1.0.1** (2025-08-17) - Beta release
- **1.0.0** (2025-08-15) - Initial release
