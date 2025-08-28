# Azure Bot Framework + LangGraph Integration Setup

This document provides instructions for setting up and testing the hybrid integration between the Azure Bot Framework and LangGraph API.

## Architecture Overview

The hybrid integration works as follows:
```
User → Bot Framework (API Layer) → LangGraph API (AI Backend) → Gemini AI
```

**Azure Bot Framework** serves as the API layer handling:
- Channel management (Teams, Web Chat, etc.)
- User authentication and session management
- Message formatting and rich content
- Error handling and fallbacks

**LangGraph API** serves as the AI backend providing:
- Intelligent conversation processing
- Geography expertise with specialized prompts
- Persistent conversation state
- Advanced workflow orchestration

## Prerequisites

1. **.NET 9.0 SDK** - For running the Bot Framework project
2. **Python 3.10+** - For running the LangGraph API
3. **Google Gemini API Key** - For AI processing
4. **Bot Framework Emulator** - For testing locally

## Setup Instructions

### 1. Setup LangGraph API Backend

First, start the LangGraph API server:

```bash
cd C:\Users\pachumon\git\LangGraph-Agent

# Create virtual environment
python -m venv .venv

# Activate virtual environment (Windows)
.venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt

# Create .env file with your Gemini API key
echo "GEMINI_API_KEY=your_actual_api_key_here" > .env

# Start the LangGraph API server
python run_api.py
```

The LangGraph API will be available at: `http://localhost:8000`

**Verify it's running:**
- Open: http://localhost:8000/docs
- Check health: http://localhost:8000/api/v1/health

### 2. Setup Azure Bot Framework

In a new terminal, start the Bot Framework project:

```bash
cd C:\Users\pachumon\git\AzureBot-Teams-Integrated\BotApp

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the bot
dotnet run
```

The Bot Framework will be available at: `http://localhost:5000`

### 3. Test with Bot Framework Emulator

1. **Download and install** [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator/releases)

2. **Start the emulator** and create a new bot configuration:
   - **Bot URL**: `http://localhost:5000/api/messages`
   - **Microsoft App ID**: Leave blank for local testing
   - **Microsoft App Password**: Leave blank for local testing

3. **Start chatting** with your bot!

## Testing Scenarios

### Geography Questions (Routed to LangGraph)
These will be processed by the LangGraph geography agent:

```
User: "What's the capital of France?"
Bot: "The capital of France is Paris. Paris has been the capital since..."

User: "Tell me about Tokyo"
Bot: "Tokyo is the capital city of Japan..."

User: "What country has Berlin as its capital?"
Bot: "Berlin is the capital of Germany..."
```

### Non-Geography Questions (Default Response)
These will receive polite redirection:

```
User: "What's 2 + 2?"
Bot: "I can only help with country capitals. Please ask about a country's capital city."

User: "How are you?"
Bot: "I can only help with country capitals. Please ask about a country's capital city."
```

### Conversation Context
The bot maintains context across multiple messages:

```
User: "What's the capital of Italy?"
Bot: "The capital of Italy is Rome..."

User: "Tell me more about it"
Bot: "Rome, the capital we just discussed, is one of the world's oldest cities..."
```

### Error Scenarios
Test error handling by:

1. **Stop the LangGraph API** (Ctrl+C) and send messages
   - Bot should show fallback responses

2. **Invalid questions** that cause processing errors
   - Bot should show user-friendly error messages

## Configuration

### Bot Framework Configuration

Edit `appsettings.json`:

```json
{
  "LangGraphApi": {
    "BaseUrl": "http://localhost:8000",
    "TimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 1
  }
}
```

### LangGraph API Configuration

Edit `.env` file in LangGraph project:

```bash
GEMINI_API_KEY=your_actual_api_key_here
HOST=0.0.0.0
PORT=8000
SESSION_TIMEOUT_MINUTES=30
GEMINI_MODEL=gemini-2.0-flash-exp
GEMINI_TEMPERATURE=0.7
```

## Monitoring and Debugging

### Bot Framework Logs
The Bot Framework provides detailed logging:

```
info: AzureBot.Bots.EchoBot[0]
      Processing message from user test-user in conversation test-conversation: What's the capital of France?
      
info: AzureBot.Bots.EchoBot[0]
      Successfully processed message for user test-user, session 123e4567-e89b-12d3-a456-426614174000
```

### LangGraph API Logs
The LangGraph API provides request/response logging:

```
INFO:     POST /api/v1/sessions/ - 200 OK - 0.150s
INFO:     POST /api/v1/chat/123e4567-e89b-12d3-a456-426614174000/query - 200 OK - 2.340s
```

### Health Checks
Both services provide health endpoints:

- **Bot Framework**: http://localhost:5000 (shows API info)
- **LangGraph API**: http://localhost:8000/api/v1/health

## Troubleshooting

### Common Issues

1. **"LangGraph service is unhealthy"**
   - Ensure LangGraph API is running on port 8000
   - Check the BaseUrl in appsettings.json
   - Verify Gemini API key is set correctly

2. **"Session not found or expired"**
   - Sessions timeout after 30 minutes of inactivity
   - Restart the conversation to create a new session

3. **"Failed to create session"**
   - Check LangGraph API logs for errors
   - Verify Gemini API key has sufficient quota
   - Ensure network connectivity between services

4. **Bot responds with echo instead of AI**
   - Check that all services are properly registered in Startup.cs
   - Verify the Bot Framework can reach LangGraph API
   - Check for dependency injection errors in logs

### Development Tips

1. **Use separate terminals** for each service to see logs clearly
2. **Check health endpoints** regularly during development
3. **Monitor both sets of logs** for comprehensive debugging
4. **Use Bot Framework Emulator's** Inspector to see message metadata
5. **Test fallback scenarios** by stopping LangGraph API

## Production Deployment

For production deployment:

1. **Update configurations** for production URLs
2. **Configure proper authentication** for Bot Framework (Azure AD)
3. **Set up HTTPS** for both services
4. **Configure logging** to external systems
5. **Set up monitoring** and health checks
6. **Scale LangGraph API** horizontally if needed

## Success Criteria

The integration is working correctly when:

✅ Bot Framework starts without errors
✅ LangGraph API is healthy and responding
✅ Geography questions receive intelligent responses
✅ Non-geography questions receive polite redirections  
✅ Conversation context is maintained across messages
✅ Typing indicators appear during processing
✅ Fallback responses work when LangGraph is unavailable
✅ Session cleanup happens automatically
✅ Both services log requests and responses properly

## Next Steps

Once the basic integration is working:

1. **Add rich cards** for geography responses with maps
2. **Implement Teams-specific features** like adaptive cards
3. **Add more specialized agents** (history, culture, etc.)
4. **Set up production monitoring** and alerting
5. **Configure CI/CD pipelines** for both services
6. **Add integration tests** for the hybrid system