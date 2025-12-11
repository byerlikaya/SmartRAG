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

        await ShowCenteredWelcomeAsync();

        await Task.Delay(1500);
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

    private static async Task ShowCenteredWelcomeAsync()
    {
        var features = new[]
        {
            ("🗄️  Multi-Database RAG", "Query SQL Server, MySQL, PostgreSQL, SQLite simultaneously", ConsoleColor.Cyan),
            ("📄 Multi-Modal Intelligence", "Process PDF, Word, Excel, Images (OCR), Audio (STT)", ConsoleColor.Magenta),
            ("🏠 On-Premise & Local AI", "100% local with Ollama - GDPR/KVKK compliant", ConsoleColor.Green),
            ("💬 Conversation History", "Session-based context-aware conversations", ConsoleColor.Yellow),
            ("🔍 Advanced Semantic Search", "Vector search with Qdrant, Redis, InMemory", ConsoleColor.Cyan),
            ("🧠 Unified Query Intelligence", "Automatic query routing across all sources", ConsoleColor.Magenta),
            ("🔌 MCP Integration", "Model Context Protocol support", ConsoleColor.Green),
            ("📁 File Watcher", "Automatic document processing from watched folders", ConsoleColor.Yellow)
        };

        var windowWidth = System.Console.WindowWidth;
        var windowHeight = System.Console.WindowHeight;
        var logoWidth = SmartRagLogoLines[0].Length;
        var logoHeight = SmartRagLogoLines.Length;
        
        var totalContentHeight = logoHeight + 3 + 2 + features.Length + 3;
        var topPadding = Math.Max(0, (windowHeight - totalContentHeight) / 2);
        
        System.Console.SetCursorPosition(0, topPadding);

        var logoLeftPadding = Math.Max(0, (windowWidth - logoWidth) / 2);
        var logoPadding = new string(' ', logoLeftPadding);

        System.Console.ForegroundColor = ConsoleColor.Cyan;
        foreach (var line in SmartRagLogoLines)
        {
            System.Console.WriteLine(logoPadding + line);
        }
        System.Console.ResetColor();

        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.DarkCyan;
        CenterText("🤖 AI-Powered Retrieval-Augmented Generation Framework");
        CenterText("⚡ Multi-Database Query Coordinator | Document Intelligence");
        System.Console.ResetColor();
        System.Console.WriteLine();

        System.Console.ForegroundColor = ConsoleColor.Yellow;
        CenterText("✨ Key Features");
        System.Console.ResetColor();
        System.Console.WriteLine();

        var maxFeatureWidth = features.Max(f => f.Item1.Length + f.Item2.Length + 3);
        var featureLeftPadding = Math.Max(0, (windowWidth - maxFeatureWidth) / 2);
        var featurePadding = new string(' ', featureLeftPadding);

        foreach (var (title, description, color) in features)
        {
            System.Console.ForegroundColor = color;
            System.Console.Write(featurePadding + title);
            System.Console.ResetColor();
            System.Console.ForegroundColor = ConsoleColor.Gray;
            System.Console.WriteLine($" - {description}");
            System.Console.ResetColor();
            
            await Task.Delay(120);
        }

        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.Green;
        CenterText("✨ SmartRAG is ready to serve! ✨");
        System.Console.ResetColor();
        System.Console.WriteLine();
    }

    #endregion
}



