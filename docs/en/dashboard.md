---
layout: default
title: Dashboard
description: SmartRAG Dashboard - browser-based document management and chat UI
lang: en
---

## Overview

SmartRAG includes a built-in browser-based web UI (Dashboard). When you enable the dashboard, you get:

- **Document management**: List, upload, and delete documents from the browser
- **Supported types**: Only SmartRAG-supported document types can be uploaded (PDF, Word, Excel, text, images, audio, etc.)
- **Chat**: Chat with the currently configured AI model (the same provider and model as in your SmartRAG configuration)

The dashboard is served at a configurable path (default: `/smartrag`) and is intended for development or trusted environments. **Do not expose it publicly in production without adding your own authentication or authorization.**

## Installation

Add the SmartRAG package to your ASP.NET Core project (Dashboard is included):

```bash
dotnet add package SmartRAG
```

## Configuration

In `Program.cs` (or your startup), register SmartRAG and the dashboard:

```csharp
using SmartRAG.Extensions;
using SmartRAG.Dashboard;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSmartRag(builder.Configuration);
builder.Services.AddSmartRagDashboard(options =>
{
    options.Path = "/smartrag";
    options.EnableInDevelopmentOnly = true;
});

var app = builder.Build();

app.UseRouting();
app.UseSmartRagDashboard("/smartrag");
app.MapSmartRagDashboard("/smartrag");

app.MapControllers();
app.Run();
```

- **Path**: Base path for the dashboard (default `/smartrag`). All UI and API routes are under this path.
- **EnableInDevelopmentOnly**: When `true`, the dashboard returns 404 in non-Development environments. Set to `false` only if you explicitly want the dashboard in production and will protect it yourself.
- **AuthorizationFilter**: Optional `Func<HttpContext, bool>`. When set, the dashboard calls it for each request; if it returns `false`, the response is 403.

## Security and Production

- The dashboard has **no built-in authentication**. Anyone who can reach the URL can list, upload, delete documents and use the chat.
- **Development**: With `EnableInDevelopmentOnly = true`, the dashboard is only available when `IHostEnvironment.IsDevelopment()` is true.
- **Production**: If you enable the dashboard in production:
  - Use a reverse proxy or middleware to restrict access (IP allowlist, VPN, etc.), or
  - Integrate with your app’s auth (e.g. require a role or policy for the dashboard path), or
  - Use the `AuthorizationFilter` option to implement custom checks.

Example: restrict dashboard to a specific path and require authorization:

```csharp
builder.Services.AddSmartRagDashboard(options =>
{
    options.Path = "/smartrag";
    options.EnableInDevelopmentOnly = false;
    options.AuthorizationFilter = ctx => ctx.User.Identity?.IsAuthenticated == true;
});

// Then ensure the dashboard path is under your auth middleware/policy.
```

## API Endpoints (under the dashboard path)

All endpoints are relative to the configured path (e.g. `/smartrag`).

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/documents` | List documents (query: `skip`, `take`) |
| GET | `/api/documents/{id}` | Get one document by ID |
| DELETE | `/api/documents/{id}` | Delete a document |
| POST | `/api/documents` | Upload a document (multipart: `file`, `uploadedBy`, optional `language`) |
| GET | `/api/upload/supported-types` | Supported file extensions and MIME types |
| GET | `/api/chat/config` | Active AI provider and model name |
| POST | `/api/chat/messages` | Send a chat message (JSON: `message`, optional `sessionId`) |
| GET | `/api/chat/sessions` | List chat sessions |
| GET | `/api/chat/sessions/{sessionId}` | Get one chat session with messages |
| DELETE | `/api/chat/sessions` | Delete all chat sessions |
| DELETE | `/api/chat/sessions/{sessionId}` | Delete one chat session |
| GET | `/api/settings` | Dashboard configuration (providers, features, chunking, etc.) |

## Usage

1. Run your ASP.NET Core app (e.g. `dotnet run`).
2. Open the dashboard in a browser: `https://localhost:5000/smartrag` (or your app’s URL and path).
3. Use the **Documents** panel to upload files (with supported types), view the list, and delete documents.
4. Use the **Chat** panel to send messages to the currently configured AI model; the active provider/model is shown in the header.

The dashboard uses the same SmartRAG services (`IDocumentService`, `IAIService`, etc.) as the rest of your application, so documents and chat are consistent with your existing configuration.

## Screenshots

- **Documents panel**: Upload, list, and manage documents.
- **Chat panel**: Send messages and view conversation history.
- **Settings panel**: View configuration (providers, features, chunking).

Placeholder images (replace with actual screenshots from SmartRAG.API or Demo):

![Dashboard Documents](assets/images/dashboard-documents.png)
![Dashboard Chat](assets/images/dashboard-chat.png)
![Dashboard Settings](assets/images/dashboard-settings.png)
