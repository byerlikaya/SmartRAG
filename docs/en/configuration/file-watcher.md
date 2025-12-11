---
layout: default
title: File Watcher Configuration
description: Configure automatic document indexing from watched folders
lang: en
redirect_from: /en/configuration/file-watcher.html
---

# File Watcher Configuration

SmartRAG can automatically monitor folders for new documents and index them automatically, eliminating the need for manual document uploads.

## Overview

The File Watcher feature allows SmartRAG to:
- Monitor specified folders for file changes
- Automatically detect new, modified, or deleted files
- Automatically upload and index new documents
- Support multiple watched folders simultaneously

## Configuration

### Enable File Watcher

Add the following to your `appsettings.json`:

```json
{
  "SmartRAG": {
    "EnableFileWatcher": true,
    "WatchedFolders": [
      {
        "FolderPath": "/path/to/documents",
        "AllowedExtensions": [".pdf", ".docx", ".txt"],
        "IncludeSubdirectories": true,
        "AutoUpload": true,
        "UserId": "system",
        "Language": "en"
      }
    ]
  }
}
```

### Configuration Properties

#### EnableFileWatcher

- **Type**: `bool`
- **Default**: `true`
- **Description**: Enables or disables File Watcher functionality

#### WatchedFolders

- **Type**: `List<WatchedFolderConfig>`
- **Default**: Empty list
- **Description**: List of folder configurations to watch

#### WatchedFolderConfig Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `FolderPath` | `string` | Yes | Absolute or relative path to the folder to watch |
| `AllowedExtensions` | `List<string>` | No | List of allowed file extensions (e.g., `[".pdf", ".docx"]`). If empty, all supported file types are allowed |
| `IncludeSubdirectories` | `bool` | No | Whether to watch subdirectories (default: `true`) |
| `AutoUpload` | `bool` | No | Whether to automatically upload new files (default: `true`) |
| `UserId` | `string` | No | User ID for document ownership (default: `"system"`) |
| `Language` | `string` | No | Language code for document processing (optional) |

## Programmatic Configuration

You can also configure watched folders programmatically:

```csharp
services.AddSmartRag(configuration, options =>
{
    options.EnableFileWatcher = true;
    options.WatchedFolders.Add(new WatchedFolderConfig
    {
        FolderPath = "/path/to/documents",
        AllowedExtensions = new List<string> { ".pdf", ".docx", ".txt" },
        IncludeSubdirectories = true,
        AutoUpload = true,
        UserId = "system",
        Language = "en"
    });
});
```

## Initialization

After building the service provider, initialize file watchers:

```csharp
var serviceProvider = services.BuildServiceProvider();
await serviceProvider.InitializeSmartRagAsync();
```

This will automatically start watching all configured folders.

## Supported File Types

File Watcher supports all file types that SmartRAG can parse:
- **Documents**: `.pdf`, `.docx`, `.txt`, `.xlsx`
- **Images**: `.jpg`, `.png`, `.gif`, `.bmp` (with OCR)
- **Audio**: `.mp3`, `.wav`, `.m4a`, `.flac` (with transcription)

If `AllowedExtensions` is empty, all supported file types are allowed.

## Security

File Watcher includes built-in security features:
- **Path Traversal Prevention**: All paths are sanitized to prevent directory traversal attacks
- **Base Directory Validation**: Watched folders must be within the application's base directory
- **File Extension Validation**: Only allowed file extensions are processed

## Events

File Watcher raises events for file operations:

```csharp
var fileWatcher = serviceProvider.GetRequiredService<IFileWatcherService>();

fileWatcher.FileCreated += (sender, e) =>
{
    Console.WriteLine($"File created: {e.FileName} at {e.FilePath}");
};

fileWatcher.FileChanged += (sender, e) =>
{
    Console.WriteLine($"File changed: {e.FileName} at {e.FilePath}");
};

fileWatcher.FileDeleted += (sender, e) =>
{
    Console.WriteLine($"File deleted: {e.FileName} at {e.FilePath}");
};
```

## Manual Control

You can also manually control file watchers:

```csharp
var fileWatcher = serviceProvider.GetRequiredService<IFileWatcherService>();

// Start watching a folder
await fileWatcher.StartWatchingAsync(new WatchedFolderConfig
{
    FolderPath = "/path/to/folder",
    AutoUpload = true
});

// Stop watching a folder
await fileWatcher.StopWatchingAsync("/path/to/folder");

// Stop all watchers
await fileWatcher.StopAllWatchingAsync();

// Get list of watched folders
var watchedFolders = fileWatcher.GetWatchedFolders();
```

## Troubleshooting

### Files Not Being Indexed

If files are not being automatically indexed:
- Verify the folder path exists and is accessible
- Check that `AutoUpload` is set to `true`
- Ensure file extensions are in the `AllowedExtensions` list (or list is empty)
- Check application logs for errors

### Path Traversal Errors

If you encounter path traversal errors:
- Use absolute paths instead of relative paths
- Ensure paths are within the application's base directory
- Avoid using `..` in folder paths

### Performance Considerations

- Watching many folders or large directory trees may impact performance
- Consider using `IncludeSubdirectories: false` for better performance
- Limit `AllowedExtensions` to only needed file types

## Related Documentation

- [MCP Client Configuration]({{ site.baseurl }}/en/configuration/mcp-client/)
- [Getting Started]({{ site.baseurl }}/en/getting-started/)
- [API Reference]({{ site.baseurl }}/en/api-reference/)


