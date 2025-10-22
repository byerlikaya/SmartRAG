using System;
using System.Collections.Generic;

namespace SmartRAG.API.Contracts
{
    /// <summary>
    /// Response model for conversation operations
    /// </summary>
    public class ConversationResponse
    {
        /// <summary>
        /// Unique identifier for the conversation
        /// </summary>
        /// <example>123e4567-e89b-12d3-a456-426614174000</example>
        public Guid Id { get; set; }

        /// <summary>
        /// Conversation title
        /// </summary>
        /// <example>AI Discussion about Machine Learning</example>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// User ID who owns the conversation
        /// </summary>
        /// <example>user123</example>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// When the conversation was created
        /// </summary>
        /// <example>2024-01-15T10:30:00Z</example>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the conversation was last updated
        /// </summary>
        /// <example>2024-01-15T14:25:00Z</example>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Whether the conversation is archived
        /// </summary>
        /// <example>false</example>
        public bool IsArchived { get; set; } = false;

        /// <summary>
        /// Number of messages in the conversation
        /// </summary>
        /// <example>12</example>
        public int MessageCount { get; set; }

        /// <summary>
        /// Total tokens used in the conversation
        /// </summary>
        /// <example>1250</example>
        public int TotalTokens { get; set; }

        /// <summary>
        /// Conversation metadata
        /// </summary>
        /// <example>{"source": "web", "language": "en"}</example>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Conversation messages (included based on request)
        /// </summary>
        public List<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();

        /// <summary>
        /// Conversation statistics (optional)
        /// </summary>
        public ConversationStats Stats { get; set; } = new ConversationStats();
    }

    /// <summary>
    /// Individual message in a conversation
    /// </summary>
    public class ConversationMessage
    {
        /// <summary>
        /// Unique identifier for the message
        /// </summary>
        /// <example>123e4567-e89b-12d3-a456-426614174001</example>
        public Guid Id { get; set; }

        /// <summary>
        /// ID of the conversation this message belongs to
        /// </summary>
        /// <example>123e4567-e89b-12d3-a456-426614174000</example>
        public Guid ConversationId { get; set; }

        /// <summary>
        /// Role of the message sender
        /// </summary>
        /// <example>user</example>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Message content
        /// </summary>
        /// <example>What is machine learning?</example>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// When the message was created
        /// </summary>
        /// <example>2024-01-15T10:30:00Z</example>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Number of tokens in this message
        /// </summary>
        /// <example>25</example>
        public int TokenCount { get; set; }

        /// <summary>
        /// Time taken to generate this message (for AI responses)
        /// </summary>
        /// <example>1.25</example>
        public double ResponseTimeSeconds { get; set; }

        /// <summary>
        /// AI provider used for this message (if applicable)
        /// </summary>
        /// <example>OpenAI</example>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Model used for this message (if applicable)
        /// </summary>
        /// <example>gpt-4</example>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Whether RAG was used for this message
        /// </summary>
        /// <example>true</example>
        public bool UsedRAG { get; set; } = false;

        /// <summary>
        /// Number of documents referenced (if RAG was used)
        /// </summary>
        /// <example>3</example>
        public int DocumentsReferenced { get; set; } = 0;

        /// <summary>
        /// Message metadata
        /// </summary>
        /// <example>{"confidence": "high", "source": "web"}</example>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Whether this message was edited
        /// </summary>
        /// <example>false</example>
        public bool IsEdited { get; set; } = false;

        /// <summary>
        /// When the message was last edited (if applicable)
        /// </summary>
        public DateTime? EditedAt { get; set; }
    }

    /// <summary>
    /// Conversation statistics
    /// </summary>
    public class ConversationStats
    {
        /// <summary>
        /// Total number of user messages
        /// </summary>
        /// <example>6</example>
        public int UserMessages { get; set; }

        /// <summary>
        /// Total number of assistant messages
        /// </summary>
        /// <example>6</example>
        public int AssistantMessages { get; set; }

        /// <summary>
        /// Total number of system messages
        /// </summary>
        /// <example>1</example>
        public int SystemMessages { get; set; }

        /// <summary>
        /// Average response time for AI messages
        /// </summary>
        /// <example>1.35</example>
        public double AverageResponseTime { get; set; }

        /// <summary>
        /// Average message length in characters
        /// </summary>
        /// <example>125.5</example>
        public double AverageMessageLength { get; set; }

        /// <summary>
        /// Number of times RAG was used
        /// </summary>
        /// <example>4</example>
        public int RAGUsageCount { get; set; }

        /// <summary>
        /// Total documents referenced across all messages
        /// </summary>
        /// <example>12</example>
        public int TotalDocumentsReferenced { get; set; }

