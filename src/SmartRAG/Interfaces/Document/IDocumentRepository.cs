using SmartRAG.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Document
{

    /// <summary>
    /// Repository interface for document storage operations
    /// </summary>
    public interface IDocumentRepository
    {
        /// <summary>
        /// Adds a new document to storage
        /// </summary>
        /// <param name="document">Document entity to add</param>
        /// <returns>Added document entity</returns>
        Task<SmartRAG.Entities.Document> AddAsync(SmartRAG.Entities.Document document);

        /// <summary>
        /// Retrieves document by unique identifier
        /// </summary>
        /// <param name="id">Unique document identifier</param>
        /// <returns>Document entity or null if not found</returns>
        Task<SmartRAG.Entities.Document> GetByIdAsync(Guid id);

        /// <summary>
        /// Retrieves all documents from storage
        /// </summary>
        /// <returns>List of all document entities</returns>
        Task<List<SmartRAG.Entities.Document>> GetAllAsync();

        /// <summary>
        /// Removes document from storage by ID
        /// </summary>
        /// <param name="id">Unique document identifier</param>
        /// <returns>True if document was deleted successfully</returns>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// Gets total count of documents in storage
        /// </summary>
        /// <returns>Total number of documents</returns>
        Task<int> GetCountAsync();

        /// <summary>
        /// Searches documents using query string
        /// </summary>
        /// <param name="query">Search query string</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>List of relevant document chunks</returns>
        Task<List<SmartRAG.Entities.DocumentChunk>> SearchAsync(string query, int maxResults = 5);

        /// <summary>
        /// Gets conversation history for a session
        /// </summary>
        /// <param name="sessionId">Unique session identifier</param>
        /// <returns>Conversation history as formatted text</returns>
        Task<string> GetConversationHistoryAsync(string sessionId);

        /// <summary>
        /// Adds a question-answer pair to conversation history
        /// </summary>
        /// <param name="sessionId">Unique session identifier</param>
        /// <param name="question">User question</param>
        /// <param name="answer">AI-generated answer</param>
        Task AddToConversationAsync(string sessionId, string question, string answer);

        /// <summary>
        /// Clears conversation history for a session
        /// </summary>
        /// <param name="sessionId">Unique session identifier</param>
        Task ClearConversationAsync(string sessionId);

        /// <summary>
        /// Checks if a session exists
        /// </summary>
        /// <param name="sessionId">Unique session identifier</param>
        /// <returns>True if session exists</returns>
        Task<bool> SessionExistsAsync(string sessionId);
    }
}
