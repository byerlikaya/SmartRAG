# 🤝 Contributing to SmartRAG

Thank you for your interest in contributing to SmartRAG! We welcome contributions from the community and appreciate your efforts to improve the project.

## 📋 Table of Contents

- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Contributing Process](#contributing-process)
- [Code Guidelines](#code-guidelines)
- [Testing](#testing)
- [Submitting Changes](#submitting-changes)
- [Community Guidelines](#community-guidelines)

## 🚀 Getting Started

### Prerequisites

- **.NET 9.0 SDK** or later
- **Git**
- **Visual Studio 2022**, **VS Code**, or **JetBrains Rider**
- Basic knowledge of **C#** and **RAG (Retrieval-Augmented Generation)**

### Development Setup

1. **Fork the repository**
   ```bash
   # Fork on GitHub, then clone your fork
   git clone https://github.com/YOUR-USERNAME/SmartRAG.git
   cd SmartRAG
   ```

2. **Set up the development environment**
   ```bash
   # Restore dependencies
   dotnet restore
   
   # Build the solution
   dotnet build
   
   # Run tests
   dotnet test
   ```

3. **Configure your development tools**
   - Install relevant extensions for your IDE
   - Set up code formatting according to `.editorconfig`

## 🔄 Contributing Process

### 1. **Choose or Create an Issue**
- Browse [existing issues](https://github.com/byerlikaya/SmartRAG/issues)
- For bugs: Use the bug report template
- For features: Use the feature request template
- Comment on the issue to indicate you're working on it

### 2. **Create a Branch**
```bash
# Create a new branch from main
git checkout main
git pull origin main
git checkout -b feature/your-feature-name
```

Branch naming conventions:
- `feature/description` - for new features
- `bugfix/description` - for bug fixes
- `docs/description` - for documentation changes
- `refactor/description` - for code refactoring

### 3. **Make Your Changes**
- Write clean, maintainable code
- Follow the existing code style
- Add appropriate comments and documentation
- Include unit tests for new functionality

### 4. **Test Your Changes**
```bash
# Run all tests
dotnet test

# Run specific tests
dotnet test --filter "TestClassName"

# Check code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### 5. **Commit Your Changes**
```bash
# Stage your changes
git add .

# Commit with a descriptive message
git commit -m "feat: add support for new AI provider"
```

**Commit Message Format:**
- `feat:` - New features
- `fix:` - Bug fixes
- `docs:` - Documentation changes
- `style:` - Code style changes
- `refactor:` - Code refactoring
- `test:` - Test additions or changes
- `chore:` - Maintenance tasks

## 📝 Code Guidelines

### **C# Coding Standards**

1. **Naming Conventions**
   - Use `PascalCase` for classes, methods, properties
   - Use `camelCase` for local variables and parameters
   - Use `UPPER_CASE` for constants
   - Prefix interfaces with `I` (e.g., `IAIProvider`)
   - Suffix DTOs with `IDto` (e.g., `SearchRequestIDto`)

2. **Code Organization**
   - One class per file
   - Group related functionality in appropriate namespaces
   - Use region blocks sparingly, prefer smaller classes

3. **Documentation**
   ```csharp
   /// <summary>
   /// Generates embeddings for the given text using the specified AI provider
   /// </summary>
   /// <param name="text">The text to generate embeddings for</param>
   /// <param name="config">AI provider configuration</param>
   /// <returns>Vector embeddings as a list of floats</returns>
   public async Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config)
   ```

4. **Error Handling**
   - Use specific exception types
   - Provide meaningful error messages
   - Log errors appropriately

### **Architecture Patterns**

- Follow **SOLID principles**
- Use **Dependency Injection** consistently
- Implement proper **separation of concerns**
- Maintain **single responsibility** for classes

## 🧪 Testing

### **Testing Strategy**

1. **Unit Tests**
   - Test individual components in isolation
   - Use mocking for dependencies
   - Aim for high code coverage (80%+)

2. **Integration Tests**
   - Test component interactions
   - Use test databases/services
   - Cover critical user workflows

3. **Example Test**
   ```csharp
   [Fact]
   public async Task UploadDocument_WithValidFile_ShouldReturnDocument()
   {
       // Arrange
       var service = GetDocumentService();
       var stream = GetTestFileStream("sample.pdf");
       
       // Act
       var result = await service.UploadDocumentAsync(stream, "test.pdf", "application/pdf", "user1");
       
       // Assert
       result.Should().NotBeNull();
       result.Chunks.Should().NotBeEmpty();
   }
   ```

### **Running Tests**

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter "Category=Integration"
```

## 📤 Submitting Changes

### **Pull Request Process**

1. **Ensure your branch is up to date**
   ```bash
   git checkout main
   git pull origin main
   git checkout your-branch
   git rebase main
   ```

2. **Push your changes**
   ```bash
   git push origin your-branch
   ```

3. **Create a Pull Request**
   - Use the PR template
   - Provide a clear description
   - Link related issues
   - Request reviews from maintainers

4. **Address Review Feedback**
   - Make requested changes
   - Push updates to the same branch
   - Respond to review comments

### **PR Requirements**

- [ ] All tests pass
- [ ] Code follows style guidelines
- [ ] Documentation is updated
- [ ] No breaking changes (unless discussed)
- [ ] Appropriate test coverage

## 🌟 Types of Contributions

### **Code Contributions**
- New AI provider implementations
- Storage backend integrations
- Performance improvements
- Bug fixes

### **Documentation**
- API documentation
- Usage examples
- Tutorials and guides
- README improvements

### **Testing**
- Unit test improvements
- Integration test coverage
- Performance benchmarks
- Test automation

### **Community**
- Answering questions in issues
- Helping other contributors
- Reporting bugs
- Suggesting improvements

## 📞 Getting Help

### **Communication Channels**
- **GitHub Issues**: For bugs and feature requests
- **GitHub Discussions**: For questions and general discussion
- **Email**: [b.yerlikaya@outlook.com](mailto:b.yerlikaya@outlook.com)

### **Resources**
- [Project README](README.md)
- [API Documentation](docs/api.md)
- [Architecture Overview](docs/architecture.md)

## 🏷️ Community Guidelines

### **Code of Conduct**
- Be respectful and inclusive
- Focus on constructive feedback
- Help create a welcoming environment
- Follow GitHub's community guidelines

### **Quality Standards**
- Write clean, readable code
- Include appropriate tests
- Update documentation
- Consider backward compatibility

## 🎉 Recognition

Contributors are recognized in:
- GitHub contributors list
- Release notes for significant contributions
- Special mentions in documentation

Thank you for contributing to SmartRAG! Your efforts help make this project better for everyone. 🚀

---

**Questions?** Feel free to reach out to [Barış Yerlikaya](mailto:b.yerlikaya@outlook.com) or open an issue for clarification.
