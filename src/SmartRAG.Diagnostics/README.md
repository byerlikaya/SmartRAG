# SmartRAG.Diagnostics

Server-Sent Events (SSE) logging provider for SmartRAG applications.

> **⚠️ Not: Bu proje henüz NuGet paketi olarak yayınlanmamıştır. Şu anda sadece source code olarak kullanılabilir.**

## Features

- Real-time log streaming via Server-Sent Events
- Lightweight and performant
- Framework agnostic design
- Easy integration with existing SmartRAG applications

## Installation

### Option 1: Source Code (Current)
```bash
# Clone the repository and add project reference
git clone https://github.com/your-username/SmartRAG.git
cd SmartRAG
# Add project reference to your .csproj file
```

### Option 2: NuGet Package (Future)
```bash
# Coming soon - not yet available
dotnet add package SmartRAG.Diagnostics
```

## Quick Start

### 1. Add to Services

```csharp
using SmartRAG.Diagnostics.Extensions;

services.AddSmartRagSseLogging();
```

### 2. Add to Web Application

```csharp
using SmartRAG.Diagnostics.Extensions;

app.MapSmartRagLogStream("/api/logs/stream");
```

### 3. Connect from Frontend

```javascript
const eventSource = new EventSource('/api/logs/stream');
eventSource.onmessage = (event) => {
    console.log('Log:', event.data);
};
```

## Configuration Options

```csharp
services.AddSmartRagSseLogging(options =>
{
    options.MinLevel = LogLevel.Information;
    options.IncludeScopes = true;
    options.BufferCapacity = 1000;
});
```

## Development Status

- ✅ Core functionality implemented
- ✅ SSE streaming working
- ✅ Integration with SmartRAG
- 🔄 NuGet packaging (planned)
- 🔄 Additional configuration options (planned)

## License

MIT License - see LICENSE file for details.
