using System.Runtime.InteropServices;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class MouseModule
{
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, nint dwExtraInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_XDOWN = 0x0080;
    private const uint MOUSEEVENTF_XUP = 0x0100;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const uint MOUSEEVENTF_HWHEEL = 0x1000;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    
    private const uint XBUTTON1 = 0x0001;
    private const uint XBUTTON2 = 0x0002;

    public static Response Move(MouseMoveRequest request)
    {
        try
        {
            int targetX = request.X;
            int targetY = request.Y;
            
            if (request.Relative)
            {
                var currentPos = GetCurrentPosition();
                targetX = currentPos.X + request.X;
                targetY = currentPos.Y + request.Y;
            }
            
            if (request.Duration > 0)
            {
                SmoothMove(targetX, targetY, request.Duration);
            }
            else
            {
                SetCursorPos(targetX, targetY);
            }
            
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response Click(MouseClickRequest request)
    {
        try
        {
            var (downFlag, upFlag, data) = GetMouseButtonFlags(request.Button);
            
            if (request.Double)
            {
                mouse_event(downFlag, 0, 0, data, 0);
                mouse_event(upFlag, 0, 0, data, 0);
                Thread.Sleep(50);
                mouse_event(downFlag, 0, 0, data, 0);
                mouse_event(upFlag, 0, 0, data, 0);
            }
            else
            {
                mouse_event(downFlag, 0, 0, data, 0);
                mouse_event(upFlag, 0, 0, data, 0);
            }
            
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response Down(MouseButton button)
    {
        try
        {
            var (downFlag, _, data) = GetMouseButtonFlags(button);
            mouse_event(downFlag, 0, 0, data, 0);
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response Up(MouseButton button)
    {
        try
        {
            var (_, upFlag, data) = GetMouseButtonFlags(button);
            mouse_event(upFlag, 0, 0, data, 0);
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response Scroll(MouseScrollRequest request)
    {
        try
        {
            uint flag = request.Horizontal ? MOUSEEVENTF_HWHEEL : MOUSEEVENTF_WHEEL;
            uint amount = (uint)(request.Amount * 120);
            mouse_event(flag, 0, 0, amount, 0);
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response Drag(MouseDragRequest request)
    {
        try
        {
            var (downFlag, upFlag, data) = GetMouseButtonFlags(request.Button);
            
            SetCursorPos(request.StartX, request.StartY);
            Thread.Sleep(50);
            mouse_event(downFlag, 0, 0, data, 0);
            Thread.Sleep(50);
            
            SmoothMove(request.EndX, request.EndY, 300);
            
            Thread.Sleep(50);
            mouse_event(upFlag, 0, 0, data, 0);
            
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response GetPosition()
    {
        try
        {
            var pos = GetCurrentPosition();
            return new SuccessResponse<MousePositionResponse>(new MousePositionResponse
            {
                X = pos.X,
                Y = pos.Y
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    private static Core.Models.Point GetCurrentPosition()
    {
        GetCursorPos(out POINT pt);
        return new Core.Models.Point(pt.x, pt.y);
    }

    private static void SmoothMove(int targetX, int targetY, int duration)
    {
        var currentPos = GetCurrentPosition();
        int startX = currentPos.X;
        int startY = currentPos.Y;
        
        int steps = Math.Max(duration / 16, 10);
        int stepDuration = duration / steps;
        
        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            double easedT = t * t * (3 - 2 * t);
            
            int x = startX + (int)((targetX - startX) * easedT);
            int y = startY + (int)((targetY - startY) * easedT);
            
            SetCursorPos(x, y);
            Thread.Sleep(stepDuration);
        }
        
        SetCursorPos(targetX, targetY);
    }

    private static (uint downFlag, uint upFlag, uint data) GetMouseButtonFlags(MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => (MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, 0),
            MouseButton.Right => (MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP, 0),
            MouseButton.Middle => (MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP, 0),
            MouseButton.XButton1 => (MOUSEEVENTF_XDOWN, MOUSEEVENTF_XUP, XBUTTON1),
            MouseButton.XButton2 => (MOUSEEVENTF_XDOWN, MOUSEEVENTF_XUP, XBUTTON2),
            _ => (MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, 0)
        };
    }
}
