using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
            // Get the editor name and icon
            _ = LoadEditorInfoAsync(path);
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

    private async Task LoadEditorInfoAsync(string path)
    {
        var editors = await EditorDetectionService.DetectEditorsAsync();
        var match = editors.FirstOrDefault(e => e.ExecutablePath == path);
        if (match != null)
        {
            EditorName.Text = match.Name;
            EditorIcon.Source = match.Icon;
        }
        else
        {
            EditorName.Text = Path.GetFileNameWithoutExtension(path);
            EditorIcon.Source = null;
        }
    }

    private async void OnChangeClick(object? sender, RoutedEventArgs e)
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

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        PreferencesService.Instance.DefaultEditorPath = null;
        EditorName.Text = "None";
        EditorIcon.Source = null;
        ClearButton.IsVisible = false;
    }
}
