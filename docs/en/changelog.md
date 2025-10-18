---
layout: default
title: Changelog
description: Version history and release notes for SmartRAG
lang: en
---

<div class="page-content">
    <div class="container">
        <!-- Version History Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Version History</h2>
                    <p>Complete history of SmartRAG releases with detailed change information.</p>

                    <h3>Version 3.0.0 - 2025-10-18</h3>
                    <div class="alert alert-success">
                        <h4><i class="fas fa-star me-2"></i>Latest Release</h4>
                        <p class="mb-0">Intelligence Platform Revolution - Enhanced SQL generation with multi-language support and comprehensive database integration.</p>
                    </div>

                    <h4>SQL Generation & Multi-Language Support</h4>
                    <ul>
                        <li><strong>Language-Safe SQL Generation</strong>: Automatic detection and prevention of non-English text in SQL queries</li>
                        <li><strong>Enhanced SQL Validation</strong>: Strict validation preventing Turkish/German/Russian characters and keywords in SQL</li>
                        <li><strong>Multi-Language Query Support</strong>: AI handles queries in any language while generating pure English SQL</li>
                        <li><strong>Character Validation</strong>: Detection of non-English characters (ç, ğ, ı, ö, ş, ü, ä, ö, ü, ß, Cyrillic)</li>
                        <li><strong>Keyword Validation</strong>: Prevention of non-English keywords in SQL (sorgu, abfrage, запрос)</li>
                        <li><strong>PostgreSQL Full Support</strong>: Complete PostgreSQL integration and validation</li>
                    </ul>

                    <h4>Added</h4>
                    <ul>
                        <li><strong>Google Speech-to-Text Integration</strong>: Enterprise-grade speech recognition with Google Cloud AI</li>
                        <li><strong>Enhanced Language Support</strong>: 100+ languages including Turkish, English, and global languages</li>
                        <li><strong>Real-time Audio Processing</strong>: Advanced speech-to-text conversion with confidence scoring</li>
                        <li><strong>Detailed Transcription Results</strong>: Segment-level transcription with timestamps and confidence metrics</li>
                        <li><strong>Automatic Format Detection</strong>: Support for MP3, WAV, M4A, AAC, OGG, FLAC, WMA formats</li>
                        <li><strong>Intelligent Audio Processing</strong>: Smart audio stream validation and error handling</li>
                        <li><strong>Performance Optimized</strong>: Efficient audio processing with minimal memory footprint</li>
                        <li><strong>Structured Audio Output</strong>: Converts audio content to searchable, queryable knowledge base</li>
                        <li><strong>Comprehensive XML Documentation</strong>: Complete API documentation for all public classes and methods</li>
                    </ul>

                    <h4>Improved</h4>
                    <ul>
                        <li><strong>Audio Processing Pipeline</strong>: Enhanced audio processing with Google Cloud AI</li>
                        <li><strong>Configuration Management</strong>: Updated all configuration files to use GoogleSpeechConfig</li>
                        <li><strong>Error Handling</strong>: Enhanced error handling for audio transcription operations</li>
                        <li><strong>Documentation</strong>: Updated all language versions with Google Speech-to-Text examples</li>
                        <li><strong>Code Quality</strong>: Zero warnings policy compliance with SOLID/DRY principles</li>
                        <li><strong>Security</strong>: Fixed CodeQL high severity vulnerability with log injection protection</li>
                    </ul>

                    <h4>Documentation</h4>
                    <ul>
                        <li><strong>Audio Processing</strong>: Comprehensive audio processing feature documentation</li>
                        <li><strong>Multi-language Support</strong>: Updated all language versions (EN, TR, DE, RU) with examples</li>
                        <li><strong>API Documentation</strong>: Complete XML documentation for all public APIs</li>
                        <li><strong>Developer Experience</strong>: Better developer experience with detailed audio processing examples</li>
                    </ul>

                    <h3>Version 2.2.0 - 2025-09-15</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-info-circle me-2"></i>Previous Release</h4>
                        <p class="mb-0">Previous stable release with enhanced OCR documentation and visibility improvements.</p>
                    </div>
                    <ul>
                        <li><strong>Enhanced OCR Documentation</strong>: Comprehensive documentation showcasing OCR capabilities with real-world use cases</li>
                        <li><strong>Improved README</strong>: Detailed image processing features highlighting Tesseract 5.2.0 + SkiaSharp integration</li>
                        <li><strong>Use Case Examples</strong>: Added detailed examples for scanned documents, receipts, and image content processing</li>
                        <li><strong>Package Metadata</strong>: Updated project URLs and release notes for better user experience</li>
                        <li><strong>Documentation Structure</strong>: Enhanced documentation showcasing OCR as key differentiator</li>
                        <li><strong>User Guidance</strong>: Improved guidance for image-based document processing workflows</li>
                        <li><strong>WebP Support</strong>: Highlighted WebP to PNG conversion and multi-language OCR support</li>
                        <li><strong>Developer Experience</strong>: Better visibility of image processing features for developers</li>
                    </ul>

                    <h3>Version 2.1.0 - 2025-09-05</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-info-circle me-2"></i>Previous Release</h4>
                        <p class="mb-0">Previous stable release with automatic session management and conversation history.</p>
                    </div>
                    <ul>
                        <li><strong>Automatic Session Management</strong>: No more manual session ID handling required</li>
                        <li><strong>Persistent Conversation History</strong>: Conversations survive application restarts</li>
                        <li><strong>New Conversation Commands</strong>: /new, /reset, /clear for conversation control</li>
                        <li><strong>Enhanced API</strong>: Backward-compatible with optional startNewConversation parameter</li>
                        <li><strong>Storage Integration</strong>: Works seamlessly with all providers (Redis, SQLite, FileSystem, InMemory)</li>
                        <li><strong>Format Consistency</strong>: Standardized conversation format across all storage providers</li>
                        <li><strong>Thread Safety</strong>: Enhanced concurrent access handling for conversation operations</li>
                        <li><strong>Platform Agnostic</strong>: Maintains compatibility with all .NET environments</li>
                        <li><strong>Documentation Updates</strong>: All language versions (EN, TR, DE, RU) updated with real examples</li>
                        <li><strong>100% Compliance</strong>: All established rules maintained with zero warnings policy</li>
                    </ul>

                    <h3>Version 2.0.0 - 2025-08-27</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-info-circle me-2"></i>Previous Release</h4>
                        <p class="mb-0">Previous stable release with .NET Standard 2.0/2.1 migration.</p>
                    </div>
                    <ul>
                        <li><strong>.NET Standard 2.0/2.1</strong>: Compatibility with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+</li>
                        <li><strong>Maximum Compatibility</strong>: Support for legacy and enterprise .NET applications</li>
                        <li><strong>Framework Change</strong>: Migration from .NET 9.0 to .NET Standard</li>
                        <li><strong>Package Dependencies</strong>: Updated package versions for compatibility</li>
                    </ul>

                    <h3>Version 1.1.0 - 2025-08-22</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-info-circle me-2"></i>Previous Release</h4>
                        <p class="mb-0">Previous stable release with Excel support and enhanced features.</p>
                    </div>

                    <h4>Added</h4>
                    <ul>
                        <li><strong>💬 Conversation History</strong>: Automatic session-based conversation management with context awareness</li>
                        <li><strong>Session Management</strong>: Unique session IDs for maintaining conversation context across multiple questions</li>
                        <li><strong>Intelligent Context Truncation</strong>: Smart conversation history truncation to maintain optimal performance</li>
                        <li><strong>Storage Integration</strong>: Conversation data storage using configured storage providers (Redis, SQLite, etc.)</li>
                        <li><strong>Enhanced API</strong>: Updated GenerateRagAnswerAsync method with sessionId parameter</li>
                        <li><strong>Real Examples</strong>: Updated all documentation examples to use actual implementation code</li>
                    </ul>

                    <h4>Improved</h4>
                    <ul>
                        <li><strong>Documentation Reality</strong>: All examples now match actual codebase implementation</li>
                        <li><strong>Multi-language Support</strong>: Updated all language versions (EN, TR, DE, RU) with conversation features</li>
                        <li><strong>API Consistency</strong>: Ensured all API examples use real SearchController and SearchRequest models</li>
                        <li><strong>Code Quality</strong>: Applied Zero Warnings Policy with 0 errors, 0 warnings, 0 messages</li>
                    </ul>

                    <h4>Fixed</h4>
                    <ul>
                        <li><strong>Documentation Accuracy</strong>: Removed all fictional examples and replaced with real implementation</li>
                        <li><strong>Build Compliance</strong>: Achieved 100% compliance with SOLID and DRY principles</li>
                        <li><strong>Magic Numbers</strong>: Converted all magic numbers to named constants</li>
                        <li><strong>Logging Standards</strong>: Implemented LoggerMessage delegates for all conversation operations</li>
                    </ul>

                    <h3>Version 1.1.0 - 2025-08-22</h3>

                    <h4>Added</h4>
                    <ul>
                        <li><strong>Excel File Support</strong>: Added Excel file parsing (.xlsx, .xls) with EPPlus 8.1.0 integration</li>
                        <li><strong>Enhanced Retry Logic</strong>: Improved Anthropic API retry mechanism for HTTP 529 (Overloaded) errors</li>
                        <li><strong>Content Validation</strong>: Enhanced document content validation</li>
                        <li><strong>Excel Documentation</strong>: Comprehensive Excel format documentation</li>
                    </ul>

                    <h3>Version 1.0.3 - 2025-08-20</h3>
                    
                    <h4>Added</h4>
                    <ul>
                        <li><strong>Multi-language Support</strong>: Added comprehensive documentation in English, Turkish, German, and Russian</li>
                        <li><strong>GitHub Pages</strong>: Complete documentation site with modern Bootstrap design</li>
                        <li><strong>Enhanced Examples</strong>: Added comprehensive code examples and tutorials</li>
                        <li><strong>Troubleshooting Guide</strong>: Detailed troubleshooting and debugging information</li>
                        <li><strong>Contributing Guidelines</strong>: Complete contribution guide with coding standards</li>
                    </ul>

                    <h4>Improved</h4>
                    <ul>
                        <li><strong>Documentation</strong>: Complete rewrite with modern design and better organization</li>
                        <li><strong>Code Examples</strong>: More realistic and comprehensive examples</li>
                        <li><strong>API Reference</strong>: Detailed API documentation with usage patterns</li>
                        <li><strong>Configuration Guide</strong>: Enhanced configuration options and best practices</li>
                    </ul>

                    <h4>Fixed</h4>
                    <ul>
                        <li><strong>Type Conflicts</strong>: Resolved conflicts between Qdrant, OpenXML, and other libraries</li>
                        <li><strong>Global Usings</strong>: Implemented GlobalUsings for all projects to reduce code duplication</li>
                        <li><strong>Build Issues</strong>: Fixed various compilation and build warnings</li>
                    </ul>

                    <h3>Version 1.0.2 - 2025-08-19</h3>
                    
                    <h4>Added</h4>
                    <ul>
                        <li><strong>Global Usings</strong>: Implemented GlobalUsings for SmartRAG core library</li>
                        <li><strong>Type Resolution</strong>: Added explicit type resolution for conflicting types</li>
                        <li><strong>Enhanced Logging</strong>: Improved logging with LoggerMessage delegates</li>
                    </ul>

                    <h4>Improved</h4>
                    <ul>
                        <li><strong>Code Organization</strong>: Better #region organization and SOLID principles</li>
                        <li><strong>Performance</strong>: Optimized document processing and storage operations</li>
                        <li><strong>Error Handling</strong>: Enhanced error handling and exception management</li>
                    </ul>

                    <h4>Fixed</h4>
                    <ul>
                        <li><strong>Build Warnings</strong>: Resolved all compiler warnings and messages</li>
                        <li><strong>Type Conflicts</strong>: Fixed conflicts between external library types</li>
                        <li><strong>Memory Leaks</strong>: Improved resource disposal and memory management</li>
                    </ul>

                    <h3>Version 1.0.1 - 2025-08-17</h3>
                    
                    <h4>Added</h4>
                    <ul>
                        <li><strong>Test Project</strong>: Added comprehensive xUnit test suite</li>
                        <li><strong>Example Web API</strong>: Complete example web application</li>
                        <li><strong>Documentation</strong>: Initial documentation structure</li>
                    </ul>

                    <h4>Improved</h4>
                    <ul>
                        <li><strong>Code Quality</strong>: Applied SOLID and DRY principles</li>
                        <li><strong>Error Handling</strong>: Better exception handling and validation</li>
                        <li><strong>Logging</strong>: Structured logging throughout the application</li>
                    </ul>

                    <h4>Fixed</h4>
                    <ul>
                        <li><strong>Minor Bugs</strong>: Various bug fixes and improvements</li>
                        <li><strong>Performance</strong>: Optimized document processing</li>
                        <li><strong>Security</strong>: Enhanced input validation and sanitization</li>
                    </ul>

                    <h3>Version 1.0.0 - 2025-08-15</h3>
                    
                    <h4>Initial Release</h4>
                    <ul>
                        <li><strong>Core RAG Functionality</strong>: Document processing, embedding generation, and semantic search</li>
                        <li><strong>AI Provider Support</strong>: OpenAI, Anthropic, Azure OpenAI, and Gemini integration</li>
                        <li><strong>Storage Providers</strong>: Qdrant, Redis, SQLite, In-Memory, and File System support</li>
                        <li><strong>Document Formats</strong>: PDF, Word, Excel, and text document processing</li>
                        <li><strong>.NET 8 Support</strong>: Full compatibility with .NET 8 LTS</li>
                        <li><strong>Dependency Injection</strong>: Native .NET dependency injection support</li>
                        <li><strong>Async/Await</strong>: Full asynchronous operation support</li>
                        <li><strong>Extensible Architecture</strong>: Plugin-based provider system</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Versioning Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Versioning</h2>
                    <p>SmartRAG follows <a href="https://semver.org/" target="_blank">Semantic Versioning</a> (SemVer):</p>
                    
                    <ul>
                        <li><strong>MAJOR</strong>: Incompatible API changes</li>
                        <li><strong>MINOR</strong>: New functionality in a backwards compatible manner</li>
                        <li><strong>PATCH</strong>: Backwards compatible bug fixes</li>
                    </ul>

                    <h3>Release Schedule</h3>
                    <ul>
                        <li><strong>Major Releases</strong>: Every 6-12 months with significant new features</li>
                        <li><strong>Minor Releases</strong>: Every 2-3 months with new functionality</li>
                        <li><strong>Patch Releases</strong>: As needed for critical bug fixes</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Breaking Changes Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Breaking Changes</h2>
                    <p>Important breaking changes between versions.</p>
                    
                    <div class="alert alert-info">
                        <h4><i class="fas fa-info-circle me-2"></i>Good News</h4>
                        <p class="mb-0">No breaking changes between versions 1.0.0 and 1.2.0. All updates are backward compatible.</p>
                    </div>

                    <h3>Migration Guides</h3>
                    <p>All version updates from 1.0.0 to 1.2.0 are fully backward compatible. No migration is required.</p>
                    
                    <h4>New Conversation History Feature</h4>
                    <p>To use the new conversation history feature, simply add a <code>SessionId</code> parameter to your existing API calls:</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Before (still works)
