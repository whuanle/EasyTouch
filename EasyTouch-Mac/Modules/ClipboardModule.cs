using System.Diagnostics;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class ClipboardModule
{
    public static Response GetText(ClipboardGetTextRequest request)
    {
        try
        {
            var text = RunCommand("pbpaste", "");
            return new SuccessResponse<ClipboardGetTextResponse>(new ClipboardGetTextResponse { Text = text });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Get clipboard text failed: {ex.Message}");
        }
    }

    public static Response SetText(ClipboardSetTextRequest request)
    {
        try
        {
            RunCommandWithInput("pbcopy", "", request.Text);
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Set clipboard text failed: {ex.Message}");
        }
    }

    public static Response Clear(ClipboardClearRequest request)
    {
        try
        {
            RunCommandWithInput("pbcopy", "", "");
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Clear clipboard failed: {ex.Message}");
        }
    }

    public static Response GetFiles(ClipboardGetFilesRequest request)
    {
        try
        {
            var files = new List<string>();
            // Note: Getting files from clipboard on macOS requires complex AppleScript
            // For now, return empty list
            return new SuccessResponse<ClipboardGetFilesResponse>(new ClipboardGetFilesResponse { Files = files });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Get clipboard files failed: {ex.Message}");
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

    private static void RunCommandWithInput(string command, string arguments, string input)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        
        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException($"Failed to start {command}");
        
        process.StandardInput.Write(input);
        process.StandardInput.Close();
        
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        
        if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
            throw new Exception($"{command} failed: {error}");
    }
}
