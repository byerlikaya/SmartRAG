---
layout: default
title: Troubleshooting
description: Common issues, solutions, and debugging guide for SmartRAG
nav_order: 6
---

# SmartRAG Troubleshooting Guide

This guide helps you resolve common issues and errors when using SmartRAG. If you encounter a problem not covered here, please [create an issue](https://github.com/byerlikaya/SmartRAG/issues) or [contact support](mailto:b.yerlikaya@examples.com).

## 🚨 Common Error Messages & Solutions

### **AI Provider Errors**

#### **"TooManyRequests" Error (Anthropic)**
```
Error: Anthropic error: TooManyRequests
```
**Cause:** Rate limit exceeded for Anthropic API
**Solutions:**
1. **Wait and retry** - Anthropic has rate limits per minute
2. **Reduce concurrent requests** - Process documents in smaller batches
3. **Use batch processing** - SmartRAG automatically handles this
4. **Switch to fallback provider** - Configure multiple AI providers

**Configuration:**
```json
{
  "AI": {
    "Anthropic": {
      "ApiKey": "your-key",
      "Model": "claude-3.5-sonnet",
      "MaxRetries": 3,
      "RetryDelayMs": 2000
    },
    "OpenAI": {
      "ApiKey": "your-fallback-key",
      "Model": "gpt-4"
    }
  }
}
```

#### **"Unauthorized" Error (OpenAI)**
```
Error: OpenAI error: Unauthorized
```
**Cause:** Invalid or expired API key
**Solutions:**
1. **Check API key** - Verify it's correct and active
2. **Check billing** - Ensure account has credits
3. **Check permissions** - Verify API key has correct scopes
4. **Regenerate key** - Create new API key if needed

#### **"BadRequest" Error (Gemini)**
```
Error: Gemini error: BadRequest
```
**Cause:** Invalid request format or parameters
**Solutions:**
1. **Check model name** - Use correct Gemini model identifier
2. **Verify API key** - Ensure Google AI Studio key is valid
3. **Check request size** - Reduce input length if too large
4. **Update configuration** - Use latest model versions

### **Storage Provider Errors**

#### **Redis Connection Error**
```
Error: Redis connection failed: Connection refused
```
**Cause:** Redis server not running or wrong connection details
**Solutions:**
1. **Start Redis server** - `redis-server` or Docker container
2. **Check connection string** - Verify host, port, password
3. **Check firewall** - Ensure port 6379 is accessible
4. **Use Docker** - `docker run -d -p 6379:6379 redis:alpine`

**Configuration:**
```json
{
  "Storage": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Password": "",
      "Database": 0,
      "ConnectionTimeout": 30
    }
  }
}
```

#### **Qdrant Connection Error**
```
Error: Qdrant connection failed: Unable to connect
```
**Cause:** Qdrant server not accessible
**Solutions:**
1. **Start Qdrant** - `docker run -d -p 6333:6333 qdrant/qdrant`
2. **Check host/port** - Verify connection details
3. **Check SSL settings** - UseHttps: false for local development
4. **Verify collection** - Ensure collection exists

#### **SQLite Database Error**
```
Error: SQLite database locked
```
**Cause:** Multiple processes accessing database
**Solutions:**
1. **Close other connections** - Stop other applications
2. **Use connection pooling** - Configure proper connection limits
3. **Check file permissions** - Ensure write access to database file
4. **Use WAL mode** - Enable Write-Ahead Logging

### **Document Processing Errors**

#### **"Unsupported file format" Error**
```
Error: Unsupported file format: .xyz
```
**Cause:** File type not supported by SmartRAG
**Solutions:**
1. **Check file extension** - Use supported formats: PDF, DOCX, TXT, MD
2. **Convert file** - Use online converters for unsupported formats
3. **Check MIME type** - Ensure Content-Type header is correct
4. **Verify file integrity** - File might be corrupted

**Supported Formats:**
- **PDF** (.pdf) - Text extraction with iText7
- **Word** (.docx, .doc) - OpenXML processing
- **Text** (.txt, .md, .json, .xml, .csv, .html)
- **Plain Text** - UTF-8 encoding with BOM detection

#### **"Document too large" Error**
```
Error: Document size exceeds maximum limit
```
**Cause:** File size exceeds configured limits
**Solutions:**
1. **Increase limits** - Configure larger MaxDocumentSize
2. **Split document** - Break into smaller files
3. **Compress file** - Reduce file size before upload
4. **Use chunking** - SmartRAG automatically chunks large documents

**Configuration:**
```json
{
  "SmartRAG": {
    "MaxDocumentSizeMB": 100,
    "MaxChunkSize": 1000,
    "MinChunkSize": 100
  }
}
```

#### **"Text extraction failed" Error**
```
Error: Failed to extract text from document
```
**Cause:** Document parsing issues
**Solutions:**
1. **Check file corruption** - Try opening in original application
2. **Verify file format** - Ensure it's a valid document
3. **Check permissions** - Ensure read access to file
4. **Try different format** - Convert to simpler format (e.g., TXT)

### **Search & RAG Errors**

#### **"No relevant sources found" Error**
```
Error: No relevant sources found for query
```
**Cause:** Query doesn't match document content
**Solutions:**
1. **Refine query** - Use more specific keywords
2. **Check document content** - Ensure documents contain relevant information
3. **Adjust similarity threshold** - Lower threshold for more results
4. **Use keyword search** - Fallback to text-based search

#### **"Embedding generation failed" Error**
```
Error: Failed to generate embeddings
```
**Cause:** AI provider issues or rate limits
**Solutions:**
1. **Check API keys** - Verify all AI provider configurations
2. **Check rate limits** - Wait and retry
3. **Use fallback providers** - Configure multiple AI services
4. **Reduce batch size** - Process fewer documents at once

#### **"Query intent detection failed" Error**
```
Error: Unable to determine query intent
```
**Cause:** Query format issues
**Solutions:**
1. **Check query format** - Ensure proper sentence structure
2. **Use clear language** - Avoid ambiguous phrasing
3. **Check encoding** - Use UTF-8 characters
4. **Simplify query** - Break complex questions into simpler ones

## 🛠️ Performance Issues & Optimization

### **Slow Document Upload**
**Symptoms:** Upload takes >5 seconds for small files
**Solutions:**
1. **Check storage provider** - Use Redis/Qdrant instead of FileSystem
2. **Optimize chunking** - Reduce chunk overlap
3. **Use async processing** - Enable background document processing
4. **Check network** - Ensure stable connection

### **Slow Search Response**
**Symptoms:** Search takes >3 seconds
**Solutions:**
1. **Optimize embeddings** - Use cached embeddings
2. **Reduce max results** - Limit number of returned sources
3. **Use vector search** - Ensure embeddings are generated
4. **Check storage performance** - Use SSD storage for databases

### **High Memory Usage**
**Symptoms:** Application uses >500MB RAM
**Solutions:**
1. **Limit document cache** - Reduce MaxDocuments in memory
2. **Use streaming** - Process documents in streams
3. **Optimize chunking** - Reduce chunk sizes
4. **Use external storage** - Move to Redis/Qdrant

## 🔍 Debugging & Logging

### **Enable Detailed Logging**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SmartRAG": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### **Check System Information**
```bash
# Get system info
curl "http://localhost:5000/api/benchmark/system-info"

# Run performance test
curl -X POST "http://localhost:5000/api/benchmark/performance-test" \
  -H "Content-Type: application/json" \
  -d '{
    "testDocumentUpload": true,
    "testSearch": true,
    "documentSizeKB": 100,
    "searchQuery": "test query",
    "maxResults": 5
  }'
```

### **Common Log Patterns**
```
[DEBUG] EnhancedSearchService: Searching in X documents with Y chunks
[DEBUG] Embedding search: Found X chunks with similarity > Y
[DEBUG] FallbackSearchAsync: Found X chunks containing query terms
[DEBUG] EnhancedSearchService RAG successful, using enhanced response
```

## 📊 Performance Benchmarks

### **Expected Performance (Development Environment)**
- **Document Upload**: 100KB file → ~500ms
- **Search Response**: Simple query → ~200ms
- **AI Response**: 5 sources → ~2-5 seconds
- **Memory Usage**: Base ~50MB + documents

### **Production Performance (Redis + Anthropic)**
- **Document Upload**: 1MB file → ~1-2 seconds
- **Search Response**: Complex query → ~500ms
- **AI Response**: 10 sources → ~3-8 seconds
- **Memory Usage**: Base ~100MB + Redis cache

## 🚀 Getting Help

### **1. Check This Guide**
Search for your error message or symptoms above.

### **2. Check GitHub Issues**
Search existing issues: [SmartRAG Issues](https://github.com/byerlikaya/SmartRAG/issues)

### **3. Create New Issue**
Include:
- Error message
- Configuration (without API keys)
- Steps to reproduce
- System information
- Logs (if available)

### **4. Contact Support**
- **Email**: [b.yerlikaya@outlook.com](mailto:b.yerlikaya@outlook.com)
- **LinkedIn**: [Barış Yerlikaya](https://www.linkedin.com/in/barisyerlikaya)

### **5. Community Resources**
- **Discussions**: [GitHub Discussions](https://github.com/byerlikaya/SmartRAG/discussions)
- **Documentation**: [SmartRAG Docs](https://github.com/byerlikaya/SmartRAG/docs)

## 🔧 Configuration Examples

### **Complete Working Configuration**
```json
{
  "AI": {
    "Anthropic": {
      "ApiKey": "sk-ant-...",
      "Model": "claude-3.5-sonnet",
      "MaxTokens": 4096,
      "Temperature": 0.3
    },
    "OpenAI": {
      "ApiKey": "sk-...",
      "Model": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002"
    }
  },
  "Storage": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Database": 0,
      "KeyPrefix": "smartrag:"
    }
  },
  "SmartRAG": {
    "MaxDocumentSizeMB": 100,
    "MaxChunkSize": 1000,
    "MinChunkSize": 100,
    "ChunkOverlap": 200,
    "SemanticSearchThreshold": 0.3
  }
}
```

### **Development Configuration**
```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "your-key",
      "Model": "gpt-4"
    }
  },
  "Storage": {
    "InMemory": {
      "MaxDocuments": 1000
    }
  }
}
```

---

**Need more help?** Check our [Getting Started Guide](getting-started.md) or [Configuration Guide](configuration.md).
