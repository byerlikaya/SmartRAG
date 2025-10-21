using SmartRAG.Demo.Models;
using SmartRAG.Demo.Services;
using SmartRAG.Demo.Services.Console;
using SmartRAG.Enums;

namespace SmartRAG.Demo.Handlers.OllamaHandlers;

/// <summary>
/// Handler for Ollama-related operations
/// </summary>
public class OllamaHandler(IConsoleService console) : IOllamaHandler
{
    #region Fields

    private readonly IConsoleService _console = console;

    #endregion

    #region Public Methods

    public async Task SetupModelsAsync()
    {
        _console.WriteSectionHeader("🤖 Setup Ollama Models");

        var ollamaManager = new OllamaModelManager();

        var isAvailable = await ollamaManager.IsServiceAvailableAsync();
        if (!isAvailable)
        {
            _console.WriteError("Ollama service is not running!");
            System.Console.WriteLine();
            System.Console.WriteLine("Please start Ollama:");
            System.Console.WriteLine("  • Docker: cd examples/SmartRAG.LocalDemo && docker-compose up -d ollama");
            System.Console.WriteLine("  • Or download from: https://ollama.ai");
            return;
        }

        _console.WriteSuccess("Ollama service is running");
        System.Console.WriteLine();

        var installedModels = await ollamaManager.ListInstalledModelsAsync();
        System.Console.WriteLine($"Installed models: {installedModels.Count}");
        foreach (var model in installedModels)
        {
            System.Console.WriteLine($"  • {model}");
        }
        System.Console.WriteLine();

        System.Console.WriteLine("Recommended models for SmartRAG:");
        var recommended = OllamaModelManager.GetRecommendedModels();
        var index = 1;
        foreach (var kvp in recommended)
        {
            var isInstalled = installedModels.Any(m => m.Contains(kvp.Key));
            var status = isInstalled ? "✓ Installed" : "  Not installed";
            System.Console.WriteLine($"{index}. {kvp.Key} - {kvp.Value} [{status}]");
            index++;
        }
        System.Console.WriteLine();

        var choice = _console.ReadLine("Enter model number to install (0 to skip): ");

        if (int.TryParse(choice, out var modelIndex) && modelIndex > 0 && modelIndex <= recommended.Count)
        {
            var modelToInstall = recommended.Keys.ElementAt(modelIndex - 1);
            System.Console.WriteLine();
            System.Console.WriteLine($"Downloading {modelToInstall}... (This may take several minutes)");
            System.Console.WriteLine();

            await ollamaManager.DownloadModelAsync(modelToInstall, (progress) =>
            {
                System.Console.WriteLine($"  {progress}");
            });

            System.Console.WriteLine();
            _console.WriteSuccess($"Model {modelToInstall} installed successfully!");
        }
    }

    public async Task TestVectorStoreAsync(string storageProvider)
    {
        _console.WriteSectionHeader("📦 Test Vector Store");

        var healthCheck = new HealthCheckService();

        System.Console.WriteLine($"Testing {storageProvider} vector store...");
        System.Console.WriteLine();

        if (!Enum.TryParse<StorageProvider>(storageProvider, out var provider))
        {
            _console.WriteError("Invalid storage provider");
            return;
        }

        HealthStatus status;
        if (provider == StorageProvider.Qdrant)
        {
            status = await healthCheck.CheckQdrantAsync();
        }
        else if (provider == StorageProvider.Redis)
        {
            status = await healthCheck.CheckRedisAsync();
        }
        else
        {
            _console.WriteWarning($"{storageProvider} does not require health check (file-based or in-memory)");
            return;
        }

        if (status.IsHealthy)
        {
            _console.WriteSuccess($"{status.ServiceName} is healthy");
            System.Console.WriteLine($"  {status.Message}");
            System.Console.WriteLine($"  {status.Details}");
        }
        else
        {
            _console.WriteError($"{status.ServiceName} is not available");
            System.Console.WriteLine($"  {status.Message}");
            System.Console.WriteLine($"  {status.Details}");
            System.Console.WriteLine();
            System.Console.WriteLine("Solution:");
            System.Console.WriteLine("  • Start services: cd examples/SmartRAG.LocalDemo && docker-compose up -d");
        }
    }

    #endregion
}

