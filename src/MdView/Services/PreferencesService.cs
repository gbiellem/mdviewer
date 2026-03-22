using System.Text.Json;

namespace MdView.Services;

public class PreferencesService
{
    private static readonly Lazy<PreferencesService> _instance = new(() => new PreferencesService());
    public static PreferencesService Instance => _instance.Value;

    private readonly string _filePath;
    private PreferencesData _data = new();

    private PreferencesService()
    {
        var appData = OperatingSystem.IsMacOS()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MdView")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MdView");

        Directory.CreateDirectory(appData);
        _filePath = Path.Combine(appData, "preferences.json");
        Load();
    }

    public string? DefaultEditorPath
    {
        get => _data.DefaultEditorPath;
        set
        {
            _data.DefaultEditorPath = value;
            Save();
        }
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _data = JsonSerializer.Deserialize<PreferencesData>(json) ?? new();
            }
        }
        catch
        {
            _data = new();
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Silently fail if unable to save
        }
    }

    private class PreferencesData
    {
        public string? DefaultEditorPath { get; set; }
    }
}
