using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using MdView.Models;
using MdView.Services;

namespace MdView.Views;

public partial class EditorPickerWindow : Window
{
    public EditorInfo? SelectedEditor { get; private set; }

    public EditorPickerWindow()
    {
        InitializeComponent();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Apply macOS-style item theme (defined in XAML resources)
            if (Resources.TryGetResource("MacListBoxItem", ActualThemeVariant, out var theme)
                && theme is ControlTheme controlTheme)
            {
                EditorList.ItemContainerTheme = controlTheme;
            }
        }
    }

    public async Task LoadEditorsAsync()
    {
        var editors = await EditorDetectionService.DetectEditorsAsync();

        LoadingText.IsVisible = false;
        EditorList.IsVisible = true;
        EditorList.ItemsSource = editors;

        if (editors.Count == 0)
        {
            LoadingText.Text = "No text editors found.";
            LoadingText.IsVisible = true;
            EditorList.IsVisible = false;
        }
    }

    private void OnEditorSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectButton.IsEnabled = EditorList.SelectedItem != null;
    }

    private void OnEditorDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (EditorList.SelectedItem is EditorInfo editor)
        {
            SelectedEditor = editor;
            Close();
        }
    }

    private void OnSelectClick(object? sender, RoutedEventArgs e)
    {
        if (EditorList.SelectedItem is EditorInfo editor)
        {
            SelectedEditor = editor;
            Close();
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        SelectedEditor = null;
        Close();
    }
}
