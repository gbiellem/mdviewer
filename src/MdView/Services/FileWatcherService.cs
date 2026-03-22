namespace MdView.Services;

public class FileWatcherService : IDisposable
{
    private FileSystemWatcher? _watcher;
    private readonly System.Timers.Timer _debounceTimer;
    private readonly System.Timers.Timer _pollTimer;
    private string? _pendingPath;
    private string? _watchedFile;
    private DateTime _lastKnownWrite;

    public event Action<string>? FileChanged;

    public FileWatcherService()
    {
        _debounceTimer = new System.Timers.Timer(300);
        _debounceTimer.AutoReset = false;
        _debounceTimer.Elapsed += (_, _) =>
        {
            if (_pendingPath is { } path)
                FileChanged?.Invoke(path);
        };

        // Poll as fallback for editors that do atomic save (write+rename)
        _pollTimer = new System.Timers.Timer(1000);
        _pollTimer.AutoReset = true;
        _pollTimer.Elapsed += OnPollTimerElapsed;
    }

    public void Watch(string filePath)
    {
        _watcher?.Dispose();
        _watchedFile = filePath;
        _lastKnownWrite = File.GetLastWriteTimeUtc(filePath);

        var directory = Path.GetDirectoryName(filePath)!;
        var fileName = Path.GetFileName(filePath);

        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                         | NotifyFilters.FileName | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnFsEvent;
        _watcher.Created += OnFsEvent;
        _watcher.Renamed += (_, e) =>
        {
            if (e.FullPath == filePath)
                OnFsEvent(null, e);
        };

        _pollTimer.Start();
    }

    private void OnFsEvent(object? sender, FileSystemEventArgs e)
    {
        _pendingPath = _watchedFile;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void OnPollTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_watchedFile == null || !File.Exists(_watchedFile)) return;

        var currentWrite = File.GetLastWriteTimeUtc(_watchedFile);
        if (currentWrite > _lastKnownWrite)
        {
            _lastKnownWrite = currentWrite;
            _pendingPath = _watchedFile;
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _debounceTimer.Dispose();
        _pollTimer.Dispose();
    }
}
