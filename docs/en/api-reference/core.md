---
layout: default
title: Core Interfaces
description: Essential SmartRAG interfaces for document search, management, parsing, and AI services
lang: en
---

## Core Interfaces

> **Note:** Interface details are available in the source code with XML documentation. Use your IDE's IntelliSense to explore method signatures, parameters, and return types. For practical usage examples, see the [Examples]({{ site.baseurl }}/en/examples) section.

All interfaces are located in the `SmartRAG.Interfaces` namespace. To view interface definitions:

1. **In your IDE**: Navigate to the interface using "Go to Definition" (F12 in Visual Studio/VS Code)
2. **XML Documentation**: All interfaces include XML comments with detailed parameter and return type information
3. **Source Code**: Browse the `src/SmartRAG/Interfaces/` directory in the repository
4. **Examples**: See practical usage in the [Examples]({{ site.baseurl }}/en/examples) section

## Main Public Interfaces

- **`IDocumentSearchService`** - AI-powered intelligent query processing with RAG pipeline and conversation management
- **`IDocumentService`** - Document CRUD operations and management
- **`IConversationManagerService`** - Conversation session management and history tracking
- **`IDocumentParserService`** - Multi-format document parsing and text extraction
- **`IDatabaseParserService`** - Universal database support with live connections
- **`IAIService`** - AI provider communication for text generation and embeddings
- **`IStorageFactory`** - Factory for creating storage repositories
- **`IPromptBuilderService`** - Service for building AI prompts

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-lightbulb"></i>
            </div>
            <h3>Examples</h3>
            <p>See practical code examples and real-world implementations</p>
            <a href="{{ site.baseurl }}/en/examples/quick" class="btn btn-outline-primary btn-sm mt-3">
                View Examples
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-rocket"></i>
            </div>
            <h3>Getting Started</h3>
            <p>Quick installation and setup guide</p>
            <a href="{{ site.baseurl }}/en/getting-started" class="btn btn-outline-primary btn-sm mt-3">
                Get Started
            </a>
        </div>
    </div>
</div>
