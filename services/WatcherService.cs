using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SaveScribe.Services;

public class WatcherService : IDisposable
{
    private FileSystemWatcher? _watcher;
    private readonly ConcurrentDictionary<string, DateTime> _lastChangedFiles = new();
    private readonly TimeSpan _debounceTime = TimeSpan.FromSeconds(2);

    public event Action<string>? OnSaveDetected;

    public void StartMonitoring(string directoryPath)
    {
        StopMonitoring();

        if (!Directory.Exists(directoryPath)) return;

        // Use the native, high-performance Windows OS file event buffer
        _watcher = new FileSystemWatcher(directoryPath)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnFileEventOccurred;
        _watcher.Created += OnFileEventOccurred;
    }

    private void OnFileEventOccurred(object sender, FileSystemEventArgs e)
    {
        string filePath = e.FullPath;
        DateTime now = DateTime.UtcNow;

        // Smart Debounce: Prevents game engine autosave multi-writes from triggering 20 backups at once
        _lastChangedFiles.AddOrUpdate(filePath, now, (_, _) => now);

        // Defer execution safely to a background thread pool task
        Task.Run(async () =>
        {
            await Task.Delay(_debounceTime);

            if (_lastChangedFiles.TryGetValue(filePath, out var lastWriteTime) && lastWriteTime == now)
            {
                _lastChangedFiles.TryRemove(filePath, out _);
                
                // Fire notification back to the UI framework safely
                OnSaveDetected?.Invoke(filePath);
            }
        });
    }

    public void StopMonitoring()
    {
        if (_watcher == null) return;
        
        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnFileEventOccurred;
        _watcher.Created -= OnFileEventOccurred;
        _watcher.Dispose();
        _watcher = null;
    }

    public void Dispose() => StopMonitoring();
}