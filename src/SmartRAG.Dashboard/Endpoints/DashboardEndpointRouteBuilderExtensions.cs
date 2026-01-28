using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

using SmartRAG.Dashboard.Models;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Document;
using SmartRAG.Models;
using SmartRAG.Models.RequestResponse;

namespace SmartRAG.Dashboard.Endpoints;

/// <summary>
/// Provides endpoint routing configuration for the SmartRAG dashboard.
/// </summary>
public static class DashboardEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSmartRagDashboardEndpoints(
        IEndpointRouteBuilder endpoints,
        string basePath)
    {
        if (endpoints == null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        if (string.IsNullOrWhiteSpace(basePath))
        {
            basePath = "/smartrag";
        }

        var normalizedBasePath = NormalizeBasePath(basePath);
        var documentsGroup = endpoints.MapGroup($"{normalizedBasePath}/api/documents");
        var chatGroup = endpoints.MapGroup($"{normalizedBasePath}/api/chat");
        var uploadGroup = endpoints.MapGroup($"{normalizedBasePath}/api/upload");

        MapDocumentEndpoints(documentsGroup);
        MapChatEndpoints(chatGroup);
        MapUploadEndpoints(uploadGroup);

        return endpoints;
    }

    private static void MapChatEndpoints(RouteGroupBuilder group)
    {
        group.MapGet(
            "/config",
            (IOptions<SmartRagOptions> options, IAIConfigurationService aiConfig) =>
            {
                var opts = options.Value;
                var config = aiConfig.GetAIProviderConfig();
                var response = new ChatConfigResponse
                {
                    Provider = opts.AIProvider.ToString(),
                    Model = config?.Model ?? string.Empty,
                    MaxTokens = config?.MaxTokens ?? 4096
                };
                return Results.Json(response);
            });

        group.MapPost(
            "/messages",
            async (HttpRequest request, IAIService aiService, CancellationToken cancellationToken) =>
            {
                ChatMessageRequest? body;
                try
                {
                    body = await request.ReadFromJsonAsync<ChatMessageRequest>(cancellationToken).ConfigureAwait(false);
                }
                catch (JsonException)
                {
                    return Results.BadRequest("Invalid JSON body.");
                }

                if (body == null || string.IsNullOrWhiteSpace(body.Message))
                {
                    return Results.BadRequest("Message is required.");
                }

                var sessionId = string.IsNullOrWhiteSpace(body.SessionId)
                    ? Guid.NewGuid().ToString("N")
                    : body.SessionId;

                var answer = await aiService.GenerateResponseAsync(
                    body.Message.Trim(),
                    Array.Empty<string>(),
                    cancellationToken).ConfigureAwait(false);

                var response = new ChatMessageResponse
                {
                    Answer = answer ?? string.Empty,
                    SessionId = sessionId
                };
                return Results.Json(response);
            });
    }

    private static void MapUploadEndpoints(RouteGroupBuilder group)
    {
        group.MapGet(
            "/supported-types",
            (IDocumentParserService documentParserService) =>
            {
                var extensions = documentParserService.GetSupportedFileTypes().ToList();
                var mimeTypes = documentParserService.GetSupportedContentTypes().ToList();
                var response = new SupportedDocumentTypesResponse
                {
                    Extensions = extensions,
                    MimeTypes = mimeTypes
                };
                return Results.Json(response);
            });
    }

    private static void MapDocumentEndpoints(RouteGroupBuilder group)
    {
        group.MapGet(
            string.Empty,
            async (int? skip, int? take, IDocumentService documentService, CancellationToken cancellationToken) =>
            {
                var allDocuments = await documentService.GetAllDocumentsAsync(cancellationToken).ConfigureAwait(false);
                var totalCount = allDocuments.Count;

                var effectiveSkip = skip.GetValueOrDefault(0);
                if (effectiveSkip < 0)
                {
                    effectiveSkip = 0;
                }

                var effectiveTake = take.GetValueOrDefault(50);
                if (effectiveTake <= 0)
                {
                    effectiveTake = 50;
                }

                var items = allDocuments
                    .OrderByDescending(d => d.UploadedAt)
                    .Skip(effectiveSkip)
                    .Take(effectiveTake)
                    .Select(d => new DocumentSummaryResponse
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        ContentType = d.ContentType,
                        UploadedBy = d.UploadedBy,
                        UploadedAt = d.UploadedAt,
                        FileSize = d.FileSize
                    })
                    .ToList();

                var response = new PagedDocumentsResponse
                {
                    Items = items,
                    TotalCount = totalCount
                };

                return Results.Ok(response);
            });

        group.MapGet(
            "/{id:guid}",
            async (Guid id, IDocumentService documentService, CancellationToken cancellationToken) =>
            {
                var document = await documentService.GetDocumentAsync(id, cancellationToken).ConfigureAwait(false);
                if (document == null)
                {
                    return Results.NotFound();
                }

                var response = new DocumentSummaryResponse
                {
                    Id = document.Id,
                    FileName = document.FileName,
                    ContentType = document.ContentType,
                    UploadedBy = document.UploadedBy,
                    UploadedAt = document.UploadedAt,
                    FileSize = document.FileSize
                };

                return Results.Ok(response);
            });

        group.MapDelete(
            "/{id:guid}",
            async (Guid id, IDocumentService documentService, CancellationToken cancellationToken) =>
            {
                var deleted = await documentService.DeleteDocumentAsync(id, cancellationToken).ConfigureAwait(false);
                if (!deleted)
                {
                    return Results.NotFound();
                }

                return Results.NoContent();
            });

        group.MapPost(
            string.Empty,
            async (HttpRequest request, IDocumentService documentService, CancellationToken cancellationToken) =>
            {
                if (!request.HasFormContentType)
                {
                    return Results.BadRequest("Form content type is required.");
                }

                var form = await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
                var file = form.Files.GetFile("file");

                if (file == null)
                {
                    return Results.BadRequest("Form file field 'file' is required.");
                }

                var uploadedBy = form["uploadedBy"].ToString();
                if (string.IsNullOrWhiteSpace(uploadedBy))
                {
                    return Results.BadRequest("Form field 'uploadedBy' is required.");
                }

                var language = form["language"].ToString();

                await using var stream = file.OpenReadStream();

                var uploadRequest = new UploadDocumentRequest
                {
                    FileStream = stream,
                    FileName = file.FileName,
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    UploadedBy = uploadedBy,
                    Language = string.IsNullOrWhiteSpace(language) ? null : language,
                    FileSize = file.Length
                };

                var document = await documentService.UploadDocumentAsync(uploadRequest, cancellationToken).ConfigureAwait(false);

                var response = new DocumentSummaryResponse
                {
                    Id = document.Id,
                    FileName = document.FileName,
                    ContentType = document.ContentType,
                    UploadedBy = document.UploadedBy,
                    UploadedAt = document.UploadedAt,
                    FileSize = document.FileSize
                };

                return Results.Created($"/api/documents/{response.Id}", response);
            });
    }

    private static string NormalizeBasePath(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            return "/smartrag";
        }

        if (!basePath.StartsWith("/", StringComparison.Ordinal))
        {
            basePath = "/" + basePath;
        }

        return basePath.TrimEnd('/');
    }
}

