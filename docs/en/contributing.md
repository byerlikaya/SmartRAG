---
layout: default
title: Contributing
description: Learn how to contribute to SmartRAG development
lang: en
---

# Contributing

Thank you for your interest in contributing to SmartRAG! We welcome contributions from the community.

## How to Contribute

### Reporting Issues

Before creating a new issue, please:

1. **Search existing issues** to see if your problem has already been reported
2. **Check the documentation** to see if there's a solution already available
3. **Provide detailed information** when reporting issues

#### Issue Template

When creating an issue, please use this template:

```markdown
## Description
Brief description of the issue

## Steps to Reproduce
1. Step 1
2. Step 2
3. Step 3

## Expected Behavior
What you expected to happen

## Actual Behavior
What actually happened

## Environment
- .NET Version: [e.g., .NET 8.0]
- OS: [e.g., Windows 11, macOS 14, Ubuntu 22.04]
- SmartRAG Version: [e.g., 1.1.0]

## Additional Information
Any other context, logs, or screenshots
```

### Suggesting Features

We welcome feature suggestions! When suggesting a feature:

1. **Describe the problem** you're trying to solve
2. **Explain your proposed solution**
3. **Provide use cases** where this feature would be helpful
4. **Consider alternatives** and explain why your approach is better

### Code Contributions

#### Prerequisites

- .NET 8.0 SDK or later
- Git
- A GitHub account
- Basic knowledge of C# and .NET

#### Development Setup

1. **Fork the repository**
   ```bash
   git clone https://github.com/YOUR_USERNAME/SmartRAG.git
   cd SmartRAG
   ```

2. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make your changes**
   - Follow the coding standards below
   - Add tests for new functionality
   - Update documentation if needed

4. **Test your changes**
   ```bash
   dotnet build
   dotnet test
   ```

5. **Commit your changes**
   ```bash
   git add .
   git commit -m "feat: add your feature description"
   ```

6. **Push to your fork**
   ```bash
   git push origin feature/your-feature-name
   ```

7. **Create a pull request**

#### Pull Request Guidelines

- **Title**: Use conventional commit format (e.g., "feat: add new AI provider")
- **Description**: Clearly describe what the PR does and why
- **Tests**: Ensure all tests pass
- **Documentation**: Update relevant documentation
- **Breaking Changes**: Clearly mark any breaking changes

### Conventional Commits

We use [Conventional Commits](https://www.conventionalcommits.org/) for commit messages:

- `feat:` New features
- `fix:` Bug fixes
- `docs:` Documentation changes
- `style:` Code style changes (formatting, etc.)
- `refactor:` Code refactoring
- `test:` Adding or updating tests
- `chore:` Maintenance tasks

Examples:
```bash
git commit -m "feat: add support for Azure OpenAI provider"
git commit -m "fix: resolve memory leak in document processing"
git commit -m "docs: update configuration examples"
```

## Development Guidelines

### Code Standards

#### C# Coding Standards

- Use **primary constructors** for all classes
- Follow **SOLID principles** and **DRY**
- Use **#region** organization:
  ```csharp
  #region Constants
  #endregion
  #region Fields
  #endregion
  #region Properties
  #endregion
  #region Public Methods
  #endregion
  #region Private Methods
  #endregion
  ```

- Replace **magic numbers** with constants
- Use **LoggerMessage delegates** for all logging
- Extract **helper methods** for complex logic
- Implement **IDisposable pattern** where needed
- Ensure **thread safety** for shared resources

#### Example Class Structure

```csharp
public class ExampleService(ILogger<ExampleService> logger) : IExampleService
{
    #region Constants
    private const int MaxRetryAttempts = 3;
    private const int TimeoutSeconds = 30;
    #endregion

    #region Fields
    private readonly ILogger<ExampleService> _logger = logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    #endregion

    #region Properties
    public string ServiceName { get; } = "ExampleService";
    #endregion

    #region Public Methods
    public async Task<Result> ProcessDataAsync(string data)
    {
        try
        {
            await _semaphore.WaitAsync();
            return await ProcessDataInternalAsync(data);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    #endregion

    #region Private Methods
    private async Task<Result> ProcessDataInternalAsync(string data)
    {
        _logger.LogInformation("Processing data: {DataLength}", data.Length);
        // Implementation here
        return Result.Success();
    }
    #endregion
}
```

### Testing Standards

#### Test Structure

- Use **xUnit** as the testing framework
- Use **Moq** for mocking
- Follow **AAA pattern** (Arrange, Act, Assert)
- Use **descriptive test names**
- Test both **success and failure scenarios**

#### Example Test

```csharp
[Fact]
public async Task ProcessDataAsync_ValidData_ReturnsSuccess()
{
    // Arrange
    var logger = Mock.Of<ILogger<ExampleService>>();
    var service = new ExampleService(logger);
    var testData = "test data";

    // Act
    var result = await service.ProcessDataAsync(testData);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Null(result.Error);
}
```

### Documentation Standards

- Write **clear and concise** documentation
- Include **code examples** for all features
- Provide **step-by-step guides** for complex operations
- Keep documentation **up-to-date** with code changes
- Use **consistent formatting** and structure

## Project Structure

```
SmartRAG/
├── src/
│   └── SmartRAG/           # Core library
│       ├── Entities/        # Domain entities
│       ├── Interfaces/      # Service contracts
│       ├── Services/        # Business logic
│       ├── Providers/       # AI and storage providers
│       ├── Repositories/    # Data access
│       ├── Extensions/      # Extension methods
│       └── Models/          # Configuration models
├── examples/
│   └── WebAPI/             # Example web application
├── tests/
│   └── SmartRAG.Tests/     # Unit tests
└── docs/                    # Documentation
```

## Getting Help

### Development Questions

- **GitHub Discussions**: Use the Discussions tab for questions
- **GitHub Issues**: For bugs and feature requests
- **Email**: [b.yerlikaya@outlook.com](mailto:b.yerlikaya@outlook.com)

### Code Review Process

1. **Automated Checks**: All PRs must pass CI/CD checks
2. **Code Review**: At least one maintainer must approve
3. **Testing**: All tests must pass
4. **Documentation**: Documentation must be updated if needed

## Recognition

Contributors will be recognized in:

- **README.md** file
- **Release notes**
- **GitHub contributors page**
- **Project documentation**

## License

By contributing to SmartRAG, you agree that your contributions will be licensed under the same MIT License that covers the project.

## Need Help?

If you need assistance with contributing:

- [Back to Documentation]({{ site.baseurl }}/en/) - Main documentation
- [Open an issue](https://github.com/byerlikaya/SmartRAG/issues) - GitHub Issues
- [Contact support](mailto:b.yerlikaya@outlook.com) - Email support
