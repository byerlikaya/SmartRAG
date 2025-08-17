using Microsoft.Extensions.Logging;
using Scalar.AspNetCore;
using SmartRAG.Enums;
using SmartRAG.Extensions;

var builder = WebApplication.CreateBuilder(args);

RegisterServices(builder.Services, builder.Configuration);

var app = builder.Build();
ConfigureMiddleware(app, builder.Environment);

app.Run();

static void RegisterServices(IServiceCollection services, IConfiguration configuration)
{
    // Configure logging
    services.AddLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddConsole();
        builder.AddDebug();
        builder.SetMinimumLevel(LogLevel.Debug);
    });

    services.AddControllers();
    services.AddEndpointsApiExplorer();
    services.AddOpenApi();

    // Add SmartRag services with minimal configuration
    services.UseSmartRag(configuration,
        storageProvider: StorageProvider.InMemory,  // Default: InMemory
        aiProvider: AIProvider.OpenAI               // Use OpenAI provider
    );

    services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });
}

static void ConfigureMiddleware(WebApplication app, IWebHostEnvironment environment)
{

    if (environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseAuthorization();
    app.MapControllers();
}