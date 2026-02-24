using System.Diagnostics;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class ClipboardModule
{
    private const int ClipboardCommandTimeoutMs = 5000;

    public static Response GetText(ClipboardGetTextRequest request)
    {
        try
        {
            string text;
            
            if (IsWayland() && CommandExists("wl-paste"))
            {
                text = RunCommand("wl-paste", "");
            }
            else if (CommandExists("xclip"))
            {
                text = RunCommand("xclip", "-selection clipboard -o");
            }
            else if (CommandExists("xsel"))
            {
                text = RunCommand("xsel", "-b -o");
            }
            else
            {
                return new ErrorResponse(GetMissingClipboardToolMessage());
            }
            
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
            if (IsWayland() && CommandExists("wl-copy"))
            {
                RunWlCopyText(request.Text);
            }
            else if (CommandExists("xclip"))
            {
                RunCommandWithInput("xclip", "-selection clipboard", request.Text);
            }
            else if (CommandExists("xsel"))
            {
                RunCommandWithInput("xsel", "-b -i", request.Text);
            }
            else
            {
                return new ErrorResponse(GetMissingClipboardToolMessage());
            }
            
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
            if (IsWayland() && CommandExists("wl-copy"))
            {
                RunWlCopyClear();
            }
            else if (CommandExists("xclip"))
            {
                RunCommandWithInput("xclip", "-selection clipboard", "");
            }
            else if (CommandExists("xsel"))
            {
                RunCommand("xsel", "-b -c");
            }
            else
            {
                return new ErrorResponse(GetMissingClipboardToolMessage());
            }
            
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
            // Try to get files from clipboard
            var files = new List<string>();
            
            // Check for text/uri-list format
            string output;
            if (IsWayland() && CommandExists("wl-paste"))
            {
                output = RunCommand("wl-paste", "--list-types");
            }
            else if (CommandExists("xclip"))
            {
                output = RunCommand("xclip", "-selection clipboard -t TARGETS -o");
            }
            else
            {
                return new SuccessResponse<ClipboardGetFilesResponse>(new ClipboardGetFilesResponse { Files = files });
            }
            
            if (output.Contains("text/uri-list"))
            {
                string uris;
                if (IsWayland() && CommandExists("wl-paste"))
                {
                    uris = RunCommand("wl-paste", "-t text/uri-list");
                }
                else
                {
                    uris = RunCommand("xclip", "-selection clipboard -t text/uri-list -o");
                }
                
                foreach (var line in uris.Split('\n'))
                {
                    if (line.StartsWith("file://"))
                    {
                        var path = Uri.UnescapeDataString(line.Substring(7));
                        files.Add(path);
                    }
                }
            }
            
            return new SuccessResponse<ClipboardGetFilesResponse>(new ClipboardGetFilesResponse { Files = files });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Get clipboard files failed: {ex.Message}");
        }
    }

    private static string GetMissingClipboardToolMessage()
    {
        return "No clipboard tool found. Install wl-clipboard (wl-copy/wl-paste) or xclip/xsel.";
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

        var stdout = new System.Text.StringBuilder();
        var stderr = new System.Text.StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) stdout.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) stderr.AppendLine(e.Data);
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(ClipboardCommandTimeoutMs))
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new Exception($"{command} timed out");
        }

        process.WaitForExit();

        var output = stdout.ToString();
        var error = stderr.ToString();

        if (process.ExitCode != 0)
            throw new Exception(!string.IsNullOrWhiteSpace(error) ? $"{command} failed: {error}" : $"{command} failed with exit code {process.ExitCode}");

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

        var stderr = new System.Text.StringBuilder();
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) stderr.AppendLine(e.Data);
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.StandardInput.Write(input);
        process.StandardInput.Close();

        if (!process.WaitForExit(ClipboardCommandTimeoutMs))
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new Exception($"{command} timed out");
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = stderr.ToString();
            throw new Exception(!string.IsNullOrWhiteSpace(error) ? $"{command} failed: {error}" : $"{command} failed with exit code {process.ExitCode}");
        }
    }

    private static void RunWlCopyText(string input)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "wl-copy",
            Arguments = "",
            RedirectStandardInput = true,
            // Redirect output streams so wl-copy background process does not keep
            // the parent CLI stdout/stderr pipes open.
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException("Failed to start wl-copy");

        var stderr = new System.Text.StringBuilder();
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) stderr.AppendLine(e.Data);
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.StandardInput.Write(input);
        process.StandardInput.Close();

        // wl-copy usually forks to background and keeps clipboard ownership.
        // Do not wait for full exit to avoid blocking the caller.
        if (process.WaitForExit(300) && process.ExitCode != 0)
        {
            var error = stderr.ToString();
            throw new Exception(!string.IsNullOrWhiteSpace(error)
                ? $"wl-copy failed: {error}"
                : $"wl-copy failed with exit code {process.ExitCode}");
        }
    }

    private static void RunWlCopyClear()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "wl-copy",
            Arguments = "--clear",
            // Redirect output streams so wl-copy background process does not keep
            // the parent CLI stdout/stderr pipes open.
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException("Failed to start wl-copy");

        var stderr = new System.Text.StringBuilder();
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) stderr.AppendLine(e.Data);
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (process.WaitForExit(300) && process.ExitCode != 0)
        {
            var error = stderr.ToString();
            throw new Exception(!string.IsNullOrWhiteSpace(error)
                ? $"wl-copy --clear failed: {error}"
                : $"wl-copy --clear failed with exit code {process.ExitCode}");
        }
    }
}
