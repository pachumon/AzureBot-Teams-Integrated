# CLAUDE.md - Project Context for Claude Code

## Project Overview
**AzureBot-Teams-Integrated** is a simple Azure Bot Framework Echo Bot built with C# and .NET 9.0. This is a starter template for building conversational bots that can be deployed to Azure and integrated with Microsoft Teams.

## Technology Stack
- **.NET 9.0** with ASP.NET Core
- **Azure Bot Framework v4.22.7**
- **C#** programming language
- **Newtonsoft.Json** for serialization

## Project Structure
```
BotApp/
├── Program.cs                    # Application entry point
├── Startup.cs                   # DI container and middleware setup
├── Controllers/BotController.cs # HTTP endpoint (/api/messages)
├── Bots/EchoBot.cs             # Main bot logic
├── AdapterWithErrorHandler.cs   # Error handling adapter
├── AzureBot.csproj             # Project file
├── appsettings.json            # Configuration
└── Properties/launchSettings.json
```

## Key Components

### EchoBot (`Bots/EchoBot.cs`)
- Inherits from `ActivityHandler`
- **Echo functionality**: Repeats user messages with "Echo: " prefix
- **Welcome messages**: Greets new conversation members
- Main methods:
  - `OnMessageActivityAsync()` - Handles incoming messages
  - `OnMembersAddedAsync()` - Handles new member events

### BotController (`Controllers/BotController.cs`)
- API endpoint at `/api/messages`
- Accepts both GET and POST requests
- Processes bot framework activities through the adapter

### AdapterWithErrorHandler
- Extends `CloudAdapter`
- Implements error handling with user-friendly messages
- Logs exceptions for debugging

#### Error Handling Strategy Details
- **Exception Logging**: Structured logging with detailed exception messages
- **User Communication**: Sends friendly error messages to chat ("The bot encountered an error or bug.")
- **Developer Support**: Creates trace activities for Bot Framework Emulator debugging
- **Graceful Degradation**: Bot continues running after errors without crashing

## Dependency Injection Setup
- **BotFrameworkAuthentication**: `ConfigurationBotFrameworkAuthentication` (Singleton)
- **IBotFrameworkHttpAdapter**: `AdapterWithErrorHandler` (Singleton) 
- **IBot**: `EchoBot` (Transient)
- **Web Services**: Standard ASP.NET Core with Newtonsoft.Json serialization
- **Middleware Pipeline**: Static files, routing, authorization, WebSockets support

## Development Commands
```bash
# Restore dependencies
dotnet restore

# Run locally (localhost:3978)
dotnet run

# Build project
dotnet build
```

## Configuration

### Configuration Architecture
- **`appsettings.Development.json`**: Development-only settings (minimal logging config)
- **`appsettings.json`**: Production configuration with Azure credential placeholders
- **`Properties/launchSettings.json`**: Local development server settings (localhost:3978)
- **Environment-based**: Uses `ASPNETCORE_ENVIRONMENT` for configuration selection

### Environment Settings
- **Local Development**: No authentication required
- **Azure Deployment**: Requires bot credentials in `appsettings.json`:
  - `MicrosoftAppId`
  - `MicrosoftAppPassword`
  - `MicrosoftAppTenantId`
  - `MicrosoftAppType`

## Testing
- Use **Bot Framework Emulator** for local testing
- Connect to: `http://localhost:3978/api/messages`

## Current Functionality
✅ Echo messages back to users  
✅ Welcome new conversation members  
✅ Error handling and logging  
✅ Ready for Azure deployment  

## Extension Opportunities
- LUIS integration for natural language understanding
- QnA Maker for FAQ capabilities
- Microsoft Teams channel integration
- Rich cards and attachments
- Conversation state and user profiles
- Multi-turn conversations
- Adaptive cards

## Important Files to Know
- **Main bot logic**: `BotApp/Bots/EchoBot.cs`
- **API endpoint**: `BotApp/Controllers/BotController.cs`
- **Error handling**: `BotApp/AdapterWithErrorHandler.cs`
- **Configuration**: `BotApp/appsettings.json`
- **Dependencies**: `BotApp/AzureBot.csproj`

## Implementation Patterns
- **Async/await**: Throughout codebase with proper `CancellationToken` usage
- **Interface-based DI**: Constructor injection with `IBot`, `IBotFrameworkHttpAdapter`
- **ActivityHandler**: Inheritance pattern for bot behavior implementation
- **Environment-based config**: Separation between development and production settings

## Architecture Quality Notes

### Strengths
- **SOLID Principles**: Proper separation of concerns with interface-based design
- **Production Ready**: Comprehensive error handling, logging, and configuration management
- **Modern .NET**: Latest .NET 9.0 framework with nullable reference types enabled
- **Security Conscious**: No hardcoded secrets, environment-based configuration
- **Extensible Design**: Modular structure ready for enhancement

### Current Limitations
- **Stateless**: No conversation state or user profile persistence
- **Single-turn**: Limited to simple request-response interactions
- **Basic Functionality**: No AI/NLU integration (LUIS, QnA Maker)
- **Single Channel**: Not optimized for multi-channel scenarios

## Development Notes
- Project uses dependency injection pattern
- Follows Bot Framework v4 architecture
- Configured for both development and production environments
- Clean, minimal implementation suitable for learning and extension