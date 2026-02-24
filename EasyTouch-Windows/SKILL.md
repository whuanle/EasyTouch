# EasyTouch Windows MCP Skill

## 基本信息

- **名称**: EasyTouch Windows Automation
- **版本**: 1.0.0
- **描述**: Windows 系统自动化控制工具，支持鼠标、键盘、屏幕、窗口、系统资源等操作
- **作者**: 痴者工良
- **许可证**: MIT

## 功能特性

### 1. 鼠标控制
- 移动鼠标到指定坐标（支持相对/绝对位置，平滑移动）
- 鼠标点击（左/右/中键，支持双击）
- 鼠标按下/释放
- 鼠标滚轮滚动
- 获取当前鼠标位置

### 2. 键盘控制
- 按键按下和释放
- 组合键操作
- 文本输入（支持中文和模拟人类打字）
- 独立的 key_down/key_up 操作

### 3. 屏幕操作
- 全屏/区域截图（PNG格式）
- 获取指定像素颜色
- 列出所有显示器

### 4. 窗口管理
- 列出所有窗口
- 按标题/类名/PID查找窗口
- 激活/最小化/最大化/关闭窗口
- 获取前台窗口
- 设置窗口置顶

### 5. 系统信息
- 获取操作系统信息
- CPU 信息和使用率
- 内存使用情况
- 磁盘信息
- 进程列表和管理

### 6. 剪贴板操作
- 获取/设置剪贴板文本
- 获取剪贴板文件列表
- 清空剪贴板

### 7. 音频控制
- 获取/设置系统音量
- 静音/取消静音
- 列出音频设备

### 8. 浏览器自动化（Playwright .NET）
- 支持 `chromium/firefox/webkit/edge` 浏览器
- 支持页面导航、点击、输入、等待、截图、执行 JS、Cookie 管理等操作
- CLI 下浏览器会话默认通过后台 daemon 持久化，`browserId` 可跨命令复用
- 提供 daemon 管理命令：`browser_daemon_status`、`browser_daemon_stop`

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
et window_activate --handle 123456

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

# 浏览器（会话持久化）
et browser_launch --browser edge --headless false
et browser_list
et browser_navigate --browser-id browser_1 --url "https://example.com"
et browser_daemon_status
et browser_daemon_stop
```

### MCP 模式

```bash
et --mcp
```

启动后通过 stdio 接收 MCP 协议的 JSON-RPC 请求。

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
| `window_activate` | 激活窗口 | `title`, `handle` |
| `window_foreground` | 获取前台窗口 | - |
| `window_minimize` | 最小化窗口 | `handle` |
| `window_maximize` | 最大化窗口 | `handle` |
| `window_close` | 关闭窗口 | `handle` |
| `window_set_topmost` | 设置窗口置顶 | `handle`, `topmost` |
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
| `browser_launch` | 启动浏览器会话 | `browser`, `headless`, `executable`, `user-data-dir` |
| `browser_list` | 列出浏览器会话 | - |
| `browser_navigate` | 导航页面 | `browser-id`, `url`, `wait-until`, `timeout` |
| `browser_click` | 点击元素 | `browser-id`, `selector`, `selector-type` |
| `browser_fill` | 输入文本 | `browser-id`, `selector`, `value` |
| `browser_wait_for` | 等待元素状态 | `browser-id`, `selector`, `state`, `timeout` |
| `browser_screenshot` | 浏览器截图 | `browser-id`, `selector`, `output`, `full-page` |
| `browser_close` | 关闭浏览器会话 | `browser-id`, `force` |
| `browser_daemon_status` | 查询浏览器 daemon 状态 | - |
| `browser_daemon_stop` | 停止浏览器 daemon | - |

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
2. **发布模式**: 浏览器能力依赖 Playwright，发布时使用非 AOT 单文件模式（自包含）
3. **安全性**: 该工具可以控制系统，请确保只在受信任的环境中使用
4. **兼容性**: 仅在 Windows 10/11 x64 上测试通过
