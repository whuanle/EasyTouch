using System.Diagnostics;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class WindowModule
{
    public static Response List(WindowListRequest request)
    {
        try
        {
            var windows = new List<WindowInfo>();
            // Note: Window enumeration on macOS requires accessibility permissions
            // For now, return an empty list
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
            // Use osascript to activate application by PID
            var script = $"tell application \"System Events\" to set frontmost of first application process whose unix id is {request.Handle} to true";
            RunAppleScript(script);
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
            var script = "tell application \"System Events\" to get name of first application process whose frontmost is true";
            var appName = RunAppleScript(script);
            
            return new SuccessResponse<WindowInfo>(new WindowInfo
            {
                Title = "Foreground Window",
                ProcessName = appName.Trim(),
                IsVisible = true
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Get foreground window failed: {ex.Message}");
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
