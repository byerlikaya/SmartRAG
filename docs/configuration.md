---
layout: default
title: Configuration
description: Complete configuration guide for SmartRAG - AI providers, storage backends, and advanced settings
nav_order: 3
---

# Configuration Guide

This guide explains how to configure SmartRAG securely for different environments.

## 🔒 Security First

**⚠️ NEVER commit real API keys to Git!**

SmartRAG uses a secure configuration system that keeps your secrets safe.

## 📁 Configuration Files

### **appsettings.json** (Template - Safe for Git)
```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "your-openai-api-key",
      "Model": "gpt-4"
    }
  }
}
```
✅ **Safe to commit** - Contains only placeholder values.

### **appsettings.Development.json** (Your Real Keys - NEVER COMMIT)
```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-proj-REAL_KEY_HERE",
      "Model": "gpt-4"
    }
  }
}
```
❌ **NEVER commit** - Contains your real API keys.

## 🚀 Quick Setup

### **1. Copy the template**
```bash
# Create your development config
cp examples/WebAPI/appsettings.json examples/WebAPI/appsettings.Development.json
```

### **2. Add your real API keys**
Edit `appsettings.Development.json` with your actual keys:

```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-proj-YOUR_REAL_OPENAI_KEY",
      "Endpoint": "https://api.openai.com/v1",
      "Model": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002"
    },
    "Anthropic": {
      "ApiKey": "sk-ant-YOUR_REAL_ANTHROPIC_KEY",
      "Model": "claude-3.5-sonnet",
      "EmbeddingApiKey": "voyage-YOUR_REAL_VOYAGEAI_KEY",
      "EmbeddingModel": "voyage-large-2"
    },
    "Gemini": {
      "ApiKey": "YOUR_REAL_GEMINI_KEY",
      "Model": "gemini-1.5-flash"
    }
  },
  "Storage": {
    "Qdrant": {
      "Host": "your-cluster.qdrant.io",
      "ApiKey": "YOUR_REAL_QDRANT_KEY"
    },
    "Redis": {
      "ConnectionString": "your-redis-host:6379",
      "Password": "YOUR_REAL_REDIS_PASSWORD"
    }
  }
}
```

### **3. Run the application**
```bash
cd examples/WebAPI
dotnet run
```

The app will automatically load:
1. `appsettings.json` (base template)
2. `appsettings.Development.json` (your real keys)

## 🌍 Environment-Specific Configuration

### **Development**
- File: `appsettings.Development.json`
- Environment: `ASPNETCORE_ENVIRONMENT=Development`

### **Production**
- File: `appsettings.Production.json`
- Environment: `ASPNETCORE_ENVIRONMENT=Production`

### **Local Testing**
- File: `appsettings.Local.json`
- Custom environment for testing

## 🔑 Environment Variables (Alternative)

You can also use environment variables instead of config files:

```bash
# OpenAI
export AI__OpenAI__ApiKey="sk-proj-YOUR_KEY"
export AI__OpenAI__Model="gpt-4"

# Anthropic  
export AI__Anthropic__ApiKey="sk-ant-YOUR_KEY"
export AI__Anthropic__EmbeddingApiKey="voyage-YOUR_KEY"

# Qdrant
export Storage__Qdrant__Host="your-cluster.qdrant.io"
export Storage__Qdrant__ApiKey="YOUR_KEY"
```

## 🛡️ Security Best Practices

### **✅ DO:**
- Use `appsettings.Development.json` for local development
- Use environment variables in production
- Keep the template `appsettings.json` with placeholder values
- Add sensitive config files to `.gitignore`

### **❌ DON'T:**
- Commit real API keys to Git
- Share config files with real keys
- Use production keys in development
- Hard-code API keys in source code

## 🔄 CI/CD Configuration

For GitHub Actions, use repository secrets:

```yaml
# .github/workflows/ci.yml
env:
  AI__OpenAI__ApiKey: ${{ secrets.OPENAI_API_KEY }}
  AI__Anthropic__ApiKey: ${{ secrets.ANTHROPIC_API_KEY }}
  AI__Anthropic__EmbeddingApiKey: ${{ secrets.VOYAGEAI_API_KEY }}
  Storage__Qdrant__ApiKey: ${{ secrets.QDRANT_API_KEY }}
```

## 🆘 Emergency: If You Accidentally Committed Keys

1. **Immediately revoke the API keys** in your provider's dashboard
2. **Generate new API keys**
3. **Update your local configuration**
4. **Remove the sensitive data from Git history**

```bash
# Remove file from Git history (if needed)
git filter-branch --force --index-filter \
  'git rm --cached --ignore-unmatch examples/WebAPI/appsettings.Development.json' \
  --prune-empty --tag-name-filter cat -- --all
```

## 📖 Configuration Reference

### **AI Providers**

#### OpenAI
```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-proj-...",
      "Endpoint": "https://api.openai.com/v1",
      "Model": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002",
      "MaxTokens": 4096,
      "Temperature": 0.7
    }
  }
}
```

#### Anthropic (with VoyageAI embeddings)
```json
{
  "AI": {
    "Anthropic": {
      "ApiKey": "sk-ant-...",
      "Endpoint": "https://api.anthropic.com",
      "Model": "claude-3.5-sonnet",
      "MaxTokens": 4096,
      "Temperature": 0.3,
      "EmbeddingApiKey": "voyage-...",
      "EmbeddingModel": "voyage-large-2"
    }
  }
}
```

