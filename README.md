# EasyTouch (et)

跨平台系统自动化操作工具，支持 Windows、Linux、macOS。提供 CLI 命令行和 MCP 服务器两种使用方式，支持鼠标键盘控制、屏幕截图、窗口管理、系统信息查询、浏览器操作等功能。



## 功能概览

| 模块 | 功能 |
|------|------|
| 🖱️ 鼠标控制 | 移动、点击、滚动、获取位置 |
| ⌨️ 键盘控制 | 按键、组合键、文本输入 |
| 📷 屏幕操作 | 截图、获取像素颜色、多显示器支持 |
| 🪟 窗口管理 | 列出、查找、激活窗口 |
| 🖥️ 系统信息 | CPU、内存、磁盘、进程 |
| 📋 剪贴板 | 文本读写、文件列表 |
| 🌐 浏览器控制 | 启动浏览器、页面导航、元素交互、截图 |



## 安装

### NPM 安装（推荐）

```bash
# Windows
npm i @whuanle/easytouch-windows

# Linux
npm i @whuanle/easytouch-linux

# macOS
npm i @whuanle/easytouch-mac
```



或者从 [GitHub Releases](../../releases) 下载对应平台的可执行文件，并添加环境变量。



测试是否正常工作：



### 浏览器操作支持

EasyTouch 操作浏览器需要依赖 playwright，可以通过命令一键安装对应的环境：

```bash
npm install @playwright/test
```



你可以通过哦哦脚本快速安装 chromium 浏览器。

```
npx playwright install chromium
```



### 作为 MCP 工具使用

在 Claude、Cursor 等工具中，配置 MCP 的方式都是大同小异。

通过 npm/bun 等方式安装的 EasyTouch，程序文件在



在配置文件中添加：

**Windows**

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

**NPM 安装方式**

```json
{
  "mcpServers": {
    "easytouch": {
      "command": "npx",
      "args": ["-y", "easytouch-windows", "--mcp"]
    }
  }
}
```

**Linux / macOS**

```json
{
  "mcpServers": {
    "easytouch": {
      "command": "/path/to/et",
      "args": ["--mcp"]
    }
  }
}
```



### 作为 Skills 给 AI 使用





### CLI 模式

```bash
# 移动鼠标到坐标 (100, 200)
et mouse_move --x 100 --y 200

# 输入文本（支持中文）
et type_text --text "你好，世界！"

# 截图并保存
et screenshot --output screenshot.png

# 获取当前活动窗口
et window_foreground

# 查看所有命令
et --help
```



### MCP 模式

启动 MCP 服务器：

```bash
et --mcp
```



## CLI 命令参考

### 鼠标控制

```bash
# 移动鼠标（绝对坐标）
et mouse_move --x 100 --y 200

# 相对移动
t mouse_move --x 50 --y -30 --relative

# 平滑移动（500ms 动画）
et mouse_move --x 100 --y 200 --duration 500

# 左键单击（默认）
et mouse_click

# 右键双击
t mouse_click --button right --double

# 向上滚动3格
t mouse_scroll --amount 3

# 水平滚动
t mouse_scroll --amount 3 --horizontal

# 获取当前位置
t mouse_position
```

### 键盘控制

```bash
# 按下单个键
t key_press --key "enter"

# 组合键
t key_press --key "ctrl+c"
t key_press --key "alt+tab"
t key_press --key "win+d"

# 输入文本
t type_text --text "Hello World"

# 模拟人工打字（带随机间隔）
t type_text --text "Hello World" --human --interval 50
```

### 屏幕操作

```bash
# 全屏截图
t screenshot --output screenshot.png

# 区域截图
t screenshot --x 100 --y 100 --width 800 --height 600 --output region.png

# 获取像素颜色
t pixel_color --x 100 --y 200

# 列出显示器
t screen_list
```

### 窗口管理

```bash
# 列出可见窗口
t window_list

# 按标题过滤
t window_list --filter "Chrome"

# 查找窗口
t window_find --title "记事本"

# 激活窗口
t window_activate --title "记事本"

# 获取前台窗口
t window_foreground
```

### 系统信息

