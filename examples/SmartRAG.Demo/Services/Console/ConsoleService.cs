using SmartRAG.Demo.Models;

namespace SmartRAG.Demo.Services.Console;

/// <summary>
/// Console output service for consistent formatting
/// </summary>
public class ConsoleService : IConsoleService
{
    #region Constants

    private const string SeparatorLine = "═══════════════════════════════════════════════════════════════════";

    #endregion

    #region Public Methods

    public void WriteSuccess(string message)
    {
        System.Console.ForegroundColor = ConsoleColor.Green;
        System.Console.WriteLine($"✓ {message}");
        System.Console.ResetColor();
    }

    public void WriteError(string message, Exception? exception = null)
    {
        System.Console.ForegroundColor = ConsoleColor.Red;
        System.Console.WriteLine($"❌ {message}");
        
        if (exception != null)
        {
            System.Console.WriteLine($"   Error Type: {exception.GetType().Name}");
            System.Console.WriteLine($"   Details: {exception.Message}");
            
            if (exception.InnerException != null)
            {
                System.Console.WriteLine($"   Inner Exception: {exception.InnerException.Message}");
            }
        }
        
        System.Console.ResetColor();
    }

    public void WriteWarning(string message)
    {
        System.Console.ForegroundColor = ConsoleColor.Yellow;
        System.Console.WriteLine($"⚠️  {message}");
        System.Console.ResetColor();
    }

    public void WriteInfo(string message)
    {
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine(message);
        System.Console.ResetColor();
    }

    public void WriteSectionHeader(string title)
    {
        System.Console.WriteLine();
        System.Console.WriteLine(SeparatorLine);
        System.Console.WriteLine(title);
        System.Console.WriteLine(SeparatorLine);
        System.Console.WriteLine();
    }

    public void WriteSeparator()
    {
        System.Console.WriteLine(SeparatorLine);
    }

    public void WriteHealthStatus(HealthStatus status, bool inline = false)
    {
        if (status.IsHealthy)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine("✓ Healthy");
            System.Console.ResetColor();
            
            if (!inline && !string.IsNullOrEmpty(status.Message))
            {
                System.Console.WriteLine($"   {status.Message}");
            }
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine("✗ Unavailable");
            System.Console.ResetColor();
            
            if (!inline)
            {
                if (!string.IsNullOrEmpty(status.Message))
                    System.Console.WriteLine($"   {status.Message}");
                
                if (!string.IsNullOrEmpty(status.Details))
                    System.Console.WriteLine($"   {status.Details}");
            }
        }
    }

    public string? ReadLine(string prompt)
    {
        System.Console.Write(prompt);
        return System.Console.ReadLine();
    }

    public string? ReadConfirmation(string message, string confirmText = "yes")
    {
        WriteWarning(message);
        System.Console.WriteLine();
        System.Console.Write($"Type '{confirmText}' to confirm: ");
        return System.Console.ReadLine();
    }

    #endregion
}

