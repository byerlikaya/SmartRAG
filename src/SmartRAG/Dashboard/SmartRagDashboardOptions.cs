using System;

using Microsoft.AspNetCore.Http;

namespace SmartRAG.Dashboard;

/// <summary>
/// Provides configuration options for the SmartRAG dashboard.
/// </summary>
public class SmartRagDashboardOptions
{
    /// <summary>
    /// Gets or sets the base request path for the dashboard.
    /// </summary>
    public PathString Path { get; set; } = new("/smartrag");

    /// <summary>
    /// Gets or sets a value indicating whether the dashboard should only be enabled in development environments.
    /// </summary>
    public bool EnableInDevelopmentOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional authorization filter that can be used by the host application
    /// to restrict access to the dashboard.
    /// </summary>
    public Func<HttpContext, bool>? AuthorizationFilter { get; set; }
}

