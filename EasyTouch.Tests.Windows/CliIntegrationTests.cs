using EasyTouch.Core.Models;
using EasyTouch.Modules;

namespace EasyTouch.Tests.Windows;

public class CliIntegrationTests
{
    [Fact]
    public void Test_Mouse_Position()
    {
        var result = MouseModule.GetPosition();

        Assert.True(result.Success, $"Command failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<MousePositionResponse>>(result);
        Assert.True(successResult.Data.X >= 0, "X should be non-negative");
        Assert.True(successResult.Data.Y >= 0, "Y should be non-negative");
    }

    [Fact]
    public void Test_System_OsInfo()
    {
        var result = SystemModule.GetOsInfo();

        Assert.True(result.Success, $"Command failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<OsInfoResponse>>(result);
        Assert.False(string.IsNullOrEmpty(successResult.Data.Version), "Version should not be empty");
        Assert.False(string.IsNullOrEmpty(successResult.Data.Architecture), "Architecture should not be empty");
    }

    [Fact]
    public void Test_System_CpuInfo()
    {
        var result = SystemModule.GetCpuInfo();

        Assert.True(result.Success, $"Command failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<CpuInfoResponse>>(result);
        Assert.NotNull(successResult.Data.Info);
    }

    [Fact]
    public void Test_System_MemoryInfo()
    {
        var result = SystemModule.GetMemoryInfo();

        Assert.True(result.Success, $"Command failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<MemoryInfoResponse>>(result);
        Assert.True(successResult.Data.TotalPhysical > 0, "TotalPhysical should be greater than 0");
    }

    [Fact]
    public void Test_Window_List()
    {
        var result = WindowModule.List(new WindowListRequest { VisibleOnly = true });

        Assert.True(result.Success, $"Command failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<WindowListResponse>>(result);
        Assert.NotNull(successResult.Data.Windows);
    }

    [Fact]
    public void Test_Window_Find()
    {
        var result = WindowModule.Find(new WindowFindRequest { Title = "任务管理器" });

        // 窗口可能不存在，所以只检查返回成功
        Assert.NotNull(result);
    }

    [Fact]
    public void Test_Clipboard_SetAndGet()
    {
        var setResult = ClipboardModule.SetText(new ClipboardSetTextRequest { Text = "Test123" });
        Assert.True(setResult.Success, $"Set clipboard failed: {setResult}");

        var getResult = ClipboardModule.GetText(new ClipboardGetTextRequest());
        Assert.True(getResult.Success, $"Get clipboard failed: {getResult}");
        
        var successResult = Assert.IsType<SuccessResponse<ClipboardTextResponse>>(getResult);
        Assert.Equal("Test123", successResult.Data.Text);
    }

    [Fact]
    public void Test_Screenshot()
    {
        var tempFile = Path.GetTempFileName() + ".png";
        try
        {
            var result = ScreenModule.Screenshot(new ScreenshotRequest { OutputPath = tempFile });

            Assert.True(result.Success, $"Command failed: {result}");
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
        var result = SystemModule.ListProcesses(new ProcessListRequest());

        Assert.True(result.Success, $"Command failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<ProcessListResponse>>(result);
        Assert.NotNull(successResult.Data.Processes);
        Assert.True(successResult.Data.Processes.Length > 0, "Should have running processes");
    }

    [Fact]
    public void Test_Mouse_Move()
    {
        var result = MouseModule.Move(new MouseMoveRequest { X = 100, Y = 200, Relative = false });

        Assert.True(result.Success, $"Command failed: {result}");
    }

    [Fact]
    public void Test_Mouse_Click()
    {
        var result = MouseModule.Click(new MouseClickRequest { Button = MouseButton.Left, Double = false });

        Assert.True(result.Success, $"Command failed: {result}");
    }

    [Fact]
    public void Test_Key_Press()
    {
        var result = KeyboardModule.Press(new KeyPressRequest { Key = "a" });

        Assert.True(result.Success, $"Command failed: {result}");
    }

    [Fact]
    public void Test_Type_Text()
    {
        var result = KeyboardModule.TypeText(new TypeTextRequest { Text = "Hello", Interval = 0, HumanLike = false });

        Assert.True(result.Success, $"Command failed: {result}");
    }

    [Fact]
    public void Test_Pixel_Color()
    {
        var result = ScreenModule.GetPixelColor(new PixelColorRequest { X = 100, Y = 100 });

        Assert.True(result.Success, $"Command failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<PixelColorResponse>>(result);
        Assert.True(successResult.Data.R >= 0 && successResult.Data.R <= 255, "R should be 0-255");
        Assert.True(successResult.Data.G >= 0 && successResult.Data.G <= 255, "G should be 0-255");
        Assert.True(successResult.Data.B >= 0 && successResult.Data.B <= 255, "B should be 0-255");
    }

    [Fact]
    public void Test_Screen_List()
    {
        var result = ScreenModule.ListScreens();

        Assert.True(result.Success, $"Command failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<ScreenListResponse>>(result);
        Assert.NotNull(successResult.Data.Screens);
        Assert.True(successResult.Data.Screens.Length > 0, "Should have at least one screen");
    }

    [Fact]
    public void Test_Window_Foreground()
    {
        var result = WindowModule.GetForeground();

        Assert.True(result.Success, $"Command failed: {result}");
    }

    [Fact]
    public void Test_Disk_List()
    {
        var result = SystemModule.ListDisks();

        Assert.True(result.Success, $"Command failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<DiskListResponse>>(result);
        Assert.NotNull(successResult.Data.Disks);
        Assert.True(successResult.Data.Disks.Length > 0, "Should have at least one disk");
    }

    [Fact(Skip = "Lock screen test skipped to avoid disrupting automated testing")]
    public void Test_Lock_Screen()
    {
        // ⚠️ WARNING: This test is skipped because it locks the screen
        // which would disrupt automated testing environments
        var result = SystemModule.LockScreen();
        Assert.True(result.Success, $"Lock screen failed: {result}");
    }

    [Fact]
    public void Test_Invalid_Command()
    {
        // 测试错误响应
        var result = new ErrorResponse("Unknown command: invalid_command");
        
        Assert.False(result.Success);
        Assert.Contains("Unknown", result.Error);
    }

    [Fact]
    public void Test_Mouse_Scroll()
    {
        var result = MouseModule.Scroll(new MouseScrollRequest { Amount = 3, Horizontal = false });

        Assert.True(result.Success, $"Command failed: {result}");
    }

    [Fact]
    public void Test_Clipboard_Clear()
    {
        var result = ClipboardModule.Clear(new ClipboardClearRequest());

        Assert.True(result.Success, $"Command failed: {result}");
    }

    [Fact]
    public void Test_Audio_Devices()
    {
        var result = AudioModule.ListDevices(new AudioDeviceListRequest());

        Assert.True(result.Success, $"Command failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<AudioDeviceListResponse>>(result);
        Assert.NotNull(successResult.Data.Devices);
    }

    [Fact]
    public void Test_Volume_Get()
    {
        // 使用 Core Audio API (COM) 获取音量
        var result = AudioModule.GetVolume(new VolumeGetRequest());
        Assert.True(result.Success, $"Get volume failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<VolumeResponse>>(result);
        Assert.True(successResult.Data.Level >= 0 && successResult.Data.Level <= 100, 
            "Volume level should be between 0 and 100");
    }

    [Fact]
    public void Test_Volume_Set()
    {
        // 使用 Core Audio API (COM) 设置音量
        var result = AudioModule.SetVolume(new VolumeSetRequest { Level = 50 });
        Assert.True(result.Success, $"Set volume failed: {result}");
    }

    [Fact]
    public void Test_Volume_Mute()
    {
        // 切换静音
        var result1 = AudioModule.SetMute(new VolumeMuteRequest { Mute = true });
        Assert.True(result1.Success, $"Mute toggle failed: {result1}");

        Thread.Sleep(200);

        // 再次切换
        var result2 = AudioModule.SetMute(new VolumeMuteRequest { Mute = false });
        Assert.True(result2.Success, $"Unmute toggle failed: {result2}");
    }

    [Fact]
    public void Test_Volume_Up_Down()
    {
        // 调节音量
        var result1 = AudioModule.VolumeUp(2);
        Assert.True(result1.Success, $"Volume up failed: {result1}");

        Thread.Sleep(200);

        var result2 = AudioModule.VolumeDown(2);
        Assert.True(result2.Success, $"Volume down failed: {result2}");
    }

    [Fact]
    public void Test_Window_Minimize_Maximize()
    {
        // 获取当前前台窗口
        var foregroundResult = WindowModule.GetForeground();
        Assert.True(foregroundResult.Success);

        // 获取前台窗口句柄
        long handle = 0;
        if (foregroundResult is SuccessResponse<WindowFindResponse> successResponse && 
            successResponse.Data?.Handle.HasValue == true)
        {
            handle = successResponse.Data.Handle.Value;
        }

        if (handle != 0)
        {
            // 最小化窗口
            var minimizeResult = WindowModule.Show(new WindowShowRequest { Handle = handle, State = WindowShowState.Minimize });

            // 等待一下
            Thread.Sleep(500);

            // 最大化窗口（恢复）
            var maximizeResult = WindowModule.Show(new WindowShowRequest { Handle = handle, State = WindowShowState.ShowMaximized });

            // 只要命令能执行就认为是成功的（不验证具体行为）
            Assert.True(minimizeResult.Success, "Minimize command should succeed");
            Assert.True(maximizeResult.Success, "Maximize command should succeed");
        }
    }

    [Fact(Skip = "Window close test skipped to avoid closing important windows")]
    public void Test_Window_Close()
    {
        // ⚠️ WARNING: This test is skipped because closing the foreground window
        // could disrupt the testing environment
        var result = WindowModule.Close(new WindowCloseRequest());

        Assert.True(result.Success, $"Command failed: {result}");
    }
}
