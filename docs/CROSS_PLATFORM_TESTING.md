# EasyTouch 跨平台测试指南

## 🧪 测试概览

我们提供了多种测试方案，可以在不同平台下验证 EasyTouch 的功能：

1. **xUnit 单元测试** - 平台特定的 .NET 测试项目
2. **JavaScript 集成测试** - 跨平台的 Node.js 测试脚本
3. **GitHub Actions CI** - 自动化持续集成测试
4. **快速冒烟测试** - 基本的 CLI 命令验证

## 📁 测试文件结构

```
EasyTouch/
├── scripts/
│   ├── test-easytouch.js      # 主要跨平台测试脚本
│   ├── test-easytouch.bat     # Windows 包装器
│   ├── test-easytouch.sh      # Unix 包装器
│   └── run-tests.bat/sh       # xUnit 测试运行器
├── EasyTouch.Tests.Windows/   # Windows 单元测试
├── EasyTouch.Tests.Linux/     # Linux 单元测试
├── EasyTouch.Tests.Mac/       # macOS 单元测试
├── .github/workflows/
│   ├── build.yml              # 构建工作流
│   └── test.yml               # 测试工作流
└── docs/
    ├── NPM_TEST_GUIDE.md      # NPM 包测试
    └── TEST_STRUCTURE.md      # 测试结构说明
```

## 🚀 快速开始

### 方法 1: JavaScript 跨平台测试（推荐）

这个脚本可以在任何安装了 Node.js 的平台上运行：

#### Windows
```cmd
cd scripts
test-easytouch.bat

# 或详细模式
test-easytouch.bat --verbose

# 只测试 CLI
test-easytouch.bat --cli-only

# 保存结果
test-easytouch.bat --output results.json
```

#### Linux/macOS
```bash
cd scripts
chmod +x test-easytouch.sh
./test-easytouch.sh

# 详细模式
./test-easytouch.sh --verbose

# 强制重新编译
./test-easytouch.sh --build

# 只编译不测试
./test-easytouch.sh --build-only

# 只测试 MCP 模式
./test-easytouch.sh --mcp-only

# 保存结果
./test-easytouch.sh --output results.json

# 自动编译（如果未找到）并测试
./test-easytouch.sh
```

#### 自动编译功能

测试脚本现在支持自动编译：

1. **自动检测**：脚本会首先检查以下位置
   - 系统 PATH 中的 `et` 或 `et.exe`
   - npm 全局安装的包
   - 本地构建目录 (`bin/Release/net10.0/...`)

2. **自动编译**：如果没有找到 EasyTouch，脚本会自动：
   - 调用 `dotnet publish` 编译项目
   - 使用正确的运行时标识（win-x64/linux-x64/osx-x64/osx-arm64）
   - 启用 AOT 编译和单文件发布
   - 设置正确的文件权限（Unix 系统）

3. **强制重新编译**：使用 `--build` 参数
   ```bash
   ./test-easytouch.sh --build
   ```

4. **只编译不测试**：使用 `--build-only` 参数
   ```bash
   ./test-easytouch.sh --build-only
   ```

#### 测试内容
- ✅ 版本和帮助信息
- ✅ 鼠标控制（位置、移动、点击、滚轮）
- ✅ 键盘控制（按键、输入文本）
- ✅ 系统信息（OS、CPU、内存、运行时间）
- ✅ 屏幕操作（截图、像素颜色、显示器列表）
- ✅ 窗口管理（Windows 特有）
- ✅ 音频控制（Windows 特有）
- ✅ 剪贴板操作
- ✅ 进程和磁盘列表
- ✅ MCP 模式测试
- ✅ 无效命令处理
- ✅ 浏览器自动化（见下方浏览器测试）

### 方法 2: 浏览器自动化测试

测试浏览器自动化功能（需要 Playwright）：

#### 所有平台
```bash
cd scripts

# 无头模式测试（默认）
./test-browser.sh

# 有头模式测试（可见浏览器窗口）
./test-browser.sh --headed

# 详细输出
./test-browser.sh --verbose
```

#### Windows
```cmd
cd scripts
test-browser.bat

# 有头模式
test-browser.bat --headed
```

