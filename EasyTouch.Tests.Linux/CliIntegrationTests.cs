using System.Diagnostics;
using System.Text.Json;

namespace EasyTouch.Tests.Linux;

public class CliIntegrationTests
{
    private readonly string _etPath;

    public CliIntegrationTests()
    {
        // Linux 平台可执行文件路径
        _etPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..",
            "EasyTouch-Linux", "bin", "Release", "net10.0", "linux-x64", "publish", "et"
        );
        _etPath = Path.GetFullPath(_etPath);
    }

    private (int ExitCode, string Output, string Error) RunCommand(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _etPath,
            Arguments = string.Join(" ", args),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException("Failed to start process");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, output, error);
    }

    private bool IsSuccess(string output)
    {
        try
        {
            var doc = JsonDocument.Parse(output);
            if (doc.RootElement.TryGetProperty("Success", out var success))
            {
                return success.GetBoolean();
            }
        }
        catch { }
        return false;
    }

    [Fact]
    public void Test_Mouse_Position()
    {
        var (exitCode, output, error) = RunCommand("mouse_position");
        
        Assert.Equal(0, exitCode);
        Assert.True(IsSuccess(output), $"Command failed: {output}");
        Assert.Contains("X", output);
        Assert.Contains("Y", output);
    }

    [Fact]
    public void Test_System_OsInfo()
    {
        var (exitCode, output, error) = RunCommand("os_info");
        
        Assert.Equal(0, exitCode);
        Assert.True(IsSuccess(output), $"Command failed: {output}");
        Assert.Contains("Version", output);
        Assert.Contains("Architecture", output);
    }

    [Fact]
    public void Test_System_CpuInfo()
    {
        var (exitCode, output, error) = RunCommand("cpu_info");
        
        Assert.Equal(0, exitCode);
        Assert.True(IsSuccess(output), $"Command failed: {output}");
    }

    [Fact]
    public void Test_System_MemoryInfo()
    {
        var (exitCode, output, error) = RunCommand("memory_info");
        
        Assert.Equal(0, exitCode);
        Assert.True(IsSuccess(output), $"Command failed: {output}");
    }

    [Fact]
    public void Test_Clipboard_SetAndGet()
    {
        var (exitCode1, output1, _) = RunCommand("clipboard_set_text", "--text", "Test123");
        Assert.Equal(0, exitCode1);
        Assert.True(IsSuccess(output1), $"Set clipboard failed: {output1}");

        var (exitCode2, output2, _) = RunCommand("clipboard_get_text");
        Assert.Equal(0, exitCode2);
        Assert.True(IsSuccess(output2), $"Get clipboard failed: {output2}");
        Assert.Contains("Test123", output2);
    }

    [Fact]
    public void Test_Screenshot()
    {
        var tempFile = Path.GetTempFileName() + ".png";
        try
        {
            var (exitCode, output, error) = RunCommand("screenshot", "--output", tempFile);
            
            Assert.Equal(0, exitCode);
            Assert.True(IsSuccess(output), $"Command failed: {output}");
            Assert.True(File.Exists(tempFile), "Screenshot file was not created");
            Assert.True(new FileInfo(tempFile).Length > 0, "Screenshot file is empty");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Test_Process_List()
    {
        var (exitCode, output, error) = RunCommand("process_list");
        
        Assert.Equal(0, exitCode);
        Assert.True(IsSuccess(output), $"Command failed: {output}");
        Assert.Contains("Processes", output);
    }

    [Fact]
    public void Test_Mouse_Move()
    {
        var (exitCode, output, error) = RunCommand("mouse_move", "--x", "100", "--y", "200");
        
        Assert.Equal(0, exitCode);
        Assert.True(IsSuccess(output), $"Command failed: {output}");
    }

    [Fact]
    public void Test_Mouse_Click()
    {
        var (exitCode, output, error) = RunCommand("mouse_click");
        
        Assert.Equal(0, exitCode);
        Assert.True(IsSuccess(output), $"Command failed: {output}");
    }

    [Fact]
    public void Test_Key_Press()
    {
        var (exitCode, output, error) = RunCommand("key_press", "--key", "a");
        
        Assert.Equal(0, exitCode);
        Assert.True(IsSuccess(output), $"Command failed: {output}");
    }

    [Fact]
    public void Test_Type_Text()
    {
        var (exitCode, output, error) = RunCommand("type_text", "--text", "Hello");
        
        Assert.Equal(0, exitCode);
        Assert.True(IsSuccess(output), $"Command failed: {output}");
    }

    [Fact]
    public void Test_Pixel_Color()
    {
        var (exitCode, output, error) = RunCommand("pixel_color", "--x", "100", "--y", "100");
        
        Assert.Equal(0, exitCode);
        Assert.True(IsSuccess(output), $"Command failed: {output}");
        Assert.Contains("R", output);
        Assert.Contains("G", output);
        Assert.Contains("B", output);
    }

    [Fact]
    public void Test_Screen_List()
    {
        var (exitCode, output, error) = RunCommand("screen_list");
        
        Assert.Equal(0, exitCode);
        Assert.True(IsSuccess(output), $"Command failed: {output}");
        Assert.Contains("Screens", output);
    }

    [Fact]
    public void Test_Disk_List()
    {
        var (exitCode, output, error) = RunCommand("disk_list");
        
        Assert.Equal(0, exitCode);
        Assert.True(IsSuccess(output), $"Command failed: {output}");
        Assert.Contains("Disks", output);
    }

    [Fact]
    public void Test_Lock_Screen()
    {
        var (exitCode, output, error) = RunCommand("lock_screen");
        Assert.Contains("Success", output);
    }

    [Fact]
    public void Test_Invalid_Command()
    {
        var (exitCode, output, error) = RunCommand("invalid_command");
        
        Assert.NotEqual(0, exitCode);
        Assert.Contains("Unknown", output);
    }
}
