using System.Diagnostics;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class AudioModule
{
    public static Response GetVolume(VolumeGetRequest request)
    {
        try
        {
            // Use AppleScript to get volume - single line
            var script = "tell application \"System Events\" to set currentVolume to output volume of (get volume settings) \u0026 \",\" \u0026 (output muted of (get volume settings))";
            var output = RunAppleScript(script).Trim();
            var parts = output.Split(',');
            
            int volume = 0;
            bool isMuted = false;
            
            if (parts.Length >= 2)
            {
                int.TryParse(parts[0].Trim(), out volume);
                bool.TryParse(parts[1].Trim(), out isMuted);
            }
            
            return new SuccessResponse<VolumeGetResponse>(new VolumeGetResponse
            {
                Level = volume,
                IsMuted = isMuted
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Get volume failed: {ex.Message}");
        }
    }

    public static Response SetVolume(VolumeSetRequest request)
    {
        try
        {
            var level = Math.Clamp(request.Level, 0, 100);
            var script = $"set volume output volume {level}";
            RunAppleScript(script);
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Set volume failed: {ex.Message}");
        }
    }

    public static Response SetMute(VolumeMuteRequest request)
    {
        try
        {
            var state = request.Mute ? "true" : "false";
            var script = $"set volume with output muted {state}";
            RunAppleScript(script);
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Set mute failed: {ex.Message}");
        }
    }

    public static Response ListDevices(AudioDeviceListRequest request)
    {
        try
        {
            var devices = new List<AudioDeviceInfo>();
            
            // Try to get default output device
            try
            {
                var script = "tell application \"System Events\" to get name of (get current audio output device)";
                var defaultDevice = RunAppleScript(script).Trim();
                devices.Add(new AudioDeviceInfo
                {
                    Id = "default",
                    Name = defaultDevice,
                    IsDefault = true,
                    IsInput = false
                });
            }
            catch
            {
                // Fallback
                devices.Add(new AudioDeviceInfo
                {
                    Id = "default",
                    Name = "System Default",
                    IsDefault = true,
                    IsInput = false
                });
            }
            
            return new SuccessResponse<AudioDeviceListResponse>(new AudioDeviceListResponse { Devices = devices });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"List audio devices failed: {ex.Message}");
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
