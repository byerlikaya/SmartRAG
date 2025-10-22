using SmartRAG.Demo.Services.Console;

namespace SmartRAG.Demo.Services.Menu;

/// <summary>
/// Service for displaying and managing menus
/// </summary>
public class MenuService(IConsoleService console) : IMenuService
{
    #region Constants

    private const string WelcomeBanner = @"╔═══════════════════════════════════════════════════════════════════╗
║   SmartRAG Demo - Deployment Flexible RAG System                  ║
╚═══════════════════════════════════════════════════════════════════╝";

    #endregion

    #region Public Methods

    public void ShowWelcomeBanner()
    {
        System.Console.WriteLine(WelcomeBanner);
        System.Console.WriteLine();
    }

    public void ShowMainMenu()
    {
        console.WriteSectionHeader("📋 MAIN MENU");
        System.Console.WriteLine("1.  🔗 Show Database Connections");
        System.Console.WriteLine("2.  🔧 System Health Check");
        System.Console.WriteLine("3.  🗄️ Create SQL Server Test Database");
        System.Console.WriteLine("4.  🐬 Create MySQL Test Database");
        System.Console.WriteLine("5.  🐘 Create PostgreSQL Test Database");
        System.Console.WriteLine("6.  📊 Show Database Schemas");
        System.Console.WriteLine("7.  🔬 Query Analysis (SQL Generation)");
        System.Console.WriteLine("8.  🧪 Automatic Test Queries");
        System.Console.WriteLine("9.  🤖 Multi-Database Query (AI)");
        System.Console.WriteLine("10. 🤖 Setup Ollama Models");
        System.Console.WriteLine("11. 📦 Test Vector Store (InMemory/FileSystem/Redis/SQLite/Qdrant)");
        System.Console.WriteLine("12. 📄 Upload Documents (PDF, Word, Excel, Images, Audio)");
        System.Console.WriteLine("13. 📚 List Uploaded Documents");
        System.Console.WriteLine("14. 🎯 Multi-Modal RAG (Documents + Databases)");
        System.Console.WriteLine("15. 🗑️ Clear All Documents");
        System.Console.WriteLine("0.  🚪 Exit");
        System.Console.WriteLine();
    }

    public Task<string?> GetMenuChoiceAsync()
    {
        var choice = console.ReadLine("Selection: ");
        return Task.FromResult(choice);
    }

    public void PauseForUser()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("Press Enter to continue...");
        System.Console.ReadLine();
    }

    #endregion
}

