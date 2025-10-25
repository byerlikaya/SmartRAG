# 🚀 SmartRAG

**Multi-Database RAG Library for .NET**  
Ask questions about your data in natural language

## ✨ What SmartRAG Does

**Transform your data into intelligent conversations.** SmartRAG enables you to ask natural language questions across multiple databases, documents, images, and audio files - all through a single, unified API.

### 🎯 Key Capabilities

- 🗄️ **Multi-Database RAG** - Query SQL Server, MySQL, PostgreSQL, SQLite together
- 📄 **Multi-Modal Intelligence** - PDF, Word, Excel, Images (OCR), Audio (Whisper.net)
- 🔒 **On-Premise Ready** - 100% local with Ollama, LM Studio, Whisper.net
- 💬 **Conversation History** - Built-in automatic context management
- 🤖 **Universal AI Support** - OpenAI, Anthropic, Gemini, Azure, Custom APIs
- 🏢 **Enterprise Storage** - Qdrant, Redis, SQLite, FileSystem, In-Memory

## 🚀 Quick Start

```bash
dotnet add package SmartRAG
```

```csharp
// Program.cs
builder.Services.UseSmartRAG(builder.Configuration,
    aiProvider: AIProvider.OpenAI,
    storageProvider: StorageProvider.InMemory
);

// Ask questions
var result = await _intelligence.QueryIntelligenceAsync("Your question here");
```

## 📚 Documentation & Examples

- **📖 [Complete Documentation](https://byerlikaya.github.io/SmartRAG)** - Comprehensive guides, API reference, and tutorials
- **🐙 [GitHub Repository](https://github.com/byerlikaya/SmartRAG)** - Source code, examples, and community
- **💡 [Live Examples](https://byerlikaya.github.io/SmartRAG/en/examples)** - Real-world usage scenarios

## 🎯 Perfect For

- **Enterprise Applications** - Multi-database intelligence systems
- **Document Management** - PDF, Word, Excel processing with AI
- **Compliance Systems** - GDPR/KVKK/HIPAA compliant deployments
- **Local AI Solutions** - On-premise intelligence without cloud dependencies
- **Multi-Modal Applications** - Text, images, audio, and database integration

## 🆕 What's New in v3.0.2

- 🗄️ **Multi-Database RAG** - Query multiple databases with natural language
- 🖼️ **OCR Support** - Image processing with Tesseract 5.2.0
- 🎤 **Audio Support** - Local transcription with Whisper.net (99+ languages)
- 💬 **Conversation History** - Built-in session management
- 🔄 **Enhanced API** - Improved intelligence query interface

[View full changelog →](https://github.com/byerlikaya/SmartRAG/blob/main/CHANGELOG.md)

## 📞 Support & Contact

- **📧 [Email Support](mailto:b.yerlikaya@outlook.com)**
- **💼 [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/)**
- **🐙 [GitHub Issues](https://github.com/byerlikaya/SmartRAG/issues)**

## 📄 License

MIT License - see [LICENSE](https://github.com/byerlikaya/SmartRAG/blob/main/LICENSE) for details.

---

**Built with ❤️ by Barış Yerlikaya**

Made in Turkey 🇹🇷 | [Contact](mailto:b.yerlikaya@outlook.com) | [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/) | [Website](https://byerlikaya.github.io/SmartRAG/en/)