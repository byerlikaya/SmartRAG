# 🔧 Configuration Guide

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
cp src/SmartRAG.API/appsettings.json src/SmartRAG.API/appsettings.Development.json
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
      "Model": "claude-3.5-sonnet"
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
cd src/SmartRAG.API
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
  'git rm --cached --ignore-unmatch src/SmartRAG.API/appsettings.Development.json' \
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

#### Anthropic
```json
{
  "AI": {
    "Anthropic": {
      "ApiKey": "sk-ant-...",
      "Endpoint": "https://api.anthropic.com",
      "Model": "claude-3.5-sonnet",
      "MaxTokens": 4096,
      "Temperature": 0.3
    }
  }
}
```

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

---

**🔒 Remember: Security is everyone's responsibility. Keep your keys safe!**
