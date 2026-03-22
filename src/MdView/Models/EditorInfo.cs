using Avalonia.Media.Imaging;

namespace MdView.Models;

public class EditorInfo
{
    public required string Name { get; init; }
    public required string ExecutablePath { get; init; }
    public Bitmap? Icon { get; set; }
}
