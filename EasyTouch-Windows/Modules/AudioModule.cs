using System.Runtime.InteropServices;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class AudioModule
{
    private static readonly Guid CLSID_MMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid IID_IMMDeviceEnumerator = new("A95664D2-9614-4F35-A746-DE8DB63617E6");

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(EDataFlow dataFlow, DeviceState stateMask, out IMMDeviceCollection devices);

        [PreserveSig]
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);

        [PreserveSig]
        int GetDevice(string pwstrId, out IMMDevice device);

        [PreserveSig]
        int RegisterEndpointNotificationCallback(nint pClient);

        [PreserveSig]
        int UnregisterEndpointNotificationCallback(nint pClient);
    }

    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        [PreserveSig]
        int GetCount(out uint pcDevices);

        [PreserveSig]
        int Item(uint nDevice, out IMMDevice device);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, CLSCTX clsCtx, nint pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

        [PreserveSig]
        int OpenPropertyStore(StorageAccessMode stgmAccess, out nint ppProperties);

        [PreserveSig]
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);

        [PreserveSig]
        int GetState(out DeviceState pdwState);
    }

    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        [PreserveSig]
        int RegisterControlChangeNotify(nint pNotify);

        [PreserveSig]
        int UnregisterControlChangeNotify(nint pNotify);

        [PreserveSig]
        int GetChannelCount(out uint pnChannelCount);

        [PreserveSig]
        int SetMasterVolumeLevel(float fLevelDB, nint pguidEventContext);

        [PreserveSig]
        int SetMasterVolumeLevelScalar(float fLevel, nint pguidEventContext);

        [PreserveSig]
        int GetMasterVolumeLevel(out float pfLevelDB);

        [PreserveSig]
        int GetMasterVolumeLevelScalar(out float pfLevel);

        [PreserveSig]
        int SetChannelVolumeLevel(uint nChannel, float fLevelDB, nint pguidEventContext);

        [PreserveSig]
        int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, nint pguidEventContext);

        [PreserveSig]
        int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);

        [PreserveSig]
        int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);

        [PreserveSig]
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, nint pguidEventContext);

        [PreserveSig]
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);

        [PreserveSig]
        int GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);

        [PreserveSig]
        int VolumeStepUp(nint pguidEventContext);

        [PreserveSig]
        int VolumeStepDown(nint pguidEventContext);

        [PreserveSig]
        int QueryHardwareSupport(out uint pdwHardwareSupportMask);

        [PreserveSig]
        int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
    }

    private enum EDataFlow
    {
        eRender,
        eCapture,
        eAll
    }

    private enum ERole
    {
        eConsole,
        eMultimedia,
        eCommunications
    }

    private enum DeviceState
    {
        Active = 0x00000001,
        Disabled = 0x00000002,
        NotPresent = 0x00000004,
        Unplugged = 0x00000008,
        All = 0x0000000F
    }

    private enum CLSCTX
    {
        INPROC_SERVER = 0x1,
        INPROC_HANDLER = 0x2,
        LOCAL_SERVER = 0x4,
        INPROC_SERVER16 = 0x8,
        REMOTE_SERVER = 0x10,
        INPROC_HANDLER16 = 0x20,
        RESERVED1 = 0x40,
        RESERVED2 = 0x80,
        RESERVED3 = 0x100,
        RESERVED4 = 0x200,
        NO_CODE_DOWNLOAD = 0x400,
        RESERVED5 = 0x800,
        NO_CUSTOM_MARSHAL = 0x1000,
        ENABLE_CODE_DOWNLOAD = 0x2000,
        NO_FAILURE_LOG = 0x4000,
        DISABLE_AAA = 0x8000,
        ENABLE_AAA = 0x10000,
        FROM_DEFAULT_CONTEXT = 0x20000,
        ACTIVATE_X86_SERVER = 0x40000,
        ACTIVATE_32_BIT_SERVER = ACTIVATE_X86_SERVER,
        ACTIVATE_64_BIT_SERVER = 0x80000,
        ENABLE_CLOAKING = 0x100000,
        APPCONTAINER = 0x400000,
        ACTIVATE_AAA_AS_IU = 0x800000,
        RESERVED6 = 0x1000000,
        ACTIVATE_ARM32_SERVER = 0x2000000,
        PS_DLL = unchecked((int)0x80000000),
        ALL = INPROC_SERVER | INPROC_HANDLER | LOCAL_SERVER | INPROC_SERVER16 | REMOTE_SERVER | INPROC_HANDLER16
    }

    private enum StorageAccessMode
    {
        Read,
        Write,
        ReadWrite
    }

    [DllImport("ole32.dll")]
    private static extern int CoCreateInstance(ref Guid rclsid, nint pUnkOuter, uint dwClsContext, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

    public static Response GetVolume(VolumeGetRequest request)
    {
        try
        {
            var volume = GetDefaultEndpointVolume(EDataFlow.eRender);
            if (volume == null)
                return new ErrorResponse("Failed to get audio endpoint");

            volume.GetMasterVolumeLevelScalar(out float level);
            volume.GetMute(out bool mute);

            return new SuccessResponse<VolumeResponse>(new VolumeResponse
            {
                Level = (int)(level * 100),
                IsMuted = mute
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response SetVolume(VolumeSetRequest request)
    {
        try
        {
            var volume = GetDefaultEndpointVolume(EDataFlow.eRender);
            if (volume == null)
                return new ErrorResponse("Failed to get audio endpoint");

            float level = Math.Clamp(request.Level, 0, 100) / 100.0f;
            volume.SetMasterVolumeLevelScalar(level, 0);

            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response SetMute(VolumeMuteRequest request)
    {
        try
        {
            var volume = GetDefaultEndpointVolume(EDataFlow.eRender);
            if (volume == null)
                return new ErrorResponse("Failed to get audio endpoint");

            volume.SetMute(request.Mute, 0);

            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response ListDevices(AudioDeviceListRequest request)
    {
        try
        {
            var devices = new List<AudioDeviceInfo>();
            
            var enumerator = CreateDeviceEnumerator();
            if (enumerator == null)
                return new ErrorResponse("Failed to create device enumerator");

            enumerator.EnumAudioEndpoints(EDataFlow.eRender, DeviceState.Active, out var collection);
            collection.GetCount(out uint count);

            var defaultDevice = GetDefaultDevice(enumerator, EDataFlow.eRender);
            string? defaultId = null;
            defaultDevice?.GetId(out defaultId);

            for (uint i = 0; i < count; i++)
            {
                collection.Item(i, out var device);
                device.GetId(out string id);
                
                var props = GetDeviceProperties(device);
                
                devices.Add(new AudioDeviceInfo(
                    id,
                    props.name,
                    id == defaultId,
                    false,
                    0
                ));
            }

            return new SuccessResponse<AudioDeviceListResponse>(new AudioDeviceListResponse
            {
                Devices = devices.ToArray()
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    private static IMMDeviceEnumerator? CreateDeviceEnumerator()
    {
        Guid clsid = CLSID_MMDeviceEnumerator;
        Guid iid = IID_IMMDeviceEnumerator;
        int hr = CoCreateInstance(ref clsid, 0, (uint)CLSCTX.ALL, ref iid, out object obj);
        if (hr < 0) return null;
        return (IMMDeviceEnumerator)obj;
    }

    private static IAudioEndpointVolume? GetDefaultEndpointVolume(EDataFlow dataFlow)
    {
        var enumerator = CreateDeviceEnumerator();
        if (enumerator == null) return null;

        var device = GetDefaultDevice(enumerator, dataFlow);
        if (device == null) return null;

        Guid iid = typeof(IAudioEndpointVolume).GUID;
        device.Activate(ref iid, CLSCTX.ALL, 0, out object volumeObj);
        return (IAudioEndpointVolume)volumeObj;
    }

    private static IMMDevice? GetDefaultDevice(IMMDeviceEnumerator enumerator, EDataFlow dataFlow)
    {
        int hr = enumerator.GetDefaultAudioEndpoint(dataFlow, ERole.eMultimedia, out var device);
        if (hr < 0) return null;
        return device;
    }

    private static (string name, bool isMuted, float volume) GetDeviceProperties(IMMDevice device)
    {
        try
        {
            Guid iid = typeof(IAudioEndpointVolume).GUID;
            device.Activate(ref iid, CLSCTX.ALL, 0, out object volumeObj);
            var volume = (IAudioEndpointVolume)volumeObj;

            volume.GetMasterVolumeLevelScalar(out float vol);
            volume.GetMute(out bool mute);

            return ("Device", mute, vol);
        }
        catch
        {
            return ("Device", false, 0);
        }
    }
}
