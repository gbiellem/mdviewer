using System.Runtime.InteropServices;
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

    public MainWindow()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            WindowMenu.IsVisible = false;
        }

        WebView.AdapterCreated += (_, _) =>
        {
            _webViewReady = true;
            if (_pendingHtml != null)
            {
                var html = _pendingHtml;
                _pendingHtml = null;
                Dispatcher.UIThread.Post(() => WebView.NavigateToString(html));
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
            WebView.NavigateToString(html);
        }
        else
        {
            _pendingHtml = html;
        }
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

    private async void OnExportXpsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || !vm.HasFile) return;

        var defaultName = Path.GetFileNameWithoutExtension(vm.CurrentFilePath) + ".xps";
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export to XPS",
            SuggestedFileName = defaultName,
            DefaultExtension = "xps",
            FileTypeChoices =
            [
                new FilePickerFileType("XPS Documents") { Patterns = ["*.xps"] }
            ]
        });

        if (file == null) return;

        vm.IsExporting = true;
        try
        {
            var progress = new Progress<string>(msg => vm.StatusText = msg);
            await ExportService.ExportToXpsAsync(WebView, file.Path.LocalPath, progress);
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

    // --- Other handlers ---

    private void OnAboutClick(object? sender, RoutedEventArgs e) => App.ShowAboutWindow();

    private void OnToggleDarkMode(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.IsDarkMode = !vm.IsDarkMode;
    }

    private void OnExitClick(object? sender, RoutedEventArgs e) => Close();

    private void OnDragOver(object? sender, DragEventArgs e)
    {
#pragma warning disable CS0618
        e.DragEffects = e.Data.Contains(DataFormats.Files)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
#pragma warning restore CS0618
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
#pragma warning disable CS0618
        if (!e.Data.Contains(DataFormats.Files)) return;
        var files = e.Data.GetFiles();
#pragma warning restore CS0618
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
}
