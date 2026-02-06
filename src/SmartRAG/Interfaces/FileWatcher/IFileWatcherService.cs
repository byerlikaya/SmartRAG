using System;
using System.Threading.Tasks;
using SmartRAG.Models;

namespace SmartRAG.Interfaces.FileWatcher;


/// <summary>
/// Interface for file system watching and automatic document indexing
/// </summary>
public interface IFileWatcherService : IDisposable
{
    /// <summary>
    /// Starts watching a folder for file changes
    /// </summary>
    /// <param name="config">Folder watch configuration</param>
    /// <returns>Task representing the watch operation</returns>
    Task StartWatchingAsync(WatchedFolderConfig config);
}

