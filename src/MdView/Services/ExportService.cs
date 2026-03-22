using Avalonia.Controls;

namespace MdView.Services;

public static class ExportService
{
    public static async Task ExportToPdfAsync(NativeWebView webView, string outputPath, IProgress<string>? progress = null)
    {
        progress?.Report("Generating PDF...");

        await using var pdfStream = await webView.PrintToPdfStreamAsync();
        await using var fileStream = File.Create(outputPath);
        await pdfStream.CopyToAsync(fileStream);

        progress?.Report("PDF exported successfully.");
    }

    public static async Task ExportToXpsAsync(NativeWebView webView, string outputPath, IProgress<string>? progress = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("XPS export is only available on Windows.");
        }

        // Generate a temporary PDF first, then convert via XPS Document Writer
        var tempPdf = Path.Combine(Path.GetTempPath(), $"mdview_export_{Guid.NewGuid():N}.pdf");
        try
        {
            await ExportToPdfAsync(webView, tempPdf, progress);
            progress?.Report("Converting to XPS...");

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -Command \"" +
                        $"$printer = (Get-Printer | Where-Object {{$_.Name -like '*XPS*'}} | Select-Object -First 1).Name; " +
                        $"if ($printer) {{ " +
                        $"  Start-Process -FilePath '{tempPdf}' -Verb PrintTo -ArgumentList $printer -Wait; " +
                        $"  Copy-Item -Path $env:USERPROFILE\\Documents\\*.xps -Destination '{outputPath}' -Force " +
                        $"}} else {{ Write-Error 'Microsoft XPS Document Writer not found' }}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"XPS conversion failed: {error}");
            }

            progress?.Report("XPS exported successfully.");
        }
        finally
        {
            if (File.Exists(tempPdf))
                File.Delete(tempPdf);
        }
    }
}
