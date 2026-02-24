using System.Collections.Concurrent;
using System.Text.Json;
using EasyTouch.Core.Models;
using Microsoft.Playwright;

namespace EasyTouch.Modules;

/// <summary>
/// Browser automation via Microsoft.Playwright .NET SDK.
/// </summary>
public static class BrowserModule
{
    private static readonly ConcurrentDictionary<string, BrowserSession> Sessions = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly Lazy<Task<IPlaywright>> PlaywrightLazy = new(() => Microsoft.Playwright.Playwright.CreateAsync());
    private static int _browserCounter = 0;

    public static bool IsAvailable()
    {
        try
        {
            _ = GetPlaywright();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static Response Launch(BrowserLaunchRequest request)
    {
        try
        {
            var browserType = NormalizeBrowserType(request.BrowserType);
            var browserId = $"browser_{Interlocked.Increment(ref _browserCounter)}";
            var playwright = GetPlaywright();
            var type = ResolveBrowserType(playwright, browserType);

            IBrowser? browser = null;
            IBrowserContext? context = null;
            IPage? page = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(request.UserDataDir))
                {
                    context = LaunchPersistentContext(type, request);
                    browser = context.Browser;
                    page = context.Pages.FirstOrDefault() ?? RunSync(() => context.NewPageAsync());
                }
                else
                {
                    browser = LaunchBrowser(type, request);
                    context = RunSync(() => browser.NewContextAsync());
                    page = RunSync(() => context.NewPageAsync());
                }
            }
            catch (PlaywrightException ex) when (TryInstallBrowserAndRetry(ex, browserType))
            {
                if (!string.IsNullOrWhiteSpace(request.UserDataDir))
                {
                    context = LaunchPersistentContext(type, request);
                    browser = context.Browser;
                    page = context.Pages.FirstOrDefault() ?? RunSync(() => context.NewPageAsync());
                }
                else
                {
                    browser = LaunchBrowser(type, request);
                    context = RunSync(() => browser.NewContextAsync());
                    page = RunSync(() => context.NewPageAsync());
                }
            }

            if (context == null || page == null)
            {
                return new ErrorResponse("Failed to create browser session");
            }

            if (TryGetStringArg(request.Args, "url", out var url))
            {
                RunSync(() => page.GotoAsync(url));
            }

            var version = browser?.Version ?? "unknown";
            Sessions[browserId] = new BrowserSession(browserId, browserType, browser, context, page, version);

            return new SuccessResponse<BrowserLaunchResponse>(new BrowserLaunchResponse
            {
                BrowserId = browserId,
                BrowserType = browserType,
                Version = version
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to launch browser: {ex.Message}");
        }
    }

    public static Response Navigate(BrowserNavigateRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            var response = RunSync(() => session.Page.GotoAsync(request.Url, new PageGotoOptions
            {
                WaitUntil = ParseWaitUntil(request.WaitUntil),
                Timeout = request.Timeout ?? 30000
            }));

            var title = RunSync(() => session.Page.TitleAsync());
            return new SuccessResponse<BrowserNavigateResponse>(new BrowserNavigateResponse
            {
                Url = session.Page.Url,
                Title = title,
                StatusCode = response?.Status ?? 0
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Navigation failed: {ex.Message}");
        }
    }

    public static Response Click(BrowserClickRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            var locator = session.Page.Locator(BuildSelector(request.Selector, request.SelectorType)).First;
            RunSync(() => locator.ClickAsync(new LocatorClickOptions
            {
                Timeout = request.Timeout ?? 30000,
                Button = ParseMouseButton(request.Button),
                ClickCount = request.ClickCount ?? 1,
                Delay = request.Delay ?? 0
            }));

            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Click failed: {ex.Message}");
        }
    }

    public static Response Fill(BrowserFillRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            var locator = session.Page.Locator(BuildSelector(request.Selector, request.SelectorType)).First;
            var timeout = request.Timeout ?? 30000;
            if (request.Clear)
            {
                RunSync(() => locator.FillAsync(request.Value, new LocatorFillOptions
                {
                    Timeout = timeout
                }));
            }
            else
            {
                var currentValue = RunSync(() => locator.InputValueAsync()) ?? string.Empty;
                RunSync(() => locator.FillAsync($"{currentValue}{request.Value}", new LocatorFillOptions
                {
                    Timeout = timeout
                }));
            }

            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Fill failed: {ex.Message}");
        }
    }

    public static Response Find(BrowserFindRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            var locator = session.Page.Locator(BuildSelector(request.Selector, request.SelectorType)).First;
            var timeout = request.Timeout ?? 5000;
            var count = RunSync(() => locator.CountAsync());
            if (count == 0)
            {
                return new SuccessResponse<BrowserFindResponse>(new BrowserFindResponse { Found = false });
            }

            RunSync(() => locator.WaitForAsync(new LocatorWaitForOptions
            {
                Timeout = timeout,
                State = WaitForSelectorState.Attached
            }));

            var text = RunSync(() => locator.TextContentAsync()) ?? string.Empty;
            var value = RunSync(() => locator.InputValueAsync());
            var box = RunSync(() => locator.BoundingBoxAsync());
            var tagName = RunSync(() => locator.EvaluateAsync<string>("el => el.tagName.toLowerCase()"));
            var attributes = RunSync(() => locator.EvaluateAsync<Dictionary<string, string>>(
                "el => Object.fromEntries(Array.from(el.attributes).map(a => [a.name, a.value]))"));

            return new SuccessResponse<BrowserFindResponse>(new BrowserFindResponse
            {
                Found = true,
                TagName = tagName,
                Text = text,
                Value = value,
                Attributes = attributes,
                BoundingBox = box == null ? null : new BoundingBox
                {
                    X = box.X,
                    Y = box.Y,
                    Width = box.Width,
                    Height = box.Height
                }
            });
        }
        catch (TimeoutException)
        {
            return new SuccessResponse<BrowserFindResponse>(new BrowserFindResponse { Found = false });
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
        {
            return new SuccessResponse<BrowserFindResponse>(new BrowserFindResponse { Found = false });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Find failed: {ex.Message}");
        }
    }

    public static Response GetText(BrowserGetTextRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            string text;
            if (!string.IsNullOrWhiteSpace(request.Selector))
            {
                var locator = session.Page.Locator(BuildSelector(request.Selector, request.SelectorType)).First;
                text = RunSync(() => locator.TextContentAsync()) ?? string.Empty;
            }
            else
            {
                text = RunSync(() => session.Page.EvaluateAsync<string>("() => document.body?.innerText ?? ''"));
            }

            return new SuccessResponse<BrowserGetTextResponse>(new BrowserGetTextResponse
            {
                Selector = request.Selector,
                Text = text
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"GetText failed: {ex.Message}");
        }
    }

    public static Response Screenshot(BrowserScreenshotRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            var outputPath = request.OutputPath
                ?? Path.Combine(Path.GetTempPath(), $"browser_screenshot_{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.{request.Type}");

            if (!string.IsNullOrWhiteSpace(request.Selector))
            {
                var locator = session.Page.Locator(BuildSelector(request.Selector, request.SelectorType)).First;
                RunSync(() => locator.ScreenshotAsync(new LocatorScreenshotOptions
                {
                    Path = outputPath,
                    Type = ParseScreenshotType(request.Type),
                    Quality = request.Quality
                }));

                var box = RunSync(() => locator.BoundingBoxAsync());
                return new SuccessResponse<BrowserScreenshotResponse>(new BrowserScreenshotResponse
                {
                    ImagePath = outputPath,
                    Width = box == null ? 0 : (int)Math.Round(box.Width),
                    Height = box == null ? 0 : (int)Math.Round(box.Height)
                });
            }

            RunSync(() => session.Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = outputPath,
                Type = ParseScreenshotType(request.Type),
                FullPage = request.FullPage ?? false,
                Quality = request.Quality
            }));

