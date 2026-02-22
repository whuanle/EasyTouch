namespace EasyTouch.Core.Models;

public class ProcessListRequest : Request
{
    public string? NameFilter { get; set; }
}

public class ProcessListResponse
{
    public ProcessInfo[] Processes { get; set; } = [];
}

public class ProcessStartRequest : Request
{
    public string FileName { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    public string? WorkingDirectory { get; set; }
    public bool WaitForExit { get; set; } = false;
}

public class ProcessStartResponse
{
    public uint? ProcessId { get; set; }
}

public class ProcessKillRequest : Request
{
    public uint? ProcessId { get; set; }
    public string? ProcessName { get; set; }
    public bool Force { get; set; } = false;
}

public class SystemInfoResponse
{
    public SystemInfo Info { get; set; } = null!;
}

public class CpuInfoResponse
{
    public CpuInfo Info { get; set; } = null!;
    public float UsagePercent { get; set; }
}

public class MemoryInfoResponse
{
    public long TotalPhysical { get; set; }
    public long AvailablePhysical { get; set; }
    public long UsedPhysical { get; set; }
    public float UsagePercent { get; set; }
}

public class DiskListResponse
{
    public DiskInfo[] Disks { get; set; } = [];
}

public class OsInfoResponse
{
    public string Version { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public long UptimeMilliseconds { get; set; }
}
