# EasyTouch MCP 服务器测试文档

本文档用于在 Windows、Linux、macOS 真机上测试 EasyTouch 自动化工具功能。

**测试范围**:
1. **CLI 直接命令测试** - 通过命令行直接调用功能
2. **MCP stdio 服务器测试** - 通过 MCP 协议与服务器通信

请根据测试目标选择对应部分进行测试。

## 前置准备

### 1. 构建对应平台的可执行文件

```bash
# Windows
dotnet build EasyTouch-Windows/EasyTouch-Windows.csproj -c Release

# Linux
dotnet build EasyTouch-Linux/EasyTouch-Linux.csproj -c Release -r linux-x64

# macOS
dotnet build EasyTouch-Mac/EasyTouch-Mac.csproj -c Release -r osx-x64
```

### 2. 进入 MCP stdio 模式

不同平台进入 stdio 模式的命令：

```bash
# Windows (PowerShell/CMD)
.\EasyTouch-Windows\bin\Release\net10.0\win-x64\et.exe mcp stdio

# Linux
./EasyTouch-Linux/bin/Release/net10.0/linux-x64/et mcp stdio

# macOS
./EasyTouch-Mac/bin/Release/net10.0/osx-x64/et mcp stdio
```

---

## 第二部分：CLI 直接命令测试

本节测试通过命令行直接调用功能（不使用 MCP stdio 模式）。这种方式更简单直接，适合快速验证功能。

### CLI 通用说明

**命令格式**:
```bash
et <command> [options]
```

**参数格式**:
- `--key value` 或 `-key value` 设置参数
- `--flag` 或 `-flag` 设置布尔值（存在即为 true）

**示例**:
```bash
# 移动鼠标到 (100, 200)
et mouse_move --x 100 --y 200

# 点击左键双击
et mouse_click --button left --double
```

---

### CLI 测试 1: 帮助命令

**测试目的**: 验证帮助命令能正确显示使用说明

**Windows**:
```powershell
.\EasyTouch-Windows\bin\Release\net10.0\win-x64\et.exe help
```

**Linux**:
```bash
./EasyTouch-Linux/bin/Release/net10.0/linux-x64/et help
```

**macOS**:
```bash
./EasyTouch-Mac/bin/Release/net10.0/osx-x64/et help
```

**验收标准**:
- ✅ 显示使用说明和帮助信息
- ✅ 列出所有可用命令
- ✅ 包含命令语法说明

---

### CLI 测试 2: 鼠标移动 (mouse_move)

**测试目的**: 验证鼠标移动命令

**移动到绝对坐标**:
```bash
# Windows
et.exe mouse_move --x 100 --y 200

# Linux/macOS
./et mouse_move --x 100 --y 200
```

**相对移动**:
```bash
et mouse_move --x 50 --y -30 --relative
```

**带持续时间（平滑移动）**:
```bash
et mouse_move --x 500 --y 300 --duration 500
```

**预期输出**:
```json
{
  "Type": "success",
  "Data": {
    "Message": "Mouse moved to (100, 200)"
  }
}
```

**验收标准**:
- ✅ 命令返回成功
- ✅ 鼠标光标移动到指定位置（肉眼观察）
- ✅ 相对移动时，从当前位置偏移
- ✅ 带 duration 时移动更平滑

---

### CLI 测试 3: 鼠标点击 (mouse_click)

**测试目的**: 验证鼠标点击功能

**左键单击**:
```bash
et mouse_click --button left
```

**右键单击**:
```bash
et mouse_click --button right
```

**左键双击**:
```bash
et mouse_click --button left --double
```

**验收标准**:
- ✅ 命令返回成功
- ✅ 点击在当前鼠标位置执行
- ✅ 双击时能打开文件或选中文字

---

### CLI 测试 4: 鼠标按下和释放 (mouse_down/mouse_up)

**测试目的**: 验证鼠标按住和释放（拖拽场景）

**按住左键**:
```bash
et mouse_down --button left
```

**释放左键**:
```bash
et mouse_up --button left
```

**完整拖拽示例**:
```bash
# 1. 移动鼠标到起始位置
et mouse_move --x 100 --y 100

# 2. 按住左键
et mouse_down --button left

# 3. 移动鼠标到目标位置（拖拽过程）
et mouse_move --x 300 --y 300

# 4. 释放左键
et mouse_up --button left
```

**验收标准**:
- ✅ mouse_down 执行后，鼠标保持按住状态
- ✅ mouse_up 执行后，鼠标释放
- ✅ 能够完成拖拽操作（如拖拽文件到文件夹）

---

### CLI 测试 5: 鼠标滚轮 (mouse_scroll)

**测试目的**: 验证鼠标滚轮功能

**向下滚动**:
```bash
et mouse_scroll --amount 5
```

**向上滚动**:
```bash
et mouse_scroll --amount -5
```

**水平滚动**:
```bash
et mouse_scroll --amount 3 --horizontal
```

**验收标准**:
- ✅ 页面或内容按指定方向和幅度滚动
- ✅ 正数向下/右滚动，负数向上/左滚动

---

### CLI 测试 6: 获取鼠标位置 (mouse_position)

**测试目的**: 验证获取当前鼠标位置

**命令**:
```bash
et mouse_position
```

**预期输出**:
```json
{
  "Type": "success",
  "Data": {
    "X": 123,
    "Y": 456
  }
}
```

