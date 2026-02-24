using System.Text.Json;
using EasyTouch.Core.Models;
using EasyTouch.Modules;

namespace EasyTouch.Mcp;

public static class McpHost
{
    public static void RunStdio()
    {
        Console.Error.WriteLine("EasyTouch MCP Server started (stdio mode)");
        
        while (true)
        {
            try
            {
                var line = Console.ReadLine();
                if (line == null) break;
                
                var request = JsonSerializer.Deserialize<McpRequest>(line);
                if (request == null) continue;

                var response = HandleRequest(request);
                var responseJson = JsonSerializer.Serialize(response);
                Console.WriteLine(responseJson);
            }
            catch (Exception ex)
            {
                var errorResponse = new McpResponse
                {
                    Id = null,
                    Error = new McpError { Code = -1, Message = ex.Message }
                };
                Console.WriteLine(JsonSerializer.Serialize(errorResponse));
            }
        }
    }

    private static McpResponse HandleRequest(McpRequest request)
    {
        try
        {
            var method = request.Method?.ToLowerInvariant() ?? "";

            // Handle standard MCP methods
            if (method == "initialize")
            {
                return new McpResponse
                {
                    Id = request.Id,
                    Result = new
                    {
                        protocolVersion = "2024-11-05",
                        capabilities = new { tools = new { } },
                        serverInfo = new { name = "EasyTouch", version = "1.0.0" }
                    }
                };
            }

            if (method == "tools/list")
            {
                return new McpResponse
                {
                    Id = request.Id,
                    Result = GetToolsList()
                };
            }

            if (method == "tools/call")
            {
                var result = CallTool(request.Params);
                return new McpResponse
                {
                    Id = request.Id,
                    Result = result
                };
            }

            // Legacy action-based handling
            string? action = null;
            if (request.Params?.TryGetProperty("action", out var actionProp) == true)
            {
                action = actionProp.GetString()?.ToLowerInvariant();
            }
            action ??= "";

            var legacyResult = action switch
            {
                // Mouse
                "mouse_move" => MouseModule.Move(ParseRequest<MouseMoveRequest>(request.Params)),
                "mouse_click" => MouseModule.Click(ParseRequest<MouseClickRequest>(request.Params)),
                "mouse_down" => MouseModule.Down(ParseMouseButton(request.Params)),
                "mouse_up" => MouseModule.Up(ParseMouseButton(request.Params)),
                "mouse_scroll" => MouseModule.Scroll(ParseRequest<MouseScrollRequest>(request.Params)),
                "mouse_position" => new SuccessResponse<MousePositionResponse>(MouseModule.GetPosition()),

                // Keyboard
                "key_press" => KeyboardModule.Press(ParseRequest<KeyPressRequest>(request.Params)),
                "key_down" => KeyboardModule.Down(GetStringParam(request.Params, "key") ?? ""),
                "key_up" => KeyboardModule.Up(GetStringParam(request.Params, "key") ?? ""),
                "type_text" => KeyboardModule.TypeText(ParseRequest<TypeTextRequest>(request.Params)),

                // Screen
                "screenshot" => ScreenModule.Screenshot(ParseRequest<ScreenshotRequest>(request.Params)),
                "pixel_color" => ScreenModule.GetPixelColor(ParseRequest<PixelColorRequest>(request.Params)),
                "screen_list" => ScreenModule.ListScreens(),

                // Window
                "window_list" => WindowModule.List(ParseRequest<WindowListRequest>(request.Params)),
                "window_find" => WindowModule.Find(ParseRequest<WindowFindRequest>(request.Params)),
                "window_activate" => WindowModule.Activate(ParseRequest<WindowActivateRequest>(request.Params)),
                "window_foreground" => WindowModule.GetForeground(),

                // System
                "os_info" => SystemModule.GetOsInfo(),
                "cpu_info" => SystemModule.GetCpuInfo(),
                "memory_info" => SystemModule.GetMemoryInfo(),
                "disk_list" => SystemModule.ListDisks(),
                "process_list" => SystemModule.ListProcesses(ParseRequest<ProcessListRequest>(request.Params)),
                "lock_screen" => SystemModule.LockScreen(),

                // Clipboard
                "clipboard_get_text" => ClipboardModule.GetText(new ClipboardGetTextRequest()),
                "clipboard_set_text" => ClipboardModule.SetText(ParseRequest<ClipboardSetTextRequest>(request.Params)),
                "clipboard_clear" => ClipboardModule.Clear(new ClipboardClearRequest()),
                "clipboard_get_files" => ClipboardModule.GetFiles(new ClipboardGetFilesRequest()),

                // Audio
                "volume_get" => AudioModule.GetVolume(new VolumeGetRequest()),
                "volume_set" => AudioModule.SetVolume(ParseRequest<VolumeSetRequest>(request.Params)),
                "volume_mute" => AudioModule.SetMute(ParseRequest<VolumeMuteRequest>(request.Params)),
                "audio_devices" => AudioModule.ListDevices(new AudioDeviceListRequest()),

                // Browser
                "browser_launch" => BrowserModule.Launch(ParseRequest<BrowserLaunchRequest>(request.Params)),
                "browser_navigate" => BrowserModule.Navigate(ParseRequest<BrowserNavigateRequest>(request.Params)),
                "browser_click" => BrowserModule.Click(ParseRequest<BrowserClickRequest>(request.Params)),
                "browser_fill" => BrowserModule.Fill(ParseRequest<BrowserFillRequest>(request.Params)),
                "browser_find" => BrowserModule.Find(ParseRequest<BrowserFindRequest>(request.Params)),
                "browser_get_text" => BrowserModule.GetText(ParseRequest<BrowserGetTextRequest>(request.Params)),
                "browser_screenshot" => BrowserModule.Screenshot(ParseRequest<BrowserScreenshotRequest>(request.Params)),
                "browser_evaluate" => BrowserModule.Evaluate(ParseRequest<BrowserEvaluateRequest>(request.Params)),
                "browser_wait_for" => BrowserModule.WaitFor(ParseRequest<BrowserWaitForRequest>(request.Params)),
                "browser_assert_text" => BrowserModule.AssertText(ParseRequest<BrowserAssertTextRequest>(request.Params)),
                "browser_page_info" => BrowserModule.GetPageInfo(ParseRequest<BrowserGetPageInfoRequest>(request.Params)),
                "browser_go_back" => BrowserModule.GoBack(ParseRequest<BrowserGoBackRequest>(request.Params)),
                "browser_go_forward" => BrowserModule.GoForward(ParseRequest<BrowserGoForwardRequest>(request.Params)),
                "browser_reload" => BrowserModule.Reload(ParseRequest<BrowserReloadRequest>(request.Params)),
                "browser_scroll" => BrowserModule.Scroll(ParseRequest<BrowserScrollRequest>(request.Params)),
                "browser_select" => BrowserModule.Select(ParseRequest<BrowserSelectRequest>(request.Params)),
                "browser_upload" => BrowserModule.Upload(ParseRequest<BrowserUploadRequest>(request.Params)),
                "browser_get_cookies" => BrowserModule.GetCookies(ParseRequest<BrowserGetCookiesRequest>(request.Params)),
                "browser_set_cookie" => BrowserModule.SetCookie(ParseRequest<BrowserSetCookieRequest>(request.Params)),
                "browser_clear_cookies" => BrowserModule.ClearCookies(ParseRequest<BrowserClearCookiesRequest>(request.Params)),
                "browser_run_script" => BrowserModule.RunScript(ParseRequest<BrowserRunScriptRequest>(request.Params)),
                "browser_close" => BrowserModule.Close(ParseRequest<BrowserCloseRequest>(request.Params)),
                "browser_list" => BrowserModule.List(new BrowserListRequest()),

                _ => new ErrorResponse($"Unknown action: {action}")
            };

            return new McpResponse
            {
                Id = request.Id,
                Result = legacyResult
            };
        }
        catch (Exception ex)
        {
            return new McpResponse
            {
                Id = request.Id,
                Error = new McpError { Code = -1, Message = ex.Message }
            };
        }
    }

