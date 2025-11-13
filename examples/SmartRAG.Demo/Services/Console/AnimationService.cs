namespace SmartRAG.Demo.Services.Console;

/// <summary>
/// Service for console animations and visual effects
/// </summary>
public class AnimationService : IAnimationService
{
    #region Constants

    private static readonly string[] SmartRagLogoLines = new[]
    {
        "███████╗███╗   ███╗ █████╗ ██████╗ ████████╗██████╗  █████╗  ██████╗",
        "██╔════╝████╗ ████║██╔══██╗██╔══██╗╚══██╔══╝██╔══██╗██╔══██╗██╔════╝",
        "███████╗██╔████╔██║███████║██████╔╝   ██║   ██████╔╝███████║██║  ███╗",
        "╚════██║██║╚██╔╝██║██╔══██║██╔══██╗   ██║   ██╔══██╗██╔══██║██║   ██║",
        "███████║██║ ╚═╝ ██║██║  ██║██║  ██║   ██║   ██║  ██║██║  ██║╚██████╔╝",
        "╚══════╝╚═╝     ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝   ╚═╝   ╚═╝  ╚═╝╚═╝  ╚═╝ ╚═════╝"
    };

    private static readonly string[] AIThinkingFrames = new[]
    {
        "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"
    };

    #endregion

    #region Public Methods

    public async Task ShowWelcomeAnimationAsync()
    {
        if (!ConsoleHelper.IsConsoleAvailable())
        {
            return;
        }

        System.Console.Clear();

        // Phase 1: Logo reveal with colors
        await RevealLogoAsync();

        // Phase 2: Loading bar (vertically centered)
        await ShowProgressBarAsync();

        // Small delay to let user see the completion message
        await Task.Delay(800);
    }

    public async Task ShowLoadingAnimationAsync(string message, int durationMs = 2000)
    {
        if (!ConsoleHelper.IsConsoleAvailable())
        {
            return;
        }

        var frames = AIThinkingFrames;
        var startTime = DateTime.Now;
        var index = 0;

        System.Console.CursorVisible = false;

        while ((DateTime.Now - startTime).TotalMilliseconds < durationMs)
        {
            System.Console.Write($"\r{frames[index]} {message}");
            index = (index + 1) % frames.Length;
            await Task.Delay(80);
        }

        System.Console.CursorVisible = true;
        System.Console.WriteLine();
    }

    public void ShowBanner()
    {
        if (!ConsoleHelper.IsConsoleAvailable())
        {
            return;
        }

        var windowWidth = System.Console.WindowWidth;
        var logoWidth = SmartRagLogoLines[0].Length;
        var leftPadding = Math.Max(0, (windowWidth - logoWidth) / 2);
        var padding = new string(' ', leftPadding);

        System.Console.ForegroundColor = ConsoleColor.Cyan;
        foreach (var line in SmartRagLogoLines)
        {
            System.Console.WriteLine(padding + line);
        }
        System.Console.ResetColor();

        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.DarkCyan;
        CenterText("🤖 AI-Powered Retrieval-Augmented Generation Framework");
        CenterText("⚡ Multi-Database Query Coordinator | Document Intelligence");
        System.Console.ResetColor();
        System.Console.WriteLine();
    }

    private static void CenterText(string text)
    {
        var windowWidth = System.Console.WindowWidth;
        var leftPadding = Math.Max(0, (windowWidth - text.Length) / 2);
        System.Console.WriteLine(new string(' ', leftPadding) + text);
    }

    #endregion

    #region Private Methods

    private static async Task RevealLogoAsync()
    {
        var windowWidth = System.Console.WindowWidth;
        var windowHeight = System.Console.WindowHeight;
        var logoWidth = SmartRagLogoLines[0].Length;
        var logoHeight = SmartRagLogoLines.Length;

        // Calculate vertical centering - move logo up a bit
        var leftPadding = Math.Max(0, (windowWidth - logoWidth) / 2);
        var topPadding = Math.Max(0, (windowHeight - logoHeight - 4) / 2 - 3); // -3 to move logo up

        var padding = new string(' ', leftPadding);

        // Position cursor for vertical centering
        System.Console.SetCursorPosition(0, topPadding);

        // Single color for logo
        System.Console.ForegroundColor = ConsoleColor.Cyan;

        foreach (var line in SmartRagLogoLines)
        {
            System.Console.WriteLine(padding + line);
            await Task.Delay(80);
        }
        System.Console.ResetColor();

        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.Yellow;
        CenterText("🚀 Initializing AI System...");
        System.Console.ResetColor();
        System.Console.WriteLine();
        System.Console.WriteLine(); // Extra blank line after "Initializing"

        await Task.Delay(150);
    }

    private static async Task ShowProgressBarAsync()
    {
        var steps = new[]
        {
            ("Loading neural networks", ConsoleColor.Cyan),
            ("Connecting vector databases", ConsoleColor.Green),
            ("Initializing AI providers", ConsoleColor.Magenta),
            ("Analyzing database schemas", ConsoleColor.Yellow),
            ("System ready", ConsoleColor.Green)
        };

        var windowWidth = System.Console.WindowWidth;
        var windowHeight = System.Console.WindowHeight;
        var totalStepsHeight = steps.Length + 3; // +3 for spacing and final banner

        // Calculate vertical centering for the entire progress block
        // Position it below the logo area with proper spacing
        var topPadding = Math.Max(0, (windowHeight - totalStepsHeight) / 2) + 5; // +5 to move below logo with better spacing

        // Position cursor for vertical centering
        System.Console.SetCursorPosition(0, topPadding);

        // Calculate left padding to center the block but keep items left-aligned
        var maxTextLength = steps.Max(s => s.Item1.Length) + 6; // +6 for "▸ " and " ✓"
        var blockLeftPadding = Math.Max(0, (windowWidth - maxTextLength) / 2);

        foreach (var (message, color) in steps)
        {
            System.Console.ForegroundColor = color;
            var text = $"▸ {message}";
            System.Console.Write(new string(' ', blockLeftPadding) + text);

            // Animated dots with retro game sounds
            for (int i = 0; i < 3; i++)
            {
                await Task.Delay(100);
                System.Console.Write(".");
            }

            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine(" ✓");
            System.Console.ResetColor();

            await Task.Delay(150);
        }

        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.Green;

        // Create the banner as a single centered block
        var bannerLines = new[]
        {
            "╔═══════════════════════════════════════════════════════╗",
            "║           ✨ SmartRAG is ready to serve! ✨           ║",
            "╚═══════════════════════════════════════════════════════╝"
        };

        // Calculate padding for the entire banner block
        var bannerWidth = bannerLines[0].Length;
        var bannerPadding = Math.Max(0, (System.Console.WindowWidth - bannerWidth) / 2);
        var padding = new string(' ', bannerPadding);

        foreach (var line in bannerLines)
        {
            System.Console.WriteLine(padding + line);
        }

        System.Console.ResetColor();
        System.Console.WriteLine();

        await Task.Delay(1000);
    }

    #endregion
}



