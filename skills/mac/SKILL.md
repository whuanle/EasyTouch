# EasyTouch macOS MCP Skill

## 基本信息

- **名称**: EasyTouch macOS Automation
- **版本**: 1.0.0
- **描述**: macOS 系统自动化控制工具，支持鼠标、键盘、屏幕、窗口、系统资源等操作
- **作者**: MaomiAgent Team
- **许可证**: MIT

## 支持平台

- macOS 11 (Big Sur) +
- macOS 12 (Monterey)
- macOS 13 (Ventura)
- macOS 14 (Sonoma)
- macOS 15 (Sequoia)
- Intel 和 Apple Silicon (Rosetta 2)

## 依赖要求

本工具使用 AppleScript 和系统命令，无需额外依赖。

**系统要求**:
- macOS 11 或更高版本
- 辅助功能权限（首次使用时系统会提示）

## 功能特性

### 1. 鼠标控制
- 移动鼠标到指定坐标
- 鼠标点击（左/右/中键，支持双击）
- 鼠标按下/释放
- 鼠标滚轮滚动
- 获取当前鼠标位置

### 2. 键盘控制
- 按键按下和释放
- 组合键操作（⌘+C, ⌥+Tab 等）
- 文本输入（支持多语言）
- 模拟人类打字

### 3. 屏幕操作
- 全屏/区域截图
- 获取指定像素颜色
- 列出所有显示器

### 4. 窗口管理
- 列出所有窗口
- 按标题查找窗口
- 激活窗口
- 获取前台窗口

### 5. 系统信息
- 获取操作系统信息
- CPU 信息和使用率
- 内存使用情况
- 磁盘信息
- 进程列表

### 6. 剪贴板操作
- 获取/设置剪贴板文本
- 获取剪贴板文件列表
- 清空剪贴板

### 7. 音频控制
- 获取/设置系统音量
- 静音/取消静音
- 列出音频设备

## 使用方式

### CLI 命令行模式

```bash
# 鼠标操作
et mouse_move --x 100 --y 200
et mouse_click --button left --double
et mouse_position

# 键盘操作
et key_press --key "command+c"
et type_text --text "Hello World"

# 截图
et screenshot --output screenshot.png
et pixel_color --x 100 --y 100

# 窗口管理
et window_list
et window_activate --title "Safari"

# 系统信息
et os_info
et cpu_info
et memory_info
et process_list

# 剪贴板
et clipboard_get_text
et clipboard_set_text --text "Hello"

# 音频
et volume_set --level 50
et volume_mute --state true
```

### MCP 模式

```bash
et --mcp
```

## MCP Tools

| Tool | 描述 | 参数 |
|------|------|------|
| `mouse_move` | 移动鼠标 | `x`, `y`, `relative`, `duration` |
| `mouse_click` | 点击鼠标 | `button`, `double` |
| `mouse_down` | 鼠标按下 | `button` |
| `mouse_up` | 鼠标释放 | `button` |
| `mouse_scroll` | 鼠标滚轮 | `amount`, `horizontal` |
| `mouse_position` | 获取鼠标位置 | - |
| `key_press` | 按下按键 | `key` |
| `key_down` | 按键按下 | `key` |
| `key_up` | 按键释放 | `key` |
| `type_text` | 输入文本 | `text`, `interval`, `humanLike` |
| `screenshot` | 截图 | `x`, `y`, `width`, `height`, `outputPath` |
| `pixel_color` | 获取像素颜色 | `x`, `y` |
| `screen_list` | 列出显示器 | - |
| `window_list` | 列出窗口 | `visibleOnly`, `titleFilter` |
| `window_find` | 查找窗口 | `title`, `processId` |
| `window_activate` | 激活窗口 | `handle` |
| `window_foreground` | 获取前台窗口 | - |
| `os_info` | 操作系统信息 | - |
| `cpu_info` | CPU 信息 | - |
| `memory_info` | 内存信息 | - |
| `disk_list` | 磁盘列表 | - |
| `process_list` | 进程列表 | `nameFilter` |
| `lock_screen` | 锁定屏幕 | - |
| `clipboard_get_text` | 获取剪贴板文本 | - |
| `clipboard_set_text` | 设置剪贴板文本 | `text` |
| `clipboard_clear` | 清空剪贴板 | - |
| `clipboard_get_files` | 获取剪贴板文件 | - |
| `volume_get` | 获取音量 | - |
| `volume_set` | 设置音量 | `level` |
| `volume_mute` | 静音/取消静音 | `state` |
| `audio_devices` | 列出音频设备 | - |

