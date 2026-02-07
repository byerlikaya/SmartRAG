using SmartRAG.Models.Health;

namespace SmartRAG.Demo.Services.Console;

/// <summary>
/// Service for console output operations
/// </summary>
public interface IConsoleService
{
    void WriteSuccess(string message);
    void WriteError(string message, Exception? exception = null);
    void WriteWarning(string message);
    void WriteInfo(string message);
    void WriteSectionHeader(string title);
    void WriteSeparator();
    void WriteHealthStatus(HealthStatus status, bool inline = false);
    string? ReadLine(string prompt);
    string? ReadConfirmation(string message, string confirmText = "yes");
}

