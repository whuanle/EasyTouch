namespace EasyTouch.Core.Models;

// 浏览器启动请求
public class BrowserLaunchRequest : Request
{
    public string BrowserType { get; set; } = "chromium"; // chromium, firefox, webkit
    public string? ExecutablePath { get; set; }
    public string? UserDataDir { get; set; }
    public bool Headless { get; set; } = false;
    public Dictionary<string, object>? Args { get; set; }
}

// 浏览器启动响应
public class BrowserLaunchResponse
{
    public string BrowserId { get; set; } = string.Empty;
    public string BrowserType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

// 页面导航请求
public class BrowserNavigateRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? WaitUntil { get; set; } // load, domcontentloaded, networkidle
    public int? Timeout { get; set; }
}

// 页面导航响应
public class BrowserNavigateResponse
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int StatusCode { get; set; }
}

// 元素点击请求
public class BrowserClickRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string? SelectorType { get; set; } = "css"; // css, xpath, text, id
    public int? Timeout { get; set; }
    public int? Button { get; set; } // 0=left, 1=middle, 2=right
    public int? ClickCount { get; set; }
    public int? Delay { get; set; }
}

// 元素输入请求
public class BrowserFillRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string? SelectorType { get; set; } = "css";
    public string Value { get; set; } = string.Empty;
    public bool Clear { get; set; } = true;
    public int? Timeout { get; set; }
}

// 元素查找请求
public class BrowserFindRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string? SelectorType { get; set; } = "css";
    public int? Timeout { get; set; }
}

// 元素查找响应
public class BrowserFindResponse
{
    public bool Found { get; set; }
    public string? TagName { get; set; }
    public string? Text { get; set; }
    public string? Value { get; set; }
    public Dictionary<string, string>? Attributes { get; set; }
    public BoundingBox? BoundingBox { get; set; }
}

// 元素边界框
public class BoundingBox
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

// 页面文本获取请求
public class BrowserGetTextRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public string? Selector { get; set; }
    public string? SelectorType { get; set; } = "css";
}

// 页面文本获取响应
public class BrowserGetTextResponse
{
    public string Text { get; set; } = string.Empty;
    public string? Selector { get; set; }
}

// 页面截图请求
public class BrowserScreenshotRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public string? Selector { get; set; }
    public string? SelectorType { get; set; } = "css";
    public string? OutputPath { get; set; }
    public string Type { get; set; } = "png"; // png, jpeg
    public int? Quality { get; set; } // for jpeg 0-100
    public bool? FullPage { get; set; }
}

// 页面截图响应
public class BrowserScreenshotResponse
{
    public string ImagePath { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
}

// JavaScript 执行请求
public class BrowserEvaluateRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
    public object[]? Args { get; set; }
}

// JavaScript 执行响应
public class BrowserEvaluateResponse
{
    public object? Result { get; set; }
    public string ResultType { get; set; } = "undefined";
}

// 等待元素请求
public class BrowserWaitForRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string? SelectorType { get; set; } = "css";
    public string? State { get; set; } = "visible"; // visible, hidden, attached, detached
    public int? Timeout { get; set; }
}

// 文本断言请求（用于自动化测试）
public class BrowserAssertTextRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public string? Selector { get; set; }
    public string? SelectorType { get; set; } = "css";
    public string ExpectedText { get; set; } = string.Empty;
    public bool ExactMatch { get; set; } = false;
    public bool IgnoreCase { get; set; } = false;
}

// 文本断言响应
public class BrowserAssertTextResponse
{
    public bool Passed { get; set; }
    public string ExpectedText { get; set; } = string.Empty;
    public string ActualText { get; set; } = string.Empty;
}

// 浏览器列表请求
public class BrowserListRequest : Request
{
}

// 浏览器列表响应
public class BrowserListResponse
{
    public List<BrowserInfo> Browsers { get; set; } = new();
}

// 浏览器信息
public class BrowserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string CurrentUrl { get; set; } = string.Empty;
    public string CurrentTitle { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
}

// 浏览器关闭请求
public class BrowserCloseRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public bool Force { get; set; } = false;
}

// 浏览器页面信息请求
public class BrowserGetPageInfoRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
}

// 浏览器页面信息响应
public class BrowserGetPageInfoResponse
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int ScrollX { get; set; }
    public int ScrollY { get; set; }
    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }
    public int PageWidth { get; set; }
    public int PageHeight { get; set; }
}

// 浏览器后退请求
public class BrowserGoBackRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public int? Timeout { get; set; }
}

// 浏览器前进请求
public class BrowserGoForwardRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public int? Timeout { get; set; }
}

// 浏览器刷新请求
public class BrowserReloadRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public int? Timeout { get; set; }
}

// 浏览器滚动请求
public class BrowserScrollRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public int? X { get; set; }
    public int? Y { get; set; }
    public string? Selector { get; set; }
    public string? SelectorType { get; set; } = "css";
    public string? Behavior { get; set; } = "auto"; // auto, smooth
}

// 浏览器选择下拉框请求
public class BrowserSelectRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string? SelectorType { get; set; } = "css";
    public string[] Values { get; set; } = Array.Empty<string>();
}

// 浏览器下载文件请求
public class BrowserDownloadRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? DownloadPath { get; set; }
}

// 浏览器下载响应
public class BrowserDownloadResponse
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
}

// 浏览器上传文件请求
public class BrowserUploadRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string? SelectorType { get; set; } = "css";
    public string[] Files { get; set; } = Array.Empty<string>();
}

// 浏览器获取 Cookie 请求
public class BrowserGetCookiesRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public string? Url { get; set; }
}

// Cookie 信息
public class BrowserCookie
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? Path { get; set; }
    public long? Expires { get; set; }
    public bool HttpOnly { get; set; }
    public bool Secure { get; set; }
    public string? SameSite { get; set; }
}

// 浏览器设置 Cookie 请求
public class BrowserSetCookieRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? Path { get; set; }
    public long? Expires { get; set; }
    public bool HttpOnly { get; set; }
    public bool Secure { get; set; }
    public string? SameSite { get; set; }
}

// 浏览器清除 Cookies 请求
public class BrowserClearCookiesRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
}

// 浏览器拦截请求请求
public class BrowserRouteRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty; // 支持通配符如 **/*.png
    public string Action { get; set; } = "abort"; // abort, continue, fulfill
    public int? StatusCode { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public string? Body { get; set; }
}

// 浏览器拦截响应
public class BrowserRouteResponse
{
    public string RouteId { get; set; } = string.Empty;
}

// 浏览器拦截取消请求
public class BrowserUnrouteRequest : Request
{
    public string BrowserId { get; set; } = string.Empty;
    public string RouteId { get; set; } = string.Empty;
}

// 执行 JS/TS 测试脚本请求（Playwright CLI）
public class BrowserRunScriptRequest : Request
{
    public string ScriptPath { get; set; } = string.Empty;
    public string BrowserType { get; set; } = "chromium"; // chromium, firefox, webkit, edge
    public bool Headless { get; set; } = true;
    public int? Timeout { get; set; } // ms
    public string[]? ExtraArgs { get; set; } // 附加 playwright test 参数
}

// 执行脚本响应
public class BrowserRunScriptResponse
{
    public int ExitCode { get; set; }
    public string Command { get; set; } = string.Empty;
    public bool Success { get; set; }
}
