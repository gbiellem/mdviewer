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

}
