using System.Runtime.InteropServices;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

/// <summary>
/// Windows 音频控制模块 - 使用 Win32 API (waveOut + 多媒体键)
/// </summary>
public static class AudioModule
{
    #region Win32 API Imports

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern int waveOutGetVolume(nint hwo, out uint pdwVolume);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern int waveOutSetVolume(nint hwo, uint dwVolume);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

    [DllImport("winmm.dll")]
    private static extern uint waveOutGetNumDevs();

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern int waveOutGetDevCaps(uint uDeviceID, ref WAVEOUTCAPS pwoc, uint cbwoc);

    // 虚拟键码
    private const byte VK_VOLUME_MUTE = 0xAD;
    private const byte VK_VOLUME_DOWN = 0xAE;
    private const byte VK_VOLUME_UP = 0xAF;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WAVEOUTCAPS
    {
        public ushort wMid;
        public ushort wPid;
        public uint vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname;
        public uint dwFormats;
        public ushort wChannels;
        public ushort wReserved1;
        public uint dwSupport;
    }

    #endregion

    /// <summary>
    /// 获取系统音量（0-100）
    /// 使用 waveOut API 尝试获取，如果失败返回错误
    /// </summary>
    public static Response GetVolume(VolumeGetRequest request)
    {
        try
        {
            // hwo = 0 表示主音量设备
            int result = waveOutGetVolume(0, out uint volume);
            
            if (result != 0)
            {
                // 错误码解释：
                // 5 = ERROR_ACCESS_DENIED (可能需要管理员权限)
                // 11 = MMSYSERR_NODRIVER (没有驱动)
                // 其他错误
                return new ErrorResponse(
                    $"Failed to get volume. Error code: {result}. " +
                    "This typically means the waveOut API cannot access the system master volume on modern Windows. " +
                    "Consider using multimedia keys for relative volume control instead."
                );
            }

            // 解析音量值（16-bit 左声道 + 16-bit 右声道）
            ushort left = (ushort)(volume >> 16);
            ushort right = (ushort)(volume & 0xFFFF);
            
            // 计算平均音量（0-65535 转换为 0-100）
            double avgVolume = (left + right) / 2.0;
            int level = (int)((avgVolume / 65535.0) * 100);
            
            // 判断是否静音
            bool isMuted = (left == 0 && right == 0);

            return new SuccessResponse<VolumeResponse>(new VolumeResponse
            {
                Level = level,
                IsMuted = isMuted
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Get volume failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 设置系统音量（0-100）
    /// 使用 waveOut API 尝试设置，如果失败返回错误
    /// </summary>
    public static Response SetVolume(VolumeSetRequest request)
    {
        try
        {
            // 将 0-100 转换为 0-65535
            int level = Math.Clamp(request.Level, 0, 100);
            ushort volumeLevel = (ushort)((level / 100.0) * 65535);
            
            // 组合左右声道（高 16 位左声道，低 16 位右声道）
            uint volume = ((uint)volumeLevel << 16) | volumeLevel;
            
            int result = waveOutSetVolume(0, volume);
            
            if (result != 0)
            {
                return new ErrorResponse(
                    $"Failed to set volume. Error code: {result}. " +
                    "This typically means the waveOut API cannot control the system master volume on modern Windows. " +
                    "Consider using VolumeUp/VolumeDown methods for relative control."
                );
            }

            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Set volume failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 设置静音状态
    /// 使用多媒体键（系统级，不受 waveOut 限制）
    /// </summary>
    public static Response SetMute(VolumeMuteRequest request)
    {
        try
        {
            // 发送静音键（切换状态）
            keybd_event(VK_VOLUME_MUTE, 0, 0, 0);
            keybd_event(VK_VOLUME_MUTE, 0, KEYEVENTF_KEYUP, 0);
            
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Set mute failed: {ex.Message}");
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
        try
        {
            var devices = new List<AudioDeviceInfo>();
            
            // 添加默认设备
            devices.Add(new AudioDeviceInfo(
                "default",
                "Default Audio Device",
                true,
                false,
                0
            ));

            // 获取 waveOut 设备数量
            uint numDevices = waveOutGetNumDevs();
            
            // 枚举所有设备
            for (uint i = 0; i < numDevices; i++)
            {
                var caps = new WAVEOUTCAPS();
                if (waveOutGetDevCaps(i, ref caps, (uint)Marshal.SizeOf<WAVEOUTCAPS>()) == 0)
                {
                    devices.Add(new AudioDeviceInfo(
                        i.ToString(),
                        caps.szPname,
                        false,
                        false,
                        0
                    ));
                }
            }

            return new SuccessResponse<AudioDeviceListResponse>(new AudioDeviceListResponse
            {
                Devices = devices.ToArray()
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"List devices failed: {ex.Message}");
        }
    }
}
