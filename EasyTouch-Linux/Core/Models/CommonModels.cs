using System.Text.Json.Serialization;

namespace EasyTouch.Core.Models;

public abstract class Request { }

public abstract class Response
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class SuccessResponse : Response
{
    public SuccessResponse()
    {
        Success = true;
    }
}

public class SuccessResponse<T> : Response where T : class
{
    public SuccessResponse(T data)
    {
        Success = true;
        Data = data;
    }

    [JsonPropertyName("data")]
    public T Data { get; set; }
}

public class ErrorResponse : Response
{
    public ErrorResponse(string message)
    {
        Success = false;
        Error = message;
    }
}

// Mouse Models
public class MouseMoveRequest : Request
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool Relative { get; set; }
    public int Duration { get; set; }
}

public class MouseMoveResponse
{
    public int X { get; set; }
    public int Y { get; set; }
}

public enum MouseButton
{
    Left,
    Right,
    Middle,
    XButton1,
    XButton2
}

public class MouseClickRequest : Request
{
    public MouseButton Button { get; set; } = MouseButton.Left;
    public bool Double { get; set; }
}

public class MouseScrollRequest : Request
{
    public int Amount { get; set; }
    public bool Horizontal { get; set; }
}

public class MousePositionResponse
{
    public int X { get; set; }
    public int Y { get; set; }
}

// Keyboard Models
public class KeyPressRequest : Request
{
    public string Key { get; set; } = string.Empty;
}

public class TypeTextRequest : Request
{
    public string Text { get; set; } = string.Empty;
    public int Interval { get; set; }
    public bool HumanLike { get; set; }
}

// Screen Models
public class ScreenshotRequest : Request
{
    public string? OutputPath { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}

public class ScreenshotResponse
{
    public string Path { get; set; } = string.Empty;
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
}

public class ScreenInfo
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsPrimary { get; set; }
}

public class ScreenListResponse
{
    public List<ScreenInfo> Screens { get; set; } = new();
}

// Window Models
public class WindowInfo
{
    public long Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public uint ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsVisible { get; set; }
    public bool IsMinimized { get; set; }
    public bool IsMaximized { get; set; }
}

public class WindowListRequest : Request
{
    public bool VisibleOnly { get; set; } = true;
    public string? TitleFilter { get; set; }
}

public class WindowListResponse
{
    public List<WindowInfo> Windows { get; set; } = new();
}

public class WindowFindRequest : Request
{
    public string? Title { get; set; }
    public string? ClassName { get; set; }
    public uint? ProcessId { get; set; }
}

public class WindowFindResponse
{
    public WindowInfo? Window { get; set; }
    public long? Handle { get; set; }
}

public class WindowActivateRequest : Request
{
    public long Handle { get; set; }
}

// System Models
public class OsInfoResponse
{
    public string Platform { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}

public class CpuInfoResponse
{
    public string Name { get; set; } = string.Empty;
    public int CoreCount { get; set; }
    public int ThreadCount { get; set; }
    public double Usage { get; set; }
}

public class UptimeInfoResponse
{
    public double Seconds { get; set; }
    public long Milliseconds { get; set; }
    public string HumanReadable { get; set; } = string.Empty;
}

public class BatteryInfoResponse
{
    public bool Present { get; set; }
    public int Percentage { get; set; }
    public string Status { get; set; } = "Unknown";
    public bool IsCharging { get; set; }
    public int? TimeToEmptyMinutes { get; set; }
    public int? TimeToFullMinutes { get; set; }
}

public class MemoryInfoResponse
{
    public long Total { get; set; }
    public long Available { get; set; }
    public long Used { get; set; }
    public double UsagePercent { get; set; }
}

public class DiskInfo
{
    public string Name { get; set; } = string.Empty;
    public string MountPoint { get; set; } = string.Empty;
    public string FileSystem { get; set; } = string.Empty;
    public long Total { get; set; }
    public long Free { get; set; }
    public long Used { get; set; }
}

public class DiskListResponse
{
    public List<DiskInfo> Disks { get; set; } = new();
}

public class ProcessInfo
{
    public uint Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public long Memory { get; set; }
    public double CpuUsage { get; set; }
}

public class ProcessListRequest : Request
{
    public string? NameFilter { get; set; }
}

public class ProcessListResponse
{
    public List<ProcessInfo> Processes { get; set; } = new();
}

// Clipboard Models
public class ClipboardGetTextRequest : Request { }

public class ClipboardGetTextResponse
{
    public string Text { get; set; } = string.Empty;
}

public class ClipboardSetTextRequest : Request
{
    public string Text { get; set; } = string.Empty;
}

public class ClipboardClearRequest : Request { }

public class ClipboardGetFilesRequest : Request { }

public class ClipboardGetFilesResponse
{
    public List<string> Files { get; set; } = new();
}

// Audio Models
public class VolumeGetRequest : Request { }

public class VolumeGetResponse
{
    public int Level { get; set; }
    public bool IsMuted { get; set; }
}

public class VolumeSetRequest : Request
{
    public int Level { get; set; }
}

public class VolumeMuteRequest : Request
{
    public bool Mute { get; set; }
}

public class AudioDeviceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsInput { get; set; }
}

public class AudioDeviceListRequest : Request { }

public class AudioDeviceListResponse
{
    public List<AudioDeviceInfo> Devices { get; set; } = new();
}
