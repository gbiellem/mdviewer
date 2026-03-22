using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using MdView.Models;

namespace MdView.Services;

public static class EditorDetectionService
{
    // Windows fallback: hardcoded known editors
    private static readonly (string Name, string[] Paths)[] KnownWindowsEditors =
    [
        ("Visual Studio Code", [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Microsoft VS Code", "Code.exe"),
            @"C:\Program Files\Microsoft VS Code\Code.exe"
        ]),
        ("Cursor", [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "cursor", "Cursor.exe")
        ]),
        ("Sublime Text", [
            @"C:\Program Files\Sublime Text\sublime_text.exe",
            @"C:\Program Files\Sublime Text 3\sublime_text.exe"
        ]),
        ("Notepad++", [
            @"C:\Program Files\Notepad++\notepad++.exe",
            @"C:\Program Files (x86)\Notepad++\notepad++.exe"
        ]),
        ("Notepad", ["notepad.exe"]),
    ];

    public static async Task<List<EditorInfo>> DetectEditorsAsync()
    {
        if (OperatingSystem.IsMacOS())
            return await DetectMacEditorsAsync();

        return await DetectWindowsEditorsAsync();
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

    // --- macOS: Launch Services API ---

    // Well-known editors that use wildcard (*) file registration and won't
    // appear in LSCopyApplicationURLsForURL results for specific extensions
    private static readonly string[] KnownMacEditorPaths =
    [
        "/Applications/Visual Studio Code.app",
        "/Applications/Visual Studio Code - Insiders.app",
        "/Applications/Cursor.app",
        "/Applications/Zed.app",
        "/Applications/Sublime Text.app",
        "/Applications/Nova.app",
        "/Applications/BBEdit.app",
        "/Applications/CotEditor.app",
        "/Applications/TextMate.app",
        "/Applications/Rider.app",
        "/Applications/WebStorm.app",
        "/Applications/IntelliJ IDEA.app",
        "/Applications/IntelliJ IDEA CE.app",
        "/Applications/Fleet.app",
        "/Applications/MacVim.app",
        "/Applications/Emacs.app",
    ];

    private static async Task<List<EditorInfo>> DetectMacEditorsAsync()
    {
        var editors = new List<EditorInfo>();

        await Task.Run(() =>
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            // Get apps registered with Launch Services for .md files
            var appUrls = GetMacApplicationsForFileExtension(".md");
            foreach (var appPath in appUrls)
            {
                if (!seen.Add(appPath)) continue;
                if (IsMdView(appPath)) continue;

                editors.Add(CreateEditorInfo(appPath));
            }

            // Add well-known editors that don't register for .md specifically
            foreach (var path in KnownMacEditorPaths)
            {
                if (!Directory.Exists(path)) continue;
                if (!seen.Add(path)) continue;
                if (IsMdView(path)) continue;

                editors.Add(CreateEditorInfo(path));
            }
        });

        // Load icons in parallel
        await Task.WhenAll(editors.Select(async e =>
        {
            e.Icon = await LoadMacIconAsync(e.ExecutablePath);
        }));

        return editors;
    }

    private static bool IsMdView(string appPath)
    {
        var bundleId = ReadPlistKey(
            Path.Combine(appPath, "Contents", "Info.plist"),
            "CFBundleIdentifier");
        return string.Equals(bundleId, "com.mdview.app", StringComparison.OrdinalIgnoreCase);
    }

    private static EditorInfo CreateEditorInfo(string appPath)
    {
        var displayName = GetMacAppName(appPath)
            ?? Path.GetFileNameWithoutExtension(appPath);
        return new EditorInfo
        {
            Name = displayName,
            ExecutablePath = appPath
        };
    }

    private static List<string> GetMacApplicationsForFileExtension(string extension)
    {
        var results = new List<string>();

        // LSCopyApplicationURLsForURL requires the file to exist on disk
        var dummyPath = Path.Combine(Path.GetTempPath(), $"mdview_query{extension}");
        if (!File.Exists(dummyPath))
            File.Create(dummyPath).Dispose();

        var cfStr = CFStringCreate(dummyPath);
        var cfUrl = CFURLCreateWithFileSystemPath(IntPtr.Zero, cfStr, 0, false);
        CFRelease(cfStr);
        if (cfUrl == IntPtr.Zero) return results;

        try
        {
            var cfArray = LSCopyApplicationURLsForURL(cfUrl, 0xFFFFFFFF); // kLSRolesAll
            if (cfArray == IntPtr.Zero) return results;

            try
            {
                var count = CFArrayGetCount(cfArray);
                for (long i = 0; i < count; i++)
                {
                    var appUrl = CFArrayGetValueAtIndex(cfArray, i);
                    if (appUrl == IntPtr.Zero) continue;

                    var cfPath = CFURLCopyFileSystemPath(appUrl, 0); // kCFURLPOSIXPathStyle
                    if (cfPath == IntPtr.Zero) continue;

                    try
                    {
                        var path = CFStringToString(cfPath);
                        if (path != null && Directory.Exists(path))
                            results.Add(path);
                    }
                    finally
                    {
                        CFRelease(cfPath);
                    }
                }
            }
            finally
            {
                CFRelease(cfArray);
            }
        }
        finally
        {
            CFRelease(cfUrl);
        }

        return results;
    }

    // --- CoreFoundation / Launch Services P/Invoke ---

    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string CoreServices = "/System/Library/Frameworks/CoreServices.framework/CoreServices";

    [DllImport(CoreServices)]
    private static extern IntPtr LSCopyApplicationURLsForURL(IntPtr inURL, uint inRoleMask);

    [DllImport(CoreFoundation)]
    private static extern IntPtr CFURLCreateWithFileSystemPath(IntPtr allocator, IntPtr filePath, int pathStyle, bool isDirectory);

    [DllImport(CoreFoundation)]
    private static extern IntPtr CFURLCopyFileSystemPath(IntPtr url, int pathStyle);

    [DllImport(CoreFoundation)]
    private static extern long CFArrayGetCount(IntPtr theArray);

    [DllImport(CoreFoundation)]
    private static extern IntPtr CFArrayGetValueAtIndex(IntPtr theArray, long idx);

    [DllImport(CoreFoundation)]
    private static extern IntPtr CFStringCreateWithCString(IntPtr allocator, [MarshalAs(UnmanagedType.LPUTF8Str)] string str, int encoding);

    [DllImport(CoreFoundation)]
    private static extern bool CFStringGetCString(IntPtr theString, IntPtr buffer, long bufferSize, int encoding);

    [DllImport(CoreFoundation)]
    private static extern long CFStringGetLength(IntPtr theString);

    [DllImport(CoreFoundation)]
    private static extern void CFRelease(IntPtr cf);

    private static IntPtr CFStringCreate(string s) =>
        CFStringCreateWithCString(IntPtr.Zero, s, 0x08000100); // kCFStringEncodingUTF8

    private static string? CFStringToString(IntPtr cfString)
    {
        if (cfString == IntPtr.Zero) return null;
        var length = CFStringGetLength(cfString);
        var bufferSize = length * 4 + 1; // UTF-8 worst case
        var buffer = Marshal.AllocHGlobal((int)bufferSize);
        try
        {
            if (CFStringGetCString(cfString, buffer, bufferSize, 0x08000100))
                return Marshal.PtrToStringUTF8(buffer);
            return null;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    // --- macOS helpers ---

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

    // --- Windows ---

    private static async Task<List<EditorInfo>> DetectWindowsEditorsAsync()
    {
        var editors = new List<EditorInfo>();

        await Task.Run(() =>
        {
            foreach (var (name, paths) in KnownWindowsEditors)
            {
                foreach (var path in paths)
                {
                    if (File.Exists(path))
                    {
                        editors.Add(new EditorInfo
                        {
                            Name = name,
                            ExecutablePath = path
                        });
                        break;
                    }
                }
            }
        });

        return editors;
    }
}