**验收标准**:
- ✅ 返回 X 和 Y 坐标
- ✅ 坐标值为整数
- ✅ 坐标与当前鼠标实际位置一致

---

### CLI 测试 7: 键盘按键 (key_press)

**测试目的**: 验证键盘按键功能

**按下单个键**:
```bash
et key_press --key a
```

**按下功能键**:
```bash
et key_press --key enter
et key_press --key space
et key_press --key tab
```

**按下组合键（平台相关）**:
```bash
# 复制 (Ctrl+C)
et key_press --key "ctrl+c"

# 粘贴 (Ctrl+V)
et key_press --key "ctrl+v"

# 全选 (Ctrl+A)
et key_press --key "ctrl+a"
```

**验收标准**:
- ✅ 命令返回成功
- ✅ 焦点窗口接收到对应按键
- ✅ 在文本编辑器中能看到字符输入

---

### CLI 测试 8: 按住和释放键 (key_down/key_up)

**测试目的**: 验证键盘按住和释放（组合键场景）

**按住 Shift**:
```bash
et key_down --key shift
```

**释放 Shift**:
```bash
et key_up --key shift
```

**完整组合键示例**:
```bash
# 输入大写字母 A
et key_down --key shift
et key_press --key a
et key_up --key shift
```

**验收标准**:
- ✅ key_down 后，修饰键保持按下状态
- ✅ key_up 后，修饰键释放
- ✅ 能够完成组合键操作

---

### CLI 测试 9: 输入文本 (type_text)

**测试目的**: 验证文本输入功能

**基本输入**:
```bash
et type_text --text "Hello World"
```

**带间隔时间（模拟人工输入）**:
```bash
et type_text --text "Hello World" --interval 50
```

**模拟人类打字**:
```bash
et type_text --text "Hello World" --human
```

**验收标准**:
- ✅ 完整输入指定文本
- ✅ 带 interval 时，每个字符之间有延迟
- ✅ --human 时，模拟真实人类打字节奏

---

### CLI 测试 10: 截图 (screenshot)

**测试目的**: 验证截图功能

**全屏截图**:
```bash
et screenshot
```

**指定区域截图**:
```bash
et screenshot --x 100 --y 100 --width 800 --height 600
```

**指定输出路径**:
```bash
et screenshot --outputPath /tmp/myscreenshot.png
```

**预期输出**:
```json
{
  "Type": "success",
  "Data": {
    "ImagePath": "/tmp/screenshot_1234567890.png",
    "Width": 1920,
    "Height": 1080
  }
}
```

**验收标准**:
- ✅ 返回截图文件路径
- ✅ 截图文件存在且可打开
- ✅ 图片内容正确（全屏或指定区域）
- ✅ 默认保存到临时目录

---

### CLI 测试 11: 获取像素颜色 (pixel_color)

**测试目的**: 验证获取指定位置像素颜色

**命令**:
```bash
et pixel_color --x 100 --y 100
```

**预期输出**:
```json
{
  "Type": "success",
  "Data": {
    "Color": "#FF5733",
    "R": 255,
    "G": 87,
    "B": 51
  }
}
```

**验收标准**:
- ✅ 返回十六进制颜色值
- ✅ 返回 RGB 分量
- ✅ 颜色值与屏幕实际颜色一致

---

### CLI 测试 12: 获取屏幕列表 (screen_list)

**测试目的**: 验证多显示器支持

**命令**:
```bash
et screen_list
```

**预期输出**:
```json
{
  "Type": "success",
  "Data": [
    {
      "Index": 0,
      "Width": 1920,
      "Height": 1080,
      "X": 0,
      "Y": 0,
      "Primary": true
    },
    {
      "Index": 1,
      "Width": 1920,
      "Height": 1080,
      "X": 1920,
      "Y": 0,
      "Primary": false
    }
  ]
}
```

**验收标准**:
- ✅ 列出所有连接的显示器
- ✅ 每个显示器包含分辨率、位置、是否主屏
- ✅ 多显示器环境下显示多个条目

---

### CLI 测试 13: 窗口列表 (window_list)

**测试目的**: 验证窗口枚举

**列出所有窗口**:
```bash
et window_list
```

**只列出可见窗口**:
```bash
et window_list --visibleOnly
```

**按标题过滤**:
```bash
et window_list --titleFilter "Chrome"
```

**预期输出**:
```json
{
  "Type": "success",
  "Data": [
    {
      "Handle": 12345,
      "Title": "Document - Word",
      "ClassName": "Win32WindowClass",
      "ProcessId": 6789,
      "Visible": true
    }
  ]
}
```

**验收标准**:
- ✅ 返回窗口列表
- ✅ --visibleOnly 只返回可见窗口
- ✅ --titleFilter 按标题过滤

---

### CLI 测试 14: 查找窗口 (window_find)

**测试目的**: 验证查找特定窗口

**按标题查找**:
```bash
et window_find --title "计算器"
```

**按类名查找**:
```bash
et window_find --className "Chrome_WidgetWin_1"
```

**按进程 ID 查找**:
```bash
et window_find --processId 1234
```

**预期输出**:
```json
{
  "Type": "success",
  "Data": {
    "Handle": 12345,
    "Title": "计算器",
    "Found": true
  }
}
```

**验收标准**:
- ✅ 找到窗口返回窗口信息
- ✅ 未找到返回空或提示
- ✅ 支持多种查找条件

---

### CLI 测试 15: 激活窗口 (window_activate)

**测试目的**: 验证激活指定窗口

