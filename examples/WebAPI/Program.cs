using Microsoft.OpenApi.Models;
using SmartRAG.API.Filters;
using SmartRAG.Enums;
using SmartRAG.Extensions;

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
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartRAG API", Version = "v1" });

        // Configure multipart file upload for multiple files
        c.OperationFilter<MultipartFileUploadFilter>();
        
        // CORS sorununu çözmek için
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
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
        storageProvider: StorageProvider.Redis,  // Default: InMemory
        aiProvider: AIProvider.AzureOpenAI               // Use OpenAI provider
    );

    services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithExposedHeaders("Content-Disposition");
        });
        
        // Swagger UI için özel CORS policy
        options.AddPolicy("SwaggerUI", policy =>
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
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartRAG API v1");
            c.RoutePrefix = string.Empty; // Root path'te Swagger UI açılsın
        });
    }

    // Serve static files for simple upload page
    app.UseStaticFiles();

    // Development ortamında HTTPS redirection'ı devre dışı bırak
    if (!environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    
    // CORS middleware'ini doğru sırada ekle
    app.UseCors("AllowAll");
    
    app.UseRouting();
    app.UseAuthorization();
    app.MapControllers();
}