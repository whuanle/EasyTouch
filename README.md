# EasyTouch-Windows (et)

一个用于 Windows 系统自动化操作的工具，支持鼠标、键盘、屏幕、窗口、系统资源等多种操作。支持 CLI 和 MCP 两种使用方式。

## 功能模块

### 1. 鼠标控制 (Mouse)

| 命令 | 描述 |
|------|------|
| `mouse_move` | 移动鼠标到指定坐标 (相对/绝对) |
| `mouse_click` | 鼠标点击 (左/右/中键，单击/双击) |
| `mouse_down` | 鼠标按下 |
| `mouse_up` | 鼠标释放 |
| `mouse_scroll` | 鼠标滚轮滚动 |
| `mouse_position` | 获取当前鼠标位置 |

### 2. 键盘控制 (Keyboard)

| 命令 | 描述 |
|------|------|
| `key_press` | 按键按下并释放 |
| `key_down` | 按键按下 |
| `key_up` | 按键释放 |
| `type_text` | 输入文本字符串 (支持中文) |

### 3. 屏幕操作 (Screen)

| 命令 | 描述 |
|------|------|
| `screenshot` | 截图 |
| `pixel_color` | 获取指定像素颜色 |
| `screen_list` | 列出所有显示器 |

### 4. 窗口管理 (Window)

| 命令 | 描述 |
|------|------|
| `window_list` | 列出所有窗口 |
| `window_find` | 查找窗口 |
| `window_activate` | 激活窗口 |
| `window_foreground` | 获取前台窗口 |

### 5. 系统信息 (System)

| 命令 | 描述 |
|------|------|
| `os_info` | 操作系统信息 |
| `cpu_info` | CPU 信息 |
| `memory_info` | 内存使用情况 |
| `disk_list` | 磁盘列表 |
| `process_list` | 进程列表 |
| `lock_screen` | 锁定屏幕 |

### 6. 剪贴板 (Clipboard)

| 命令 | 描述 |
|------|------|
| `clipboard_get_text` | 获取剪贴板文本 |
| `clipboard_set_text` | 设置剪贴板文本 |
| `clipboard_clear` | 清空剪贴板 |
| `clipboard_get_files` | 获取剪贴板文件列表 |

### 7. 音频控制 (Audio)

| 命令 | 描述 |
|------|------|
| `volume_get` | 获取当前音量 |
| `volume_set` | 设置音量 |
| `volume_mute` | 静音/取消静音 |
| `audio_devices` | 列出音频设备 |

## CLI 命令行模式

### 基本用法

```bash
et <command> [options]
```

### 鼠标控制

**移动鼠标**
```bash
# 绝对位置
et mouse_move --x 100 --y 200

# 相对位置
et mouse_move --x 50 --y -30 --relative

# 平滑移动（模拟人类操作）
et mouse_move --x 100 --y 200 --duration 500
```

**鼠标点击**
```bash
# 左键单击
et mouse_click

# 左键双击
et mouse_click --double

# 右键单击
et mouse_click --button right

# 中键单击
et mouse_click --button middle
```

**鼠标滚轮**
```bash
# 向上滚动3格
et mouse_scroll --amount 3

# 向下滚动3格
et mouse_scroll --amount -3

# 水平滚动
et mouse_scroll --amount 3 --horizontal
```

**获取鼠标位置**
```bash
et mouse_position
```

### 键盘控制

**按键**
```bash
# 按下单个键
et key_press --key "a"
et key_press --key "enter"
et key_press --key "esc"

# 组合键
et key_press --key "ctrl+c"
et key_press --key "ctrl+v"
et key_press --key "alt+tab"
et key_press --key "win+d"
```

**输入文本**
```bash
# 普通文本
et type_text --text "Hello World"

# 中文文本
et type_text --text "你好，世界！"

# 模拟人类打字（带随机间隔）
et type_text --text "Hello World" --human --interval 50
```

### 屏幕操作

**截图**
```bash
# 全屏截图
et screenshot --output screenshot.png

# 区域截图
et screenshot --x 100 --y 100 --width 800 --height 600 --output region.png

# 截图到剪贴板（不保存文件）
et screenshot
```

**获取像素颜色**
```bash
et pixel_color --x 100 --y 200
```

**列出显示器**
```bash
et screen_list
```

### 窗口管理

