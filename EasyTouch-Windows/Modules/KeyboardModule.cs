using System.Runtime.InteropServices;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class KeyboardModule
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nint dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_SCANCODE = 0x0008;

    private const byte VK_LBUTTON = 0x01;
    private const byte VK_RBUTTON = 0x02;
    private const byte VK_CANCEL = 0x03;
    private const byte VK_MBUTTON = 0x04;
    private const byte VK_XBUTTON1 = 0x05;
    private const byte VK_XBUTTON2 = 0x06;
    private const byte VK_BACK = 0x08;
    private const byte VK_TAB = 0x09;
    private const byte VK_CLEAR = 0x0C;
    private const byte VK_RETURN = 0x0D;
    private const byte VK_SHIFT = 0x10;
    private const byte VK_CONTROL = 0x11;
    private const byte VK_MENU = 0x12;
    private const byte VK_PAUSE = 0x13;
    private const byte VK_CAPITAL = 0x14;
    private const byte VK_ESCAPE = 0x1B;
    private const byte VK_SPACE = 0x20;
    private const byte VK_PRIOR = 0x21;
    private const byte VK_NEXT = 0x22;
    private const byte VK_END = 0x23;
    private const byte VK_HOME = 0x24;
    private const byte VK_LEFT = 0x25;
    private const byte VK_UP = 0x26;
    private const byte VK_RIGHT = 0x27;
    private const byte VK_DOWN = 0x28;
    private const byte VK_SELECT = 0x29;
    private const byte VK_PRINT = 0x2A;
    private const byte VK_EXECUTE = 0x2B;
    private const byte VK_SNAPSHOT = 0x2C;
    private const byte VK_INSERT = 0x2D;
    private const byte VK_DELETE = 0x2E;
    private const byte VK_HELP = 0x2F;
    private const byte VK_0 = 0x30;
    private const byte VK_1 = 0x31;
    private const byte VK_2 = 0x32;
    private const byte VK_3 = 0x33;
    private const byte VK_4 = 0x34;
    private const byte VK_5 = 0x35;
    private const byte VK_6 = 0x36;
    private const byte VK_7 = 0x37;
    private const byte VK_8 = 0x38;
    private const byte VK_9 = 0x39;
    private const byte VK_A = 0x41;
    private const byte VK_B = 0x42;
    private const byte VK_C = 0x43;
    private const byte VK_D = 0x44;
    private const byte VK_E = 0x45;
    private const byte VK_F = 0x46;
    private const byte VK_G = 0x47;
    private const byte VK_H = 0x48;
    private const byte VK_I = 0x49;
    private const byte VK_J = 0x4A;
    private const byte VK_K = 0x4B;
    private const byte VK_L = 0x4C;
    private const byte VK_M = 0x4D;
    private const byte VK_N = 0x4E;
    private const byte VK_O = 0x4F;
    private const byte VK_P = 0x50;
    private const byte VK_Q = 0x51;
    private const byte VK_R = 0x52;
    private const byte VK_S = 0x53;
    private const byte VK_T = 0x54;
    private const byte VK_U = 0x55;
    private const byte VK_V = 0x56;
    private const byte VK_W = 0x57;
    private const byte VK_X = 0x58;
    private const byte VK_Y = 0x59;
    private const byte VK_Z = 0x5A;
    private const byte VK_LWIN = 0x5B;
    private const byte VK_RWIN = 0x5C;
    private const byte VK_APPS = 0x5D;
    private const byte VK_SLEEP = 0x5F;
    private const byte VK_NUMPAD0 = 0x60;
    private const byte VK_NUMPAD1 = 0x61;
    private const byte VK_NUMPAD2 = 0x62;
    private const byte VK_NUMPAD3 = 0x63;
    private const byte VK_NUMPAD4 = 0x64;
    private const byte VK_NUMPAD5 = 0x65;
    private const byte VK_NUMPAD6 = 0x66;
    private const byte VK_NUMPAD7 = 0x67;
    private const byte VK_NUMPAD8 = 0x68;
    private const byte VK_NUMPAD9 = 0x69;
    private const byte VK_MULTIPLY = 0x6A;
    private const byte VK_ADD = 0x6B;
    private const byte VK_SEPARATOR = 0x6C;
    private const byte VK_SUBTRACT = 0x6D;
    private const byte VK_DECIMAL = 0x6E;
    private const byte VK_DIVIDE = 0x6F;
    private const byte VK_F1 = 0x70;
    private const byte VK_F2 = 0x71;
    private const byte VK_F3 = 0x72;
    private const byte VK_F4 = 0x73;
    private const byte VK_F5 = 0x74;
    private const byte VK_F6 = 0x75;
    private const byte VK_F7 = 0x76;
    private const byte VK_F8 = 0x77;
    private const byte VK_F9 = 0x78;
    private const byte VK_F10 = 0x79;
    private const byte VK_F11 = 0x7A;
    private const byte VK_F12 = 0x7B;
    private const byte VK_NUMLOCK = 0x90;
    private const byte VK_SCROLL = 0x91;
    private const byte VK_LSHIFT = 0xA0;
    private const byte VK_RSHIFT = 0xA1;
    private const byte VK_LCONTROL = 0xA2;
    private const byte VK_RCONTROL = 0xA3;
    private const byte VK_LMENU = 0xA4;
    private const byte VK_RMENU = 0xA5;

    private static readonly Dictionary<string, byte> KeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ctrl"] = VK_CONTROL,
        ["control"] = VK_CONTROL,
        ["lctrl"] = VK_LCONTROL,
        ["rctrl"] = VK_RCONTROL,
        ["alt"] = VK_MENU,
        ["lalt"] = VK_LMENU,
        ["ralt"] = VK_RMENU,
        ["shift"] = VK_SHIFT,
        ["lshift"] = VK_LSHIFT,
        ["rshift"] = VK_RSHIFT,
        ["win"] = VK_LWIN,
        ["lwin"] = VK_LWIN,
        ["rwin"] = VK_RWIN,
        ["enter"] = VK_RETURN,
        ["return"] = VK_RETURN,
        ["tab"] = VK_TAB,
        ["space"] = VK_SPACE,
        ["backspace"] = VK_BACK,
        ["delete"] = VK_DELETE,
        ["del"] = VK_DELETE,
        ["insert"] = VK_INSERT,
        ["ins"] = VK_INSERT,
        ["home"] = VK_HOME,
        ["end"] = VK_END,
        ["pageup"] = VK_PRIOR,
        ["pagedown"] = VK_NEXT,
        ["up"] = VK_UP,
        ["down"] = VK_DOWN,
        ["left"] = VK_LEFT,
        ["right"] = VK_RIGHT,
        ["esc"] = VK_ESCAPE,
        ["escape"] = VK_ESCAPE,
        ["f1"] = VK_F1,
        ["f2"] = VK_F2,
        ["f3"] = VK_F3,
        ["f4"] = VK_F4,
        ["f5"] = VK_F5,
        ["f6"] = VK_F6,
        ["f7"] = VK_F7,
        ["f8"] = VK_F8,
        ["f9"] = VK_F9,
        ["f10"] = VK_F10,
        ["f11"] = VK_F11,
        ["f12"] = VK_F12,
        ["0"] = VK_0,
        ["1"] = VK_1,
        ["2"] = VK_2,
        ["3"] = VK_3,
        ["4"] = VK_4,
        ["5"] = VK_5,
        ["6"] = VK_6,
        ["7"] = VK_7,
        ["8"] = VK_8,
        ["9"] = VK_9,
        ["a"] = VK_A,
        ["b"] = VK_B,
        ["c"] = VK_C,
        ["d"] = VK_D,
        ["e"] = VK_E,
        ["f"] = VK_F,
        ["g"] = VK_G,
        ["h"] = VK_H,
        ["i"] = VK_I,
        ["j"] = VK_J,
        ["k"] = VK_K,
        ["l"] = VK_L,
        ["m"] = VK_M,
        ["n"] = VK_N,
        ["o"] = VK_O,
        ["p"] = VK_P,
        ["q"] = VK_Q,
        ["r"] = VK_R,
        ["s"] = VK_S,
        ["t"] = VK_T,
        ["u"] = VK_U,
        ["v"] = VK_V,
        ["w"] = VK_W,
        ["x"] = VK_X,
        ["y"] = VK_Y,
        ["z"] = VK_Z,
    };

    public static Response Press(KeyPressRequest request)
    {
        try
        {
            if (TryParseKey(request.Key, out byte vk))
            {
                keybd_event(vk, 0, 0, 0);
                keybd_event(vk, 0, KEYEVENTF_KEYUP, 0);
                return new SuccessResponse();
            }
            return new ErrorResponse($"Unknown key: {request.Key}");
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response Down(string key)
    {
        try
        {
            if (TryParseKey(key, out byte vk))
            {
                keybd_event(vk, 0, 0, 0);
                return new SuccessResponse();
            }
            return new ErrorResponse($"Unknown key: {key}");
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response Up(string key)
    {
        try
        {
            if (TryParseKey(key, out byte vk))
            {
                keybd_event(vk, 0, KEYEVENTF_KEYUP, 0);
                return new SuccessResponse();
            }
            return new ErrorResponse($"Unknown key: {key}");
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response Combo(KeyComboRequest request)
    {
        try
        {
            var keys = new List<byte>();
            foreach (var key in request.Keys)
            {
                if (TryParseKey(key, out byte vk))
                {
                    keys.Add(vk);
                }
                else
                {
                    return new ErrorResponse($"Unknown key: {key}");
                }
            }

            foreach (var vk in keys)
            {
                keybd_event(vk, 0, 0, 0);
            }

            foreach (var vk in keys.AsEnumerable().Reverse())
            {
                keybd_event(vk, 0, KEYEVENTF_KEYUP, 0);
            }

            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response TypeText(TypeTextRequest request)
    {
        try
        {
            var random = request.HumanLike ? new Random() : null;
            
            foreach (char c in request.Text)
            {
                if (TryGetCharKeyCode(c, out byte vk, out bool shift))
                {
                    if (shift)
                    {
                        keybd_event(VK_SHIFT, 0, 0, 0);
                    }
                    
                    keybd_event(vk, 0, 0, 0);
                    keybd_event(vk, 0, KEYEVENTF_KEYUP, 0);
                    
                    if (shift)
                    {
                        keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, 0);
                    }
                }

                if (request.Interval > 0)
                {
                    int delay = request.Interval;
                    if (request.HumanLike && random != null)
                    {
                        delay = request.Interval + random.Next(-20, 21);
                        delay = Math.Max(10, delay);
                    }
                    Thread.Sleep(delay);
                }
            }

            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response GetKeyState(KeyStateRequest request)
    {
        try
        {
            if (TryParseKey(request.Key, out byte vk))
            {
                short state = GetAsyncKeyState(vk);
                bool isPressed = (state & 0x8000) != 0;
                return new SuccessResponse<KeyStateResponse>(new KeyStateResponse
                {
                    IsPressed = isPressed
                });
            }
            return new ErrorResponse($"Unknown key: {request.Key}");
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    private static bool TryParseKey(string key, out byte vk)
    {
        vk = 0;
        
        if (string.IsNullOrEmpty(key))
            return false;

        if (KeyMap.TryGetValue(key, out vk))
            return true;

        if (key.Length == 1)
        {
            char c = char.ToLower(key[0]);
            if (c >= 'a' && c <= 'z')
            {
                vk = (byte)(VK_A + (c - 'a'));
                return true;
            }
            if (c >= '0' && c <= '9')
            {
                vk = (byte)(VK_0 + (c - '0'));
                return true;
            }
        }

        return false;
    }

    private static bool TryGetCharKeyCode(char c, out byte vk, out bool shift)
    {
        vk = 0;
        shift = false;

        if (c >= 'a' && c <= 'z')
        {
            vk = (byte)(VK_A + (c - 'a'));
            return true;
        }

        if (c >= 'A' && c <= 'Z')
        {
            vk = (byte)(VK_A + (c - 'A'));
            shift = true;
            return true;
        }

        if (c >= '0' && c <= '9')
        {
            vk = (byte)(VK_0 + (c - '0'));
            return true;
        }

        switch (c)
        {
            case ' ': vk = VK_SPACE; return true;
            case '\n': vk = VK_RETURN; return true;
            case '\t': vk = VK_TAB; return true;
            case '-': vk = VK_SUBTRACT; return true;
            case '=': vk = 0xBB; return true;
            case '[': vk = 0xDB; return true;
            case ']': vk = 0xDD; return true;
            case '\\': vk = 0xDC; return true;
            case ';': vk = 0xBA; return true;
            case '\'': vk = 0xDE; return true;
            case ',': vk = 0xBC; return true;
            case '.': vk = 0xBE; return true;
            case '/': vk = 0xBF; return true;
            case '`': vk = 0xC0; return true;
            case '!': vk = VK_1; shift = true; return true;
            case '@': vk = VK_2; shift = true; return true;
            case '#': vk = VK_3; shift = true; return true;
            case '$': vk = VK_4; shift = true; return true;
            case '%': vk = VK_5; shift = true; return true;
            case '^': vk = VK_6; shift = true; return true;
            case '&': vk = VK_7; shift = true; return true;
            case '*': vk = VK_8; shift = true; return true;
            case '(': vk = VK_9; shift = true; return true;
            case ')': vk = VK_0; shift = true; return true;
            case '_': vk = VK_SUBTRACT; shift = true; return true;
            case '+': vk = VK_ADD; shift = true; return true;
            case '{': vk = 0xDB; shift = true; return true;
            case '}': vk = 0xDD; shift = true; return true;
            case '|': vk = 0xDC; shift = true; return true;
            case ':': vk = 0xBA; shift = true; return true;
            case '\"': vk = 0xDE; shift = true; return true;
            case '<': vk = 0xBC; shift = true; return true;
            case '>': vk = 0xBE; shift = true; return true;
            case '?': vk = 0xBF; shift = true; return true;
            case '~': vk = 0xC0; shift = true; return true;
        }

        return false;
    }
}
