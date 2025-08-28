using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using AzureBot.Models;

namespace AzureBot.Services
{
    public interface IConversationSessionManager
    {
        Task<string> GetOrCreateLangGraphSessionAsync(string conversationId, string userId, CancellationToken cancellationToken = default);
        Task EndConversationAsync(string conversationId, CancellationToken cancellationToken = default);
        void ClearExpiredSessions();
        int GetActiveSessionCount();
    }

    public class ConversationSessionManager : IConversationSessionManager, IDisposable
    {
        private readonly ILangGraphService _langGraphService;
        private readonly ILogger<ConversationSessionManager> _logger;
        private readonly ConcurrentDictionary<string, ConversationSession> _conversationSessions;
        private readonly Timer _cleanupTimer;
        private readonly TimeSpan _sessionTimeout;

        public ConversationSessionManager(
            ILangGraphService langGraphService,
            ILogger<ConversationSessionManager> logger)
        {
            _langGraphService = langGraphService ?? throw new ArgumentNullException(nameof(langGraphService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _conversationSessions = new ConcurrentDictionary<string, ConversationSession>();
            _sessionTimeout = TimeSpan.FromMinutes(30); // Match LangGraph default timeout
            
            // Setup cleanup timer to run every 5 minutes
            _cleanupTimer = new Timer(
                _ => ClearExpiredSessions(),
                null,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(5));

            _logger.LogInformation("ConversationSessionManager initialized with {TimeoutMinutes}min timeout", 
                _sessionTimeout.TotalMinutes);
        }

        public async Task<string> GetOrCreateLangGraphSessionAsync(string conversationId, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentNullException(nameof(conversationId));

            // Create a composite key using conversation and user ID for better isolation
            var sessionKey = $"{conversationId}:{userId ?? "anonymous"}";

            // Check if we have an existing session
            if (_conversationSessions.TryGetValue(sessionKey, out var existingSession))
            {
                if (!existingSession.IsExpired(_sessionTimeout))
                {
                    existingSession.UpdateActivity();
                    _logger.LogDebug("Using existing LangGraph session {LangGraphSessionId} for conversation {ConversationId}", 
                        existingSession.LangGraphSessionId, conversationId);
                    return existingSession.LangGraphSessionId;
                }
                else
                {
                    _logger.LogInformation("Existing session expired for conversation {ConversationId}, creating new one", conversationId);
                    // Remove expired session
                    _conversationSessions.TryRemove(sessionKey, out _);
                    // End the LangGraph session (fire and forget)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _langGraphService.EndSessionAsync(existingSession.LangGraphSessionId, CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to end expired LangGraph session {LangGraphSessionId}", 
                                existingSession.LangGraphSessionId);
                        }
                    }, CancellationToken.None);
                }
            }

            // Create new LangGraph session
            try
            {
                var sessionResponse = await _langGraphService.CreateSessionAsync(cancellationToken);
                
                var conversationSession = new ConversationSession
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    LangGraphSessionId = sessionResponse.SessionId,
                    CreatedAt = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow
                };

                _conversationSessions.TryAdd(sessionKey, conversationSession);
                
                _logger.LogInformation("Created new LangGraph session {LangGraphSessionId} for conversation {ConversationId}", 
                    sessionResponse.SessionId, conversationId);
                
                return sessionResponse.SessionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create LangGraph session for conversation {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task EndConversationAsync(string conversationId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                return;

            // Find and remove all sessions for this conversation
            var sessionsToEnd = _conversationSessions
                .Where(kvp => kvp.Value.ConversationId == conversationId)
                .ToList();

            foreach (var sessionKvp in sessionsToEnd)
            {
                _conversationSessions.TryRemove(sessionKvp.Key, out var session);
                
                if (session != null)
                {
                    try
                    {
                        await _langGraphService.EndSessionAsync(session.LangGraphSessionId, cancellationToken);
                        _logger.LogInformation("Ended LangGraph session {LangGraphSessionId} for conversation {ConversationId}", 
                            session.LangGraphSessionId, conversationId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to end LangGraph session {LangGraphSessionId} for conversation {ConversationId}", 
                            session.LangGraphSessionId, conversationId);
                    }
                }
            }
        }

        public void ClearExpiredSessions()
        {
            var expiredSessions = _conversationSessions
                .Where(kvp => kvp.Value.IsExpired(_sessionTimeout))
                .ToList();

            if (expiredSessions.Count == 0)
                return;

            _logger.LogInformation("Cleaning up {ExpiredCount} expired conversation sessions", expiredSessions.Count);

            foreach (var expiredSession in expiredSessions)
            {
                _conversationSessions.TryRemove(expiredSession.Key, out var session);
                
                if (session != null)
                {
                    // End LangGraph session asynchronously (fire and forget)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _langGraphService.EndSessionAsync(session.LangGraphSessionId, CancellationToken.None);
                            _logger.LogDebug("Cleaned up expired LangGraph session {LangGraphSessionId}", 
                                session.LangGraphSessionId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to end expired LangGraph session {LangGraphSessionId}", 
                                session.LangGraphSessionId);
                        }
                    }, CancellationToken.None);
                }
            }
        }

        public int GetActiveSessionCount()
        {
            return _conversationSessions.Count;
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }

    public class ConversationSession
    {
        public string ConversationId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string LangGraphSessionId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivity { get; set; }

        public void UpdateActivity()
        {
            LastActivity = DateTime.UtcNow;
        }

        public bool IsExpired(TimeSpan timeout)
        {
            return DateTime.UtcNow - LastActivity > timeout;
        }
    }
}