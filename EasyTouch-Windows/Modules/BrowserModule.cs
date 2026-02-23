using System.Diagnostics;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

/// <summary>
/// 浏览器自动化模块 - 使用 Playwright CLI
/// 
/// 可用命令：
/// - browser_launch: 使用 playwright open
/// - browser_screenshot: 使用 playwright screenshot
/// - browser_codegen: 使用 playwright codegen
/// 
/// 需要安装：npx playwright install chromium
/// </summary>
public static class BrowserModule
{
    private static int _browserCounter = 0;
    private static string? _npxPath = null;

    /// <summary>
    /// 查找 npx 路径
    /// </summary>
    private static string FindNpxPath()
    {
        if (_npxPath != null) return _npxPath;

        // 尝试常见路径
        var paths = new[]
        {
            @"C:\Program Files\nodejs\npx.cmd",
            @"C:\Program Files (x86)\nodejs\npx.cmd",
            @"G:\Program Files\nodejs\npx.cmd",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"npm\npx.cmd"),
            "npx" // 回退到 PATH
        };

        foreach (var path in paths)
        {
            try
            {
                var result = RunCommand(path, "--version", 5000);
                if (!result.StartsWith("ERROR:"))
                {
                    _npxPath = path;
                    return path;
                }
            }
            catch { }
        }

        return "npx";
    }

    /// <summary>
    /// 检查 Playwright 是否可用
    /// </summary>
    public static bool IsAvailable()
    {
        try
        {
            var npx = FindNpxPath();
            var result = RunCommand(npx, "playwright --version", 10000);
            return !result.StartsWith("ERROR:");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 运行命令
    /// </summary>
    private static string RunCommand(string fileName, string arguments, int timeoutMs)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit(timeoutMs);

            if (process.ExitCode != 0)
            {
                return $"ERROR: {error}";
            }

            return output;
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    /// <summary>
    /// 启动浏览器（使用 playwright open）
    /// </summary>
    public static Response Launch(BrowserLaunchRequest request)
    {
        try
        {
            var npx = FindNpxPath();
            var browserId = $"browser_{Interlocked.Increment(ref _browserCounter)}";
            
            // 构建命令参数
            var args = $"playwright open --browser={request.BrowserType}";
            if (request.Headless)
            {
                args += " --headless";
            }

            // 启动进程（不等待，因为是交互式的）
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = npx,
                    Arguments = args,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    UseShellExecute = true,
                    CreateNoWindow = !request.Headless
                }
            };

            if (!string.IsNullOrEmpty(request.ExecutablePath))
            {
                process.StartInfo.EnvironmentVariables["PLAYWRIGHT_EXECUTABLE_PATH"] = request.ExecutablePath;
            }

            // 尝试启动
            try
            {
                process.Start();
                
                // 如果 headless 模式，稍微等待一下确认启动成功
                if (request.Headless)
                {
                    Thread.Sleep(1000);
                    if (process.HasExited)
                    {
                        return new ErrorResponse("Browser process exited immediately");
                    }
                }

                return new SuccessResponse<BrowserLaunchResponse>(new BrowserLaunchResponse
                {
                    BrowserId = browserId,
                    BrowserType = request.BrowserType,
                    Version = "unknown"
                });
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to start browser: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to launch browser: {ex.Message}");
        }
    }

    /// <summary>
    /// 截图（使用 playwright screenshot）
    /// </summary>
    public static Response Screenshot(BrowserScreenshotRequest request)
    {
        try
        {
            var npx = FindNpxPath();
            var outputPath = request.OutputPath ?? Path.Combine(Path.GetTempPath(), $"screenshot_{Guid.NewGuid()}.png");
            
            // 确保目录存在
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            // 使用示例 URL 截图（因为 open 命令没有返回 URL）
            var url = "about:blank";
            var args = $"playwright screenshot \"{url}\" \"{outputPath}\"";
            
            if (request.FullPage == true)
            {
                args += " --full-page";
            }

            var result = RunCommand(npx, args, 30000);
            
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse($"Screenshot failed: {result.Substring(6)}");
            }

            return new SuccessResponse<BrowserScreenshotResponse>(new BrowserScreenshotResponse
            {
                ImagePath = outputPath,
                Width = 0,
                Height = 0
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Screenshot failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 代码生成（使用 playwright codegen）
    /// </summary>
    public static Response Codegen(BrowserCodegenRequest request)
    {
        try
        {
            var npx = FindNpxPath();
            var args = $"playwright codegen --browser={request.BrowserType ?? "chromium"}";
            
            if (!string.IsNullOrEmpty(request.OutputFile))
            {
                args += $" --output={request.OutputFile}";
            }

            if (!string.IsNullOrEmpty(request.Target))
            {
                args += $" --target={request.Target}";
            }

            if (!string.IsNullOrEmpty(request.Url))
            {
                args += $" \"{request.Url}\"";
            }

            // 启动 codegen 进程（非阻塞）
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = npx,
                    Arguments = args,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    UseShellExecute = true,
                    CreateNoWindow = false
                }
            };

            process.Start();

            return new SuccessResponse<object>(new 
            { 
                Message = "Codegen started. Interact with the browser and code will be generated.",
                ProcessId = process.Id
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Codegen failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 列出浏览器（返回空列表，因为 CLI 模式不维护状态）
    /// </summary>
    public static Response List(BrowserListRequest request)
    {
        return new SuccessResponse<BrowserListResponse>(new BrowserListResponse
        {
            Browsers = new List<BrowserInfo>()
        });
    }

    /// <summary>
    /// 关闭浏览器（在 CLI 模式下只是返回成功）
    /// </summary>
    public static Response Close(BrowserCloseRequest request)
    {
        // CLI 模式下无法追踪进程，直接返回成功
        return new SuccessResponse();
    }

    /// <summary>
    /// 不支持的命令
    /// </summary>
    public static Response Navigate(BrowserNavigateRequest request)
    {
        return new ErrorResponse("Navigate is not supported in CLI mode. Use browser_launch with URL or browser_screenshot instead.");
    }

    public static Response Click(BrowserClickRequest request)
    {
        return new ErrorResponse("Click is not supported in CLI mode. Use browser_codegen to record interactions.");
    }

    public static Response Fill(BrowserFillRequest request)
    {
        return new ErrorResponse("Fill is not supported in CLI mode. Use browser_codegen to record interactions.");
    }

    public static Response Find(BrowserFindRequest request)
    {
        return new ErrorResponse("Find is not supported in CLI mode.");
    }

    public static Response GetText(BrowserGetTextRequest request)
    {
        return new ErrorResponse("GetText is not supported in CLI mode.");
    }

    public static Response Evaluate(BrowserEvaluateRequest request)
    {
        return new ErrorResponse("Evaluate is not supported in CLI mode.");
    }

    public static Response WaitFor(BrowserWaitForRequest request)
    {
        return new ErrorResponse("WaitFor is not supported in CLI mode.");
    }
}

// Codegen 请求
public class BrowserCodegenRequest : Request
{
    public string? BrowserType { get; set; }
    public string? Url { get; set; }
    public string? OutputFile { get; set; }
    public string? Target { get; set; }
}

// 兼容性保留
public class BrowserProcess
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public Process? Process { get; set; }
    public DateTime StartTime { get; set; }
}
