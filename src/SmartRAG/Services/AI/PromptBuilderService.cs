#nullable enable

using SmartRAG.Interfaces.Support;
using SmartRAG.Interfaces.AI;

namespace SmartRAG.Services.AI
{
    /// <summary>
    /// Service for building AI prompts
    /// </summary>
    public class PromptBuilderService : IPromptBuilderService
    {
        private readonly IConversationManagerService _conversationManager;

        /// <summary>
        /// Initializes a new instance of the PromptBuilderService
        /// </summary>
        /// <param name="conversationManager">Service for managing conversation sessions and history</param>
        public PromptBuilderService(IConversationManagerService conversationManager)
        {
            _conversationManager = conversationManager;
        }

        /// <summary>
        /// Builds a prompt for document-based RAG answer generation
        /// </summary>
        public string BuildDocumentRagPrompt(string query, string context, string? conversationHistory = null)
        {
            var historyContext = !string.IsNullOrEmpty(conversationHistory)
                ? $"\n\nRecent conversation context:\n{_conversationManager.TruncateConversationHistory(conversationHistory, maxTurns: 2)}\n"
                : "";

            return $@"You are a helpful document analysis assistant. Answer questions based ONLY on the provided document context.

CRITICAL RULES: 
- Base your answer ONLY on the document context provided below
- Do NOT use information from previous conversations unless it's in the current document context
- If you find the information in documents, provide a clear and concise answer
- If you cannot find it in the documents, simply say 'I cannot find this information in the provided documents'
- Be precise and use exact information from documents
- Keep responses focused on the current question

{historyContext}Current question: {query}

Document context: {context}

Answer:";
        }

        /// <summary>
        /// Builds a prompt for merging hybrid results (database + documents)
        /// </summary>
        public string BuildHybridMergePrompt(string query, string? databaseContext, string? documentContext, string? conversationHistory = null)
        {
            var combinedContext = new System.Collections.Generic.List<string>();
            
            if (!string.IsNullOrEmpty(databaseContext))
            {
                combinedContext.Add($"=== DATABASE INFORMATION ===\n{databaseContext}");
            }
            
            if (!string.IsNullOrEmpty(documentContext))
            {
                combinedContext.Add($"=== DOCUMENT INFORMATION ===\n{documentContext}");
            }

            var historyContext = !string.IsNullOrEmpty(conversationHistory)
                ? $"\n\nRecent context:\n{_conversationManager.TruncateConversationHistory(conversationHistory, maxTurns: 2)}\n"
                : "";

            return $@"Answer the user's question using the provided information.

CRITICAL RULES:
- Provide DIRECT, CONCISE answer to the question
- Use information from the sources below (database OR documents)
- Do NOT explain where information came from
- Do NOT mention missing information or unavailable data
- Do NOT add unnecessary explanations
- Do NOT include irrelevant information
- Keep response SHORT and TO THE POINT

{historyContext}Question: {query}

Available Information:
{string.Join("\n\n", combinedContext)}

Direct Answer:";
        }

        /// <summary>
        /// Builds a prompt for general conversation
        /// </summary>
        public string BuildConversationPrompt(string query, string? conversationHistory = null)
        {
            var historyContext = !string.IsNullOrEmpty(conversationHistory)
                ? $"\n\nRecent conversation context:\n{_conversationManager.TruncateConversationHistory(conversationHistory, maxTurns: 3)}\n"
                : "";

            return $@"You are a helpful AI assistant. Answer the user's question naturally and friendly.
Keep responses concise and relevant to the current question.

{historyContext}Current question: {query}

Answer:";
        }
    }
}

