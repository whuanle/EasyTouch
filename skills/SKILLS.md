# EasyTouch Skills

EasyTouch 作为 Model Context Protocol (MCP) 服务器，为 AI 助手提供跨平台的桌面自动化能力。

## 概述

EasyTouch 实现了 MCP stdio 接口，允许 AI 助手通过标准化的 JSON-RPC 协议调用桌面自动化功能。

### 核心能力

1. **输入控制** - 鼠标和键盘的精确控制
2. **屏幕捕获** - 截图和像素级分析
3. **窗口管理** - 枚举和操作应用程序窗口
4. **系统监控** - 获取系统资源和状态信息
5. **浏览器自动化** - 通过 Playwright 实现 Web 自动化

## MCP 配置

### Claude Desktop 配置示例

在 `claude_desktop_config.json` 中添加：

```json
{
  "mcpServers": {
    "easytouch": {
      "command": "et",
      "args": ["mcp", "stdio"]
    }
  }
}
```

如果通过 npm 安装：

```json
{
  "mcpServers": {
    "easytouch": {
      "command": "npx",
      "args": ["-y", "easytouch", "mcp", "stdio"]
    }
  }
}
```

### Cursor 配置示例

在 `.cursor/mcp.json` 中添加：

```json
{
  "mcpServers": {
    "easytouch": {
      "command": "et",
      "args": ["mcp", "stdio"]
    }
  }
}
```

## 工具列表

### 鼠标控制

- `mouse_move` - 移动鼠标到指定坐标
- `mouse_click` - 点击鼠标按钮
- `mouse_down` / `mouse_up` - 按住/释放鼠标
- `mouse_scroll` - 滚动鼠标滚轮
- `mouse_position` - 获取当前鼠标位置

### 键盘控制

- `key_press` - 按下单个按键
- `key_down` / `key_up` - 按住/释放按键
- `type_text` - 输入文本字符串

### 屏幕操作

- `screenshot` - 截取屏幕或指定区域
- `pixel_color` - 获取指定坐标的像素颜色
- `screen_list` - 列出所有显示器

### 窗口管理

- `window_list` - 列出所有窗口
- `window_find` - 根据条件查找窗口
- `window_activate` - 激活指定窗口
- `window_foreground` - 获取当前活动窗口

### 系统信息

- `system_info` / `os_info` - 获取操作系统信息
- `cpu_info` - 获取 CPU 信息
- `memory_info` - 获取内存信息
- `disk_list` - 列出磁盘驱动器
- `process_list` - 列出运行中的进程
- `lock_screen` - 锁定屏幕

### 剪贴板

- `clipboard_get_text` - 获取剪贴板文本
- `clipboard_set_text` - 设置剪贴板文本
- `clipboard_clear` - 清空剪贴板
- `clipboard_get_files` - 获取剪贴板中的文件列表

### 音频控制

- `volume_get` - 获取当前音量
- `volume_set` - 设置音量
- `volume_mute` - 静音/取消静音
- `audio_devices` - 列出音频设备

### 浏览器自动化（可选）

需要安装 Playwright：

- `browser_launch` - 启动浏览器实例
- `browser_navigate` - 导航到 URL
- `browser_click` - 点击页面元素
- `browser_fill` - 填充输入框
- `browser_find` - 查找元素
- `browser_get_text` - 获取页面文本
- `browser_screenshot` - 页面截图
- `browser_evaluate` - 执行 JavaScript
- `browser_wait_for` - 等待元素出现
- `browser_go_back` / `browser_go_forward` - 后退/前进
- `browser_reload` - 刷新页面
- `browser_scroll` - 滚动页面
- `browser_select` - 选择下拉框选项
- `browser_upload` - 上传文件
- `browser_get_cookies` / `browser_set_cookie` / `browser_clear_cookies` - Cookie 管理
- `browser_add_route` / `browser_remove_route` - 网络请求拦截
- `browser_close` - 关闭浏览器

## 使用示例

### 自动化工作流

AI 助手可以使用 EasyTouch 执行复杂的自动化任务：

```
用户："帮我打开计算器，计算 123 + 456，然后截图保存"

AI：
1. 使用 window_find 查找计算器窗口
2. 使用 window_activate 激活计算器
3. 使用 click 点击数字按钮
4. 使用 screenshot 截图
5. 使用 mouse_move 和 mouse_click 保存截图
```

### 数据提取

```
用户："打开浏览器，访问 example.com，提取所有链接"

AI：
1. 使用 browser_launch 启动浏览器
2. 使用 browser_navigate 访问网站
3. 使用 browser_evaluate 执行 JS 提取链接
4. 使用 browser_close 关闭浏览器
```

### 系统监控

```
用户："检查系统资源使用情况"

AI：
1. 使用 cpu_info 获取 CPU 信息
2. 使用 memory_info 获取内存信息
3. 使用 process_list 获取进程列表
4. 汇总报告给用户
```

## 安全考虑

- EasyTouch 需要适当的权限才能控制系统
- 浏览器自动化功能需要用户显式安装 Playwright
- 建议仅在受信任的环境中使用
- 敏感操作（如锁屏）应谨慎使用

## 故障排除

### MCP 连接失败

1. 确认 `et` 命令在 PATH 中
2. 检查 Claude Desktop / Cursor 的配置文件格式
3. 查看 EasyTouch 的输出日志

### 工具调用失败

1. 检查平台兼容性（某些功能在特定平台可能受限）
2. 确认参数格式正确
3. 查看错误消息获取详细信息

### 浏览器自动化不可用

1. 确认已安装 Playwright：`npx playwright --version`
2. 确认已安装浏览器二进制文件：`npx playwright install chromium`
3. 查看详细安装指南：[BROWSER_SETUP.md](BROWSER_SETUP.md)

## 相关资源

- [完整测试指南](../docs/MCP_TEST_GUIDE.md)
- [浏览器自动化设置](BROWSER_SETUP.md)
- [GitHub 仓库](https://github.com/yourusername/easytouch)