**命令**:
```bash
et window_activate --handle 12345
```

**验收标准**:
- ✅ 命令返回成功
- ✅ 指定窗口被激活（前台显示）
- ✅ 窗口标题栏高亮（Windows）

---

### CLI 测试 16: 获取前台窗口 (window_foreground)

**测试目的**: 验证获取当前活动窗口

**命令**:
```bash
et window_foreground
```

**预期输出**:
```json
{
  "Type": "success",
  "Data": {
    "Handle": 12345,
    "Title": "当前活动窗口标题"
  }
}
```

**验收标准**:
- ✅ 返回当前活动窗口信息
- ✅ 与当前实际活动窗口一致

---

### CLI 测试 17: 系统信息

**测试目的**: 验证获取系统信息

**Windows**:
```bash
et system_info
```

**Linux/macOS**:
```bash
et os_info
et cpu_info
et memory_info
```

**预期输出 (os_info)**:
```json
{
  "Type": "success",
  "Data": {
    "Platform": "Linux",
    "Version": "Ubuntu 22.04",
    "Architecture": "x64"
  }
}
```

**验收标准**:
- ✅ 返回操作系统类型和版本
- ✅ Linux/macOS 支持 cpu_info 和 memory_info

---

### CLI 测试 18: 进程列表 (process_list)

**测试目的**: 验证进程枚举

**列出所有进程**:
```bash
et process_list
```

**按名称过滤**:
```bash
et process_list --nameFilter "chrome"
```

**预期输出**:
```json
{
  "Type": "success",
  "Data": [
    {
      "Id": 1234,
      "Name": "chrome",
      "Memory": 123456789
    }
  ]
}
```

**验收标准**:
- ✅ 返回进程列表
- ✅ 包含进程 ID、名称、内存使用
- ✅ 支持按名称过滤

---

### CLI 测试 19: 锁屏 (lock_screen)

**测试目的**: 验证锁屏功能

**命令**:
```bash
et lock_screen
```

**验收标准**:
- ✅ 执行后系统进入锁屏状态
- ✅ 需要重新输入密码解锁

---

### CLI 测试 20: 剪贴板操作

**测试目的**: 验证剪贴板读写

**设置剪贴板文本**:
```bash
et clipboard_set_text --text "Hello from CLI"
```

**获取剪贴板文本**:
```bash
et clipboard_get_text
```

**清空剪贴板**:
```bash
et clipboard_clear
```

**获取剪贴板文件列表**（如果有）:
```bash
et clipboard_get_files
```

**预期输出**:
```json
{
  "Type": "success",
  "Data": {
    "Text": "Hello from CLI"
  }
}
```

**验收标准**:
- ✅ 设置后能获取到相同文本
- ✅ 清空后获取为空或 null
- ✅ 文件列表正确（复制文件后测试）

---

### CLI 测试 21: 音量控制

**测试目的**: 验证音量控制

**获取当前音量**:
```bash
et volume_get
```

**设置音量为 50%**:
```bash
et volume_set --level 50
```

**静音**:
```bash
et volume_mute --muted true
```

**取消静音**:
```bash
et volume_mute --muted false
```

**列出音频设备**:
```bash
et audio_devices
```

**预期输出 (volume_get)**:
```json
{
  "Type": "success",
  "Data": {
    "Level": 50,
    "Muted": false
  }
}
```

**验收标准**:
- ✅ 获取当前音量值（0-100）
- ✅ 设置后系统音量变化
- ✅ 静音/取消静音有效
- ✅ 音频设备列表正确

---

### CLI 测试 22: 磁盘列表 (disk_list)

**测试目的**: 验证磁盘枚举（Linux/macOS）

**命令**:
```bash
et disk_list
```

**预期输出**:
```json
{
  "Type": "success",
  "Data": [
    {
      "Name": "/dev/sda1",
      "MountPoint": "/",
      "Total": 1000000000,
      "Used": 500000000,
      "Free": 500000000
    }
  ]
}
```

**验收标准**:
- ✅ 列出所有磁盘/分区
- ✅ 包含总容量、已用、可用空间

---

## 第三部分：MCP 协议测试

---

## 测试用例

### 测试 1: MCP 初始化握手 (initialize)

**测试目的**: 验证服务器能正确响应 MCP 初始化请求

**测试命令**:
```json
{"jsonrpc":"2.0","id":"init-1","method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test-client","version":"1.0.0"}}}
```

**发送方式**: 将上述 JSON 作为一行文本发送到进程 stdin

**预期输出**:
```json
{"jsonrpc":"2.0","id":"init-1","result":{"protocolVersion":"2024-11-05","capabilities":{"tools":{}},"serverInfo":{"name":"EasyTouch","version":"1.0.0"}}}
```

**验收标准**:
- ✅ 返回的 `jsonrpc` 字段为 `"2.0"`
- ✅ `id` 与请求中的 `id` 一致
- ✅ `result.protocolVersion` 为 `"2024-11-05"`
- ✅ `result.serverInfo.name` 为 `"EasyTouch"`
- ✅ 包含 `capabilities.tools` 字段
- ❌ 不能有 `error` 字段

---

### 测试 2: 工具列表查询 (tools/list)

**测试目的**: 验证服务器能返回完整的工具列表

**前置条件**: 必须先完成初始化握手（某些 MCP 客户端要求）

**测试命令**:
```json
{"jsonrpc":"2.0","id":"list-1","method":"tools/list","params":{}}
```

