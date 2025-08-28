using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;
using AzureBot.Configuration;
using AzureBot.Models;

namespace AzureBot.Services
{
    public interface ILangGraphService
    {
        Task<SessionResponse> CreateSessionAsync(CancellationToken cancellationToken = default);
        Task<QueryResponse> SendQueryAsync(string sessionId, string query, CancellationToken cancellationToken = default);
        Task<SessionHistoryResponse> GetSessionHistoryAsync(string sessionId, CancellationToken cancellationToken = default);
        Task<SessionEndResponse> EndSessionAsync(string sessionId, CancellationToken cancellationToken = default);
        Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    }

    public class LangGraphService : ILangGraphService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LangGraphService> _logger;
        private readonly LangGraphApiOptions _options;

        public LangGraphService(
            HttpClient httpClient,
            IOptions<LangGraphApiOptions> options,
            ILogger<LangGraphService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AzureBot-LangGraph-Integration/1.0");
        }

        public async Task<SessionResponse> CreateSessionAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new LangGraph session");

            try
            {
                var requestContent = new StringContent(
                    JsonConvert.SerializeObject(new CreateSessionRequest()),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("/api/v1/sessions/", requestContent, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var sessionResponse = JsonConvert.DeserializeObject<SessionResponse>(content);
                    
                    _logger.LogInformation("Created LangGraph session: {SessionId}", sessionResponse?.SessionId);
                    return sessionResponse ?? throw new InvalidOperationException("Failed to deserialize session response");
                }

                await HandleErrorResponse(response, "create session");
                throw new HttpRequestException($"Failed to create session: {response.StatusCode}");
            }
            catch (Exception ex) when (!(ex is HttpRequestException))
            {
                _logger.LogError(ex, "Error creating LangGraph session");
                throw new LangGraphServiceException("Failed to create session", ex);
            }
        }

        public async Task<QueryResponse> SendQueryAsync(string sessionId, string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId));
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            _logger.LogInformation("Sending query to LangGraph session {SessionId}: {Query}", 
                sessionId, query.Length > 50 ? $"{query[..50]}..." : query);

            try
            {
                var requestContent = new StringContent(
                    JsonConvert.SerializeObject(new QueryRequest { Query = query }),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync($"/api/v1/chat/{sessionId}/query", requestContent, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var queryResponse = JsonConvert.DeserializeObject<QueryResponse>(content);
                    
                    _logger.LogInformation("Received response from LangGraph session {SessionId} in {ProcessingTime}ms", 
                        sessionId, queryResponse?.ProcessingTime * 1000);
                    
                    return queryResponse ?? throw new InvalidOperationException("Failed to deserialize query response");
                }

                await HandleErrorResponse(response, "send query");
                throw new HttpRequestException($"Failed to send query: {response.StatusCode}");
            }
            catch (Exception ex) when (!(ex is HttpRequestException))
            {
                _logger.LogError(ex, "Error sending query to LangGraph session {SessionId}", sessionId);
                throw new LangGraphServiceException("Failed to send query", ex);
            }
        }

        public async Task<SessionHistoryResponse> GetSessionHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId));

            _logger.LogDebug("Getting history for LangGraph session {SessionId}", sessionId);

            try
            {
                var response = await _httpClient.GetAsync($"/api/v1/chat/{sessionId}/history", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var historyResponse = JsonConvert.DeserializeObject<SessionHistoryResponse>(content);
                    
                    _logger.LogDebug("Retrieved history for session {SessionId}: {MessageCount} messages", 
                        sessionId, historyResponse?.MessageCount);
                    
                    return historyResponse ?? throw new InvalidOperationException("Failed to deserialize history response");
                }

                await HandleErrorResponse(response, "get session history");
                throw new HttpRequestException($"Failed to get session history: {response.StatusCode}");
            }
            catch (Exception ex) when (!(ex is HttpRequestException))
            {
                _logger.LogError(ex, "Error getting history for LangGraph session {SessionId}", sessionId);
                throw new LangGraphServiceException("Failed to get session history", ex);
            }
        }

        public async Task<SessionEndResponse> EndSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId));

            _logger.LogInformation("Ending LangGraph session {SessionId}", sessionId);

            try
            {
                var response = await _httpClient.DeleteAsync($"/api/v1/sessions/{sessionId}", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var endResponse = JsonConvert.DeserializeObject<SessionEndResponse>(content);
                    
                    _logger.LogInformation("Ended LangGraph session {SessionId}: {Success}", 
                        sessionId, endResponse?.Success);
                    
                    return endResponse ?? throw new InvalidOperationException("Failed to deserialize end session response");
                }

                await HandleErrorResponse(response, "end session");
                throw new HttpRequestException($"Failed to end session: {response.StatusCode}");
            }
            catch (Exception ex) when (!(ex is HttpRequestException))
            {
                _logger.LogError(ex, "Error ending LangGraph session {SessionId}", sessionId);
                throw new LangGraphServiceException("Failed to end session", ex);
            }
        }

        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/v1/health", cancellationToken);
                var isHealthy = response.IsSuccessStatusCode;
                
                _logger.LogDebug("LangGraph API health check: {Status}", isHealthy ? "Healthy" : "Unhealthy");
                return isHealthy;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LangGraph API health check failed");
                return false;
            }
        }

        private async Task HandleErrorResponse(HttpResponseMessage response, string operation)
        {
            try
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(errorContent);
                
                _logger.LogWarning("LangGraph API error during {Operation}: {StatusCode} - {Error}: {Detail}", 
                    operation, response.StatusCode, errorResponse?.Error, errorResponse?.Detail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse error response from LangGraph API during {Operation}: {StatusCode}", 
                    operation, response.StatusCode);
            }
        }
    }

    public class LangGraphServiceException : Exception
    {
        public LangGraphServiceException(string message) : base(message) { }
        public LangGraphServiceException(string message, Exception innerException) : base(message, innerException) { }
    }
}