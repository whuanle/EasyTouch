using System.Diagnostics;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class KeyboardModule
{
    private static readonly Dictionary<string, string> KeyMap = new()
    {
        ["enter"] = "return",
        ["return"] = "return",
        ["tab"] = "tab",
        ["space"] = "space",
        ["backspace"] = "delete",
        ["delete"] = "forward delete",
        ["del"] = "forward delete",
        ["escape"] = "escape",
        ["esc"] = "escape",
        ["home"] = "home",
        ["end"] = "end",
        ["pageup"] = "page up",
        ["pagedown"] = "page down",
        ["up"] = "up",
        ["down"] = "down",
        ["left"] = "left",
        ["right"] = "right",
        ["f1"] = "f1",
        ["f2"] = "f2",
        ["f3"] = "f3",
        ["f4"] = "f4",
        ["f5"] = "f5",
        ["f6"] = "f6",
        ["f7"] = "f7",
        ["f8"] = "f8",
        ["f9"] = "f9",
        ["f10"] = "f10",
        ["f11"] = "f11",
        ["f12"] = "f12",
    };

    public static Response Press(KeyPressRequest request)
    {
        try
        {
            var keys = request.Key.Split('+', StringSplitOptions.RemoveEmptyEntries);
            
            if (keys.Length > 1)
            {
                // Handle key combinations
                var modifiers = new List<string>();
                var mainKey = "";
                
                foreach (var key in keys)
                {
                    var k = key.Trim().ToLowerInvariant();
                    if (k is "ctrl" or "control" or "command" or "cmd" or "alt" or "option" or "shift")
                    {
                        modifiers.Add(k switch
                        {
                            "ctrl" or "control" => "control down",
                            "command" or "cmd" => "command down",
                            "alt" or "option" => "option down",
                            "shift" => "shift down",
                            _ => ""
                        });
                    }
                    else
                    {
                        mainKey = NormalizeKey(k);
                    }
                }
                
                var script = $"tell application \"System Events\" to keystroke \"{mainKey}\" using {{{string.Join(", ", modifiers)}}}";
                RunAppleScript(script);
            }
            else
            {
                // Single key
                var key = NormalizeKey(keys[0]);
                RunAppleScript($"tell application \"System Events\" to key code {GetKeyCode(key)}");
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
            RunAppleScript($"tell application \"System Events\" to key down {GetKeyCode(normalized)}");
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
            RunAppleScript($"tell application \"System Events\" to key up {GetKeyCode(normalized)}");
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
            if (request.HumanLike && request.Interval > 0)
            {
                // Simulate human typing
                foreach (var c in request.Text)
                {
                    RunAppleScript($"tell application \"System Events\" to keystroke \"{EscapeChar(c)}\"");
                    Thread.Sleep(request.Interval + new Random().Next(-10, 10));
                }
            }
            else
            {
                RunAppleScript($"tell application \"System Events\" to keystroke \"{EscapeText(request.Text)}\"");
            }
            
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Type text failed: {ex.Message}");
        }
    }

    private static string NormalizeKey(string key)
    {
        var lower = key.ToLowerInvariant();
        if (KeyMap.TryGetValue(lower, out var mapped))
            return mapped;
        return key;
    }

    private static int GetKeyCode(string key)
    {
        // macOS key codes (simplified, most common keys)
        return key.ToLowerInvariant() switch
        {
            "a" => 0,
            "s" => 1,
            "d" => 2,
            "f" => 3,
            "h" => 4,
            "g" => 5,
            "z" => 6,
            "x" => 7,
            "c" => 8,
            "v" => 9,
            "b" => 11,
            "q" => 12,
            "w" => 13,
            "e" => 14,
            "r" => 15,
            "y" => 16,
            "t" => 17,
            "1" => 18,
            "2" => 19,
            "3" => 20,
            "4" => 21,
            "6" => 22,
            "5" => 23,
            "=" => 24,
            "9" => 25,
            "7" => 26,
            "-" => 27,
            "8" => 28,
            "0" => 29,
            "]" => 30,
            "o" => 31,
            "u" => 32,
            "[" => 33,
            "i" => 34,
            "p" => 35,
            "return" => 36,
            "l" => 37,
            "j" => 38,
            "'" => 39,
            "k" => 40,
            ";" => 41,
            "\\" => 42,
            "," => 43,
            "/" => 44,
            "n" => 45,
            "m" => 46,
            "." => 47,
            "tab" => 48,
            "space" => 49,
            "`" => 50,
            "delete" => 51,
            "escape" => 53,
            "command" => 55,
            "shift" => 56,
            "caps lock" => 57,
            "option" => 58,
            "control" => 59,
            "right shift" => 60,
            "right option" => 61,
            "right control" => 62,
            "left" => 123,
            "right" => 124,
            "down" => 125,
            "up" => 126,
            _ => 0
        };
    }

    private static string EscapeText(string text)
    {
        return text.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    private static string EscapeChar(char c)
    {
        return c switch
        {
            '"' => "\\\"",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            _ => c.ToString()
        };
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
