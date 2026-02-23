# EasyTouch 测试项目结构

## 📊 测试覆盖概览

| 平台 | 测试项目 | 测试数量 | 特有功能测试 |
|------|---------|---------|-------------|
| Windows | EasyTouch.Tests.Windows | 22+ | 窗口管理、音量控制 |
| Linux | EasyTouch.Tests.Linux | 19+ | X11 功能、系统信息 |
| macOS | EasyTouch.Tests.Mac | 20+ | 电池信息、Spotlight |

## 📁 测试项目结构

```
EasyTouch/
├── EasyTouch.Tests.Windows/
│   ├── CliIntegrationTests.cs    (22个测试方法)
│   └── EasyTouch.Tests.Windows.csproj
├── EasyTouch.Tests.Linux/
│   ├── CliIntegrationTests.cs    (19个测试方法)
│   └── EasyTouch.Tests.Linux.csproj
├── EasyTouch.Tests.Mac/
│   ├── CliIntegrationTests.cs    (20个测试方法)
│   └── EasyTouch.Tests.Mac.csproj
└── EasyTouch.Tests/              (旧版，已弃用)
    └── CliIntegrationTests.cs
```

## ✅ 测试方法列表

### 通用测试（所有平台）
- Test_Mouse_Position - 获取鼠标位置
- Test_Mouse_Move - 移动鼠标
- Test_Mouse_Click - 鼠标点击
- Test_Mouse_Scroll - 鼠标滚轮
- Test_Key_Press - 按键
- Test_Type_Text - 输入文本
- Test_System_OsInfo - 操作系统信息
- Test_System_CpuInfo - CPU 信息
- Test_System_MemoryInfo - 内存信息
- Test_Screen_List - 显示器列表
- Test_Pixel_Color - 像素颜色
- Test_Screenshot - 截图功能
- Test_Process_List - 进程列表
- Test_Disk_List - 磁盘列表
- Test_Clipboard_SetAndGet - 剪贴板读写
- Test_Clipboard_Clear - 清空剪贴板
- Test_Lock_Screen - 锁定屏幕
- Test_Invalid_Command - 无效命令处理

### Windows 特有测试
- Test_Window_List - 窗口列表
- Test_Window_Find - 查找窗口
- Test_Window_Foreground - 前台窗口
- Test_Window_Minimize - 最小化窗口
- Test_Window_Maximize - 最大化窗口
- Test_Window_Close - 关闭窗口
- Test_Volume_Get - 获取音量
- Test_Volume_Set - 设置音量
- Test_Volume_Mute - 静音控制
- Test_Audio_Devices - 音频设备列表

### Linux/macOS 特有测试
- Test_System_Uptime - 系统运行时间
- Test_Battery_Info - 电池信息

### macOS 特有测试
- Test_Spotlight_Search - Spotlight 搜索

## 🚀 运行测试

### Windows
```bash
cd scripts
run-tests.bat
```

### Linux/macOS
```bash
cd scripts
chmod +x run-tests.sh
./run-tests.sh
```

### 使用 dotnet CLI
```bash
# Windows
dotnet test EasyTouch.Tests.Windows/EasyTouch.Tests.Windows.csproj

# Linux
dotnet test EasyTouch.Tests.Linux/EasyTouch.Tests.Linux.csproj

# macOS
dotnet test EasyTouch.Tests.Mac/EasyTouch.Tests.Mac.csproj

# 所有平台
dotnet test EasyTouch.sln
```

## 📝 添加新测试

1. 在对应平台的测试项目中创建新的测试类
2. 使用 `[Fact]` 特性标记测试方法
3. 使用 `RunCommand()` 辅助方法调用 CLI 命令
4. 使用 `Assert` 验证结果

示例：
```csharp
[Fact]
public void Test_New_Feature()
{
    var (exitCode, output, error) = RunCommand("new_command", "--param", "value");
    
    Assert.Equal(0, exitCode);
    Assert.True(IsSuccess(output), $"Command failed: {output}");
    Assert.Contains("ExpectedValue", output);
}
```

## 🔧 测试要求

### Windows
- Windows 10/11 x64
- .NET 10 SDK
- 部分测试需要管理员权限

### Linux
- Linux x64 (Ubuntu/Debian/CentOS)
- X11 显示服务器
- .NET 10 SDK
- xclip 或 xsel (剪贴板功能)

### macOS
- macOS 10.15+ (Intel/Apple Silicon)
- .NET 10 SDK
- 辅助功能权限
- 屏幕录制权限 (截图功能)
