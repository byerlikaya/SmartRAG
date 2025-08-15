# 📦 NuGet Publishing Guide

This guide explains how to publish SmartRAG to your NuGet account ([barisyerlikaya](https://www.nuget.org/profiles/barisyerlikaya)).

## 🔑 Prerequisites

### **1. Get Your NuGet API Key**
1. Go to [nuget.org](https://www.nuget.org) and sign in
2. Click your username → **API Keys**
3. Create a new API key:
   - **Key Name**: `SmartRAG Publishing`
   - **Expiration**: 365 days (or your preference)
   - **Scopes**: ✅ Push new packages and package versions
   - **Glob Pattern**: `SmartRAG*`

### **2. Set Environment Variable (Recommended)**
```bash
# Windows (PowerShell)
$env:NUGET_API_KEY = "your-nuget-api-key-here"

# Windows (CMD)
set NUGET_API_KEY=your-nuget-api-key-here

# Linux/Mac
export NUGET_API_KEY="your-nuget-api-key-here"
```

## 🚀 Publishing Steps

### **Option 1: Manual Publishing (Recommended for first release)**

```bash
# 1. Clean and build in Release mode
dotnet clean
dotnet build --configuration Release

# 2. Create the NuGet package
dotnet pack src/SmartRAG/SmartRAG.csproj --configuration Release --output ./nupkgs

# 3. Verify the package
ls ./nupkgs/
# Should show: SmartRAG.1.0.0.nupkg and SmartRAG.1.0.0.snupkg

# 4. Publish to NuGet
dotnet nuget push ./nupkgs/SmartRAG.1.0.0.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
```

### **Option 2: Automated Publishing via GitHub Actions**

The project already includes automated publishing! To use it:

1. **Add NuGet API Key to GitHub Secrets:**
   - Go to your GitHub repository
   - Settings → Secrets and variables → Actions
   - Add new secret: `NUGET_API_KEY` = your API key

2. **Trigger automatic publishing:**
   ```bash
   # Make changes and commit with [release] tag
   git add .
   git commit -m "feat: ready for NuGet release [release]"
   git push origin main
   ```

3. **GitHub Actions will automatically:**
   - Build the project
   - Run tests
   - Create NuGet package
   - Publish to NuGet.org

## 📋 Package Information

Your SmartRAG package will appear as:

**📦 Package Details:**
- **Name**: SmartRAG
- **Author**: Barış Yerlikaya
- **Description**: It intelligently enables AI-powered question answering from your documents using Retrieval-Augmented Generation (RAG). Supports multiple AI providers and storage options.
- **Tags**: RAG, AI, LLM, OpenAI, Anthropic, Gemini, vector-search, document-processing, semantic-search
- **Target Framework**: .NET 9.0
- **License**: MIT

## 🔄 Version Management

### **Current Version Strategy:**
- **1.0.0** - Initial release
- **1.0.x** - Bug fixes and minor improvements
- **1.x.0** - New features (backward compatible)
- **2.0.0** - Breaking changes

### **Updating Versions:**
```xml
<!-- In src/SmartRAG/SmartRAG.csproj -->
<PackageVersion>1.0.1</PackageVersion>
<PackageReleaseNotes>Bug fixes and performance improvements</PackageReleaseNotes>
```

## 📊 Your NuGet Portfolio

SmartRAG will join your successful package collection:

| Package | Downloads | Latest Version |
|---------|-----------|----------------|
| **SmartWhere** | 6,085 | 2.2.2.1 |
| **AmazonWebServices** | 4,856 | 2.1.4.1 |
| **SmartOrderBy** | 4,270 | 1.2.0.1 |
| **Basic.RabbitMQ** | 3,680 | 2.0.1 |
| **EntityGuardian** | 3,239 | 3.0.0 |
| **SqlBackupToS3** | 2,532 | 2.1.1.2 |
| **SmartRAG** | 🚀 New! | 1.0.0 |

**Total Portfolio Downloads: 24,656+ (and growing!)**

## ✅ Pre-Publishing Checklist

Before publishing, ensure:

- [ ] ✅ Build succeeds: `dotnet build --configuration Release`
- [ ] ✅ All tests pass: `dotnet test`
- [ ] ✅ Package builds: `dotnet pack --configuration Release`
- [ ] ✅ README.md is comprehensive and accurate
- [ ] ✅ No API keys or secrets in repository
- [ ] ✅ Version number is correct
- [ ] ✅ Release notes are updated

## 🔍 Post-Publishing Verification

After publishing:

1. **Check NuGet.org:** https://www.nuget.org/packages/SmartRAG
2. **Test installation:**
   ```bash
   # Create a test project and install
   dotnet new console -n SmartRAGTest
   cd SmartRAGTest
   dotnet add package SmartRAG
   dotnet build
   ```

3. **Update documentation:** Add NuGet badges to README.md

## 🆘 Troubleshooting

### **Common Issues:**

**❌ Package already exists**
```
Error: Response status code does not indicate success: 409 (Conflict)
```
**✅ Solution:** Increment version number in `.csproj`

**❌ Authentication failed**
```
Error: Response status code does not indicate success: 403 (Forbidden)
```
**✅ Solution:** Check your API key and permissions

**❌ Package too large**
```
Error: The package exceeds the maximum allowed size
```
**✅ Solution:** Our package is optimized and should be well under limits

## 🎉 Success!

Once published, SmartRAG will be available to the global .NET community:

```bash
# Anyone can install it with:
dotnet add package SmartRAG
```

Your contribution to the .NET ecosystem continues to grow! 🚀

---

**📞 Need Help?** Contact: [b.yerlikaya@outlook.com](mailto:b.yerlikaya@outlook.com)
