namespace EasyTouch.Core.Models;

public class ScreenshotRequest : Request
{
    public int? X { get; set; }
    public int? Y { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public nint? WindowHandle { get; set; }
    public string? OutputPath { get; set; }
    public string Format { get; set; } = "png";
}

public class ScreenshotResponse
{
    public string? FilePath { get; set; }
    public string? Base64Data { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class PixelColorRequest : Request
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class PixelColorResponse
{
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
    public string Hex { get; set; } = string.Empty;
}

public class ScreenListResponse
{
    public ScreenInfo[] Screens { get; set; } = [];
}
