---
layout: default
title: Dashboard
description: SmartRAG Dashboard - tarayıcı tabanlı doküman yönetimi ve chat arayüzü
lang: tr
---

## Genel Bakış

**SmartRAG.Dashboard** paketi, uygulamanıza tarayıcı tabanlı bir web arayüzü ekler. Dashboard eklendiğinde:

- **Doküman yönetimi**: Tarayıcıdan dokümanları listeleme, yükleme ve silme
- **Desteklenen tipler**: Sadece SmartRAG’in desteklediği doküman tipleri yüklenebilir (PDF, Word, Excel, metin, görsel, ses vb.)
- **Chat**: O an yapılandırılmış AI modeli ile sohbet (SmartRAG konfigürasyonundaki aynı provider ve model)

Dashboard varsayılan olarak `/smartrag` yolunda sunulur ve geliştirme veya güvenilen ortamlar için tasarlanmıştır. **Kendi kimlik doğrulama veya yetkilendirmenizi eklemeden production ortamında herkese açık bırakmayın.**

## Kurulum

ASP.NET Core projenize SmartRAG çekirdeği ve Dashboard paketini ekleyin:

```bash
dotnet add package SmartRAG
dotnet add package SmartRAG.Dashboard
```

## Yapılandırma

`Program.cs` (veya startup) içinde SmartRAG ve dashboard’u kaydedin:

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

- **Path**: Dashboard’un temel yolu (varsayılan `/smartrag`). Tüm UI ve API rotaları bu yol altındadır.
- **EnableInDevelopmentOnly**: `true` iken dashboard sadece Development ortamında çalışır; diğer ortamlarda 404 döner. Production’da açmak istiyorsanız `false` yapıp erişimi kendiniz kısıtlamalısınız.
- **AuthorizationFilter**: Opsiyonel `Func<HttpContext, bool>`. Tanımlandığında her istek için çağrılır; `false` dönerse yanıt 403 olur.

## Güvenlik ve Production

- Dashboard **yerleşik kimlik doğrulama içermez**. URL’ye erişebilen herkes doküman listesini görebilir, yükleme/silme yapabilir ve chat kullanabilir.
- **Geliştirme**: `EnableInDevelopmentOnly = true` ile dashboard yalnızca `IHostEnvironment.IsDevelopment()` true iken kullanılabilir.
- **Production**: Dashboard’u production’da açacaksanız:
  - Erişimi reverse proxy veya middleware ile kısıtlayın (IP listesi, VPN vb.), veya
  - Uygulamanızın auth’u ile entegre edin (dashboard yoluna rol/policy zorunlu kılın), veya
  - `AuthorizationFilter` ile özel kontrol yazın.

Örnek: dashboard’u sadece kimliği doğrulanmış kullanıcılara açmak:

```csharp
builder.Services.AddSmartRagDashboard(options =>
{
    options.Path = "/smartrag";
    options.EnableInDevelopmentOnly = false;
    options.AuthorizationFilter = ctx => ctx.User.Identity?.IsAuthenticated == true;
});

// Dashboard yolunun auth middleware/policy altında olduğundan emin olun.
```

## API Uçları (dashboard yolu altında)

Tüm uçlar yapılandırılan path’e göredir (örn. `/smartrag`).

| Method | Path | Açıklama |
|--------|------|----------|
| GET | `/api/documents` | Doküman listesi (query: `skip`, `take`) |
| GET | `/api/documents/{id}` | Tek doküman |
| DELETE | `/api/documents/{id}` | Doküman silme |
| POST | `/api/documents` | Doküman yükleme (multipart: `file`, `uploadedBy`, opsiyonel `language`) |
| GET | `/api/upload/supported-types` | Desteklenen uzantı ve MIME tipleri |
| GET | `/api/chat/config` | Aktif AI provider ve model adı |
| POST | `/api/chat/messages` | Chat mesajı (JSON: `message`, opsiyonel `sessionId`) |

## Kullanım

1. ASP.NET Core uygulamanızı çalıştırın (örn. `dotnet run`).
2. Tarayıcıda dashboard’u açın: `https://localhost:5000/smartrag` (veya uygulama URL’iniz ve path).
3. **Documents** panelinden desteklenen türde dosya yükleyin, listeyi görüntüleyin ve silin.
4. **Chat** panelinden o anki AI modeli ile mesaj gönderin; aktif provider/model üstte gösterilir.

Dashboard, uygulamanızın geri kalanıyla aynı SmartRAG servislerini (`IDocumentService`, `IAIService` vb.) kullandığı için dokümanlar ve chat mevcut yapılandırmanızla tutarlıdır.
