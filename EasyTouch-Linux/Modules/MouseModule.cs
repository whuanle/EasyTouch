using System.Diagnostics;
using System.Runtime.InteropServices;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class MouseModule
{
    // X11 constants
    private const int XButton1 = 8;
    private const int XButton2 = 9;
    
    public static Response Move(MouseMoveRequest request)
    {
        try
        {
            if (IsWayland())
            {
                return MoveWayland(request);
            }
            
            // X11 implementation using xdotool
            var args = $"mousemove {request.X} {request.Y}";
            if (request.Relative)
            {
                args = $"mousemove --relative {request.X} {request.Y}";
            }
            
            if (request.Duration > 0)
            {
                // For smooth movement, use multiple steps
                var currentPos = GetPosition();
                var steps = request.Duration / 10;
                var stepX = (request.X - currentPos.X) / steps;
                var stepY = (request.Y - currentPos.Y) / steps;
                
                for (int i = 0; i < steps; i++)
                {
                    var newX = (int)(currentPos.X + stepX * i);
                    var newY = (int)(currentPos.Y + stepY * i);
                    RunXdotool($"mousemove {newX} {newY}");
                    Thread.Sleep(10);
                }
            }
            
            RunXdotool(args);
            
            var finalPos = GetPosition();
            return new SuccessResponse<MousePositionResponse>(finalPos);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Mouse move failed: {ex.Message}");
        }
    }

    public static Response Click(MouseClickRequest request)
    {
        try
        {
            if (IsWayland())
            {
                return ClickWayland(request);
            }
            
            var button = request.Button switch
            {
                MouseButton.Left => "1",
                MouseButton.Right => "3",
                MouseButton.Middle => "2",
                MouseButton.XButton1 => "8",
                MouseButton.XButton2 => "9",
                _ => "1"
            };
            
            var clickType = request.Double ? "click --repeat 2" : "click";
            RunXdotool($"{clickType} {button}");
            
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Mouse click failed: {ex.Message}");
        }
    }

    public static Response Down(MouseButton button)
    {
        try
        {
            if (IsWayland())
            {
                return DownWayland(button);
            }
            
            var btn = button switch
            {
                MouseButton.Left => "1",
                MouseButton.Right => "3",
                MouseButton.Middle => "2",
                _ => "1"
            };
            
            RunXdotool($"mousedown {btn}");
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Mouse down failed: {ex.Message}");
        }
    }

    public static Response Up(MouseButton button)
    {
        try
        {
            if (IsWayland())
            {
                return UpWayland(button);
            }
            
            var btn = button switch
            {
                MouseButton.Left => "1",
                MouseButton.Right => "3",
                MouseButton.Middle => "2",
                _ => "1"
            };
            
            RunXdotool($"mouseup {btn}");
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Mouse up failed: {ex.Message}");
        }
    }

    public static Response Scroll(MouseScrollRequest request)
    {
        try
        {
            if (IsWayland())
            {
                return ScrollWayland(request);
            }
            
            var direction = request.Horizontal ? "h" : "";
            var amount = Math.Abs(request.Amount);
            var button = request.Amount > 0 ? 4 : 5; // 4=up, 5=down for vertical; 6=left, 7=right for horizontal
            
            if (request.Horizontal)
            {
                button = request.Amount > 0 ? 7 : 6;
            }
            
            for (int i = 0; i < amount; i++)
            {
                RunXdotool($"click {button}");
            }
            
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Mouse scroll failed: {ex.Message}");
        }
    }

    public static MousePositionResponse GetPosition()
    {
        try
        {
            if (IsWayland())
            {
                return GetPositionWayland();
            }
            
            var output = RunXdotool("getmouselocation --shell");
            var x = 0;
            var y = 0;
            
            foreach (var line in output.Split('\n'))
            {
                if (line.StartsWith("X="))
                    int.TryParse(line.Substring(2), out x);
                else if (line.StartsWith("Y="))
                    int.TryParse(line.Substring(2), out y);
            }
            
            return new MousePositionResponse { X = x, Y = y };
        }
        catch
        {
            return new MousePositionResponse { X = 0, Y = 0 };
        }
    }

    private static bool IsWayland()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));
    }

    private static Response MoveWayland(MouseMoveRequest request)
    {
        // For Wayland, we can use ydotool or wtype
        try
        {
            var args = $"mousemove -x {request.X} -y {request.Y}";
            if (request.Relative)
            {
                args = $"mousemove -x {request.X} -y {request.Y} --relative";
            }
            
            RunCommand("ydotool", args);
            var pos = GetPositionWayland();
            return new SuccessResponse<MousePositionResponse>(pos);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Wayland mouse move failed: {ex.Message}");
        }
    }

    private static Response ClickWayland(MouseClickRequest request)
    {
        try
        {
            var button = request.Button switch
            {
                MouseButton.Left => "0",
                MouseButton.Right => "1",
                MouseButton.Middle => "2",
                _ => "0"
            };
            
            if (request.Double)
            {
                RunCommand("ydotool", $"click {button}");
                Thread.Sleep(50);
            }
            
            RunCommand("ydotool", $"click {button}");
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Wayland mouse click failed: {ex.Message}");
        }
    }

    private static Response DownWayland(MouseButton button)
    {
        // ydotool doesn't support mouse down/up separately well
        return new ErrorResponse("Mouse down/up not well supported on Wayland");
    }

    private static Response UpWayland(MouseButton button)
    {
        return new ErrorResponse("Mouse down/up not well supported on Wayland");
    }

    private static Response ScrollWayland(MouseScrollRequest request)
    {
        try
        {
            var direction = request.Horizontal ? "h" : "";
            var times = Math.Abs(request.Amount);
            var scrollArg = request.Amount > 0 ? "4" : "5"; // scroll up/down
            
            if (request.Horizontal)
            {
                scrollArg = request.Amount > 0 ? "7" : "6"; // scroll left/right
            }
            
            for (int i = 0; i < times; i++)
            {
                RunCommand("ydotool", $"click {scrollArg}");
            }
            
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Wayland scroll failed: {ex.Message}");
        }
    }

    private static MousePositionResponse GetPositionWayland()
    {
        // Wayland doesn't provide easy way to get mouse position
        // Return last known or 0,0
        return new MousePositionResponse { X = 0, Y = 0 };
    }

    private static string RunXdotool(string arguments)
    {
        return RunCommand("xdotool", arguments);
    }

    private static string RunCommand(string command, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        
        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException($"Failed to start {command}");
        
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        
        if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
            throw new Exception($"{command} failed: {error}");
        
        return output;
    }
}
