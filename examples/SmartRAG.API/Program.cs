using Microsoft.OpenApi.Models;
using SmartRAG.API.Filters;
using SmartRAG.Enums;
using System;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel server options for file uploads
builder.WebHost.UseKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100 MB
});

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
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo 
        { 
            Title = "SmartRAG API", 
            Version = "v2.4.0",
            Description = "Enterprise-grade RAG (Retrieval-Augmented Generation) API with universal database support",
            Contact = new OpenApiContact
            {
                Name = "Barış Yerlikaya",
                Email = "b.yerlikaya@outlook.com",
                Url = new Uri("https://www.linkedin.com/in/barisyerlikaya/")
            },
            License = new OpenApiLicense
            {
                Name = "MIT License",
                Url = new Uri("https://github.com/byerlikaya/SmartRAG/blob/main/LICENSE")
            }
        });

        // Include XML documentation
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }

        // Include SmartRAG library XML documentation
        var smartRagXmlPath = Path.Combine(AppContext.BaseDirectory, "SmartRAG.xml");
        if (File.Exists(smartRagXmlPath))
        {
            c.IncludeXmlComments(smartRagXmlPath);
        }

        // Configure multipart file upload for multiple files
        c.OperationFilter<MultipartFileUploadFilter>();

        // Configure example values
        c.SchemaFilter<ExampleSchemaFilter>();
    });

    // Configure form options for file uploads
    services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100 MB
        options.ValueLengthLimit = int.MaxValue;
        options.ValueCountLimit = int.MaxValue;
        options.KeyLengthLimit = int.MaxValue;
    });

    // Add SmartRag services with minimal configuration
    services.UseSmartRag(configuration,
        storageProvider: StorageProvider.Redis,        // Use InMemory as requested
        aiProvider: AIProvider.Anthropic                     // Use Gemini provider
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
        app.MapSwagger();
        app.UseSwaggerUI();
    }

    // Serve static files for simple upload page
    app.UseStaticFiles();

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseAuthorization();
    app.MapControllers();


}