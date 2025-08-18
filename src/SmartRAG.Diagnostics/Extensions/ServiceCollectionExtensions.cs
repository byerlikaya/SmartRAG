using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartRAG.Diagnostics.Interfaces;
using SmartRAG.Diagnostics.Logging;
using SmartRAG.Diagnostics.Services;

namespace SmartRAG.Diagnostics.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddSmartRagSseLogging(this IServiceCollection services)
	{
		// Single ILogStream instance for all consumers
		services.AddSingleton<ILogStream, LogStreamService>();
		// Register logger provider via DI (no BuildServiceProvider usage)
		services.AddSingleton<ILoggerProvider, SseLoggerProvider>();
		return services;
	}
}