**预期输出** (部分):
```json
{
  "jsonrpc": "2.0",
  "id": "list-1",
  "result": {
    "tools": [
      {"name":"mouse_move","description":"Move mouse cursor to specified coordinates","inputSchema":{...}},
      {"name":"mouse_click","description":"Click mouse button","inputSchema":{...}},
      {"name":"mouse_position","description":"Get current mouse position","inputSchema":{...}},
      {"name":"key_press","description":"Press a key","inputSchema":{...}},
      {"name":"type_text","description":"Type text","inputSchema":{...}},
      {"name":"screenshot","description":"Take a screenshot","inputSchema":{...}},
      {"name":"window_list","description":"List all windows","inputSchema":{...}},
      {"name":"volume_get","description":"Get current volume level","inputSchema":{...}},
      ...
    ]
  }
}
```

**验收标准**:
- ✅ 返回 `tools` 数组
- ✅ 包含至少 30 个工具
- ✅ 每个工具包含 `name`、`description`、`inputSchema` 字段
- ✅ 工具名称符合规范（小写下划线格式）
- ✅ `inputSchema` 使用 JSON Schema 格式
- ✅ 常见工具必须包含：mouse_move、mouse_click、key_press、screenshot、window_list

**各平台工具数量差异**:
- Windows: 约 16-20 个工具
- Linux: 约 33 个工具（包括 mouse_down/up、key_down/up、screen_list 等）
- macOS: 约 33 个工具

---

### 测试 3: 鼠标移动 (mouse_move)

**测试目的**: 验证鼠标控制功能

**测试命令**:
```json
{"jsonrpc":"2.0","id":"mouse-1","method":"tools/call","params":{"name":"mouse_move","arguments":{"x":100,"y":200}}}
```

**预期输出**:
```json
{"jsonrpc":"2.0","id":"mouse-1","result":{"content":[{"type":"text","text":"{...}"}],"isError":false}}
```

**验收标准**:
- ✅ 返回 `isError: false`
- ✅ 执行后鼠标光标移动到屏幕坐标 (100, 200)
- ✅ 返回的 content 中包含成功信息

**手动验证**:
- 测试前记录鼠标位置
- 发送命令后，观察鼠标是否移动
- Windows/Linux 可用命令验证位置

---

### 测试 4: 鼠标点击 (mouse_click)

**测试目的**: 验证鼠标点击功能

**测试命令**:
```json
{"jsonrpc":"2.0","id":"click-1","method":"tools/call","params":{"name":"mouse_click","arguments":{"button":"left"}}}
```

**预期输出**:
```json
{"jsonrpc":"2.0","id":"click-1","result":{"content":[{"type":"text","text":"{...}"}],"isError":false}}
```

**验收标准**:
- ✅ 返回 `isError: false`
- ✅ 当前位置的 UI 元素接收到点击事件

**手动验证**:
- 将鼠标移到可点击元素（如按钮）上
- 发送点击命令
- 观察按钮是否被点击

---

### 测试 5: 获取鼠标位置 (mouse_position)

**测试目的**: 验证获取鼠标位置功能

**测试命令**:
```json
{"jsonrpc":"2.0","id":"pos-1","method":"tools/call","params":{"name":"mouse_position","arguments":{}}}
```

**预期输出**:
```json
{"jsonrpc":"2.0","id":"pos-1","result":{"content":[{"type":"text","text":"{\"X\":123,\"Y\":456}"}],"isError":false}}
```

**验收标准**:
- ✅ 返回 `isError: false`
- ✅ content[0].text 中包含 X 和 Y 坐标
- ✅ 坐标值为整数
- ✅ 坐标值在合理范围内（根据屏幕分辨率）

---

### 测试 6: 键盘按键 (key_press)

**测试目的**: 验证键盘按键功能

**测试命令**:
```json
{"jsonrpc":"2.0","id":"key-1","method":"tools/call","params":{"name":"key_press","arguments":{"key":"a"}}}
```

**预期输出**:
```json
{"jsonrpc":"2.0","id":"key-1","result":{"content":[{"type":"text","text":"{...}"}],"isError":false}}
```

**验收标准**:
- ✅ 返回 `isError: false`
- ✅ 焦点窗口接收到 'a' 按键

**手动验证**:
- 打开一个文本编辑器
- 发送 key_press 命令
- 观察是否输入了字符 'a'

---

### 测试 7: 输入文本 (type_text)

**测试目的**: 验证文本输入功能

**测试命令**:
```json
{"jsonrpc":"2.0","id":"type-1","method":"tools/call","params":{"name":"type_text","arguments":{"text":"Hello MCP"}}}
```

**预期输出**:
```json
{"jsonrpc":"2.0","id":"type-1","result":{"content":[{"type":"text","text":"{...}"}],"isError":false}}
```

**验收标准**:
- ✅ 返回 `isError: false`
- ✅ 焦点窗口输入了 "Hello MCP"

---

### 测试 8: 截图 (screenshot)

**测试目的**: 验证截图功能

**测试命令**:
```json
{"jsonrpc":"2.0","id":"screenshot-1","method":"tools/call","params":{"name":"screenshot","arguments":{}}}
```

**预期输出**:
```json
{"jsonrpc":"2.0","id":"screenshot-1","result":{"content":[{"type":"text","text":"{\"ImagePath\":\"/path/to/screenshot.png\",\"Width\":1920,\"Height\":1080}"}],"isError":false}}
```

