namespace EasyTouch.Core.Models;

public class MouseMoveRequest : Request
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool Relative { get; set; } = false;
    public int Duration { get; set; } = 0;
    public bool HumanLike { get; set; } = false;
}

public class MouseClickRequest : Request
{
    public MouseButton Button { get; set; } = MouseButton.Left;
    public bool Double { get; set; } = false;
}

public class MouseScrollRequest : Request
{
    public int Amount { get; set; }
    public bool Horizontal { get; set; } = false;
}

public class MouseDragRequest : Request
{
    public int StartX { get; set; }
    public int StartY { get; set; }
    public int EndX { get; set; }
    public int EndY { get; set; }
    public MouseButton Button { get; set; } = MouseButton.Left;
    public bool HumanLike { get; set; } = false;
}

public class MousePositionResponse
{
    public int X { get; set; }
    public int Y { get; set; }
}
