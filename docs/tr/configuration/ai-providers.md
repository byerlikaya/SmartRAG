---
layout: default
title: AI Sağlayıcıları
description: SmartRAG AI sağlayıcı yapılandırması - OpenAI, Anthropic, Google Gemini, Azure OpenAI ve özel sağlayıcılar
lang: tr
---

## AI Sağlayıcı Yapılandırması

SmartRAG çeşitli AI sağlayıcılarını destekler:

## OpenAI

```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-proj-ANAHTARINIZ",
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

**Modeller:**
- `gpt-5.1` - En gelişmiş akıl yürütme modeli (önerilen)
- `gpt-5` - Gelişmiş akıl yürütme yetenekleri
- `gpt-5-mini` - Uygun maliyetli GPT-5 varyantı
- `gpt-4o` - Önceki nesil gelişmiş model
- `gpt-4o-mini` - Uygun maliyetli önceki nesil
- `text-embedding-3-small`, `text-embedding-3-large` - Embedding'ler (önerilen)
- `text-embedding-ada-002` - Eski embedding'ler

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
      "Model": "claude-sonnet-4-5",
      "MaxTokens": 4096,
      "Temperature": 0.3,
      "EmbeddingApiKey": "pa-VOYAGE_ANAHTARINIZ",
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

**Claude Modelleri:**
- `claude-sonnet-4-5` - En yeni ve en akıllı (önerilen)
- `claude-3.5-sonnet` - Önceki nesil
- `claude-3-opus-20240229` - En yüksek yetenek
- `claude-3-haiku-20240307` - En hızlı
- `claude-3-opus-20240229` - En yüksek yetenek
- `claude-3-haiku-20240307` - En hızlı

**VoyageAI Embedding Modelleri:**
- `voyage-3.5` - Yüksek kalite (önerilen)
- `voyage-code-2` - Kod için optimize edilmiş
- `voyage-2` - Genel amaçlı

## Google Gemini

```json
{
  "AI": {
    "Gemini": {
      "ApiKey": "GEMINI_ANAHTARINIZ",
      "Model": "gemini-3-pro-preview",
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
- `gemini-3-pro-preview` - En gelişmiş çok modlu model (önerilen)
- `gemini-2.5-pro` - Gelişmiş akıl yürütme yetenekleri
- `gemini-2.5-flash` - Hızlı ve uygun maliyetli
- `gemini-2.0-flash` - Önceki nesil iş modeli
- `embedding-001` - Metin embedding'leri

## Azure OpenAI

```json
{
  "AI": {
    "AzureOpenAI": {
      "ApiKey": "AZURE_ANAHTARINIZ",
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

## Sağlayıcı Karşılaştırması

<p>Kullanım durumunuz için en iyi seçeneği seçmek üzere AI sağlayıcılarını karşılaştırın:</p>

<div class="table-responsive">
<table class="table">
<thead>
<tr>
<th>Sağlayıcı</th>
<th>Güçlü Yönler</th>
<th>Zayıf Yönler</th>
<th>En İyi Kullanım</th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>OpenAI</strong></td>
<td>Gelişmiş modeller, güvenilir</td>
<td>Pahalı, veri gizliliği endişeleri</td>
<td>Üretim, kritik uygulamalar</td>
</tr>
<tr>
<td><strong>Anthropic</strong></td>
<td>Güvenlik odaklı, kaliteli çıktı</td>
<td>VoyageAI gerekli, sınırlı erişim</td>
<td>Güvenlik kritik uygulamalar</td>
</tr>
<tr>
<td><strong>Google Gemini</strong></td>
<td>Uygun maliyetli, çok modlu</td>
<td>Sınırlı üretim desteği</td>
<td>Prototip, geliştirme</td>
</tr>
<tr>
<td><strong>Azure OpenAI</strong></td>
<td>Kurumsal güvenlik, SLA</td>
<td>Karmaşık kurulum</td>
<td>Kurumsal uygulamalar</td>
</tr>
<tr>
<td><strong>Ollama/LM Studio</strong></td>
<td>%100 on-premise, ücretsiz</td>
<td>Performans sınırları</td>
<td>Veri gizliliği kritik</td>
</tr>
</tbody>
</table>
</div>

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
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
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
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