**验收标准**:
- ✅ 返回 `isError: false`
- ✅ 返回的 content 中包含图片路径
- ✅ 截图文件存在且可读
- ✅ 图片尺寸与屏幕分辨率匹配

**手动验证**:
- 检查返回的图片路径
- 打开图片查看是否为当前屏幕截图

---

### 测试 9: 获取窗口列表 (window_list)

**测试目的**: 验证窗口枚举功能

**测试命令**:
```json
{"jsonrpc":"2.0","id":"win-1","method":"tools/call","params":{"name":"window_list","arguments":{"visibleOnly":true}}}
```

**预期输出**:
```json
{"jsonrpc":"2.0","id":"win-1","result":{"content":[{"type":"text","text":"[{\"Handle\":12345,\"Title\":\"Window Title\",...}]"}],"isError":false}}
```

**验收标准**:
- ✅ 返回 `isError: false`
- ✅ 返回窗口列表数组
- ✅ 每个窗口包含基本属性（Handle、Title 等）
- ✅ 包含当前可见的窗口

**手动验证**:
- 确认输出的窗口列表与当前打开的窗口一致

---

### 测试 10: 系统信息 (system_info 或 os_info)

**测试目的**: 验证系统信息获取

**Windows 命令**:
```json
{"jsonrpc":"2.0","id":"sys-1","method":"tools/call","params":{"name":"system_info","arguments":{}}}
```

**Linux/macOS 命令**:
```json
{"jsonrpc":"2.0","id":"sys-1","method":"tools/call","params":{"name":"os_info","arguments":{}}}
```

**预期输出**:
```json
{"jsonrpc":"2.0","id":"sys-1","result":{"content":[{"type":"text","text":"{\"Platform\":\"Windows\",\"Version\":\"10.0.19045\",...}"}],"isError":false}}
```

**验收标准**:
- ✅ 返回 `isError: false`
- ✅ 返回操作系统类型（Windows/Linux/macOS）
- ✅ 返回操作系统版本信息

---

### 测试 11: 剪贴板操作

**测试目的**: 验证剪贴板读写

**设置剪贴板**:
```json
{"jsonrpc":"2.0","id":"clip-set","method":"tools/call","params":{"name":"clipboard_set_text","arguments":{"text":"Test from MCP"}}}
```

**获取剪贴板**:
```json
{"jsonrpc":"2.0","id":"clip-get","method":"tools/call","params":{"name":"clipboard_get_text","arguments":{}}}
```

**验收标准**:
- ✅ `clipboard_set_text` 返回成功
- ✅ `clipboard_get_text` 返回刚才设置的文本 "Test from MCP"

---

### 测试 12: 音量控制（如支持）

**测试目的**: 验证音量控制功能

**获取音量**:
```json
{"jsonrpc":"2.0","id":"vol-1","method":"tools/call","params":{"name":"volume_get","arguments":{}}}
```

**设置音量为 50%**:
```json
{"jsonrpc":"2.0","id":"vol-2","method":"tools/call","params":{"name":"volume_set","arguments":{"level":50}}}
```

**验收标准**:
- ✅ `volume_get` 返回当前音量值（0-100）
- ✅ `volume_set` 执行后系统音量变为 50%

---

### 测试 13: 错误处理

**测试目的**: 验证错误处理机制

**测试命令** (调用不存在的工具):
```json
{"jsonrpc":"2.0","id":"error-1","method":"tools/call","params":{"name":"nonexistent_tool","arguments":{}}}
```

**预期输出**:
```json
{"jsonrpc":"2.0","id":"error-1","result":{"content":[{"type":"text","text":"Unknown tool: nonexistent_tool"}],"isError":true}}
```

**验收标准**:
- ✅ 返回 `isError: true`
- ✅ content 中包含错误描述
- ✅ 服务器不崩溃，继续响应后续请求

---

### 测试 14: JSON-RPC 错误响应

**测试目的**: 验证 JSON-RPC 协议错误处理

**测试命令** (无效的方法名):
```json
{"jsonrpc":"2.0","id":"err-1","method":"invalid/method","params":{}}
```

**预期输出**:
```json
{"jsonrpc":"2.0","id":"err-1","error":{"code":-32601,"message":"Method not found: invalid/method"}}
```

**验收标准**:
- ✅ 返回包含 `error` 字段的响应
- ✅ `error.code` 符合 JSON-RPC 规范
- ✅ 错误消息描述清晰

---

## 自动化测试脚本

### Python 测试脚本示例

