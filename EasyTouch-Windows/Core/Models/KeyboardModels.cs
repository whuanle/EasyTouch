namespace EasyTouch.Core.Models;

public class KeyPressRequest : Request
{
    public string Key { get; set; } = string.Empty;
}

public class KeyComboRequest : Request
{
    public string[] Keys { get; set; } = [];
}

public class TypeTextRequest : Request
{
    public string Text { get; set; } = string.Empty;
    public int Interval { get; set; } = 0;
    public bool HumanLike { get; set; } = false;
}

public class KeyStateRequest : Request
{
    public string Key { get; set; } = string.Empty;
}

public class KeyStateResponse
{
    public bool IsPressed { get; set; }
}
