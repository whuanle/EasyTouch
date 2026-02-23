using System.Runtime.InteropServices;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

/// <summary>
/// Windows 音频控制模块 - 使用 Core Audio API (COM)
/// 参考 C++ 实现：IAudioEndpointVolume 接口
/// </summary>
public static class AudioModule
{
    #region COM Interfaces (只定义必要的方法)

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        // 我们不使用这些方法，但必须声明它们来保持 vtable 顺序
        void NotImpl1(); // EnumAudioEndpoints
        
        [PreserveSig]
        int GetDefaultAudioEndpoint(DataFlow dataFlow, Role role, out IMMDevice endpoint);
        
        void NotImpl2(); // GetDevice
        void NotImpl3(); // RegisterEndpointNotificationCallback
        void NotImpl4(); // UnregisterEndpointNotificationCallback
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, CLSCTX clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);
        
        void NotImpl1(); // OpenPropertyStore
        void NotImpl2(); // GetId
        void NotImpl3(); // GetState
    }

    [ComImport]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        void NotImpl1(); // RegisterControlChangeNotify
        void NotImpl2(); // UnregisterControlChangeNotify
        void NotImpl3(); // GetChannelCount
        void NotImpl4(); // SetMasterVolumeLevel
        
        [PreserveSig]
        int SetMasterVolumeLevelScalar(float level, Guid eventContext);
        
        void NotImpl5(); // GetMasterVolumeLevel
        
        [PreserveSig]
        int GetMasterVolumeLevelScalar(out float level);
        
        void NotImpl6(); // SetChannelVolumeLevel
        void NotImpl7(); // SetChannelVolumeLevelScalar
        void NotImpl8(); // GetChannelVolumeLevel
        void NotImpl9(); // GetChannelVolumeLevelScalar
        
        [PreserveSig]
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, Guid eventContext);
        
        [PreserveSig]
        int GetMute(out bool mute);
        
        void NotImpl10(); // GetVolumeStepInfo
        void NotImpl11(); // VolumeStepUp
        void NotImpl12(); // VolumeStepDown
        void NotImpl13(); // QueryHardwareSupport
        void NotImpl14(); // GetVolumeRange
    }

    [ComImport]
    [Guid("1BE09788-6894-4089-8586-9A2A6C265AC5")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        [PreserveSig]
        int GetCount(out uint count);
        
        [PreserveSig]
        int Item(uint index, out IMMDevice device);
    }

    #endregion

    #region Enums

    private enum DataFlow { eRender, eCapture, eAll }
    private enum Role { eConsole, eMultimedia, eCommunications }
    
    [Flags]
    private enum CLSCTX : uint
    {
        INPROC_SERVER = 0x1,
        INPROC_HANDLER = 0x2,
        LOCAL_SERVER = 0x4,
        ALL = 0x17
    }

    #endregion

    #region Win32 API

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

    private const uint COINIT_APARTMENTTHREADED = 0x2;
    private const byte VK_VOLUME_MUTE = 0xAD;
    private const byte VK_VOLUME_DOWN = 0xAE;
    private const byte VK_VOLUME_UP = 0xAF;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private static readonly Guid GUID_NULL = Guid.Empty;

    #endregion

    /// <summary>
    /// 获取系统音量（0-100）
    /// </summary>
    public static Response GetVolume(VolumeGetRequest request)
    {
        IAudioEndpointVolume? volume = null;
        IMMDevice? device = null;
        IMMDeviceEnumerator? enumerator = null;

        try
        {
            CoInitializeEx(IntPtr.Zero, COINIT_APARTMENTTHREADED);

            // 创建设备枚举器: CoCreateInstance(__uuidof(MMDeviceEnumerator), ...)
            var type = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"));
            enumerator = (IMMDeviceEnumerator?)Activator.CreateInstance(type!);

            if (enumerator == null)
                return new ErrorResponse("Failed to create MMDeviceEnumerator");

            // 获取默认音频设备: GetDefaultAudioEndpoint(eRender, eMultimedia, &pDevice)
            int hr = enumerator.GetDefaultAudioEndpoint(DataFlow.eRender, Role.eMultimedia, out device);
            if (hr != 0 || device == null)
                return new ErrorResponse($"Failed to get default audio endpoint. HRESULT: 0x{hr:X8}");

            // 激活 IAudioEndpointVolume: pDevice->Activate(__uuidof(IAudioEndpointVolume), ...)
            Guid iid = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
            hr = device.Activate(ref iid, CLSCTX.ALL, IntPtr.Zero, out object? volumeObj);
            if (hr != 0 || volumeObj == null)
                return new ErrorResponse($"Failed to activate IAudioEndpointVolume. HRESULT: 0x{hr:X8}");

            volume = (IAudioEndpointVolume)volumeObj;

            // 获取音量: pAudioEndpointVolume->GetMasterVolumeLevelScalar(&volume)
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
        catch (Exception ex)
        {
            return new ErrorResponse($"Get volume failed: {ex.Message}");
        }
        finally
        {
            if (volume != null) Marshal.ReleaseComObject(volume);
            if (device != null) Marshal.ReleaseComObject(device);
            if (enumerator != null) Marshal.ReleaseComObject(enumerator);
        }
    }

    /// <summary>
    /// 设置系统音量（0-100）
    /// </summary>
    public static Response SetVolume(VolumeSetRequest request)
    {
        IAudioEndpointVolume? volume = null;
        IMMDevice? device = null;
        IMMDeviceEnumerator? enumerator = null;

        try
        {
            CoInitializeEx(IntPtr.Zero, COINIT_APARTMENTTHREADED);

            var type = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"));
            enumerator = (IMMDeviceEnumerator?)Activator.CreateInstance(type!);

            if (enumerator == null)
                return new ErrorResponse("Failed to create MMDeviceEnumerator");

            int hr = enumerator.GetDefaultAudioEndpoint(DataFlow.eRender, Role.eMultimedia, out device);
            if (hr != 0 || device == null)
                return new ErrorResponse($"Failed to get default audio endpoint. HRESULT: 0x{hr:X8}");

            Guid iid = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
            hr = device.Activate(ref iid, CLSCTX.ALL, IntPtr.Zero, out object? volumeObj);
            if (hr != 0 || volumeObj == null)
                return new ErrorResponse($"Failed to activate IAudioEndpointVolume. HRESULT: 0x{hr:X8}");

            volume = (IAudioEndpointVolume)volumeObj;

            // 设置音量: pAudioEndpointVolume->SetMasterVolumeLevelScalar(fVolume, &GUID_NULL)
            float level = Math.Clamp(request.Level, 0, 100) / 100.0f;
            hr = volume.SetMasterVolumeLevelScalar(level, GUID_NULL);
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
            if (volume != null) Marshal.ReleaseComObject(volume);
            if (device != null) Marshal.ReleaseComObject(device);
            if (enumerator != null) Marshal.ReleaseComObject(enumerator);
        }
    }

    /// <summary>
    /// 设置静音状态
    /// </summary>
    public static Response SetMute(VolumeMuteRequest request)
    {
        IAudioEndpointVolume? volume = null;
        IMMDevice? device = null;
        IMMDeviceEnumerator? enumerator = null;

        try
        {
            CoInitializeEx(IntPtr.Zero, COINIT_APARTMENTTHREADED);

            var type = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"));
            enumerator = (IMMDeviceEnumerator?)Activator.CreateInstance(type!);

            if (enumerator == null)
                return new ErrorResponse("Failed to create MMDeviceEnumerator");

            int hr = enumerator.GetDefaultAudioEndpoint(DataFlow.eRender, Role.eMultimedia, out device);
            if (hr != 0 || device == null)
                return new ErrorResponse($"Failed to get default audio endpoint. HRESULT: 0x{hr:X8}");

            Guid iid = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
            hr = device.Activate(ref iid, CLSCTX.ALL, IntPtr.Zero, out object? volumeObj);
            if (hr != 0 || volumeObj == null)
                return new ErrorResponse($"Failed to activate IAudioEndpointVolume. HRESULT: 0x{hr:X8}");

            volume = (IAudioEndpointVolume)volumeObj;

            // 设置静音: pAudioEndpointVolume->SetMute(TRUE/FALSE, NULL)
            hr = volume.SetMute(request.Mute, GUID_NULL);
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
            if (volume != null) Marshal.ReleaseComObject(volume);
            if (device != null) Marshal.ReleaseComObject(device);
            if (enumerator != null) Marshal.ReleaseComObject(enumerator);
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
