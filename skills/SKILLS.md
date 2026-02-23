# EasyTouch MCP 技能

EasyTouch 作为 MCP 服务器，为 AI 助手提供跨平台的桌面自动化能力。

## 快速配置

### Claude Desktop

配置文件位置：
- Windows: `%APPDATA%\Claude\claude_desktop_config.json`
- macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`
- Linux: `~/.config/Claude/claude_desktop_config.json`

添加配置：

```json
{
  "mcpServers": {
    "easytouch": {
      "command": "et",
      "args": ["--mcp"]
    }
  }
}
```

使用 NPM 安装的快捷方式：

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

### Cursor

配置文件：`.cursor/mcp.json`

```json
{
  "mcpServers": {
    "easytouch": {
      "command": "et",
      "args": ["--mcp"]
    }
  }
}
```

## 工具列表

### 鼠标控制

| Tool | 功能 | 关键参数 |
|------|------|----------|
| `mouse_move` | 移动鼠标 | `x`, `y`, `relative`, `duration` |
| `mouse_click` | 点击鼠标 | `button` (left/right/middle), `double` |
| `mouse_down` | 按住鼠标按钮 | `button` |
| `mouse_up` | 释放鼠标按钮 | `button` |
| `mouse_scroll` | 滚动滚轮 | `amount`, `horizontal` |
| `mouse_position` | 获取当前位置 | - |

### 键盘控制

| Tool | 功能 | 关键参数 |
|------|------|----------|
| `key_press` | 按下并释放按键 | `key` |
| `key_down` | 按住按键 | `key` |
| `key_up` | 释放按键 | `key` |
| `type_text` | 输入文本 | `text`, `interval`, `humanLike` |

**支持的按键名称**: `a-z`, `0-9`, `f1-f12`, `enter`, `esc`, `tab`, `space`, `backspace`, `delete`, `up`, `down`, `left`, `right`, `home`, `end`, `pageup`, `pagedown`, `ctrl`, `alt`, `shift`, `win`, `cmd`

**组合键格式**: `ctrl+c`, `alt+tab`, `ctrl+shift+esc`, `win+d`

### 屏幕操作

| Tool | 功能 | 关键参数 |
|------|------|----------|
| `screenshot` | 截图 | `x`, `y`, `width`, `height`, `outputPath` |
| `pixel_color` | 获取像素颜色 | `x`, `y` |
| `screen_list` | 列出显示器 | - |

### 窗口管理

| Tool | 功能 | 关键参数 |
|------|------|----------|
| `window_list` | 列出窗口 | `visibleOnly`, `titleFilter` |
| `window_find` | 查找窗口 | `title`, `className`, `processId` |
| `window_activate` | 激活窗口 | `title` 或 `handle` |
| `window_foreground` | 获取前台窗口 | - |

### 系统信息

| Tool | 功能 |
|------|------|
| `os_info` | 操作系统信息 |
| `cpu_info` | CPU 信息 |
| `memory_info` | 内存使用情况 |
| `disk_list` | 磁盘列表 |
| `process_list` | 运行中的进程 |
| `lock_screen` | 锁定屏幕 |

### 剪贴板

| Tool | 功能 | 关键参数 |
|------|------|----------|
| `clipboard_get_text` | 获取文本 | - |
| `clipboard_set_text` | 设置文本 | `text` |
| `clipboard_clear` | 清空 | - |
| `clipboard_get_files` | 获取文件列表 | - |

### 音频控制

| Tool | 功能 | 关键参数 |
|------|------|----------|
| `volume_get` | 获取音量 | - |
| `volume_set` | 设置音量 | `level` (0-100) |
| `volume_mute` | 静音/取消静音 | `state` (true/false) |
| `audio_devices` | 列出音频设备 | - |

## 使用场景

### 界面自动化

用户："帮我打开计算器并计算 123 + 456"

AI 执行：
1. `window_find` 查找计算器窗口，如未找到则提示用户打开
2. `window_activate` 激活计算器
3. `mouse_click` 依次点击 1、2、3
4. `mouse_click` 点击加号
5. `mouse_click` 依次点击 4、5、6
6. `mouse_click` 点击等号
7. `screenshot` 截取结果

### 数据录入

用户："帮我在这个表单中填写信息"

AI 执行：
1. `screenshot` 查看当前界面
2. `mouse_click` 点击第一个输入框
3. `type_text` 输入内容
4. `key_press` 按 Tab 切换到下一个字段
5. 重复直到完成

### 系统监控

用户："检查系统资源使用情况"

AI 执行：
1. `cpu_info` 获取 CPU 信息
2. `memory_info` 获取内存信息
3. `process_list` 获取进程列表
4. 汇总报告给用户

### 自动化测试

用户："测试这个按钮的功能"

AI 执行：
1. `screenshot` 记录初始状态
2. `mouse_click` 点击目标按钮
3. `screenshot` 记录变化
4. 比较前后差异，验证结果

## 故障排除

### MCP 连接失败

1. 验证 `et` 是否在 PATH 中：`which et` (Linux/macOS) 或 `where et` (Windows)
2. 检查配置文件 JSON 格式是否有效
3. 查看 Claude/Cursor 日志获取错误信息

### 工具调用失败

1. 检查平台兼容性（Linux 需要 X11）
2. 验证参数格式和类型
3. 查看错误消息了解详情

### 权限问题

- **Windows**: 以管理员身份运行
- **macOS**: 系统设置 → 隐私与安全性 → 辅助功能 → 添加终端应用
- **Linux**: 确保在 X11 会话中运行

## 平台限制

| 功能 | Windows | Linux | macOS |
|------|---------|-------|-------|
| 鼠标控制 | ✅ 完整 | ✅ 完整 | ✅ 完整 |
| 键盘控制 | ✅ 完整 | ✅ 完整 | ✅ 完整 |
| 截图 | ✅ 完整 | ✅ 完整 | ✅ 完整 |
| 窗口管理 | ✅ 完整 | ⚠️ 部分 | ⚠️ 部分 |
| 音频控制 | ✅ 完整 | ⚠️ 部分 | ⚠️ 部分 |

**Linux 限制**:
- 需要 X11，不支持 Wayland
- 部分桌面环境功能受限

**macOS 限制**:
- 需要辅助功能权限
- 截图需要屏幕录制权限

## 浏览器自动化（可选）

EasyTouch 支持通过 Playwright 进行浏览器自动化。

安装 Playwright：

```bash
npm install -g playwright
npx playwright install chromium
```

可用的浏览器工具：
- `browser_launch` - 启动浏览器
- `browser_navigate` - 导航到 URL
- `browser_click` - 点击元素
- `browser_fill` - 填充输入框
- `browser_screenshot` - 页面截图
- `browser_evaluate` - 执行 JavaScript
- 更多...

详细设置见 [BROWSER_SETUP.md](BROWSER_SETUP.md)

## 相关文档

- [MCP 测试指南](../docs/MCP_TEST_GUIDE.md) - 完整的 MCP 功能测试指南
- [浏览器自动化设置](BROWSER_SETUP.md) - Playwright 安装和配置
- [项目 README](../README.md) - 项目总览和 CLI 使用说明
