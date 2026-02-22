using System.Text.Json;
using System.Text.Json.Serialization;
using EasyTouch.Core.Models;
using EasyTouch.Modules;

namespace EasyTouch.Cli;

public static class CliHost
{
    public static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return 1;
        }

        try
        {
            var command = args[0].ToLowerInvariant();
            var subArgs = args.Skip(1).ToArray();

            Response result;
            
            result = command switch
            {
                "help" or "--help" or "-h" => PrintHelp(),
                _ => TryHandleDirectCommand(command, subArgs)
            };

            if (result is ErrorResponse error)
            {
                PrintResult(error);
                return 1;
            }

            PrintResult(result);
            return 0;
        }
        catch (Exception ex)
        {
            PrintResult(new ErrorResponse(ex.Message));
            return 1;
        }
    }

    private static Response TryHandleDirectCommand(string command, string[] args)
    {
        try
        {
            return HandleDirectCommand(command, args);
        }
        catch
        {
            return new ErrorResponse($"Unknown command: {command}");
        }
    }

    private static Response HandleDirectCommand(string command, string[] args)
    {
        var options = ParseOptions(args);

        return command switch
        {
            // Mouse commands
            "mouse_move" => MouseModule.Move(new MouseMoveRequest
            {
                X = GetIntOption(options, "x", 0),
                Y = GetIntOption(options, "y", 0),
                Relative = GetBoolOption(options, "relative", false),
                Duration = GetIntOption(options, "duration", 0)
            }),
            "mouse_click" => MouseModule.Click(new MouseClickRequest
            {
                Button = ParseMouseButton(GetStringOption(options, "button", "left")),
                Double = GetBoolOption(options, "double", false)
            }),
            "mouse_down" => MouseModule.Down(ParseMouseButton(GetStringOption(options, "button", "left"))),
            "mouse_up" => MouseModule.Up(ParseMouseButton(GetStringOption(options, "button", "left"))),
            "mouse_scroll" => MouseModule.Scroll(new MouseScrollRequest
            {
                Amount = GetIntOption(options, "amount", 0),
                Horizontal = GetBoolOption(options, "horizontal", false)
            }),
            "mouse_position" => new SuccessResponse<MousePositionResponse>(MouseModule.GetPosition()),
            
            // Keyboard commands
            "key_press" => KeyboardModule.Press(new KeyPressRequest
            {
                Key = GetStringOption(options, "key", "")!
            }),
            "key_down" => KeyboardModule.Down(GetStringOption(options, "key", "")!),
            "key_up" => KeyboardModule.Up(GetStringOption(options, "key", "")!),
            "type_text" => KeyboardModule.TypeText(new TypeTextRequest
            {
                Text = GetStringOption(options, "text", "")!,
                Interval = GetIntOption(options, "interval", 0),
                HumanLike = GetBoolOption(options, "human", false)
            }),
            
            // Screen commands
            "screenshot" => ScreenModule.Screenshot(new ScreenshotRequest
            {
                OutputPath = GetStringOption(options, "output", null),
                X = GetNullableIntOption(options, "x"),
                Y = GetNullableIntOption(options, "y"),
                Width = GetNullableIntOption(options, "width"),
                Height = GetNullableIntOption(options, "height")
            }),
            "pixel_color" => ScreenModule.GetPixelColor(new PixelColorRequest
            {
                X = GetIntOption(options, "x", 0),
                Y = GetIntOption(options, "y", 0)
            }),
            "screen_list" => ScreenModule.ListScreens(),
            
            // Window commands
            "window_list" => WindowModule.List(new WindowListRequest
            {
                VisibleOnly = GetBoolOption(options, "visible-only", true),
                TitleFilter = GetStringOption(options, "filter", null)
            }),
            "window_find" => WindowModule.Find(new WindowFindRequest
            {
                Title = GetStringOption(options, "title", null),
                ClassName = GetStringOption(options, "class", null),
                ProcessId = GetNullableUintOption(options, "pid")
            }),
            "window_activate" => ActivateWindowByTitleOrHandle(options),
            "window_foreground" => WindowModule.GetForeground(),
            
            // System commands
            "os_info" => SystemModule.GetOsInfo(),
            "cpu_info" => SystemModule.GetCpuInfo(),
            "memory_info" => SystemModule.GetMemoryInfo(),
            "disk_list" => SystemModule.ListDisks(),
            "process_list" => SystemModule.ListProcesses(new ProcessListRequest
            {
                NameFilter = GetStringOption(options, "filter", null)
            }),
            "lock_screen" => SystemModule.LockScreen(),
            
            // Clipboard commands
            "clipboard_get_text" => ClipboardModule.GetText(new ClipboardGetTextRequest()),
            "clipboard_set_text" => ClipboardModule.SetText(new ClipboardSetTextRequest
            {
                Text = GetStringOption(options, "text", "")!
            }),
            "clipboard_clear" => ClipboardModule.Clear(new ClipboardClearRequest()),
            "clipboard_get_files" => ClipboardModule.GetFiles(new ClipboardGetFilesRequest()),
            
            // Audio commands
            "volume_get" => AudioModule.GetVolume(new VolumeGetRequest()),
            "volume_set" => AudioModule.SetVolume(new VolumeSetRequest
            {
                Level = GetIntOption(options, "level", 50)
            }),
            "volume_mute" => AudioModule.SetMute(new VolumeMuteRequest
            {
                Mute = GetBoolOption(options, "state", true)
            }),
            "audio_devices" => AudioModule.ListDevices(new AudioDeviceListRequest()),
            
            _ => new ErrorResponse($"Unknown command: {command}")
        };
    }

    private static Response ActivateWindowByTitleOrHandle(Dictionary<string, string> options)
    {
        var title = GetStringOption(options, "title", null);
        var handle = GetLongOption(options, "handle", 0);

        if (!string.IsNullOrEmpty(title))
        {
            var findResult = WindowModule.Find(new WindowFindRequest { Title = title });
            if (findResult is SuccessResponse<WindowFindResponse> success && success.Data?.Handle != null)
            {
                handle = success.Data.Handle.Value;
            }
            else
            {
                return new ErrorResponse($"Window with title '{title}' not found");
            }
        }

        if (handle == 0)
        {
            return new ErrorResponse("Either --title or --handle must be specified");
        }

        return WindowModule.Activate(new WindowActivateRequest { Handle = handle });
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--") && i + 1 < args.Length)
            {
                var key = args[i][2..];
                var value = args[i + 1];
                if (!value.StartsWith("--"))
                {
                    options[key] = value;
                    i++;
                }
                else
                {
                    options[key] = "true";
                }
            }
            else if (args[i].StartsWith("--"))
            {
                options[args[i][2..]] = "true";
            }
        }
        return options;
    }

    private static string? GetStringOption(Dictionary<string, string> options, string key, string? defaultValue)
    {
        return options.TryGetValue(key, out var value) ? value : defaultValue;
    }

    private static int GetIntOption(Dictionary<string, string> options, string key, int defaultValue)
    {
        return options.TryGetValue(key, out var value) && int.TryParse(value, out var result) ? result : defaultValue;
    }

    private static int? GetNullableIntOption(Dictionary<string, string> options, string key)
    {
        return options.TryGetValue(key, out var value) && int.TryParse(value, out var result) ? result : null;
    }

    private static uint? GetNullableUintOption(Dictionary<string, string> options, string key)
    {
        return options.TryGetValue(key, out var value) && uint.TryParse(value, out var result) ? result : null;
    }

    private static long GetLongOption(Dictionary<string, string> options, string key, long defaultValue)
    {
        return options.TryGetValue(key, out var value) && long.TryParse(value, out var result) ? result : defaultValue;
    }

    private static bool GetBoolOption(Dictionary<string, string> options, string key, bool defaultValue)
    {
        if (options.TryGetValue(key, out var value))
        {
            if (bool.TryParse(value, out var result))
                return result;
            return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        return defaultValue;
    }

    private static MouseButton ParseMouseButton(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "left" => MouseButton.Left,
            "right" => MouseButton.Right,
            "middle" => MouseButton.Middle,
            _ => MouseButton.Left
        };
    }

    private static Response PrintHelp()
    {
        Console.WriteLine("EasyTouch Linux Automation Tool");
        Console.WriteLine();
        Console.WriteLine("Usage: et <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  mouse_move --x <n> --y <n> [--relative] [--duration <ms>]");
        Console.WriteLine("  mouse_click [--button left|right|middle] [--double]");
        Console.WriteLine("  mouse_position");
        Console.WriteLine("  key_press --key <key>");
        Console.WriteLine("  type_text --text <text> [--interval <ms>] [--human]");
        Console.WriteLine("  screenshot [--output <path>] [--x <n>] [--y <n>] [--width <n>] [--height <n>]");
        Console.WriteLine("  pixel_color --x <n> --y <n>");
        Console.WriteLine("  window_list [--visible-only] [--filter <text>]");
        Console.WriteLine("  window_find [--title <text>] [--class <name>] [--pid <n>]");
        Console.WriteLine("  window_activate --title <text> | --handle <n>");
        Console.WriteLine("  window_foreground");
        Console.WriteLine("  os_info, cpu_info, memory_info, disk_list");
        Console.WriteLine("  process_list [--filter <text>]");
        Console.WriteLine("  clipboard_get_text, clipboard_set_text --text <text>");
        Console.WriteLine("  volume_get, volume_set --level <0-100>");
        Console.WriteLine();
        Console.WriteLine("  help       Show this help");
        
        return new SuccessResponse();
    }

    private static void PrintResult(Response result)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        Console.WriteLine(JsonSerializer.Serialize(result, result.GetType(), options));
    }
}
