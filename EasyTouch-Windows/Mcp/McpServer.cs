using System.Text.Json;
using System.Text.Json.Serialization;
using EasyTouch.Core;
using EasyTouch.Core.Models;
using EasyTouch.Modules;

namespace EasyTouch.Mcp;

public static class McpServer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonContext.Default.Options)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task RunAsync()
    {
        await Console.Error.WriteLineAsync("EasyTouch MCP Server started (stdio mode)");
        
        // Send initialization response
        await SendMessage(new JsonRpcResponse
        {
            Id = null,
            Result = new { protocolVersion = "2024-11-05", capabilities = new { tools = new { } }, serverInfo = new { name = "EasyTouch", version = "1.0.0" } }
        });

        while (true)
        {
            try
            {
                var line = await Console.In.ReadLineAsync();
                if (line == null) break;
                
                var request = JsonSerializer.Deserialize<JsonRpcRequest>(line, JsonOptions);
                if (request == null) continue;

                var response = await HandleRequestAsync(request);
                await SendMessage(response);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            }
        }
    }

    private static async Task SendMessage(object message)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);
        await Console.Out.WriteLineAsync(json);
        await Console.Out.FlushAsync();
    }

    private static async Task<JsonRpcResponse> HandleRequestAsync(JsonRpcRequest request)
    {
        try
        {
            var method = request.Method?.ToLowerInvariant() ?? "";
            var args = request.Params ?? new Dictionary<string, JsonElement>();

            object? result = method switch
            {
                "tools/list" => GetToolsList(),
                "tools/call" => await CallToolAsync(args),
                _ => null
            };

            if (result == null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32601, Message = $"Method not found: {method}" }
                };
            }

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = result
            };
        }
        catch (Exception ex)
        {
            return new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError { Code = -32603, Message = ex.Message }
            };
        }
    }

    private static object GetToolsList()
    {
        return new
        {
            tools = new object[]
            {
                new { name = "mouse_move", description = "Move mouse cursor to specified coordinates", inputSchema = new { type = "object", properties = new { x = new { type = "integer" }, y = new { type = "integer" }, relative = new { type = "boolean" }, duration = new { type = "integer" } }, required = new[] { "x", "y" } } },
                new { name = "mouse_click", description = "Click mouse button", inputSchema = new { type = "object", properties = new { button = new { type = "string", @enum = new[] { "left", "right", "middle" } }, @double = new { type = "boolean" } } } },
                new { name = "mouse_position", description = "Get current mouse position", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } },
                new { name = "key_press", description = "Press a key", inputSchema = new { type = "object", properties = new { key = new { type = "string" } }, required = new[] { "key" } } },
                new { name = "type_text", description = "Type text", inputSchema = new { type = "object", properties = new { text = new { type = "string" }, interval = new { type = "integer" }, humanLike = new { type = "boolean" } }, required = new[] { "text" } } },
                new { name = "screenshot", description = "Take a screenshot", inputSchema = new { type = "object", properties = new { x = new { type = "integer" }, y = new { type = "integer" }, width = new { type = "integer" }, height = new { type = "integer" }, outputPath = new { type = "string" } } } },
                new { name = "pixel_color", description = "Get color of a pixel", inputSchema = new { type = "object", properties = new { x = new { type = "integer" }, y = new { type = "integer" } }, required = new[] { "x", "y" } } },
                new { name = "window_list", description = "List all windows", inputSchema = new { type = "object", properties = new { visibleOnly = new { type = "boolean" }, titleFilter = new { type = "string" } } } },
                new { name = "window_find", description = "Find a window", inputSchema = new { type = "object", properties = new { title = new { type = "string" }, className = new { type = "string" }, processId = new { type = "integer" } } } },
                new { name = "window_activate", description = "Activate a window", inputSchema = new { type = "object", properties = new { handle = new { type = "integer" } }, required = new[] { "handle" } } },
                new { name = "system_info", description = "Get system information", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } },
                new { name = "process_list", description = "List running processes", inputSchema = new { type = "object", properties = new { nameFilter = new { type = "string" } } } },
                new { name = "clipboard_get_text", description = "Get text from clipboard", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } },
                new { name = "clipboard_set_text", description = "Set text to clipboard", inputSchema = new { type = "object", properties = new { text = new { type = "string" } }, required = new[] { "text" } } },
                new { name = "volume_get", description = "Get current volume level", inputSchema = new { type = "object", properties = new Dictionary<string, object>() } },
                new { name = "volume_set", description = "Set volume level (0-100)", inputSchema = new { type = "object", properties = new { level = new { type = "integer", minimum = 0, maximum = 100 } }, required = new[] { "level" } } }
            }
        };
    }

    private static async Task<object> CallToolAsync(Dictionary<string, JsonElement> args)
    {
        if (!args.TryGetValue("name", out var nameElement))
            throw new ArgumentException("Tool name is required");
        
        var toolName = nameElement.GetString()?.ToLowerInvariant() ?? "";
        var arguments = args.TryGetValue("arguments", out var argsElement) 
            ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argsElement.GetRawText()) ?? new Dictionary<string, JsonElement>()
            : new Dictionary<string, JsonElement>();

        Response result = toolName switch
        {
            "mouse_move" => MouseModule.Move(ParseArgs<MouseMoveRequest>(arguments)),
            "mouse_click" => MouseModule.Click(ParseArgs<MouseClickRequest>(arguments)),
            "mouse_position" => MouseModule.GetPosition(),
            "key_press" => KeyboardModule.Press(ParseArgs<KeyPressRequest>(arguments)),
            "type_text" => KeyboardModule.TypeText(ParseArgs<TypeTextRequest>(arguments)),
            "screenshot" => ScreenModule.Screenshot(ParseArgs<ScreenshotRequest>(arguments)),
            "pixel_color" => ScreenModule.GetPixelColor(ParseArgs<PixelColorRequest>(arguments)),
            "window_list" => WindowModule.List(ParseArgs<WindowListRequest>(arguments)),
            "window_find" => WindowModule.Find(ParseArgs<WindowFindRequest>(arguments)),
            "window_activate" => WindowModule.Activate(ParseArgs<WindowActivateRequest>(arguments)),
            "system_info" => GetSystemInfo(),
            "process_list" => SystemModule.ListProcesses(ParseArgs<ProcessListRequest>(arguments)),
            "clipboard_get_text" => ClipboardModule.GetText(new ClipboardGetTextRequest()),
            "clipboard_set_text" => ClipboardModule.SetText(ParseArgs<ClipboardSetTextRequest>(arguments)),
            "volume_get" => AudioModule.GetVolume(new VolumeGetRequest()),
            "volume_set" => AudioModule.SetVolume(ParseArgs<VolumeSetRequest>(arguments)),
            _ => new ErrorResponse($"Unknown tool: {toolName}")
        };

        return ToToolResult(result);
    }

    private static Response GetSystemInfo()
    {
        var results = new Dictionary<string, Response>
        {
            ["os"] = SystemModule.GetOsInfo(),
            ["cpu"] = SystemModule.GetCpuInfo(),
            ["memory"] = SystemModule.GetMemoryInfo()
        };
        return new SuccessResponse<object>(results);
    }

    private static T ParseArgs<T>(Dictionary<string, JsonElement> args) where T : class, new()
    {
        var json = JsonSerializer.Serialize(args, JsonContext.Default.Options);
        return JsonSerializer.Deserialize(json, typeof(T), JsonContext.Default) as T ?? new T();
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

        var json = JsonSerializer.Serialize(response, typeof(Response), JsonContext.Default);
        return new
        {
            content = new[] { new { type = "text", text = json } },
            isError = false
        };
    }
}

public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("method")]
    public string? Method { get; set; }
    
    [JsonPropertyName("params")]
    public Dictionary<string, JsonElement>? Params { get; set; }
}

public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("result")]
    public object? Result { get; set; }
    
    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}

public class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
