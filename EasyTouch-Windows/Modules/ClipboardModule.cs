using System.Runtime.InteropServices;
using System.Text;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class ClipboardModule
{
    [DllImport("user32.dll")]
    private static extern bool OpenClipboard(nint hWndNewOwner);

    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll")]
    private static extern nint GetClipboardData(uint uFormat);

    [DllImport("user32.dll")]
    private static extern nint SetClipboardData(uint uFormat, nint hMem);

    [DllImport("kernel32.dll")]
    private static extern nint GlobalLock(nint hMem);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(nint hMem);

    [DllImport("kernel32.dll")]
    private static extern nint GlobalAlloc(uint uFlags, nuint dwBytes);

    [DllImport("kernel32.dll")]
    private static extern nint GlobalFree(nint hMem);

    [DllImport("kernel32.dll")]
    private static extern nuint GlobalSize(nint hMem);

    private const uint CF_TEXT = 1;
    private const uint CF_BITMAP = 2;
    private const uint CF_UNICODETEXT = 13;
    private const uint CF_HDROP = 15;
    private const uint CF_DIB = 8;

    private const uint GMEM_MOVEABLE = 0x0002;
    private const uint GMEM_ZEROINIT = 0x0040;

    public static Response GetText(ClipboardGetTextRequest request)
    {
        try
        {
            if (!OpenClipboard(0))
                return new ErrorResponse("Failed to open clipboard");

            try
            {
                nint hData = GetClipboardData(CF_UNICODETEXT);
                if (hData == 0)
                {
                    hData = GetClipboardData(CF_TEXT);
                    if (hData == 0)
                        return new SuccessResponse<ClipboardTextResponse>(new ClipboardTextResponse { Text = null });

                    nint ptr = GlobalLock(hData);
                    if (ptr == 0)
                        return new ErrorResponse("Failed to lock clipboard data");

                    try
                    {
                        string text = Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
                        return new SuccessResponse<ClipboardTextResponse>(new ClipboardTextResponse { Text = text });
                    }
                    finally
                    {
                        GlobalUnlock(hData);
                    }
                }
                else
                {
                    nint ptr = GlobalLock(hData);
                    if (ptr == 0)
                        return new ErrorResponse("Failed to lock clipboard data");

                    try
                    {
                        string text = Marshal.PtrToStringUni(ptr) ?? string.Empty;
                        return new SuccessResponse<ClipboardTextResponse>(new ClipboardTextResponse { Text = text });
                    }
                    finally
                    {
                        GlobalUnlock(hData);
                    }
                }
            }
            finally
            {
                CloseClipboard();
            }
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response SetText(ClipboardSetTextRequest request)
    {
        try
        {
            if (!OpenClipboard(0))
                return new ErrorResponse("Failed to open clipboard");

            try
            {
                EmptyClipboard();

                int size = (request.Text.Length + 1) * 2;
                nint hMem = GlobalAlloc(GMEM_MOVEABLE | GMEM_ZEROINIT, (nuint)size);
                if (hMem == 0)
                    return new ErrorResponse("Failed to allocate memory");

                nint ptr = GlobalLock(hMem);
                if (ptr == 0)
                {
                    GlobalFree(hMem);
                    return new ErrorResponse("Failed to lock memory");
                }

                try
                {
                    Marshal.Copy(request.Text.ToCharArray(), 0, ptr, request.Text.Length);
                }
                finally
                {
                    GlobalUnlock(hMem);
                }

                if (SetClipboardData(CF_UNICODETEXT, hMem) == 0)
                {
                    GlobalFree(hMem);
                    return new ErrorResponse("Failed to set clipboard data");
                }

                return new SuccessResponse();
            }
            finally
            {
                CloseClipboard();
            }
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response Clear(ClipboardClearRequest request)
    {
        try
        {
            if (!OpenClipboard(0))
                return new ErrorResponse("Failed to open clipboard");

            try
            {
                EmptyClipboard();
                return new SuccessResponse();
            }
            finally
            {
                CloseClipboard();
            }
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response GetFiles(ClipboardGetFilesRequest request)
    {
        try
        {
            if (!OpenClipboard(0))
                return new ErrorResponse("Failed to open clipboard");

            try
            {
                nint hData = GetClipboardData(CF_HDROP);
                if (hData == 0)
                    return new SuccessResponse<ClipboardFilesResponse>(new ClipboardFilesResponse { Files = [] });

                var files = new List<string>();
                nint ptr = GlobalLock(hData);
                if (ptr == 0)
                    return new ErrorResponse("Failed to lock clipboard data");

                try
                {
                    uint fileCount = DragQueryFile(ptr, 0xFFFFFFFF, null, 0);
                    for (uint i = 0; i < fileCount; i++)
                    {
                        var sb = new StringBuilder(260);
                        DragQueryFile(ptr, i, sb, 260);
                        files.Add(sb.ToString());
                    }
                }
                finally
                {
                    GlobalUnlock(hData);
                }

                return new SuccessResponse<ClipboardFilesResponse>(new ClipboardFilesResponse
                {
                    Files = files.ToArray()
                });
            }
            finally
            {
                CloseClipboard();
            }
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint DragQueryFile(nint hDrop, uint iFile, StringBuilder? lpszFile, uint cch);
}
