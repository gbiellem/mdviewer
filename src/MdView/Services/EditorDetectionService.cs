using System.Diagnostics;
using Avalonia.Media.Imaging;
using MdView.Models;

namespace MdView.Services;

public static class EditorDetectionService
{
    private static readonly (string Name, string[] MacPaths, string[] WinPaths)[] KnownEditors =
    [
        ("Visual Studio Code", [
            "/Applications/Visual Studio Code.app",
            "/Applications/Visual Studio Code - Insiders.app"
        ], [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Microsoft VS Code", "Code.exe"),
            @"C:\Program Files\Microsoft VS Code\Code.exe"
        ]),
        ("Cursor", ["/Applications/Cursor.app"], [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "cursor", "Cursor.exe")
        ]),
        ("Zed", ["/Applications/Zed.app"], []),
        ("Sublime Text", ["/Applications/Sublime Text.app"], [
            @"C:\Program Files\Sublime Text\sublime_text.exe",
            @"C:\Program Files\Sublime Text 3\sublime_text.exe"
        ]),
        ("Nova", ["/Applications/Nova.app"], []),
        ("BBEdit", ["/Applications/BBEdit.app"], []),
        ("CotEditor", ["/Applications/CotEditor.app"], []),
        ("TextMate", ["/Applications/TextMate.app"], []),
        ("Notepad++", [], [
            @"C:\Program Files\Notepad++\notepad++.exe",
            @"C:\Program Files (x86)\Notepad++\notepad++.exe"
        ]),
        ("Rider", ["/Applications/Rider.app"], []),
        ("WebStorm", ["/Applications/WebStorm.app"], []),
        ("IntelliJ IDEA", [
            "/Applications/IntelliJ IDEA.app",
            "/Applications/IntelliJ IDEA CE.app"
        ], []),
        ("Fleet", [
            "/Applications/Fleet.app",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Applications", "Fleet.app")
        ], []),
        ("Xcode", ["/Applications/Xcode.app"], []),
        ("MacVim", ["/Applications/MacVim.app"], []),
        ("Emacs", ["/Applications/Emacs.app"], []),
        ("TextEdit", ["/System/Applications/TextEdit.app"], []),
        ("Notepad", [], ["notepad.exe"]),
    ];

    public static async Task<List<EditorInfo>> DetectEditorsAsync()
    {
        var editors = new List<EditorInfo>();

        await Task.Run(() =>
        {
            foreach (var (name, macPaths, winPaths) in KnownEditors)
            {
                var paths = OperatingSystem.IsMacOS() ? macPaths : winPaths;
                foreach (var path in paths)
                {
                    if (PathExists(path))
                    {
                        var displayName = OperatingSystem.IsMacOS()
                            ? GetMacAppName(path) ?? name
                            : name;

                        editors.Add(new EditorInfo
                        {
                            Name = displayName,
                            ExecutablePath = path
                        });
                        break;
                    }
                }
            }
        });

        // Load icons in parallel
        await Task.WhenAll(editors.Select(async e =>
        {
            e.Icon = await LoadIconAsync(e.ExecutablePath);
        }));

        return editors;
    }

    public static void LaunchEditor(string editorPath, string filePath)
    {
        if (OperatingSystem.IsMacOS())
        {
            Process.Start("open", $"-a \"{editorPath}\" \"{filePath}\"");
        }
        else
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = editorPath,
                Arguments = $"\"{filePath}\"",
                UseShellExecute = false
            });
        }
    }

    private static bool PathExists(string path)
    {
        if (OperatingSystem.IsMacOS() && path.EndsWith(".app"))
            return Directory.Exists(path);
        return File.Exists(path);
    }

    private static string? GetMacAppName(string appPath)
    {
        var plistPath = Path.Combine(appPath, "Contents", "Info.plist");
        foreach (var key in new[] { "CFBundleDisplayName", "CFBundleName" })
        {
            var value = ReadPlistKey(plistPath, key);
            if (value != null) return value;
        }
        return null;
    }

    /// <summary>
    /// Reads a string value from a plist file using plutil, which handles both XML and binary formats.
    /// </summary>
    private static string? ReadPlistKey(string plistPath, string key)
    {
        if (!File.Exists(plistPath)) return null;
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "/usr/bin/plutil",
                Arguments = $"-extract {key} raw \"{plistPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            if (process == null) return null;
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return process.ExitCode == 0 && !string.IsNullOrEmpty(output) ? output : null;
        }
        catch { return null; }
    }

    private static async Task<Bitmap?> LoadIconAsync(string editorPath)
    {
        try
        {
            if (OperatingSystem.IsMacOS())
                return await LoadMacIconAsync(editorPath);
        }
        catch { }

        return null;
    }

    private static async Task<Bitmap?> LoadMacIconAsync(string appPath)
    {
        try
        {
            var plistPath = Path.Combine(appPath, "Contents", "Info.plist");
            var iconFileName = ReadPlistKey(plistPath, "CFBundleIconFile");
            if (string.IsNullOrEmpty(iconFileName)) return null;
            if (!iconFileName.EndsWith(".icns"))
                iconFileName += ".icns";

            var icnsPath = Path.Combine(appPath, "Contents", "Resources", iconFileName);
            if (!File.Exists(icnsPath)) return null;

            var tmpPng = Path.Combine(Path.GetTempPath(), $"mdview_icon_{Guid.NewGuid():N}.png");
            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "/usr/bin/sips",
                    Arguments = $"-s format png \"{icnsPath}\" --out \"{tmpPng}\" --resampleWidth 32",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                });

                if (process == null) return null;
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && File.Exists(tmpPng))
                {
                    await using var stream = File.OpenRead(tmpPng);
                    return new Bitmap(stream);
                }
            }
            finally
            {
                if (File.Exists(tmpPng))
                    File.Delete(tmpPng);
            }
        }
        catch { }

        return null;
    }
}
