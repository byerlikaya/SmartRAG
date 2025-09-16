# SmartRAG Console Chat Application

A simple console application that demonstrates how to use SmartRAG for AI-powered conversations. This application allows you to chat with various AI providers using SmartRAG's conversation history management and intelligent query routing.

## Features

- 🤖 **AI Chat**: Direct conversation with AI providers (OpenAI, Anthropic, Gemini, Azure OpenAI, Custom)
- 💬 **Conversation History**: Automatic session-based conversation management
- 🧠 **Smart Query Routing**: Automatically detects general conversation vs document search
- 🌍 **Language Agnostic**: Works with any language without hardcoded patterns
- ⚙️ **Configurable**: Easy switching between AI providers via configuration
- 🔧 **Simple Setup**: Minimal configuration required

## Quick Start

### 1. Configuration

Copy the development configuration template and add your API keys:

```bash
# Copy template (if not exists)
cp appsettings.Development.json appsettings.Development.json

# Edit with your real API keys
```

### 2. Update Configuration

Edit `appsettings.Development.json` with your API keys:

```json
{
  "SmartRAG": {
    "AIProvider": "OpenAI"
  },
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-proj-YOUR_REAL_OPENAI_KEY_HERE",
      "Model": "gpt-4o-mini"
    }
  }
}
```

### 3. Run the Application

```bash
dotnet run
```

### 4. Start Chatting

```
🚀 SmartRAG Console Chat Application
=====================================

✅ SmartRAG initialized successfully!
💬 Start chatting with AI. Type 'exit' to quit, 'clear' to clear conversation history.

You: Hello! How are you today?
AI: Hello! I'm doing well, thank you for asking! I'm here to help you with any questions...

You: What's the weather like?
AI: I don't have access to real-time weather data, but I'd be happy to help you with...

You: exit
👋 Goodbye!
```

## Supported AI Providers

### OpenAI
```json
{
  "SmartRAG": {
    "AIProvider": "OpenAI"
  },
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-proj-...",
      "Model": "gpt-4o-mini",
      "EmbeddingModel": "text-embedding-ada-002"
    }
  }
}
```

### Anthropic (Claude)
```json
{
  "SmartRAG": {
    "AIProvider": "Anthropic"
  },
  "AI": {
    "Anthropic": {
      "ApiKey": "sk-ant-...",
      "Model": "claude-3.5-sonnet",
      "EmbeddingApiKey": "voyage-...",
      "EmbeddingModel": "voyage-large-2"
    }
  }
}
```

### Google Gemini
```json
{
  "SmartRAG": {
    "AIProvider": "Gemini"
  },
  "AI": {
    "Gemini": {
      "ApiKey": "your-gemini-api-key",
      "Model": "gemini-1.5-pro"
    }
  }
}
```

### Custom Provider (OpenRouter, Groq, etc.)
```json
{
  "SmartRAG": {
    "AIProvider": "Custom"
  },
  "AI": {
    "Custom": {
      "ApiKey": "your-api-key",
      "Endpoint": "https://api.openrouter.ai/v1/chat/completions",
      "Model": "anthropic/claude-3.5-sonnet"
    }
  }
}
```

## Commands

- **Normal text**: Chat with AI
- **`exit`**: Quit the application
- **`clear`**: Clear conversation history (placeholder - needs implementation)

## Features Demonstrated

### 1. Smart Query Intent Detection
The application automatically detects whether your input is:
- **General conversation**: Direct AI chat response
- **Document search**: RAG-based search (when documents are available)

### 2. Conversation History
- Automatic session management
- Context-aware responses
- Persistent conversation across multiple interactions

### 3. Multi-Language Support
- Works with any language (Turkish, English, German, etc.)
- No hardcoded language patterns
- Automatic language detection

## Configuration Options

### AI Provider Selection
Change the AI provider in `appsettings.Development.json`:

```json
{
  "SmartRAG": {
    "AIProvider": "Anthropic"  // Change this
  }
}
```

### Model Configuration
Customize AI model settings:

```json
{
  "AI": {
    "OpenAI": {
      "Model": "gpt-4o",           // Model selection
      "Temperature": 0.7,          // Creativity level
      "MaxTokens": 4096            // Response length
    }
  }
}
```

## Troubleshooting

### Configuration Issues
- Ensure `appsettings.Development.json` exists
- Verify API keys are correct and valid
- Check AI provider selection matches configuration

### API Errors
- Verify network connectivity
- Check API key validity
- Ensure sufficient API credits

### Build Issues
- Run `dotnet restore` to restore packages
- Ensure SmartRAG reference is properly added

## Example Conversations

### General Chat
```
You: How are you today?
AI: I'm doing well, thank you for asking! I'm here to help you with any questions...

You: Tell me a joke
AI: Why don't scientists trust atoms? Because they make up everything! 😄
```

### Multi-Language
```
You: Merhaba! Nasılsın?
AI: Merhaba! Ben iyiyim, teşekkür ederim. Size nasıl yardımcı olabilirim?

You: Wie geht es dir?
AI: Mir geht es gut, danke der Nachfrage! Wie kann ich Ihnen heute helfen?
```

## Next Steps

- Add document upload functionality
- Implement conversation history persistence
- Add more interactive commands
- Integrate with document search capabilities

---

**Built with ❤️ using SmartRAG**

For more information, visit: [SmartRAG Documentation](https://byerlikaya.github.io/SmartRAG)