var response = await _documentSearchService.GenerateRagAnswerAsync(query, maxResults);

// After (with conversation history)
var response = await _documentSearchService.GenerateRagAnswerAsync(query, sessionId, maxResults);</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Support Policy Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Support Policy</h2>
                    <p>Our commitment to supporting different versions of SmartRAG.</p>
                    
                    <ul>
                        <li><strong>Current Version</strong>: Full support and bug fixes</li>
                        <li><strong>Previous Version</strong>: Security updates and critical bug fixes only</li>
                        <li><strong>Older Versions</strong>: No support</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Roadmap Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Roadmap</h2>
                    <p>Upcoming features and future plans for SmartRAG.</p>
                    
                    <h3>Upcoming Features (1.3.0)</h3>
                    <ul>
                        <li><strong>Advanced Chunking</strong>: Intelligent document chunking strategies</li>
                        <li><strong>Custom Embeddings</strong>: Support for custom embedding models</li>
                        <li><strong>Batch Processing</strong>: Improved batch document processing</li>
                        <li><strong>Performance Monitoring</strong>: Built-in performance metrics and monitoring</li>
                        <li><strong>Cloud Integration</strong>: Enhanced cloud provider support</li>
                    </ul>

                    <h3>Future Plans (2.0.0)</h3>
                    <ul>
                        <li><strong>Multi-modal Support</strong>: Image and audio document processing</li>
                        <li><strong>Advanced Search</strong>: Semantic search with context awareness</li>
                        <li><strong>Real-time Updates</strong>: Live document indexing and search</li>
                        <li><strong>Distributed Processing</strong>: Support for distributed deployments</li>
                        <li><strong>Advanced Analytics</strong>: Document usage and search analytics</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Contributing Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Contributing to Changelog</h2>
                    <p>When contributing to SmartRAG, please update the changelog:</p>
                    
                    <ol>
                        <li><strong>Add your changes</strong> to the appropriate section</li>
                        <li><strong>Use consistent formatting</strong> following the existing style</li>
                        <li><strong>Group changes</strong> by type (Added, Improved, Fixed, etc.)</li>
                        <li><strong>Provide clear descriptions</strong> of what changed</li>
                        <li><strong>Include breaking changes</strong> in a separate section</li>
                    </ol>

                    <h3>Changelog Entry Format</h3>
                    <div class="code-example">
                        <pre><code class="language-markdown">### Added
- **Feature Name**: Brief description of the new feature

### Improved
- **Component Name**: Description of improvements made

### Fixed
- **Issue Description**: Description of the bug fix</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <div class="alert alert-info">
                        <h4><i class="fas fa-question-circle me-2"></i>Need Help?</h4>
                        <p class="mb-0">If you need assistance with version updates:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/en/getting-started">Getting Started</a></li>
                            <li><a href="{{ site.baseurl }}/en/configuration">Configuration</a></li>
                            <li><a href="{{ site.baseurl }}/en/api-reference">API Reference</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub Issues</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">Email Support</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>
