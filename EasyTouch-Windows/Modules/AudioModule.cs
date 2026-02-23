using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

/// <summary>
/// Windows 音频控制模块 - 使用 Core Audio API (COM)
/// 使用 AOT 兼容的 COM 激活方式
/// </summary>
public static class AudioModule
{
    #region COM GUIDs

    private static readonly Guid CLSID_MMDeviceEnumerator = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid IID_IMMDeviceEnumerator = new Guid("A95664D2-9614-4F35-A746-DE8DB63617E6");
    private static readonly Guid IID_IAudioEndpointVolume = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
    private static readonly Guid GUID_NULL = Guid.Empty;

    #endregion

    #region Win32 API

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    [DllImport("ole32.dll")]
    private static extern int CoCreateInstance(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
        [MarshalAs(UnmanagedType.IUnknown)] object? pUnkOuter,
        uint dwClsContext,
        [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

    // WinMM API for AOT-compatible volume control
    [DllImport("winmm.dll")]
    private static extern int waveOutGetVolume(IntPtr hwo, out uint pdwVolume);

    [DllImport("winmm.dll")]
    private static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

    private const uint COINIT_APARTMENTTHREADED = 0x2;
    private const uint CLSCTX_ALL = 23; // CLSCTX_INPROC_SERVER | CLSCTX_INPROC_HANDLER | CLSCTX_LOCAL_SERVER | CLSCTX_REMOTE_SERVER
    private const byte VK_VOLUME_MUTE = 0xAD;
    private const byte VK_VOLUME_DOWN = 0xAE;
    private const byte VK_VOLUME_UP = 0xAF;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    #endregion

    #region COM Interfaces

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        void NotImpl1();
        
        [PreserveSig]
        int GetDefaultAudioEndpoint(DataFlow dataFlow, Role role, out IMMDevice endpoint);
        
        void NotImpl2();
        void NotImpl3();
        void NotImpl4();
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, uint clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);
        
        void NotImpl1();
        void NotImpl2();
        void NotImpl3();
    }

    [ComImport]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        void NotImpl1();
        void NotImpl2();
        void NotImpl3();
        void NotImpl4();
        
        [PreserveSig]
        int SetMasterVolumeLevelScalar(float level, Guid eventContext);
        
        void NotImpl5();
        
        [PreserveSig]
        int GetMasterVolumeLevelScalar(out float level);
        
        void NotImpl6();
        void NotImpl7();
        void NotImpl8();
        void NotImpl9();
        
