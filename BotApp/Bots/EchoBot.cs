using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using AzureBot.Services;

namespace AzureBot.Bots
{
    public class EchoBot : ActivityHandler
    {
        private readonly ILangGraphService _langGraphService;
        private readonly IConversationSessionManager _sessionManager;
        private readonly IMessageTransformationService _messageTransformation;
        private readonly ILogger<EchoBot> _logger;

        public EchoBot(
            ILangGraphService langGraphService,
            IConversationSessionManager sessionManager,
            IMessageTransformationService messageTransformation,
            ILogger<EchoBot> logger)
        {
            _langGraphService = langGraphService ?? throw new ArgumentNullException(nameof(langGraphService));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _messageTransformation = messageTransformation ?? throw new ArgumentNullException(nameof(messageTransformation));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var conversationId = turnContext.Activity.Conversation.Id;
            var userId = turnContext.Activity.From.Id;
            var userText = _messageTransformation.ExtractTextFromActivity(turnContext.Activity);

            _logger.LogInformation("Processing message from user {UserId} in conversation {ConversationId}: {UserText}",
                userId, conversationId, userText.Length > 50 ? $"{userText[..50]}..." : userText);

            // Skip empty messages
            if (string.IsNullOrWhiteSpace(userText))
            {
                _logger.LogWarning("Received empty message from user {UserId}", userId);
                return;
            }

            // Show typing indicator
            await turnContext.SendActivityAsync(_messageTransformation.CreateTypingActivity(turnContext.Activity), cancellationToken);

            try
            {
                // Check if LangGraph service is healthy
                var isHealthy = await _langGraphService.IsHealthyAsync(cancellationToken);
                if (!isHealthy)
                {
                    _logger.LogWarning("LangGraph service is unhealthy, providing fallback response");
                    await SendFallbackResponseAsync(turnContext, userText, cancellationToken);
                    return;
                }

                // Get or create LangGraph session for this conversation
                var langGraphSessionId = await _sessionManager.GetOrCreateLangGraphSessionAsync(
                    conversationId, userId, cancellationToken);

                // Send query to LangGraph
                var response = await _langGraphService.SendQueryAsync(langGraphSessionId, userText, cancellationToken);

                // Transform LangGraph response back to Bot Framework format
                var replyActivity = _messageTransformation.CreateReplyActivity(turnContext.Activity, response);

                // Send response to user
                await turnContext.SendActivityAsync(replyActivity, cancellationToken);

                _logger.LogInformation("Successfully processed message for user {UserId}, session {SessionId}",
                    userId, langGraphSessionId);
            }
            catch (LangGraphServiceException ex)
            {
                _logger.LogError(ex, "LangGraph service error for user {UserId}: {Error}", userId, ex.Message);
                
                var errorActivity = _messageTransformation.CreateErrorActivity(
                    turnContext.Activity, ex.Message, isLangGraphDown: true);
                await turnContext.SendActivityAsync(errorActivity, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing message for user {UserId}: {Error}", userId, ex.Message);
                
                var errorActivity = _messageTransformation.CreateErrorActivity(
                    turnContext.Activity, ex.Message, isLangGraphDown: false);
                await turnContext.SendActivityAsync(errorActivity, cancellationToken);
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello! I'm an AI assistant powered by LangGraph. I can help answer questions about geography, particularly country capitals. Feel free to ask me anything!";
            
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                    
                    _logger.LogInformation("Welcomed new member {MemberId} to conversation {ConversationId}", 
                        member.Id, turnContext.Activity.Conversation.Id);
                }
            }
        }

        protected override async Task OnEndOfConversationActivityAsync(ITurnContext<IEndOfConversationActivity> turnContext, CancellationToken cancellationToken)
        {
            var conversationId = turnContext.Activity.Conversation.Id;
            _logger.LogInformation("Ending conversation {ConversationId}", conversationId);

            try
            {
                await _sessionManager.EndConversationAsync(conversationId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to properly end conversation {ConversationId}", conversationId);
            }

            await base.OnEndOfConversationActivityAsync(turnContext, cancellationToken);
        }

        private async Task SendFallbackResponseAsync(ITurnContext<IMessageActivity> turnContext, string userText, CancellationToken cancellationToken)
        {
            // Simple fallback when LangGraph is unavailable
            var fallbackResponse = $"I'm currently experiencing technical difficulties with my AI backend. " +
                                 $"However, I can still provide a simple response: You said '{userText}'. " +
                                 $"Please try again in a few moments for more intelligent responses.";

            var fallbackActivity = MessageFactory.Text(fallbackResponse);
            fallbackActivity.Properties = new Newtonsoft.Json.Linq.JObject
            {
                ["fallbackMode"] = true,
                ["langGraphUnavailable"] = true
            };

            await turnContext.SendActivityAsync(fallbackActivity, cancellationToken);
        }
    }
}