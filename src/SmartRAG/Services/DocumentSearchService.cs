using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using SmartRAG.Services.Logging;

namespace SmartRAG.Services;

public class DocumentSearchService(
    IDocumentRepository documentRepository,
    IAIService aiService,
    IAIProviderFactory aiProviderFactory,
    IConfiguration configuration,
    SmartRagOptions options,
    ISemanticSearchProvider semanticSearchProvider,
    ILogger<DocumentSearchService> logger) : IDocumentSearchService
{
    public async Task<RagResponse> GenerateRagAnswerAsync(string query, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be empty", nameof(query));

        // Use semantic search provider directly
        var chunks = await semanticSearchProvider.SearchDocumentsAsync(query, maxResults);

        // Check if we have relevant content for document-based answers
        var hasRelevantContent = chunks.Count > 0 && chunks.Any(c => c.RelevanceScore > 0.3);

        if (hasRelevantContent)
        {
            // Document search query - generate answer from found chunks
            return await GenerateAnswerFromChunksAsync(query, chunks);
        }
        else
        {
            // General conversation - no relevant documents found
            ServiceLogMessages.LogGeneralConversationQuery(logger, null);
            var chatResponse = await HandleGeneralConversationAsync(query);
            return new RagResponse
            {
                Query = query,
                Answer = chatResponse,
                Sources = new List<SearchSource>(),
                SearchedAt = DateTime.UtcNow,
                Configuration = GetRagConfiguration()
            };
        }
    }

    #region Private Helper Methods

    private async Task<RagResponse> GenerateAnswerFromChunksAsync(string query, List<DocumentChunk> chunks)
    {
        var context = string.Join("\n\n", chunks.Select(c => c.Content));

        // Enhanced prompt for better AI understanding
        var enhancedPrompt = $@"You are a helpful document analysis assistant. Answer questions based on the provided document context.

IMPORTANT: 
- Carefully analyze the context
- Look for specific information that answers the question
- If you find the information, provide a clear answer
- If you cannot find it, say 'I cannot find this specific information in the provided documents'
- Be precise and use exact information from documents

Question: {query}

Context: {context}

Answer:";

        var answer = await aiService.GenerateResponseAsync(enhancedPrompt, new List<string> { context });

        return new RagResponse
        {
            Query = query,
            Answer = answer,
            Sources = chunks.Select(c => new SearchSource
            {
                DocumentId = c.DocumentId,
                FileName = "Document",
                RelevantContent = c.Content,
                RelevanceScore = c.RelevanceScore ?? 0.0
            }).ToList(),
            SearchedAt = DateTime.UtcNow,
            Configuration = GetRagConfiguration()
        };
    }

    private RagConfiguration GetRagConfiguration()
    {
        // Get the actual configured model from the selected AI provider
        var providerKey = options.AIProvider.ToString();
        var providerConfig = configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();
        var model = providerConfig?.Model ?? "default-model";

        return new RagConfiguration
        {
            AIProvider = options.AIProvider.ToString(),
            StorageProvider = options.StorageProvider.ToString(),
            Model = model
        };
    }

    /// <summary>
    /// Handle general conversation queries
    /// </summary>
    private async Task<string> HandleGeneralConversationAsync(string query)
    {
        try
        {
            // Use the configured AI provider from options
            var aiProvider = aiProviderFactory.CreateProvider(options.AIProvider);
            var providerKey = options.AIProvider.ToString();
            var providerConfig = configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

            if (providerConfig == null || string.IsNullOrEmpty(providerConfig.ApiKey))
            {
                return "Sorry, I cannot chat right now. Please try again later.";
            }

            var prompt = $@"You are a helpful AI assistant. Answer the user's question naturally and friendly.

User: {query}

Answer:";

            return await aiProvider.GenerateTextAsync(prompt, providerConfig);
        }
        catch (Exception)
        {
            // Log error using structured logging
            return "Sorry, I cannot chat right now. Please try again later.";
        }
    }

    #endregion
}
