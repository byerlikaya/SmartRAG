---
layout: default
title: AI Providers
description: SmartRAG AI provider configuration - OpenAI, Anthropic, Google Gemini, Azure OpenAI and custom providers
lang: en
---

## AI Provider Configuration

<p>SmartRAG supports various AI providers:</p>

## OpenAI

<p>OpenAI provides advanced language models and embeddings for production-ready applications:</p>

```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-proj-YOUR_KEY",
      "Endpoint": "https://api.openai.com/v1",
      "Model": "gpt-5.1",
      "EmbeddingModel": "text-embedding-3-small",
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
- `gpt-5.1` - Latest advanced reasoning model (recommended)
- `gpt-5` - Advanced reasoning capabilities
- `gpt-4o` - Previous generation advanced model
- `gpt-4o-mini` - Cost-effective and fast
- `text-embedding-3-small`, `text-embedding-3-large` - Embeddings (recommended)
- `text-embedding-ada-002` - Legacy embeddings

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
      "Model": "claude-sonnet-4-5",
      "MaxTokens": 4096,
      "Temperature": 0.3,
      "EmbeddingApiKey": "pa-VOYAGE_KEY",
      "EmbeddingModel": "voyage-3.5"
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
- `claude-sonnet-4-5` - Latest and most intelligent (recommended)
- `claude-3.5-sonnet` - Previous generation
- `claude-3-opus-20240229` - Highest capability
- `claude-3-haiku-20240307` - Fastest

**VoyageAI Embedding Models:**
- `voyage-3.5` - High quality (recommended)
- `voyage-code-2` - Optimized for code
- `voyage-2` - General purpose

## Google Gemini

<p>Google Gemini offers cost-effective AI models with multimodal capabilities:</p>

```json
{
  "AI": {
    "Gemini": {
      "ApiKey": "GEMINI_KEY",
      "Model": "gemini-2.5-pro",
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
- `gemini-2.5-pro` - Advanced reasoning capabilities (recommended)
- `gemini-2.5-flash` - Fast and cost-effective
- `gemini-2.0-flash` - Previous generation workhorse
- `gemini-1.5-pro` - Legacy advanced model
- `embedding-001` - Text embeddings

## Azure OpenAI

<p>Azure OpenAI provides enterprise-grade AI services with enhanced security and compliance:</p>

```json
{
  "AI": {
    "AzureOpenAI": {
      "ApiKey": "AZURE_KEY",
      "Endpoint": "https://your-resource.openai.azure.com/",
      "Model": "gpt-5.1",
      "EmbeddingModel": "text-embedding-3-small",
      "DeploymentName": "gpt-5.1-deployment",
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

## Provider Comparison

<p>Compare AI providers to choose the best option for your use case:</p>

<div class="table-responsive">
<table class="table">
<thead>
<tr>
<th>Provider</th>
<th>Strengths</th>
<th>Weaknesses</th>
<th>Best Use Case</th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>OpenAI</strong></td>
<td>Advanced models, reliable</td>
<td>Expensive, data privacy concerns</td>
<td>Production, critical applications</td>
</tr>
<tr>
<td><strong>Anthropic</strong></td>
<td>Security-focused, quality output</td>
<td>VoyageAI required, limited access</td>
<td>Security-critical applications</td>
</tr>
<tr>
<td><strong>Google Gemini</strong></td>
<td>Cost-effective, multimodal</td>
<td>Limited production support</td>
<td>Prototyping, development</td>
</tr>
<tr>
<td><strong>Azure OpenAI</strong></td>
<td>Enterprise security, SLA</td>
<td>Complex setup</td>
<td>Enterprise applications</td>
</tr>
<tr>
<td><strong>Ollama/LM Studio</strong></td>
<td>100% on-premise, free</td>
<td>Performance limitations</td>
<td>Data privacy critical</td>
</tr>
</tbody>
</table>
</div>

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
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
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
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
