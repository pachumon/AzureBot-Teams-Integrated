using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using AzureBot.Models;

namespace AzureBot.Services
{
    public interface IMessageTransformationService
    {
        string ExtractTextFromActivity(IMessageActivity activity);
        IMessageActivity CreateReplyActivity(IMessageActivity originalActivity, QueryResponse langGraphResponse);
        IMessageActivity CreateTypingActivity(IMessageActivity originalActivity);
        IMessageActivity CreateErrorActivity(IMessageActivity originalActivity, string errorMessage, bool isLangGraphDown = false);
    }

    public class MessageTransformationService : IMessageTransformationService
    {
        private readonly ILogger<MessageTransformationService> _logger;

        public MessageTransformationService(ILogger<MessageTransformationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string ExtractTextFromActivity(IMessageActivity activity)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));

            // Extract text from the activity, handling different scenarios
            var text = activity.Text?.Trim() ?? string.Empty;

            // Handle @mentions in Teams (remove bot mentions)
            if (activity.Entities != null)
            {
                foreach (var entity in activity.Entities)
                {
                    if (entity.Type == "mention" && entity.Properties != null)
                    {
                        // Remove @bot mentions from the text
                        var mention = entity.Properties["text"]?.ToString();
                        if (!string.IsNullOrEmpty(mention))
                        {
                            text = text.Replace(mention, "").Trim();
                        }
                    }
                }
            }

            // Clean up any extra whitespace
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

            _logger.LogDebug("Extracted text from activity: '{Text}' (original: '{OriginalText}')", 
                text, activity.Text);

            return text;
        }

        public IMessageActivity CreateReplyActivity(IMessageActivity originalActivity, QueryResponse langGraphResponse)
        {
            if (originalActivity == null)
                throw new ArgumentNullException(nameof(originalActivity));
            if (langGraphResponse == null)
                throw new ArgumentNullException(nameof(langGraphResponse));

            var reply = MessageFactory.Text(langGraphResponse.Response);

            // Copy conversation reference
            reply.Conversation = originalActivity.Conversation;
            reply.From = originalActivity.Recipient; // Bot becomes the sender
            reply.Recipient = originalActivity.From; // User becomes the recipient

            // Add metadata about processing
            if (langGraphResponse.ProcessingTime > 0)
            {
                reply.Properties = new Newtonsoft.Json.Linq.JObject
                {
                    ["langGraphProcessingTime"] = langGraphResponse.ProcessingTime,
                    ["langGraphSessionId"] = langGraphResponse.SessionId,
                    ["langGraphMessageCount"] = langGraphResponse.MessageCount
                };
            }

            _logger.LogDebug("Created reply activity for session {SessionId} with {ResponseLength} characters", 
                langGraphResponse.SessionId, langGraphResponse.Response?.Length ?? 0);

            return reply;
        }

        public IMessageActivity CreateTypingActivity(IMessageActivity originalActivity)
        {
            if (originalActivity == null)
                throw new ArgumentNullException(nameof(originalActivity));

            var typingActivity = new Activity
            {
                Type = ActivityTypes.Typing,
                Conversation = originalActivity.Conversation,
                From = originalActivity.Recipient, // Bot is typing
                Recipient = originalActivity.From
            };

            _logger.LogDebug("Created typing activity for conversation {ConversationId}", 
                originalActivity.Conversation?.Id);

            return typingActivity;
        }

        public IMessageActivity CreateErrorActivity(IMessageActivity originalActivity, string errorMessage, bool isLangGraphDown = false)
        {
            if (originalActivity == null)
                throw new ArgumentNullException(nameof(originalActivity));
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentNullException(nameof(errorMessage));

            string userFriendlyMessage;

            if (isLangGraphDown)
            {
                userFriendlyMessage = "I'm currently having trouble connecting to my AI backend. Please try again in a few moments. " +
                                    "If the problem persists, I'll fall back to basic responses.";
            }
            else
            {
                userFriendlyMessage = "I encountered an issue while processing your request. Please try rephrasing your question or try again later.";
            }

            var errorReply = MessageFactory.Text(userFriendlyMessage);

            // Copy conversation reference
            errorReply.Conversation = originalActivity.Conversation;
            errorReply.From = originalActivity.Recipient; // Bot becomes the sender
            errorReply.Recipient = originalActivity.From; // User becomes the recipient

            // Add error metadata for debugging (not shown to user)
            errorReply.Properties = new Newtonsoft.Json.Linq.JObject
            {
                ["errorType"] = isLangGraphDown ? "LangGraphUnavailable" : "ProcessingError",
                ["errorMessage"] = errorMessage,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            _logger.LogWarning("Created error activity for conversation {ConversationId}: {ErrorMessage}", 
                originalActivity.Conversation?.Id, errorMessage);

            return errorReply;
        }
    }
}