using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using SmartRAG.Diagnostics.Interfaces;

namespace SmartRAG.Diagnostics.Extensions;

public static class EndpointRouteBuilderExtensions
{
	public static IEndpointRouteBuilder MapSmartRagLogStream(this IEndpointRouteBuilder endpoints, string path = "/api/logs/stream")
	{
		endpoints.MapGet(path, async (HttpContext context) =>
		{
			context.Response.Headers.Add("Content-Type", "text/event-stream");
			context.Response.Headers.Add("Cache-Control", "no-cache");
			context.Response.Headers.Add("Connection", "keep-alive");

			var stream = context.RequestServices.GetRequiredService<ILogStream>();
			var (id, reader) = stream.Subscribe();
			try
			{
				await foreach (var message in reader.ReadAllAsync(context.RequestAborted))
				{
					await context.Response.WriteAsync($"data: {message}\n\n");
					await context.Response.Body.FlushAsync();
				}
			}
			finally
			{
				stream.Unsubscribe(id);
			}
		});

		return endpoints;
	}
}
