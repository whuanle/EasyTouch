using System.Text.Json;
using System.Text.Encodings.Web;
using EasyTouch.Core.Models;
using EasyTouch.Modules;

namespace EasyTouch.Mcp;

public static class McpHost
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void RunStdio()
    {
        Console.Error.WriteLine("EasyTouch MCP Server started (stdio mode)");
        
        while (true)
        {
            try
            {
                var line = Console.ReadLine();
                if (line == null) break;
                
                var request = JsonSerializer.Deserialize<McpRequest>(line, JsonOptions);
                if (request == null) continue;

                var response = HandleRequest(request);
                var responseJson = JsonSerializer.Serialize(response, JsonOptions);
                Console.WriteLine(responseJson);
            }
            catch (Exception ex)
            {
                var errorResponse = new McpResponse
                {
                    Id = null,
                    Error = new McpError { Code = -1, Message = ex.Message }
                };
                Console.WriteLine(JsonSerializer.Serialize(errorResponse, JsonOptions));
            }
        }
    }

    private static McpResponse HandleRequest(McpRequest request)
    {
        try
        {
            string? action = null;
            if (request.Params?.TryGetProperty("action", out var actionProp) == true)
            {
                action = actionProp.GetString()?.ToLowerInvariant();
            }
            action ??= "";
            var result = action switch
            {
                // Mouse
                "mouse_move" => MouseModule.Move(ParseRequest<MouseMoveRequest>(request.Params)),
                "mouse_click" => MouseModule.Click(ParseRequest<MouseClickRequest>(request.Params)),
                "mouse_down" => MouseModule.Down(ParseMouseButton(request.Params)),
                "mouse_up" => MouseModule.Up(ParseMouseButton(request.Params)),
                "mouse_scroll" => MouseModule.Scroll(ParseRequest<MouseScrollRequest>(request.Params)),
                "mouse_drag" => MouseModule.Drag(ParseRequest<MouseDragRequest>(request.Params)),
                "mouse_position" => MouseModule.GetPosition(),

                // Keyboard
                "key_press" => KeyboardModule.Press(ParseRequest<KeyPressRequest>(request.Params)),
                "key_down" => KeyboardModule.Down(GetStringParam(request.Params, "key") ?? ""),
                "key_up" => KeyboardModule.Up(GetStringParam(request.Params, "key") ?? ""),
                "key_combo" => KeyboardModule.Combo(ParseRequest<KeyComboRequest>(request.Params)),
                "type_text" => KeyboardModule.TypeText(ParseRequest<TypeTextRequest>(request.Params)),
                "key_state" => KeyboardModule.GetKeyState(ParseRequest<KeyStateRequest>(request.Params)),

                // Screen
                "screenshot" => ScreenModule.Screenshot(ParseRequest<ScreenshotRequest>(request.Params)),
                "pixel_color" => ScreenModule.GetPixelColor(ParseRequest<PixelColorRequest>(request.Params)),
                "screen_list" => ScreenModule.ListScreens(),

                // Window
                "window_list" => WindowModule.List(ParseRequest<WindowListRequest>(request.Params)),
                "window_find" => WindowModule.Find(ParseRequest<WindowFindRequest>(request.Params)),
                "window_activate" => WindowModule.Activate(ParseRequest<WindowActivateRequest>(request.Params)),
                "window_show" => WindowModule.Show(ParseRequest<WindowShowRequest>(request.Params)),
                "window_move" => WindowModule.Move(ParseRequest<WindowMoveRequest>(request.Params)),
                "window_topmost" => WindowModule.SetTopmost(ParseRequest<WindowSetTopmostRequest>(request.Params)),
                "window_close" => WindowModule.Close(ParseRequest<WindowCloseRequest>(request.Params)),
                "window_foreground" => WindowModule.GetForeground(),

                // System
                "os_info" => SystemModule.GetOsInfo(),
                "cpu_info" => SystemModule.GetCpuInfo(),
                "memory_info" => SystemModule.GetMemoryInfo(),
                "disk_list" => SystemModule.ListDisks(),
                "process_list" => SystemModule.ListProcesses(ParseRequest<ProcessListRequest>(request.Params)),
                "process_start" => SystemModule.StartProcess(ParseRequest<ProcessStartRequest>(request.Params)),
                "process_kill" => SystemModule.KillProcess(ParseRequest<ProcessKillRequest>(request.Params)),
                "lock_screen" => SystemModule.LockScreen(),
                "shutdown" => SystemModule.Shutdown(),
                "restart" => SystemModule.Restart(),
                "logoff" => SystemModule.Logoff(),

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

                _ => new ErrorResponse($"Unknown action: {action}")
            };

            return new McpResponse
            {
                Id = request.Id,
                Result = result
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
            "x1" => MouseButton.XButton1,
            "x2" => MouseButton.XButton2,
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
