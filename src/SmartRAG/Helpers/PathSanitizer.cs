using System.IO;

namespace SmartRAG.Helpers;


/// <summary>
/// Utility class for sanitizing file paths to prevent path traversal attacks
/// </summary>
public static class PathSanitizer
{
    /// <summary>
    /// Sanitizes a file path and validates it's within allowed base directory
    /// </summary>
    /// <param name="folderPath">Path to sanitize</param>
    /// <param name="baseDirectory">Base directory that the path must be within</param>
    /// <returns>Sanitized full path</returns>
    /// <exception cref="ArgumentException">Thrown when folder path is null or empty</exception>
    /// <exception cref="System.Security.SecurityException">Thrown when path traversal is detected</exception>
    public static string SanitizePath(string folderPath, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path cannot be null or empty", nameof(folderPath));

        if (string.IsNullOrWhiteSpace(baseDirectory))
            throw new ArgumentException("Base directory cannot be null or empty", nameof(baseDirectory));

        if (folderPath.Contains(".."))
            throw new System.Security.SecurityException("Path traversal detected: '..' is not allowed");

        var baseDir = Path.GetFullPath(baseDirectory);

        string fullPath;
        string effectiveBaseDir;

        if (Path.IsPathRooted(folderPath))
        {
            fullPath = Path.GetFullPath(folderPath);

            var userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userHomeDir))
            {
                var normalizedUserHome = Path.GetFullPath(userHomeDir);
                var normalizedPath = Path.GetFullPath(fullPath);

                effectiveBaseDir = normalizedPath.StartsWith(normalizedUserHome, StringComparison.OrdinalIgnoreCase) ? 
                    normalizedUserHome : 
                    Path.GetPathRoot(fullPath);
            }
            else
            {
                effectiveBaseDir = Path.GetPathRoot(fullPath);
            }
        }
        else
        {
            var normalizedPath = folderPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            fullPath = Path.GetFullPath(Path.Combine(baseDir, normalizedPath));
            effectiveBaseDir = baseDir;
        }

        var normalizedBaseDir = Path.GetFullPath(effectiveBaseDir);
        var normalizedFullPath = Path.GetFullPath(fullPath);

        return !normalizedFullPath.StartsWith(normalizedBaseDir, StringComparison.OrdinalIgnoreCase) ? 
            throw new System.Security.SecurityException("Path traversal detected") : 
            normalizedFullPath;
    }
}


