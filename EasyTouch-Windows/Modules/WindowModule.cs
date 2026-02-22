using System.Runtime.InteropServices;
using System.Text;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class WindowModule
{
    [DllImport("user32.dll")]
    private static extern nint FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern nint FindWindowEx(nint hwndParent, nint hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hWnd, uint Msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern nint SendMessage(nint hWnd, uint Msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern int GetClassName(nint hWnd, StringBuilder lpClassName, int nMaxCount);

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private const uint WM_CLOSE = 0x0010;
    private const uint WM_SYSCOMMAND = 0x0112;
    private const nint SC_CLOSE = 0xF060;

    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_NOACTIVATE = 0x0010;

    private static readonly nint HWND_TOP = 0;
    private static readonly nint HWND_BOTTOM = 1;
    private static readonly nint HWND_TOPMOST = new(-1);
    private static readonly nint HWND_NOTOPMOST = new(-2);

    public static Response List(WindowListRequest request)
    {
        try
        {
            var windows = new List<WindowInfo>();
            
            EnumWindows((hwnd, _) =>
            {
                if (!IsWindowVisible(hwnd) && request.VisibleOnly)
                    return true;

                string title = GetWindowTitle(hwnd);
                
                if (!string.IsNullOrEmpty(request.TitleFilter) && 
                    !title.Contains(request.TitleFilter, StringComparison.OrdinalIgnoreCase))
                    return true;

                GetWindowRect(hwnd, out RECT rect);
                GetWindowThreadProcessId(hwnd, out uint pid);
                string className = GetClassName(hwnd);

                windows.Add(new WindowInfo(
                    (long)hwnd,
                    title,
                    className,
                    new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top),
                    IsWindowVisible(hwnd),
                    pid
                ));

                return true;
            }, 0);

            return new SuccessResponse<WindowListResponse>(new WindowListResponse
            {
                Windows = windows.ToArray()
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response Find(WindowFindRequest request)
    {
        try
        {
            nint hwnd = 0;

            if (!string.IsNullOrEmpty(request.Title) || !string.IsNullOrEmpty(request.ClassName))
            {
                hwnd = FindWindow(request.ClassName, request.Title);
            }
            else if (request.ProcessId.HasValue)
            {
                EnumWindows((h, _) =>
                {
                    GetWindowThreadProcessId(h, out uint pid);
                    if (pid == request.ProcessId.Value)
                    {
                        hwnd = h;
                        return false;
                    }
                    return true;
                }, 0);
            }

            if (hwnd == 0)
            {
                return new SuccessResponse<WindowFindResponse>(new WindowFindResponse { Handle = null });
            }

            GetWindowRect(hwnd, out RECT rect);
            GetWindowThreadProcessId(hwnd, out uint pid);
            string title = GetWindowTitle(hwnd);
            string className = GetClassName(hwnd);

            return new SuccessResponse<WindowFindResponse>(new WindowFindResponse
            {
                Handle = (long)hwnd,
                Window = new WindowInfo(
                    hwnd,
                    title,
                    className,
                    new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top),
                    IsWindowVisible(hwnd),
                    pid
                )
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response Activate(WindowActivateRequest request)
    {
        try
        {
            if (!IsWindow((nint)request.Handle))
                return new ErrorResponse("Invalid window handle");

            SetForegroundWindow((nint)request.Handle);
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response Show(WindowShowRequest request)
    {
        try
        {
            if (!IsWindow((nint)request.Handle))
                return new ErrorResponse("Invalid window handle");

            int cmdShow = request.State switch
            {
                WindowShowState.Hide => 0,
                WindowShowState.ShowNormal => 1,
                WindowShowState.ShowMinimized => 2,
                WindowShowState.ShowMaximized => 3,
                WindowShowState.ShowNoActivate => 4,
                WindowShowState.Show => 5,
                WindowShowState.Minimize => 6,
                WindowShowState.ShowMinNoActive => 7,
                WindowShowState.ShowNA => 8,
                WindowShowState.Restore => 9,
                WindowShowState.ShowDefault => 10,
                WindowShowState.ForceMinimize => 11,
                _ => 1
            };

            ShowWindow((nint)request.Handle, cmdShow);
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response Move(WindowMoveRequest request)
    {
        try
        {
            if (!IsWindow((nint)request.Handle))
                return new ErrorResponse("Invalid window handle");

            GetWindowRect((nint)request.Handle, out RECT currentRect);
            int width = request.Width ?? (currentRect.Right - currentRect.Left);
            int height = request.Height ?? (currentRect.Bottom - currentRect.Top);

            SetWindowPos((nint)request.Handle, HWND_TOP, request.X, request.Y, width, height, 
                SWP_FRAMECHANGED | SWP_SHOWWINDOW);
            
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response SetTopmost(WindowSetTopmostRequest request)
    {
        try
        {
            if (!IsWindow((nint)request.Handle))
                return new ErrorResponse("Invalid window handle");

            nint zOrder = request.Topmost ? HWND_TOPMOST : HWND_NOTOPMOST;
            SetWindowPos((nint)request.Handle, zOrder, 0, 0, 0, 0, 
                SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response Close(WindowCloseRequest request)
    {
        try
        {
            if (!IsWindow((nint)request.Handle))
                return new ErrorResponse("Invalid window handle");

            if (request.Force)
            {
                PostMessage((nint)request.Handle, WM_CLOSE, 0, 0);
            }
            else
            {
                SendMessage((nint)request.Handle, WM_SYSCOMMAND, SC_CLOSE, 0);
            }
            
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response GetForeground()
    {
        try
        {
            nint hwnd = GetForegroundWindow();
            if (hwnd == 0)
            {
                return new SuccessResponse<WindowFindResponse>(new WindowFindResponse { Handle = null });
            }

            GetWindowRect(hwnd, out RECT rect);
            GetWindowThreadProcessId(hwnd, out uint pid);
            string title = GetWindowTitle(hwnd);
            string className = GetClassName(hwnd);

            return new SuccessResponse<WindowFindResponse>(new WindowFindResponse
            {
                Handle = (long)hwnd,
                Window = new WindowInfo(
                    hwnd,
                    title,
                    className,
                    new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top),
                    IsWindowVisible(hwnd),
                    pid
                )
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    private static string GetWindowTitle(nint hwnd)
    {
        int length = GetWindowTextLength(hwnd);
        if (length == 0) return string.Empty;
        
        var sb = new StringBuilder(length + 1);
        GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static string GetClassName(nint hwnd)
    {
        var sb = new StringBuilder(256);
        GetClassName(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }
}