```python
import subprocess
import json
import sys

def test_mcp_server(executable_path):
    """测试 MCP 服务器"""
    
    # 启动进程
    proc = subprocess.Popen(
        [executable_path, "mcp", "stdio"],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True
    )
    
    def send_request(req):
        """发送请求并读取响应"""
        line = json.dumps(req)
        proc.stdin.write(line + "\n")
        proc.stdin.flush()
        
        response_line = proc.stdout.readline()
        return json.loads(response_line)
    
    try:
        # 测试 1: 初始化
        init_req = {
            "jsonrpc": "2.0",
            "id": "init-1",
            "method": "initialize",
            "params": {
                "protocolVersion": "2024-11-05",
                "capabilities": {},
                "clientInfo": {"name": "test-client", "version": "1.0.0"}
            }
        }
        resp = send_request(init_req)
        assert "result" in resp, f"初始化失败: {resp}"
        assert resp["result"]["serverInfo"]["name"] == "EasyTouch"
        print("✅ 测试 1: 初始化通过")
        
        # 测试 2: 工具列表
        list_req = {
            "jsonrpc": "2.0",
            "id": "list-1",
            "method": "tools/list",
            "params": {}
        }
        resp = send_request(list_req)
        tools = resp["result"]["tools"]
        assert len(tools) > 0, "工具列表为空"
        print(f"✅ 测试 2: 工具列表通过 (共 {len(tools)} 个工具)")
        
        # 测试 3: 获取鼠标位置
        pos_req = {
            "jsonrpc": "2.0",
            "id": "pos-1",
            "method": "tools/call",
            "params": {
                "name": "mouse_position",
                "arguments": {}
            }
        }
        resp = send_request(pos_req)
        assert resp["result"]["isError"] == False
        print("✅ 测试 3: 鼠标位置获取通过")
        
        print("\n所有测试通过!")
        
    finally:
        proc.terminate()
        proc.wait()

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("用法: python test_mcp.py <path_to_executable>")
        sys.exit(1)
    
    test_mcp_server(sys.argv[1])
```

---

## 各平台差异说明

### Windows 特有
- 使用 `EasyTouch-Windows` 项目
- 可执行文件: `et.exe`
- 窗口句柄使用整数表示
- 支持 `system_info` 工具

### Linux 特有
- 使用 `EasyTouch-Linux` 项目
- 可执行文件: `et`
- 依赖外部工具: xdotool, xclip/xsel, wmctrl
- 支持更多工具: `mouse_down`, `mouse_up`, `key_down`, `key_up`, `screen_list`
- 使用 `os_info` 代替 `system_info`

### macOS 特有
- 使用 `EasyTouch-Mac` 项目
- 可执行文件: `et`
- 使用 AppleScript 和 Cocoa API
- 支持工具与 Linux 类似
- 使用 `os_info` 代替 `system_info`

---

## 故障排除

### 问题 1: 进程启动后立即退出
**检查**:
- 可执行文件路径是否正确
- 是否有执行权限（Linux/macOS: `chmod +x et`）
- 依赖库是否齐全

### 问题 2: 无响应或超时
**检查**:
- 是否正确发送换行符 (`\n`)
- JSON 格式是否正确（可使用 jsonlint 验证）
- 是否忘记调用 `stdin.flush()`

### 问题 3: 工具调用返回错误
**检查**:
- 工具名称是否拼写正确
- 参数是否符合 inputSchema
- 平台是否支持该工具

### 问题 4: 截图失败
**检查**:
- Linux: 是否安装了 grim（Wayland）或 gnome-screenshot
- 是否有写入临时目录的权限

### 问题 5: 浏览器操作失败
**检查**:
- 是否是首次运行（首次会自动下载浏览器内核，网络较慢时可能超时）
- 是否有可写临时目录权限
- Edge 模式下本机是否已安装 Microsoft Edge（`--browser edge`）

---

## 浏览器自动化测试（基于 Microsoft.Playwright）

**前置条件**：
```bash
# 1. 确认可执行文件可用
et --help

# 2. 首次运行浏览器命令（会自动安装内核）
et browser_launch --browser chromium --headless true
```

---

### 浏览器测试 1: 启动浏览器 (browser_launch)

**测试目的**: 验证浏览器启动功能

**CLI 测试**:
```bash
# Windows
.\et.exe browser_launch --browser chromium --headless

# Linux/macOS
./et browser_launch --browser chromium --headless
```

**MCP 测试**:
```json
{"jsonrpc":"2.0","id":"browser-1","method":"tools/call","params":{"name":"browser_launch","arguments":{"browserType":"chromium","headless":true}}}
```

**预期输出**:
```json
{"jsonrpc":"2.0","id":"browser-1","result":{"content":[{"type":"text","text":"{\"BrowserId\":\"browser_1\",\"BrowserType\":\"chromium\",\"Version\":\"...\"}"}],"isError":false}}
```

**验收标准**:
- ✅ 首次运行后自动完成浏览器内核准备
- ✅ 返回 browserId
- ✅ 返回浏览器版本
- ❌ 浏览器内核下载失败时返回友好提示

---

### 浏览器测试 2: 导航到页面 (browser_navigate)

**测试目的**: 验证页面导航功能

**前置条件**: 已启动浏览器（browser_1）

**CLI 测试**:
```bash
et browser_navigate --browser-id browser_1 --url "https://example.com"
```

**MCP 测试**:
```json
{"jsonrpc":"2.0","id":"nav-1","method":"tools/call","params":{"name":"browser_navigate","arguments":{"browserId":"browser_1","url":"https://example.com"}}}
```

**预期输出**:
```json
{"jsonrpc":"2.0","id":"nav-1","result":{"content":[{"type":"text","text":"{\"Url\":\"https://example.com\",\"Title\":\"Example Domain\",\"StatusCode\":200}"}],"isError":false}}
```

**验收标准**:
- ✅ 返回 `isError: false`
- ✅ 返回正确的 URL
- ✅ 返回页面标题
- ✅ 返回 HTTP 状态码

---

### 浏览器测试 3: 点击元素 (browser_click)

**测试目的**: 验证元素点击功能

**前置条件**: 已导航到包含按钮的页面

**CLI 测试**:
```bash
et browser_click --browser-id browser_1 --selector "button#submit"
```

**MCP 测试**:
```json
{"jsonrpc":"2.0","id":"click-1","method":"tools/call","params":{"name":"browser_click","arguments":{"browserId":"browser_1","selector":"button#submit","selectorType":"css"}}}
```

