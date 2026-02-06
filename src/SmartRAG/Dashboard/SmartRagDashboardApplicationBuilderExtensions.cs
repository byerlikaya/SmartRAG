

namespace SmartRAG.Dashboard;

/// <summary>
/// Provides extension methods for wiring the SmartRAG dashboard into the ASP.NET Core pipeline.
/// </summary>
public static class SmartRagDashboardApplicationBuilderExtensions
{
    /// <summary>
    /// Registers the SmartRAG dashboard middleware on the given path.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="path">The dashboard base path. If null, the configured options path is used.</param>
    /// <returns>The same application builder for chaining.</returns>
    public static IApplicationBuilder UseSmartRagDashboard(
        this IApplicationBuilder app,
        string? path = null)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        if (string.IsNullOrWhiteSpace(path))
            return app.UseMiddleware<SmartRagDashboardMiddleware>();

        if (app.ApplicationServices.GetService(typeof(SmartRagDashboardOptions)) is SmartRagDashboardOptions options)
        {
            options.Path = path;
        }

        return app.UseMiddleware<SmartRagDashboardMiddleware>();
    }

    /// <summary>
    /// Registers the SmartRAG dashboard middleware and maps its API endpoints
    /// for applications using the <see cref="WebApplication"/> hosting model.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="path">The dashboard base path. If null, the configured options path is used.</param>
    /// <param name="mapApiEndpoints">
    /// If true, maps the JSON API endpoints under the same base path. Set to false to serve only the UI shell.
    /// </param>
    /// <returns>The same web application for chaining.</returns>
    public static WebApplication UseSmartRagDashboard(
        this WebApplication app,
        string? path = null,
        bool mapApiEndpoints = true)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        ((IApplicationBuilder)app).UseSmartRagDashboard(path);

        if (mapApiEndpoints)
        {
            app.MapSmartRagDashboard(path);
        }

        return app;
    }

    /// <summary>
    /// Maps the SmartRAG dashboard endpoints to the specified route base path using endpoint routing.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="path">The dashboard base path. If null, the configured options path is used.</param>
    /// <returns>The same endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapSmartRagDashboard(
        this IEndpointRouteBuilder endpoints,
        string? path = null)
    {
        if (endpoints == null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        var options = endpoints.ServiceProvider.GetService(typeof(SmartRagDashboardOptions)) as SmartRagDashboardOptions;
        if (!string.IsNullOrWhiteSpace(path) && options != null)
        {
            options.Path = path;
        }

        var effectivePath = !string.IsNullOrWhiteSpace(path)
            ? path!
            : options?.Path.Value ?? "/smartrag";

        DashboardEndpointRouteBuilderExtensions.MapSmartRagDashboardEndpoints(endpoints, effectivePath);

        return endpoints;
    }
}

