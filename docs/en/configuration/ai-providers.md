---
layout: default
title: AI Providers
description: SmartRAG AI provider configuration - OpenAI, Anthropic, Google Gemini, Azure OpenAI and custom providers
lang: en
---

## AI Provider Configuration

SmartRAG supports various AI providers:

---

## OpenAI

```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-proj-YOUR_KEY",
      "Endpoint": "https://api.openai.com/v1",
      "Model": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002",
      "MaxTokens": 4096,
      "Temperature": 0.7
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
});
```

**Models:**
- `gpt-4`, `gpt-4-turbo`, `gpt-4o` - Advanced reasoning
- `gpt-3.5-turbo` - Fast and cost-effective
- `text-embedding-ada-002`, `text-embedding-3-small`, `text-embedding-3-large` - Embeddings

---

## Anthropic (Claude)

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Important: VoyageAI Required</h4>
    <p>
        Anthropic Claude models require a <strong>separate VoyageAI API key</strong> for embeddings since Anthropic doesn't provide embedding models.
    </p>
    <ul class="mb-0">
        <li><strong>Get VoyageAI Key:</strong> <a href="https://console.voyageai.com/" target="_blank">console.voyageai.com</a></li>
        <li><strong>Documentation:</strong> <a href="https://docs.anthropic.com/en/docs/build-with-claude/embeddings" target="_blank">Anthropic Embeddings Guide</a></li>
    </ul>
</div>

```json
{
  "AI": {
    "Anthropic": {
      "ApiKey": "sk-ant-ANTHROPIC_KEY",
      "Model": "claude-3-5-sonnet-20241022",
      "MaxTokens": 4096,
      "Temperature": 0.3,
      "EmbeddingApiKey": "pa-VOYAGE_KEY",
      "EmbeddingModel": "voyage-large-2"
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
});
```

**Claude Models:**
- `claude-3-5-sonnet-20241022` - Most intelligent (recommended)
- `claude-3-opus-20240229` - Highest capability
- `claude-3-haiku-20240307` - Fastest

**VoyageAI Embedding Models:**
- `voyage-large-2` - High quality (recommended)
- `voyage-code-2` - Optimized for code
- `voyage-2` - General purpose

---

## Google Gemini

```json
{
  "AI": {
    "Gemini": {
      "ApiKey": "GEMINI_KEY",
      "Model": "gemini-pro",
      "EmbeddingModel": "embedding-001",
      "MaxTokens": 4096,
      "Temperature": 0.7
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Gemini;
});
```

**Models:**
- `gemini-pro` - Text generation
- `gemini-pro-vision` - Multimodal (text + vision)
- `embedding-001` - Text embeddings

---

## Azure OpenAI

```json
{
  "AI": {
    "AzureOpenAI": {
      "ApiKey": "AZURE_KEY",
      "Endpoint": "https://your-resource.openai.azure.com/",
      "Model": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002",
      "DeploymentName": "gpt-4-deployment",
      "MaxTokens": 4096,
      "Temperature": 0.7
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.AzureOpenAI;
});
```

---

## Custom Provider (Ollama / LM Studio)

<div class="alert alert-success">
    <h4><i class="fas fa-server me-2"></i> Ollama / LM Studio with 100% On-Premise AI</h4>
    <p>Run AI models completely on-premise for complete data privacy - perfect for enterprise deployments, GDPR/HIPAA compliance.</p>
</div>

### Ollama (On-premise Models)

```json
{
  "AI": {
    "Custom": {
      "ApiKey": "not-required",
      "Endpoint": "http://localhost:11434/v1/chat/completions",
      "Model": "llama2",
      "EmbeddingModel": "nomic-embed-text"
    }
  }
}
```

### LM Studio (On-premise Models)

```json
{
  "AI": {
    "Custom": {
      "ApiKey": "not-required",
      "Endpoint": "http://localhost:1234/v1/chat/completions",
      "Model": "local-model",
      "EmbeddingModel": "local-embedding"
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Custom;
});
```

**Supported Custom APIs:**
- 🦙 Ollama - On-premise models
- 🏠 LM Studio - On-premise AI environment
- 🔗 OpenRouter - Access to 100+ models
- ⚡ Groq - Lightning-fast inference
- 🌐 Together AI - Open source models
- Any OpenAI-compatible API

---

## Provider Comparison

| Provider | Strengths | Weaknesses | Best Use Case |
|----------|-----------|------------|---------------|
| **OpenAI** | Advanced models, reliable | Expensive, data privacy concerns | Production, critical applications |
| **Anthropic** | Security-focused, quality output | VoyageAI required, limited access | Security-critical applications |
| **Google Gemini** | Cost-effective, multimodal | Limited production support | Prototyping, development |
| **Azure OpenAI** | Enterprise security, SLA | Complex setup | Enterprise applications |
| **Ollama/LM Studio** | 100% on-premise, free | Performance limitations | Data privacy critical |

---

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-database"></i>
            </div>
            <h3>Storage Providers</h3>
            <p>Qdrant, Redis, SQLite and other storage options</p>
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
            <p>Multi-database connections and schema analysis</p>
            <a href="{{ site.baseurl }}/en/configuration/database" class="btn btn-outline-primary btn-sm mt-3">
                Database Configuration
            </a>
        </div>
    </div>
</div>