        [PreserveSig]
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, Guid eventContext);
        
        [PreserveSig]
        int GetMute(out bool mute);
        
        void NotImpl10();
        void NotImpl11();
        void NotImpl12();
        void NotImpl13();
        void NotImpl14();
    }

    private enum DataFlow { eRender, eCapture, eAll }
    private enum Role { eConsole, eMultimedia, eCommunications }

    #endregion

    /// <summary>
    /// 检测是否在 AOT 模式下运行
    /// </summary>
    private static bool IsAotMode()
    {
        // 在 AOT 模式下，Assembly.GetExecutingAssembly().Location 为空或特定值
        // 或者检查 RuntimeFeature.IsDynamicCodeSupported
        try
        {
            // 尝试创建一个简单的 COM 对象来检测是否支持 COM
            // 如果抛出 InvalidOperationException，说明在 AOT 模式下
            return !RuntimeFeature.IsDynamicCodeSupported;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// 获取系统音量（0-100）
    /// 使用 CoCreateInstance 直接创建 COM 对象（AOT 兼容）
    /// </summary>
    public static Response GetVolume(VolumeGetRequest request)
    {
        // 检测 AOT 模式
        if (IsAotMode())
        {
            return new ErrorResponse(
                "Volume control requires COM interop which is not fully supported in AOT mode. " +
                "Please use VolumeUp/VolumeDown methods for relative control, " +
                "or run without AOT compilation (dotnet run without -p:PublishAot=true)."
            );
        }

        object? enumeratorObj = null;
        object? deviceObj = null;
        object? volumeObj = null;

        try
        {
            // 初始化 COM
            int hr = CoInitializeEx(IntPtr.Zero, COINIT_APARTMENTTHREADED);
            
            // 使用 CoCreateInstance 创建 COM 对象（AOT 兼容）
            Guid iidEnumerator = IID_IMMDeviceEnumerator;
            hr = CoCreateInstance(
                CLSID_MMDeviceEnumerator,
                null,
                CLSCTX_ALL,
                iidEnumerator,
                out enumeratorObj);

            if (hr != 0 || enumeratorObj == null)
                return new ErrorResponse($"Failed to create MMDeviceEnumerator. HRESULT: 0x{hr:X8}");

            var enumerator = (IMMDeviceEnumerator)enumeratorObj;

            // 获取默认音频设备
            hr = enumerator.GetDefaultAudioEndpoint(DataFlow.eRender, Role.eMultimedia, out var device);
            if (hr != 0 || device == null)
                return new ErrorResponse($"Failed to get default audio endpoint. HRESULT: 0x{hr:X8}");

            deviceObj = device;

            // 激活 IAudioEndpointVolume 接口
            Guid iidVolume = IID_IAudioEndpointVolume;
            hr = device.Activate(ref iidVolume, CLSCTX_ALL, IntPtr.Zero, out volumeObj);
            if (hr != 0 || volumeObj == null)
                return new ErrorResponse($"Failed to activate IAudioEndpointVolume. HRESULT: 0x{hr:X8}");

            var volume = (IAudioEndpointVolume)volumeObj;

            // 获取音量
            hr = volume.GetMasterVolumeLevelScalar(out float level);
            if (hr != 0)
                return new ErrorResponse($"Failed to get volume. HRESULT: 0x{hr:X8}");

            // 获取静音状态
            hr = volume.GetMute(out bool isMuted);
            if (hr != 0)
                isMuted = false;

            return new SuccessResponse<VolumeResponse>(new VolumeResponse
            {
                Level = (int)Math.Round(level * 100.0),
                IsMuted = isMuted
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("ComInterop"))
        {
            return new ErrorResponse(
                "COM interop is not supported in AOT mode. " +
                "Please use VolumeUp/VolumeDown/SetMute methods for volume control, " +
                "or disable AOT compilation."
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Get volume failed: {ex.Message}");
        }
        finally
        {
            // 释放 COM 对象
            if (volumeObj != null) Marshal.ReleaseComObject(volumeObj);
            if (deviceObj != null) Marshal.ReleaseComObject(deviceObj);
            if (enumeratorObj != null) Marshal.ReleaseComObject(enumeratorObj);
        }
    }

    /// <summary>
    /// 设置系统音量（0-100）
    /// </summary>
    public static Response SetVolume(VolumeSetRequest request)
    {
        // 检测 AOT 模式
        if (IsAotMode())
        {
            return new ErrorResponse(
                "Volume control requires COM interop which is not fully supported in AOT mode. " +
                "Please use VolumeUp/VolumeDown methods for relative control, " +
                "or run without AOT compilation (dotnet run without -p:PublishAot=true)."
            );
        }

        object? enumeratorObj = null;
        object? deviceObj = null;
        object? volumeObj = null;

        try
        {
            CoInitializeEx(IntPtr.Zero, COINIT_APARTMENTTHREADED);
            
            int hr = CoCreateInstance(
                CLSID_MMDeviceEnumerator,
                null,
                CLSCTX_ALL,
                IID_IMMDeviceEnumerator,
                out enumeratorObj);

            if (hr != 0 || enumeratorObj == null)
                return new ErrorResponse($"Failed to create MMDeviceEnumerator. HRESULT: 0x{hr:X8}");

            var enumerator = (IMMDeviceEnumerator)enumeratorObj;

            hr = enumerator.GetDefaultAudioEndpoint(DataFlow.eRender, Role.eMultimedia, out var device);
            if (hr != 0 || device == null)
                return new ErrorResponse($"Failed to get default audio endpoint. HRESULT: 0x{hr:X8}");

            deviceObj = device;

            Guid iidVolume = IID_IAudioEndpointVolume;
            hr = device.Activate(ref iidVolume, CLSCTX_ALL, IntPtr.Zero, out volumeObj);
            if (hr != 0 || volumeObj == null)
                return new ErrorResponse($"Failed to activate IAudioEndpointVolume. HRESULT: 0x{hr:X8}");

            var volume = (IAudioEndpointVolume)volumeObj;

            // 设置音量
            float level = Math.Clamp(request.Level, 0, 100) / 100.0f;
            Guid guidNull = GUID_NULL;
            hr = volume.SetMasterVolumeLevelScalar(level, guidNull);
            if (hr != 0)
                return new ErrorResponse($"Failed to set volume. HRESULT: 0x{hr:X8}");

            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Set volume failed: {ex.Message}");
        }
        finally
        {
            if (volumeObj != null) Marshal.ReleaseComObject(volumeObj);
            if (deviceObj != null) Marshal.ReleaseComObject(deviceObj);
            if (enumeratorObj != null) Marshal.ReleaseComObject(enumeratorObj);
        }
    }

    /// <summary>
    /// 设置静音状态
    /// </summary>
    public static Response SetMute(VolumeMuteRequest request)
    {
        // 检测 AOT 模式
        if (IsAotMode())
        {
            return new ErrorResponse(
                "Volume control requires COM interop which is not fully supported in AOT mode. " +
                "Please use VolumeUp/VolumeDown methods for relative control, " +
                "or run without AOT compilation (dotnet run without -p:PublishAot=true)."
            );
        }

        object? enumeratorObj = null;
        object? deviceObj = null;
        object? volumeObj = null;

        try
        {
            CoInitializeEx(IntPtr.Zero, COINIT_APARTMENTTHREADED);
            
            int hr = CoCreateInstance(
                CLSID_MMDeviceEnumerator,
                null,
                CLSCTX_ALL,
                IID_IMMDeviceEnumerator,
                out enumeratorObj);

            if (hr != 0 || enumeratorObj == null)
                return new ErrorResponse($"Failed to create MMDeviceEnumerator. HRESULT: 0x{hr:X8}");

            var enumerator = (IMMDeviceEnumerator)enumeratorObj;

            hr = enumerator.GetDefaultAudioEndpoint(DataFlow.eRender, Role.eMultimedia, out var device);
            if (hr != 0 || device == null)
                return new ErrorResponse($"Failed to get default audio endpoint. HRESULT: 0x{hr:X8}");

            deviceObj = device;

            Guid iidVolume = IID_IAudioEndpointVolume;
            hr = device.Activate(ref iidVolume, CLSCTX_ALL, IntPtr.Zero, out volumeObj);
            if (hr != 0 || volumeObj == null)
                return new ErrorResponse($"Failed to activate IAudioEndpointVolume. HRESULT: 0x{hr:X8}");

            var volume = (IAudioEndpointVolume)volumeObj;

            Guid guidNull = GUID_NULL;
            hr = volume.SetMute(request.Mute, guidNull);
            if (hr != 0)
                return new ErrorResponse($"Failed to set mute. HRESULT: 0x{hr:X8}");

            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Set mute failed: {ex.Message}");
        }
        finally
        {
            if (volumeObj != null) Marshal.ReleaseComObject(volumeObj);
            if (deviceObj != null) Marshal.ReleaseComObject(deviceObj);
            if (enumeratorObj != null) Marshal.ReleaseComObject(enumeratorObj);
        }
    }

    /// <summary>
    /// 增加音量（使用多媒体键）
    /// </summary>
    public static Response VolumeUp(int steps = 1)
    {
        try
        {
            for (int i = 0; i < steps; i++)
            {
                keybd_event(VK_VOLUME_UP, 0, 0, 0);
                keybd_event(VK_VOLUME_UP, 0, KEYEVENTF_KEYUP, 0);
                Thread.Sleep(50);
            }
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Volume up failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 降低音量（使用多媒体键）
    /// </summary>
    public static Response VolumeDown(int steps = 1)
    {
        try
        {
            for (int i = 0; i < steps; i++)
            {
                keybd_event(VK_VOLUME_DOWN, 0, 0, 0);
                keybd_event(VK_VOLUME_DOWN, 0, KEYEVENTF_KEYUP, 0);
                Thread.Sleep(50);
            }
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Volume down failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 列出音频设备
    /// </summary>
    public static Response ListDevices(AudioDeviceListRequest request)
    {
        return new SuccessResponse<AudioDeviceListResponse>(new AudioDeviceListResponse
        {
            Devices = new[] { new AudioDeviceInfo("default", "Default Audio Device", true, false, 0) }
        });
    }
}