**验收标准**:
- ✅ 命令返回成功
- ✅ 页面上的按钮被点击（可通过后续操作验证）

---

### 浏览器测试 4: 填充输入框 (browser_fill)

**测试目的**: 验证文本输入功能

**CLI 测试**:
```bash
et browser_fill --browser-id browser_1 --selector "input#username" --value "testuser"
```

**MCP 测试**:
```json
{"jsonrpc":"2.0","id":"fill-1","method":"tools/call","params":{"name":"browser_fill","arguments":{"browserId":"browser_1","selector":"input#username","value":"testuser"}}}
```

**验收标准**:
- ✅ 返回成功
- ✅ 输入框被填充指定文本

---

### 浏览器测试 5: 页面截图 (browser_screenshot)

**测试目的**: 验证浏览器截图功能

**CLI 测试**:
```bash
et browser_screenshot --browser-id browser_1 --output ./page.png --full-page
```

**MCP 测试**:
```json
{"jsonrpc":"2.0","id":"screenshot-1","method":"tools/call","params":{"name":"browser_screenshot","arguments":{"browserId":"browser_1","outputPath":"./page.png","fullPage":true}}}
```

**预期输出**:
```json
{"jsonrpc":"2.0","id":"screenshot-1","result":{"content":[{"type":"text","text":"{\"ImagePath\":\"./page.png\",\"Width\":1920,\"Height\":1080}"}],"isError":false}}
```

**验收标准**:
- ✅ 返回图片路径
- ✅ 图片文件存在且可打开
- ✅ 图片内容为网页截图

---

### 浏览器测试 6: 执行 JavaScript (browser_evaluate)

**测试目的**: 验证 JavaScript 执行功能

**CLI 测试**:
```bash
et browser_evaluate --browser-id browser_1 --script "document.title"
```

**MCP 测试**:
```json
{"jsonrpc":"2.0","id":"eval-1","method":"tools/call","params":{"name":"browser_evaluate","arguments":{"browserId":"browser_1","script":"document.title"}}}
```

**预期输出**:
```json
{"jsonrpc":"2.0","id":"eval-1","result":{"content":[{"type":"text","text":"{\"Result\":\"Example Domain\",\"ResultType\":\"string\"}"}],"isError":false}}
```

**验收标准**:
- ✅ 返回执行结果
- ✅ 结果类型正确

---

### 浏览器测试 7: 等待元素 (browser_wait_for)

**测试目的**: 验证元素等待功能

**CLI 测试**:
```bash
et browser_wait_for --browser-id browser_1 --selector ".loaded-content" --state visible --timeout 10000
```

**MCP 测试**:
```json
{"jsonrpc":"2.0","id":"wait-1","method":"tools/call","params":{"name":"browser_wait_for","arguments":{"browserId":"browser_1","selector":".loaded-content","state":"visible","timeout":10000}}}
```

**验收标准**:
- ✅ 元素出现时立即返回
- ✅ 超时时间内元素未出现则报错

---

### 浏览器测试 8: 获取页面文本 (browser_get_text)

**测试目的**: 验证页面文本获取功能

**CLI 测试**:
```bash
# 获取整个页面文本
et browser_get_text --browser-id browser_1

# 获取特定元素文本
et browser_get_text --browser-id browser_1 --selector "h1"
```

**MCP 测试**:
```json
{"jsonrpc":"2.0","id":"text-1","method":"tools/call","params":{"name":"browser_get_text","arguments":{"browserId":"browser_1","selector":"h1"}}}
```

**验收标准**:
- ✅ 返回页面或元素的文本内容

---

### 浏览器测试 9: 查找元素 (browser_find)

**测试目的**: 验证元素查找功能

**CLI 测试**:
```bash
et browser_find --browser-id browser_1 --selector "#main-content"
```

**MCP 测试**:
```json
{"jsonrpc":"2.0","id":"find-1","method":"tools/call","params":{"name":"browser_find","arguments":{"browserId":"browser_1","selector":"#main-content"}}}
```

**预期输出**:
```json
{"jsonrpc":"2.0","id":"find-1","result":{"content":[{"type":"text","text":"{\"Found\":true,\"TagName\":\"div\",\"Text\":\"...\",\"BoundingBox\":{...}}"}],"isError":false}}
```

**验收标准**:
- ✅ 返回元素是否找到
- ✅ 返回标签名、文本内容
- ✅ 返回元素位置和尺寸

---

### 浏览器测试 10: 关闭浏览器 (browser_close)

**测试目的**: 验证浏览器关闭功能

**CLI 测试**:
```bash
et browser_close --browser-id browser_1
```

**MCP 测试**:
```json
{"jsonrpc":"2.0","id":"close-1","method":"tools/call","params":{"name":"browser_close","arguments":{"browserId":"browser_1"}}}
```

**验收标准**:
- ✅ 浏览器进程被关闭
- ✅ 返回成功信息

---

### 浏览器测试 11: Edge 启动 (browser_launch + edge)

**测试目的**: 验证 Edge 通道启动能力

**CLI 测试**:
```bash
et browser_launch --browser edge --headless false
```

**MCP 测试**:
```json
{"jsonrpc":"2.0","id":"browser-edge-1","method":"tools/call","params":{"name":"browser_launch","arguments":{"browserType":"edge","headless":false}}}
```

