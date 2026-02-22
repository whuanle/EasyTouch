using System.Diagnostics;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class KeyboardModule
{
    private static readonly Dictionary<string, string> KeyMap = new()
    {
        // Special keys
        ["enter"] = "Return",
        ["return"] = "Return",
        ["tab"] = "Tab",
        ["space"] = "space",
        ["backspace"] = "BackSpace",
        ["delete"] = "Delete",
        ["del"] = "Delete",
        ["escape"] = "Escape",
        ["esc"] = "Escape",
        ["home"] = "Home",
        ["end"] = "End",
        ["pageup"] = "Page_Up",
        ["pagedown"] = "Page_Down",
        ["up"] = "Up",
        ["down"] = "Down",
        ["left"] = "Left",
        ["right"] = "Right",
        ["f1"] = "F1",
        ["f2"] = "F2",
        ["f3"] = "F3",
        ["f4"] = "F4",
        ["f5"] = "F5",
        ["f6"] = "F6",
        ["f7"] = "F7",
        ["f8"] = "F8",
        ["f9"] = "F9",
        ["f10"] = "F10",
        ["f11"] = "F11",
        ["f12"] = "F12",
        // Modifiers
        ["ctrl"] = "Control_L",
        ["control"] = "Control_L",
        ["alt"] = "Alt_L",
        ["shift"] = "Shift_L",
        ["win"] = "Super_L",
        ["command"] = "Super_L",
        ["meta"] = "Super_L",
    };

    public static Response Press(KeyPressRequest request)
    {
        try
        {
            var keys = request.Key.Split('+', StringSplitOptions.RemoveEmptyEntries);
            
            if (keys.Length > 1)
            {
                // Handle key combinations
                var keydowns = new List<string>();
                var keyups = new List<string>();
                
                foreach (var key in keys)
                {
                    var normalized = NormalizeKey(key.Trim());
                    keydowns.Add($"keydown {normalized}");
                    keyups.Insert(0, $"keyup {normalized}");
                }
                
                RunXdotool(string.Join(" ", keydowns));
                Thread.Sleep(50);
                RunXdotool(string.Join(" ", keyups));
            }
            else
            {
                // Single key
                var key = NormalizeKey(keys[0]);
                RunXdotool($"key {key}");
            }
            
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Key press failed: {ex.Message}");
        }
    }

    public static Response Down(string key)
    {
        try
        {
            var normalized = NormalizeKey(key);
            RunXdotool($"keydown {normalized}");
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Key down failed: {ex.Message}");
        }
    }

    public static Response Up(string key)
    {
        try
        {
            var normalized = NormalizeKey(key);
            RunXdotool($"keyup {normalized}");
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Key up failed: {ex.Message}");
        }
    }

    public static Response TypeText(TypeTextRequest request)
    {
        try
        {
            if (IsWayland())
            {
                return TypeTextWayland(request);
            }
            
            if (request.HumanLike && request.Interval > 0)
            {
                // Simulate human typing
                foreach (var c in request.Text)
                {
                    var key = CharToX11Key(c);
                    if (!string.IsNullOrEmpty(key))
                    {
                        RunXdotool($"key {key}");
                        Thread.Sleep(request.Interval + new Random().Next(-10, 10));
                    }
                    else
                    {
                        RunXdotool($"type \"{c}\"");
                        Thread.Sleep(request.Interval);
                    }
                }
            }
            else if (request.Interval > 0)
            {
                RunXdotool($"type --delay {request.Interval} \"{EscapeText(request.Text)}\"");
            }
            else
            {
                RunXdotool($"type \"{EscapeText(request.Text)}\"");
            }
            
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Type text failed: {ex.Message}");
        }
    }

    private static Response TypeTextWayland(TypeTextRequest request)
    {
        try
        {
            if (request.HumanLike && request.Interval > 0)
            {
                foreach (var c in request.Text)
                {
                    RunCommand("ydotool", $"type \"{c}\"");
                    Thread.Sleep(request.Interval);
                }
            }
            else
            {
                RunCommand("ydotool", $"type \"{EscapeText(request.Text)}\"");
            }
            
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Wayland type text failed: {ex.Message}");
        }
    }

    private static string NormalizeKey(string key)
    {
        var lower = key.ToLowerInvariant();
        if (KeyMap.TryGetValue(lower, out var mapped))
            return mapped;
        
        // Return single character as-is
        if (key.Length == 1)
            return key;
        
        return key;
    }

    private static string? CharToX11Key(char c)
    {
        return c switch
        {
            '\n' => "Return",
            '\t' => "Tab",
            ' ' => "space",
            _ => null
        };
    }

    private static string EscapeText(string text)
    {
        return text.Replace("\"", "\\\"").Replace("$", "\\$");
    }

    private static bool IsWayland()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));
    }

    private static void RunXdotool(string arguments)
    {
        RunCommand("xdotool", arguments);
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