```bash
# 操作系统信息
et os_info

# CPU 信息
et cpu_info

# 内存信息
et memory_info

# 磁盘列表
et disk_list

# 进程列表
et process_list --filter "chrome"

# 锁定屏幕
et lock_screen
```

### 剪贴板

```bash
# 获取文本
et clipboard_get_text

# 设置文本
et clipboard_set_text --text "Hello World"

# 清空
et clipboard_clear

# 获取文件列表
et clipboard_get_files
```

### 浏览器控制

```bash
# 列出浏览器实例
et browser_list

# 启动 Chromium（无头模式）
et browser_launch --browser chromium --headless

# 打开页面
et browser_navigate --browser-id <id> --url "https://example.com"

# 点击元素
et browser_click --browser-id <id> --selector "#submit"

# 输入内容
et browser_fill --browser-id <id> --selector "input[name='q']" --value "EasyTouch"

# 页面截图
et browser_screenshot --browser-id <id> --output page.png --full-page true

# 执行脚本
et browser_evaluate --browser-id <id> --script "document.title"

# 关闭浏览器
et browser_close --browser-id <id>
```



## MCP 集成

### Claude Desktop



### 可用 MCP Tools

| Tool | 描述 |
|------|------|
| `mouse_move` | 移动鼠标 |
| `mouse_click` | 点击鼠标 |
| `mouse_position` | 获取鼠标位置 |
| `key_press` | 按下按键 |
| `type_text` | 输入文本 |
| `screenshot` | 截图 |
| `pixel_color` | 获取像素颜色 |
| `window_list` | 列出窗口 |
| `window_find` | 查找窗口 |
| `window_activate` | 激活窗口 |
| `system_info` | 系统信息 |
| `process_list` | 进程列表 |
| `clipboard_get_text` | 获取剪贴板文本 |
| `clipboard_set_text` | 设置剪贴板文本 |
| `browser_launch` | 启动浏览器 |
| `browser_navigate` | 页面导航 |
| `browser_click` | 点击页面元素 |
| `browser_fill` | 填充输入框 |
| `browser_find` | 查找页面元素 |
| `browser_get_text` | 获取页面文本 |
| `browser_screenshot` | 浏览器截图 |
| `browser_evaluate` | 执行页面脚本 |
| `browser_wait_for` | 等待元素状态 |
| `browser_close` | 关闭浏览器 |
| `browser_list` | 列出浏览器实例 |



更多 MCP 使用文档见 [skills/SKILLS.md](skills/SKILLS.md)

## 技术规格

- **目标框架**: .NET 10
- **编译方式**: AOT (Ahead-of-Time)
- **输出**: 单文件可执行程序，无需运行时
- **文件大小**: ~3-5 MB（依平台而异）
- **支持平台**:
  - Windows 10/11 x64
  - Linux x64（X11，不支持 Wayland）
  - macOS x64 / ARM64

## 平台说明

### Windows
- 完全支持所有功能
- 部分功能可能需要管理员权限

### Linux
- 需要 X11 显示服务器
- 不支持 Wayland
- 建议在图形界面环境中使用

### macOS
- 需要授予辅助功能权限（系统设置 → 隐私与安全性 → 辅助功能）
- 截图功能需要屏幕录制权限

## 项目结构

```
EasyTouch/
├── EasyTouch-Windows/    # Windows 版本
├── EasyTouch-Linux/      # Linux 版本
├── EasyTouch-Mac/        # macOS 版本
├── EasyTouch.Tests/      # 共享测试
├── EasyTouch.Tests.*     # 平台特定测试
├── docs/                 # 文档
├── skills/               # MCP 技能文档
├── scripts/              # 构建脚本
├── npx/                  # NPM 包装器
└── README.md
```

## 文档

- [MCP 测试指南](docs/MCP_TEST_GUIDE.md) - MCP 功能测试
- [NPM 测试指南](docs/NPM_TEST_GUIDE.md) - NPM 包测试
- [跨平台测试](docs/CROSS_PLATFORM_TESTING.md) - 跨平台测试策略
- [发布指南](docs/PUBLISHING.md) - NPM 包发布流程
- [浏览器自动化](skills/BROWSER_SETUP.md) - Playwright 浏览器自动化

## 许可证

MIT License
