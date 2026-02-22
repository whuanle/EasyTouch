using System.Diagnostics;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class WindowModule
{
    public static Response List(WindowListRequest request)
    {
        try
        {
            if (IsWayland())
            {
                return ListWayland(request);
            }
            
            var windows = new List<WindowInfo>();
            
            // Get window list using wmctrl or xdotool
            if (CommandExists("wmctrl"))
            {
                var output = RunCommand("wmctrl", "-l -p");
                var lines = output.Split('\n');
                
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    // Format: 0x0440000b 0 1234 Window Title
                    var parts = line.Split(new[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        if (long.TryParse(parts[0].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out var handle))
                        {
                            var desktop = parts[1];
                            var pidStr = parts[2];
                            var title = parts[3];
                            
                            if (!string.IsNullOrEmpty(request.TitleFilter) &&
                                !title.Contains(request.TitleFilter, StringComparison.OrdinalIgnoreCase))
                                continue;
                            
                            uint pid = 0;
                            uint.TryParse(pidStr, out pid);
                            
                            // Get window geometry
                            var (x, y, width, height) = GetWindowGeometry(handle);
                            var isVisible = desktop != "-1";
                            
                            if (request.VisibleOnly && !isVisible)
                                continue;
                            
                            windows.Add(new WindowInfo
                            {
                                Handle = handle,
                                Title = title,
                                ClassName = GetWindowClass(handle),
                                ProcessId = pid,
                                ProcessName = GetProcessName(pid),
                                X = x,
                                Y = y,
                                Width = width,
                                Height = height,
                                IsVisible = isVisible,
                                IsMinimized = false,
                                IsMaximized = false
                            });
                        }
                    }
                }
            }
            else if (CommandExists("xdotool"))
            {
                // Fallback to xdotool
                var output = RunCommand("xdotool", "search --all --onlyvisible --class \".*\"");
                var ids = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var id in ids)
                {
                    if (long.TryParse(id, out var handle))
                    {
                        try
                        {
                            var title = RunCommand("xdotool", $"getwindowname {handle}").Trim();
                            var (x, y, width, height) = GetWindowGeometry(handle);
                            
                            if (!string.IsNullOrEmpty(request.TitleFilter) &&
                                !title.Contains(request.TitleFilter, StringComparison.OrdinalIgnoreCase))
                                continue;
                            
                            windows.Add(new WindowInfo
                            {
                                Handle = handle,
                                Title = title,
                                X = x,
                                Y = y,
                                Width = width,
                                Height = height,
                                IsVisible = true
                            });
                        }
                        catch { }
                    }
                }
            }
            
            return new SuccessResponse<WindowListResponse>(new WindowListResponse { Windows = windows });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"List windows failed: {ex.Message}");
        }
    }

    public static Response Find(WindowFindRequest request)
    {
        try
        {
            var listResult = List(new WindowListRequest { VisibleOnly = false });
            if (listResult is SuccessResponse<WindowListResponse> success)
            {
                var windows = success.Data.Windows;
                
                WindowInfo? found = null;
                
                if (!string.IsNullOrEmpty(request.Title))
                {
                    found = windows.FirstOrDefault(w =>
                        w.Title.Contains(request.Title, StringComparison.OrdinalIgnoreCase));
                }
                else if (!string.IsNullOrEmpty(request.ClassName))
                {
                    found = windows.FirstOrDefault(w =>
                        w.ClassName.Contains(request.ClassName, StringComparison.OrdinalIgnoreCase));
                }
                else if (request.ProcessId.HasValue)
                {
                    found = windows.FirstOrDefault(w => w.ProcessId == request.ProcessId.Value);
                }
                
                return new SuccessResponse<WindowFindResponse>(new WindowFindResponse
                {
                    Window = found,
                    Handle = found?.Handle
                });
            }
            
            return new ErrorResponse("Failed to find window");
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Find window failed: {ex.Message}");
        }
    }

    public static Response Activate(WindowActivateRequest request)
    {
        try
        {
            if (IsWayland())
            {
                return ActivateWayland(request);
            }
            
            if (CommandExists("wmctrl"))
            {
                RunCommand("wmctrl", $"-i -r {request.Handle} -b add,above");
                RunCommand("wmctrl", $"-i -a {request.Handle}");
            }
            else if (CommandExists("xdotool"))
            {
                RunCommand("xdotool", $"windowactivate {request.Handle}");
            }
            else
            {
                return new ErrorResponse("No window manager tool found");
            }
            
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Activate window failed: {ex.Message}");
        }
    }

    public static Response GetForeground()
    {
        try
        {
            if (IsWayland())
            {
                return new SuccessResponse<WindowInfo>(new WindowInfo { Title = "Wayland - Not supported" });
            }
            
            if (CommandExists("xdotool"))
            {
                var output = RunCommand("xdotool", "getactivewindow");
                if (long.TryParse(output.Trim(), out var handle))
                {
                    var title = RunCommand("xdotool", $"getwindowname {handle}").Trim();
                    var (x, y, width, height) = GetWindowGeometry(handle);
                    
                    return new SuccessResponse<WindowInfo>(new WindowInfo
                    {
                        Handle = handle,
                        Title = title,
                        X = x,
                        Y = y,
                        Width = width,
                        Height = height,
                        IsVisible = true
                    });
                }
            }
            
            return new ErrorResponse("Cannot get foreground window");
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Get foreground window failed: {ex.Message}");
        }
    }

    private static Response ListWayland(WindowListRequest request)
    {
        // Wayland doesn't provide easy window enumeration
        return new SuccessResponse<WindowListResponse>(new WindowListResponse { Windows = new List<WindowInfo>() });
    }

    private static Response ActivateWayland(WindowActivateRequest request)
    {
        // Wayland doesn't support direct window activation from other processes
        return new ErrorResponse("Window activation not supported on Wayland");
    }

    private static (int x, int y, int width, int height) GetWindowGeometry(long handle)
    {
        try
        {
            if (CommandExists("xwininfo"))
            {
                var output = RunCommand("xwininfo", $"-id {handle}");
                
                var xMatch = System.Text.RegularExpressions.Regex.Match(output, @"Absolute upper-left X:\s+(-?\d+)");
                var yMatch = System.Text.RegularExpressions.Regex.Match(output, @"Absolute upper-left Y:\s+(-?\d+)");
                var wMatch = System.Text.RegularExpressions.Regex.Match(output, @"Width:\s+(\d+)");
                var hMatch = System.Text.RegularExpressions.Regex.Match(output, @"Height:\s+(\d+)");
                
                int x = 0, y = 0, w = 0, h = 0;
                
                if (xMatch.Success) int.TryParse(xMatch.Groups[1].Value, out x);
                if (yMatch.Success) int.TryParse(yMatch.Groups[1].Value, out y);
                if (wMatch.Success) int.TryParse(wMatch.Groups[1].Value, out w);
                if (hMatch.Success) int.TryParse(hMatch.Groups[1].Value, out h);
                
                return (x, y, w, h);
            }
        }
        catch { }
        
        return (0, 0, 0, 0);
    }

    private static string GetWindowClass(long handle)
    {
        try
        {
            if (CommandExists("xprop"))
            {
                var output = RunCommand("xprop", $"-id {handle} WM_CLASS");
                var match = System.Text.RegularExpressions.Regex.Match(output, "\"([^\"]+)\"");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
        }
        catch { }
        
        return "";
    }

    private static string GetProcessName(uint pid)
    {
        try
        {
            if (File.Exists($"/proc/{pid}/comm"))
            {
                return File.ReadAllText($"/proc/{pid}/comm").Trim();
            }
        }
        catch { }
        
        return "";
    }

    private static bool IsWayland()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));
    }

    private static bool CommandExists(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "which",
                Arguments = command,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            
            using var process = Process.Start(psi);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
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