## 技术规格

- **目标框架**: .NET 10
- **编译方式**: AOT (Ahead-of-Time)
- **输出文件**: `et` (单文件，自包含)
- **文件大小**: ~4 MB
- **运行平台**: macOS x64 / arm64

## 安装方法

### 方式一：直接下载

根据你的 Mac 芯片架构选择正确的版本：

**Apple Silicon (M1/M2/M3/M4)：**
```bash
sudo cp et-arm64 /usr/local/bin/et
sudo chmod +x /usr/local/bin/et
```

**Intel Mac：**
```bash
sudo cp et-x64 /usr/local/bin/et
sudo chmod +x /usr/local/bin/et
```

**如何检测芯片架构：**
```bash
uname -m
# arm64 = Apple Silicon
# x86_64 = Intel
```

### 方式二：Homebrew 安装（推荐）

```bash
# 添加 tap（后续发布）
brew tap maomiaent/easytouch
brew install easytouch
```

### 方式三：从源码编译

```bash
# 克隆仓库
cd tools/EasyTouch/EasyTouch-Mac

# Intel Mac (x64)
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishAot=true
# 输出: bin/Release/net10.0/osx-x64/publish/et

# Apple Silicon (arm64)
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishAot=true
# 输出: bin/Release/net10.0/osx-arm64/publish/et

# 构建通用二进制（可选，需要两个架构的二进制）
lipo -create bin/Release/net10.0/osx-x64/publish/et \
             bin/Release/net10.0/osx-arm64/publish/et \
             -output et-universal
```

## 集成到 MCP 客户端

### Claude Desktop 配置

在 `claude_desktop_config.json` 中添加：

```json
{
  "mcpServers": {
    "easytouch": {
      "command": "/usr/local/bin/et",
      "args": ["--mcp"]
    }
  }
}
```

### 其他 MCP 客户端

配置命令为 `et`，参数为 `--mcp`，使用 stdio 传输。

## 权限设置

### 辅助功能权限

首次使用鼠标/键盘控制功能时，系统会提示授予辅助功能权限：

1. 打开 **系统设置** → **隐私与安全性** → **辅助功能**
2. 点击 **+** 按钮
3. 选择 **终端** 或你使用的应用
4. 开启权限开关

### 屏幕录制权限

如果使用截图功能，需要授予屏幕录制权限：

1. 打开 **系统设置** → **隐私与安全性** → **屏幕录制**
2. 添加 **终端** 或你使用的应用

## 注意事项

1. **辅助功能权限**: 鼠标和键盘控制需要辅助功能权限，请在系统设置中授权
2. **安全性**: macOS 的安全机制可能会阻止某些操作，请确保在受信任的环境中使用
3. **芯片架构**: 
   - Apple Silicon (M1/M2/M3/M4) 用户请使用 `et-arm64` 版本以获得最佳性能
   - Intel Mac 用户请使用 `et-x64` 版本
   - arm64 版本也可在 Apple Silicon 上通过 Rosetta 2 运行 x64 版本，但性能会有损失
4. **沙盒**: 在沙盒环境中运行时部分功能可能受限

### 芯片架构兼容性

| 你的 Mac | 推荐版本 | 兼容版本 |
|---------|---------|---------|
| M4, M3, M2, M1 | et-arm64 | et-arm64, et-x64 (Rosetta 2) |
| Intel Core i/i5/i7/i9 | et-x64 | et-x64 |

**检测架构：**
```bash
# 终端运行
uname -m
# 输出 arm64 = Apple Silicon
# 输出 x86_64 = Intel
```

## 故障排除

### 命令无响应
- 检查辅助功能权限：系统设置 → 隐私与安全性 → 辅助功能
- 重启终端应用以应用权限更改

### 截图失败
- 检查屏幕录制权限：系统设置 → 隐私与安全性 → 屏幕录制
- 确保输出目录可写

### 权限错误
```bash
# 查看详细错误信息
et mouse_position 2>&1

# 重置权限尝试
sudo tccutil reset All
```

## 与其他自动化工具对比

| 功能 | EasyTouch | AppleScript | Automator | Shortcuts |
|------|-----------|-------------|-----------|-----------|
| CLI 支持 | ✅ | ⚠️ | ❌ | ❌ |
| MCP 集成 | ✅ | ❌ | ❌ | ❌ |
| 跨平台 | ✅ (W/L/M) | ❌ | ❌ | ❌ |
| 学习曲线 | 低 | 高 | 中 | 低 |
| 性能 | 高 | 中 | 中 | 中 |