#### 浏览器测试内容
1. **启动浏览器** - 启动 Chromium 浏览器
2. **页面导航** - 导航到 example.com
3. **查找元素** - 查找页面元素（如 h1）
4. **获取文本** - 获取元素文本内容
5. **执行脚本** - 执行 JavaScript（如获取 document.title）
6. **截图** - 对页面进行截图
7. **列表浏览器** - 获取活跃的浏览器实例列表
8. **关闭浏览器** - 正确关闭浏览器

**注意**: 浏览器测试需要 Playwright 已安装。如果未安装，可以先运行：
```bash
npx playwright install chromium
```

### 方法 2: xUnit 单元测试

#### Windows
```bash
cd scripts
run-tests.bat

# 或使用 dotnet CLI
dotnet test EasyTouch.Tests.Windows/EasyTouch.Tests.Windows.csproj
```

#### Linux
```bash
cd scripts
chmod +x run-tests.sh
./run-tests.sh

# 或使用 dotnet CLI
dotnet test EasyTouch.Tests.Linux/EasyTouch.Tests.Linux.csproj
```

#### macOS
```bash
cd scripts
chmod +x run-tests.sh
./run-tests.sh

# 或使用 dotnet CLI
dotnet test EasyTouch.Tests.Mac/EasyTouch.Tests.Mac.csproj
```

### 方法 3: 手动测试

如果你已经安装了 EasyTouch，可以直接测试：

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

# 截图（保存到文件）
et screenshot --output test.png

# 剪贴板
et clipboard_set_text --text "Hello"
et clipboard_get_text

# MCP 模式（stdio）
et --mcp
```

## 📊 测试覆盖矩阵

| 功能 | Windows | Linux | macOS | 测试方法 |
|------|---------|-------|-------|---------|
| 鼠标位置 | ✅ | ✅ | ✅ | JS/xUnit |
| 鼠标移动 | ✅ | ✅ | ✅ | JS/xUnit |
| 鼠标点击 | ✅ | ✅ | ✅ | JS/xUnit |
| 鼠标滚轮 | ✅ | ✅ | ✅ | JS/xUnit |
| 按键输入 | ✅ | ✅ | ✅ | JS/xUnit |
| 文本输入 | ✅ | ✅ | ✅ | JS/xUnit |
| 系统信息 | ✅ | ✅ | ✅ | JS/xUnit |
| CPU 信息 | ✅ | ✅ | ✅ | JS/xUnit |
| 内存信息 | ✅ | ✅ | ✅ | JS/xUnit |
| 运行时间 | ❌ | ✅ | ✅ | JS/xUnit |
| 电池信息 | ❌ | ✅ | ✅ | JS/xUnit |
| 截图 | ✅ | ✅ | ✅ | JS/xUnit |
| 像素颜色 | ✅ | ✅ | ✅ | JS/xUnit |
| 显示器列表 | ✅ | ✅ | ✅ | JS/xUnit |
| 窗口管理 | ✅ | ❌ | ❌ | JS/xUnit |
| 音量控制 | ✅ | ❌ | ❌ | JS/xUnit |
| 剪贴板 | ✅ | ✅ | ✅ | JS/xUnit |
| 进程列表 | ✅ | ✅ | ✅ | JS/xUnit |
| 磁盘列表 | ✅ | ✅ | ✅ | JS/xUnit |
| MCP 模式 | ✅ | ✅ | ✅ | JS |

## 🔧 GitHub Actions 自动测试

项目配置了 GitHub Actions，每次推送或 PR 时自动运行测试：

### 触发条件
- 推送到 main/master 分支
- 创建 Pull Request
- 手动触发（workflow_dispatch）

### 测试矩阵
| 平台 | 运行时 | 架构 |
|------|--------|------|
| Windows Server | win-x64 | x64 |
| Ubuntu | linux-x64 | x64 |
| macOS 13 | osx-x64 | Intel |
| macOS Latest | osx-arm64 | Apple Silicon |

### 查看测试结果
1. 打开 GitHub 仓库
2. 点击 "Actions" 标签
3. 选择 "Cross-Platform Tests" 工作流
4. 查看详细的测试报告

## 📝 测试结果示例

### JavaScript 测试输出
```
╔════════════════════════════════════════════════════════════╗
║     EasyTouch Cross-Platform Test Suite                   ║
╚════════════════════════════════════════════════════════════╝

