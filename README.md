# Azure Bot Framework - Echo Bot

A simple Azure Bot Framework bot built with C# and ASP.NET Core that echoes back user messages.

## Features

- Echo bot functionality - responds to user messages
- Welcome messages for new users
- Error handling and logging
- Ready for local development and Azure deployment

## Prerequisites

- .NET 6.0 SDK or later
- Visual Studio 2022 or Visual Studio Code
- Bot Framework Emulator (for testing locally)

## Getting Started

### 1. Restore Dependencies
```bash
dotnet restore
```

### 2. Run the Bot Locally
```bash
dotnet run
```

The bot will start running on `http://localhost:5000` or `https://localhost:5001`.

### 3. Test with Bot Framework Emulator

1. Download and install [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator/releases)
2. Start the emulator
3. Connect to your bot at: `http://localhost:5000/api/messages`
4. Start chatting with your bot!

## Configuration

### Local Development
For local development, no configuration is needed. The bot runs without authentication.

### Azure Deployment
When deploying to Azure, update `appsettings.json` with your bot's credentials:

```json
{
  "MicrosoftAppType": "MultiTenant",
  "MicrosoftAppId": "your-app-id",
  "MicrosoftAppPassword": "your-app-password",
  "MicrosoftAppTenantId": ""
}
```

## Project Structure

- `Program.cs` - Application entry point and web host configuration
- `Startup.cs` - ASP.NET Core services and middleware configuration
- `Controllers/BotController.cs` - HTTP endpoint for bot messages
- `Bots/EchoBot.cs` - Main bot logic and message handling
- `AdapterWithErrorHandler.cs` - Bot adapter with error handling
- `appsettings.json` - Configuration settings

## Deployment to Azure

1. Create an Azure Bot resource in the Azure portal
2. Configure the messaging endpoint to point to your deployed app
3. Update `appsettings.json` with your bot credentials
4. Deploy using Azure App Service or Azure Container Instances

## Next Steps

- Add LUIS for natural language understanding
- Integrate with QnA Maker for FAQ capabilities
- Connect to Microsoft Teams or other channels
- Add rich cards and attachments
- Implement conversation state and user profiles

## Resources

- [Bot Framework Documentation](https://docs.microsoft.com/en-us/azure/bot-service/)
- [Bot Framework Samples](https://github.com/Microsoft/BotBuilder-Samples)
- [Azure Bot Service](https://azure.microsoft.com/en-us/services/bot-service/)