# 🔧 GitHub Repository Setup Guide

After pushing SmartRAG to GitHub, you need to configure some settings for the automated workflows to work properly.

## 🔑 Required Secrets

### **1. NuGet API Key (REQUIRED for auto-publishing)**

1. **Get your NuGet API Key:**
   - Go to [nuget.org](https://www.nuget.org)
   - Sign in with your account
   - Click your username → **API Keys**
   - Create a new API key:
     - **Key Name**: `SmartRAG GitHub Actions`
     - **Expiration**: 365 days (recommended)
     - **Scopes**: ✅ Push new packages and package versions
     - **Glob Pattern**: `SmartRAG*`
   - **Copy the generated API key** (you won't see it again!)

2. **Add to GitHub Secrets:**
   - Go to your GitHub repository: `https://github.com/byerlikaya/SmartRAG`
   - Navigate to **Settings** → **Secrets and variables** → **Actions**
   - Click **New repository secret**
   - **Name**: `NUGET_API_KEY`
   - **Secret**: Paste your NuGet API key
   - Click **Add secret**

### **2. CodeCov Token (OPTIONAL - for coverage reports)**

1. **Get CodeCov token:**
   - Go to [codecov.io](https://codecov.io)
   - Sign in with GitHub
   - Add your `SmartRAG` repository
   - Copy the repository token

2. **Add to GitHub Secrets:**
   - **Name**: `CODECOV_TOKEN`
   - **Secret**: Paste your CodeCov token

## 🛡️ Required Permissions

### **1. Actions Permissions**
- Go to **Settings** → **Actions** → **General**
- **Actions permissions**: Allow all actions and reusable workflows
- **Workflow permissions**: Read and write permissions
- **Allow GitHub Actions to create and approve pull requests**: ✅ Check this

### **2. Security Permissions (for CodeQL)**
- Go to **Settings** → **Security and analysis**
- **Code scanning**: Enable
- **Secret scanning**: Enable (recommended)
- **Dependency review**: Enable (recommended)

## 🚀 How the Workflows Work

### **CI/CD Pipeline (`ci.yml`)**

**🔄 Triggers:**
- Every push to `main` or `develop` branch
- Every pull request to `main` branch

**📊 What happens:**

1. **Test Job** (Always runs):
   ```bash
   ✅ Checkout code
   ✅ Setup .NET 9.0
   ✅ Restore dependencies
   ✅ Build project
   ✅ Run tests
   ✅ Generate coverage report
   ```

2. **Build Job** (Only on `main` branch):
   ```bash
   ✅ Create NuGet package
   ✅ Save as artifact
   ```

3. **Publish Job** (Only with `[release]` in commit message):
   ```bash
   ✅ Download NuGet package
   ✅ Publish to NuGet.org automatically
   ```

**🎯 Usage Examples:**

```bash
# Regular development - runs tests only
git commit -m "feat: add new AI provider"
git push origin main

# Create package but don't publish - runs tests + build
git commit -m "feat: ready for testing"
git push origin main

# Auto-publish to NuGet - runs tests + build + publish
git commit -m "feat: version 1.1.0 ready [release]"
git push origin main
```

### **Security Analysis (`codeql-analysis.yml`)**

**🔄 Triggers:**
- Every push to `main` or `develop` branch
- Every pull request to `main` branch
- Every Monday at 2:30 AM (weekly security scan)

**🛡️ What happens:**
```bash
✅ Scan C# code for security vulnerabilities
✅ Check for SQL injection, XSS, memory leaks
✅ Generate security report in GitHub Security tab
✅ Alert on high-severity issues
```

## 📊 Monitoring Your Workflows

### **1. View Workflow Runs**
- Go to your repository
- Click **Actions** tab
- See all workflow runs, success/failure status

### **2. View Security Results**
- Go to **Security** tab
- Click **Code scanning** to see CodeQL results
- Review any security alerts

### **3. View NuGet Packages**
- Successful publishes will appear on [nuget.org/packages/SmartRAG](https://www.nuget.org/packages/SmartRAG)
- Check your [profile](https://www.nuget.org/profiles/barisyerlikaya) for package stats

## 🎯 Workflow Status Badges

Add these to your README.md to show build status:

```markdown
[![Build Status](https://github.com/byerlikaya/SmartRAG/workflows/CI%2FCD%20Pipeline/badge.svg)](https://github.com/byerlikaya/SmartRAG/actions)
[![CodeQL](https://github.com/byerlikaya/SmartRAG/workflows/CodeQL%20Analysis/badge.svg)](https://github.com/byerlikaya/SmartRAG/actions)
[![NuGet](https://img.shields.io/nuget/v/SmartRAG.svg)](https://www.nuget.org/packages/SmartRAG)
```

## 🔧 Customizing Workflows

### **Change .NET Version:**
```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '9.0.x'  # Change this if needed
```

### **Add More Test Environments:**
```yaml
strategy:
  matrix:
    os: [ubuntu-latest, windows-latest, macos-latest]
    dotnet-version: ['8.0.x', '9.0.x']
```

### **Change Release Trigger:**
```yaml
# Instead of [release] in commit message, use tags:
if: startsWith(github.ref, 'refs/tags/v')
```

## ✅ Setup Checklist

After pushing to GitHub, verify:

- [ ] ✅ Repository created: `https://github.com/byerlikaya/SmartRAG`
- [ ] ✅ Actions enabled in repository settings
- [ ] ✅ `NUGET_API_KEY` secret added
- [ ] ✅ First workflow run completed successfully
- [ ] ✅ CodeQL analysis completed
- [ ] ✅ Security tab shows no critical issues

## 🆘 Troubleshooting

### **Common Issues:**

**❌ Workflow fails on first run**
- Check if secrets are properly configured
- Verify repository permissions
- Review workflow logs in Actions tab

**❌ NuGet publish fails**
- Verify `NUGET_API_KEY` is correct
- Check if package version already exists
- Ensure commit message contains `[release]`

**❌ CodeQL analysis fails**
- Usually resolves automatically on retry
- Check if project builds successfully locally

**❌ Tests fail in GitHub but pass locally**
- Check for hardcoded paths
- Verify all test data files are committed
- Review environment differences

## 📞 Getting Help

If you encounter issues:
1. Check the **Actions** tab for detailed logs
2. Review the **Issues** tab for known problems
3. Contact: [b.yerlikaya@outlook.com](mailto:b.yerlikaya@outlook.com)

---

**🎉 Once configured, your repository will have enterprise-grade CI/CD automation!**