Platform: win32 (x64)
Date: 2024-01-20T10:30:00.000Z

✓ Found EasyTouch: C:\Users\...\et.exe

Version: 1.0.0

Running 27 tests...

======================================================================
  1/27 版本检查                 ... ✓ PASS (45ms)
  2/27 帮助信息                 ... ✓ PASS (32ms)
  3/27 鼠标位置                 ... ✓ PASS (28ms)
  4/27 鼠标移动                 ... ✓ PASS (31ms)
  ...
 27/27 无效命令                ... ✓ PASS (12ms)
======================================================================

📊 Test Summary
----------------------------------------------------------------------
Total:   27
Passed:  27 ✓
Failed:  0 ✗
Skipped: 0 ⊘
----------------------------------------------------------------------
Pass Rate: 100.0%

🔌 MCP Mode Tests
======================================================================
✓ MCP Test: Server responds correctly
```

## 🐛 故障排除

### 测试找不到 EasyTouch

```bash
# 检查是否已安装
which et        # Linux/macOS
where et        # Windows

# 如果未安装，可以：
# 1. 使用 npm 安装
npm install -g easytouch

# 2. 或从源码构建
dotnet publish EasyTouch-Windows -c Release -r win-x64 --self-contained
```

### Linux 测试失败

常见原因：
1. **环境不在官方验证范围**: 当前 Linux 仅以 Ubuntu Desktop（22.04/24.04）为验证基线，其他发行版/桌面环境为 best-effort。

2. **缺少自动化依赖库**（Ubuntu）：
   ```bash
   # 基础依赖（推荐）
   sudo apt install xdotool xclip xsel imagemagick gnome-screenshot

   # Wayland 补充依赖（按需）
   sudo apt install ydotool wl-clipboard grim
   ```

3. **无图形显示或 DISPLAY 不可用**（CI/远程会话）：
   ```bash
   sudo apt install xvfb
   export DISPLAY=:99
   Xvfb :99 -screen 0 1920x1080x24 &
   ```

4. **权限问题**: 某些功能需要加入 `input` 组
   ```bash
   sudo usermod -a -G input $USER
   # 重新登录
   ```


### macOS 测试失败

1. **权限问题**: 需要在系统偏好设置中授权
   - 系统偏好设置 → 安全性与隐私 → 辅助功能 → 添加终端
   - 屏幕录制权限（用于截图）

2. **Apple Silicon 兼容性**: 确保使用正确的架构版本
   ```bash
   # 检查架构
   uname -m  # arm64 或 x86_64
   ```

### Windows 测试失败

1. **管理员权限**: 某些功能（如窗口操作）需要管理员权限
   - 右键点击终端选择"以管理员身份运行"

2. **杀毒软件**: 可能拦截自动化操作，添加白名单

## 📈 性能基准

各平台测试执行时间参考（在 GitHub Actions 上）：

| 平台 | xUnit 测试 | JS 集成测试 | 总时间 |
|------|-----------|------------|--------|
| Windows | ~15s | ~25s | ~40s |
| Linux | ~12s | ~20s | ~32s |
| macOS Intel | ~18s | ~30s | ~48s |
| macOS ARM | ~10s | ~18s | ~28s |

## 🔄 添加新测试

### 在 JavaScript 测试中添加

编辑 `scripts/test-easytouch.js`：

```javascript
const TEST_CASES = {
  common: [
    // ... 现有测试
    { 
      name: '新功能测试', 
      args: ['new_command', '--param', 'value'], 
      expectSuccess: true,
      checkKeys: ['ExpectedKey']  // 可选：检查输出包含特定键
    },
  ]
};
```

### 在 xUnit 测试中添加

在对应平台的测试项目中添加：

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

## 📞 获取帮助

如果测试持续失败：

1. 查看详细输出：`--verbose` 选项
2. 保存测试结果：`--output results.json`
3. 检查 GitHub Actions 日志
4. 提交 Issue 并附上测试结果
