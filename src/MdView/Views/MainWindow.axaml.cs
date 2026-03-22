using System.Runtime.InteropServices;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MdView.Services;
using MdView.ViewModels;

namespace MdView.Views;

public partial class MainWindow : Window
{
    private bool _webViewReady;
    private string? _pendingHtml;
    private string? _tempHtmlPath;

    public MainWindow()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            WindowMenu.IsVisible = false;

            // Register keyboard shortcuts since hidden menu HotKeys are inactive
            KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.O, KeyModifiers.Meta), Command = new ActionCommand(async () => await OpenFileAsync()) });
            KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.E, KeyModifiers.Meta | KeyModifiers.Shift), Command = new ActionCommand(OpenInEditor) });
            KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.P, KeyModifiers.Meta), Command = new ActionCommand(PrintContent) });
            KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.E, KeyModifiers.Meta), Command = new ActionCommand(async () => await ExportPdfAsync()) });
            KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.OemPlus, KeyModifiers.Meta), Command = new ActionCommand(ZoomIn) });
            KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.OemMinus, KeyModifiers.Meta), Command = new ActionCommand(ZoomOut) });
            KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.D0, KeyModifiers.Meta), Command = new ActionCommand(ResetZoom) });
        }

        WebView.AdapterCreated += (_, _) =>
        {
            _webViewReady = true;
            if (_pendingHtml != null)
            {
                var html = _pendingHtml;
                _pendingHtml = null;
                Dispatcher.UIThread.Post(() => NavigateToHtml(html));
            }
        };
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is MainWindowViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.HtmlContent))
                {
                    Dispatcher.UIThread.Post(() => LoadHtmlInWebView(vm.HtmlContent));
                }
            };

            if (!string.IsNullOrEmpty(vm.HtmlContent))
            {
                Dispatcher.UIThread.Post(() => LoadHtmlInWebView(vm.HtmlContent));
            }
        }
    }

    private void LoadHtmlInWebView(string html)
    {
        if (string.IsNullOrEmpty(html)) return;

        if (_webViewReady)
        {
            NavigateToHtml(html);
        }
        else
        {
            _pendingHtml = html;
        }
    }

    private void NavigateToHtml(string html)
    {
        // Write HTML to a temp file and navigate to it so that file:// script
        // references (e.g. mermaid.min.js) work correctly. NavigateToString
        // uses an about:blank origin that blocks local file access.
        _tempHtmlPath ??= Path.Combine(Path.GetTempPath(), $"mdview_{Environment.ProcessId}.html");
        File.WriteAllText(_tempHtmlPath, html);
        WebView.Navigate(new Uri(_tempHtmlPath));
    }

    // --- File open handlers ---

    private async void OnOpenClick(object? sender, RoutedEventArgs e) => await OpenFileAsync();

    private async void OnNativeOpenClick(object? sender, EventArgs e) => await OpenFileAsync();

    private async Task OpenFileAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Markdown File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Markdown Files")
                {
                    Patterns = ["*.md", "*.markdown", "*.mdown", "*.mkd", "*.mkdn"]
                },
                FilePickerFileTypes.All
            ]
        });

        if (files.Count > 0 && DataContext is MainWindowViewModel vm)
        {
            vm.LoadFile(files[0].Path.LocalPath);
        }
    }

    // --- Print handler ---

    private void OnPrintClick(object? sender, RoutedEventArgs e) => PrintContent();

    private void OnNativePrintClick(object? sender, EventArgs e) => PrintContent();

    private void PrintContent()
    {
        if (DataContext is not MainWindowViewModel vm || !vm.HasFile) return;
        if (_webViewReady)
        {
            WebView.ShowPrintUI();
        }
    }

    // --- Export handlers ---

    private async void OnExportPdfClick(object? sender, RoutedEventArgs e) => await ExportPdfAsync();

    private async void OnNativeExportPdfClick(object? sender, EventArgs e) => await ExportPdfAsync();

    private async Task ExportPdfAsync()
    {
        if (DataContext is not MainWindowViewModel vm || !vm.HasFile) return;

        var defaultName = Path.GetFileNameWithoutExtension(vm.CurrentFilePath) + ".pdf";
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export to PDF",
            SuggestedFileName = defaultName,
            DefaultExtension = "pdf",
            FileTypeChoices =
            [
                new FilePickerFileType("PDF Documents") { Patterns = ["*.pdf"] }
            ]
        });

        if (file == null) return;

        vm.IsExporting = true;
        try
        {
            var progress = new Progress<string>(msg => vm.StatusText = msg);
            await ExportService.ExportToPdfAsync(WebView, file.Path.LocalPath, progress);
        }
        catch (Exception ex)
        {
            vm.StatusText = $"Export failed: {ex.Message}";
        }
        finally
        {
            vm.IsExporting = false;
        }
    }

    // --- Editor handlers ---

    private void OnOpenInEditorClick(object? sender, RoutedEventArgs e) => OpenInEditor();

    private void OnNativeOpenInEditorClick(object? sender, EventArgs e) => OpenInEditor();

    private void OpenInEditor()
    {
        if (DataContext is not MainWindowViewModel vm || !vm.HasFile) return;

        var defaultPath = PreferencesService.Instance.DefaultEditorPath;
        if (defaultPath != null && (File.Exists(defaultPath) || Directory.Exists(defaultPath)))
        {
            EditorDetectionService.LaunchEditor(defaultPath, vm.CurrentFilePath);
            vm.StatusText = "Opened in editor";
        }
        else
        {
            if (defaultPath != null)
                PreferencesService.Instance.DefaultEditorPath = null;
            vm.StatusText = "No default editor set. Use Settings > Set Default Editor.";
        }
    }

    private void OnPreferencesClick(object? sender, RoutedEventArgs e) => App.ShowPreferencesWindow();

    // --- Other handlers ---

    private void OnAboutClick(object? sender, RoutedEventArgs e) => App.ShowAboutWindow();

    private void OnZoomIn(object? sender, RoutedEventArgs e) => ZoomIn();
    private void OnNativeZoomIn(object? sender, EventArgs e) => ZoomIn();
    private void ZoomIn()
    {
        if (_webViewReady) _ = WebView.InvokeScript("setZoom(getZoom() + 0.1)");
    }

    private void OnZoomOut(object? sender, RoutedEventArgs e) => ZoomOut();
    private void OnNativeZoomOut(object? sender, EventArgs e) => ZoomOut();
    private void ZoomOut()
    {
        if (_webViewReady) _ = WebView.InvokeScript("setZoom(getZoom() - 0.1)");
    }

    private void OnZoomReset(object? sender, RoutedEventArgs e) => ResetZoom();
    private void OnNativeZoomReset(object? sender, EventArgs e) => ResetZoom();
    private void ResetZoom()
    {
        if (_webViewReady) _ = WebView.InvokeScript("setZoom(1.0)");
    }

    private void OnExitClick(object? sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (_tempHtmlPath != null)
        {
            try { File.Delete(_tempHtmlPath); } catch { }
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (!e.DataTransfer.Contains(DataFormat.File)) return;
        var files = e.DataTransfer.TryGetFiles();
        if (files == null) return;

        foreach (var file in files)
        {
            var path = file.Path.LocalPath;
            if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase))
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.LoadFile(path);
                    break;
                }
            }
        }
    }

    private sealed class ActionCommand(Action action) : ICommand
    {
#pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => action();
    }
}
