using System.Runtime.InteropServices;

namespace SmartRAG.Demo.Services.Console;

/// <summary>
/// Helper class for console window operations
/// </summary>
public static class ConsoleHelper
{
    #region Windows API

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private const int SW_MAXIMIZE = 3;

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets console window to centered position with optimal size for animations
    /// </summary>
    public static void SetOptimalWindowSize()
    {
        if (!IsConsoleAvailable())
        {
            return;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Set optimal size for animations (100 columns x 35 rows)
                const int width = 100;
                const int height = 35;
                
                try
                {
                    System.Console.SetWindowSize(width, height);
                    System.Console.SetBufferSize(width, height);
                }
                catch
                {
                    // If that fails, try smaller size
                    System.Console.SetWindowSize(80, 30);
                    System.Console.SetBufferSize(80, 30);
                }
                
                // Center the window on screen
                CenterConsoleWindow();
            }
            // Note: SetBufferSize is Windows-specific
            // Linux/macOS terminals handle buffer size automatically
        }
        catch
        {
            // Ignore errors - not critical
        }
    }
    
    /// <summary>
    /// Centers the console window on screen (Windows only)
    /// </summary>
    private static void CenterConsoleWindow()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var handle = GetConsoleWindow();
                if (handle != IntPtr.Zero)
                {
                    // Get screen dimensions
                    var screenWidth = GetSystemMetrics(0);  // SM_CXSCREEN
                    var screenHeight = GetSystemMetrics(1); // SM_CYSCREEN
                    
                    // Get window dimensions
                    if (GetWindowRect(handle, out var rect))
                    {
                        var windowWidth = rect.Right - rect.Left;
                        var windowHeight = rect.Bottom - rect.Top;
                        
                        // Calculate center position
                        var x = (screenWidth - windowWidth) / 2;
                        var y = (screenHeight - windowHeight) / 2;
                        
                        // Move window to center
                        SetWindowPos(handle, IntPtr.Zero, x, y, 0, 0, 0x0001); // SWP_NOSIZE
                    }
                }
            }
        }
        catch
        {
            // Ignore errors - not critical
        }
    }

    /// <summary>
    /// Sets optimal console settings for animations
    /// </summary>
    public static void ConfigureForAnimations()
    {
        if (!IsConsoleAvailable())
        {
            return;
        }

        try
        {
            System.Console.CursorVisible = false;
            System.Console.Clear();
            
            // Set UTF-8 encoding for special characters
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            // Set optimal centered window
            SetOptimalWindowSize();
        }
        catch
        {
            // Ignore errors - not critical
        }
    }

    /// <summary>
    /// Resets console to normal state after animations
    /// </summary>
    public static void ResetCursor()
    {
        if (!IsConsoleAvailable())
        {
            return;
        }

        try
        {
            System.Console.CursorVisible = true;
            
            // Restore buffer size to a larger value to prevent text from disappearing
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    // Set a very large buffer size so scrollback history is preserved
                    var currentWidth = System.Console.WindowWidth;
                    var newHeight = 9999; // Maximum buffer for scrollback (prevents content from being cleared)
                    
                    // Only set if current buffer is smaller
                    if (System.Console.BufferHeight < newHeight)
                    {
                        System.Console.SetBufferSize(currentWidth, newHeight);
                    }
                }
                catch
                {
                    // Ignore if we can't change buffer size
                }
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    /// <summary>
    /// Indicates whether a console window is available for interactive operations.
    /// </summary>
    public static bool IsConsoleAvailable()
    {
        if (!Environment.UserInteractive)
        {
            return false;
        }

        try
        {
            _ = System.Console.WindowWidth;
            _ = System.Console.WindowHeight;
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
    }

    #endregion
}

