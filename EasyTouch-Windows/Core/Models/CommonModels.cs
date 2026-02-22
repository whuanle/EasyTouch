namespace EasyTouch.Core.Models;

public record Point(int X, int Y);

public record Size(int Width, int Height);

public record Rect(int X, int Y, int Width, int Height);

public record WindowInfo(
    long Handle,
    string Title,
    string ClassName,
    Rect Bounds,
    bool IsVisible,
    uint ProcessId
);

public record ScreenInfo(
    int Index,
    string DeviceName,
    Rect Bounds,
    Rect WorkingArea,
    bool IsPrimary,
    int BitsPerPixel
);

public record ProcessInfo(
    uint Id,
    string Name,
    string? ExecutablePath,
    long WorkingSetMemory
);

public record SystemInfo(
    string OsVersion,
    string MachineName,
    string UserName,
    int ProcessorCount,
    long TotalMemory,
    long AvailableMemory
);

public record DiskInfo(
    string DriveLetter,
    string VolumeLabel,
    long TotalSize,
    long AvailableFreeSpace,
    long TotalFreeSpace,
    string DriveType
);

public record CpuInfo(
    string ProcessorName,
    int NumberOfCores,
    int NumberOfLogicalProcessors,
    string Architecture
);

public record AudioDeviceInfo(
    string Id,
    string Name,
    bool IsDefault,
    bool IsMuted,
    float Volume
);

public enum MouseButton
{
    Left,
    Right,
    Middle,
    XButton1,
    XButton2
}

public enum WindowShowState
{
    Hide = 0,
    ShowNormal = 1,
    ShowMinimized = 2,
    ShowMaximized = 3,
    ShowNoActivate = 4,
    Show = 5,
    Minimize = 6,
    ShowMinNoActive = 7,
    ShowNA = 8,
    Restore = 9,
    ShowDefault = 10,
    ForceMinimize = 11
}

public enum WindowZOrder
{
    HWND_TOP = 0,
    HWND_BOTTOM = 1,
    HWND_TOPMOST = -1,
    HWND_NOTOPMOST = -2
}
