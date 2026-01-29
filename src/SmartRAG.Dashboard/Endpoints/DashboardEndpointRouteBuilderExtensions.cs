using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using SmartRAG.Dashboard.Models;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Storage;
using SmartRAG.Interfaces.Support;
using SmartRAG.Models;
using SmartRAG.Models.RequestResponse;
using SmartRAG.Entities;

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
        var settingsGroup = endpoints.MapGroup($"{normalizedBasePath}/api/settings");

        MapDocumentEndpoints(documentsGroup);
        MapChatEndpoints(chatGroup);
        MapUploadEndpoints(uploadGroup);
        MapSettingsEndpoints(settingsGroup);

        return endpoints;
    }

    private static void MapSettingsEndpoints(RouteGroupBuilder group)
    {
        group.MapGet(
            string.Empty,
            (IOptions<SmartRagOptions> options, IAIConfigurationService aiConfig, IConfiguration configuration) =>
            {
                var opts = options.Value;
                var aiProviderConfig = aiConfig.GetAIProviderConfig();
                var features = opts.Features ?? new FeatureToggles();
                var whisper = opts.WhisperConfig ?? new WhisperConfig();

                var response = new SettingsResponse
                {
                    Providers = new SettingsProviders
                    {
                        Ai = opts.AIProvider.ToString(),
                        Storage = opts.StorageProvider.ToString(),
                        Conversation = opts.ConversationStorageProvider?.ToString() ?? opts.StorageProvider.ToString()
                    },
                    Features = new SettingsFeatures
                    {
                        EnableDatabaseSearch = features.EnableDatabaseSearch,
                        EnableDocumentSearch = features.EnableDocumentSearch,
                        EnableAudioSearch = features.EnableAudioSearch,
                        EnableImageSearch = features.EnableImageSearch,
                        EnableMcpSearch = features.EnableMcpSearch,
                        EnableFileWatcher = features.EnableFileWatcher
                    },
                    Chunking = new SettingsChunking
                    {
                        MaxChunkSize = opts.MaxChunkSize,
                        MinChunkSize = opts.MinChunkSize,
                        ChunkOverlap = opts.ChunkOverlap
                    },
                    Retry = new SettingsRetry
                    {
                        MaxRetryAttempts = opts.MaxRetryAttempts,
                        RetryDelayMs = opts.RetryDelayMs,
                        RetryPolicy = opts.RetryPolicy.ToString(),
                        EnableFallbackProviders = opts.EnableFallbackProviders,
                        FallbackProviders = opts.FallbackProviders?.Select(p => p.ToString()).ToList() ?? new List<string>()
                    },
                    Whisper = new SettingsWhisper
                    {
                        ModelPath = whisper.ModelPath ?? string.Empty,
                        DefaultLanguage = whisper.DefaultLanguage ?? "auto",
                        MinConfidenceThreshold = whisper.MinConfidenceThreshold,
                        IncludeWordTimestamps = false,
                        MaxThreads = whisper.MaxThreads
                    },
                    ActiveAi = new SettingsActiveAi
                    {
                        Provider = opts.AIProvider.ToString(),
                        Model = aiProviderConfig?.Model ?? string.Empty,
                        MaxTokens = aiProviderConfig?.MaxTokens ?? 4096,
                        Temperature = aiProviderConfig?.Temperature ?? 0.3,
                        Endpoint = aiProviderConfig?.Endpoint ?? string.Empty
                    },
                    McpServers = (opts.McpServers ?? new List<McpServerConfig>())
                        .Select(s => new SettingsMcpServer
                        {
                            ServerId = s.ServerId,
                            Endpoint = s.Endpoint,
                            AutoConnect = s.AutoConnect,
                            TimeoutSeconds = s.TimeoutSeconds
                        })
                        .ToList(),
                    WatchedFolders = (opts.WatchedFolders ?? new List<WatchedFolderConfig>())
                        .Select(w => new SettingsWatchedFolder
                        {
                            FolderPath = w.FolderPath,
                            AllowedExtensions = w.AllowedExtensions ?? new List<string>(),
                            IncludeSubdirectories = w.IncludeSubdirectories,
                            AutoUpload = w.AutoUpload,
                            UserId = w.UserId ?? "system"
                        })
                        .ToList(),
                    DatabaseConnections = (opts.DatabaseConnections ?? new List<DatabaseConnectionConfig>())
                        .Select(d => new SettingsDatabaseConnection
                        {
                            Name = d.Name ?? string.Empty,
                            DatabaseType = d.DatabaseType.ToString(),
                            Enabled = d.Enabled
                        })
                        .ToList(),
                    RemainingByCategory = GetRemainingConfigByCategory(configuration.GetSection("SmartRAG"))
                };
                return Results.Json(response);
            });
    }

    private static readonly HashSet<string> KnownCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "AIProvider", "StorageProvider", "ConversationStorageProvider",
        "MaxChunkSize", "MinChunkSize", "ChunkOverlap",
        "MaxRetryAttempts", "RetryDelayMs", "RetryPolicy", "EnableFallbackProviders", "FallbackProviders",
        "WhisperConfig", "Features", "McpServers", "WatchedFolders", "DatabaseConnections"
    };

    private static Dictionary<string, List<SettingsEntry>> GetRemainingConfigByCategory(IConfigurationSection section)
    {
        var result = new Dictionary<string, List<SettingsEntry>>(StringComparer.OrdinalIgnoreCase);
        if (!section.Exists())
            return result;

        var allEntries = FlattenConfigSection(section, "SmartRAG");
        foreach (var entry in allEntries)
        {
            var path = entry.Path;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("SmartRAG:", StringComparison.OrdinalIgnoreCase))
                continue;
            var after = path.Substring("SmartRAG:".Length);
            var firstSegment = after.IndexOf(':') >= 0 ? after.Substring(0, after.IndexOf(':')) : after;
            if (KnownCategories.Contains(firstSegment))
                continue;
            if (!result.TryGetValue(firstSegment, out var list))
            {
                list = new List<SettingsEntry>();
                result[firstSegment] = list;
            }
            list.Add(entry);
        }

        return result;
    }

    private static List<SettingsEntry> FlattenConfigSection(IConfigurationSection section, string prefix)
    {
        var result = new List<SettingsEntry>();
        if (!section.Exists())
            return result;

        foreach (var child in section.GetChildren())
        {
            var path = string.IsNullOrEmpty(prefix) ? child.Key : prefix + ":" + child.Key;
            if (child.Value != null)
            {
                var value = child.Value;
                if (ShouldMaskConfigKey(path))
                    value = string.IsNullOrEmpty(value) ? "" : "***";
                result.Add(new SettingsEntry { Path = path, Value = value });
            }
            else
            {
                result.AddRange(FlattenConfigSection(child, path));
            }
        }

        return result;
    }

    private static bool ShouldMaskConfigKey(string path)
    {
        var lower = path.ToLowerInvariant();
        return lower.Contains("key") || lower.Contains("password") || lower.Contains("secret")
            || lower.Contains("token") || lower.Contains("authorization") || lower.Contains("connectionstring");
    }

    private static void MapChatEndpoints(RouteGroupBuilder group)
    {
        group.MapGet(
            "/config",
            (IOptions<SmartRagOptions> options, IAIConfigurationService aiConfig) =>
            {
                var opts = options.Value;
                var config = aiConfig.GetAIProviderConfig();

                var features = opts.Features ?? new FeatureToggles();
                var mcpServers = opts.McpServers ?? new List<McpServerConfig>();

                var response = new ChatConfigResponse
                {
                    Provider = opts.AIProvider.ToString(),
                    Model = config?.Model ?? string.Empty,
                    MaxTokens = config?.MaxTokens ?? 4096,
                    Features = new ChatFeatureFlags
                    {
                        EnableDatabaseSearch = features.EnableDatabaseSearch,
                        EnableDocumentSearch = features.EnableDocumentSearch,
                        EnableAudioSearch = features.EnableAudioSearch,
                        EnableImageSearch = features.EnableImageSearch,
                        EnableMcpSearch = features.EnableMcpSearch,
                        EnableFileWatcher = features.EnableFileWatcher
                    },
                    McpServers = mcpServers
                        .Select(s => new ChatMcpServerInfo
                        {
                            Id = s.ServerId,
                            Endpoint = s.Endpoint,
                            AutoConnect = s.AutoConnect
                        })
                        .ToList()
                };
                return Results.Json(response);
            });

        group.MapPost(
            "/messages",
            async (
                HttpRequest request,
                IRagAnswerGeneratorService ragAnswerGenerator,
                IConversationManagerService conversationManager,
                CancellationToken cancellationToken) =>
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

                var incomingMessage = body.Message.Trim();

                var sessionId = string.IsNullOrWhiteSpace(body.SessionId)
                    ? await conversationManager.StartNewConversationAsync(cancellationToken).ConfigureAwait(false)
                    : body.SessionId!;

                var fullHistory = await conversationManager
                    .GetConversationHistoryAsync(sessionId, cancellationToken)
                    .ConfigureAwait(false);

                var truncatedHistory = conversationManager.TruncateConversationHistory(fullHistory);

                var ragRequest = new GenerateRagAnswerRequest
                {
                    Query = incomingMessage,
                    MaxResults = 8,
                    ConversationHistory = truncatedHistory
                };

                var ragResponse = await ragAnswerGenerator
                    .GenerateBasicRagAnswerAsync(ragRequest, cancellationToken)
                    .ConfigureAwait(false);

                var answer = ragResponse.Answer ?? string.Empty;

                try
                {
                    await conversationManager
                        .AddToConversationAsync(sessionId, incomingMessage, answer, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch
                {
                    // Swallow conversation storage errors to avoid breaking chat
                }

                var sourceList = ragResponse.Sources ?? new List<SearchSource>();
                var sources = sourceList
                    .Select(s => new ChatSourceItem
                    {
                        DocumentId = s.DocumentId.ToString(),
                        FileName = s.FileName ?? string.Empty,
                        SourceType = s.SourceType ?? "Document",
                        ChunkIndex = s.ChunkIndex,
                        RelevantContent = s.RelevantContent ?? string.Empty,
                        Location = s.Location,
                        RelevanceScore = s.RelevanceScore
                    })
                    .ToList();

                if (sources.Count > 0)
                {
                    try
                    {
                        await conversationManager
                            .AddSourcesForLastTurnAsync(sessionId, JsonSerializer.Serialize(sources), cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch
                    {
                        // Swallow sources storage errors
                    }
                }

                var response = new ChatMessageResponse
                {
                    Answer = answer,
                    SessionId = sessionId,
                    Sources = sources
                };
                return Results.Json(response);
            });

        group.MapGet(
            "/sessions",
            async (IConversationRepository conversationRepository, CancellationToken cancellationToken) =>
            {
                var sessionIds = await conversationRepository.GetAllSessionIdsAsync(cancellationToken).ConfigureAwait(false);
                var summaries = new List<ChatSessionSummaryResponse>();

                foreach (var id in sessionIds)
                {
                    var history = await conversationRepository.GetConversationHistoryAsync(id, cancellationToken).ConfigureAwait(false);
                    var summary = BuildChatSessionSummary(id, history);
                    summaries.Add(summary);
                }

                return Results.Json(summaries);
            });

        group.MapDelete(
            "/sessions",
            async (IConversationManagerService conversationManager, CancellationToken cancellationToken) =>
            {
                await conversationManager.ClearAllConversationsAsync(cancellationToken).ConfigureAwait(false);
                return Results.NoContent();
            });

        group.MapDelete(
            "/sessions/{sessionId}",
            async (string sessionId, IConversationRepository conversationRepository, CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    return Results.BadRequest("sessionId is required.");
                }

                var exists = await conversationRepository.SessionExistsAsync(sessionId, cancellationToken).ConfigureAwait(false);
                if (!exists)
                {
                    return Results.NotFound();
                }

                await conversationRepository.ClearConversationAsync(sessionId, cancellationToken).ConfigureAwait(false);
                return Results.NoContent();
            });

        group.MapGet(
            "/sessions/{sessionId}",
            async (string sessionId, IConversationRepository conversationRepository, IConversationManagerService conversationManager, CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    return Results.BadRequest("sessionId is required.");
                }

                var exists = await conversationRepository.SessionExistsAsync(sessionId, cancellationToken).ConfigureAwait(false);
                if (!exists)
                {
                    return Results.NotFound();
                }

                var history = await conversationRepository.GetConversationHistoryAsync(sessionId, cancellationToken).ConfigureAwait(false);
                var messages = ParseConversationHistory(history);

                var sourcesJson = await conversationManager.GetSourcesForSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(sourcesJson))
                {
                    try
                    {
                        var sourcesByTurn = JsonSerializer.Deserialize<List<List<ChatSourceItem>>>(sourcesJson);
                        if (sourcesByTurn != null && sourcesByTurn.Count > 0)
                        {
                            var assistantIndex = 0;
                            foreach (var msg in messages)
                            {
                                if (string.Equals(msg.Role, "assistant", StringComparison.OrdinalIgnoreCase) && assistantIndex < sourcesByTurn.Count)
                                {
                                    msg.Sources = sourcesByTurn[assistantIndex];
                                    assistantIndex++;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore deserialization errors
                    }
                }

                var detail = new ChatSessionDetailResponse
                {
                    Id = sessionId,
                    Messages = messages
                };

                return Results.Json(detail);
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
                var userDocuments = allDocuments
                    .Where(d => !IsSchemaDocument(d))
                    .ToList();

                var totalCount = userDocuments.Count;

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

                var items = userDocuments
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
            "/schemas",
            async (int? skip, int? take, IDocumentService documentService, CancellationToken cancellationToken) =>
            {
                var allDocuments = await documentService.GetAllDocumentsAsync(cancellationToken).ConfigureAwait(false);
                var schemaDocuments = allDocuments
                    .Where(IsSchemaDocument)
                    .ToList();

                var totalCount = schemaDocuments.Count;

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

                var items = schemaDocuments
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

        group.MapGet(
            "/{id:guid}/chunks",
            async (Guid id, IDocumentService documentService, CancellationToken cancellationToken) =>
            {
                var document = await documentService.GetDocumentAsync(id, cancellationToken).ConfigureAwait(false);
                if (document == null)
                {
                    return Results.NotFound();
                }

                var chunks = document.Chunks
                    .OrderBy(c => c.ChunkIndex)
                    .Select(c => new
                    {
                        id = c.Id,
                        chunkIndex = c.ChunkIndex,
                        content = c.Content,
                        startPosition = c.StartPosition,
                        endPosition = c.EndPosition,
                        documentType = c.DocumentType
                    })
                    .ToList();

                return Results.Json(chunks);
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

        group.MapDelete(
            string.Empty,
            async (IDocumentService documentService, CancellationToken cancellationToken) =>
            {
                var success = await documentService.ClearAllDocumentsAsync(cancellationToken).ConfigureAwait(false);
                if (!success)
                {
                    return Results.StatusCode(StatusCodes.Status500InternalServerError);
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

    private static bool IsSchemaDocument(Document document)
    {
        if (document?.Metadata == null)
        {
            return false;
        }

        if (!document.Metadata.TryGetValue("documentType", out var docType) || docType == null)
        {
            return false;
        }

        return string.Equals(docType.ToString(), "Schema", StringComparison.OrdinalIgnoreCase);
    }

    private static ChatSessionSummaryResponse BuildChatSessionSummary(string sessionId, string history)
    {
        var title = "Conversation";
        if (!string.IsNullOrWhiteSpace(history))
        {
            var lines = history.Split('\n');
            var firstUserLine = lines.FirstOrDefault(l => l.StartsWith("User:", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(firstUserLine))
            {
                var text = firstUserLine.Substring("User:".Length).Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    title = text.Length > 40 ? text[..37] + "..." : text;
                }
            }
        }

        return new ChatSessionSummaryResponse
        {
            Id = sessionId,
            Title = title,
            LastUpdated = null
        };
    }

    private static List<ChatMessageItem> ParseConversationHistory(string history)
    {
        var result = new List<ChatMessageItem>();
        if (string.IsNullOrWhiteSpace(history))
        {
            return result;
        }

        var lines = history.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("User:", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new ChatMessageItem
                {
                    Role = "user",
                    Text = line["User:".Length..].Trim()
                });
            }
            else if (line.StartsWith("Assistant:", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new ChatMessageItem
                {
                    Role = "assistant",
                    Text = line["Assistant:".Length..].Trim()
                });
            }
            else if (result.Count > 0)
            {
                var last = result[result.Count - 1];
                last.Text = last.Text + "\n" + line;
            }
        }

        return result;
    }
}

