# 🚀 SmartRAG Pull Request

## 📝 Description
Brief description of changes made in this PR.

## 🔗 Related Issue
Fixes #(issue number)

## 🔄 Type of Change
Please mark the relevant options:

- [ ] 🐛 Bug fix (non-breaking change which fixes an issue)
- [ ] ✨ New feature (non-breaking change which adds functionality)
- [ ] 💥 Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] 📚 Documentation update
- [ ] 🧪 Test improvements
- [ ] 🔧 Code refactoring
- [ ] 🚀 Release preparation

## 🧪 Testing
Please describe the tests that you ran to verify your changes:

- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual testing completed
- [ ] SmartRAG.Demo runs without errors
- [ ] Build succeeds with 0 errors, 0 warnings

## 📋 Checklist
- [ ] My code follows the SmartRAG style guidelines
- [ ] I have performed a self-review of my own code
- [ ] I have commented my code, particularly in hard-to-understand areas
- [ ] I have made corresponding changes to the documentation (EN + TR)
- [ ] My changes generate no new warnings
- [ ] I have added tests that prove my fix is effective or that my feature works
- [ ] New and existing unit tests pass locally with my changes
- [ ] LoggerMessage definitions are correct (no parameter mismatches)
- [ ] EventId assignments are unique (no conflicts)
- [ ] Code is generic and provider-agnostic (no hardcoded domain-specific names)
- [ ] All public APIs have XML documentation

## 🚨 Critical Rules Compliance
- [ ] **Generic Code**: No hardcoded table/database/column names
- [ ] **Error Fixing**: Only fixed the reported error, no refactoring
- [ ] **Build Quality**: 0 errors, 0 warnings, 0 messages
- [ ] **Language**: All code elements in English only
- [ ] **SOLID/DRY**: Principles followed

## 📸 Screenshots (if applicable)
Add screenshots here to help explain your changes.

## 📋 Additional Context
Add any other context about the pull request here.

## 🔄 Migration Guide (if breaking changes)
If this PR contains breaking changes, provide migration instructions:

```csharp
// Before (vX.Y.Z)
// Code example

// After (vX.Y.Z)
// Code example
```

## 📊 Performance Impact
- [ ] No performance impact
- [ ] Performance improvement
- [ ] Performance regression (explain below)

## 🔒 Security Considerations
- [ ] No security implications
- [ ] Security improvement
- [ ] Security concern (explain below)

---

**Reviewer Guidelines:**
- Check for generic, provider-agnostic code
- Verify LoggerMessage parameter counts match format strings
- Ensure EventId assignments are unique
- Confirm 0 errors, 0 warnings build
- Validate documentation updates (EN + TR)