using System.Diagnostics;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class ScreenModule
{
    public static Response Screenshot(ScreenshotRequest request)
    {
        try
        {
            string outputPath;
            
            if (string.IsNullOrEmpty(request.OutputPath))
            {
                outputPath = Path.Combine(Path.GetTempPath(), $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            }
            else
            {
                outputPath = request.OutputPath;
            }
            
            if (IsWayland())
            {
                return ScreenshotWayland(request, outputPath);
            }
            
            // X11 screenshot using import (ImageMagick) or gnome-screenshot
            if (request.X.HasValue && request.Y.HasValue && request.Width.HasValue && request.Height.HasValue)
            {
                // Area screenshot
                if (CommandExists("gnome-screenshot"))
                {
                    RunCommand("gnome-screenshot", $"-a -f \"{outputPath}\"");
                }
                else if (CommandExists("import"))
                {
                    RunCommand("import", $"-window root -crop {request.Width}x{request.Height}+{request.X}+{request.Y} \"{outputPath}\"");
                }
                else
                {
                    // Fallback to xwd + convert
                    RunCommand("xwd", $"-root -silent | convert - -crop {request.Width}x{request.Height}+{request.X}+{request.Y} \"{outputPath}\"");
                }
            }
            else
            {
                // Full screenshot
                if (CommandExists("gnome-screenshot"))
                {
                    RunCommand("gnome-screenshot", $"-f \"{outputPath}\"");
                }
                else if (CommandExists("import"))
                {
                    RunCommand("import", $"-window root \"{outputPath}\"");
                }
                else
                {
                    RunCommand("xwd", $"-root -silent | convert - \"{outputPath}\"");
                }
            }
            
            if (!File.Exists(outputPath))
            {
                return new ErrorResponse("Screenshot file was not created");
            }
            
            var info = new FileInfo(outputPath);
            return new SuccessResponse<ScreenshotResponse>(new ScreenshotResponse
            {
                Path = outputPath,
                Width = request.Width ?? 1920,
                Height = request.Height ?? 1080
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Screenshot failed: {ex.Message}");
        }
    }

    private static Response ScreenshotWayland(ScreenshotRequest request, string outputPath)
    {
        try
        {
            // Try different Wayland screenshot tools
            if (CommandExists("grim"))
            {
                if (request.X.HasValue && request.Y.HasValue && request.Width.HasValue && request.Height.HasValue)
                {
                    RunCommand("grim", $"-g \"{request.X},{request.Y} {request.Width}x{request.Height}\" \"{outputPath}\"");
                }
                else
                {
                    RunCommand("grim", $"\"{outputPath}\"");
                }
            }
            else if (CommandExists("gnome-screenshot"))
            {
                RunCommand("gnome-screenshot", $"-f \"{outputPath}\"");
            }
            else
            {
                return new ErrorResponse("No Wayland screenshot tool found. Please install grim or gnome-screenshot.");
            }
            
            if (!File.Exists(outputPath))
            {
                return new ErrorResponse("Screenshot file was not created");
            }
            
            return new SuccessResponse<ScreenshotResponse>(new ScreenshotResponse
            {
                Path = outputPath,
                Width = request.Width ?? 1920,
                Height = request.Height ?? 1080
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Wayland screenshot failed: {ex.Message}");
        }
    }

    public static Response GetPixelColor(PixelColorRequest request)
    {
        try
        {
            if (IsWayland())
            {
                return GetPixelColorWayland(request);
            }
            
            // Use ImageMagick's convert
            var output = RunCommand("convert", $"x:{request.X},{request.Y} -format \"%[fx:int(255*r)],%[fx:int(255*g)],%[fx:int(255*b)]\" info:");
            var parts = output.Split(',');
            
            if (parts.Length == 3 &&
                int.TryParse(parts[0], out var r) &&
                int.TryParse(parts[1], out var g) &&
                int.TryParse(parts[2], out var b))
            {
                return new SuccessResponse<PixelColorResponse>(new PixelColorResponse
                {
                    R = r,
                    G = g,
                    B = b
                });
            }
            
            return new ErrorResponse("Failed to parse pixel color");
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Get pixel color failed: {ex.Message}");
        }
    }

    private static Response GetPixelColorWayland(PixelColorRequest request)
    {
        // Wayland doesn't provide easy pixel color access
        // Take a 1x1 screenshot and get color
        try
        {
            var tempFile = Path.GetTempFileName() + ".png";
            RunCommand("grim", $"-g \"{request.X},{request.Y} 1x1\" \"{tempFile}\"");
            
            var output = RunCommand("convert", $"\"{tempFile}\" -format \"%[fx:int(255*r)],%[fx:int(255*g)],%[fx:int(255*b)]\" info:");
            File.Delete(tempFile);
            
            var parts = output.Split(',');
            if (parts.Length == 3 &&
                int.TryParse(parts[0], out var r) &&
                int.TryParse(parts[1], out var g) &&
                int.TryParse(parts[2], out var b))
            {
                return new SuccessResponse<PixelColorResponse>(new PixelColorResponse
                {
                    R = r,
                    G = g,
                    B = b
                });
            }
            
            return new ErrorResponse("Failed to parse pixel color");
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Wayland pixel color failed: {ex.Message}");
        }
    }

    public static Response ListScreens()
    {
        try
        {
            var screens = new List<ScreenInfo>();
            
            if (IsWayland())
            {
                return ListScreensWayland();
            }
            
            // X11 - use xrandr
            var output = RunCommand("xrandr", "--listactivemonitors");
            var lines = output.Split('\n');
            int index = 0;
            
            foreach (var line in lines.Skip(1)) // Skip header
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                // Parse:  0: +*DP-1 1920/531x1080/299+0+0  DP-1
                var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                {
                    var geometry = parts[2]; // 1920/531x1080/299+0+0
                    var name = parts[3];
                    var isPrimary = line.Contains("+{");
                    
                    // Parse geometry
                    var dims = geometry.Split(new[] { 'x', '+' });
                    if (dims.Length >= 2 &&
                        int.TryParse(dims[0].Split('/')[0], out var width) &&
                        int.TryParse(dims[1].Split('/')[0], out var height))
                    {
                        int x = 0, y = 0;
                        if (dims.Length >= 4)
                        {
                            int.TryParse(dims[2], out x);
                            int.TryParse(dims[3], out y);
                        }
                        
                        screens.Add(new ScreenInfo
                        {
                            Index = index++,
                            Name = name,
                            X = x,
                            Y = y,
                            Width = width,
                            Height = height,
                            IsPrimary = isPrimary
                        });
                    }
                }
            }
            
            return new SuccessResponse<ScreenListResponse>(new ScreenListResponse { Screens = screens });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"List screens failed: {ex.Message}");
        }
    }

    private static Response ListScreensWayland()
    {
        try
        {
            var screens = new List<ScreenInfo>();
            
            // Try wlr-randr for wlroots-based compositors
            if (CommandExists("wlr-randr"))
            {
                var output = RunCommand("wlr-randr", "");
                // Parse output (format varies by compositor)
                // This is a simplified version
            }
            
            // Fallback: assume single screen
            screens.Add(new ScreenInfo
            {
                Index = 0,
                Name = "Wayland",
                X = 0,
                Y = 0,
                Width = 1920,
                Height = 1080,
                IsPrimary = true
            });
            
            return new SuccessResponse<ScreenListResponse>(new ScreenListResponse { Screens = screens });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"List Wayland screens failed: {ex.Message}");
        }
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
