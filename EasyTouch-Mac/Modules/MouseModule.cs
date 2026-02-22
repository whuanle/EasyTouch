using System.Diagnostics;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class MouseModule
{
    public static Response Move(MouseMoveRequest request)
    {
        try
        {
            if (request.Duration > 0)
            {
                // Smooth movement
                var currentPos = GetPosition();
                var steps = request.Duration / 10;
                var stepX = (request.X - currentPos.X) / (double)steps;
                var stepY = (request.Y - currentPos.Y) / (double)steps;
                
                for (int i = 0; i < steps; i++)
                {
                    var newX = (int)(currentPos.X + stepX * i);
                    var newY = (int)(currentPos.Y + stepY * i);
                    RunAppleScript($"tell application \"System Events\" to set mouse location to {{{newX}, {newY}}}");
                    Thread.Sleep(10);
                }
            }
            
            RunAppleScript($"tell application \"System Events\" to set mouse location to {{{request.X}, {request.Y}}}");
            
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
            var button = request.Button switch
            {
                MouseButton.Left => "left",
                MouseButton.Right => "right",
                MouseButton.Middle => "middle",
                _ => "left"
            };
            
            if (request.Double)
            {
                RunAppleScript($"tell application \"System Events\" to {button} click");
                Thread.Sleep(50);
                RunAppleScript($"tell application \"System Events\" to {button} click");
            }
            else
            {
                RunAppleScript($"tell application \"System Events\" to {button} click");
            }
            
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
            var btn = button switch
            {
                MouseButton.Left => "left",
                MouseButton.Right => "right",
                MouseButton.Middle => "middle",
                _ => "left"
            };
            
            RunAppleScript($"tell application \"System Events\" to {btn} mouse down");
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
            var btn = button switch
            {
                MouseButton.Left => "left",
                MouseButton.Right => "right",
                MouseButton.Middle => "middle",
                _ => "left"
            };
            
            RunAppleScript($"tell application \"System Events\" to {btn} mouse up");
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
            var direction = request.Amount > 0 ? "up" : "down";
            var times = Math.Abs(request.Amount);
            
            for (int i = 0; i < times; i++)
            {
                if (request.Horizontal)
                {
                    // macOS doesn't have native horizontal scroll in AppleScript
                    // Use cliclick as fallback
                    RunCommand("cliclick", $"hl{direction}");
                }
                else
                {
                    RunAppleScript($"tell application \"System Events\" to scroll {direction}");
                }
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
            var output = RunAppleScript("tell application \"System Events\" to get mouse location");
            // Parse {x, y} format
            var match = System.Text.RegularExpressions.Regex.Match(output, "\\{(\\d+),\\s*(\\d+)\\}");
            if (match.Success &&
                int.TryParse(match.Groups[1].Value, out var x) &&
                int.TryParse(match.Groups[2].Value, out var y))
            {
                return new MousePositionResponse { X = x, Y = y };
            }
            
            return new MousePositionResponse { X = 0, Y = 0 };
        }
        catch
        {
            return new MousePositionResponse { X = 0, Y = 0 };
        }
    }

    private static string RunAppleScript(string script)
    {
        return RunCommand("osascript", $"-e '{script.Replace("'", "'\\''")}'");
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
