using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MdView.Services;

namespace MdView.Views;

public partial class PreferencesWindow : Window
{
    public PreferencesWindow()
    {
        InitializeComponent();
        Title = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "Preferences" : "Settings";
        UpdateEditorDisplay();
    }

    private void UpdateEditorDisplay()
    {
        var path = PreferencesService.Instance.DefaultEditorPath;
        if (path != null && (File.Exists(path) || Directory.Exists(path)))
        {
            EditorName.Text = GetEditorDisplayName(path);
            ClearButton.IsVisible = true;
        }
        else
        {
            EditorName.Text = "None";
            EditorIcon.Source = null;
            ClearButton.IsVisible = false;

            if (path != null)
                PreferencesService.Instance.DefaultEditorPath = null;
        }
    }

    private static string GetEditorDisplayName(string path)
    {
        if (OperatingSystem.IsWindows() && File.Exists(path))
        {
            var info = FileVersionInfo.GetVersionInfo(path);
            if (!string.IsNullOrEmpty(info.FileDescription))
                return info.FileDescription;
        }

        return Path.GetFileNameWithoutExtension(path);
    }

    private async void OnChangeClick(object? sender, RoutedEventArgs e)
    {
        if (OperatingSystem.IsMacOS())
        {
            await PickEditorFromListAsync();
        }
        else
        {
            await BrowseForEditorAsync();
        }
    }

    private async Task PickEditorFromListAsync()
    {
        var picker = new EditorPickerWindow();
        _ = picker.LoadEditorsAsync();
        await picker.ShowDialog(this);

        if (picker.SelectedEditor != null)
        {
            PreferencesService.Instance.DefaultEditorPath = picker.SelectedEditor.ExecutablePath;
            EditorName.Text = picker.SelectedEditor.Name;
            EditorIcon.Source = picker.SelectedEditor.Icon;
            ClearButton.IsVisible = true;
        }
    }

    private async Task BrowseForEditorAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Editor",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Applications") { Patterns = ["*.exe"] },
                FilePickerFileTypes.All
            ]
        });

        if (files.Count > 0)
        {
            var path = files[0].Path.LocalPath;
            PreferencesService.Instance.DefaultEditorPath = path;
            EditorName.Text = GetEditorDisplayName(path);
            EditorIcon.Source = null;
            ClearButton.IsVisible = true;
        }
    }

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        PreferencesService.Instance.DefaultEditorPath = null;
        EditorName.Text = "None";
        EditorIcon.Source = null;
        ClearButton.IsVisible = false;
    }
}
