# EasyTouch Linux MCP Skill

## 基本信息

- **名称**: EasyTouch Linux Automation
- **版本**: 1.0.0
- **描述**: Linux 系统自动化控制工具，支持鼠标、键盘、屏幕、窗口、系统资源等操作
- **作者**: MaomiAgent Team
- **许可证**: MIT

## 支持平台

- Ubuntu 20.04+
- Debian 10+
- CentOS 8+
- Fedora 32+
- Arch Linux
- 其他支持 X11 或 Wayland 的发行版

## 依赖要求

### X11 环境
- `xdotool` - 鼠标键盘控制
- `xclip` 或 `xsel` - 剪贴板操作
- `scrot` 或 `gnome-screenshot` - 截图
- `wmctrl` - 窗口管理

### Wayland 环境
- `ydotool` - 鼠标键盘控制
- `wl-clipboard` - 剪贴板操作
- `grim` - 截图

### 音频控制
- `alsa-utils` (amixer) 或 `pulseaudio-utils` (pactl) 或 `wireplumber` (wpctl)

安装依赖：
```bash
# Debian/Ubuntu
sudo apt install xdotool xclip scrot wmctrl alsa-utils

# 或 Wayland
sudo apt install ydotool wl-clipboard grim

# Fedora
sudo dnf install xdotool xclip scrot wmctrl alsa-utils

# Arch
sudo pacman -S xdotool xclip scrot wmctrl alsa-utils
```

## 功能特性

### 1. 鼠标控制
- 移动鼠标到指定坐标（支持 X11 和 Wayland）
- 鼠标点击（左/右/中键，支持双击）
- 鼠标按下/释放
- 鼠标滚轮滚动
- 获取当前鼠标位置

### 2. 键盘控制
- 按键按下和释放
- 组合键操作（Ctrl+C, Alt+Tab 等）
- 文本输入（支持多语言）
- 模拟人类打字

### 3. 屏幕操作
- 全屏/区域截图
- 获取指定像素颜色
- 列出所有显示器

### 4. 窗口管理
- 列出所有窗口
- 按标题查找窗口
- 激活窗口（仅限 X11）
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
et key_press --key "ctrl+c"
et type_text --text "Hello World"

# 截图
et screenshot --output screenshot.png
et pixel_color --x 100 --y 100

# 窗口管理
et window_list
et window_activate --title "Firefox"

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
| `window_find` | 查找窗口 | `title`, `className`, `processId` |
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
- **运行平台**: Linux x64 (glibc 2.17+)

## 安装方法

### 方式一：直接下载
下载 `et` 文件，放置到系统 PATH 目录：
```bash
sudo cp et /usr/local/bin/
sudo chmod +x /usr/local/bin/et
```

### 方式二：从源码编译

```bash
# 克隆仓库
cd tools/EasyTouch/EasyTouch-Linux

# 构建（需要 .NET 10 SDK）
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishAot=true

# 输出文件位于 bin/Release/net10.0/linux-x64/publish/et
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

## 注意事项

1. **X11 vs Wayland**: Wayland 安全模型限制了部分功能（如窗口激活），工具会自动检测并使用适当的方法
2. **权限**: 某些功能可能需要用户权限，建议在普通用户下运行
3. **依赖**: 确保安装了相应的系统工具（xdotool/xclip 等）
4. **兼容性**: 在纯 Wayland 环境下部分功能受限

## 已知限制

- **Wayland 窗口管理**: 由于安全限制，Wayland 不支持直接枚举和激活其他应用的窗口
- **全局快捷键**: 无法通过工具模拟全局快捷键（需要窗口管理器支持）
- **屏幕录制权限**: macOS 风格的屏幕录制权限管理可能会影响某些发行版

## 故障排除

### 命令无响应
- 检查依赖是否安装：`which xdotool`
- 检查权限：确保当前用户有访问 X11/Wayland 的权限

### 截图失败
- 安装截图工具：`sudo apt install scrot` 或 `sudo apt install grim`
- 检查文件权限：确保输出目录可写

### 音频控制失败
- 检查音频系统：`pactl info` 或 `amixer`
- 确保用户属于 audio 组：`sudo usermod -a -G audio $USER`
