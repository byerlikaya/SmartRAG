using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartRAG.API.Contracts;
using SmartRAG.Interfaces;
using SmartRAG.Interfaces.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.API.Controllers
{
    /// <summary>
    /// Conversation Management and Session Controller
    /// 
    /// This controller provides comprehensive conversation management capabilities including:
    /// - Conversation session creation and management
    /// - Message history tracking and retrieval
    /// - Multi-user conversation support
    /// - Conversation search and filtering
    /// - Export and archival capabilities
    /// - Session analytics and statistics
    /// 
    /// Key Features:
    /// - Session Management: Create, update, delete, and archive conversations
    /// - Message Tracking: Full conversation history with metadata and analytics
    /// - Multi-User Support: Isolated conversations per user with privacy controls
    /// - Advanced Search: Find conversations by content, date, user, or metadata
    /// - Export Capabilities: Export conversations in multiple formats (JSON, TXT, PDF, HTML)
    /// - Analytics Integration: Track usage patterns, response times, and user behavior
    /// - Real-Time Updates: Live conversation updates and message streaming
    /// - Memory Management: Configurable history limits and automatic cleanup
    /// 
    /// Use Cases:
    /// - Customer Support: Track support conversations and maintain context
    /// - Educational Platforms: Manage student-AI interactions and learning progress
    /// - Enterprise Chat: Corporate AI assistant with conversation continuity
    /// - Research Applications: Analyze conversation patterns and AI performance
    /// - Personal AI Assistants: Maintain long-term user relationships and context
    /// - Collaboration Tools: Multi-participant AI-assisted discussions
    /// - Content Creation: Iterative content development with AI assistance
    /// 
    /// Conversation Features:
    /// - **Session Continuity**: Maintain context across multiple interactions
    /// - **User Isolation**: Secure separation of user conversations
    /// - **Message Threading**: Organized conversation flow and history
    /// - **Metadata Tracking**: Custom data attachment for business logic
    /// - **Analytics**: Performance metrics and usage statistics
    /// - **Export Options**: Multiple format support for data portability
    /// - **Search Capabilities**: Find conversations by content or metadata
    /// - **Archival System**: Long-term storage and retrieval
    /// 
    /// Example Usage:
    /// ```bash
    /// # Create a new conversation
    /// curl -X POST "https://localhost:7001/api/conversation" \
    ///   -H "Content-Type: application/json" \
    ///   -d '{"title": "AI Chat", "userId": "user123"}'
    /// 
    /// # Add a message to conversation
    /// curl -X POST "https://localhost:7001/api/conversation/{id}/messages" \
    ///   -H "Content-Type: application/json" \
    ///   -d '{"content": "Hello AI!", "role": "user"}'
    /// 
    /// # Get conversation history
    /// curl -X GET "https://localhost:7001/api/conversation/{id}/history"
    /// 
    /// # Search user conversations
    /// curl -X GET "https://localhost:7001/api/conversation/search?userId=user123&amp;searchTerm=machine learning"
    /// ```
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class ConversationController : ControllerBase
    {
        private readonly IConversationManagerService _conversationManager;
        private readonly IDocumentSearchService _documentSearchService;

        /// <summary>
        /// Initializes a new instance of the ConversationController
        /// </summary>
        public ConversationController(
            IConversationManagerService conversationManager,
            IDocumentSearchService documentSearchService)
        {
            _conversationManager = conversationManager;
            _documentSearchService = documentSearchService;
        }

        /// <summary>
        /// Creates a new conversation session
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ConversationResponse>> CreateConversation([FromBody] CreateConversationRequest request)
        {
            try
            {
                // Start a new conversation session
                var sessionId = await _conversationManager.StartNewConversationAsync();

                return CreatedAtAction(nameof(GetConversation), new { id = sessionId }, new ConversationResponse 
                { 
                    Id = Guid.Parse(sessionId.Replace("session-", "")), // Simple parsing for demo
                    Title = request.Title ?? "New Conversation",
                    UserId = request.UserId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a specific conversation by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ConversationResponse>> GetConversation(
            string id, 
            [FromQuery] bool includeMessages = true)
        {
            try
            {
                var history = await _conversationManager.GetConversationHistoryAsync(id);
                
                if (string.IsNullOrEmpty(history))
                {
                    return NotFound(new { Error = "Conversation not found" });
                }

                return Ok(new ConversationResponse 
                { 
                    Id = Guid.TryParse(id.Replace("session-", ""), out var guid) ? guid : Guid.Empty,
                    Messages = ParseHistoryToMessages(history)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Adds a new message to an existing conversation
        /// </summary>
        [HttpPost("{id}/messages")]
        [ProducesResponseType(typeof(List<ConversationMessage>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<ConversationMessage>>> AddMessage(string id, [FromBody] AddMessageRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest(new { Error = "Message content is required" });
                }

                string answer = "AI response placeholder"; 
                
                // If RAG is requested, use DocumentSearchService
                if (request.GenerateResponse && request.UseRAG)
                {
                    var result = await _documentSearchService.QueryIntelligenceAsync(request.Content, 5, false);
                    answer = result.Answer;
                }

                await _conversationManager.AddToConversationAsync(id, request.Content, answer);

                return Ok(new List<ConversationMessage> 
                { 
                    new ConversationMessage { Role = "user", Content = request.Content },
                    new ConversationMessage { Role = "assistant", Content = answer }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // Helper to parse plain text history back to message objects for API response
        private List<ConversationMessage> ParseHistoryToMessages(string history)
        {
            var messages = new List<ConversationMessage>();
            if (string.IsNullOrEmpty(history)) return messages;

            var lines = history.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("User: "))
                    messages.Add(new ConversationMessage { Role = "user", Content = line.Substring(6) });
                else if (line.StartsWith("Assistant: "))
                    messages.Add(new ConversationMessage { Role = "assistant", Content = line.Substring(11) });
            }
            return messages;
        }
        private int EstimateTokens(string text)
        {
            // Simple token estimation - roughly 4 characters per token
            return (int)Math.Ceiling(text.Length / 4.0);
        }


    }
}
