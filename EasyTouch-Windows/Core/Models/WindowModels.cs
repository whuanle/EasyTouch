namespace EasyTouch.Core.Models;

public class WindowListRequest : Request
{
    public bool VisibleOnly { get; set; } = true;
    public string? TitleFilter { get; set; }
}

public class WindowListResponse
{
    public WindowInfo[] Windows { get; set; } = [];
}

public class WindowFindRequest : Request
{
    public string? Title { get; set; }
    public string? ClassName { get; set; }
    public uint? ProcessId { get; set; }
}

public class WindowFindResponse
{
    public long? Handle { get; set; }
    public WindowInfo? Window { get; set; }
}

public class WindowActivateRequest : Request
{
    public long Handle { get; set; }
}

public class WindowShowRequest : Request
{
    public long Handle { get; set; }
    public WindowShowState State { get; set; } = WindowShowState.Show;
}

public class WindowMoveRequest : Request
{
    public long Handle { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}

public class WindowSetTopmostRequest : Request
{
    public long Handle { get; set; }
    public bool Topmost { get; set; } = true;
}

public class WindowCloseRequest : Request
{
    public long Handle { get; set; }
    public bool Force { get; set; } = false;
}
