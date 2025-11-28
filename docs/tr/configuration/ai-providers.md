---
layout: default
title: AI Sağlayıcıları
description: SmartRAG AI sağlayıcı yapılandırması - OpenAI, Anthropic, Google Gemini, Azure OpenAI ve özel sağlayıcılar
lang: tr
---

## AI Sağlayıcı Yapılandırması

SmartRAG çeşitli AI sağlayıcılarını destekler:

---

## OpenAI

```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-proj-ANAHTARINIZ",
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

**Modeller:**
- `gpt-4`, `gpt-4-turbo`, `gpt-4o` - Gelişmiş akıl yürütme
- `gpt-3.5-turbo` - Hızlı ve uygun maliyetli
- `text-embedding-ada-002`, `text-embedding-3-small`, `text-embedding-3-large` - Embedding'ler

---

## Anthropic (Claude)

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Önemli: VoyageAI Gerekli</h4>
    <p>
        Anthropic Claude modelleri, embedding'ler için <strong>ayrı bir VoyageAI API anahtarı</strong> gerektirir çünkü Anthropic embedding modelleri sağlamaz.
    </p>
    <ul class="mb-0">
        <li><strong>VoyageAI Anahtarı Alın:</strong> <a href="https://console.voyageai.com/" target="_blank">console.voyageai.com</a></li>
        <li><strong>Dokümantasyon:</strong> <a href="https://docs.anthropic.com/en/docs/build-with-claude/embeddings" target="_blank">Anthropic Embeddings Kılavuzu</a></li>
    </ul>
</div>

```json
{
  "AI": {
    "Anthropic": {
      "ApiKey": "sk-ant-ANTHROPIC_ANAHTARINIZ",
      "Model": "claude-3-5-sonnet-20241022",
      "MaxTokens": 4096,
      "Temperature": 0.3,
      "EmbeddingApiKey": "pa-VOYAGE_ANAHTARINIZ",
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

**Claude Modelleri:**
- `claude-3-5-sonnet-20241022` - En akıllı (önerilen)
- `claude-3-opus-20240229` - En yüksek yetenek
- `claude-3-haiku-20240307` - En hızlı

**VoyageAI Embedding Modelleri:**
- `voyage-large-2` - Yüksek kalite (önerilen)
- `voyage-code-2` - Kod için optimize edilmiş
- `voyage-2` - Genel amaçlı

---

## Google Gemini

```json
{
  "AI": {
    "Gemini": {
      "ApiKey": "GEMINI_ANAHTARINIZ",
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

**Modeller:**
- `gemini-pro` - Metin üretimi
- `gemini-pro-vision` - Çok modlu (metin + görsel)
- `embedding-001` - Metin embedding'leri

---

## Azure OpenAI

```json
{
  "AI": {
    "AzureOpenAI": {
      "ApiKey": "AZURE_ANAHTARINIZ",
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

## Özel Sağlayıcı (Ollama / LM Studio)

<div class="alert alert-success">
    <h4><i class="fas fa-server me-2"></i> Ollama / LM Studio ile %100 On-Premise AI</h4>
    <p>Tam veri gizliliği için AI modellerini tamamen on-premise olarak çalıştırın - şirket içi dağıtımlar, GDPR/KVKK/HIPAA uyumluluğu için mükemmel.</p>
</div>

### Ollama (On-premise Modeller)

```json
{
  "AI": {
    "Custom": {
      "ApiKey": "gerekli-degil",
      "Endpoint": "http://localhost:11434/v1/chat/completions",
      "Model": "llama2",
      "EmbeddingModel": "nomic-embed-text"
    }
  }
}
```

### LM Studio (On-premise Modeller)

```json
{
  "AI": {
    "Custom": {
      "ApiKey": "gerekli-degil",
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

**Desteklenen Özel API'ler:**
- 🦙 Ollama - On-premise modeller
- 🏠 LM Studio - On-premise AI ortamı
- 🔗 OpenRouter - 100+ modele erişim
- ⚡ Groq - Yıldırım hızı çıkarım
- 🌐 Together AI - Açık kaynak modeller
- Herhangi bir OpenAI-uyumlu API

---

## Sağlayıcı Karşılaştırması

| Sağlayıcı | Güçlü Yönler | Zayıf Yönler | En İyi Kullanım |
|-----------|--------------|--------------|-----------------|
| **OpenAI** | Gelişmiş modeller, güvenilir | Pahalı, veri gizliliği endişeleri | Üretim, kritik uygulamalar |
| **Anthropic** | Güvenlik odaklı, kaliteli çıktı | VoyageAI gerekli, sınırlı erişim | Güvenlik kritik uygulamalar |
| **Google Gemini** | Uygun maliyetli, çok modlu | Sınırlı üretim desteği | Prototip, geliştirme |
| **Azure OpenAI** | Kurumsal güvenlik, SLA | Karmaşık kurulum | Kurumsal uygulamalar |
| **Ollama/LM Studio** | %100 on-premise, ücretsiz | Performans sınırları | Veri gizliliği kritik |

---

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-database"></i>
            </div>
            <h3>Depolama Sağlayıcıları</h3>
            <p>Qdrant, Redis, SQLite ve diğer depolama seçenekleri</p>
            <a href="{{ site.baseurl }}/tr/configuration/storage" class="btn btn-outline-primary btn-sm mt-3">
                Depolama Sağlayıcıları
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-server"></i>
            </div>
            <h3>Veritabanı Yapılandırması</h3>
            <p>Çoklu veritabanı bağlantıları ve şema analizi</p>
            <a href="{{ site.baseurl }}/tr/configuration/database" class="btn btn-outline-primary btn-sm mt-3">
                Veritabanı Yapılandırması
            </a>
        </div>
    </div>
</div>
