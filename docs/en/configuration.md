---
layout: default
title: Configuration
description: Complete configuration guide for SmartRAG - AI providers, storage, databases, and advanced options
lang: en
---

## Configuration Categories

SmartRAG configuration is organized into the following categories:

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-cog"></i>
            </div>
            <h3>Basic Configuration</h3>
            <p>Configuration methods, core options, chunking and retry settings</p>
            <a href="{{ site.baseurl }}/en/configuration/basic" class="btn btn-outline-primary btn-sm mt-3">
                Basic Configuration
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-brain"></i>
            </div>
            <h3>AI Providers</h3>
            <p>OpenAI, Anthropic, Google Gemini, Azure OpenAI and custom providers</p>
            <a href="{{ site.baseurl }}/en/configuration/ai-providers" class="btn btn-outline-primary btn-sm mt-3">
                AI Providers
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-database"></i>
            </div>
            <h3>Storage Providers</h3>
            <p>Qdrant, Redis, SQLite, FileSystem and InMemory storage options</p>
            <a href="{{ site.baseurl }}/en/configuration/storage" class="btn btn-outline-primary btn-sm mt-3">
                Storage Providers
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-server"></i>
            </div>
            <h3>Database Configuration</h3>
            <p>Multi-database connections, schema analysis and security settings</p>
            <a href="{{ site.baseurl }}/en/configuration/database" class="btn btn-outline-primary btn-sm mt-3">
                Database Configuration
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-microphone"></i>
            </div>
            <h3>Audio & OCR</h3>
            <p>Whisper.net and Tesseract OCR configuration</p>
            <a href="{{ site.baseurl }}/en/configuration/audio-ocr" class="btn btn-outline-primary btn-sm mt-3">
                Audio & OCR
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-cogs"></i>
            </div>
            <h3>Advanced Configuration</h3>
            <p>Fallback providers, best practices and next steps</p>
            <a href="{{ site.baseurl }}/en/configuration/advanced" class="btn btn-outline-primary btn-sm mt-3">
                Advanced Configuration
            </a>
        </div>
    </div>
</div>

## Quick Start

### Simple Configuration

```csharp
builder.Services.UseSmartRag(configuration,
    storageProvider: StorageProvider.InMemory,
    aiProvider: AIProvider.Gemini
);
```

### Advanced Configuration

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
    options.MaxRetryAttempts = 3;
});
```

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-rocket"></i>
            </div>
            <h3>Getting Started</h3>
            <p>Integrate SmartRAG into your project</p>
            <a href="{{ site.baseurl }}/en/getting-started" class="btn btn-outline-primary btn-sm mt-3">
                Getting Started Guide
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-code"></i>
            </div>
            <h3>Examples</h3>
            <p>See practical examples and real-world usage scenarios</p>
            <a href="{{ site.baseurl }}/en/examples" class="btn btn-outline-primary btn-sm mt-3">
                View Examples
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-book"></i>
            </div>
            <h3>API Reference</h3>
            <p>Detailed API documentation and method references</p>
            <a href="{{ site.baseurl }}/en/api-reference" class="btn btn-outline-primary btn-sm mt-3">
                API Reference
            </a>
        </div>
    </div>
</div>