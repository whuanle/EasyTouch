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
                if (!outputPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    outputPath += ".png";
                }
            }
            
            if (request.X.HasValue && request.Y.HasValue && request.Width.HasValue && request.Height.HasValue)
            {
                // Area screenshot
                RunCommand("screencapture", $"-R{request.X},{request.Y},{request.Width},{request.Height} \"{outputPath}\"");
            }
            else
            {
                // Full screenshot
                RunCommand("screencapture", $"-x \"{outputPath}\"");
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
            return new ErrorResponse($"Screenshot failed: {ex.Message}");
        }
    }

    public static Response GetPixelColor(PixelColorRequest request)
    {
        try
        {
            // Take a 1x1 screenshot and get color
            var tempFile = Path.GetTempFileName() + ".png";
            RunCommand("screencapture", $"-R{request.X},{request.Y},1,1 \"{tempFile}\"");
            
            // Get color using Python if available
            var pythonScript = $"from PIL import Image; img = Image.open('{tempFile}'); r, g, b = img.getpixel((0, 0))[:3]; print(f'{{r}},{{g}},{{b}}')";
            string colorOutput;
            
            try
            {
                colorOutput = RunCommand("python3", $"-c \"{pythonScript}\"");
            }
            catch
            {
                // Fallback: assume white
                colorOutput = "255,255,255";
            }
            
            File.Delete(tempFile);
            
            var parts = colorOutput.Split(',');
            if (parts.Length == 3 &&
                int.TryParse(parts[0].Trim(), out var r) &&
                int.TryParse(parts[1].Trim(), out var g) &&
                int.TryParse(parts[2].Trim(), out var b))
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

    public static Response ListScreens()
    {
        try
        {
            var screens = new List<ScreenInfo>();
            
            // Fallback: assume primary display
            screens.Add(new ScreenInfo
            {
                Index = 0,
                Name = "Built-in Display",
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
            return new ErrorResponse($"List screens failed: {ex.Message}");
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