    private static object GetToolsList()
    {
        var tools = new List<object>
        {
            new { name = "mouse_move", description = "Move mouse cursor to specified coordinates", inputSchema = new { type = "object", properties = new { x = new { type = "integer" }, y = new { type = "integer" }, relative = new { type = "boolean" }, duration = new { type = "integer" } }, required = new[] { "x", "y" } } },
            new { name = "mouse_click", description = "Click mouse button", inputSchema = new { type = "object", properties = new { button = new { type = "string", @enum = new[] { "left", "right", "middle" } }, @double = new { type = "boolean" } } } },
            new { name = "mouse_down", description = "Press and hold mouse button", inputSchema = new { type = "object", properties = new { button = new { type = "string", @enum = new[] { "left", "right", "middle" } } } } },
            new { name = "mouse_up", description = "Release mouse button", inputSchema = new { type = "object", properties = new { button = new { type = "string", @enum = new[] { "left", "right", "middle" } } } } },
            new { name = "mouse_scroll", description = "Scroll mouse wheel", inputSchema = new { type = "object", properties = new { amount = new { type = "integer" } }, required = new[] { "amount" } } },
            new { name = "mouse_position", description = "Get current mouse position", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } },
            new { name = "key_press", description = "Press a key", inputSchema = new { type = "object", properties = new { key = new { type = "string" } }, required = new[] { "key" } } },
            new { name = "key_down", description = "Press and hold a key", inputSchema = new { type = "object", properties = new { key = new { type = "string" } }, required = new[] { "key" } } },
            new { name = "key_up", description = "Release a key", inputSchema = new { type = "object", properties = new { key = new { type = "string" } }, required = new[] { "key" } } },
            new { name = "type_text", description = "Type text", inputSchema = new { type = "object", properties = new { text = new { type = "string" }, interval = new { type = "integer" }, humanLike = new { type = "boolean" } }, required = new[] { "text" } } },
            new { name = "screenshot", description = "Take a screenshot", inputSchema = new { type = "object", properties = new { x = new { type = "integer" }, y = new { type = "integer" }, width = new { type = "integer" }, height = new { type = "integer" }, outputPath = new { type = "string" } } } },
            new { name = "pixel_color", description = "Get color of a pixel", inputSchema = new { type = "object", properties = new { x = new { type = "integer" }, y = new { type = "integer" } }, required = new[] { "x", "y" } } },
            new { name = "screen_list", description = "List all screens/monitors", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } },
            new { name = "window_list", description = "List all windows", inputSchema = new { type = "object", properties = new { visibleOnly = new { type = "boolean" }, titleFilter = new { type = "string" } } } },
            new { name = "window_find", description = "Find a window", inputSchema = new { type = "object", properties = new { title = new { type = "string" }, className = new { type = "string" }, processId = new { type = "integer" } } } },
            new { name = "window_activate", description = "Activate a window", inputSchema = new { type = "object", properties = new { handle = new { type = "integer" } }, required = new[] { "handle" } } },
            new { name = "window_foreground", description = "Get the foreground window", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } },
            new { name = "os_info", description = "Get OS information", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } },
            new { name = "cpu_info", description = "Get CPU information", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } },
            new { name = "memory_info", description = "Get memory information", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } },
            new { name = "disk_list", description = "List disk drives", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } },
            new { name = "process_list", description = "List running processes", inputSchema = new { type = "object", properties = new { nameFilter = new { type = "string" } } } },
            new { name = "lock_screen", description = "Lock the screen", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } },
            new { name = "clipboard_get_text", description = "Get text from clipboard", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } },
            new { name = "clipboard_set_text", description = "Set text to clipboard", inputSchema = new { type = "object", properties = new { text = new { type = "string" } }, required = new[] { "text" } } },
            new { name = "clipboard_clear", description = "Clear clipboard", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } },
            new { name = "clipboard_get_files", description = "Get files from clipboard", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } },
            new { name = "volume_get", description = "Get current volume level", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } },
            new { name = "volume_set", description = "Set volume level (0-100)", inputSchema = new { type = "object", properties = new { level = new { type = "integer", minimum = 0, maximum = 100 } }, required = new[] { "level" } } },
            new { name = "volume_mute", description = "Mute/unmute audio", inputSchema = new { type = "object", properties = new { muted = new { type = "boolean" } }, required = new[] { "muted" } } },
            new { name = "audio_devices", description = "List audio devices", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } }
        };

        // 添加浏览器工具（如果配置了插件）
        if (BrowserModule.IsAvailable())
        {
            tools.AddRange(new List<object>
            {
                new { name = "browser_launch", description = "Launch a browser instance", inputSchema = new { type = "object", properties = new { browserType = new { type = "string", @enum = new[] { "chromium", "firefox", "webkit", "edge" } }, headless = new { type = "boolean" }, executablePath = new { type = "string" }, userDataDir = new { type = "string" } }, required = new[] { "browserType" } } },
                new { name = "browser_navigate", description = "Navigate to URL", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, url = new { type = "string" }, waitUntil = new { type = "string", @enum = new[] { "load", "domcontentloaded", "networkidle" } }, timeout = new { type = "integer" } }, required = new[] { "browserId", "url" } } },
                new { name = "browser_click", description = "Click element", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, selector = new { type = "string" }, selectorType = new { type = "string", @enum = new[] { "css", "xpath", "text", "id" } }, button = new { type = "integer" }, clickCount = new { type = "integer" } }, required = new[] { "browserId", "selector" } } },
                new { name = "browser_fill", description = "Fill input field", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, selector = new { type = "string" }, value = new { type = "string" }, clear = new { type = "boolean" } }, required = new[] { "browserId", "selector", "value" } } },
                new { name = "browser_find", description = "Find element", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, selector = new { type = "string" }, selectorType = new { type = "string" } }, required = new[] { "browserId", "selector" } } },
                new { name = "browser_get_text", description = "Get page text", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, selector = new { type = "string" } }, required = new[] { "browserId" } } },
                new { name = "browser_screenshot", description = "Take page screenshot", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, selector = new { type = "string" }, fullPage = new { type = "boolean" }, outputPath = new { type = "string" } }, required = new[] { "browserId" } } },
                new { name = "browser_evaluate", description = "Execute JavaScript", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, script = new { type = "string" }, args = new { type = "array" } }, required = new[] { "browserId", "script" } } },
                new { name = "browser_wait_for", description = "Wait for element", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, selector = new { type = "string" }, state = new { type = "string", @enum = new[] { "visible", "hidden", "attached", "detached" } }, timeout = new { type = "integer" } }, required = new[] { "browserId", "selector" } } },
                new { name = "browser_assert_text", description = "Assert text in page or element", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, selector = new { type = "string" }, expectedText = new { type = "string" }, exactMatch = new { type = "boolean" }, ignoreCase = new { type = "boolean" } }, required = new[] { "browserId", "expectedText" } } },
                new { name = "browser_page_info", description = "Get current page info", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" } }, required = new[] { "browserId" } } },
                new { name = "browser_go_back", description = "Navigate back", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, timeout = new { type = "integer" } }, required = new[] { "browserId" } } },
                new { name = "browser_go_forward", description = "Navigate forward", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, timeout = new { type = "integer" } }, required = new[] { "browserId" } } },
                new { name = "browser_reload", description = "Reload page", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, timeout = new { type = "integer" } }, required = new[] { "browserId" } } },
                new { name = "browser_scroll", description = "Scroll page or element", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, x = new { type = "integer" }, y = new { type = "integer" }, selector = new { type = "string" } }, required = new[] { "browserId" } } },
                new { name = "browser_select", description = "Select option in dropdown", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, selector = new { type = "string" }, values = new { type = "array", items = new { type = "string" } } }, required = new[] { "browserId", "selector", "values" } } },
                new { name = "browser_upload", description = "Upload files", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, selector = new { type = "string" }, files = new { type = "array", items = new { type = "string" } } }, required = new[] { "browserId", "selector", "files" } } },
                new { name = "browser_get_cookies", description = "Get cookies", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, url = new { type = "string" } }, required = new[] { "browserId" } } },
                new { name = "browser_set_cookie", description = "Set cookie", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, name = new { type = "string" }, value = new { type = "string" }, domain = new { type = "string" }, path = new { type = "string" } }, required = new[] { "browserId", "name", "value" } } },
                new { name = "browser_clear_cookies", description = "Clear all cookies", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" } }, required = new[] { "browserId" } } },
                new { name = "browser_run_script", description = "Run JS/TS Playwright test script file", inputSchema = new { type = "object", properties = new { scriptPath = new { type = "string" }, browserType = new { type = "string", @enum = new[] { "chromium", "firefox", "webkit", "edge" } }, headless = new { type = "boolean" }, timeout = new { type = "integer" }, extraArgs = new { type = "array", items = new { type = "string" } } }, required = new[] { "scriptPath" } } },
                new { name = "browser_close", description = "Close browser", inputSchema = new { type = "object", properties = new { browserId = new { type = "string" }, force = new { type = "boolean" } }, required = new[] { "browserId" } } },
                new { name = "browser_list", description = "List browser instances", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } }
            });
        }

        return new { tools = tools.ToArray() };
    }

    private static object CallTool(JsonElement? paramsElement)
    {
        if (paramsElement == null)
            throw new ArgumentException("Params are required");

        string? toolName = null;
        if (paramsElement.Value.TryGetProperty("name", out var nameProp))
        {
            toolName = nameProp.GetString()?.ToLowerInvariant();
        }

        if (string.IsNullOrEmpty(toolName))
            throw new ArgumentException("Tool name is required");

        JsonElement? arguments = null;
        if (paramsElement.Value.TryGetProperty("arguments", out var argsProp))
        {
            arguments = argsProp;
        }

        Response result = toolName switch
        {
            "mouse_move" => MouseModule.Move(ParseElement<MouseMoveRequest>(arguments)),
            "mouse_click" => MouseModule.Click(ParseElement<MouseClickRequest>(arguments)),
            "mouse_down" => MouseModule.Down(ParseMouseButtonFromElement(arguments)),
            "mouse_up" => MouseModule.Up(ParseMouseButtonFromElement(arguments)),
            "mouse_scroll" => MouseModule.Scroll(ParseElement<MouseScrollRequest>(arguments)),
            "mouse_position" => new SuccessResponse<MousePositionResponse>(MouseModule.GetPosition()),
            "key_press" => KeyboardModule.Press(ParseElement<KeyPressRequest>(arguments)),
            "key_down" => KeyboardModule.Down(GetStringFromElement(arguments, "key") ?? ""),
            "key_up" => KeyboardModule.Up(GetStringFromElement(arguments, "key") ?? ""),
            "type_text" => KeyboardModule.TypeText(ParseElement<TypeTextRequest>(arguments)),
            "screenshot" => ScreenModule.Screenshot(ParseElement<ScreenshotRequest>(arguments)),
            "pixel_color" => ScreenModule.GetPixelColor(ParseElement<PixelColorRequest>(arguments)),
            "screen_list" => ScreenModule.ListScreens(),
            "window_list" => WindowModule.List(ParseElement<WindowListRequest>(arguments)),
            "window_find" => WindowModule.Find(ParseElement<WindowFindRequest>(arguments)),
            "window_activate" => WindowModule.Activate(ParseElement<WindowActivateRequest>(arguments)),
            "window_foreground" => WindowModule.GetForeground(),
            "os_info" => SystemModule.GetOsInfo(),
            "cpu_info" => SystemModule.GetCpuInfo(),
            "memory_info" => SystemModule.GetMemoryInfo(),
            "disk_list" => SystemModule.ListDisks(),
            "process_list" => SystemModule.ListProcesses(ParseElement<ProcessListRequest>(arguments)),
            "lock_screen" => SystemModule.LockScreen(),
            "clipboard_get_text" => ClipboardModule.GetText(new ClipboardGetTextRequest()),
            "clipboard_set_text" => ClipboardModule.SetText(ParseElement<ClipboardSetTextRequest>(arguments)),
            "clipboard_clear" => ClipboardModule.Clear(new ClipboardClearRequest()),
            "clipboard_get_files" => ClipboardModule.GetFiles(new ClipboardGetFilesRequest()),
            "volume_get" => AudioModule.GetVolume(new VolumeGetRequest()),
                "volume_set" => AudioModule.SetVolume(ParseElement<VolumeSetRequest>(arguments)),
                "volume_mute" => AudioModule.SetMute(ParseElement<VolumeMuteRequest>(arguments)),
                "audio_devices" => AudioModule.ListDevices(new AudioDeviceListRequest()),
                // Browser tools
                "browser_launch" => BrowserModule.Launch(ParseElement<BrowserLaunchRequest>(arguments)),
                "browser_navigate" => BrowserModule.Navigate(ParseElement<BrowserNavigateRequest>(arguments)),
                "browser_click" => BrowserModule.Click(ParseElement<BrowserClickRequest>(arguments)),
                "browser_fill" => BrowserModule.Fill(ParseElement<BrowserFillRequest>(arguments)),
                "browser_find" => BrowserModule.Find(ParseElement<BrowserFindRequest>(arguments)),
                "browser_get_text" => BrowserModule.GetText(ParseElement<BrowserGetTextRequest>(arguments)),
                "browser_screenshot" => BrowserModule.Screenshot(ParseElement<BrowserScreenshotRequest>(arguments)),
                "browser_evaluate" => BrowserModule.Evaluate(ParseElement<BrowserEvaluateRequest>(arguments)),
                "browser_wait_for" => BrowserModule.WaitFor(ParseElement<BrowserWaitForRequest>(arguments)),
                "browser_assert_text" => BrowserModule.AssertText(ParseElement<BrowserAssertTextRequest>(arguments)),
                "browser_page_info" => BrowserModule.GetPageInfo(ParseElement<BrowserGetPageInfoRequest>(arguments)),
                "browser_go_back" => BrowserModule.GoBack(ParseElement<BrowserGoBackRequest>(arguments)),
                "browser_go_forward" => BrowserModule.GoForward(ParseElement<BrowserGoForwardRequest>(arguments)),
                "browser_reload" => BrowserModule.Reload(ParseElement<BrowserReloadRequest>(arguments)),
                "browser_scroll" => BrowserModule.Scroll(ParseElement<BrowserScrollRequest>(arguments)),
                "browser_select" => BrowserModule.Select(ParseElement<BrowserSelectRequest>(arguments)),
                "browser_upload" => BrowserModule.Upload(ParseElement<BrowserUploadRequest>(arguments)),
                "browser_get_cookies" => BrowserModule.GetCookies(ParseElement<BrowserGetCookiesRequest>(arguments)),
                "browser_set_cookie" => BrowserModule.SetCookie(ParseElement<BrowserSetCookieRequest>(arguments)),
                "browser_clear_cookies" => BrowserModule.ClearCookies(ParseElement<BrowserClearCookiesRequest>(arguments)),
                "browser_run_script" => BrowserModule.RunScript(ParseElement<BrowserRunScriptRequest>(arguments)),
                "browser_close" => BrowserModule.Close(ParseElement<BrowserCloseRequest>(arguments)),
                "browser_list" => BrowserModule.List(new BrowserListRequest()),
                _ => new ErrorResponse($"Unknown tool: {toolName}")
        };

        return ToToolResult(result);
    }

    private static object ToToolResult(Response response)
    {
        if (response is ErrorResponse error)
        {
            return new
            {
                content = new[] { new { type = "text", text = error.Error ?? "Unknown error" } },
                isError = true
            };
        }

        var json = JsonSerializer.Serialize(response);
        return new
        {
            content = new[] { new { type = "text", text = json } },
            isError = false
        };
    }

    private static T ParseElement<T>(JsonElement? element) where T : Request, new()
    {
        if (element == null) return new T();
        return JsonSerializer.Deserialize<T>(element.Value.GetRawText()) ?? new T();
    }

    private static MouseButton ParseMouseButtonFromElement(JsonElement? element)
    {
        if (element == null) return MouseButton.Left;
        string? button = null;
        if (element.Value.TryGetProperty("button", out var buttonProp))
        {
            button = buttonProp.GetString();
        }
        button ??= "left";
        return button.ToLowerInvariant() switch
        {
            "left" => MouseButton.Left,
            "right" => MouseButton.Right,
            "middle" => MouseButton.Middle,
            _ => MouseButton.Left
        };
    }

    private static string? GetStringFromElement(JsonElement? element, string key)
    {
        if (element == null) return null;
        if (element.Value.TryGetProperty(key, out var prop))
        {
            return prop.GetString();
        }
        return null;
    }

    private static T ParseRequest<T>(JsonElement? element) where T : Request, new()
    {
        if (element == null) return new T();
        return JsonSerializer.Deserialize<T>(element.Value.GetRawText()) ?? new T();
    }

    private static MouseButton ParseMouseButton(JsonElement? element)
    {
        if (element == null) return MouseButton.Left;
        string? button = null;
        if (element.Value.TryGetProperty("button", out var buttonProp))
        {
            button = buttonProp.GetString();
        }
        button ??= "left";
        return button.ToLowerInvariant() switch
        {
            "left" => MouseButton.Left,
            "right" => MouseButton.Right,
            "middle" => MouseButton.Middle,
            _ => MouseButton.Left
        };
    }

    private static string? GetStringParam(JsonElement? element, string key)
    {
        if (element == null) return null;
        if (element.Value.TryGetProperty(key, out var prop))
        {
            return prop.GetString();
        }
        return null;
    }
}

public class McpRequest
{
    public string? Id { get; set; }
    public string? Method { get; set; }
    public JsonElement? Params { get; set; }
}

public class McpResponse
{
    public string? Id { get; set; }
    public object? Result { get; set; }
    public McpError? Error { get; set; }
}

public class McpError
{
    public int Code { get; set; }
    public string? Message { get; set; }
}