**验收标准**:
- ✅ 返回 `BrowserType: edge`
- ✅ 可正常打开并操作页面

---

### 浏览器测试 12: 执行 JS/TS 测试脚本 (browser_run_script)

**测试目的**: 验证本地 Playwright 脚本执行能力（支持 AI 生成测试脚本）

**CLI 测试**:
```bash
# 执行 TS 测试脚本
et browser_run_script --script-path "./tests/example.spec.ts" --browser edge --headless true

# 透传额外参数（逗号分隔）
et browser_run_script --script-path "./tests/example.spec.ts" --browser chromium --extra-args "--reporter=list,--workers=1"
```

**MCP 测试**:
```json
{"jsonrpc":"2.0","id":"run-script-1","method":"tools/call","params":{"name":"browser_run_script","arguments":{"scriptPath":"./tests/example.spec.ts","browserType":"edge","headless":true,"extraArgs":["--reporter=list","--workers=1"]}}}
```

**验收标准**:
- ✅ 返回 `exitCode`
- ✅ `exitCode=0` 时 `success=true`
- ✅ 失败时包含可复现的命令行信息（`command` 字段）

---

## 浏览器自动化完整流程测试

**测试场景**: 自动化登录流程

```bash
#!/bin/bash

# 1. 启动浏览器
BROWSER_ID=$(./et browser_launch --browser chromium --headless | jq -r '.Data.BrowserId')

# 2. 导航到登录页
./et browser_navigate --browser-id $BROWSER_ID --url "https://example.com/login"

# 3. 输入用户名
./et browser_fill --browser-id $BROWSER_ID --selector "input[name='username']" --value "admin"

# 4. 输入密码
./et browser_fill --browser-id $BROWSER_ID --selector "input[name='password']" --value "secret"

# 5. 点击登录按钮
./et browser_click --browser-id $BROWSER_ID --selector "button[type='submit']"

# 6. 等待登录完成
./et browser_wait_for --browser-id $BROWSER_ID --selector ".dashboard" --state "visible"

# 7. 截图验证
./et browser_screenshot --browser-id $BROWSER_ID --output ./logged_in.png

# 8. 关闭浏览器
./et browser_close --browser-id $BROWSER_ID
```

**验收标准**:
- ✅ 整个流程无错误
- ✅ 截图显示已登录状态

---

## 验收清单

### CLI 命令测试清单

- [ ] CLI help 命令显示帮助信息
- [ ] mouse_move 能移动鼠标
- [ ] mouse_click 能点击鼠标
- [ ] mouse_down/up 能按住/释放鼠标
- [ ] mouse_scroll 能滚动页面
- [ ] mouse_position 能获取位置
- [ ] key_press 能发送按键
- [ ] key_down/up 能按住/释放键
- [ ] type_text 能输入文本
- [ ] screenshot 能截图
- [ ] pixel_color 能获取像素颜色
- [ ] screen_list 能列出显示器
- [ ] window_list 能列出窗口
- [ ] window_find 能查找窗口
- [ ] window_activate 能激活窗口
- [ ] window_foreground 能获取活动窗口
- [ ] system/os/cpu/memory_info 能获取系统信息
- [ ] process_list 能列出进程
- [ ] lock_screen 能锁屏
- [ ] clipboard_set_text 能设置剪贴板
- [ ] clipboard_get_text 能获取剪贴板
- [ ] clipboard_clear 能清空剪贴板
- [ ] volume_get/set/mute 能控制音量
- [ ] audio_devices 能列出音频设备
- [ ] disk_list 能列出磁盘（Linux/macOS）

### MCP 协议测试清单

- [ ] 初始化握手成功
- [ ] tools/list 返回工具列表
- [ ] mouse_move 能移动鼠标
- [ ] mouse_click 能点击鼠标
- [ ] mouse_position 能获取位置
- [ ] key_press 能发送按键
- [ ] type_text 能输入文本
- [ ] screenshot 能截图
- [ ] window_list 能列出窗口
- [ ] 剪贴板操作正常
- [ ] 错误处理正确
- [ ] 无效方法返回错误

### 浏览器自动化测试清单（Microsoft.Playwright）

**前置条件**: ☐ `et browser_launch --browser chromium --headless true` 首次执行成功

- [ ] browser_launch 能启动浏览器
- [ ] browser_navigate 能导航到页面
- [ ] browser_click 能点击元素
- [ ] browser_fill 能填充输入框
- [ ] browser_screenshot 能截取页面
- [ ] browser_evaluate 能执行 JavaScript
- [ ] browser_wait_for 能等待元素
- [ ] browser_get_text 能获取页面文本
- [ ] browser_find 能查找元素
- [ ] browser_assert_text 能完成文本断言
- [ ] browser_page_info 能获取页面信息
- [ ] browser_go_back/go_forward/reload 能控制导航
- [ ] browser_scroll 能滚动页面/元素
- [ ] browser_select 能执行下拉选择
- [ ] browser_upload 能上传文件
- [ ] browser_get_cookies/set_cookie/clear_cookies 能管理 Cookie
- [ ] browser_run_script 能执行 JS/TS 脚本
- [ ] browser_close 能关闭浏览器
- [ ] 浏览器内核缺失/下载失败时显示友好提示

**测试人员**: _______________  
**测试日期**: _______________  
**测试平台**: ☐ Windows ☐ Linux ☐ macOS
**浏览器**: ☐ Chromium ☐ Firefox ☐ WebKit ☐ Edge
