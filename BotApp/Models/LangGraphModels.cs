using Newtonsoft.Json;

namespace AzureBot.Models
{
    public class CreateSessionRequest
    {
        // Empty request body for session creation
    }

    public class SessionResponse
    {
        [JsonProperty("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonProperty("created_at")]
        public double CreatedAt { get; set; }

        [JsonProperty("message_count")]
        public int MessageCount { get; set; }
    }

    public class QueryRequest
    {
        [JsonProperty("query")]
        public string Query { get; set; } = string.Empty;
    }

    public class QueryResponse
    {
        [JsonProperty("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonProperty("query")]
        public string Query { get; set; } = string.Empty;

        [JsonProperty("response")]
        public string Response { get; set; } = string.Empty;

        [JsonProperty("message_count")]
        public int MessageCount { get; set; }

        [JsonProperty("processing_time")]
        public double ProcessingTime { get; set; }

        [JsonProperty("timestamp")]
        public double Timestamp { get; set; }
    }

    public class ConversationMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; } = string.Empty;

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        [JsonProperty("timestamp")]
        public double Timestamp { get; set; }
    }

    public class SessionHistoryResponse
    {
        [JsonProperty("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonProperty("created_at")]
        public double CreatedAt { get; set; }

        [JsonProperty("last_activity")]
        public double LastActivity { get; set; }

        [JsonProperty("message_count")]
        public int MessageCount { get; set; }

        [JsonProperty("conversation_history")]
        public List<ConversationMessage> ConversationHistory { get; set; } = new List<ConversationMessage>();
    }

    public class ErrorResponse
    {
        [JsonProperty("error")]
        public string Error { get; set; } = string.Empty;

        [JsonProperty("detail")]
        public string Detail { get; set; } = string.Empty;

        [JsonProperty("timestamp")]
        public double Timestamp { get; set; }
    }

    public class SessionEndResponse
    {
        [JsonProperty("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
    }
}