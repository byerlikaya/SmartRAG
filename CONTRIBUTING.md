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

- **.NET SDK** (version 6.0 or later for building examples and library)
- **Git**
- **Visual Studio 2022**, **VS Code**, or **JetBrains Rider**
- Basic knowledge of **C#** and **RAG (Retrieval-Augmented Generation)**

**Note**: The SmartRAG library (`src/SmartRAG/`) targets **.NET 6** for wide compatibility. Example projects may target newer .NET versions.

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

### 4. **Commit Your Changes**
```bash
# Stage your changes
git add .

# Commit with a descriptive message (ALWAYS in English)
git commit -m "feat: add support for new AI provider"
```

**Commit Message Format:**
- **MUST be in English** - Never use Turkish or other languages
- Format: `<type>[optional scope]: <description>`
- Types:
  - `feat:` - New features
  - `fix:` - Bug fixes
  - `docs:` - Documentation changes
  - `style:` - Code style changes
  - `refactor:` - Code refactoring
  - `perf:` - Performance improvements
  - `test:` - Test additions or changes
  - `build:` - Build system changes
  - `ci:` - CI/CD configuration
  - `chore:` - Maintenance tasks
  - `revert:` - Revert a previous commit

**Categorization Rule (Optional):**
If you have changes in multiple categories, you may commit them separately for better organization:
```bash
# 1. Documentation
git add docs/en/*.md docs/tr/*.md
git commit -m "docs: update API documentation"

# 2. Code changes
git add src/SmartRAG/**/*.cs
git commit -m "feat: add new feature"
```

**⚠️ Release Tagging:**
- **NEVER** add `[release]` tag unless explicitly instructed
- `[release]` tag triggers automatic NuGet package publishing
- Format: `[release] feat: add feature v3.2.0`

## 📝 Code Guidelines

### **C# Coding Standards**

1. **Naming Conventions**
   - Use `PascalCase` for classes, methods, properties
   - Use `camelCase` for local variables and parameters
   - Use `UPPER_CASE` for constants
   - Prefix interfaces with `I` (e.g., `IAIProvider`)

2. **Constructors**
   - Primary constructors (C# 12+) and standard constructors are both allowed. Use consistently within the codebase.

3. **Logging**
   - **ALWAYS** use `ILogger<T>` for logging
   - **NEVER** use `Console.WriteLine`
   - Use structured logging with message templates

4. **Documentation**
   - Use XML documentation for all public members
   - Avoid unnecessary comments that restate the obvious

5. **Error Handling**
   - Use specific exception types
   - Provide meaningful error messages
   - Log errors appropriately with `ILogger<T>`

6. **Language Requirements**
   - **ALL code must be in English** (variable names, comments, documentation)
   - No Turkish or other language words in code

7. **Build Requirements**
   - **MUST** build with 0 errors, 0 warnings, 0 messages
   - Always run `dotnet clean` before `dotnet build`
   - Fix all warnings before committing

**For detailed coding standards**, see [Code Standards](.cursor/rules/03-KOD-STANDARTLARI.mdc)

### **Architecture Patterns**

- Follow **SOLID principles** and **DRY principle**
- Use **Dependency Injection** consistently
- Implement proper **separation of concerns**

### **Generic Code Requirements**

**CRITICAL**: SmartRAG is a generic library - never write domain-specific code:
- ❌ No hardcoded table names (e.g., "Products", "Orders", "Customers")
- ❌ No hardcoded database names (e.g., "ProductCatalog", "SalesManagement")
- ❌ No domain-specific scenarios (e.g., e-commerce, inventory management)
- ✅ Use generic placeholders: "TableA", "ColumnX", "Database1"
- ✅ Use schema-based logic that works for any database structure
- ✅ Use interfaces for provider-agnostic code

## 🧪 Example Projects Verification

While there are no mandatory unit tests for the library, **example projects must be verified** before submitting changes.

### **SmartRAG.Demo Verification**

Before submitting changes that affect the demo project:

1. **Build Verification**
   ```bash
   cd examples/SmartRAG.Demo
   dotnet clean
   dotnet build
   ```
   - Must build with 0 errors, 0 warnings

2. **Runtime Verification**
   - Run the demo application
   - Verify initialization menu works
   - Test at least one query scenario
   - Verify test query generation works (if modified)

3. **Configuration Verification**
   - Check `appsettings.json` and `appsettings.Development.json`
   - Verify all required settings are present
   - Test with different storage providers (Qdrant, Redis, InMemory)

### **SmartRAG.API Verification**

Before submitting changes that affect the API project:

1. **Build Verification**
   ```bash
   cd examples/SmartRAG.API
   dotnet clean
   dotnet build
   ```
   - Must build with 0 errors, 0 warnings

2. **API Verification**
   - Start the API application
   - Verify Swagger UI loads correctly (`/swagger`)
   - Test at least one endpoint manually
   - Verify file upload functionality (if modified)
   - Check CORS configuration works

3. **Configuration Verification**
   - Check `appsettings.json` and `appsettings.Development.json`
   - Verify SmartRAG services are properly configured
   - Test with different AI and storage providers

### **What to Verify**

- ✅ Application starts without errors
- ✅ Configuration files are valid
- ✅ Key features work as expected
- ✅ No runtime exceptions in console/logs
- ✅ API endpoints respond correctly (for API project)

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

- [ ] Build successful (0 errors, 0 warnings, 0 messages)
- [ ] Code follows style guidelines
- [ ] Code is generic and provider-agnostic (no domain-specific code)
- [ ] All code is in English (no Turkish or other languages)
- [ ] Documentation is updated (both EN and TR if applicable)
- [ ] No breaking changes (unless discussed)
- [ ] Commit messages are in English
- [ ] No `[release]` tag (unless explicitly approved)
- [ ] Example projects verified (if changes affect them)

## 🌟 Types of Contributions

### **Code Contributions**
- New AI provider implementations
- Storage backend integrations
- Performance improvements
- Bug fixes

**Important Notes:**
- All code changes must be in `src/SmartRAG/` directory
- Changes in `examples/` projects are not included in NuGet package
- Changelog entries should only reference `src/SmartRAG/` changes

### **Documentation**
- API documentation
- Usage examples
- Tutorials and guides
- README improvements

### **Testing** (Optional)
- Test improvements for example projects
- Performance benchmarks

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
- [Documentation Site](https://byerlikaya.github.io/SmartRAG/en/)
- [Project Rules](.cursor/rules/00-ANA-INDEKS.mdc) - Complete project rules and guidelines
- [Code Standards](.cursor/rules/03-KOD-STANDARTLARI.mdc) - Detailed C# coding standards
- [Git Commit Rules](.cursor/rules/02-GIT-COMMIT-RULES.mdc) - Commit message guidelines

## 🏷️ Community Guidelines

### **Code of Conduct**
- Be respectful and inclusive
- Focus on constructive feedback
- Help create a welcoming environment
- Follow GitHub's community guidelines

### **Quality Standards**
- Write clean, readable code
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
