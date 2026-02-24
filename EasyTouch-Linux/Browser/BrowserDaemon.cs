using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using EasyTouch.Cli;
using EasyTouch.Core.Models;

namespace EasyTouch.Browser;

public static class BrowserDaemonHost
{
    public const string PipeName = "easytouch-browser-daemon-linux-v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static async Task RunAsync()
    {
        var shouldStop = false;
        while (!shouldStop)
        {
            using var pipe = new NamedPipeServerStream(
                PipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            await pipe.WaitForConnectionAsync();

            using var reader = new StreamReader(pipe, Encoding.UTF8, false, leaveOpen: true);
            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };

            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            DaemonRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<DaemonRequest>(line, JsonOptions);
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(new DaemonResponse
                {
                    Success = false,
                    Error = $"Invalid daemon request: {ex.Message}"
                }, JsonOptions));
                continue;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Command))
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(new DaemonResponse
                {
                    Success = false,
                    Error = "Daemon request command is required"
                }, JsonOptions));
                continue;
            }

            if (request.Command == "__stop__")
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(new DaemonResponse
                {
                    Success = true,
                    ResponseJson = "{\"success\":true,\"data\":{\"message\":\"Browser daemon stopped\"}}"
                }, JsonOptions));
                shouldStop = true;
                continue;
            }

            if (!request.Command.StartsWith("browser_", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(new DaemonResponse
                {
                    Success = false,
                    Error = $"Unsupported daemon command: {request.Command}"
                }, JsonOptions));
                continue;
            }

            try
            {
                var args = request.Args ?? Array.Empty<string>();
                var result = CliHost.ExecuteCommandForDaemon(request.Command.ToLowerInvariant(), args);
                await writer.WriteLineAsync(JsonSerializer.Serialize(new DaemonResponse
                {
                    Success = true,
                    ResponseJson = CliHost.SerializeResponseForOutput(result)
                }, JsonOptions));
            }
            catch (Exception ex)
            {
                var response = new ErrorResponse($"Daemon execution failed: {ex.Message}");
                await writer.WriteLineAsync(JsonSerializer.Serialize(new DaemonResponse
                {
                    Success = true,
                    ResponseJson = CliHost.SerializeResponseForOutput(response)
                }, JsonOptions));
            }
        }
    }
}

public static class BrowserDaemonClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static bool ShouldProxy(string command)
    {
        return command.StartsWith("browser_", StringComparison.OrdinalIgnoreCase) &&
               command != "browser_daemon_status" &&
               command != "browser_daemon_stop";
    }

    public static DaemonExecResult Execute(string command, string[] args)
    {
        if (TrySend(command, args, out var responseJson, out var error))
        {
            return new DaemonExecResult(IsSuccessfulResponse(responseJson), responseJson);
        }

        if (!EnsureDaemonStarted(out var startupError))
        {
            var errorJson = BuildErrorJson(startupError ?? error ?? "Failed to start browser daemon");
            return new DaemonExecResult(false, errorJson);
        }

        if (TrySend(command, args, out responseJson, out error))
        {
            return new DaemonExecResult(IsSuccessfulResponse(responseJson), responseJson);
        }

        return new DaemonExecResult(false, BuildErrorJson(error ?? "Browser daemon is unavailable"));
    }

    public static Response GetStatusResponse()
    {
        var running = TrySend("browser_list", Array.Empty<string>(), out var responseJson, out _);
        return new SuccessResponse<object>(new
        {
            running,
            response = running ? responseJson : null
        });
    }

    public static Response StopDaemonResponse()
    {
        if (!TrySend("__stop__", Array.Empty<string>(), out _, out var error))
        {
            return new ErrorResponse(error ?? "Browser daemon is not running");
        }

        return new SuccessResponse<object>(new { message = "Browser daemon stopped" });
    }

    private static bool EnsureDaemonStarted(out string? error)
    {
        error = null;
        try
        {
            var executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                error = "Unable to resolve current executable path";
                return false;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                // Avoid inheriting caller stdio pipes. Otherwise callers using
                // close-based waits can hang while daemon keeps pipes open.
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("--browser-daemon");

            var daemon = Process.Start(startInfo);
            daemon?.StandardInput.Close();

            for (var i = 0; i < 20; i++)
            {
                Thread.Sleep(150);
                if (TryPing())
                {
                    return true;
                }
            }

            error = "Timed out waiting for browser daemon startup";
            return false;
        }
        catch (Exception ex)
        {
            error = $"Failed to start browser daemon: {ex.Message}";
            return false;
        }
    }

    private static bool TryPing() => TrySend("browser_list", Array.Empty<string>(), out _, out _);

    private static bool TrySend(string command, string[] args, out string responseJson, out string? error)
    {
        responseJson = string.Empty;
        error = null;

        try
        {
            using var pipe = new NamedPipeClientStream(".", BrowserDaemonHost.PipeName, PipeDirection.InOut);
            pipe.Connect(500);

            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(pipe, Encoding.UTF8, false, leaveOpen: true);

            var request = new DaemonRequest
            {
                Command = command,
                Args = args
            };

            writer.WriteLine(JsonSerializer.Serialize(request, JsonOptions));

            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                error = "Browser daemon returned an empty response";
                return false;
            }

            var response = JsonSerializer.Deserialize<DaemonResponse>(line, JsonOptions);
            if (response == null)
            {
                error = "Browser daemon response is invalid";
                return false;
            }

            if (!response.Success)
            {
                error = response.Error ?? "Browser daemon request failed";
                return false;
            }

            responseJson = response.ResponseJson ?? BuildErrorJson("Browser daemon response JSON is missing");
            return true;
        }
        catch (TimeoutException)
        {
            error = "Browser daemon connection timed out";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool IsSuccessfulResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("success", out var successElement) && successElement.GetBoolean();
        }
        catch
        {
            return false;
        }
    }

    private static string BuildErrorJson(string message)
    {
        return JsonSerializer.Serialize(new
        {
            success = false,
            error = message
        }, JsonOptions);
    }
}

public sealed record DaemonExecResult(bool Success, string Json);

internal sealed class DaemonRequest
{
    public string Command { get; set; } = string.Empty;
    public string[]? Args { get; set; }
}

internal sealed class DaemonResponse
{
    public bool Success { get; set; }
    public string? ResponseJson { get; set; }
    public string? Error { get; set; }
}
