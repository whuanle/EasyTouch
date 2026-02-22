namespace EasyTouch.Core.Models;

public class ClipboardGetTextRequest : Request
{
}

public class ClipboardSetTextRequest : Request
{
    public string Text { get; set; } = string.Empty;
}

public class ClipboardTextResponse
{
    public string? Text { get; set; }
}

public class ClipboardGetImageRequest : Request
{
    public string? OutputPath { get; set; }
}

public class ClipboardImageResponse
{
    public string? FilePath { get; set; }
    public string? Base64Data { get; set; }
}

public class ClipboardGetFilesRequest : Request
{
}

public class ClipboardFilesResponse
{
    public string[] Files { get; set; } = [];
}

public class ClipboardSetFilesRequest : Request
{
    public string[] Files { get; set; } = [];
}

public class ClipboardClearRequest : Request
{
}
