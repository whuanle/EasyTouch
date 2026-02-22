using System.Runtime.InteropServices;
using System.Text;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class ScreenModule
{
    [DllImport("user32.dll")]
    private static extern nint GetDC(nint hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint hwnd, nint hdc);

    [DllImport("gdi32.dll")]
    private static extern nint CreateCompatibleDC(nint hdc);

    [DllImport("gdi32.dll")]
    private static extern nint CreateCompatibleBitmap(nint hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern nint SelectObject(nint hdc, nint hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(nint hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(nint hdc);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(nint hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, nint hdcSrc, int nXSrc, int nYSrc, uint dwRop);

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(nint hdc, int nIndex);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(nint hdc, int nXPos, int nYPos);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(nint hdc, nint hbmp, uint uStartScan, uint cScanLines, nint lpvBits, ref BITMAPINFO lpbi, uint uUsage);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public uint[] bmiColors;
    }

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;
    private const int SM_CMONITORS = 80;

    private const uint SRCCOPY = 0x00CC0020;
    private const uint DIB_RGB_COLORS = 0;

    public static Response Screenshot(ScreenshotRequest request)
    {
        try
        {
            int x = request.X ?? 0;
            int y = request.Y ?? 0;
            int width, height;
            nint hwnd = request.WindowHandle ?? 0;

            if (hwnd != 0 && IsWindow(hwnd))
            {
                GetWindowRect(hwnd, out RECT rect);
                x = rect.Left;
                y = rect.Top;
                width = rect.Right - rect.Left;
                height = rect.Bottom - rect.Top;
            }
            else if (request.Width.HasValue && request.Height.HasValue)
            {
                width = request.Width.Value;
                height = request.Height.Value;
            }
            else
            {
                width = GetSystemMetrics(SM_CXSCREEN);
                height = GetSystemMetrics(SM_CYSCREEN);
            }

            nint hdcScreen = GetDC(0);
            nint hdcMem = CreateCompatibleDC(hdcScreen);
            nint hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
            nint hOldBitmap = SelectObject(hdcMem, hBitmap);

            BitBlt(hdcMem, 0, 0, width, height, hdcScreen, x, y, SRCCOPY);

            byte[]? pngData = null;
            string? filePath = null;

            if (!string.IsNullOrEmpty(request.OutputPath))
            {
                filePath = request.OutputPath;
                pngData = BitmapToPng(hdcMem, hBitmap, width, height);
                File.WriteAllBytes(filePath, pngData);
            }
            else
            {
                pngData = BitmapToPng(hdcMem, hBitmap, width, height);
            }

            SelectObject(hdcMem, hOldBitmap);
            DeleteObject(hBitmap);
            DeleteDC(hdcMem);
            ReleaseDC(0, hdcScreen);

            return new SuccessResponse<ScreenshotResponse>(new ScreenshotResponse
            {
                FilePath = filePath,
                Base64Data = pngData != null ? Convert.ToBase64String(pngData) : null,
                Width = width,
                Height = height
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response GetPixelColor(PixelColorRequest request)
    {
        try
        {
            nint hdc = GetDC(0);
            uint color = GetPixel(hdc, request.X, request.Y);
            ReleaseDC(0, hdc);

            int r = (int)(color & 0xFF);
            int g = (int)((color >> 8) & 0xFF);
            int b = (int)((color >> 16) & 0xFF);

            return new SuccessResponse<PixelColorResponse>(new PixelColorResponse
            {
                R = r,
                G = g,
                B = b,
                Hex = $"#{r:X2}{g:X2}{b:X2}"
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response ListScreens()
    {
        try
        {
            int screenCount = GetSystemMetrics(SM_CMONITORS);
            var screens = new List<ScreenInfo>();

            for (int i = 0; i < screenCount; i++)
            {
                int virtualX = GetSystemMetrics(SM_XVIRTUALSCREEN);
                int virtualY = GetSystemMetrics(SM_YVIRTUALSCREEN);
                int virtualWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
                int virtualHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

                screens.Add(new ScreenInfo(
                    i,
                    $"\\\\.\\DISPLAY{i + 1}",
                    new Rect(virtualX, virtualY, virtualWidth, virtualHeight),
                    new Rect(virtualX, virtualY, virtualWidth, virtualHeight),
                    i == 0,
                    32
                ));
            }

            return new SuccessResponse<ScreenListResponse>(new ScreenListResponse
            {
                Screens = screens.ToArray()
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    private static byte[] BitmapToPng(nint hdc, nint hBitmap, int width, int height)
    {
        var bmi = new BITMAPINFO
        {
            bmiHeader = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = -height,
                biPlanes = 1,
                biBitCount = 24,
                biCompression = 0,
                biSizeImage = (uint)(width * height * 3)
            },
            bmiColors = new uint[256]
        };

        int rowSize = ((width * 3 + 3) / 4) * 4;
        byte[] pixelData = new byte[rowSize * height];

        GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
        try
        {
            GetDIBits(hdc, hBitmap, 0, (uint)height, handle.AddrOfPinnedObject(), ref bmi, DIB_RGB_COLORS);
        }
        finally
        {
            handle.Free();
        }

        return EncodePng(width, height, pixelData, rowSize);
    }

    private static byte[] EncodePng(int width, int height, byte[] pixelData, int rowSize)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        WriteChunk(writer, "IHDR", CreateIHDR(width, height));
        WriteChunk(writer, "IDAT", CompressIDAT(width, height, pixelData, rowSize));
        WriteChunk(writer, "IEND", Array.Empty<byte>());

        return ms.ToArray();
    }

    private static byte[] CreateIHDR(int width, int height)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.WriteBigEndian(width);
        writer.WriteBigEndian(height);
        writer.Write((byte)8);
        writer.Write((byte)2);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((byte)0);
        return ms.ToArray();
    }

    private static byte[] CompressIDAT(int width, int height, byte[] pixelData, int rowSize)
    {
        using var ms = new MemoryStream();
        using (var zlib = new System.IO.Compression.ZLibStream(ms, System.IO.Compression.CompressionLevel.Optimal))
        {
            for (int y = 0; y < height; y++)
            {
                zlib.WriteByte(0);
                for (int x = 0; x < width; x++)
                {
                    int srcIdx = y * rowSize + x * 3;
                    zlib.WriteByte(pixelData[srcIdx + 2]);
                    zlib.WriteByte(pixelData[srcIdx + 1]);
                    zlib.WriteByte(pixelData[srcIdx]);
                }
            }
        }
        return ms.ToArray();
    }

    private static void WriteChunk(BinaryWriter writer, string type, byte[] data)
    {
        writer.WriteBigEndian(data.Length);
        writer.Write(Encoding.ASCII.GetBytes(type));
        writer.Write(data);
        
        uint crc = Crc32(Encoding.ASCII.GetBytes(type), data);
        writer.WriteBigEndian((int)crc);
    }

    private static uint Crc32(byte[] type, byte[] data)
    {
        uint[] crcTable = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int j = 0; j < 8; j++)
            {
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            }
            crcTable[i] = c;
        }

        uint crc = 0xFFFFFFFF;
        foreach (byte b in type) crc = crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        foreach (byte b in data) crc = crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFF;
    }
}

internal static class BinaryWriterExtensions
{
    public static void WriteBigEndian(this BinaryWriter writer, int value)
    {
        writer.Write((byte)((value >> 24) & 0xFF));
        writer.Write((byte)((value >> 16) & 0xFF));
        writer.Write((byte)((value >> 8) & 0xFF));
        writer.Write((byte)(value & 0xFF));
    }
}
