using SmartRAG.Extensions;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SmartRAG.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            System.Console.WriteLine("🚀 SmartRAG Console Chat Application");
            System.Console.WriteLine("=====================================");
            System.Console.WriteLine();

            // Configuration setup
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();

            // Service collection setup
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning); // Reduce console noise
            });

            // Add SmartRAG with configuration
            services.AddSmartRag(configuration, options =>
            {
                options.StorageProvider = StorageProvider.InMemory;  // Simple in-memory storage for console
                options.AIProvider = AIProvider.OpenAI;             // Default to OpenAI, can be changed in config
            });

            // Build service provider
            var serviceProvider = services.BuildServiceProvider();

            // Get required services
            var documentSearchService = serviceProvider.GetRequiredService<IDocumentSearchService>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            System.Console.WriteLine("✅ SmartRAG initialized successfully!");
            System.Console.WriteLine("💬 Start chatting with AI. Type 'exit' to quit, 'clear' to clear conversation history.");
            System.Console.WriteLine();

            // Conversation loop
            while (true)
            {
                System.Console.Write("You: ");
                var userInput = System.Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userInput))
                    continue;

                if (userInput.ToLower() == "exit")
                {
                    System.Console.WriteLine("👋 Goodbye!");
                    break;
                }

                if (userInput.ToLower() == "clear")
                {
                    // Note: In a real implementation, you'd need to clear conversation history
                    System.Console.WriteLine("🔄 Conversation history cleared!");
                    continue;
                }

                try
                {
                    System.Console.WriteLine();
                    System.Console.Write("AI: ");
                    
                    // Generate AI response using SmartRAG
                    var response = await documentSearchService.GenerateRagAnswerAsync(
                        userInput,
                        maxResults: 3
                    );

                    System.Console.WriteLine(response.Answer);
                    System.Console.WriteLine();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during AI conversation");
                    System.Console.WriteLine($"❌ Error: {ex.Message}");
                    System.Console.WriteLine();
                }
            }

            // Cleanup
            serviceProvider.Dispose();
        }
    }
}