**列出窗口**
```bash
# 列出所有可见窗口
et window_list

# 列出所有窗口（包括隐藏）
et window_list --visible-only false

# 按标题过滤
et window_list --filter "Chrome"
```

**查找窗口**
```bash
# 按标题查找
et window_find --title "记事本"

# 按类名查找
et window_find --class "Notepad"

# 按进程ID查找
et window_find --pid 1234
```

**激活窗口**
```bash
# 通过标题激活
et window_activate --title "记事本"

# 通过窗口句柄激活
et window_activate --handle 123456
```

**获取前台窗口**
```bash
et window_foreground
```

### 系统信息

**操作系统信息**
```bash
et os_info
```

**CPU 信息**
```bash
et cpu_info
```

**内存信息**
```bash
et memory_info
```

**磁盘信息**
```bash
et disk_list
```

**进程列表**
```bash
# 列出所有进程
et process_list

# 按名称过滤
et process_list --filter "chrome"
```

**锁定屏幕**
```bash
et lock_screen
```

### 剪贴板操作

**获取剪贴板文本**
```bash
et clipboard_get_text
```

**设置剪贴板文本**
```bash
et clipboard_set_text --text "Hello World"
```

**清空剪贴板**
```bash
et clipboard_clear
```

**获取剪贴板文件列表**
```bash
et clipboard_get_files
```

### 音频控制

**获取音量**
```bash
et volume_get
```

**设置音量**
```bash
et volume_set --level 50
```

**静音/取消静音**
```bash
# 静音
et volume_mute --state true

# 取消静音
et volume_mute --state false
```

**列出音频设备**
```bash
et audio_devices
```

## MCP 模式

### stdio 模式
```bash
et --mcp
```

启动后通过 stdio 接收 MCP 协议的 JSON-RPC 请求。

### MCP Tools

| Tool | 描述 | 参数 |
|------|------|------|
| `mouse_move` | 移动鼠标 | `x`, `y`, `relative`, `duration` |
| `mouse_click` | 点击鼠标 | `button`, `double` |
| `mouse_position` | 获取鼠标位置 | - |
| `key_press` | 按下按键 | `key` |
| `type_text` | 输入文本 | `text`, `interval`, `humanLike` |
| `screenshot` | 截图 | `x`, `y`, `width`, `height`, `outputPath` |
| `pixel_color` | 获取像素颜色 | `x`, `y` |
| `window_list` | 列出窗口 | `visibleOnly`, `titleFilter` |
| `window_find` | 查找窗口 | `title`, `className`, `processId` |
| `window_activate` | 激活窗口 | `handle` |
| `system_info` | 系统信息 | - |
| `process_list` | 进程列表 | `nameFilter` |
| `clipboard_get_text` | 获取剪贴板 | - |
| `clipboard_set_text` | 设置剪贴板 | `text` |
| `volume_get` | 获取音量 | - |
| `volume_set` | 设置音量 | `level` |

## 技术规格

- **目标框架**: .NET 10
- **编译方式**: AOT (Ahead-of-Time)
- **输出文件**: `et.exe` (单文件，自包含)
- **文件大小**: ~3.9 MB
- **运行平台**: Windows 10/11 x64

## 安装方法

### 方式一：直接下载
下载 `et.exe` 文件，放置到系统 PATH 目录或任意位置。

### 方式二：从源码编译

```bash
# 克隆仓库
git clone <repository-url>
cd tools/EasyTouch/EasyTouch-Windows

# 构建（需要 .NET 10 SDK）
dotnet publish EasyTouch-Windows.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=true

# 输出文件位于 bin/Release/net10.0/win-x64/publish/et.exe
```

## 集成到 MCP 客户端

### Claude Desktop 配置

在 `claude_desktop_config.json` 中添加：

```json
{
  "mcpServers": {
    "easytouch": {
      "command": "C:\\path\\to\\et.exe",
      "args": ["--mcp"]
    }
  }
}
```

### 其他 MCP 客户端

配置命令为 `et.exe`，参数为 `--mcp`，使用 stdio 传输。

## 注意事项

1. **管理员权限**: 部分功能（如操作系统关机、某些窗口操作）可能需要以管理员权限运行
2. **AOT 编译**: 单文件已包含所有依赖，无需安装 .NET 运行时
3. **安全性**: 该工具可以控制系统，请确保只在受信任的环境中使用
4. **兼容性**: 仅在 Windows 10/11 x64 上测试通过

## 许可证

MIT License
