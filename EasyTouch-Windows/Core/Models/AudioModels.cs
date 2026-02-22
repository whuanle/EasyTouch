namespace EasyTouch.Core.Models;

public class VolumeGetRequest : Request
{
}

public class VolumeSetRequest : Request
{
    public int Level { get; set; }
}

public class VolumeMuteRequest : Request
{
    public bool Mute { get; set; } = true;
}

public class VolumeResponse
{
    public int Level { get; set; }
    public bool IsMuted { get; set; }
}

public class AudioDeviceListRequest : Request
{
}

public class AudioDeviceListResponse
{
    public AudioDeviceInfo[] Devices { get; set; } = [];
}

public class AudioDeviceSetDefaultRequest : Request
{
    public string DeviceId { get; set; } = string.Empty;
    public bool IsPlayback { get; set; } = true;
}
