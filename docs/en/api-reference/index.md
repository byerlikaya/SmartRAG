---
layout: default
title: API Reference
description: Complete API documentation for SmartRAG interfaces, methods, and models
lang: en
---

## API Reference

> **Note:** All API details (interfaces, models, enumerations) are available in the source code with XML documentation. Use your IDE's IntelliSense to explore method signatures, parameters, return types, and property definitions.

SmartRAG provides well-defined interfaces, models, and enumerations for all operations. All code elements are located in the following namespaces:

- **`SmartRAG.Interfaces`** - All service interfaces
- **`SmartRAG.Models`** - Data models (RagResponse, Document, DocumentChunk, etc.)
- **`SmartRAG.Enums`** - Enumerations (AIProvider, StorageProvider, DatabaseType, etc.)

### How to View API Details

1. **In your IDE**: Navigate to any class, interface, or enum using "Go to Definition" (F12 in Visual Studio/VS Code)
2. **XML Documentation**: All code elements include XML comments with detailed parameter, return type, and property information
3. **Source Code**: Browse the repository directories:
   - `src/SmartRAG/Interfaces/` - All interfaces
   - `src/SmartRAG/Models/` - All data models
   - `src/SmartRAG/Enums/` - All enumerations
4. **Examples**: See practical usage in the [Examples]({{ site.baseurl }}/en/examples) section

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
