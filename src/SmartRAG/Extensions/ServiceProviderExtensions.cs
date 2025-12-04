using Microsoft.Extensions.DependencyInjection;
using SmartRAG.Services.Startup;
using System;
using System.Threading.Tasks;

namespace SmartRAG.Extensions
{
    /// <summary>
    /// Extension methods for service provider initialization
    /// </summary>
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Initializes SmartRAG features (MCP connections and file watchers) from configuration
        /// </summary>
        /// <param name="serviceProvider">Service provider</param>
        /// <returns>Task representing the initialization operation</returns>
        public static async Task InitializeSmartRagAsync(this IServiceProvider serviceProvider)
        {
            var startupService = serviceProvider.GetService<ISmartRagStartupService>();
            if (startupService != null)
            {
                await startupService.InitializeAsync();
            }
        }
    }
}

