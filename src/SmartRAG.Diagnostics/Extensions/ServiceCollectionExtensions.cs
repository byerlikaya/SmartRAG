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
		services.AddSingleton<ILogStream, LogStreamService>();
		services.AddLogging(builder =>
		{
			builder.AddProvider(new SseLoggerProvider(services.BuildServiceProvider().GetRequiredService<ILogStream>()));
		});
		return services;
	}
}