**⚠️ Important for Anthropic users:**
- **Claude models don't provide embeddings**
- **VoyageAI API key is required** for document embeddings
- **Get VoyageAI key:** [console.voyageai.com](https://console.voyageai.com/)
- **Recommended models:** `voyage-large-2`, `voyage-code-2`, `voyage-01`

#### Google Gemini
```json
{
  "AI": {
    "Gemini": {
      "ApiKey": "AIzaSy...",
      "Endpoint": "https://generativelanguage.googleapis.com/v1beta",
      "Model": "gemini-1.5-flash",
      "EmbeddingModel": "embedding-001",
      "MaxTokens": 4096,
      "Temperature": 0.3
    }
  }
}
```

#### Azure OpenAI
```json
{
  "AI": {
    "AzureOpenAI": {
      "ApiKey": "sk-...",
      "Endpoint": "https://your-resource.openai.azure.com/",
      "Model": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002",
      "MaxTokens": 4096,
      "Temperature": 0.7
    }
  }
}
```

### **Storage Providers**

#### Qdrant
```json
{
  "Storage": {
    "Qdrant": {
      "Host": "your-cluster.qdrant.io",
      "UseHttps": true,
      "ApiKey": "your-api-key",
      "CollectionName": "smartrag_documents",
      "VectorSize": 1536,
      "DistanceMetric": "Cosine"
    }
  }
}
```

#### Redis
```json
{
  "Storage": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Password": "your-password",
      "Database": 0,
      "KeyPrefix": "smartrag:",
      "EnableSsl": false
    }
  }
}
```

### **SmartRAG Core Options**

#### Enhanced Chunking & Search
```json
{
  "SmartRAG": {
    "MaxChunkSize": 1000,
    "MinChunkSize": 100,
    "ChunkOverlap": 200,
    "SemanticSearchThreshold": 0.3,
    "EnableWordBoundaryValidation": true,
    "HybridScoringWeights": {
      "SemanticWeight": 0.8,
      "KeywordWeight": 0.2
    }
  }
}
```

#### Semantic Search Service
```json
{
  "SemanticSearch": {
    "EnableEnhancedScoring": true,
    "ContextualKeywordDetection": true,
    "SemanticCoherenceAnalysis": true,
    "MaxTokenChunkSize": 100
  }
}
```

## 🎯 Advanced Configuration Examples

### **Complete Production Configuration**
```json
{
  "AI": {
    "Anthropic": {
      "ApiKey": "sk-ant-...",
      "Model": "claude-3.5-sonnet",
      "MaxTokens": 4096,
      "Temperature": 0.3,
      "EmbeddingApiKey": "voyage-...",
      "EmbeddingModel": "voyage-large-2"
    }
  },
  "Storage": {
    "Qdrant": {
      "Host": "your-cluster.qdrant.io",
      "UseHttps": true,
      "ApiKey": "your-api-key",
      "CollectionName": "smartrag_production",
      "VectorSize": 1536,
      "DistanceMetric": "Cosine"
    }
  },
  "SmartRAG": {
    "MaxChunkSize": 1200,
    "MinChunkSize": 150,
    "ChunkOverlap": 250,
    "SemanticSearchThreshold": 0.25,
    "EnableWordBoundaryValidation": true
  }
}
```

### **Development with Fallback Providers**
```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-...",
      "Model": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002"
    },
    "Anthropic": {
      "ApiKey": "sk-ant-...",
      "Model": "claude-3.5-sonnet",
      "EmbeddingApiKey": "voyage-...",
      "EmbeddingModel": "voyage-large-2"
    }
  },
  "Storage": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Database": 0,
      "KeyPrefix": "smartrag_dev:"
    }
  },
  "SmartRAG": {
    "EnableFallbackProviders": true,
    "FallbackProviders": ["Anthropic", "OpenAI"]
  }
}
```

## 🔧 Configuration Binding Priority

SmartRAG uses a **user-first configuration system**:

### **Priority Order (Highest to Lowest)**
1. **User Configuration** (Program.cs) - **Absolute Priority**
2. **Environment Variables** - Override defaults
3. **appsettings.{Environment}.json** - Environment-specific
4. **appsettings.json** - Base template (safe for Git)

### **Example: User Configuration Takes Priority**
```csharp
// Program.cs - This will ALWAYS be used
services.UseSmartRag(configuration,
    storageProvider: StorageProvider.Qdrant,  // ✅ Takes priority
    aiProvider: AIProvider.AzureOpenAI        // ✅ Takes priority
);

// appsettings.json - This will NOT override user settings
{
  "SmartRAG": {
    "AIProvider": "OpenAI",      // ❌ Ignored
    "StorageProvider": "Redis"    // ❌ Ignored
  }
}
```

## 🚀 Performance Tuning

### **Chunking Optimization**
```json
{
  "SmartRAG": {
    "MaxChunkSize": 1000,        // Larger chunks = more context
    "MinChunkSize": 100,         // Smaller chunks = faster search
    "ChunkOverlap": 200,         // Higher overlap = better context
    "SemanticSearchThreshold": 0.3  // Lower threshold = more results
  }
}
```

### **Search Performance**
```json
{
  "SemanticSearch": {
    "MaxTokenChunkSize": 100,    // Smaller token chunks = faster processing
    "EnableCaching": true,       // Cache semantic scores
    "CacheExpirationMinutes": 60
  }
}
```

---

**🔒 Remember: Security is everyone's responsibility. Keep your keys safe!**
