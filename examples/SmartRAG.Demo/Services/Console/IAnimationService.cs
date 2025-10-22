namespace SmartRAG.Demo.Services.Console;

/// <summary>
/// Service for console animations
/// </summary>
public interface IAnimationService
{
    Task ShowWelcomeAnimationAsync();
    Task ShowLoadingAnimationAsync(string message, int durationMs = 2000);
    void ShowBanner();
}

