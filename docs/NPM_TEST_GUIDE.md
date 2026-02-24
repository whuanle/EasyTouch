# EasyTouch NPM 包测试指南

## 📦 安装方式

### 方式一：安装主包（推荐）
```bash
npm install -g easytouch
```

安装后直接使用：
```bash
et --help
et mouse_position
```

### 方式二：安装平台特定包
```bash
# Windows
npm install -g easytouch-windows

# Linux
npm install -g easytouch-linux

# macOS
npm install -g easytouch-macos
```

## 🧪 测试命令

### 1. CLI 命令测试

#### Windows
```powershell
# 基础命令
et --version
et --help

# 鼠标控制
et mouse_position
et mouse_move --x 100 --y 100
et mouse_click

# 系统信息
et os_info
et cpu_info
et memory_info

# 屏幕操作
et screenshot --output test.png
et screen_list

# 窗口管理
et window_list
et window_foreground

# 剪贴板
et clipboard_set_text --text "Hello World"
et clipboard_get_text

# 进程和磁盘
et process_list
et disk_list
```

#### Linux
```bash
# 基础命令
et --version
et --help

# 鼠标控制
et mouse_position
et mouse_move --x 100 --y 100
et mouse_click

# 系统信息
et os_info
et cpu_info
et memory_info

# 屏幕操作 (需要 X11)
et screenshot --output test.png
et screen_list

# 剪贴板 (需要 xclip 或 xsel)
et clipboard_set_text --text "Hello World"
et clipboard_get_text

# 进程和磁盘
et process_list
et disk_list
```

#### macOS
```bash
# 基础命令
et --version
et --help

# 鼠标控制
et mouse_position
et mouse_move --x 100 --y 100
et mouse_click

# 系统信息
et os_info
et cpu_info
et memory_info

# 屏幕操作 (需要权限)
et screenshot --output test.png
et screen_list

# 剪贴板
et clipboard_set_text --text "Hello World"
et clipboard_get_text

# 进程和磁盘
et process_list
et disk_list
```

### 2. 运行内置测试

```bash
# 运行完整测试套件
npm test -g easytouch

# 或在安装目录
cd $(npm root -g)/easytouch
node test.js
```

### 3. MCP 模式测试

#### 测试 MCP stdio 模式
```bash
# 启动 MCP 模式
et --mcp

# 在另一个终端，发送测试请求
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}' | et --mcp
```

#### Claude Desktop 配置测试

创建或编辑配置文件：

**Windows:**
```powershell
# 配置文件路径
$env:AppData\Claude\claude_desktop_config.json
```

**macOS:**
```bash
# 配置文件路径
~/Library/Application Support/Claude/claude_desktop_config.json
```

**Linux:**
```bash
# 配置文件路径
~/.config/Claude/claude_desktop_config.json
```

配置内容：
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

测试步骤：
1. 保存配置后重启 Claude Desktop
2. 在对话中输入：`你能获取一下我的系统信息吗？`
3. 检查是否调用了 `system_info` tool

## 🔧 故障排除

### 问题：命令未找到 (command not found)

**Windows:**
```powershell
# 检查 npm 全局安装路径
npm config get prefix

# 确保路径在 PATH 环境变量中
# 通常需要添加: C:\Users\<用户名>\AppData\Roaming\npm
```

**Linux/macOS:**
```bash
# 检查 npm 全局安装路径
npm config get prefix

# 确保路径在 PATH 中
export PATH="$PATH:$(npm config get prefix)/bin"

# 添加到 ~/.bashrc 或 ~/.zshrc
```

### 问题：权限不足

**Windows:**
- 以管理员身份运行 PowerShell 或 CMD
- 或右键点击终端选择"以管理员身份运行"

**Linux:**
```bash
# 某些功能需要加入 input 组
sudo usermod -a -G input $USER
# 重新登录后生效
```

**macOS:**
1. 打开"系统偏好设置" → "安全性与隐私" → "辅助功能"
2. 添加并启用你的终端应用
3. 对于截图功能，还需要在"屏幕录制"中添加终端

### 问题：MCP 连接失败

1. 检查可执行文件路径：
```bash
which et
# Windows: where et
```

2. 测试直接运行：
```bash
et --version
```

3. 检查 MCP 配置路径是否正确
4. 查看 Claude Desktop 日志：
   - Windows: `%AppData%\Claude\logs\`
   - macOS: `~/Library/Logs/Claude/`
   - Linux: `~/.config/Claude/logs/`

### 问题：截图失败

**Windows:**
- 确保有足够的磁盘空间
- 检查输出目录是否有写入权限

**Linux:**
- 官方验证环境为 Ubuntu Desktop（22.04/24.04）
- 可手动安装依赖：
  ```bash
  sudo apt install xdotool xclip xsel imagemagick gnome-screenshot
  sudo apt install ydotool wl-clipboard grim   # Wayland 按需
  ```
- 确保 `DISPLAY`（X11）或 Wayland 会话环境变量已设置

**macOS:**
- 在"系统偏好设置" → "安全性与隐私" → "屏幕录制"中授权终端应用

## ✅ 验证清单

安装后请检查：

- [ ] `et --version` 显示版本号
- [ ] `et --help` 显示帮助信息
- [ ] `et mouse_position` 返回坐标
- [ ] `et os_info` 返回系统信息
- [ ] `et screenshot --output test.png` 成功创建截图
- [ ] MCP 配置后 Claude 能调用 EasyTouch 工具

## 📝 完整测试脚本

### Windows (PowerShell)
```powershell
Write-Host "Testing EasyTouch..." -ForegroundColor Green

# 基础测试
et --version
et --help

# 功能测试
et mouse_position
et os_info | ConvertFrom-Json
et screenshot --output "$env:TEMP\test.png"

if (Test-Path "$env:TEMP\test.png") {
    Write-Host "✓ Screenshot test passed" -ForegroundColor Green
    Remove-Item "$env:TEMP\test.png"
} else {
    Write-Host "✗ Screenshot test failed" -ForegroundColor Red
}

Write-Host "Test complete!" -ForegroundColor Green
```

### Linux/macOS (Bash)
```bash
#!/bin/bash
set -e

echo "Testing EasyTouch..."

# 基础测试
et --version
et --help

# 功能测试
et mouse_position
et os_info

# 截图测试
et screenshot --output /tmp/test.png
if [ -f /tmp/test.png ]; then
    echo "✓ Screenshot test passed"
    rm /tmp/test.png
else
    echo "✗ Screenshot test failed"
    exit 1
fi

echo "Test complete!"
```
