using SmartRAG.Enums;
using SmartRAG.Models;
using SmartRAG.Entities;

namespace SmartRAG.Providers;

/// <summary>
/// Azure OpenAI provider implementation
/// </summary>
public class AzureOpenAIProvider : BaseAIProvider
{
    public override AIProvider ProviderType => AIProvider.AzureOpenAI;

    public override async Task<string> GenerateTextAsync(string prompt, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config);

        if (!isValid)
            return errorMessage;

        using var client = CreateHttpClient(config.ApiKey);

        var payload = new
        {
            messages = new[]
            {
                new { role = "system", content = "You are a helpful AI assistant that answers questions based on provided context. Always base your answers on the context information provided. If the context doesn't contain enough information, say so clearly." },
                new { role = "user", content = prompt }
            },
            max_tokens = config.MaxTokens,
            temperature = config.Temperature,
            stream = false
        };

        var url = $"{config.Endpoint!.TrimEnd('/')}/openai/deployments/{config.Model}/chat/completions?api-version={config.ApiVersion}";

        var (success, response, error) = await MakeHttpRequestAsync(client, url, payload, "Azure OpenAI");

        if (!success)
            return error;

        return ParseTextResponse(response);
    }

    public override async Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config)
    {
        var (isValid, _) = ValidateConfig(config, requireApiKey: true, requireEndpoint: true, requireModel: false);

        if (!isValid)
            return [];

        if (string.IsNullOrEmpty(config.EmbeddingModel))
            return [];

        using var client = CreateHttpClient(config.ApiKey);

        var payload = new
        {
            input = text
        };

        var url = $"{config.Endpoint!.TrimEnd('/')}/openai/deployments/{config.EmbeddingModel}/embeddings?api-version={config.ApiVersion}";

        var (success, response, error) = await MakeHttpRequestAsync(client, url, payload, "Azure OpenAI");

        if (!success)
            return [];

        return ParseEmbeddingResponse(response);
    }

    public override async Task<List<List<float>>?> GenerateEmbeddingsBatchAsync(List<string> texts, AIProviderConfig config)
    {
        var (isValid, _) = ValidateConfig(config, requireApiKey: true, requireEndpoint: true, requireModel: false);

        if (!isValid || texts == null || texts.Count == 0)
            return null;

        if (string.IsNullOrEmpty(config.EmbeddingModel))
            return null;

        using var client = CreateHttpClient(config.ApiKey);

        var payload = new
        {
            input = texts.ToArray()
        };

        var url = $"{config.Endpoint!.TrimEnd('/')}/openai/deployments/{config.EmbeddingModel}/embeddings?api-version={config.ApiVersion}";

        var (success, response, error) = await MakeHttpRequestAsync(client, url, payload, "Azure OpenAI");

        if (!success)
            return null;

        // Parse batch embedding response
        using var doc = System.Text.Json.JsonDocument.Parse(response);
        if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            var results = new List<List<float>>();
            foreach (var item in data.EnumerateArray())
            {
                if (item.TryGetProperty("embedding", out var embedding) && embedding.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var embeddingList = embedding.EnumerateArray()
                        .Select(x => x.GetSingle())
                        .ToList();
                    results.Add(embeddingList);
                }
                else
                {
                    results.Add(new List<float>());
                }
            }
            return results.Count == texts.Count ? results : null;
        }

        return null;
    }

    public override async Task ClearEmbeddingsAsync(List<DocumentChunk> chunks)
    {
        // Azure OpenAI'da embedding'ler API'de cache'lenmez, sadece local memory'de tutulur
        // Bu provider için özel cache temizliği gerekmez, base implementation yeterli
        // Ancak gelecekte Azure OpenAI embedding cache'i eklenirse burada implement edilebilir
        
        // Şu an için no-op, çünkü Azure OpenAI embedding cache'i yok
        await Task.CompletedTask;
    }

    public override async Task ClearAllEmbeddingsAsync()
    {
        // Azure OpenAI'da tüm embedding'leri temizle
        // Şu an için no-op, çünkü Azure OpenAI embedding cache'i yok
        
        await Task.CompletedTask;
    }

    public override async Task<bool> RegenerateAllEmbeddingsAsync(List<Document> documents)
    {
        // Azure OpenAI için embedding regeneration
        // Her document'ın chunk'ları için yeni embedding'ler oluştur
        
        var totalChunks = documents.Sum(d => d.Chunks.Count);
        var processedChunks = 0;
        var successCount = 0;
        
        foreach (var document in documents)
        {
            foreach (var chunk in document.Chunks)
            {
                try
                {
                    // Skip if embedding already exists and is valid
                    if (chunk.Embedding != null && chunk.Embedding.Count > 0)
                    {
                        processedChunks++;
                        continue;
                    }
                    
                    // Generate new embedding
                    var newEmbedding = await GenerateEmbeddingAsync(chunk.Content, new AIProviderConfig
                    {
                        ApiKey = "", // Bu config DocumentService'den gelmeli
                        Endpoint = "",
                        EmbeddingModel = ""
                    });
                    
                    if (newEmbedding != null && newEmbedding.Count > 0)
                    {
                        chunk.Embedding = newEmbedding;
                        successCount++;
                    }
                    
                    processedChunks++;
                }
                catch (Exception)
                {
                    processedChunks++;
                }
            }
        }
        
        return successCount > 0;
    }
}