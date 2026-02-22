using System.Diagnostics;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class AudioModule
{
    public static Response GetVolume(VolumeGetRequest request)
    {
        try
        {
            int volume = 0;
            bool isMuted = false;
            
            // Try amixer first
            if (CommandExists("amixer"))
            {
                var output = RunCommand("amixer", "sget Master");
                
                // Parse volume: [50%]
                var volMatch = System.Text.RegularExpressions.Regex.Match(output, "\\[(\\d+)%\\]");
                if (volMatch.Success)
                {
                    int.TryParse(volMatch.Groups[1].Value, out volume);
                }
                
                // Parse mute: [on] or [off]
                isMuted = output.Contains("[off]");
            }
            // Try pactl (PulseAudio)
            else if (CommandExists("pactl"))
            {
                var output = RunCommand("pactl", "list sinks");
                var volMatch = System.Text.RegularExpressions.Regex.Match(output, "Volume:.*? (\\d+)%");
                if (volMatch.Success)
                {
                    int.TryParse(volMatch.Groups[1].Value, out volume);
                }
                
                isMuted = output.Contains("Mute: yes");
            }
            // Try wpctl (PipeWire)
            else if (CommandExists("wpctl"))
            {
                var output = RunCommand("wpctl", "get-volume @DEFAULT_AUDIO_SINK@");
                var volMatch = System.Text.RegularExpressions.Regex.Match(output, @"Volume: ([\d.]+)");
                if (volMatch.Success && double.TryParse(volMatch.Groups[1].Value, out var vol))
                {
                    volume = (int)(vol * 100);
                }
                
                isMuted = output.Contains("[MUTED]");
            }
            else
            {
                return new ErrorResponse("No audio control tool found. Please install alsa-utils, pulseaudio-utils, or wireplumber.");
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
            
            if (CommandExists("amixer"))
            {
                RunCommand("amixer", $"sset Master {level}%");
            }
            else if (CommandExists("pactl"))
            {
                RunCommand("pactl", $"set-sink-volume @DEFAULT_SINK@ {level}%");
            }
            else if (CommandExists("wpctl"))
            {
                var vol = level / 100.0;
                RunCommand("wpctl", $"set-volume @DEFAULT_AUDIO_SINK@ {vol:F2}");
            }
            else
            {
                return new ErrorResponse("No audio control tool found");
            }
            
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
            if (CommandExists("amixer"))
            {
                var state = request.Mute ? "mute" : "unmute";
                RunCommand("amixer", $"sset Master {state}");
            }
            else if (CommandExists("pactl"))
            {
                var state = request.Mute ? "1" : "0";
                RunCommand("pactl", $"set-sink-mute @DEFAULT_SINK@ {state}");
            }
            else if (CommandExists("wpctl"))
            {
                var state = request.Mute ? "mute" : "unmute";
                RunCommand("wpctl", $"set-mute @DEFAULT_AUDIO_SINK@ {state}");
            }
            else
            {
                return new ErrorResponse("No audio control tool found");
            }
            
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
            
            if (CommandExists("pactl"))
            {
                var output = RunCommand("pactl", "list sinks");
                var lines = output.Split('\n');
                int index = 0;
                
                foreach (var line in lines)
                {
                    if (line.StartsWith("Name: "))
                    {
                        var name = line.Substring(6).Trim();
                        var isDefault = name.Contains("@DEFAULT_SINK@");
                        
                        devices.Add(new AudioDeviceInfo
                        {
                            Id = name,
                            Name = name,
                            IsDefault = isDefault,
                            IsInput = false
                        });
                        index++;
                    }
                }
            }
            else if (CommandExists("wpctl"))
            {
                var output = RunCommand("wpctl", "status");
                // Parse wpctl output for devices
                // This is a simplified version
            }
            else if (CommandExists("amixer"))
            {
                // ALSA doesn't have device enumeration like PulseAudio
                devices.Add(new AudioDeviceInfo
                {
                    Id = "default",
                    Name = "Default ALSA Device",
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
