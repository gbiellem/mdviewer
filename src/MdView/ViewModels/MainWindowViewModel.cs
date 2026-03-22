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

    private string _currentMarkdown = string.Empty;

    public MainWindowViewModel()
    {
        _fileWatcher.FileChanged += OnFileChanged;
        ShowWelcomePage();
    }

    private void ShowWelcomePage()
    {
        var shortcut = OperatingSystem.IsMacOS() ? "⌘O" : "Ctrl+O";
        var welcomeHtml = $"""
            <div style="display:flex; flex-direction:column; align-items:center; justify-content:center;
                        min-height:60vh; text-align:center; opacity:0.6;">
                <div style="font-size:48px; margin-bottom:16px;">📄</div>
                <h2 style="border:none; margin:0 0 8px;">Welcome to MdView</h2>
                <p style="margin:0;">Open a Markdown file to get started</p>
                <p style="margin:8px 0 0; font-size:13px;">
                    Press <strong>{shortcut}</strong> or drag and drop a <code>.md</code> file
                </p>
            </div>
            """;
        HtmlContent = HtmlTemplate.Wrap(welcomeHtml);
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
        HtmlContent = _markdownService.BuildFullHtml(_currentMarkdown);
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

    public void LoadFromArgs(string[] args)
    {
        var filePath = args.FirstOrDefault(a => !a.StartsWith('-') && File.Exists(a));
        if (filePath != null)
            LoadFile(Path.GetFullPath(filePath));
    }
}
