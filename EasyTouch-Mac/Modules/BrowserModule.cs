using System.Diagnostics;
using System.Text.Json;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

/// <summary>
/// 浏览器自动化模块 - 通过外部 Playwright 命令
/// 
/// 使用说明：
/// 1. 安装 Node.js
/// 2. 安装 Playwright: npm install -g playwright
/// 3. 安装浏览器: npx playwright install chromium
/// 4. 开始使用浏览器功能
/// </summary>
public static class BrowserModule
{
    private static readonly Dictionary<string, BrowserInstance> Browsers = new();
    private static int _browserCounter = 0;
    private static bool _playwrightChecked = false;
    private static bool _playwrightAvailable = false;

    /// <summary>
    /// 检查 Playwright 是否可用
    /// </summary>
    public static bool IsAvailable()
    {
        if (!_playwrightChecked)
        {
            _playwrightAvailable = CheckPlaywrightAvailable();
            _playwrightChecked = true;
        }
        return _playwrightAvailable;
    }

    /// <summary>
    /// 检查 Playwright 命令是否可用
    /// </summary>
    private static bool CheckPlaywrightAvailable()
    {
        try
        {
            // 首先检查 node 是否可用
            var nodeCheck = RunCommand("node", "--version", 5000);
            if (nodeCheck.StartsWith("ERROR:"))
            {
                _lastError = "Node.js is not installed or not in PATH";
                return false;
            }

            // 检查 npx 是否可用
            var npxCheck = RunCommand("npx", "--version", 5000);
            if (npxCheck.StartsWith("ERROR:"))
            {
                _lastError = "npx is not available. Please install Node.js";
                return false;
            }

            // 检查 playwright 是否安装
            var playwrightCheck = RunCommand("npx", "playwright --version", 10000);
            if (playwrightCheck.StartsWith("ERROR:"))
            {
                _lastError = "Playwright is not installed";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _lastError = $"Failed to check Playwright: {ex.Message}";
            return false;
        }
    }

    private static string _lastError = "";

    /// <summary>
    /// 运行命令并返回输出
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
    /// 启动浏览器
    /// </summary>
    public static Response Launch(BrowserLaunchRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        try
        {
            var browserId = $"browser_{Interlocked.Increment(ref _browserCounter)}";
            
            // 启动浏览器进程
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "npx",
                    Arguments = $"playwright launch --browser={request.BrowserType} --headless={request.Headless.ToString().ToLower()}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            if (!string.IsNullOrEmpty(request.ExecutablePath))
            {
                process.StartInfo.EnvironmentVariables["PLAYWRIGHT_EXECUTABLE_PATH"] = request.ExecutablePath;
            }

            process.Start();
            
            // 读取启动结果
            var output = process.StandardOutput.ReadLine();
            process.WaitForExit(10000);

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                return new ErrorResponse($"Failed to launch browser: {error}");
            }

            var instance = new BrowserInstance
            {
                Id = browserId,
                Type = request.BrowserType,
                Process = process
            };
            Browsers[browserId] = instance;

            return new SuccessResponse<BrowserLaunchResponse>(new BrowserLaunchResponse
            {
                BrowserId = browserId,
                BrowserType = request.BrowserType,
                Version = output?.Trim() ?? "unknown"
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to launch browser: {ex.Message}");
        }
    }

    /// <summary>
    /// 导航到 URL
    /// </summary>
    public static Response Navigate(BrowserNavigateRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.TryGetValue(request.BrowserId, out var instance))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var args = $"playwright navigate --url=\"{request.Url}\"";
            if (!string.IsNullOrEmpty(request.WaitUntil))
            {
                args += $" --wait-until={request.WaitUntil}";
            }
            if (request.Timeout.HasValue)
            {
                args += $" --timeout={request.Timeout.Value}";
            }

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            var response = JsonSerializer.Deserialize<BrowserNavigateResponse>(result);
            return new SuccessResponse<BrowserNavigateResponse>(response!);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Navigation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 点击元素
    /// </summary>
    public static Response Click(BrowserClickRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var args = $"playwright click --selector=\"{request.Selector}\" --selector-type={request.SelectorType ?? "css"}";
            
            if (request.Button.HasValue)
                args += $" --button={request.Button.Value}";
            if (request.ClickCount.HasValue)
                args += $" --click-count={request.ClickCount.Value}";
            if (request.Timeout.HasValue)
                args += $" --timeout={request.Timeout.Value}";

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            return new SuccessResponse<object>(new { Message = $"Element clicked: {request.Selector}" });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Click failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 填充输入框
    /// </summary>
    public static Response Fill(BrowserFillRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var args = $"playwright fill --selector=\"{request.Selector}\" --value=\"{request.Value}\" --selector-type={request.SelectorType ?? "css"}";
            
            if (!request.Clear)
                args += " --no-clear";
            if (request.Timeout.HasValue)
                args += $" --timeout={request.Timeout.Value}";

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            return new SuccessResponse<object>(new { Message = $"Element filled: {request.Selector}" });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Fill failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 查找元素
    /// </summary>
    public static Response Find(BrowserFindRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var args = $"playwright find --selector=\"{request.Selector}\" --selector-type={request.SelectorType ?? "css"}";
            
            if (request.Timeout.HasValue)
                args += $" --timeout={request.Timeout.Value}";

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            var response = JsonSerializer.Deserialize<BrowserFindResponse>(result);
            return new SuccessResponse<BrowserFindResponse>(response!);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Find failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取页面文本
    /// </summary>
    public static Response GetText(BrowserGetTextRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var args = "playwright get-text";
            
            if (!string.IsNullOrEmpty(request.Selector))
            {
                args += $" --selector=\"{request.Selector}\" --selector-type={request.SelectorType ?? "css"}";
            }

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            return new SuccessResponse<BrowserGetTextResponse>(new BrowserGetTextResponse
            {
                Text = result.Trim(),
                Selector = request.Selector
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"GetText failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 页面截图
    /// </summary>
    public static Response Screenshot(BrowserScreenshotRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var outputPath = request.OutputPath ?? Path.Combine(Path.GetTempPath(), $"browser_screenshot_{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.png");
            
            var args = $"playwright screenshot --output=\"{outputPath}\" --type={request.Type}";
            
            if (!string.IsNullOrEmpty(request.Selector))
            {
                args += $" --selector=\"{request.Selector}\" --selector-type={request.SelectorType ?? "css"}";
            }
            if (request.FullPage == true)
                args += " --full-page";
            if (request.Quality.HasValue)
                args += $" --quality={request.Quality.Value}";

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
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
    /// 执行 JavaScript
    /// </summary>
    public static Response Evaluate(BrowserEvaluateRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            // 将脚本写入临时文件
            var tempScriptFile = Path.Combine(Path.GetTempPath(), $"script_{Guid.NewGuid()}.js");
            File.WriteAllText(tempScriptFile, request.Script);

            var args = $"playwright evaluate --script-file=\"{tempScriptFile}\"";

            if (request.Args?.Length > 0)
            {
                args += $" --args=\"{JsonSerializer.Serialize(request.Args).Replace("\"", "\\\"")}\"";
            }

            var result = ExecutePlaywrightCommand(args);
            
            try { File.Delete(tempScriptFile); } catch { }

            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            return new SuccessResponse<BrowserEvaluateResponse>(new BrowserEvaluateResponse
            {
                Result = result.Trim(),
                ResultType = "string"
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Evaluate failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 等待元素
    /// </summary>
    public static Response WaitFor(BrowserWaitForRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var args = $"playwright wait-for --selector=\"{request.Selector}\" --selector-type={request.SelectorType ?? "css"} --state={request.State ?? "visible"}";
            
            if (request.Timeout.HasValue)
                args += $" --timeout={request.Timeout.Value}";

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            return new SuccessResponse<object>(new { Message = $"Element {request.State}: {request.Selector}" });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"WaitFor failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取浏览器列表
    /// </summary>
    public static Response List(BrowserListRequest request)
    {
        try
        {
            var browsers = Browsers.Values.Select(b => new BrowserInfo
            {
                Id = b.Id,
                Type = b.Type,
                Version = b.Version,
                IsConnected = IsBrowserConnected(b)
            }).ToList();

            return new SuccessResponse<BrowserListResponse>(new BrowserListResponse
            {
                Browsers = browsers
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"List failed: {ex.Message}");
        }
    }

    private static bool IsBrowserConnected(BrowserInstance instance)
    {
        if (instance.Process != null)
        {
            try
            {
                return !instance.Process.HasExited;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// 关闭浏览器
    /// </summary>
    public static Response Close(BrowserCloseRequest request)
    {
        if (!Browsers.TryGetValue(request.BrowserId, out var instance))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            if (instance.Process != null && !instance.Process.HasExited)
            {
                if (request.Force)
                {
                    instance.Process.Kill();
                }
                else
                {
                    instance.Process.CloseMainWindow();
                }
                instance.Process.WaitForExit(5000);
            }

            Browsers.Remove(request.BrowserId);
            return new SuccessResponse<object>(new { Message = $"Browser closed: {request.BrowserId}" });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Close failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取页面信息
    /// </summary>
    public static Response GetPageInfo(BrowserGetPageInfoRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var result = ExecutePlaywrightCommand("playwright page-info");
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            var response = JsonSerializer.Deserialize<BrowserGetPageInfoResponse>(result);
            return new SuccessResponse<BrowserGetPageInfoResponse>(response!);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"GetPageInfo failed: {ex.Message}");
        }
    }

    // ==================== 新增功能 ====================

    /// <summary>
    /// 后退
    /// </summary>
    public static Response GoBack(BrowserGoBackRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var args = $"playwright go-back --browser-id={request.BrowserId}";
            if (request.Timeout.HasValue)
            {
                args += $" --timeout={request.Timeout.Value}";
            }

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            return new SuccessResponse<object>(new { Message = "Navigated back" });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"GoBack failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 前进
    /// </summary>
    public static Response GoForward(BrowserGoForwardRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var args = $"playwright go-forward --browser-id={request.BrowserId}";
            if (request.Timeout.HasValue)
            {
                args += $" --timeout={request.Timeout.Value}";
            }

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            return new SuccessResponse<object>(new { Message = "Navigated forward" });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"GoForward failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 刷新页面
    /// </summary>
    public static Response Reload(BrowserReloadRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var args = $"playwright reload --browser-id={request.BrowserId}";
            if (request.Timeout.HasValue)
            {
                args += $" --timeout={request.Timeout.Value}";
            }

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            return new SuccessResponse<object>(new { Message = "Page reloaded" });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Reload failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 滚动页面
    /// </summary>
    public static Response Scroll(BrowserScrollRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var args = $"playwright scroll --browser-id={request.BrowserId} --x={request.X ?? 0} --y={request.Y ?? 0}";
            
            if (!string.IsNullOrEmpty(request.Selector))
            {
                args += $" --selector=\"{request.Selector}\" --selector-type={request.SelectorType ?? "css"}";
            }
            if (!string.IsNullOrEmpty(request.Behavior))
            {
                args += $" --behavior={request.Behavior}";
            }

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            return new SuccessResponse<object>(new { Message = "Scrolled" });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Scroll failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 选择下拉框选项
    /// </summary>
    public static Response Select(BrowserSelectRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var args = $"playwright select --browser-id={request.BrowserId} --selector=\"{request.Selector}\" --values=\"{string.Join(",", request.Values)}\"";
            
            if (!string.IsNullOrEmpty(request.SelectorType))
            {
                args += $" --selector-type={request.SelectorType}";
            }

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            return new SuccessResponse<object>(new { Message = $"Selected: {string.Join(", ", request.Values)}" });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Select failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    public static Response Upload(BrowserUploadRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var args = $"playwright upload --browser-id={request.BrowserId} --selector=\"{request.Selector}\" --files=\"{string.Join(",", request.Files)}\"";
            
            if (!string.IsNullOrEmpty(request.SelectorType))
            {
                args += $" --selector-type={request.SelectorType}";
            }

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            return new SuccessResponse<object>(new { Message = $"Uploaded: {string.Join(", ", request.Files)}" });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Upload failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取 Cookies
    /// </summary>
    public static Response GetCookies(BrowserGetCookiesRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var args = $"playwright get-cookies --browser-id={request.BrowserId}";
            
            if (!string.IsNullOrEmpty(request.Url))
            {
                args += $" --url=\"{request.Url}\"";
            }

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            var cookies = JsonSerializer.Deserialize<List<BrowserCookie>>(result);
            return new SuccessResponse<List<BrowserCookie>>(cookies!);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"GetCookies failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 设置 Cookie
    /// </summary>
    public static Response SetCookie(BrowserSetCookieRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var args = $"playwright set-cookie --browser-id={request.BrowserId} --name=\"{request.Name}\" --value=\"{request.Value}\"";
            
            if (!string.IsNullOrEmpty(request.Domain))
            {
                args += $" --domain=\"{request.Domain}\"";
            }
            if (!string.IsNullOrEmpty(request.Path))
            {
                args += $" --path=\"{request.Path}\"";
            }
            if (request.Expires.HasValue)
            {
                args += $" --expires={request.Expires.Value}";
            }
            if (request.HttpOnly)
            {
                args += " --http-only";
            }
            if (request.Secure)
            {
                args += " --secure";
            }
            if (!string.IsNullOrEmpty(request.SameSite))
            {
                args += $" --same-site={request.SameSite}";
            }

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            return new SuccessResponse<object>(new { Message = $"Cookie set: {request.Name}" });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"SetCookie failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 清除所有 Cookies
    /// </summary>
    public static Response ClearCookies(BrowserClearCookiesRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var args = $"playwright clear-cookies --browser-id={request.BrowserId}";

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            return new SuccessResponse<object>(new { Message = "Cookies cleared" });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"ClearCookies failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 添加网络路由拦截
    /// </summary>
    public static Response AddRoute(BrowserRouteRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var args = $"playwright route --browser-id={request.BrowserId} --url=\"{request.Url}\" --action={request.Action}";
            
            if (request.StatusCode.HasValue)
            {
                args += $" --status-code={request.StatusCode.Value}";
            }
            if (!string.IsNullOrEmpty(request.Body))
            {
                args += $" --body=\"{request.Body}\"";
            }

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            var response = JsonSerializer.Deserialize<BrowserRouteResponse>(result);
            return new SuccessResponse<BrowserRouteResponse>(response!);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"AddRoute failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 移除网络路由拦截
    /// </summary>
    public static Response RemoveRoute(BrowserUnrouteRequest request)
    {
        if (!IsAvailable())
        {
            return new ErrorResponse(GetPlaywrightNotInstalledMessage());
        }

        if (!Browsers.ContainsKey(request.BrowserId))
        {
            return new ErrorResponse($"Browser {request.BrowserId} not found");
        }

        try
        {
            var args = $"playwright unroute --browser-id={request.BrowserId} --route-id=\"{request.RouteId}\"";

            var result = ExecutePlaywrightCommand(args);
            if (result.StartsWith("ERROR:"))
            {
                return new ErrorResponse(result.Substring(6));
            }

            return new SuccessResponse<object>(new { Message = "Route removed" });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"RemoveRoute failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 关闭所有浏览器
    /// </summary>
    public static void CloseAll()
    {
        foreach (var browserId in Browsers.Keys.ToList())
        {
            Close(new BrowserCloseRequest { BrowserId = browserId, Force = true });
        }
    }

    /// <summary>
    /// 执行 Playwright 命令
    /// </summary>
    private static string ExecutePlaywrightCommand(string arguments)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "npx",
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
            process.WaitForExit(30000);

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
    /// 获取 Playwright 未安装的提示信息
    /// </summary>
    private static string GetPlaywrightNotInstalledMessage()
    {
        var osName = OperatingSystem.IsWindows() ? "Windows" :
                     OperatingSystem.IsLinux() ? "Linux" : "macOS";
        
        var message = $@"❌ Browser automation is not available

Detected Issue: {_lastError}

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔧 Installation Guide for {osName}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Step 1: Install Node.js
   • Download from: https://nodejs.org/
   • Recommended: LTS version (16.x or higher)
   • Verify: node --version

Step 2: Install Playwright
   npm install -g playwright

Step 3: Install Browser Binaries
   # Install Chromium only (recommended, ~100MB)
   npx playwright install chromium
   
   # Or install all browsers (~500MB)
   npx playwright install

Step 4: Verify Installation
   npx playwright --version

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📚 Documentation: BROWSER_SETUP.md
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Note: Browser binaries will be downloaded to:
   {GetPlaywrightCachePath()}

After installation, restart EasyTouch.";

        return message;
    }

    /// <summary>
    /// 获取 Playwright 缓存路径
    /// </summary>
    private static string GetPlaywrightCachePath()
    {
        if (OperatingSystem.IsWindows())
        {
            return "%LOCALAPPDATA%\\ms-playwright";
        }
        else if (OperatingSystem.IsMacOS())
        {
            return "~/Library/Caches/ms-playwright";
        }
        else
        {
            return "~/.cache/ms-playwright";
        }
    }

    /// <summary>
    /// 浏览器实例
    /// </summary>
    private class BrowserInstance
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public Process? Process { get; set; }
    }
}
