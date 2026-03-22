using CommunityToolkit.Mvvm.ComponentModel;
using MdView.Services;

namespace MdView.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly MarkdownService _markdownService = new();
    private readonly FileWatcherService _fileWatcher = new();

    [ObservableProperty]
    private string _currentFilePath = string.Empty;

    [ObservableProperty]
    private string _windowTitle = "MdView";

    [ObservableProperty]
    private string _htmlContent = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private bool _hasFile;

    [ObservableProperty]
    private bool _isXpsAvailable = OperatingSystem.IsWindows();

    [ObservableProperty]
    private bool _isDarkMode;

    private string _currentMarkdown = string.Empty;

    public MainWindowViewModel()
    {
        _fileWatcher.FileChanged += OnFileChanged;
    }

    public void LoadFile(string filePath)
    {
        if (!File.Exists(filePath)) return;

        CurrentFilePath = filePath;
        WindowTitle = $"{Path.GetFileName(filePath)} - MdView";
        HasFile = true;

        _currentMarkdown = File.ReadAllText(filePath);
        RenderMarkdown();

        _fileWatcher.Watch(filePath);
        StatusText = $"Loaded: {Path.GetFileName(filePath)}";
    }

    private void RenderMarkdown()
    {
        HtmlContent = _markdownService.BuildFullHtml(_currentMarkdown, IsDarkMode);
    }

    private void OnFileChanged(string filePath)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            try
            {
                _currentMarkdown = File.ReadAllText(filePath);
                RenderMarkdown();
                StatusText = $"Reloaded: {Path.GetFileName(filePath)}";
            }
            catch
            {
                // File might be locked during write
            }
        });
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        if (!string.IsNullOrEmpty(_currentMarkdown))
            RenderMarkdown();
    }

    public void LoadFromArgs(string[] args)
    {
        var filePath = args.FirstOrDefault(a => !a.StartsWith('-') && File.Exists(a));
        if (filePath != null)
            LoadFile(Path.GetFullPath(filePath));
    }
}
