# EasyTouch 浏览器自动化设置指南

EasyTouch 支持通过 Playwright 进行浏览器自动化操作。

## 安装步骤

### 1. 安装 Node.js

首先确保已安装 Node.js（建议版本 16+）：

```bash
# 检查是否已安装
node --version

# 如未安装，从 https://nodejs.org/ 下载安装
```

### 2. 安装 Playwright

全局安装 Playwright：

```bash
npm install -g playwright
```

### 3. 安装浏览器

安装所需的浏览器（推荐安装 Chromium）：

```bash
# 仅安装 Chromium（推荐，体积较小）
npx playwright install chromium

# 或者安装所有浏览器
npx playwright install
```

## 使用方法

安装完成后，EasyTouch 会自动检测 Playwright 并启用浏览器功能。

### CLI 示例

```bash
# 启动浏览器
et browser_launch --browser chromium --headless

# 导航到页面
et browser_navigate --browser-id browser_1 --url "https://example.com"

# 点击元素
et browser_click --browser-id browser_1 --selector "button#submit"

# 输入文本
et browser_fill --browser-id browser_1 --selector "input#username" --value "admin"

# 截图
et browser_screenshot --browser-id browser_1 --output ./page.png

# 关闭浏览器
et browser_close --browser-id browser_1
```

### MCP 示例

通过 MCP 协议调用浏览器功能：

```json
// 启动浏览器
{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "tools/call",
  "params": {
    "name": "browser_launch",
    "arguments": {
      "browserType": "chromium",
      "headless": true
    }
  }
}

// 导航
{
  "jsonrpc": "2.0",
  "id": "2",
  "method": "tools/call",
  "params": {
    "name": "browser_navigate",
    "arguments": {
      "browserId": "browser_1",
      "url": "https://example.com"
    }
  }
}
```

## 故障排除

### 提示 "Playwright is not installed"

1. 确认 Node.js 已安装：`node --version`
2. 确认 Playwright 已安装：`npx playwright --version`
3. 确认浏览器已安装：`npx playwright install chromium`

### 浏览器启动失败

可能是浏览器二进制文件未正确安装，尝试重新安装：

```bash
npx playwright install --force chromium
```

### 权限问题（Linux/macOS）

如果遇到权限错误，尝试：

```bash
# Linux
sudo npx playwright install-deps chromium

# macOS 通常不需要额外权限
```

## 支持的浏览器

- `chromium` - Chromium（推荐，兼容性最好）
- `firefox` - Firefox
- `webkit` - WebKit (Safari)

## 完整功能列表

### 基础操作
- `browser_launch` - 启动浏览器
- `browser_navigate` - 导航到 URL
- `browser_click` - 点击元素
- `browser_fill` - 填充输入框
- `browser_find` - 查找元素
- `browser_get_text` - 获取页面文本
- `browser_screenshot` - 页面截图
- `browser_evaluate` - 执行 JavaScript
- `browser_wait_for` - 等待元素
- `browser_close` - 关闭浏览器
- `browser_list` - 列出浏览器实例

### 导航操作
- `browser_go_back` - 后退
- `browser_go_forward` - 前进
- `browser_reload` - 刷新页面

### 页面操作
- `browser_scroll` - 滚动页面
- `browser_select` - 选择下拉框选项
- `browser_upload` - 上传文件

### Cookie 管理
- `browser_get_cookies` - 获取 Cookies
- `browser_set_cookie` - 设置 Cookie
- `browser_clear_cookies` - 清除所有 Cookies

### 网络拦截
- `browser_add_route` - 添加路由拦截
- `browser_remove_route` - 移除路由拦截

## 高级功能示例

### Cookie 管理

```bash
# 获取所有 Cookies
et browser_get_cookies --browser-id browser_1

# 设置 Cookie
et browser_set_cookie --browser-id browser_1 --name "session" --value "abc123" --domain ".example.com"

# 清除所有 Cookies
et browser_clear_cookies --browser-id browser_1
```

### 网络拦截

```bash
# 拦截所有图片请求
et browser_add_route --browser-id browser_1 --url "**/*.png" --action abort

# 拦截并修改响应
et browser_add_route --browser-id browser_1 --url "**/api/data" --action fulfill --status-code 200 --body '{"mock": true}'

# 移除拦截
et browser_remove_route --browser-id browser_1 --route-id route_xxx
```

### 文件上传

```bash
et browser_upload --browser-id browser_1 --selector "input[type='file']" --files "/path/to/file1.jpg,/path/to/file2.pdf"
```

### 下拉框选择

```bash
# 单选
et browser_select --browser-id browser_1 --selector "select#country" --values "China"

# 多选
et browser_select --browser-id browser_1 --selector "select#hobbies" --values "reading,swimming"
```

## 注意事项

1. 浏览器自动化需要 Node.js 和 Playwright 环境
2. 首次安装浏览器可能需要下载几百 MB 的二进制文件
3. 无头模式（headless）适合服务器环境，有界面模式适合调试
