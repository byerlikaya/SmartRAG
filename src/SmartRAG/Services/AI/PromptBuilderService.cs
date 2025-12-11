#nullable enable

using System;
using System.Linq;
using System.Text.RegularExpressions;
using SmartRAG.Interfaces.Support;
using SmartRAG.Interfaces.AI;

namespace SmartRAG.Services.AI
{
    /// <summary>
    /// Service for building AI prompts
    /// </summary>
    public class PromptBuilderService : IPromptBuilderService
    {
        private readonly Lazy<IConversationManagerService> _conversationManager;

        /// <summary>
        /// Initializes a new instance of the PromptBuilderService
        /// </summary>
        /// <param name="conversationManager">Service for managing conversation sessions and history (lazy to break circular dependency)</param>
        public PromptBuilderService(Lazy<IConversationManagerService> conversationManager)
        {
            _conversationManager = conversationManager;
        }

        /// <summary>
        /// Builds a prompt for document-based RAG answer generation
        /// </summary>
        public string BuildDocumentRagPrompt(string query, string context, string? conversationHistory = null, string? preferredLanguage = null)
        {
            var historyContext = !string.IsNullOrEmpty(conversationHistory)
                ? $"\n\nRecent conversation context:\n{_conversationManager.Value.TruncateConversationHistory(conversationHistory, maxTurns: 30)}\n"
                : "";

            var isVagueQuery = IsVagueQuery(query);

            var hasQuestionPunctuation = query.IndexOf('?', StringComparison.Ordinal) >= 0 ||
                                       query.IndexOf('¿', StringComparison.Ordinal) >= 0 ||
                                       query.IndexOf('؟', StringComparison.Ordinal) >= 0;
            var hasNumbers = query.Any(char.IsDigit);
            var queryTokens = query.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var queryLength = queryTokens.Length;


            var isCountingOrListingQuery = hasQuestionPunctuation && (
                hasNumbers ||
                queryLength >= 5 ||
                HasNumericRangeOrList(query) ||
                HasMultipleNumericGroups(query)
            );

            var countingInstructions = isCountingOrListingQuery
                ? @"
SPECIAL INSTRUCTIONS FOR COUNTING/LISTING QUESTIONS (applies to all languages):
- Look carefully for numbered lists (1., 2., 3., etc.) or bullet points in the context
- Count all items mentioned, even if they are in different parts of the context
- If you see partial lists, look for continuation in adjacent text
- Extract ALL items from numbered or bulleted lists
- If the question asks for a count or quantity (regardless of the language used), provide the exact count AND list all items
- If the question asks for locations or places (regardless of the language used), list all locations mentioned
- Be thorough - scan the ENTIRE context for related information"
                : "";

            var vagueQueryInstructions = isVagueQuery && !string.IsNullOrEmpty(conversationHistory)
                ? @"
SPECIAL INSTRUCTIONS FOR VAGUE QUESTIONS (questions referring to generic person/entity without naming them):
- The current question is vague and refers to a person/entity without naming them
- Use the conversation history above to identify the specific person/entity being discussed
- Search the document context for information about that specific person/entity
- If conversation history mentions a specific name, search for that name in the document context
- If the question asks about attributes (title, position, role, etc.) without naming the person, look for that information about the person mentioned in conversation history
- Combine conversation history context with document context to provide a complete answer"
                : "";

            var languageInstruction = !string.IsNullOrEmpty(preferredLanguage)
                ? GetLanguageInstructionForCode(preferredLanguage)
                : "respond in the SAME language as the query";

            return $@"You are a helpful document analysis assistant. Answer questions based ONLY on the provided document context.

CRITICAL RULES: 
- Base your answer ONLY on the document context provided below
- The query and context may be in ANY language - {languageInstruction}
- SEARCH THOROUGHLY through the entire context before concluding information is missing
- IMPORTANT: The FIRST part of the context (beginning) often contains key document information like headers, titles, key-value pairs, structured data, and important metadata - pay special attention to it
- Look for information in ALL forms: paragraphs, lists, tables, numbered items, bullet points, headings
- TABLE DATA DETECTION: If the context contains structured data (tables, key-value pairs, form fields), carefully extract information from these structures
- TABLE READING: When reading tables, look for column headers and corresponding values - information may be organized in rows and columns
- STRUCTURED DATA: Pay attention to patterns like ""Label: Value"", ""Field: Data"", or tabular structures - these often contain the exact information requested
- KEY-VALUE PAIRS: Look for patterns where a label/question is followed by a value/answer (e.g., ""Label: Value"", ""Field: Data"", ""Question: Answer"")
- DOCUMENT STRUCTURE: PDF documents often have structured sections with labels and values - search for these patterns even if they appear in table-like formats
- If you find ANY relevant or related information (even if not a perfect match), provide an answer based on what you found
- If you have partial information, share it and clearly explain what is available and what might be missing
- If you cannot find the information after thorough search, return exactly and only: [NO_ANSWER_FOUND]
- Do NOT return [NO_ANSWER_FOUND] if you can answer even partially.
- DO provide information if there is ANY related content found, even if incomplete - it's better to share partial information than to say nothing
- Be precise and use exact information from documents
- Synthesize information from multiple parts of the context when needed
- Keep responses focused on the current question
{countingInstructions}
{vagueQueryInstructions}

{historyContext}Current question: {query}

Document context: {context}

Answer:";
        }