        /// <summary>
        /// Conversation duration from first to last message
        /// </summary>
        /// <example>01:23:45</example>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Most used AI provider in this conversation
        /// </summary>
        /// <example>OpenAI</example>
        public string PrimaryProvider { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model for conversation search results
    /// </summary>
    public class ConversationSearchResponse
    {
        /// <summary>
        /// List of conversations matching the search criteria
        /// </summary>
        public List<ConversationResponse> Conversations { get; set; } = new List<ConversationResponse>();

        /// <summary>
        /// Total number of conversations matching the criteria (before pagination)
        /// </summary>
        /// <example>45</example>
        public int TotalCount { get; set; }

        /// <summary>
        /// Number of conversations returned in this response
        /// </summary>
        /// <example>20</example>
        public int ReturnedCount { get; set; }

        /// <summary>
        /// Number of conversations skipped (pagination offset)
        /// </summary>
        /// <example>0</example>
        public int Skip { get; set; }

        /// <summary>
        /// Maximum number of conversations requested
        /// </summary>
        /// <example>20</example>
        public int Limit { get; set; }

        /// <summary>
        /// Whether there are more results available
        /// </summary>
        /// <example>true</example>
        public bool HasMore { get; set; }

        /// <summary>
        /// Search criteria used
        /// </summary>
        public ConversationSearchCriteria SearchCriteria { get; set; } = new ConversationSearchCriteria();
    }

    /// <summary>
    /// Search criteria information
    /// </summary>
    public class ConversationSearchCriteria
    {
        /// <summary>
        /// User ID filter applied
        /// </summary>
        /// <example>user123</example>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Search term used
        /// </summary>
        /// <example>machine learning</example>
        public string SearchTerm { get; set; } = string.Empty;

        /// <summary>
        /// Date range start
        /// </summary>
        /// <example>2024-01-01T00:00:00Z</example>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Date range end
        /// </summary>
        /// <example>2024-01-31T23:59:59Z</example>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Sort order applied
        /// </summary>
        /// <example>created_desc</example>
        public string SortBy { get; set; } = string.Empty;

        /// <summary>
        /// Whether messages were included in results
        /// </summary>
        /// <example>false</example>
        public bool IncludeMessages { get; set; }
    }

    /// <summary>
    /// Response model for conversation session information
    /// </summary>
    public class ConversationSessionResponse
    {
        /// <summary>
        /// Active conversation sessions for a user
        /// </summary>
        public List<ConversationSession> ActiveSessions { get; set; } = new List<ConversationSession>();

        /// <summary>
        /// Total number of active sessions
        /// </summary>
        /// <example>3</example>
        public int TotalActiveSessions { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        /// <example>user123</example>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Session statistics
        /// </summary>
        public SessionStats Stats { get; set; } = new SessionStats();
    }

    /// <summary>
    /// Individual conversation session
    /// </summary>
    public class ConversationSession
    {
        /// <summary>
        /// Conversation ID
        /// </summary>
        /// <example>123e4567-e89b-12d3-a456-426614174000</example>
        public Guid ConversationId { get; set; }

        /// <summary>
        /// Conversation title
        /// </summary>
        /// <example>AI Discussion</example>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Last activity timestamp
        /// </summary>
        /// <example>2024-01-15T14:25:00Z</example>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// Number of messages exchanged
        /// </summary>
        /// <example>8</example>
        public int MessageCount { get; set; }

        /// <summary>
        /// Session duration
        /// </summary>
        /// <example>00:45:30</example>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Whether the session is currently active
        /// </summary>
        /// <example>true</example>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Session statistics
    /// </summary>
    public class SessionStats
    {
        /// <summary>
        /// Total conversations created by user
        /// </summary>
        /// <example>25</example>
        public int TotalConversations { get; set; }

        /// <summary>
        /// Total messages sent by user
        /// </summary>
        /// <example>150</example>
        public int TotalMessages { get; set; }

        /// <summary>
        /// Average conversation length
        /// </summary>
        /// <example>6.5</example>
        public double AverageConversationLength { get; set; }

        /// <summary>
        /// Total time spent in conversations
        /// </summary>
        /// <example>05:30:45</example>
        public TimeSpan TotalTimeSpent { get; set; }

        /// <summary>
        /// Most active day of the week
        /// </summary>
        /// <example>Monday</example>
        public string MostActiveDay { get; set; } = string.Empty;

        /// <summary>
        /// Preferred AI provider
        /// </summary>
        /// <example>OpenAI</example>
        public string PreferredProvider { get; set; } = string.Empty;
    }
}