            var viewport = session.Page.ViewportSize;
            return new SuccessResponse<BrowserScreenshotResponse>(new BrowserScreenshotResponse
            {
                ImagePath = outputPath,
                Width = viewport?.Width ?? 0,
                Height = viewport?.Height ?? 0
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Screenshot failed: {ex.Message}");
        }
    }

    public static Response Evaluate(BrowserEvaluateRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            object? arg = null;
            if (request.Args is { Length: 1 })
            {
                arg = request.Args[0];
            }
            else if (request.Args is { Length: > 1 })
            {
                arg = request.Args;
            }

            var result = RunSync(() => session.Page.EvaluateAsync<JsonElement?>(request.Script, arg));
            var resultType = result?.ValueKind.ToString().ToLowerInvariant() ?? "undefined";

            return new SuccessResponse<BrowserEvaluateResponse>(new BrowserEvaluateResponse
            {
                Result = result,
                ResultType = resultType
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Evaluate failed: {ex.Message}");
        }
    }

    public static Response WaitFor(BrowserWaitForRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            var locator = session.Page.Locator(BuildSelector(request.Selector, request.SelectorType)).First;
            RunSync(() => locator.WaitForAsync(new LocatorWaitForOptions
            {
                Timeout = request.Timeout ?? 30000,
                State = ParseWaitForState(request.State)
            }));

            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"WaitFor failed: {ex.Message}");
        }
    }

    public static Response AssertText(BrowserAssertTextRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            var actual = string.Empty;
            if (!string.IsNullOrWhiteSpace(request.Selector))
            {
                var locator = session.Page.Locator(BuildSelector(request.Selector, request.SelectorType)).First;
                actual = RunSync(() => locator.TextContentAsync()) ?? string.Empty;
            }
            else
            {
                actual = RunSync(() => session.Page.EvaluateAsync<string>("() => document.body?.innerText ?? ''"));
            }

            var expected = request.ExpectedText ?? string.Empty;
            var comparison = request.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var passed = request.ExactMatch
                ? string.Equals(actual.Trim(), expected.Trim(), comparison)
                : actual.Contains(expected, comparison);

            return new SuccessResponse<BrowserAssertTextResponse>(new BrowserAssertTextResponse
            {
                Passed = passed,
                ExpectedText = expected,
                ActualText = actual
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"AssertText failed: {ex.Message}");
        }
    }

    public static Response GetPageInfo(BrowserGetPageInfoRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            var title = RunSync(() => session.Page.TitleAsync());
            var metrics = RunSync(() => session.Page.EvaluateAsync<PageMetrics>(
                "() => ({ scrollX: window.scrollX, scrollY: window.scrollY, viewportWidth: window.innerWidth, viewportHeight: window.innerHeight, pageWidth: document.documentElement.scrollWidth, pageHeight: document.documentElement.scrollHeight })"));

            return new SuccessResponse<BrowserGetPageInfoResponse>(new BrowserGetPageInfoResponse
            {
                Url = session.Page.Url,
                Title = title,
                ScrollX = metrics.ScrollX,
                ScrollY = metrics.ScrollY,
                ViewportWidth = metrics.ViewportWidth,
                ViewportHeight = metrics.ViewportHeight,
                PageWidth = metrics.PageWidth,
                PageHeight = metrics.PageHeight
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"GetPageInfo failed: {ex.Message}");
        }
    }

    public static Response GoBack(BrowserGoBackRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            RunSync(() => session.Page.GoBackAsync(new PageGoBackOptions
            {
                Timeout = request.Timeout ?? 30000,
                WaitUntil = WaitUntilState.Load
            }));
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"GoBack failed: {ex.Message}");
        }
    }

    public static Response GoForward(BrowserGoForwardRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            RunSync(() => session.Page.GoForwardAsync(new PageGoForwardOptions
            {
                Timeout = request.Timeout ?? 30000,
                WaitUntil = WaitUntilState.Load
            }));
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"GoForward failed: {ex.Message}");
        }
    }

    public static Response Reload(BrowserReloadRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            RunSync(() => session.Page.ReloadAsync(new PageReloadOptions
            {
                Timeout = request.Timeout ?? 30000,
                WaitUntil = WaitUntilState.Load
            }));
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Reload failed: {ex.Message}");
        }
    }

    public static Response Scroll(BrowserScrollRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            var x = request.X ?? 0;
            var y = request.Y ?? 0;
            if (!string.IsNullOrWhiteSpace(request.Selector))
            {
                var locator = session.Page.Locator(BuildSelector(request.Selector, request.SelectorType)).First;
                RunSync(() => locator.EvaluateAsync("([dx, dy]) => this.scrollBy(dx, dy)", new object[] { x, y }));
            }
            else
            {
                RunSync(() => session.Page.EvaluateAsync("([dx, dy]) => window.scrollBy(dx, dy)", new object[] { x, y }));
            }
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Scroll failed: {ex.Message}");
        }
    }

    public static Response Select(BrowserSelectRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            var locator = session.Page.Locator(BuildSelector(request.Selector, request.SelectorType)).First;
            var values = request.Values.Select(v => new SelectOptionValue { Value = v }).ToArray();
            RunSync(() => locator.SelectOptionAsync(values));
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Select failed: {ex.Message}");
        }
    }

    public static Response Upload(BrowserUploadRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            var locator = session.Page.Locator(BuildSelector(request.Selector, request.SelectorType)).First;
            RunSync(() => locator.SetInputFilesAsync(request.Files));
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Upload failed: {ex.Message}");
        }
    }

    public static Response GetCookies(BrowserGetCookiesRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            var urls = string.IsNullOrWhiteSpace(request.Url) ? null : new[] { request.Url };
            var cookies = RunSync(() => session.Context.CookiesAsync(urls));
            var result = cookies.Select(c => new BrowserCookie
            {
                Name = c.Name,
                Value = c.Value,
                Domain = c.Domain,
                Path = c.Path,
                Expires = c.Expires == -1 ? null : (long?)c.Expires,
                HttpOnly = c.HttpOnly,
                Secure = c.Secure,
                SameSite = c.SameSite.ToString()
            }).ToArray();
            return new SuccessResponse<object>(new { Cookies = result });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"GetCookies failed: {ex.Message}");
        }
    }

    public static Response SetCookie(BrowserSetCookieRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            RunSync(() => session.Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Name = request.Name,
                    Value = request.Value,
                    Domain = request.Domain,
                    Path = request.Path,
                    Expires = request.Expires,
                    HttpOnly = request.HttpOnly,
                    Secure = request.Secure,
                    SameSite = ParseSameSite(request.SameSite)
                }
            }));
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"SetCookie failed: {ex.Message}");
        }
    }

    public static Response ClearCookies(BrowserClearCookiesRequest request)
    {
        if (!TryGetSession(request.BrowserId, out var session, out var error))
        {
            return new ErrorResponse(error);
        }

        try
        {
            RunSync(() => session.Context.ClearCookiesAsync());
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"ClearCookies failed: {ex.Message}");
        }
    }

    public static Response List(BrowserListRequest request)
    {
        var result = new BrowserListResponse();
        foreach (var session in Sessions.Values.OrderBy(s => s.BrowserId))
        {
            var title = string.Empty;
            try
            {
                title = RunSync(() => session.Page.TitleAsync());
            }
            catch
            {
                title = string.Empty;
            }

            result.Browsers.Add(new BrowserInfo
            {
                Id = session.BrowserId,
                Type = session.BrowserType,
                Version = session.Version,
                CurrentUrl = session.Page.Url,
                CurrentTitle = title,
                IsConnected = session.Browser?.IsConnected ?? true
            });
        }

        return new SuccessResponse<BrowserListResponse>(result);
    }

    public static Response Close(BrowserCloseRequest request)
    {
        if (!Sessions.TryRemove(request.BrowserId, out var session))
        {
            return new ErrorResponse($"Browser not found: {request.BrowserId}");
        }

        try
        {
            RunSync(() => session.Context.CloseAsync());
            if (session.Browser != null)
            {
                RunSync(() => session.Browser.CloseAsync());
            }

            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Close failed: {ex.Message}");
        }
    }

    public static Response Codegen(BrowserCodegenRequest request)
    {
        try
        {
            var browserType = NormalizeBrowserType(request.BrowserType ?? "chromium");
            var args = new List<string> { "codegen", "--browser", browserType };
            if (!string.IsNullOrWhiteSpace(request.Url))
            {
                args.Add(request.Url);
            }

            _ = Task.Run(() => Microsoft.Playwright.Program.Main(args.ToArray()));
            return new SuccessResponse<object>(new
            {
                Message = "Codegen started"
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Codegen failed: {ex.Message}");
        }
    }

    public static Response RunScript(BrowserRunScriptRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ScriptPath))
            {
                return new ErrorResponse("ScriptPath is required");
            }

            var scriptPath = Path.GetFullPath(request.ScriptPath);
            if (!File.Exists(scriptPath))
            {
                return new ErrorResponse($"Script file not found: {scriptPath}");
            }

            var browserType = NormalizeBrowserType(request.BrowserType);
            var args = new List<string> { "test", scriptPath, "--browser", MapCliBrowser(browserType) };
            if (browserType == "edge")
            {
                args.Add("--channel");
                args.Add("msedge");
            }
            if (!request.Headless)
            {
                args.Add("--headed");
            }
            if (request.Timeout.HasValue && request.Timeout.Value > 0)
            {
                args.Add("--timeout");
                args.Add(request.Timeout.Value.ToString());
            }
            if (request.ExtraArgs is { Length: > 0 })
            {
                args.AddRange(request.ExtraArgs.Where(a => !string.IsNullOrWhiteSpace(a)));
            }

            var exitCode = Microsoft.Playwright.Program.Main(args.ToArray());
            return new SuccessResponse<BrowserRunScriptResponse>(new BrowserRunScriptResponse
            {
                ExitCode = exitCode,
                Success = exitCode == 0,
                Command = $"playwright {string.Join(" ", args.Select(QuoteArgForDisplay))}"
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"RunScript failed: {ex.Message}");
        }
    }

    private static bool TryGetSession(string browserId, out BrowserSession session, out string error)
    {
        if (Sessions.TryGetValue(browserId, out session!))
        {
            error = string.Empty;
            return true;
        }

        error = $"Browser not found: {browserId}";
        return false;
    }

    private static string BuildSelector(string selector, string? selectorType)
    {
        var type = (selectorType ?? "css").Trim().ToLowerInvariant();
        return type switch
        {
            "xpath" => $"xpath={selector}",
            "text" => $"text={selector}",
            "id" => $"#{selector.TrimStart('#')}",
            _ => selector
        };
    }

    private static bool TryGetStringArg(Dictionary<string, object>? args, string key, out string value)
    {
        value = string.Empty;
        if (args == null || !args.TryGetValue(key, out var raw) || raw == null)
        {
            return false;
        }

        value = raw.ToString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static IBrowserType ResolveBrowserType(IPlaywright playwright, string browserType)
    {
        return browserType switch
        {
            "firefox" => playwright.Firefox,
            "webkit" => playwright.Webkit,
            _ => playwright.Chromium
        };
    }

    private static string NormalizeBrowserType(string browserType)
    {
        var type = browserType.Trim().ToLowerInvariant();
        return type switch
        {
            "msedge" => "edge",
            "chromium" or "firefox" or "webkit" or "edge" => type,
            _ => "chromium"
        };
    }

    private static IBrowser LaunchBrowser(IBrowserType browserType, BrowserLaunchRequest request)
    {
        return RunSync(() => browserType.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = request.Headless,
            Channel = GetBrowserChannel(request.BrowserType),
            ExecutablePath = string.IsNullOrWhiteSpace(request.ExecutablePath) ? null : request.ExecutablePath
        }));
    }

    private static IBrowserContext LaunchPersistentContext(IBrowserType browserType, BrowserLaunchRequest request)
    {
        var userDataDir = request.UserDataDir!;
        Directory.CreateDirectory(userDataDir);
        return RunSync(() => browserType.LaunchPersistentContextAsync(userDataDir, new BrowserTypeLaunchPersistentContextOptions
        {
            Headless = request.Headless,
            Channel = GetBrowserChannel(request.BrowserType),
            ExecutablePath = string.IsNullOrWhiteSpace(request.ExecutablePath) ? null : request.ExecutablePath
        }));
    }

    private static bool TryInstallBrowserAndRetry(PlaywrightException ex, string browserType)
    {
        if (!ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase) &&
            !ex.Message.Contains("browser has not been found", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var installTarget = browserType == "edge" ? "chromium" : browserType;
        var code = Microsoft.Playwright.Program.Main(new[] { "install", installTarget });
        return code == 0;
    }

    private static string? GetBrowserChannel(string? browserType)
    {
        var normalized = NormalizeBrowserType(browserType ?? "chromium");
        return normalized == "edge" ? "msedge" : null;
    }

    private static string MapCliBrowser(string browserType)
    {
        return browserType == "edge" ? "chromium" : browserType;
    }

    private static string QuoteArgForDisplay(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        return value.Any(char.IsWhiteSpace) ? $"\"{value}\"" : value;
    }

    private static WaitUntilState ParseWaitUntil(string? waitUntil)
    {
        return (waitUntil ?? "load").Trim().ToLowerInvariant() switch
        {
            "domcontentloaded" => WaitUntilState.DOMContentLoaded,
            "networkidle" => WaitUntilState.NetworkIdle,
            _ => WaitUntilState.Load
        };
    }

    private static WaitForSelectorState ParseWaitForState(string? state)
    {
        return (state ?? "visible").Trim().ToLowerInvariant() switch
        {
            "hidden" => WaitForSelectorState.Hidden,
            "attached" => WaitForSelectorState.Attached,
            "detached" => WaitForSelectorState.Detached,
            _ => WaitForSelectorState.Visible
        };
    }

    private static SameSiteAttribute? ParseSameSite(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "strict" => SameSiteAttribute.Strict,
            "lax" => SameSiteAttribute.Lax,
            "none" => SameSiteAttribute.None,
            _ => null
        };
    }

    private static Microsoft.Playwright.MouseButton ParseMouseButton(int? button)
    {
        return button switch
        {
            1 => Microsoft.Playwright.MouseButton.Middle,
            2 => Microsoft.Playwright.MouseButton.Right,
            _ => Microsoft.Playwright.MouseButton.Left
        };
    }

    private static ScreenshotType ParseScreenshotType(string? type)
    {
        return string.Equals(type, "jpeg", StringComparison.OrdinalIgnoreCase) ? ScreenshotType.Jpeg : ScreenshotType.Png;
    }

    private static IPlaywright GetPlaywright() => RunSync(() => PlaywrightLazy.Value);

    private static T RunSync<T>(Func<Task<T>> action) => action().GetAwaiter().GetResult();

    private static void RunSync(Func<Task> action) => action().GetAwaiter().GetResult();

    private sealed class PageMetrics
    {
        public int ScrollX { get; set; }
        public int ScrollY { get; set; }
        public int ViewportWidth { get; set; }
        public int ViewportHeight { get; set; }
        public int PageWidth { get; set; }
        public int PageHeight { get; set; }
    }

    private sealed record BrowserSession(
        string BrowserId,
        string BrowserType,
        IBrowser? Browser,
        IBrowserContext Context,
        IPage Page,
        string Version);
}

public class BrowserCodegenRequest : Request
{
    public string? BrowserType { get; set; }
    public string? Url { get; set; }
    public string? OutputFile { get; set; }
    public string? Target { get; set; }
}
