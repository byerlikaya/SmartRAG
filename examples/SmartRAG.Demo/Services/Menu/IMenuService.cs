namespace SmartRAG.Demo.Services.Menu;

/// <summary>
/// Service for menu operations
/// </summary>
public interface IMenuService
{
    void ShowMainMenu();
    void ShowWelcomeBanner();
    Task<string?> GetMenuChoiceAsync();
    void PauseForUser();
}