        /// <summary>
        /// Builds a prompt for merging hybrid results (database + documents)
        /// </summary>
        public string BuildHybridMergePrompt(string query, string? databaseContext, string? documentContext, string? conversationHistory = null, string? preferredLanguage = null)
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
                ? $"\n\nRecent context:\n{_conversationManager.Value.TruncateConversationHistory(conversationHistory, maxTurns: 30)}\n"
                : "";

            var languageInstruction = !string.IsNullOrEmpty(preferredLanguage)
                ? GetLanguageInstructionForCode(preferredLanguage)
                : "respond in the same language as the query";

            return $@"Answer the user's question using the provided information.

CRITICAL RULES:
- Provide DIRECT, CONCISE answer to the question
- {languageInstruction}
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
        public string BuildConversationPrompt(string query, string? conversationHistory = null, string? preferredLanguage = null)
        {
            var historyContext = !string.IsNullOrEmpty(conversationHistory)
                ? $"\n\nRecent conversation context:\n{_conversationManager.Value.TruncateConversationHistory(conversationHistory, maxTurns: 30)}\n"
                : "";

            var languageInstruction = !string.IsNullOrEmpty(preferredLanguage)
                ? GetLanguageInstructionForCode(preferredLanguage)
                : "respond naturally in the same language as the user's question";

            return $@"You are a helpful AI assistant. Answer the user's question naturally and friendly.
Keep responses concise and relevant to the current question.
{languageInstruction}

{historyContext}Current question: {query}

Answer:";
        }

        /// <summary>
        /// Gets language-specific instruction for AI prompt
        /// </summary>
        /// <param name="languageCode">ISO 639-1 language code (e.g., "tr", "en", "de")</param>
        /// <returns>Explicit language instruction for AI</returns>
        private static string GetLanguageInstructionForCode(string languageCode)
        {
            // Generic language instruction - works for any language without hardcoding specific language names
            // The AI model understands ISO 639-1 codes and will respond in the appropriate language
            return languageCode.ToLowerInvariant() switch
            {
                "en" => "you MUST respond in English",
                _ => $"you MUST respond in the language with ISO code: {languageCode}. Use the appropriate language for user queries and responses."
            };
        }

        /// <summary>
        /// Detects numeric ranges or lists in query (language-agnostic)
        /// Examples: "1-5", "1, 2, 3", "10-20" (works for all languages)
        /// </summary>
        private static bool HasNumericRangeOrList(string input)
        {
            return Regex.IsMatch(input, @"\b\d+\s*[-–—]\s*\d+\b") || // Range pattern: "1-5"
                   Regex.IsMatch(input, @"\b\d+\s*,\s*\d+(\s*,\s*\d+)+\b"); // List pattern: "1, 2, 3"
        }

        /// <summary>
        /// Detects multiple numeric groups in query (language-agnostic)
        /// Example: "Show items 1, 2, 3" has 3 numeric groups
        /// </summary>
        private static bool HasMultipleNumericGroups(string input)
        {
            var matches = Regex.Matches(input, @"\p{Nd}+");
            return matches.Count >= 2;
        }

        /// <summary>
        /// Detects if query is vague (refers to generic person/entity without naming them)
        /// CONSERVATIVE: Only marks as vague if query explicitly uses generic terms AND conversation history exists
        /// This prevents false positives for queries that can be answered directly from document context
        /// Language-agnostic: Uses structural patterns that work for all languages
        /// </summary>
        private static bool IsVagueQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return false;

            var hasName = Regex.IsMatch(query, @"\b\p{Lu}\p{Ll}+\s+\p{Lu}\p{Ll}+\b", RegexOptions.None);
            if (hasName)
                return false;

            var lowerQuery = query.ToLowerInvariant();
            
            var articleGenericPattern = @"\b\p{L}{1,3}\s+\p{L}{4,}\b";
            var hasArticleGenericStructure = Regex.IsMatch(lowerQuery, articleGenericPattern, RegexOptions.None);

            var possessivePattern = @"\b\p{L}+\p{M}*\p{L}*\s+\p{L}+\b";
            var hasPossessiveStructure = Regex.IsMatch(lowerQuery, possessivePattern, RegexOptions.None);

            var genericQuestionPattern = @"\b\p{L}{4,}\s+\p{L}{2,6}\b";
            var hasGenericQuestionStructure = Regex.IsMatch(lowerQuery, genericQuestionPattern, RegexOptions.None);

            var isVague = hasArticleGenericStructure || hasPossessiveStructure || hasGenericQuestionStructure;

            return isVague;
        }
    }
}

