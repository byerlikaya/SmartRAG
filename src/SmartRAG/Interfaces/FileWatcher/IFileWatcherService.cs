using System;
using System.Threading.Tasks;
using SmartRAG.Services.FileWatcher.Events;
using SmartRAG.Models;

namespace SmartRAG.Interfaces.FileWatcher
{
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

        /// <summary>
        /// Stops watching all folders
        /// </summary>
        /// <returns>Task representing the stop operation</returns>
        Task StopAllWatchingAsync();

        /// <summary>
        /// Event raised when a file is created
        /// </summary>
        event EventHandler<FileWatcherEventArgs> FileCreated;

        /// <summary>
        /// Event raised when a file is changed
        /// </summary>
        event EventHandler<FileWatcherEventArgs> FileChanged;

        /// <summary>
        /// Event raised when a file is deleted
        /// </summary>
        event EventHandler<FileWatcherEventArgs> FileDeleted;
    }
}
