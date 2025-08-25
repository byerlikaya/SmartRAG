---
layout: default
title: Contributing
description: How to contribute to SmartRAG - Guidelines, setup, and development workflow
---

# Contributing to SmartRAG

Thank you for your interest in contributing to SmartRAG! We welcome contributions from developers of all skill levels. This guide will help you get started.

## 🚀 How to Contribute

### **Types of Contributions We Welcome:**

- 🐛 **Bug Reports** - Help us identify and fix issues
- ✨ **Feature Requests** - Suggest new features and improvements
- 📚 **Documentation** - Improve guides, examples, and API docs
- 🔧 **Code Contributions** - Submit pull requests with fixes or features
- 🧪 **Testing** - Help test new features and report issues
- 🌍 **Localization** - Translate documentation to other languages

## 📋 Before You Start

### **Prerequisites:**
- .NET 9.0 SDK or later
- Git
- Visual Studio 2022, VS Code, or JetBrains Rider
- Basic understanding of C# and .NET

### **Development Environment Setup:**
```bash
# Clone the repository
git clone https://github.com/byerlikaya/SmartRAG.git
cd SmartRAG

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

## 🔧 Development Workflow

### **1. Fork and Clone**
1. Fork the [SmartRAG repository](https://github.com/byerlikaya/SmartRAG)
2. Clone your fork locally
3. Add the upstream remote:
   ```bash
   git remote add upstream https://github.com/byerlikaya/SmartRAG.git
   ```

### **2. Create a Feature Branch**
```bash
# Create and switch to a new branch
git checkout -b feature/your-feature-name

# Or for bug fixes
git checkout -b fix/issue-description
```

### **3. Make Your Changes**
- Follow the existing code style and conventions
- Add appropriate tests for new functionality
- Update documentation if needed
- Ensure all tests pass

### **4. Commit Your Changes**
```bash
# Use conventional commit messages
git commit -m "feat: add new document processing feature"
git commit -m "fix: resolve memory leak in document service"
git commit -m "docs: update API documentation examples"
```

### **5. Push and Create Pull Request**
```bash
git push origin feature/your-feature-name
```

Then create a Pull Request on GitHub with:
- Clear description of changes
- Reference to any related issues
- Screenshots if UI changes
- Test results

## 📝 Code Style Guidelines

### **C# Coding Standards:**
- Use **PascalCase** for public members
- Use **camelCase** for private fields and parameters
- Use **UPPER_CASE** for constants
- Prefer **readonly** over **const** when possible
- Use **var** for local variables when type is obvious

### **Example:**
```csharp
public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _repository;
    private readonly ILogger<DocumentService> _logger;
    
    public DocumentService(
        IDocumentRepository repository,
        ILogger<DocumentService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<Document> ProcessDocumentAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            
        // Your implementation here
    }
}
```

### **Documentation Standards:**
- Use clear, concise language
- Include code examples
- Add XML documentation for public APIs
- Keep examples up-to-date with code changes

## 🧪 Testing Guidelines

### **Unit Tests:**
- Test all public methods
- Use descriptive test names
- Follow AAA pattern (Arrange, Act, Assert)
- Mock external dependencies

### **Example Test:**
```csharp
[Fact]
public async Task ProcessDocument_ValidFilePath_ReturnsDocument()
{
    // Arrange
    var mockRepository = new Mock<IDocumentRepository>();
    var service = new DocumentService(mockRepository.Object, Mock.Of<ILogger<DocumentService>>());
    var filePath = "test-document.pdf";
    
    // Act
    var result = await service.ProcessDocumentAsync(filePath);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(filePath, result.FilePath);
}
```

### **Integration Tests:**
- Test complete workflows
- Use test databases/containers
- Clean up test data after each test

## 📚 Documentation Contributions

### **What to Document:**
- New features and APIs
- Configuration options
- Troubleshooting guides
- Code examples
- Performance tips

### **Documentation Structure:**
```
docs/
├── getting-started.md      # Quick start guide
├── configuration.md        # Configuration options
├── api-reference.md        # API documentation
├── examples.md             # Usage examples
├── troubleshooting.md      # Common issues
└── contributing.md         # This file
```

### **Markdown Guidelines:**
- Use clear headings and subheadings
- Include code blocks with syntax highlighting
- Add links to related sections
- Use emojis sparingly for visual appeal

## 🐛 Reporting Issues

### **Bug Report Template:**
```
**Description:**
Brief description of the issue

