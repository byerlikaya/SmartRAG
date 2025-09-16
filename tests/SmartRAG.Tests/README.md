# SmartRAG Tests

This directory contains integration tests for SmartRAG library, configured exactly like the SmartRAG.Api project.

## Configuration Structure

The test project follows the same configuration pattern as SmartRAG.Api:

### 1. `appsettings.json` (Committed to Git)
- **Template configuration** with placeholder values
- **All AI Providers** configured with `your-*-api-key` placeholders
- **All Storage Providers** configured with template values
- **SmartRAG settings** with default values

### 2. `appsettings.Development.json` (Gitignored)
- **Real test configuration** with actual test values
- **Overrides** `appsettings.json` values
- **Contains test API keys** and endpoints
- **Never committed** to prevent exposing sensitive data

## How It Works

1. **Base Configuration**: `appsettings.json` provides template values
2. **Development Override**: `appsettings.Development.json` overrides with test values
3. **Test Execution**: Tests use the selected AI Provider from configuration
4. **UseSmartRag**: Same extension method as API project

## Configuration Example

### Base Configuration (`appsettings.json`)
```json
{
  "SmartRAG": {
    "AIProvider": "OpenAI"
  },
  "AI": {
    "OpenAI": {
      "ApiKey": "your-openai-api-key",
      "Model": "gpt-4o-mini"
    }
  }
}
```

### Development Override (`appsettings.Development.json`)
```json
{
  "SmartRAG": {
    "AIProvider": "Anthropic"
  },
  "AI": {
    "Anthropic": {
      "ApiKey": "test-anthropic-key",
      "Model": "claude-3-sonnet-20240229"
    }
  }
}
```

**Result**: Tests will use Anthropic provider with test configuration.

## Running Tests

### Default Test
Tests run with the AI Provider specified in `appsettings.Development.json`:

```bash
dotnet test
```

### Change Provider
To test a different provider, update `appsettings.Development.json`:

```json
{
  "SmartRAG": {
    "AIProvider": "Gemini"  // Change this
  }
}
```

## Test Structure

- **`TestAIProviderWithInMemoryStorage_ShouldWork`**: Tests selected AI Provider with RAG
- **`TestAIProviderConfiguration_ShouldBeValid`**: Validates provider configuration
- **`TestStorageProviders_ShouldWork`**: Tests storage providers
- **`TestAIProviderFactory_ShouldCreateAllProviders`**: Tests AI Provider factory
- **`TestStorageFactory_ShouldCreateAllProviders`**: Tests Storage factory
- **`TestEndToEndWorkflow_ShouldWork`**: Tests complete workflow
- **`FileUploadTests`**: Tests document upload functionality including OCR

## Key Features

### ✅ **Same as API Project**
- Uses `UseSmartRag` extension method
- Same configuration structure
- Same service registration pattern

### ✅ **Flexible Provider Selection**
- Change provider in development config
- No code changes needed
- All providers supported (OpenAI, Anthropic, Gemini, Azure OpenAI, Custom)

### ✅ **OCR and Speech-to-Text Testing Support**
- Image processing and OCR functionality tests
- Multiple image format support (.jpg, .png, .gif, .bmp, .tiff, .webp)
- Table extraction and confidence scoring tests
- Audio processing and Speech-to-Text functionality tests
- Multiple audio format support (.mp3, .wav, .m4a, .aac, .ogg, .flac, .wma)
- Transcription accuracy and confidence scoring tests

### ✅ **Enhanced Features Testing**
- Conversation history management
- Enhanced semantic search with hybrid scoring
- Smart query intent detection
- Multi-language OCR support

### ✅ **Secure Configuration**
- Template values in committed files
- Real values in gitignored files
- No accidental API key exposure

## Setup Instructions

1. **Copy development config** (if not exists):
   ```bash
   cp appsettings.Development.json appsettings.Development.json
   ```

2. **Edit development config** with your test values

3. **Select AI Provider** by changing `"AIProvider"` value

4. **Run tests**:
   ```bash
   dotnet test
   ```

## Troubleshooting

### Configuration Not Found
- Ensure `appsettings.Development.json` exists
- Check file paths and JSON syntax
- Verify provider selection in config

### Provider Not Working
- Check API key validity in development config
- Verify endpoint URLs and model names
- Ensure network connectivity to AI services

### UseSmartRag Issues
- Verify SmartRAG package reference
- Check extension method availability
- Ensure configuration binding works

## 📞 Support

For questions, issues, or contributions, please visit our [GitHub repository](https://github.com/byerlikaya/SmartRAG).

### Contact Information
- **📧 [Contact & Support](mailto:b.yerlikaya@outlook.com)**
- **💼 [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/)**
- **🐙 [GitHub Profile](https://github.com/byerlikaya)**
- **📦 [NuGet Packages](https://www.nuget.org/profiles/byerlikaya)**
- **📖 [Documentation](https://byerlikaya.github.io/SmartRAG)** - Comprehensive guides and API reference

---
**Made in Turkey 🇹🇷 | [Contact](mailto:b.yerlikaya@outlook.com) | [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/)**
