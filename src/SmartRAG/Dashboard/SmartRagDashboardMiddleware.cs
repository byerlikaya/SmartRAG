using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace SmartRAG.Dashboard;

/// <summary>
/// ASP.NET Core middleware that serves the SmartRAG dashboard UI and related assets.
/// </summary>
public sealed class SmartRagDashboardMiddleware
{
    private const string AssetsSegment = "/assets/";

    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;
    private readonly SmartRagDashboardOptions _options;
    private readonly IFileProvider _embeddedFileProvider;

    public SmartRagDashboardMiddleware(
        RequestDelegate next,
        IWebHostEnvironment environment,
        SmartRagDashboardOptions options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        var assembly = Assembly.GetExecutingAssembly();
        var baseNamespace = assembly.GetName().Name + ".Dashboard.wwwroot";
        _embeddedFileProvider = new EmbeddedFileProvider(assembly, baseNamespace);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (!IsDashboardRequest(context.Request.Path, out var relativePath))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (_options.EnableInDevelopmentOnly && !_environment.IsDevelopment())
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (_options.AuthorizationFilter != null && !_options.AuthorizationFilter(context))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        if (relativePath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrEmpty(relativePath) || relativePath == "/")
        {
            await ServeIndexHtmlAsync(context).ConfigureAwait(false);
            return;
        }

        if (relativePath.StartsWith(AssetsSegment, StringComparison.OrdinalIgnoreCase))
        {
            var assetPath = relativePath.Substring(AssetsSegment.Length);
            var contentType = GetContentType(assetPath);
            await ServeFileAsync(context, Path.Combine("assets", assetPath), contentType).ConfigureAwait(false);
            return;
        }

        // API endpoints will be added in subsequent implementation steps.
        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }

    private bool IsDashboardRequest(PathString requestPath, out string relativePath)
    {
        relativePath = string.Empty;

        var basePath = _options.Path.HasValue ? _options.Path : new PathString("/smartrag");

        if (!requestPath.StartsWithSegments(basePath, out var remaining))
        {
            return false;
        }

        relativePath = remaining.HasValue ? remaining.Value ?? string.Empty : "/";
        return true;
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension switch
        {
            ".js" => "application/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".png" => "image/png",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            _ => "application/octet-stream"
        };
    }

    private async Task ServeIndexHtmlAsync(HttpContext context)
    {
        const string Placeholder = "{{BASE_PATH}}";
        var fileInfo = _embeddedFileProvider.GetFileInfo("index.html");
        if (!fileInfo.Exists)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var basePath = _options.Path.HasValue ? _options.Path.Value : "/smartrag";
        if (!basePath.EndsWith("/", StringComparison.Ordinal))
        {
            basePath += "/";
        }

        await using var stream = fileInfo.CreateReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var html = await reader.ReadToEndAsync().ConfigureAwait(false);
        html = html.Replace(Placeholder, basePath);

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html, context.RequestAborted).ConfigureAwait(false);
    }

    private async Task ServeFileAsync(HttpContext context, string filePath, string contentType)
    {
        var fileInfo = _embeddedFileProvider.GetFileInfo(filePath);
        if (!fileInfo.Exists)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = contentType;

        await using var stream = fileInfo.CreateReadStream();
        await stream.CopyToAsync(context.Response.Body, context.RequestAborted).ConfigureAwait(false);
    }
}

