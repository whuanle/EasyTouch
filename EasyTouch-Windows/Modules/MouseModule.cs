using System.Runtime.InteropServices;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class MouseModule
{
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, nint dwExtraInfo);

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
    
    private const uint XBUTTON1 = 0x0001;
    private const uint XBUTTON2 = 0x0002;

    private static readonly Random _random = new Random();

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
                if (request.HumanLike)
                {
                    HumanLikeMove(targetX, targetY, request.Duration);
                }
                else
                {
                    SmoothMove(targetX, targetY, request.Duration);
                }
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
            
            if (request.HumanLike)
            {
                HumanLikeMove(request.EndX, request.EndY, 300);
            }
            else
            {
                SmoothMove(request.EndX, request.EndY, 300);
            }
            
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
        GetCursorPos(out System.Drawing.Point pt);
        return new Core.Models.Point(pt.X, pt.Y);
    }

    private static void HumanLikeMove(int targetX, int targetY, int duration)
    {
        var currentPos = GetCurrentPosition();
        int startX = currentPos.X;
        int startY = currentPos.Y;
        
        double distance = Math.Sqrt(Math.Pow(targetX - startX, 2) + Math.Pow(targetY - startY, 2));
        int adjustedDuration = (int)(duration * (0.8 + _random.NextDouble() * 0.4));
        
        var controlPoints = GenerateBezierControlPoints(startX, startY, targetX, targetY, distance);
        
        int baseSteps = Math.Max(20, (int)(distance / 10));
        int steps = baseSteps + _random.Next(-5, 6);
        steps = Math.Max(10, steps);
        
        var pathPoints = new List<(int x, int y)>();
        
        for (int i = 0; i <= steps; i++)
        {
            double t = (double)i / steps;
            var (bx, by) = CalculateBezierPoint(t, startX, startY, controlPoints);
            double jitterAmount = CalculateJitterAmount(t, distance);
            int jitterX = (int)(_random.NextDouble() * jitterAmount * 2 - jitterAmount);
            int jitterY = (int)(_random.NextDouble() * jitterAmount * 2 - jitterAmount);
            pathPoints.Add((bx + jitterX, by + jitterY));
        }
        
        for (int i = 0; i < pathPoints.Count; i++)
        {
            double t = (double)i / pathPoints.Count;
            int stepDuration = CalculateStepDuration(t, adjustedDuration, pathPoints.Count);
            
            if (_random.NextDouble() < 0.05 && i > 0 && i < pathPoints.Count - 1)
            {
                Thread.Sleep(_random.Next(20, 80));
            }
            
            SetCursorPos(pathPoints[i].x, pathPoints[i].y);
            Thread.Sleep(stepDuration);
        }
        
        SetCursorPos(targetX, targetY);
    }

    private static (int x1, int y1, int x2, int y2) GenerateBezierControlPoints(
        int startX, int startY, int targetX, int targetY, double distance)
    {
        double dx = targetX - startX;
        double dy = targetY - startY;
        double length = Math.Sqrt(dx * dx + dy * dy);
        
        double perpX = -dy / length;
        double perpY = dx / length;
        
        double offset1 = _random.NextDouble() * distance * 0.3;
        double offset2 = _random.NextDouble() * distance * 0.3;
        
        int direction1 = _random.Next(2) == 0 ? 1 : -1;
        int direction2 = _random.Next(2) == 0 ? 1 : -1;
        
        double t1 = 0.25 + _random.NextDouble() * 0.2;
        int cp1X = (int)(startX + dx * t1 + perpX * offset1 * direction1);
        int cp1Y = (int)(startY + dy * t1 + perpY * offset1 * direction1);
        
        double t2 = 0.55 + _random.NextDouble() * 0.2;
        int cp2X = (int)(startX + dx * t2 + perpX * offset2 * direction2);
        int cp2Y = (int)(startY + dy * t2 + perpY * offset2 * direction2);
        
        return (cp1X, cp1Y, cp2X, cp2Y);
    }

    private static (int x, int y) CalculateBezierPoint(double t, int startX, int startY, 
        (int x1, int y1, int x2, int y2) controlPoints)
    {
        double u = 1 - t;
        double u2 = u * u;
        double u3 = u2 * u;
        double t2 = t * t;
        double t3 = t2 * t;
        
        int x = (int)(u3 * startX + 3 * u2 * t * controlPoints.x1 + 3 * u * t2 * controlPoints.x2 + t3 * controlPoints.x2);
        int y = (int)(u3 * startY + 3 * u2 * t * controlPoints.y1 + 3 * u * t2 * controlPoints.y2 + t3 * controlPoints.y2);
        
        return (x, y);
    }

    private static double CalculateJitterAmount(double t, double distance)
    {
        double sineValue = Math.Sin(t * Math.PI);
        double maxJitter = Math.Min(5, distance * 0.02);
        return sineValue * maxJitter * (0.5 + _random.NextDouble() * 0.5);
    }

    private static int CalculateStepDuration(double t, int totalDuration, int totalSteps)
    {
        double baseDuration = (double)totalDuration / totalSteps;
        double speedFactor = 0.6 + 0.8 * Math.Sin(t * Math.PI);
        speedFactor = Math.Max(0.3, Math.Min(2.0, speedFactor));
        double randomFactor = 0.8 + _random.NextDouble() * 0.4;
        return (int)(baseDuration / speedFactor * randomFactor);
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
            MouseButton.Left => (MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, 0u),
            MouseButton.Right => (MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP, 0u),
            MouseButton.Middle => (MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP, 0u),
            MouseButton.XButton1 => (MOUSEEVENTF_XDOWN, MOUSEEVENTF_XUP, XBUTTON1),
            MouseButton.XButton2 => (MOUSEEVENTF_XDOWN, MOUSEEVENTF_XUP, XBUTTON2),
            _ => (MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, 0u)
        };
    }
}
