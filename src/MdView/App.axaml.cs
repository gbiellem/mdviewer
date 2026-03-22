using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MdView.ViewModels;
using MdView.Views;

namespace MdView;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private MainWindowViewModel? _vm;
    private string? _pendingFilePath;
    private bool _windowReady;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            _vm = new MainWindowViewModel();
            _mainWindow = new MainWindow
            {
                DataContext = _vm,
            };

            desktop.MainWindow = _mainWindow;
            _mainWindow.Opened += OnMainWindowOpened;

            // Store CLI args for deferred loading
            if (desktop.Args is { Length: > 0 })
            {
                var filePath = desktop.Args.FirstOrDefault(a => !a.StartsWith('-') && File.Exists(a));
                if (filePath != null)
                    _pendingFilePath = Path.GetFullPath(filePath);
            }

            // Handle macOS "Open With" / Finder file activation events
            if (desktop is IActivatableLifetime activatable)
            {
                activatable.Activated += OnActivated;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnMainWindowOpened(object? sender, EventArgs e)
    {
        _windowReady = true;

        if (_pendingFilePath != null)
        {
            var path = _pendingFilePath;
            _pendingFilePath = null;
            _vm?.LoadFile(path);
        }
    }

    private void LoadFileFromExternalEvent(string path)
    {
        if (_windowReady && _vm != null)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => _vm.LoadFile(path));
        }
        else
        {
            _pendingFilePath = path;
        }
    }

    private void OnActivated(object? sender, ActivatedEventArgs args)
    {
        if (args is FileActivatedEventArgs fileArgs)
        {
            foreach (var file in fileArgs.Files)
            {
                var path = file.Path.LocalPath;
                if (File.Exists(path))
                {
                    LoadFileFromExternalEvent(path);
                    break;
                }
            }
        }
        else if (args is ProtocolActivatedEventArgs protoArgs)
        {
            var uri = protoArgs.Uri;
            if (uri.IsFile && File.Exists(uri.LocalPath))
            {
                LoadFileFromExternalEvent(uri.LocalPath);
            }
        }
    }

    private void OnAboutClick(object? sender, EventArgs e) => ShowAboutWindow();

    private void OnPreferencesClick(object? sender, EventArgs e) => ShowPreferencesWindow();

    public static void ShowPreferencesWindow()
    {
        var prefsWindow = new PreferencesWindow();

        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is not null)
        {
            prefsWindow.ShowDialog(desktop.MainWindow);
        }
        else
        {
            prefsWindow.Show();
        }
    }

    public static void ShowAboutWindow()
    {
        var aboutWindow = new AboutWindow();

        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is not null)
        {
            aboutWindow.ShowDialog(desktop.MainWindow);
        }
        else
        {
            aboutWindow.Show();
        }
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        // In Avalonia 12, data annotation validation is no longer a plugin-based system.
        // This method is kept as a no-op for future reference.
    }
}
