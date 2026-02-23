# EasyTouch Skills

EasyTouch 是一个跨平台的桌面自动化工具，提供鼠标、键盘、屏幕、窗口、系统信息、剪贴板、音频控制以及浏览器自动化等功能。

## 安装方式

### 方式一：通过 NPM 安装（推荐）

```bash
# 全局安装
npm install -g easytouch

# 或者本地安装
npm install easytouch
```

安装后可直接使用：
```bash
et --version
et mouse_move --x 100 --y 200
```

### 方式二：手动下载

从 [GitHub Releases](https://github.com/yourusername/easytouch/releases) 下载对应平台的可执行文件：

- **Windows**: `et-windows-x64.exe`
- **Linux**: `et-linux-x64`
- **macOS**: `et-macos-x64`

下载后添加到系统 PATH 即可使用。

### 方式三：从源码构建

```bash
# 克隆仓库
git clone https://github.com/yourusername/easytouch.git
cd easytouch

# Windows
dotnet build EasyTouch-Windows/EasyTouch-Windows.csproj -c Release

# Linux
dotnet build EasyTouch-Linux/EasyTouch-Linux.csproj -c Release

# macOS
dotnet build EasyTouch-Mac/EasyTouch-Mac.csproj -c Release
```

## 核心功能

### 🖱️ 鼠标控制
- 移动、点击、滚动
- 获取当前位置
- 拖拽操作

### ⌨️ 键盘控制
- 按键、组合键
- 输入文本
- 模拟人工打字

### 📸 屏幕操作
- 截图
- 获取像素颜色
- 多显示器支持

### 🪟 窗口管理
- 列出、查找窗口
- 激活窗口
- 获取窗口信息

### 🖥️ 系统信息
- 操作系统信息
- CPU、内存、磁盘
- 进程列表

### 📋 剪贴板
- 文本读写
- 文件操作

### 🔊 音频控制
- 音量调节
- 静音控制
- 音频设备列表

### 🌐 浏览器自动化（可选）
需要额外安装 Playwright：

```bash
npm install -g playwright
npx playwright install chromium
```

功能包括：
- 启动/关闭浏览器
- 页面导航、点击、输入
- 截图、执行 JavaScript
- Cookie 管理
- 网络请求拦截

详见 [浏览器设置指南](BROWSER_SETUP.md)

## 使用 MCP 协议

EasyTouch 支持 Model Context Protocol (MCP)，可以通过 stdio 与 AI 助手集成：

```bash
et mcp stdio
```

AI 助手可以通过 MCP 调用所有功能。

## 文档

- [测试指南](../docs/MCP_TEST_GUIDE.md) - 详细的测试用例
- [浏览器设置](BROWSER_SETUP.md) - 浏览器自动化配置
- [API 文档](../docs/API.md) - 完整的 API 参考

## 跨平台支持

| 功能 | Windows | Linux | macOS |
|------|---------|-------|-------|
| 鼠标控制 | ✅ | ✅ | ✅ |
| 键盘控制 | ✅ | ✅ | ✅ |
| 截图 | ✅ | ✅ | ✅ |
| 窗口管理 | ✅ | ✅ | ✅ |
| 剪贴板 | ✅ | ✅ | ✅ |
| 音频控制 | ✅ | ✅ | ✅ |
| 浏览器自动化 | ✅ | ✅ | ✅ |

## 许可证

MIT License - 详见 [LICENSE](../LICENSE.txt)

## 贡献

欢迎提交 Issue 和 Pull Request！

## 相关链接

- [GitHub 仓库](https://github.com/yourusername/easytouch)
- [NPM 包](https://www.npmjs.com/package/easytouch)
- [问题反馈](https://github.com/yourusername/easytouch/issues)
