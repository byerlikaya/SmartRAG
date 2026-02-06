using System;

using Microsoft.Extensions.DependencyInjection;

namespace SmartRAG.Dashboard;

/// <summary>
/// Provides extension methods for registering SmartRAG dashboard services.
/// </summary>
public static class SmartRagDashboardServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SmartRAG dashboard services and configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An optional delegate to configure dashboard options.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddSmartRagDashboard(
        this IServiceCollection services,
        Action<SmartRagDashboardOptions>? configure = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var options = new SmartRagDashboardOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

        return services;
    }
}