**Steps to Reproduce:**
1. Step 1
2. Step 2
3. Step 3

**Expected Behavior:**
What should happen

**Actual Behavior:**
What actually happens

**Environment:**
- OS: Windows 11 / macOS / Linux
- .NET Version: 9.0.x
- SmartRAG Version: 1.0.3

**Additional Information:**
Screenshots, logs, or other relevant details
```

### **Feature Request Template:**
```
**Description:**
Clear description of the feature you'd like to see

**Use Case:**
How would this feature help you or others?

**Proposed Solution:**
Your ideas for implementation (optional)

**Alternatives Considered:**
Other approaches you've considered (optional)
```

## 🔄 Pull Request Process

### **Before Submitting:**
- [ ] Code follows style guidelines
- [ ] Tests pass locally
- [ ] Documentation is updated
- [ ] Commit messages are clear
- [ ] Branch is up-to-date with main

### **Review Process:**
1. **Automated Checks** - CI/CD pipeline runs tests
2. **Code Review** - Maintainers review your code
3. **Feedback** - Address any review comments
4. **Merge** - Once approved, your PR is merged

### **Common Review Comments:**
- "Please add tests for this functionality"
- "Consider adding error handling for edge cases"
- "This could be simplified using LINQ"
- "Please update the documentation"

## 🏷️ Release Process

### **Versioning:**
We use [Semantic Versioning](https://semver.org/):
- **MAJOR** - Breaking changes
- **MINOR** - New features, backward compatible
- **PATCH** - Bug fixes, backward compatible

### **Release Schedule:**
- **Patch releases** - As needed for critical fixes
- **Minor releases** - Monthly for new features
- **Major releases** - Quarterly for breaking changes

## 🎯 Areas That Need Help

### **High Priority:**
- Performance optimization
- Additional storage providers
- Enhanced error handling
- Comprehensive test coverage

### **Medium Priority:**
- Additional document formats
- More AI provider integrations
- Advanced search algorithms
- Monitoring and metrics

### **Low Priority:**
- UI improvements
- Additional examples
- Documentation translations
- Community tools

## 🤝 Community Guidelines

### **Be Respectful:**
- Treat all contributors with respect
- Welcome newcomers and help them learn
- Provide constructive feedback
- Be patient with questions

### **Communication:**
- Use clear, professional language
- Ask questions when unsure
- Share knowledge and help others
- Report inappropriate behavior

## 📞 Getting Help

### **Need Assistance?**
- 📖 **Documentation** - Check our guides first
- 🐛 **Issues** - Search existing issues
- 💬 **Discussions** - Ask questions in GitHub Discussions
- 📧 **Email** - Contact us at b.yerlikaya@outlook.com

### **Resources:**
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [C# Programming Guide](https://docs.microsoft.com/dotnet/csharp/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core/)

## 🎉 Recognition

### **Contributor Hall of Fame:**
We recognize all contributors in our:
- README.md file
- Release notes
- Documentation
- Community announcements

### **Special Recognition:**
- **First Contribution** - Welcome badge
- **Major Features** - Feature credit
- **Long-term Support** - Maintainer status

## 📄 License

By contributing to SmartRAG, you agree that your contributions will be licensed under the [MIT License]({{ site.baseurl }}/license).

---

## 🚀 Ready to Contribute?

1. **Start Small** - Begin with documentation or small bug fixes
2. **Ask Questions** - Don't hesitate to ask for help
3. **Be Patient** - Learning takes time, we're here to help
4. **Have Fun** - Open source should be enjoyable!

**Thank you for contributing to SmartRAG!** 🎉

---

<div class="text-center mt-5">
    <p class="text-muted">
        <i class="fas fa-heart text-danger"></i> Built with love by the SmartRAG community
    </p>
</div>
